using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using LonserviceMonitoring.Models;

namespace LonserviceMonitoring.Services
{
    public class MonitoringService : BackgroundService
    {
        private readonly MonitoringSettings _monitoringSettings;
        private readonly IFolderInitializationService _folderInitializationService;
        private readonly IFolderMonitoringService _folderMonitoringService;
        private readonly IEmailMonitoringService _emailMonitoringService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<MonitoringService> _logger;
        private readonly SemaphoreSlim _processingLock = new SemaphoreSlim(1, 1);

        public MonitoringService(
            IOptions<MonitoringSettings> monitoringSettings,
            IFolderInitializationService folderInitializationService,
            IFolderMonitoringService folderMonitoringService,
            IEmailMonitoringService emailMonitoringService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<MonitoringService> logger)
        {
            _monitoringSettings = monitoringSettings.Value;
            _folderInitializationService = folderInitializationService;
            _folderMonitoringService = folderMonitoringService;
            _emailMonitoringService = emailMonitoringService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Lonservice Monitoring Service starting...");
            
            try
            {
                // Initialize folder structure
                await _folderInitializationService.InitializeAsync();
                
                // Create a scope for logging service initialization
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var loggingService = scope.ServiceProvider.GetRequiredService<ILoggingService>();
                    await loggingService.LogAsync("Info", "MonitoringService", "Service started successfully");
                }

                // Subscribe to CSV file detection events
                _folderMonitoringService.CsvFileDetected += OnCsvFileDetected;
                _emailMonitoringService.CsvFileDetected += OnCsvFileDetected;

                // Start monitoring based on configuration
                if (_monitoringSettings.MonitoringType.Equals("PHYSICAL_PATH", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Starting physical path monitoring for: {SourcePath}", _monitoringSettings.SourcePath);
                    
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var loggingService = scope.ServiceProvider.GetRequiredService<ILoggingService>();
                        await loggingService.LogAsync("Info", "MonitoringService", 
                            $"Starting physical path monitoring for: {_monitoringSettings.SourcePath}");
                    }
                    
                    await _folderMonitoringService.StartMonitoringAsync(stoppingToken);
                }
                else if (_monitoringSettings.MonitoringType.Equals("EMAIL", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Starting Office 365 email monitoring");
                    
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var loggingService = scope.ServiceProvider.GetRequiredService<ILoggingService>();
                        await loggingService.LogAsync("Info", "MonitoringService", "Starting Office 365 email monitoring");
                    }
                    
                    await _emailMonitoringService.StartMonitoringAsync(stoppingToken);
                }
                else
                {
                    _logger.LogError("Invalid monitoring type: {MonitoringType}. Expected: PHYSICAL_PATH or EMAIL", 
                        _monitoringSettings.MonitoringType);
                    
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var loggingService = scope.ServiceProvider.GetRequiredService<ILoggingService>();
                        await loggingService.LogAsync("Error", "MonitoringService", 
                            $"Invalid monitoring type: {_monitoringSettings.MonitoringType}");
                    }
                    
                    throw new InvalidOperationException($"Invalid monitoring type: {_monitoringSettings.MonitoringType}");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Monitoring service cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in monitoring service");
                
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var loggingService = scope.ServiceProvider.GetRequiredService<ILoggingService>();
                    await loggingService.LogAsync("Error", "MonitoringService", 
                        "Fatal error in monitoring service", exception: ex.ToString());
                }
                throw;
            }
            finally
            {
                // Unsubscribe from events
                _folderMonitoringService.CsvFileDetected -= OnCsvFileDetected;
                _emailMonitoringService.CsvFileDetected -= OnCsvFileDetected;
                
                _logger.LogInformation("Lonservice Monitoring Service stopped");
                
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var loggingService = scope.ServiceProvider.GetRequiredService<ILoggingService>();
                    await loggingService.LogAsync("Info", "MonitoringService", "Service stopped");
                }
            }
        }

        private async void OnCsvFileDetected(object? sender, string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            _logger.LogInformation("CSV file detected: {FileName}", fileName);

            // Ensure only one file is processed at a time to avoid DbContext conflicts
            await _processingLock.WaitAsync();
            
            try
            {
                // Create a new scope for each file processing to get fresh service instances
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var csvProcessingService = scope.ServiceProvider.GetRequiredService<ICsvProcessingService>();
                    var loggingService = scope.ServiceProvider.GetRequiredService<ILoggingService>();
                    
                    await loggingService.LogAsync("Info", "MonitoringService", 
                        $"CSV file detected: {fileName}", fileName);
                    
                    // Process the CSV file
                    await csvProcessingService.ProcessCsvFileAsync(filePath);
                    
                    _logger.LogInformation("CSV file processed successfully: {FileName}", fileName);
                    await loggingService.LogAsync("Info", "MonitoringService", 
                        $"CSV file processed successfully: {fileName}", fileName);
                    
                    // Return to waiting state for user visibility
                    _logger.LogInformation("Waiting for CSV files...");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CSV file: {FileName}", fileName);
                
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var loggingService = scope.ServiceProvider.GetRequiredService<ILoggingService>();
                    await loggingService.LogAsync("Error", "MonitoringService", 
                        $"Error processing CSV file: {fileName}", fileName, exception: ex.ToString());
                }
                
                // Don't rethrow - continue monitoring even if one file fails
                _logger.LogInformation("Continuing monitoring despite error with file: {FileName}", fileName);
                
                // Return to waiting state for user visibility
                _logger.LogInformation("Waiting for CSV files...");
            }
            finally
            {
                _processingLock.Release();
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Monitoring service stop requested");
            await base.StopAsync(cancellationToken);
        }
    }
}
