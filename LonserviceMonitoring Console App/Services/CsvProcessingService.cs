using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.Json;
using System.Dynamic;
using System.Data;
using Microsoft.Data.SqlClient;
using LonserviceMonitoring.Models;
using LonserviceMonitoring.Data;

namespace LonserviceMonitoring.Services
{
    public interface ICsvProcessingService
    {
        Task ProcessCsvFileAsync(string filePath);
    }

    public class CsvProcessingService : ICsvProcessingService
    {
        private readonly LonserviceContext _context;
        private readonly MonitoringSettings _monitoringSettings;
        private readonly CsvSettings _csvSettings;
        private readonly DatabaseSettings _databaseSettings;
        private readonly ILogger<CsvProcessingService> _logger;
        private readonly ILoggingService _loggingService;
        private readonly IDatabaseSchemaService _databaseSchemaService;

        public CsvProcessingService(
            LonserviceContext context,
            IOptions<MonitoringSettings> monitoringSettings,
            IOptions<CsvSettings> csvSettings,
            IOptions<DatabaseSettings> databaseSettings,
            ILogger<CsvProcessingService> logger,
            ILoggingService loggingService,
            IDatabaseSchemaService databaseSchemaService)
        {
            _context = context;
            _monitoringSettings = monitoringSettings.Value;
            _csvSettings = csvSettings.Value;
            _databaseSettings = databaseSettings.Value;
            _logger = logger;
            _loggingService = loggingService;
            _databaseSchemaService = databaseSchemaService;
        }

        public async Task ProcessCsvFileAsync(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var timeBlock = DateTime.Now.ToString("ddMMyyyy_HHmm");
            var processingHistory = new CsvProcessingHistory
            {
                FileName = fileName,
                TimeBlock = timeBlock,
                SourcePath = filePath,
                Status = "PROCESSING"
            };

            var processingLog = new List<string>();
            
            try
            {
                processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting CSV processing for: {fileName}");
                _logger.LogInformation("Starting CSV processing for: {FileName}", fileName);

                // Create time-block work folder
                var workFolderPath = Path.Combine(_monitoringSettings.WorkFolder, timeBlock);
                Directory.CreateDirectory(workFolderPath);

                // Move/Copy file to work folder with timestamp
                var newFileName = AddTimestampToFileName(fileName, timeBlock);
                var workFilePath = Path.Combine(workFolderPath, newFileName);
                
                if (File.Exists(filePath))
                {
                    File.Copy(filePath, workFilePath, true);
                    processingHistory.WorkPath = workFilePath;
                    processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] File copied to work folder: {workFilePath}");
                }

                // Process CSV data
                var (recordsProcessed, recordsSkipped, columns) = await ProcessCsvDataAsync(workFilePath, timeBlock, fileName, processingLog);

                // Ensure database schema supports new columns
                await EnsureDatabaseSchemaAsync(columns, processingLog);

                // Move to loaded folder
                var loadedFolderPath = Path.Combine(_monitoringSettings.LoadedFolder, $"{timeBlock}_Loaded");
                Directory.CreateDirectory(loadedFolderPath);
                var loadedFilePath = Path.Combine(loadedFolderPath, newFileName);
                File.Move(workFilePath, loadedFilePath);
                
                processingHistory.LoadedPath = loadedFilePath;
                processingHistory.RecordsProcessed = recordsProcessed;
                processingHistory.RecordsSkipped = recordsSkipped;
                processingHistory.Status = "SUCCESS";
                
                processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] File moved to loaded folder: {loadedFilePath}");
                processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Processing completed. Records processed: {recordsProcessed}, Skipped: {recordsSkipped}");

                // Delete original file if it was from source folder
                if (filePath.StartsWith(_monitoringSettings.SourcePath) && File.Exists(filePath))
                {
                    File.Delete(filePath);
                    processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Original file deleted from source folder");
                }

