using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LonserviceMonitoring.Models
{
    [Table("CsvData")]
    public class CsvDataRecord
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public bool Contacted { get; set; } = false;
        
        public string Notes { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? ModifiedDate { get; set; }
        
        public string SourceFileName { get; set; } = string.Empty;
        
        public string TimeBlock { get; set; } = string.Empty;
        
        // Dynamic CSV columns will be added as additional properties
        public string? Company { get; set; }
        public string? Department { get; set; }
        public string? Employee_ID { get; set; }
        public string? Employee_Name { get; set; }
        public string? Payroll_Error_Type { get; set; }
        public decimal? Amount { get; set; }
        
        // Additional dynamic columns storage (JSON format for flexibility)
        public string AdditionalData { get; set; } = "{}";
    }

    [Table("ProcessingLogs")]
    public class ProcessingLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [Required]
        public string LogLevel { get; set; } = "Info";
        
        [Required]
        public string Source { get; set; } = string.Empty;
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public string? FileName { get; set; }
        
        public string? TimeBlock { get; set; }
        
        public string? Exception { get; set; }
        
        public string AdditionalData { get; set; } = "{}";
    }

    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [Required]
        public string TableName { get; set; } = string.Empty;
        
        [Required]
        public string Operation { get; set; } = string.Empty; // INSERT, UPDATE, DELETE
        
        [Required]
        public Guid RecordId { get; set; }
        
        public string? OldValues { get; set; }
        
        public string? NewValues { get; set; }
        
        public string? ModifiedBy { get; set; } = "System";
        
        public string Changes { get; set; } = string.Empty;
    }

    [Table("CsvProcessingHistory")]
    public class CsvProcessingHistory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        public string TimeBlock { get; set; } = string.Empty;
        
        [Required]
        public DateTime ProcessedDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public string Status { get; set; } = string.Empty; // SUCCESS, ERROR, PARTIAL
        
        public int RecordsProcessed { get; set; } = 0;
        
        public int RecordsSkipped { get; set; } = 0;
        
        public string ProcessingLog { get; set; } = string.Empty; // Concatenated audit log
        
        public string? ErrorMessage { get; set; }
        
        public string SourcePath { get; set; } = string.Empty;
        
        public string WorkPath { get; set; } = string.Empty;
        
        public string? LoadedPath { get; set; }
    }
}
