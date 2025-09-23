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
                        options.UseSqlServer(connectionString));

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

                // Ensure database is created and migrated
                using (var scope = host.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<LonserviceContext>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    
                    try
                    {
                        logger.LogInformation("Ensuring database is created...");
                        await context.Database.EnsureCreatedAsync();
                        logger.LogInformation("Database initialization completed");
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
    }
}
