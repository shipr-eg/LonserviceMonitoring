using Microsoft.Data.SqlClient;
using LonserviceMonitoring.Models;
using System.Data;
using Newtonsoft.Json;

namespace LonserviceMonitoring.Services
{
    public class DataService
    {
        private readonly SqlConnection _connection;
        private readonly IConfiguration _configuration;

        public DataService(IDbConnection connection, IConfiguration configuration)
        {
            _connection = (SqlConnection)connection;
            _configuration = configuration;
        }

        public async Task<List<CsvDataModel>> GetAllDataAsync()
        {
            var data = new List<CsvDataModel>();

            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();

            using var command = _connection.CreateCommand();
            // Select all columns - we'll filter out system columns in code
            // command.CommandText = "SELECT * FROM CsvData ORDER BY Firmanr, Id";
            command.CommandText = @"SELECT 
                c.*,
                cd.ProcessedStatus,
                (e.FirstName + ' ' + e.LastName) AS AssigneeName
            FROM [LonserviceMonitoringDB].[dbo].[CsvData] AS c
            LEFT JOIN [LonserviceMonitoringDB].[dbo].[CompanyDetails] AS cd 
                ON c.Firmanr = cd.Firmanr
            LEFT JOIN [LonserviceMonitoringDB].[dbo].[EmployeeList] AS e 
                ON e.EmployeeID = cd.Assignee";
            
            using var reader = await command.ExecuteReaderAsync();
            var columnNames = new List<string>();

            // Get all column names
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnNames.Add(reader.GetName(i));
            }

            while (await reader.ReadAsync())
            {
                var record = new CsvDataModel();
                var additionalProps = new Dictionary<string, object>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                    // Map known columns to properties and ALL columns to AdditionalProperties
                    switch (columnName.ToLower())
                    {
                        case "id":
                            if (Guid.TryParse(value?.ToString(), out var id))
                                record.Id = id;
                            else
                                record.Id = Guid.NewGuid();
                            additionalProps[columnName] = value ?? "";
                            break;
                        case "firmanr":
                            record.Firmanr = value?.ToString();
                            additionalProps[columnName] = value ?? "";
                            break;
                        case "createddate":
                            if (DateTime.TryParse(value?.ToString(), out var createdDate))
                                record.CreatedDate = createdDate;
                            additionalProps[columnName] = value ?? "";
                            break;
                        case "confirmed":
                            record.Confirmed = Convert.ToBoolean(value ?? false);
                            additionalProps[columnName] = value ?? false;
                            break;
                        case "assigneename":
                            record.AssigneeName = value?.ToString();
                            additionalProps[columnName] = value ?? "";
                            break;
                        case "processedstatus":
                            record.ProcessedStatus = value?.ToString();
                            additionalProps[columnName] = value ?? "";
                            break;
                        case "notes":
                            record.Notes = value?.ToString();
                            additionalProps[columnName] = value ?? "";
                            break;
                        default:
                            // All other columns also go into additional properties
                            additionalProps[columnName] = value ?? "";
                            break;
                    }
                }

                record.AdditionalProperties = additionalProps;
                data.Add(record);
            }