                _logger.LogInformation("CSV processing completed for: {FileName}. Processed: {Processed}, Skipped: {Skipped}", 
                    fileName, recordsProcessed, recordsSkipped);
            }
            catch (Exception ex)
            {
                processingHistory.Status = "ERROR";
                processingHistory.ErrorMessage = ex.Message;
                processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {ex.Message}");
                
                _logger.LogError(ex, "Error processing CSV file: {FileName}", fileName);
                
                await _loggingService.LogAsync("Error", "CsvProcessingService", 
                    $"Failed to process CSV file: {fileName}", fileName, timeBlock, ex.ToString());
            }
            finally
            {
                processingHistory.ProcessingLog = string.Join("\n", processingLog);
                _context.CsvProcessingHistory.Add(processingHistory);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<(int recordsProcessed, int recordsSkipped, List<string> columns)> ProcessCsvDataAsync(
            string filePath, string timeBlock, string fileName, List<string> processingLog)
        {
            var recordsProcessed = 0;
            var recordsSkipped = 0;
            var detectedColumns = new List<string>();

            // Detect delimiter if auto-detection is enabled
            char delimiter = ',';
            if (_csvSettings.AutoDetectDelimiter)
            {
                delimiter = await DetectDelimiterAsync(filePath);
                processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Auto-detected delimiter: '{delimiter}'");
            }
            else
            {
                delimiter = _csvSettings.Delimiter.FirstOrDefault(',');
                processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Using configured delimiter: '{delimiter}'");
            }

            using var reader = new StringReader(await File.ReadAllTextAsync(filePath));
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                HeaderValidated = null,
                TrimOptions = TrimOptions.Trim,
                Delimiter = delimiter.ToString()
            });

            try
            {
                await csv.ReadAsync();
                csv.ReadHeader();
                
                if (csv.HeaderRecord != null)
                {
                    detectedColumns = csv.HeaderRecord.ToList();
                    processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Detected columns: {string.Join(", ", detectedColumns)}");
                    
                    // Check for missing expected columns
                    var missingColumns = _csvSettings.DefaultColumns.Except(detectedColumns, StringComparer.OrdinalIgnoreCase).ToList();
                    if (missingColumns.Any())
                    {
                        processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Missing expected columns: {string.Join(", ", missingColumns)}");
                        
                        await _loggingService.LogAsync("Warning", "CsvProcessingService", 
                            $"Missing columns detected in CSV: {string.Join(", ", missingColumns)}", fileName, timeBlock);
                    }
                    
                    // Check for new columns that don't exist in database
                    var existingDbColumns = await _databaseSchemaService.GetExistingColumnsAsync();
                    var newColumns = detectedColumns.Except(existingDbColumns, StringComparer.OrdinalIgnoreCase).ToList();
                    if (newColumns.Any())
                    {
                        processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] New columns detected: {string.Join(", ", newColumns)}");
                        
                        await _loggingService.LogAsync("Info", "CsvProcessingService", 
                            $"New columns detected in CSV: {string.Join(", ", newColumns)}", fileName, timeBlock);

                        // Add new columns to database schema
                        var schemaUpdated = await _databaseSchemaService.EnsureColumnsExistAsync(newColumns);
                        if (schemaUpdated)
                        {
                            processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Database schema updated with new columns: {string.Join(", ", newColumns)}");
                        }
                    }
                }

