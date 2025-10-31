namespace LonserviceMonitoring.Models
{
    public class MonitoringSettings
    {
        public string MonitoringType { get; set; } = "PHYSICAL_PATH";
        public int CheckIntervalMinutes { get; set; } = 5;
        public string SourcePath { get; set; } = string.Empty;
        public string WorkFolder { get; set; } = string.Empty;
        public string LoadedFolder { get; set; } = string.Empty;
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public class EmailSettings
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string MailboxEmail { get; set; } = string.Empty;
        public string FolderName { get; set; } = "Inbox";
    }

    public class CsvSettings
    {
        public List<string> DefaultColumns { get; set; } = new List<string>();
        public string Delimiter { get; set; } = ",";
        public bool AutoDetectDelimiter { get; set; } = true;
    }
}
