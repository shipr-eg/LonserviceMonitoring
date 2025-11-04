using LonserviceMonitoring.Models;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace LonserviceMonitoring.Services
{
    public class AuditService
    {
        private readonly string _connectionString;

        public AuditService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task LogCompanyDetailsAuditAsync(Guid recordId, string action, string columnName, 
            string? oldValue, string? newValue, string modifiedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if audit table exists
                using var checkTableCommand = connection.CreateCommand();
                checkTableCommand.CommandText = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = 'CompanyDetailsAudit'";

                var tableExists = (int)await checkTableCommand.ExecuteScalarAsync() > 0;
                
                if (!tableExists)
                {
                    // Table doesn't exist yet, skip logging
                    return;
                }

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO CompanyDetailsAudit (RecordId, Action, ColumnName, OldValue, NewValue, ModifiedBy, Timestamp)
                    VALUES (@recordId, @action, @columnName, @oldValue, @newValue, @modifiedBy, GETUTCDATE())";

                command.Parameters.AddWithValue("@recordId", recordId);
                command.Parameters.AddWithValue("@action", action);
                command.Parameters.AddWithValue("@columnName", columnName);
                command.Parameters.AddWithValue("@oldValue", oldValue ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@newValue", newValue ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@modifiedBy", modifiedBy);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Log the error but don't throw it to avoid breaking the main functionality
                Console.WriteLine($"Error logging company details audit: {ex.Message}");
            }
        }

        public async Task LogCsvDataAuditAsync(Guid recordId, string action, string columnName, 
            string? oldValue, string? newValue, string modifiedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if audit table exists
                using var checkTableCommand = connection.CreateCommand();
                checkTableCommand.CommandText = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = 'CsvDataAudit'";

                var tableExists = (int)await checkTableCommand.ExecuteScalarAsync() > 0;
                
                if (!tableExists)
                {
                    // Table doesn't exist yet, skip logging
                    return;
                }

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO CsvDataAudit (RecordId, Action, ColumnName, OldValue, NewValue, ModifiedBy, Timestamp)
                    VALUES (@recordId, @action, @columnName, @oldValue, @newValue, @modifiedBy, GETUTCDATE())";

                command.Parameters.AddWithValue("@recordId", recordId);
                command.Parameters.AddWithValue("@action", action);
                command.Parameters.AddWithValue("@columnName", columnName);
                command.Parameters.AddWithValue("@oldValue", oldValue ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@newValue", newValue ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@modifiedBy", modifiedBy);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Log the error but don't throw it to avoid breaking the main functionality
                Console.WriteLine($"Error logging CSV data audit: {ex.Message}");
            }
        }

        public async Task<List<CompanyHistoryModel>> GetCompanyHistoryAsync(string firmanr)
        {
            var history = new List<CompanyHistoryModel>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if audit table exists
                using var checkTableCommand = connection.CreateCommand();
                checkTableCommand.CommandText = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = 'CompanyDetailsAudit'";

                var tableExists = (int)await checkTableCommand.ExecuteScalarAsync() > 0;
                
                if (!tableExists)
                {
                    // Return empty history if audit table doesn't exist yet
                    return history;
                }

                // Get the company details record ID first
                using var getIdCommand = connection.CreateCommand();
                getIdCommand.CommandText = "SELECT GUID FROM CompanyDetails WHERE Firmanr = @firmanr";
                getIdCommand.Parameters.AddWithValue("@firmanr", firmanr);

                var recordIdObj = await getIdCommand.ExecuteScalarAsync();
                if (recordIdObj == null) return history;

                var recordId = (Guid)recordIdObj;

                // Get audit history
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Action, ColumnName, OldValue, NewValue, ModifiedBy, Timestamp
                    FROM CompanyDetailsAudit 
                    WHERE RecordId = @recordId
                    ORDER BY Timestamp DESC";

                command.Parameters.AddWithValue("@recordId", recordId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    history.Add(new CompanyHistoryModel
                    {
                        Action = reader["Action"].ToString() ?? "",
                        ColumnName = reader["ColumnName"].ToString() ?? "",
                        OldValue = reader["OldValue"] == DBNull.Value ? null : reader["OldValue"].ToString(),
                        NewValue = reader["NewValue"] == DBNull.Value ? null : reader["NewValue"].ToString(),
                        ModifiedBy = reader["ModifiedBy"].ToString() ?? "",
                        Timestamp = (DateTime)reader["Timestamp"]
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw it - return empty history instead
                // You might want to log this to your logging system
                Console.WriteLine($"Error fetching company history: {ex.Message}");
            }

            return history;
        }

        public async Task UpdateCompanyTotalCountsAsync(string firmanr, string modifiedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if the required columns exist
                using var checkColumnsCommand = connection.CreateCommand();
                checkColumnsCommand.CommandText = @"
                    SELECT 
                        SUM(CASE WHEN COLUMN_NAME = 'TotalRows' THEN 1 ELSE 0 END) as HasTotalRows,
                        SUM(CASE WHEN COLUMN_NAME = 'TotalRowsProcessed' THEN 1 ELSE 0 END) as HasTotalRowsProcessed,
                        SUM(CASE WHEN COLUMN_NAME = 'Confirmed' THEN 1 ELSE 0 END) as HasConfirmed
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME IN ('CompanyDetails', 'CsvData') 
                    AND COLUMN_NAME IN ('TotalRows', 'TotalRowsProcessed', 'Confirmed')";

                using var checkReader = await checkColumnsCommand.ExecuteReaderAsync();
                bool hasRequiredColumns = false;
                
                if (await checkReader.ReadAsync())
                {
                    var hasTotalRows = (int)checkReader["HasTotalRows"] > 0;
                    var hasTotalRowsProcessed = (int)checkReader["HasTotalRowsProcessed"] > 0;
                    var hasConfirmed = (int)checkReader["HasConfirmed"] > 0;
                    hasRequiredColumns = hasTotalRows && hasTotalRowsProcessed && hasConfirmed;
                }
                checkReader.Close();

                if (!hasRequiredColumns)
                {
                    // Required columns don't exist yet, skip the update
                    return;
                }

                using var transaction = connection.BeginTransaction();

                try
                {
                    // Get current counts
                    using var getCountsCommand = connection.CreateCommand();
                    getCountsCommand.Transaction = transaction;
                    getCountsCommand.CommandText = @"
                        SELECT 
                            COUNT(*) as TotalRows,
                            SUM(CASE WHEN Confirmed = 1 THEN 1 ELSE 0 END) as ProcessedRows
                        FROM CsvData 
                        WHERE Firmanr = @firmanr";
                    getCountsCommand.Parameters.AddWithValue("@firmanr", firmanr);

                    using var reader = await getCountsCommand.ExecuteReaderAsync();
                    int totalRows = 0, processedRows = 0;

                    if (await reader.ReadAsync())
                    {
                        totalRows = (int)reader["TotalRows"];
                        processedRows = (int)reader["ProcessedRows"];
                    }
                    reader.Close();

                    // Get current company details
                    using var getCurrentCommand = connection.CreateCommand();
                    getCurrentCommand.Transaction = transaction;
                    getCurrentCommand.CommandText = @"
                        SELECT GUID, TotalRows, TotalRowsProcessed 
                        FROM CompanyDetails 
                        WHERE Firmanr = @firmanr";
                    getCurrentCommand.Parameters.AddWithValue("@firmanr", firmanr);

                    using var currentReader = await getCurrentCommand.ExecuteReaderAsync();
                    Guid companyId = Guid.Empty;
                    int currentTotalRows = 0, currentProcessedRows = 0;

                    if (await currentReader.ReadAsync())
                    {
                        companyId = (Guid)currentReader["GUID"];
                        currentTotalRows = (int)currentReader["TotalRows"];
                        currentProcessedRows = (int)currentReader["TotalRowsProcessed"];
                    }
                    currentReader.Close();

                    // Update if there are changes
                    bool totalChanged = currentTotalRows != totalRows;
                    bool processedChanged = currentProcessedRows != processedRows;

                    if (totalChanged || processedChanged)
                    {
                        // Log audit entries for changes
                        if (totalChanged)
                        {
                            await LogCompanyDetailsAuditAsync(companyId, "UPDATE", "TotalRows", 
                                currentTotalRows.ToString(), totalRows.ToString(), modifiedBy);
                        }

                        if (processedChanged)
                        {
                            await LogCompanyDetailsAuditAsync(companyId, "UPDATE", "TotalRowsProcessed", 
                                currentProcessedRows.ToString(), processedRows.ToString(), modifiedBy);
                        }

                        // Update the company details
                        using var updateCommand = connection.CreateCommand();
                        updateCommand.Transaction = transaction;
                        updateCommand.CommandText = @"
                            UPDATE CompanyDetails 
                            SET TotalRows = @totalRows, 
                                TotalRowsProcessed = @processedRows,
                                LastModified = GETUTCDATE(),
                                LastModifiedBy = @modifiedBy
                            WHERE Firmanr = @firmanr";

                        updateCommand.Parameters.AddWithValue("@totalRows", totalRows);
                        updateCommand.Parameters.AddWithValue("@processedRows", processedRows);
                        updateCommand.Parameters.AddWithValue("@modifiedBy", modifiedBy);
                        updateCommand.Parameters.AddWithValue("@firmanr", firmanr);

                        await updateCommand.ExecuteNonQueryAsync();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw it to avoid breaking the main functionality
                Console.WriteLine($"Error updating company total counts: {ex.Message}");
            }
        }
    }
}