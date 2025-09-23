namespace LonserviceMonitoring.Settings
{
    public class MonitoringSettings
    {
        public string MonitoredFolderPath { get; set; } = "";
        public bool EnableEmailMonitoring { get; set; } = false;
        public string[] SupportedExtensions { get; set; } = { ".csv" };
        public int PollingIntervalSeconds { get; set; } = 30;
    }

    public class CsvSettings
    {
        public string[] DefaultColumns { get; set; } = 
        {
            "Company", "Department", "Employee_ID", "Employee_Name", 
            "Payroll_Error_Type", "Amount"
        };
        public bool HasHeader { get; set; } = true;
        public string Delimiter { get; set; } = ",";
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = "";
        public int CommandTimeoutSeconds { get; set; } = 30;
        public bool EnableRetryOnFailure { get; set; } = true;
    }

    public class EmailSettings
    {
        public string ClientId { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string MonitoredEmailAddress { get; set; } = "";
        public string[] AllowedSenders { get; set; } = Array.Empty<string>();
        public string[] FileExtensions { get; set; } = { ".csv" };
    }
}
