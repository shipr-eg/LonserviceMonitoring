using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LonserviceMonitoring.Models
{
    public class CsvDataModel
    {
        // Static properties for known columns
        public Guid Id { get; set; }
        public string? Firmanr { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool Confirmed { get; set; }
        public string? Notes { get; set; }
        public string? AssigneeName { get; set; }
        public string? ProcessedStatus { get; set; }
        
        // Dynamic properties for any additional database columns
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalData { get; set; } = new();
        
        public object? GetProperty(string propertyName)
        {
            // Check static properties first
            switch (propertyName.ToLower())
            {
                case "id": return Id;
                case "firmanr":
                case "company": // Legacy support for Company column
                    return Firmanr;
                case "createddate": return CreatedDate;
                case "confirmed":
                case "contacted": // Legacy support for Contacted column
                    return Confirmed;
                case "notes": return Notes;
                case "assigneename": return AssigneeName;
                case "processedstatus": 
                case "companyprocessedstatus": // Map CompanyProcessedStatus from DB to ProcessedStatus property
                    return ProcessedStatus;
                default: return AdditionalData.ContainsKey(propertyName) ? AdditionalData[propertyName] : null;
            }
        }
        
        public void SetProperty(string propertyName, object? value)
        {
            // Set static properties first
            switch (propertyName.ToLower())
            {
                case "id": 
                    if (Guid.TryParse(value?.ToString(), out var id)) Id = id;
                    break;
                case "firmanr":
                case "company": // Legacy support for Company column
                    Firmanr = value?.ToString();
                    break;
                case "createddate":
                    if (DateTime.TryParse(value?.ToString(), out var date)) CreatedDate = date;
                    break;
                case "confirmed":
                case "contacted": // Legacy support for Contacted column
                    Confirmed = Convert.ToBoolean(value ?? false);
                    break;
                case "notes":
                    Notes = value?.ToString();
                    break;
                case "assigneename":
                    AssigneeName = value?.ToString();
                    break;
                case "processedstatus":
                case "companyprocessedstatus": // Map CompanyProcessedStatus from DB to ProcessedStatus property
                    ProcessedStatus = value?.ToString();
                    break;
                default: 
                    // Store all other columns in AdditionalData
                    AdditionalData[propertyName] = value ?? "";
                    break;
            }
        }
        
        public IEnumerable<string> GetAllPropertyNames()
        {
            var staticProps = new[] { "Id", "Firmanr", "CreatedDate", "Confirmed", "Notes", "AssigneeName", "ProcessedStatus" };
            return staticProps.Concat(AdditionalData.Keys);
        }
        
        public Dictionary<string, object> GetAllProperties()
        {
            var result = new Dictionary<string, object>
            {
                ["Id"] = Id,
                ["Firmanr"] = Firmanr ?? "",
                ["CreatedDate"] = CreatedDate,
                ["Confirmed"] = Confirmed,
                ["Notes"] = Notes ?? "",
                ["AssigneeName"] = AssigneeName ?? "",
                ["ProcessedStatus"] = ProcessedStatus ?? ""
            };
            
            foreach (var prop in AdditionalData)
            {
                result[prop.Key] = prop.Value;
            }
            
            return result;
        }
    }

    public class SaveChangesRequest
    {
        public List<CsvDataModel> Changes { get; set; } = new();
    }

    public class AdminLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuditLogModel
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string RecordId { get; set; } = string.Empty;
        public string Changes { get; set; } = string.Empty;
    }

    public class DataUpdateNotification
    {
        public bool HasNewData { get; set; }
        public int NewRecordCount { get; set; }
        public DateTime LastChecked { get; set; }
    }

    public class DashboardConfiguration
    {
        public List<string> HiddenColumns { get; set; } = new();
        public List<string> EssentialColumns { get; set; } = new();
        public GroupingConfiguration Grouping { get; set; } = new();
    }

    public class GroupingConfiguration
    {
        public bool EnableGrouping { get; set; } = true;
        public string GroupByColumn { get; set; } = "Firmanr";
        public string SortDirection { get; set; } = "asc"; // "asc" or "desc"
        public string SortByColumn { get; set; } = "createddate";
        public List<GroupingOption> AvailableGroupingColumns { get; set; } = new();
    }

    public class GroupingOption
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }

    public class EmployeeList
    {
        public Guid GUID { get; set; }
        public string EmployeeID { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }
        // Additional computed property for display purposes
        public string FullName => $"{FirstName} {LastName}";
    }

    public class CompanyDetails
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Firmanr { get; set; } = string.Empty;
        public int? Assignee { get; set; }
        
        public string? AssigneeName { get; set; }
        public string? ProcessedStatus { get; set; }
        public int TotalRows { get; set; } = 0;
        public int TotalRowsProcessed { get; set; } = 0;
        public DateTime? Created { get; set; }
        public DateTime? LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
        
        // Additional computed property for display purposes
        // public decimal ContactedPercentage => TotalRecords > 0 ? (decimal)ContactedRecords / TotalRecords * 100 : 0;
    }

    public class CompanyHistoryModel
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
    }
}