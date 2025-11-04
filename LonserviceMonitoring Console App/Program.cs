using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using LonserviceMonitoring.Data;
using LonserviceMonitoring.Models;
using LonserviceMonitoring.Services;

namespace LonserviceMonitoring
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Setup Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/lonservice-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting Lonservice Monitoring Application");

                var builder = Host.CreateDefaultBuilder(args);

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                });

                builder.ConfigureServices((context, services) =>
                {
                    // Configuration
                    services.Configure<MonitoringSettings>(
                        context.Configuration.GetSection("MonitoringSettings"));
                    services.Configure<DatabaseSettings>(
                        context.Configuration.GetSection("DatabaseSettings"));
                    services.Configure<EmailSettings>(
                        context.Configuration.GetSection("EmailSettings"));
                    services.Configure<CsvSettings>(
                        context.Configuration.GetSection("CsvSettings"));

                    // Database
                    var connectionString = context.Configuration.GetConnectionString("DefaultConnection") 
                        ?? context.Configuration.GetSection("DatabaseSettings:ConnectionString").Value;
                    
                    services.AddDbContext<LonserviceContext>(options =>
                    {
                        options.UseSqlServer(connectionString);
                        // Disable OUTPUT clause due to database triggers
                        options.UseSqlServer(connectionString, sqlOptions => 
                        {
                            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        });
                    });

                    // Services
                    services.AddTransient<IFolderInitializationService, FolderInitializationService>();
                    services.AddTransient<IFolderMonitoringService, FolderMonitoringService>();
                    services.AddTransient<IEmailMonitoringService, EmailMonitoringService>();
                    services.AddTransient<ICsvProcessingService, CsvProcessingService>();
                    services.AddTransient<ILoggingService, LoggingService>();
                    services.AddTransient<IDatabaseSchemaService, DatabaseSchemaService>();

                    // Background service
                    services.AddHostedService<MonitoringService>();
                });

                builder.UseSerilog();

                var host = builder.Build();

                // Ensure database is created and properly initialized
                using (var scope = host.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<LonserviceContext>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    
                    try
                    {
                        logger.LogInformation("Initializing database for LonserviceMonitoring...");
                        
                        // Ensure database exists (creates if not exists)
                        var created = await context.Database.EnsureCreatedAsync();
                        if (created)
                        {
                            logger.LogInformation("✅ Database created successfully: LonserviceMonitoringDB");
                        }
                        else
                        {
                            logger.LogInformation("✅ Database already exists: LonserviceMonitoringDB");
                        }
                        
                        // Verify all required tables exist by checking each DbSet
                        await VerifyDatabaseTablesAsync(context, logger);
                        
                        logger.LogInformation("✅ Database initialization completed successfully");
                        logger.LogInformation("📊 Database ready for CSV processing with semicolon delimiter support");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error during database initialization");
                        throw;
                    }
                }

                // Setup proper signal handling for continuous operation
                var cancellationTokenSource = new CancellationTokenSource();
                
                // Handle Ctrl+C gracefully
                Console.CancelKeyPress += (sender, e) =>
                {
                    Log.Information("Application shutdown requested by user (Ctrl+C)");
                    e.Cancel = true; // Prevent immediate termination
                    cancellationTokenSource.Cancel();
                };

                Log.Information("Application started. Press Ctrl+C to shut down.");
                Log.Information("Monitoring will continue until manually stopped...");

                // Run the host with proper cancellation handling
                await host.RunAsync(cancellationTokenSource.Token);
                
                Log.Information("Application shutdown completed gracefully");
            }
            catch (OperationCanceledException)
            {
                Log.Information("Application was cancelled");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
                throw; // Re-throw to ensure proper exit code
            }
            finally
            {
                Log.Information("Cleaning up and closing logs...");
                await Log.CloseAndFlushAsync();
            }
        }

        private static async Task VerifyDatabaseTablesAsync(LonserviceContext context, Microsoft.Extensions.Logging.ILogger<Program> logger)
        {
            try
            {
                // Verify each table exists by attempting to count records
                var tables = new Dictionary<string, Func<Task<int>>>
                {
                    ["CsvData"] = async () => await context.CsvData.CountAsync(),
                    ["ProcessingLogs"] = async () => await context.ProcessingLogs.CountAsync(),
                    ["AuditLogs"] = async () => await context.AuditLogs.CountAsync(),
                    ["CsvProcessingHistory"] = async () => await context.CsvProcessingHistory.CountAsync()
                };

                logger.LogInformation("🔍 Verifying database tables...");

                foreach (var table in tables)
                {
                    try
                    {
                        var count = await table.Value();
                        logger.LogInformation("✅ Table '{TableName}' verified - {RecordCount} records", table.Key, count);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("❌ Table '{TableName}' verification failed: {Error}", table.Key, ex.Message);
                        throw;
                    }
                }

                logger.LogInformation("📋 Database Schema Summary:");
                logger.LogInformation("   • CsvData: Main CSV record storage with semicolon delimiter support");
                logger.LogInformation("   • ProcessingLogs: Application and processing logs");
                logger.LogInformation("   • AuditLogs: Automatic change tracking for all records");
                logger.LogInformation("   • CsvProcessingHistory: File processing history and metrics");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Database table verification failed");
                throw;
            }
        }
    }
}
