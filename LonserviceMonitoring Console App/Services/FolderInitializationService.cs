using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LonserviceMonitoring.Models;

namespace LonserviceMonitoring.Services
{
    public interface IFolderInitializationService
    {
        Task InitializeAsync();
    }

    public class FolderInitializationService : IFolderInitializationService
    {
        private readonly MonitoringSettings _monitoringSettings;
        private readonly ILogger<FolderInitializationService> _logger;

        public FolderInitializationService(
            IOptions<MonitoringSettings> monitoringSettings,
            ILogger<FolderInitializationService> logger)
        {
            _monitoringSettings = monitoringSettings.Value;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing folder structure...");

            var foldersToCreate = new[]
            {
                _monitoringSettings.SourcePath,
                _monitoringSettings.WorkFolder,
                _monitoringSettings.LoadedFolder
            };

            foreach (var folder in foldersToCreate)
            {
                if (!Directory.Exists(folder))
                {
                    try
                    {
                        Directory.CreateDirectory(folder);
                        _logger.LogInformation("Created folder: {Folder}", folder);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create folder: {Folder}", folder);
                        throw;
                    }
                }
                else
                {
                    _logger.LogInformation("Folder already exists: {Folder}", folder);
                }
            }

            _logger.LogInformation("Folder structure initialization completed");
            await Task.CompletedTask;
        }
    }
}
