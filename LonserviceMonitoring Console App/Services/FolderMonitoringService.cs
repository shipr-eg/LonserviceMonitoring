using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LonserviceMonitoring.Models;

namespace LonserviceMonitoring.Services
{
    public interface IFolderMonitoringService
    {
        Task StartMonitoringAsync(CancellationToken cancellationToken);
        event EventHandler<string> CsvFileDetected;
    }

    public class FolderMonitoringService : IFolderMonitoringService, IDisposable
    {
        private readonly MonitoringSettings _monitoringSettings;
        private readonly ILogger<FolderMonitoringService> _logger;
        private FileSystemWatcher? _fileWatcher;
        private readonly Dictionary<string, DateTime> _processedFiles = new Dictionary<string, DateTime>();

        public event EventHandler<string>? CsvFileDetected;

        public FolderMonitoringService(
            IOptions<MonitoringSettings> monitoringSettings,
            ILogger<FolderMonitoringService> logger)
        {
            _monitoringSettings = monitoringSettings.Value;
            _logger = logger;
        }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting folder monitoring for: {SourcePath}", _monitoringSettings.SourcePath);

            if (!Directory.Exists(_monitoringSettings.SourcePath))
            {
                _logger.LogError("Source path does not exist: {SourcePath}", _monitoringSettings.SourcePath);
                throw new DirectoryNotFoundException($"Source path does not exist: {_monitoringSettings.SourcePath}");
            }

            try
            {
                // Process existing files first
                await ProcessExistingFilesAsync();

                // Set up file system watcher for new files
                SetupFileWatcher();

                // Keep the monitoring running indefinitely until cancellation is requested
                _logger.LogInformation("Folder monitoring started successfully. Waiting for CSV files...");
                
                // Periodic scan loop as backup for FileSystemWatcher
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Wait 10 seconds between scans (reduced for testing)
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                        
                        // Perform periodic scan to catch files that FileSystemWatcher might miss
                        _logger.LogInformation("Performing periodic scan...");
                        await PeriodicScanAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Monitoring cancellation requested");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in monitoring loop, continuing...");
                        // Continue monitoring even if there's an error
                        await Task.Delay(TimeSpan.FromSeconds(30), CancellationToken.None);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Folder monitoring cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in folder monitoring");
                throw;
            }
            finally
            {
                _fileWatcher?.Dispose();
                _logger.LogInformation("Folder monitoring stopped");
            }
        }

        private async Task ProcessExistingFilesAsync()
        {
            _logger.LogInformation("Processing existing CSV files in source folder...");

            try
            {
                var csvFiles = Directory.GetFiles(_monitoringSettings.SourcePath, "*.csv", SearchOption.TopDirectoryOnly);
                
                foreach (var file in csvFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var lastModified = File.GetLastWriteTime(file);
                    
                    // Check if file is new or has been modified since last processing
                    if (!_processedFiles.ContainsKey(fileName) || _processedFiles[fileName] < lastModified)
                    {
                        _logger.LogInformation("Found existing CSV file: {FileName}", fileName);
                        _processedFiles[fileName] = lastModified;
                        CsvFileDetected?.Invoke(this, file);
                        await Task.Delay(100); // Small delay between files
                    }
                }

                _logger.LogInformation("Finished processing {Count} existing CSV files", csvFiles.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing existing files");
            }
        }

        private async Task PeriodicScanAsync()
        {
            try
            {
                var csvFiles = Directory.GetFiles(_monitoringSettings.SourcePath, "*.csv", SearchOption.TopDirectoryOnly);
                
                foreach (var file in csvFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var lastModified = File.GetLastWriteTime(file);
                    
                    // Check if file is new or has been modified since last processing
                    if (!_processedFiles.ContainsKey(fileName) || _processedFiles[fileName] < lastModified)
                    {
                        _logger.LogInformation("Periodic scan detected CSV file: {FileName}", fileName);
                        _processedFiles[fileName] = lastModified;
                        CsvFileDetected?.Invoke(this, file);
                        await Task.Delay(100); // Small delay between files
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic scan");
            }
        }

        private void SetupFileWatcher()
        {
            _fileWatcher = new FileSystemWatcher(_monitoringSettings.SourcePath)
            {
                Filter = "*.csv",
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _fileWatcher.Created += OnFileCreated;
            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Error += OnError;

            _logger.LogInformation("File system watcher setup completed for CSV files");
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            ProcessFileEvent(e.FullPath, "Created");
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            ProcessFileEvent(e.FullPath, "Changed");
        }

        private void ProcessFileEvent(string filePath, string eventType)
        {
            var fileName = Path.GetFileName(filePath);
            
            try
            {
                // Wait a bit to ensure file is completely written
                Thread.Sleep(1000);

                if (File.Exists(filePath) && IsFileAccessible(filePath))
                {
                    var lastModified = File.GetLastWriteTime(filePath);
                    
                    // Check if file is new or has been modified since last processing
                    if (!_processedFiles.ContainsKey(fileName) || _processedFiles[fileName] < lastModified)
                    {
                        _logger.LogInformation("CSV file {EventType}: {FileName}", eventType, fileName);
                        _processedFiles[fileName] = lastModified;
                        CsvFileDetected?.Invoke(this, filePath);
                    }
                    else
                    {
                        _logger.LogDebug("File {FileName} already processed (last modified: {LastModified})", fileName, lastModified);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file event for: {FilePath}", filePath);
            }
        }

        private static bool IsFileAccessible(string filePath)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            _logger.LogError(e.GetException(), "File system watcher error");
        }

        public void Dispose()
        {
            _fileWatcher?.Dispose();
        }
    }
}
