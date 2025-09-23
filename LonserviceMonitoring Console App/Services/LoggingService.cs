using Microsoft.Extensions.Logging;
using LonserviceMonitoring.Models;
using LonserviceMonitoring.Data;

namespace LonserviceMonitoring.Services
{
    public interface ILoggingService
    {
        Task LogAsync(string logLevel, string source, string message, string? fileName = null, 
            string? timeBlock = null, string? exception = null, Dictionary<string, object>? additionalData = null);
        Task LogCsvOperationAsync(string operation, string fileName, string timeBlock, string details);
        Task<List<ProcessingLog>> GetRecentLogsAsync(int count = 100);
        Task<List<ProcessingLog>> GetLogsByFileAsync(string fileName);
    }

    public class LoggingService : ILoggingService
    {
        private readonly LonserviceContext _context;
        private readonly ILogger<LoggingService> _logger;

        public LoggingService(LonserviceContext context, ILogger<LoggingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(string logLevel, string source, string message, string? fileName = null, 
            string? timeBlock = null, string? exception = null, Dictionary<string, object>? additionalData = null)
        {
            try
            {
                var log = new ProcessingLog
                {
                    LogLevel = logLevel,
                    Source = source,
                    Message = message,
                    FileName = fileName,
                    TimeBlock = timeBlock,
                    Exception = exception,
                    AdditionalData = additionalData != null 
                        ? System.Text.Json.JsonSerializer.Serialize(additionalData) 
                        : "{}"
                };

                _context.ProcessingLogs.Add(log);
                await _context.SaveChangesAsync();

                // Also log to standard logger
                switch (logLevel.ToLowerInvariant())
                {
                    case "error":
                        _logger.LogError("{Source}: {Message} - File: {FileName}, TimeBlock: {TimeBlock}", 
                            source, message, fileName, timeBlock);
                        break;
                    case "warning":
                        _logger.LogWarning("{Source}: {Message} - File: {FileName}, TimeBlock: {TimeBlock}", 
                            source, message, fileName, timeBlock);
                        break;
                    case "info":
                        _logger.LogInformation("{Source}: {Message} - File: {FileName}, TimeBlock: {TimeBlock}", 
                            source, message, fileName, timeBlock);
                        break;
                    default:
                        _logger.LogDebug("{Source}: {Message} - File: {FileName}, TimeBlock: {TimeBlock}", 
                            source, message, fileName, timeBlock);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to processing log");
            }
        }

        public async Task LogCsvOperationAsync(string operation, string fileName, string timeBlock, string details)
        {
            await LogAsync("Info", "CsvOperations", $"{operation}: {details}", fileName, timeBlock);
        }

        public async Task<List<ProcessingLog>> GetRecentLogsAsync(int count = 100)
        {
            return await Task.FromResult(
                _context.ProcessingLogs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(count)
                    .ToList()
            );
        }

        public async Task<List<ProcessingLog>> GetLogsByFileAsync(string fileName)
        {
            return await Task.FromResult(
                _context.ProcessingLogs
                    .Where(l => l.FileName == fileName)
                    .OrderByDescending(l => l.Timestamp)
                    .ToList()
            );
        }
    }
}