                // Use direct SQL insertion for dynamic columns
                var (processedCount, skippedCount) = await ProcessRecordsWithDynamicColumnsAsync(csv, detectedColumns, fileName, timeBlock, processingLog);
                recordsProcessed = processedCount;
                recordsSkipped = skippedCount;

            }
            catch (Exception ex)
            {
                processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CSV parsing error: {ex.Message}");
                throw;
            }

            return (recordsProcessed, recordsSkipped, detectedColumns);
        }

        private async Task<(int recordsProcessed, int recordsSkipped)> ProcessRecordsWithDynamicColumnsAsync(CsvReader csv, List<string> detectedColumns, 
            string fileName, string timeBlock, List<string> processingLog)
        {
            var existingColumns = await _databaseSchemaService.GetExistingColumnsAsync();
            var batch = new List<Dictionary<string, object?>>();
            var recordsProcessed = 0;
            var recordsSkipped = 0;
            var firmanrValues = new List<string>(); // Track all Firmanr values (not just unique)

            while (await csv.ReadAsync())
            {
                try
                {
                    var record = new Dictionary<string, object?>
                    {
                        ["Id"] = Guid.NewGuid(),
                        ["SourceFileName"] = fileName,
                        ["TimeBlock"] = timeBlock,
                        ["CreatedDate"] = DateTime.UtcNow,
                        ["Contacted"] = false,
                        ["Notes"] = string.Empty,
                        ["AdditionalData"] = "{}"
                    };

                    // Process each column from CSV
                    foreach (var column in detectedColumns)
                    {
                        var value = csv.GetField(column)?.Trim();
                        
                        if (string.IsNullOrEmpty(value))
                            continue;

                        var sanitizedColumnName = SanitizeColumnName(column);
                        
                        // Track all Firmanr values for CompanyDetails processing (including duplicates for counting)
                        if (column.Equals("Firmanr", StringComparison.OrdinalIgnoreCase))
                        {
                            firmanrValues.Add(value);
                        }
                        
                        // Check if column exists in database
                        if (existingColumns.Contains(sanitizedColumnName, StringComparer.OrdinalIgnoreCase))
                        {
                            // Handle special columns with specific data types
                            if (sanitizedColumnName.Equals("Amount", StringComparison.OrdinalIgnoreCase))
                            {
                                if (decimal.TryParse(value, out decimal amount))
                                    record[sanitizedColumnName] = amount;
                                else
                                    record[sanitizedColumnName] = DBNull.Value;
                            }
                            else
                            {
                                record[sanitizedColumnName] = value;
                            }
                        }
                    }

                    batch.Add(record);
                    recordsProcessed++;

                    // Process in batches of 100
                    if (batch.Count >= 100)
                    {
                        await InsertBatchAsync(batch, existingColumns);
                        batch.Clear();
                    }
                }
                catch (Exception ex)
                {
                    recordsSkipped++;
                    processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Skipped record {recordsProcessed + recordsSkipped}: {ex.Message}");
                    
                    await _loggingService.LogAsync("Warning", "CsvProcessingService", 
                        $"Skipped record in CSV {fileName}: {ex.Message}", fileName, timeBlock);
                }
            }

            // Insert remaining records
            if (batch.Any())
            {
                await InsertBatchAsync(batch, existingColumns);
            }

            // Process Firmanr values for CompanyDetails table
            await ProcessFirmanrForCompanyDetailsAsync(firmanrValues, fileName, timeBlock, processingLog);

            return (recordsProcessed, recordsSkipped);
        }

        private async Task InsertBatchAsync(List<Dictionary<string, object?>> batch, List<string> availableColumns)
        {
            if (!batch.Any()) return;

            // Get columns that actually have data in this batch
            var columnsWithData = batch.First().Keys
                .Where(key => availableColumns.Contains(key, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var columnNames = string.Join(", ", columnsWithData.Select(c => $"[{c}]"));
            var parameterPlaceholders = string.Join(", ", columnsWithData.Select(c => $"@{c}"));

            var sql = $@"
                INSERT INTO CsvData ({columnNames}) 
                VALUES ({parameterPlaceholders})";

            using var connection = new SqlConnection(_databaseSettings.ConnectionString);
            await connection.OpenAsync();

            foreach (var record in batch)
            {
                using var command = new SqlCommand(sql, connection);
                
                foreach (var column in columnsWithData)
                {
                    var value = record.ContainsKey(column) ? record[column] : DBNull.Value;
                    command.Parameters.AddWithValue($"@{column}", value ?? DBNull.Value);
                }

                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task ProcessFirmanrForCompanyDetailsAsync(List<string> firmanrValues, string fileName, string timeBlock, List<string> processingLog)
        {
            if (!firmanrValues.Any())
            {
                processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] No Firmanr values found in CSV to process for CompanyDetails");
                return;
            }

            var newCompaniesAdded = 0;
            var existingCompaniesUpdated = 0;
            var processedCompaniesNewRows = 0;

            try
            {
                // Count rows in the current CSV for each Firmanr
                var firmanrRowCounts = new Dictionary<string, int>();
                foreach (var firmanr in firmanrValues)
                {
                    if (firmanrRowCounts.ContainsKey(firmanr))
                        firmanrRowCounts[firmanr]++;
                    else
                        firmanrRowCounts[firmanr] = 1;
                }

                foreach (var firmanr in firmanrValues.Distinct())
                {
                    var rowsForThisFirmanr = firmanrRowCounts[firmanr];

                    // Check if company already exists in CompanyDetails table
                    var existingCompany = await _context.CompanyDetails
                        .FirstOrDefaultAsync(cd => cd.Firmanr == firmanr);

                    if (existingCompany == null)
                    {
                        // Non-existing value: Add new company with all default columns
                        var newCompany = new CompanyDetails
                        {
                            Firmanr = firmanr,
                            ProcessedStatus = "Not Started",
                            Assignee = null, // Empty as requested
                            TotalRows = rowsForThisFirmanr,
                            TotalRowsProcessed = 0,
                            Created = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow,
                            LastModifiedBy = "System"
                        };

                        _context.CompanyDetails.Add(newCompany);
                        newCompaniesAdded++;
                        
                        processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Added new company to CompanyDetails: {firmanr} with {rowsForThisFirmanr} rows");
                    }
                    else
                    {
                        // Existing company - check ProcessedStatus
                        if (existingCompany.ProcessedStatus?.Equals("PROCESSED", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            // ProcessedStatus is PROCESSED: Create a new row
                            var newCompanyRow = new CompanyDetails
                            {
                                Firmanr = firmanr,
                                ProcessedStatus = "Not Started",
                                Assignee = null, // Empty as requested
                                TotalRows = rowsForThisFirmanr,
                                TotalRowsProcessed = 0,
                                Created = DateTime.UtcNow,
                                LastModified = DateTime.UtcNow,
                                LastModifiedBy = "System"
                            };

                            _context.CompanyDetails.Add(newCompanyRow);
                            processedCompaniesNewRows++;
                            
                            processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Created new row for PROCESSED company: {firmanr} with {rowsForThisFirmanr} rows");
                        }
                        else
                        {
                            // ProcessedStatus is not PROCESSED: Increase TotalRows and TotalRowsProcessed
                            existingCompany.TotalRows += rowsForThisFirmanr;
                            existingCompany.TotalRowsProcessed += rowsForThisFirmanr;
                            existingCompany.LastModified = DateTime.UtcNow;
                            existingCompany.LastModifiedBy = "System";

                            _context.CompanyDetails.Update(existingCompany);
                            existingCompaniesUpdated++;
                            
                            processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Updated existing company: {firmanr} - added {rowsForThisFirmanr} rows (Total: {existingCompany.TotalRows}, Processed: {existingCompany.TotalRowsProcessed})");
                        }
                    }
                }

                // Save changes to database
                var totalChanges = newCompaniesAdded + existingCompaniesUpdated + processedCompaniesNewRows;
                if (totalChanges > 0)
                {
                    await _context.SaveChangesAsync();
                    processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CompanyDetails updated: {newCompaniesAdded} new companies, {existingCompaniesUpdated} updated, {processedCompaniesNewRows} new rows for processed companies");
                    
                    await _loggingService.LogAsync("Info", "CsvProcessingService", 
                        $"CompanyDetails updated from CSV {fileName}: {newCompaniesAdded} new, {existingCompaniesUpdated} updated, {processedCompaniesNewRows} new rows", 
                        fileName, timeBlock);
                }
                else
                {
                    processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] No changes made to CompanyDetails table");
                }
            }
            catch (Exception ex)
            {
                processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error updating CompanyDetails: {ex.Message}");
                
                await _loggingService.LogAsync("Error", "CsvProcessingService", 
                    $"Failed to update CompanyDetails from CSV {fileName}: {ex.Message}", fileName, timeBlock, ex.ToString());
                
                throw;
            }
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

            // Limit length to 128 characters
            if (sanitized.Length > 128)
            {
                sanitized = sanitized.Substring(0, 128);
            }

            return sanitized;
        }

        private async Task EnsureDatabaseSchemaAsync(List<string> columns, List<string> processingLog)
        {
            // This is a simplified approach - in a real system, you might use migrations
            // For now, we'll log what would need to be done
            var existingColumns = await _databaseSchemaService.GetExistingColumnsAsync();
            var newColumns = columns.Except(existingColumns, StringComparer.OrdinalIgnoreCase).ToList();
            
            if (newColumns.Any())
            {
                processingLog.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Note: New columns detected that could be added to schema: {string.Join(", ", newColumns)}");
                
                await _loggingService.LogAsync("Info", "CsvProcessingService", 
                    $"Schema extension opportunity: New columns detected: {string.Join(", ", newColumns)}");
            }

            await Task.CompletedTask;
        }

        private static string AddTimestampToFileName(string fileName, string timeBlock)
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            return $"{nameWithoutExtension}_{timeBlock}{extension}";
        }

        private async Task<char> DetectDelimiterAsync(string filePath)
        {
            try
            {
                // Read first few lines to detect delimiter
                var lines = new List<string>();
                using var reader = new StreamReader(filePath);
                for (int i = 0; i < 5 && !reader.EndOfStream; i++)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                        lines.Add(line);
                }

                if (!lines.Any())
                    return ','; // Default to comma

                // Count occurrences of common delimiters
                var delimiters = new char[] { ',', ';', '\t', '|' };
                var delimiterCounts = new Dictionary<char, int>();

                foreach (var delimiter in delimiters)
                {
                    var count = 0;
                    foreach (var line in lines)
                    {
                        count += line.Count(c => c == delimiter);
                    }
                    delimiterCounts[delimiter] = count;
                }

                // Return the delimiter with the highest count (and count > 0)
                var bestDelimiter = delimiterCounts
                    .Where(kvp => kvp.Value > 0)
                    .OrderByDescending(kvp => kvp.Value)
                    .FirstOrDefault();

                return bestDelimiter.Key != default ? bestDelimiter.Key : ',';
            }
            catch
            {
                // If detection fails, return default comma
                return ',';
            }
        }
    }
}
