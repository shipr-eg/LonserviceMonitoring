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
            command.CommandText = "SELECT * FROM CsvData ORDER BY Company, Id";

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

                    switch (columnName.ToLower())
                    {
                        case "id":
                            if (Guid.TryParse(value?.ToString(), out var id))
                                record.Id = id;
                            else
                                record.Id = Guid.NewGuid();
                            break;
                        case "company":
                            record.Company = value?.ToString();
                            break;
                        case "contacted":
                            record.Contacted = Convert.ToBoolean(value ?? false);
                            break;
                        case "notes":
                            record.Notes = value?.ToString();
                            break;
                        default:
                            // All other columns go into additional properties
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

                foreach (var change in changes)
                {
                    // Update the main record
                    using var updateCmd = _connection.CreateCommand();
                    updateCmd.Transaction = transaction;
                    updateCmd.CommandText = @"
                        UPDATE CsvData 
                        SET Contacted = @contacted, Notes = @notes 
                        WHERE Id = @id";

                    updateCmd.Parameters.Add(new SqlParameter("@contacted", change.Contacted));
                    updateCmd.Parameters.Add(new SqlParameter("@notes", change.Notes ?? (object)DBNull.Value));
                    updateCmd.Parameters.Add(new SqlParameter("@id", change.Id));

                    await updateCmd.ExecuteNonQueryAsync();

                    // Log the audit trail
                    await LogAuditAsync(change.Id.ToString(), "UPDATE", user, JsonConvert.SerializeObject(change), transaction);
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
                    Changes = reader.GetString("Changes")
                });
            }

            return logs;
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

        private async Task LogAuditAsync(string recordId, string action, string user, string changes, SqlTransaction transaction)
        {
            using var auditCmd = _connection.CreateCommand();
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
    }
}