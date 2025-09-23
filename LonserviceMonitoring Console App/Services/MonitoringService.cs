using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LonserviceMonitoring.Models;

namespace LonserviceMonitoring.Services
{
    public class MonitoringService : BackgroundService
    {
        private readonly MonitoringSettings _monitoringSettings;
        private readonly IFolderInitializationService _folderInitializationService;
        private readonly IFolderMonitoringService _folderMonitoringService;
        private readonly IEmailMonitoringService _emailMonitoringService;
        private readonly ICsvProcessingService _csvProcessingService;
        private readonly ILoggingService _loggingService;
        private readonly ILogger<MonitoringService> _logger;

        public MonitoringService(
            IOptions<MonitoringSettings> monitoringSettings,
            IFolderInitializationService folderInitializationService,
            IFolderMonitoringService folderMonitoringService,
            IEmailMonitoringService emailMonitoringService,
            ICsvProcessingService csvProcessingService,
            ILoggingService loggingService,
            ILogger<MonitoringService> logger)
        {
            _monitoringSettings = monitoringSettings.Value;
            _folderInitializationService = folderInitializationService;
            _folderMonitoringService = folderMonitoringService;
            _emailMonitoringService = emailMonitoringService;
            _csvProcessingService = csvProcessingService;
            _loggingService = loggingService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Lonservice Monitoring Service starting...");
            
            try
            {
                // Initialize folder structure
                await _folderInitializationService.InitializeAsync();
                await _loggingService.LogAsync("Info", "MonitoringService", "Service started successfully");

                // Subscribe to CSV file detection events
                _folderMonitoringService.CsvFileDetected += OnCsvFileDetected;
                _emailMonitoringService.CsvFileDetected += OnCsvFileDetected;

                // Start monitoring based on configuration
                if (_monitoringSettings.MonitoringType.Equals("PHYSICAL_PATH", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Starting physical path monitoring for: {SourcePath}", _monitoringSettings.SourcePath);
                    await _loggingService.LogAsync("Info", "MonitoringService", 
                        $"Starting physical path monitoring for: {_monitoringSettings.SourcePath}");
                    
                    await _folderMonitoringService.StartMonitoringAsync(stoppingToken);
                }
                else if (_monitoringSettings.MonitoringType.Equals("EMAIL", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Starting Office 365 email monitoring");
                    await _loggingService.LogAsync("Info", "MonitoringService", "Starting Office 365 email monitoring");
                    
                    await _emailMonitoringService.StartMonitoringAsync(stoppingToken);
                }
                else
                {
                    _logger.LogError("Invalid monitoring type: {MonitoringType}. Expected: PHYSICAL_PATH or EMAIL", 
                        _monitoringSettings.MonitoringType);
                    
                    await _loggingService.LogAsync("Error", "MonitoringService", 
                        $"Invalid monitoring type: {_monitoringSettings.MonitoringType}");
                    
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
                await _loggingService.LogAsync("Error", "MonitoringService", 
                    "Fatal error in monitoring service", exception: ex.ToString());
                throw;
            }
            finally
            {
                // Unsubscribe from events
                _folderMonitoringService.CsvFileDetected -= OnCsvFileDetected;
                _emailMonitoringService.CsvFileDetected -= OnCsvFileDetected;
                
                _logger.LogInformation("Lonservice Monitoring Service stopped");
                await _loggingService.LogAsync("Info", "MonitoringService", "Service stopped");
            }
        }

        private async void OnCsvFileDetected(object? sender, string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            _logger.LogInformation("CSV file detected: {FileName}", fileName);

            try
            {
                await _loggingService.LogAsync("Info", "MonitoringService", 
                    $"CSV file detected: {fileName}", fileName);
                
                // Process the CSV file
                await _csvProcessingService.ProcessCsvFileAsync(filePath);
                
                _logger.LogInformation("CSV file processed successfully: {FileName}", fileName);
                await _loggingService.LogAsync("Info", "MonitoringService", 
                    $"CSV file processed successfully: {fileName}", fileName);
                
                // Return to waiting state for user visibility
                _logger.LogInformation("Waiting for CSV files...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CSV file: {FileName}", fileName);
                await _loggingService.LogAsync("Error", "MonitoringService", 
                    $"Error processing CSV file: {fileName}", fileName, exception: ex.ToString());
                
                // Don't rethrow - continue monitoring even if one file fails
                _logger.LogInformation("Continuing monitoring despite error with file: {FileName}", fileName);
                
                // Return to waiting state for user visibility
                _logger.LogInformation("Waiting for CSV files...");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Monitoring service stop requested");
            await base.StopAsync(cancellationToken);
        }
    }
}
