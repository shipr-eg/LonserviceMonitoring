using System.ComponentModel.DataAnnotations;

namespace LonserviceMonitoring.Models
{
    public class CsvDataModel
    {
        public Guid Id { get; set; }
        public string? Company { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool Contacted { get; set; }
        public string? Notes { get; set; }
        
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
    }
}