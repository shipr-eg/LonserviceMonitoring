using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LonserviceMonitoring.Models
{
    public class CsvDataModel
    {
        public Guid Id { get; set; }
        public string? Firmanr { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool Confirmed { get; set; }
        public string? Notes { get; set; }
        public string? AssigneeName { get; set; }
        public string? ProcessedStatus { get; set; }
        // Dynamic properties for additional columns
        public Dictionary<string, object> AdditionalProperties { get; set; } = new();
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
        public string? Firmanr { get; set; }
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

    public class DataUpdateNotification
    {
        public bool HasNewData { get; set; }
        public int NewRecordCount { get; set; }
        public DateTime LastChecked { get; set; }
    }

    public class DashboardConfiguration
    {
        public List<string> SystemColumns { get; set; } = new();
        public List<string> EssentialColumns { get; set; } = new();
        public List<string> EssentialColumnsSuffix { get; set; } = new();
        public GroupingConfiguration? Grouping { get; set; }
    }

    public class GroupingConfiguration
    {
        public bool EnableGrouping { get; set; }
        public string GroupByColumn { get; set; } = "firmanr";
        public string SortByColumn { get; set; } = "createddate";
        public string SortDirection { get; set; } = "desc";
    }

    public class CsvProcessingHistory
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string TimeBlock { get; set; } = string.Empty;
        public DateTime ProcessedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int RecordsProcessed { get; set; }
        public int RecordsSkipped { get; set; }
        public string ProcessingLog { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string SourcePath { get; set; } = string.Empty;
        public string WorkPath { get; set; } = string.Empty;
        public string? LoadedPath { get; set; }
    }

    public class ProcessingLog
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string LogLevel { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? TimeBlock { get; set; }
        public string? Exception { get; set; }
        public string AdditionalData { get; set; } = string.Empty;
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
        public string Company { get; set; } = string.Empty;
        public int? Assignee { get; set; }
        
        public string? AssigneeName { get; set; }
        public string? ProcessedStatus { get; set; }
        public DateTime? Created { get; set; }
        
        // Additional computed property for display purposes
        // public decimal ContactedPercentage => TotalRecords > 0 ? (decimal)ContactedRecords / TotalRecords * 100 : 0;
    }
}