using Microsoft.Data.SqlClient;
using LonserviceMonitoring.Models;
using System.Data;
using Newtonsoft.Json;

namespace LonserviceMonitoring.Services
{
    public class DataService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AuditService _auditService;

        public DataService(IConfiguration configuration, AuditService auditService)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection string not found");
            _auditService = auditService;
        }

        public async Task<List<CsvDataModel>> GetAllDataAsync()
        {
            var data = new List<CsvDataModel>();
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                
                // Get all columns from CsvData table joined with CompanyDetails
                // Hide companies only if ALL their records are "Processed"
                command.CommandText = @"
                    SELECT 
                        c.*,
                        ISNULL(cd_status.EffectiveStatus, 'Not Started') as CompanyProcessedStatus,
                        cd_assignee.Assignee as CompanyAssignee,
                        cd_assignee.AssigneeName as CompanyAssigneeName
                    FROM CsvData c
                    LEFT JOIN (
                        SELECT 
                            Firmanr,
                            CASE 
                                WHEN COUNT(*) = SUM(CASE WHEN ProcessedStatus = 'Processed' THEN 1 ELSE 0 END)
                                THEN 'Processed'
                                ELSE MAX(CASE WHEN ProcessedStatus != 'Processed' THEN ProcessedStatus ELSE 'Not Started' END)
                            END as EffectiveStatus
                        FROM CompanyDetails 
                        GROUP BY Firmanr
                    ) cd_status ON c.Firmanr = cd_status.Firmanr
                     
                    LEFT JOIN (
                        SELECT DISTINCT 
                            cd.Firmanr,
                            cd.Assignee,
                            CONCAT(el.FirstName, ' ', el.LastName) as AssigneeName
                        FROM CompanyDetails cd
                        LEFT JOIN EmployeeList el ON cd.Assignee = el.EmployeeID
                        WHERE cd.Assignee IS NOT NULL
                    ) cd_assignee ON c.Firmanr = cd_assignee.Firmanr 
                    WHERE ISNULL(cd_status.EffectiveStatus, 'Not Started') != 'Processed' 
                    ORDER BY c.CreatedDate DESC";

                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    try
                    {
                        var record = new CsvDataModel();
                        
                        // Safely read Id - might be GUID or string
                        if (!reader.IsDBNull("Id"))
                        {
                            var idValue = reader["Id"];
                            if (idValue is Guid guidId)
                            {
                                record.Id = guidId;
                            }
                            else if (Guid.TryParse(idValue.ToString(), out var parsedGuid))
                            {
                                record.Id = parsedGuid;
                            }
                            else
                            {
                                record.Id = Guid.NewGuid(); // fallback
                            }
                        }
                        else
                        {
                            record.Id = Guid.NewGuid();
                        }
                        
                        // Dynamically read all other columns
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);
                            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            
                            // Set properties using the dynamic approach
                            record.SetProperty(columnName, value);
                        }
                        
                        // Set ProcessedStatus from CompanyDetails if available
                        string recordstatus=record.GetProperty("ProcessedStatus")?.ToString() ?? "";
                        record.ProcessedStatus =  recordstatus != "" ? recordstatus : "Not Started";
                        
                        data.Add(record);
                    }
                    catch (Exception rowEx)
                    {
                        // Skip this row and continue with next
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the actual error instead of silently returning empty list
                Console.WriteLine($"ERROR in GetAllDataAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to see the actual error
            }

            return data;
        }

        public async Task<bool> SaveChangesAsync(List<CsvDataModel> changes, string user = "Anonymous")
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();

                foreach (var change in changes)
                {
                    // Get the original values for audit comparison
                    using var getOriginalCmd = connection.CreateCommand();
                    getOriginalCmd.Transaction = transaction;
                    getOriginalCmd.CommandText = "SELECT Confirmed, Notes FROM CsvData WHERE Id = @id";
                    getOriginalCmd.Parameters.Add(new SqlParameter("@id", change.Id));

                    using var reader = await getOriginalCmd.ExecuteReaderAsync();
                    bool originalConfirmed = false;
                    string? originalNotes = null;

                    if (await reader.ReadAsync())
                    {
                        originalConfirmed = reader.GetBoolean("Confirmed");
                        originalNotes = reader.IsDBNull("Notes") ? null : reader.GetString("Notes");
                    }
                    reader.Close();

                    // Update the main record
                    using var updateCmd = connection.CreateCommand();
                    updateCmd.Transaction = transaction;
                    updateCmd.CommandText = @"
                        UPDATE CsvData 
                        SET Confirmed = @confirmed, Notes = @notes 
                        WHERE Id = @id";

                    updateCmd.Parameters.Add(new SqlParameter("@confirmed", change.Confirmed));
                    updateCmd.Parameters.Add(new SqlParameter("@notes", change.Notes ?? (object)DBNull.Value));
                    updateCmd.Parameters.Add(new SqlParameter("@id", change.Id));

                    await updateCmd.ExecuteNonQueryAsync();

                    // Log audit trail for each changed field
                    if (originalConfirmed != change.Confirmed)
                    {
                        await LogCsvDataAuditAsync(change.Id, "UPDATE", "Confirmed", 
                            originalConfirmed.ToString(), change.Confirmed.ToString(), user, connection, transaction);
                    }

                    if (originalNotes != change.Notes)
                    {
                        await LogCsvDataAuditAsync(change.Id, "UPDATE", "Notes", 
                            originalNotes, change.Notes, user, connection, transaction);
                    }

                    // Update company counts after each change
                    if (originalConfirmed != change.Confirmed)
                    {
                        var firmanr = change.GetProperty("Firmanr")?.ToString();
                        if (!string.IsNullOrEmpty(firmanr))
                        {
                            await UpdateCompanyCountsInternalAsync(firmanr, user, connection, transaction);
                        }
                    }

                    // Log the legacy audit trail for compatibility
                    await LogAuditAsync(change.Id.ToString(), "UPDATE", user, JsonConvert.SerializeObject(change), connection, transaction);
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

        private async Task LogCsvDataAuditAsync(Guid recordId, string action, string columnName, 
            string? oldValue, string? newValue, string modifiedBy, SqlConnection connection, SqlTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
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

        private async Task UpdateCompanyCountsInternalAsync(string firmanr, string modifiedBy, 
            SqlConnection connection, SqlTransaction transaction)
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
                totalRows = reader.GetInt32("TotalRows");
                processedRows = reader.GetInt32("ProcessedRows");
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
                companyId = currentReader.GetGuid("GUID");
                currentTotalRows = currentReader.GetInt32("TotalRows");
                currentProcessedRows = currentReader.GetInt32("TotalRowsProcessed");
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
                    await _auditService.LogCompanyDetailsAuditAsync(companyId, "UPDATE", "TotalRows", 
                        currentTotalRows.ToString(), totalRows.ToString(), modifiedBy);
                }

                if (processedChanged)
                {
                    await _auditService.LogCompanyDetailsAuditAsync(companyId, "UPDATE", "TotalRowsProcessed", 
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
        }

        public async Task<List<AuditLogModel>> GetAuditLogsAsync(string searchTerm = "")
        {
            var logs = new List<AuditLogModel>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            
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
                    Changes = reader.GetString("Changes")
                });
            }

            return logs;
        }

        public async Task<DataUpdateNotification> CheckForNewDataAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
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

        private async Task LogAuditAsync(string recordId, string action, string user, string changes, SqlConnection connection, SqlTransaction transaction)
        {
            using var auditCmd = connection.CreateCommand();
            auditCmd.Transaction = transaction;
            auditCmd.CommandText = @"
                INSERT INTO AuditLog (Timestamp, Action, [User], RecordId, Changes)
                VALUES (@timestamp, @action, @user, @recordId, @changes)";

            auditCmd.Parameters.Add(new SqlParameter("@timestamp", DateTime.Now));
            auditCmd.Parameters.Add(new SqlParameter("@action", action));
            auditCmd.Parameters.Add(new SqlParameter("@user", user));
            auditCmd.Parameters.Add(new SqlParameter("@recordId", recordId));
            auditCmd.Parameters.Add(new SqlParameter("@changes", changes));

            await auditCmd.ExecuteNonQueryAsync();
        }

        public async Task<List<string>> GetColumnNamesAsync()
        {
            var columns = new List<string>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
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

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
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

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT GUID, Firmanr, Assignee, ProcessedStatus, Created 
                FROM CompanyDetails 
                ORDER BY Firmanr";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                companies.Add(new CompanyDetails
                {
                    Id = reader.GetGuid("GUID"),
                    Firmanr = reader.IsDBNull("Firmanr") ? "" : reader["Firmanr"].ToString() ?? "",
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

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"SELECT 
                    cd.GUID,
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
                    Id = reader.GetGuid("GUID"),
                    Firmanr = reader.IsDBNull("Firmanr") ? "" : reader["Firmanr"].ToString() ?? "",
                    Assignee = reader.IsDBNull("Assignee") ? 0 : Convert.ToInt32(reader["Assignee"]),
                    AssigneeName = reader.IsDBNull("AssigneeName") ? "" : reader["AssigneeName"].ToString() ?? "",
                    ProcessedStatus = reader.IsDBNull("ProcessedStatus") ? "" : reader["ProcessedStatus"].ToString() ?? "",
                    Created = reader["Created"] == DBNull.Value ? null : Convert.ToDateTime(reader["Created"]),

                });
            }

            return companies;
        }

        public async Task<int> InsertCompanyDetailsAsync(CompanyDetails company, string user = "System")
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if a record with the same company name exists
                bool recordExists = false;
                CompanyDetails existing = null;
                Guid? companyId = null;
                
                using (var checkCommand = connection.CreateCommand())
                {
                    checkCommand.CommandText = @"
                        SELECT TOP 1 GUID, Firmanr, Assignee, ProcessedStatus
                        FROM CompanyDetails
                        WHERE Firmanr = @firmanr
                        ORDER BY Created DESC";
                    checkCommand.Parameters.Add(new SqlParameter("@firmanr", company.Firmanr));

                    using var reader = await checkCommand.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        recordExists = true;
                        companyId = reader.GetGuid("GUID");
                        existing = new CompanyDetails
                        {
                            Id = companyId.Value,
                            Firmanr = reader.IsDBNull("Firmanr") ? null : reader.GetString("Firmanr"),
                            Assignee = reader.IsDBNull("Assignee") ? null : reader.GetInt32("Assignee"),
                            ProcessedStatus = reader.IsDBNull("ProcessedStatus") ? null : reader.GetString("ProcessedStatus")
                        };
                    }
                }

                int rowsAffected = 0;

                if (recordExists && companyId.HasValue)
                {
                    // Update existing record with audit logging
                    using var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = @"
                        UPDATE CompanyDetails 
                        SET Assignee = @assignee, 
                            ProcessedStatus = @processedStatus,
                            LastModified = GETUTCDATE(),
                            LastModifiedBy = @modifiedBy
                        WHERE Firmanr = @firmanr";

                    // Use provided values or keep existing ones if new values are null/empty
                    var assigneeToUpdate = company.Assignee ?? existing?.Assignee;
                    var statusToUpdate = string.IsNullOrEmpty(company.ProcessedStatus) ? existing?.ProcessedStatus : company.ProcessedStatus;

                    updateCommand.Parameters.Add(new SqlParameter("@firmanr", company.Firmanr));
                    updateCommand.Parameters.Add(new SqlParameter("@assignee", (object?)assigneeToUpdate ?? DBNull.Value));
                    updateCommand.Parameters.Add(new SqlParameter("@processedStatus", (object?)statusToUpdate ?? DBNull.Value));
                    updateCommand.Parameters.Add(new SqlParameter("@modifiedBy", user));

                    rowsAffected = await updateCommand.ExecuteNonQueryAsync();

                    // Log audit trail for changed fields
                    if (existing?.Assignee != assigneeToUpdate)
                    {
                        await _auditService.LogCompanyDetailsAuditAsync(companyId.Value, "UPDATE", "Assignee", 
                            existing?.Assignee?.ToString(), assigneeToUpdate?.ToString(), user);
                    }

                    if (existing?.ProcessedStatus != statusToUpdate)
                    {
                        await _auditService.LogCompanyDetailsAuditAsync(companyId.Value, "UPDATE", "ProcessedStatus", 
                            existing?.ProcessedStatus, statusToUpdate, user);
                    }
                }
                else
                {
                    // Insert new record with audit logging
                    using var insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = @"
                        INSERT INTO CompanyDetails (GUID, Firmanr, Assignee, ProcessedStatus, Created, LastModified, LastModifiedBy)
                        OUTPUT INSERTED.GUID
                        VALUES (NEWID(), @firmanr, @assignee, @processedStatus, GETUTCDATE(), GETUTCDATE(), @modifiedBy)";

                    insertCommand.Parameters.Add(new SqlParameter("@firmanr", company.Firmanr));
                    insertCommand.Parameters.Add(new SqlParameter("@assignee", (object?)company.Assignee ?? DBNull.Value));
                    insertCommand.Parameters.Add(new SqlParameter("@processedStatus", (object?)company.ProcessedStatus ?? DBNull.Value));
                    insertCommand.Parameters.Add(new SqlParameter("@modifiedBy", user));

                    var newId = await insertCommand.ExecuteScalarAsync();
                    if (newId != null && Guid.TryParse(newId.ToString(), out var newCompanyId))
                    {
                        rowsAffected = 1;
                        
                        // Log audit trail for insert
                        await _auditService.LogCompanyDetailsAuditAsync(newCompanyId, "INSERT", "Firmanr", 
                            null, company.Firmanr, user);
                        
                        if (company.Assignee.HasValue)
                        {
                            await _auditService.LogCompanyDetailsAuditAsync(newCompanyId, "INSERT", "Assignee", 
                                null, company.Assignee.ToString(), user);
                        }
                        
                        if (!string.IsNullOrEmpty(company.ProcessedStatus))
                        {
                            await _auditService.LogCompanyDetailsAuditAsync(newCompanyId, "INSERT", "ProcessedStatus", 
                                null, company.ProcessedStatus, user);
                        }
                    }
                }

                return rowsAffected > 0 ? 1 : 0; // Return 1 for success, 0 for failure
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error upserting company details: {ex.Message}");
                throw;
            }
        }                
    }
}