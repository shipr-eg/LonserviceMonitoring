using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using LonserviceMonitoring.Models;
using LonserviceMonitoring.Data;

namespace LonserviceMonitoring.Services
{
    public interface IDatabaseSchemaService
    {
        Task<bool> EnsureColumnsExistAsync(List<string> columnNames);
        Task<List<string>> GetExistingColumnsAsync();
    }

    public class DatabaseSchemaService : IDatabaseSchemaService
    {
        private readonly DatabaseSettings _databaseSettings;
        private readonly ILogger<DatabaseSchemaService> _logger;
        private readonly ILoggingService _loggingService;

        public DatabaseSchemaService(
            IOptions<DatabaseSettings> databaseSettings,
            ILogger<DatabaseSchemaService> logger,
            ILoggingService loggingService)
        {
            _databaseSettings = databaseSettings.Value;
            _logger = logger;
            _loggingService = loggingService;
        }

        public async Task<bool> EnsureColumnsExistAsync(List<string> columnNames)
        {
            try
            {
                var existingColumns = await GetExistingColumnsAsync();
                var columnsToAdd = columnNames
                    .Where(col => !existingColumns.Contains(col, StringComparer.OrdinalIgnoreCase))
                    .Where(col => !IsSystemColumn(col))
                    .ToList();

                if (!columnsToAdd.Any())
                {
                    return true; // All columns already exist
                }

                _logger.LogInformation("Adding new columns to CsvData table: {Columns}", string.Join(", ", columnsToAdd));

                using var connection = new SqlConnection(_databaseSettings.ConnectionString);
                await connection.OpenAsync();

                foreach (var columnName in columnsToAdd)
                {
                    var sanitizedColumnName = SanitizeColumnName(columnName);
                    var addColumnSql = $@"
                        IF NOT EXISTS (
                            SELECT * FROM sys.columns 
                            WHERE object_id = OBJECT_ID('CsvData') 
                            AND name = '{sanitizedColumnName}'
                        )
                        BEGIN
                            ALTER TABLE CsvData 
                            ADD [{sanitizedColumnName}] NVARCHAR(MAX) NULL
                        END";

                    using var command = new SqlCommand(addColumnSql, connection);
                    await command.ExecuteNonQueryAsync();

                    _logger.LogInformation("Added column: {ColumnName} to CsvData table", sanitizedColumnName);
                    
                    await _loggingService.LogAsync("Info", "DatabaseSchemaService",
                        $"Added new column '{sanitizedColumnName}' to CsvData table");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding columns to database");
                await _loggingService.LogAsync("Error", "DatabaseSchemaService",
                    "Failed to add new columns to database", exception: ex.ToString());
                return false;
            }
        }

        public async Task<List<string>> GetExistingColumnsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_databaseSettings.ConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT COLUMN_NAME 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'CsvData' 
                    AND TABLE_SCHEMA = 'dbo'
                    ORDER BY ORDINAL_POSITION";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                var columns = new List<string>();
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(0)); // COLUMN_NAME is the first column
                }

                return columns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving existing columns");
                return new List<string>();
            }
        }

        private static bool IsSystemColumn(string columnName)
        {
            var systemColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Id", "Contacted", "Notes", "CreatedDate", "ModifiedDate",
                "SourceFileName", "TimeBlock", "AdditionalData"
            };

            return systemColumns.Contains(columnName);
        }

        private static string SanitizeColumnName(string columnName)
        {
            // Remove special characters and replace spaces with underscores
            var sanitized = System.Text.RegularExpressions.Regex.Replace(columnName, @"[^a-zA-Z0-9_]", "_");
            
            // Ensure it doesn't start with a number
            if (char.IsDigit(sanitized[0]))
            {
                sanitized = "Col_" + sanitized;
            }

            // Limit length to 128 characters (SQL Server limit)
            if (sanitized.Length > 128)
            {
                sanitized = sanitized.Substring(0, 128);
            }

            return sanitized;
        }
    }
}