            return data;
        }

        public async Task<bool> SaveChangesAsync(List<CsvDataModel> changes, string user = "Anonymous")
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                using var transaction = _connection.BeginTransaction();

                // Get Windows username
                var systemUser = Environment.UserName;

                foreach (var change in changes)
                {
                    // First, get the old values from the database
                    CsvDataModel oldRecord = null;
                    using (var selectCmd = _connection.CreateCommand())
                    {
                        selectCmd.Transaction = transaction;
                        selectCmd.CommandText = "SELECT Confirmed, Notes, Firmanr FROM CsvData WHERE Id = @id";
                        selectCmd.Parameters.Add(new SqlParameter("@id", change.Id));
                        
                        using var reader = await selectCmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            oldRecord = new CsvDataModel
                            {
                                Confirmed = !reader.IsDBNull(0) && reader.GetBoolean(0),
                                Notes = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Firmanr = reader.IsDBNull(2) ? null : reader.GetString(2)
                            };
                        }
                    }

                    // Update the main record
                    using var updateCmd = _connection.CreateCommand();
                    updateCmd.Transaction = transaction;
                    updateCmd.CommandText = @"
                        UPDATE CsvData 
                        SET Confirmed = @confirmed, Notes = @notes 
                        WHERE Id = @id";

                    updateCmd.Parameters.Add(new SqlParameter("@confirmed", change.Confirmed));
                    updateCmd.Parameters.Add(new SqlParameter("@notes", change.Notes ?? (object)DBNull.Value));
                    updateCmd.Parameters.Add(new SqlParameter("@id", change.Id));

                    await updateCmd.ExecuteNonQueryAsync();

                    // Build change tracking - only log what actually changed
                    var changesList = new List<Dictionary<string, object>>();
                    
                    if (oldRecord != null)
                    {
                        if (oldRecord.Confirmed != change.Confirmed)
                        {
                            changesList.Add(new Dictionary<string, object>
                            {
                                { "Field", "Confirmed" },
                                { "OldValue", oldRecord.Confirmed },
                                { "NewValue", change.Confirmed }
                            });
                        }
                        
                        if (oldRecord.Notes != change.Notes)
                        {
                            changesList.Add(new Dictionary<string, object>
                            {
                                { "Field", "Notes" },
                                { "OldValue", oldRecord.Notes ?? "" },
                                { "NewValue", change.Notes ?? "" }
                            });
                        }
                    }

                    // Only log if there are actual changes
                    if (changesList.Count > 0)
                    {
                        var changesJson = JsonConvert.SerializeObject(changesList);
                        var firmanr = oldRecord?.Firmanr ?? change.Firmanr;
                        
                        // Log the audit trail with RecordId (GUID), Firmanr, and system username
                        await LogAuditAsync(change.Id.ToString(), "UPDATE", systemUser, changesJson, firmanr, transaction);
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error saving changes: {ex.Message}");
                return false;
            }
        }

        public async Task<List<AuditLogModel>> GetAuditLogsAsync(string searchTerm = "")
        {
            var logs = new List<AuditLogModel>();

            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();

            using var command = _connection.CreateCommand();

            if (string.IsNullOrEmpty(searchTerm))
            {
                command.CommandText = "SELECT TOP 1000 * FROM AuditLog ORDER BY Timestamp DESC";
            }
            else
            {
                command.CommandText = @"
                    SELECT TOP 1000 * FROM AuditLog 
                    WHERE Action LIKE @search OR [User] LIKE @search OR RecordId LIKE @search OR Changes LIKE @search
                    ORDER BY Timestamp DESC";
                command.Parameters.Add(new SqlParameter("@search", $"%{searchTerm}%"));
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new AuditLogModel
                {
                    Id = reader.GetInt32("Id"),
                    Timestamp = reader.GetDateTime("Timestamp"),
                    Action = reader.GetString("Action"),
                    User = reader.GetString("User"),
                    RecordId = reader.GetString("RecordId"),
                    Changes = reader.GetString("Changes"),
                    Firmanr = reader.IsDBNull(reader.GetOrdinal("Firmanr")) ? null : reader.GetString("Firmanr")
                });
            }

            return logs;
        }

        public async Task<List<CompanyHistoryModel>> GetCompanyHistoryAsync(string firmanr)
        {
            var history = new List<CompanyHistoryModel>();

            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();

            using var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT Timestamp, Action, [User], Changes, RecordId, Firmanr
                FROM AuditLog
                WHERE Firmanr = @firmanr
                ORDER BY Timestamp DESC";
            command.Parameters.Add(new SqlParameter("@firmanr", firmanr));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var changes = reader.GetString("Changes");
                var action = reader.GetString("Action");
                var timestamp = reader.GetDateTime("Timestamp");
                var user = reader.GetString("User");

                // Parse the changes JSON to extract field-level changes
                try
                {
                    var changesList = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(changes);
                    if (changesList != null)
                    {
                        foreach (var change in changesList)
                        {
                            history.Add(new CompanyHistoryModel
                            {
                                Timestamp = timestamp,
                                Action = action,
                                ColumnName = change.ContainsKey("Field") ? change["Field"].ToString() ?? "" : "",
                                OldValue = change.ContainsKey("OldValue") ? change["OldValue"]?.ToString() : null,
                                NewValue = change.ContainsKey("NewValue") ? change["NewValue"]?.ToString() : null,
                                ModifiedBy = user
                            });
                        }
                    }
                }
                catch
                {
                    // If JSON parsing fails, create a single entry with the raw changes
                    history.Add(new CompanyHistoryModel
                    {
                        Timestamp = timestamp,
                        Action = action,
                        ColumnName = "Multiple Fields",
                        OldValue = null,
                        NewValue = changes,
                        ModifiedBy = user
                    });
                }
            }

            return history;
        }

        public async Task<DataUpdateNotification> CheckForNewDataAsync()
        {
            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();

            using var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*) as NewCount 
                FROM CsvData 
                WHERE CreatedDate > DATEADD(minute, -5, GETDATE())";

            var newCount = Convert.ToInt32(await command.ExecuteScalarAsync());

            return new DataUpdateNotification
            {
                HasNewData = newCount > 0,
                NewRecordCount = newCount,
                LastChecked = DateTime.Now
            };
        }

        private async Task LogAuditAsync(string recordId, string action, string user, string changes, string firmanr, SqlTransaction transaction)
        {
            using var auditCmd = _connection.CreateCommand();
            auditCmd.Transaction = transaction;
            auditCmd.CommandText = @"
                INSERT INTO AuditLog (Timestamp, Action, [User], RecordId, Changes, Firmanr)
                VALUES (@timestamp, @action, @user, @recordId, @changes, @firmanr)";

            auditCmd.Parameters.Add(new SqlParameter("@timestamp", DateTime.Now));
            auditCmd.Parameters.Add(new SqlParameter("@action", action));
            auditCmd.Parameters.Add(new SqlParameter("@user", user));
            auditCmd.Parameters.Add(new SqlParameter("@recordId", recordId));
            auditCmd.Parameters.Add(new SqlParameter("@changes", changes));
            auditCmd.Parameters.Add(new SqlParameter("@firmanr", (object)firmanr ?? DBNull.Value));

            await auditCmd.ExecuteNonQueryAsync();
        }

        public async Task<List<string>> GetColumnNamesAsync()
        {
            var columns = new List<string>();

            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();

            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT TOP 1 * FROM CsvData";

            using var reader = await command.ExecuteReaderAsync();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }

            return columns;
        }

        public async Task<List<EmployeeList>> GetAllEmployeesAsync()
        {
            var employees = new List<EmployeeList>();

            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();

            using var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT GUID, EmployeeID, FirstName, LastName, IsAdmin, IsActive 
                FROM EmployeeList 
                ORDER BY FirstName, LastName";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                employees.Add(new EmployeeList
                {
                    GUID = reader.GetGuid("GUID"),
                    EmployeeID = reader.IsDBNull("EmployeeID") ? "" : reader["EmployeeID"].ToString() ?? "",
                    FirstName = reader.IsDBNull("FirstName") ? "" : reader.GetString("FirstName"),
                    LastName = reader.IsDBNull("LastName") ? "" : reader.GetString("LastName"),
                    IsAdmin = reader.IsDBNull("IsAdmin") ? false : reader.GetBoolean("IsAdmin"),
                    IsActive = reader.IsDBNull("IsActive") ? false : reader.GetBoolean("IsActive")
                });
            }

            return employees;
        }

        // All CRUD operations for CompanyDetails
        public async Task<List<CompanyDetails>> GetAllCompanyDetailsAsync()
        {
            var companies = new List<CompanyDetails>();

            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();

            using var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT Firmanr, Assignee, ProcessedStatus, Created 
                FROM CompanyDetails 
                ORDER BY Firmanr";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                companies.Add(new CompanyDetails
                {
                    // CompanyID = reader.IsDBNull("CompanyID") ? 0 : Convert.ToInt32(reader["CompanyID"]),
                    Company = reader.IsDBNull("Firmanr") ? "" : reader["Firmanr"].ToString() ?? "",
                    Assignee = reader.IsDBNull("Assignee") ? 0 : Convert.ToInt32(reader["Assignee"]),
                    ProcessedStatus = reader.IsDBNull("ProcessedStatus") ? "" : reader["ProcessedStatus"].ToString() ?? "",
                    Created = reader["Created"] == DBNull.Value ? null : Convert.ToDateTime(reader["Created"])
                });
            }

            return companies;
        }

        public async Task<List<CompanyDetails>> GetCompanyDetailsByNameAsync(string companyName)
        {
            var companies = new List<CompanyDetails>();

            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();

            using var command = _connection.CreateCommand();
            // command.CommandText = @"
            //     SELECT Company, Assignee, ProcessedStatus, TotalRecords, ContactedRecords, Created
            //     FROM CompanyDetails 
            //     WHERE Company = @companyName";
            command.CommandText = @"SELECT 
                    cd.Firmanr,
                    cd.Assignee,
                    el.FirstName + ' ' + el.LastName AS AssigneeName,
                    cd.ProcessedStatus,
                    cd.Created
                FROM CompanyDetails cd
                LEFT JOIN EmployeeList el 
                    ON cd.Assignee = el.EmployeeID
                WHERE cd.Firmanr = @companyName
                ORDER BY cd.Created DESC";


            command.Parameters.Add(new SqlParameter("@companyName", companyName));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                companies.Add(new CompanyDetails
                {
                    Company = reader.IsDBNull("Firmanr") ? "" : reader["Firmanr"].ToString() ?? "",
                    Assignee = reader.IsDBNull("Assignee") ? 0 : Convert.ToInt32(reader["Assignee"]),
                    AssigneeName = reader.IsDBNull("AssigneeName") ? "" : reader["AssigneeName"].ToString() ?? "",
                    ProcessedStatus = reader.IsDBNull("ProcessedStatus") ? "" : reader["ProcessedStatus"].ToString() ?? "",
                    Created = reader["Created"] == DBNull.Value ? null : Convert.ToDateTime(reader["Created"]),

                });
            }

            return companies;
        }

        public async Task<int> InsertCompanyDetailsAsync(CompanyDetails company)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                // Check if a record with the same company name exists
                bool recordExists = false;
                CompanyDetails existing = null;
                
                using (var checkCommand = _connection.CreateCommand())
                {
                    checkCommand.CommandText = @"
                        SELECT TOP 1 Firmanr, Assignee, ProcessedStatus
                        FROM CompanyDetails
                        WHERE Firmanr = @company
                        ORDER BY Created DESC";
                    checkCommand.Parameters.Add(new SqlParameter("@company", company.Company));

                    using var reader = await checkCommand.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        recordExists = true;
                        existing = new CompanyDetails
                        {
                            Company = reader.IsDBNull("Firmanr") ? null : reader.GetString("Firmanr"),
                            Assignee = reader.IsDBNull("Assignee") ? null : reader.GetInt32("Assignee"),
                            ProcessedStatus = reader.IsDBNull("ProcessedStatus") ? null : reader.GetString("ProcessedStatus")
                        };
                    }
                }

                int rowsAffected = 0;

                if (recordExists)
                {
                    // Update existing record
                    using var updateCommand = _connection.CreateCommand();
                    updateCommand.CommandText = @"
                        UPDATE CompanyDetails 
                        SET Assignee = @assignee, 
                            ProcessedStatus = @processedStatus,
                            Created = GETDATE()
                        WHERE Firmanr = @company";

                    // Use provided values or keep existing ones if new values are null/empty
                    var assigneeToUpdate = company.Assignee ?? existing?.Assignee;
                    var statusToUpdate = string.IsNullOrEmpty(company.ProcessedStatus) ? existing?.ProcessedStatus : company.ProcessedStatus;

                    updateCommand.Parameters.Add(new SqlParameter("@company", company.Company));
                    updateCommand.Parameters.Add(new SqlParameter("@assignee", (object?)assigneeToUpdate ?? DBNull.Value));
                    updateCommand.Parameters.Add(new SqlParameter("@processedStatus", (object?)statusToUpdate ?? DBNull.Value));

                    rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert new record
                    using var insertCommand = _connection.CreateCommand();
                    insertCommand.CommandText = @"
                        INSERT INTO CompanyDetails (Firmanr, Assignee, ProcessedStatus)
                        VALUES (@company, @assignee, @processedStatus)";

                    insertCommand.Parameters.Add(new SqlParameter("@company", company.Company));
                    insertCommand.Parameters.Add(new SqlParameter("@assignee", (object?)company.Assignee ?? DBNull.Value));
                    insertCommand.Parameters.Add(new SqlParameter("@processedStatus", (object?)company.ProcessedStatus ?? DBNull.Value));

                    rowsAffected = await insertCommand.ExecuteNonQueryAsync();
                }

                return rowsAffected > 0 ? 1 : 0; // Return 1 for success, 0 for failure
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error upserting company details: {ex.Message}");
                throw;
            }
        }                
    
        // CSV Processing History methods
        public async Task<List<CsvProcessingHistory>> GetCsvProcessingHistoryAsync()
        {
            var history = new List<CsvProcessingHistory>();

            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();

            using var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, FileName, TimeBlock, ProcessedDate, Status, RecordsProcessed, RecordsSkipped, 
                       ProcessingLog, ErrorMessage, SourcePath, WorkPath, LoadedPath
                FROM CsvProcessingHistory
                ORDER BY ProcessedDate DESC";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                history.Add(new CsvProcessingHistory
                {
                    Id = reader.GetGuid(0),
                    FileName = reader.GetString(1),
                    TimeBlock = reader.GetString(2),
                    ProcessedDate = reader.GetDateTime(3),
                    Status = reader.GetString(4),
                    RecordsProcessed = reader.GetInt32(5),
                    RecordsSkipped = reader.GetInt32(6),
                    ProcessingLog = reader.GetString(7),
                    ErrorMessage = reader.IsDBNull(8) ? null : reader.GetString(8),
                    SourcePath = reader.GetString(9),
                    WorkPath = reader.GetString(10),
                    LoadedPath = reader.IsDBNull(11) ? null : reader.GetString(11)
                });
            }

            return history;
        }

        public async Task<List<ProcessingLog>> GetProcessingLogsByFileAsync(string fileName, string timeBlock)
        {
            var logs = new List<ProcessingLog>();

            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();

            using var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Timestamp, LogLevel, Source, Message, FileName, TimeBlock, Exception, AdditionalData
                FROM ProcessingLogs
                WHERE FileName = @fileName AND TimeBlock = @timeBlock
                ORDER BY Timestamp ASC";

            command.Parameters.Add(new SqlParameter("@fileName", fileName));
            command.Parameters.Add(new SqlParameter("@timeBlock", timeBlock));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logs.Add(new ProcessingLog
                {
                    Id = reader.GetGuid(0),
                    Timestamp = reader.GetDateTime(1),
                    LogLevel = reader.GetString(2),
                    Source = reader.GetString(3),
                    Message = reader.GetString(4),
                    FileName = reader.IsDBNull(5) ? null : reader.GetString(5),
                    TimeBlock = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Exception = reader.IsDBNull(7) ? null : reader.GetString(7),
                    AdditionalData = reader.GetString(8)
                });
            }

            return logs;
        }

        // Employee CRUD operations
        public async Task<int> GetNextEmployeeIdAsync()
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                using var command = _connection.CreateCommand();
                command.CommandText = "SELECT ISNULL(MAX(CAST(EmployeeID AS INT)), 1000) + 1 FROM EmployeeList WHERE ISNUMERIC(EmployeeID) = 1";

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting next employee ID: {ex.Message}");
                return 1001; // Default starting ID
            }
        }

        public async Task<bool> AddEmployeeAsync(EmployeeList employee)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                // Auto-generate Employee ID if not provided or is empty
                if (string.IsNullOrWhiteSpace(employee.EmployeeID))
                {
                    employee.EmployeeID = (await GetNextEmployeeIdAsync()).ToString();
                }

                using var command = _connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO EmployeeList (EmployeeID, FirstName, LastName, IsAdmin, IsActive)
                    VALUES (@employeeId, @firstName, @lastName, @isAdmin, @isActive)";

                command.Parameters.Add(new SqlParameter("@employeeId", employee.EmployeeID));
                command.Parameters.Add(new SqlParameter("@firstName", employee.FirstName));
                command.Parameters.Add(new SqlParameter("@lastName", employee.LastName));
                command.Parameters.Add(new SqlParameter("@isAdmin", employee.IsAdmin));
                command.Parameters.Add(new SqlParameter("@isActive", employee.IsActive));

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding employee: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateEmployeeAsync(EmployeeList employee)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                using var command = _connection.CreateCommand();
                command.CommandText = @"
                    UPDATE EmployeeList
                    SET FirstName = @firstName, LastName = @lastName, 
                        IsAdmin = @isAdmin, IsActive = @isActive
                    WHERE GUID = @guid";

                command.Parameters.Add(new SqlParameter("@guid", employee.GUID));
                command.Parameters.Add(new SqlParameter("@firstName", employee.FirstName));
                command.Parameters.Add(new SqlParameter("@lastName", employee.LastName));
                command.Parameters.Add(new SqlParameter("@isAdmin", employee.IsAdmin));
                command.Parameters.Add(new SqlParameter("@isActive", employee.IsActive));

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating employee: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CanDeleteEmployeeAsync(string employeeId)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                using var command = _connection.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(*) 
                    FROM CompanyDetails 
                    WHERE Assignee = @employeeId 
                    AND (ProcessedStatus IS NULL OR ProcessedStatus != 'Processed')";

                command.Parameters.Add(new SqlParameter("@employeeId", employeeId));

                var count = (int)await command.ExecuteScalarAsync();
                return count == 0; // Can delete if no non-processed records
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking employee deletion: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteEmployeeAsync(Guid guid)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                    await _connection.OpenAsync();

                // First get the employee ID to check if deletion is allowed
                string employeeId;
                using (var checkCommand = _connection.CreateCommand())
                {
                    checkCommand.CommandText = "SELECT EmployeeID FROM EmployeeList WHERE GUID = @guid";
                    checkCommand.Parameters.Add(new SqlParameter("@guid", guid));
                    var result = await checkCommand.ExecuteScalarAsync();
                    if (result == null) return false;
                    employeeId = result.ToString();
                }

                // Check if employee can be deleted
                if (!await CanDeleteEmployeeAsync(employeeId))
                {
                    Console.WriteLine($"Cannot delete employee {employeeId}: has non-processed records");
                    return false;
                }

                using var command = _connection.CreateCommand();
                command.CommandText = "DELETE FROM EmployeeList WHERE GUID = @guid";
                command.Parameters.Add(new SqlParameter("@guid", guid));

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting employee: {ex.Message}");
                return false;
            }
        }
    }
}
