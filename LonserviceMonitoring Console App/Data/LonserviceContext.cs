using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using LonserviceMonitoring.Models;
using System.Text.Json;

namespace LonserviceMonitoring.Data
{
    public class LonserviceContext : DbContext
    {
        public LonserviceContext(DbContextOptions<LonserviceContext> options) : base(options)
        {
        }

        public DbSet<CsvDataRecord> CsvData { get; set; }
        public DbSet<ProcessingLog> ProcessingLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<CsvProcessingHistory> CsvProcessingHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure CsvDataRecord
            modelBuilder.Entity<CsvDataRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.HasIndex(e => e.SourceFileName);
                entity.HasIndex(e => e.TimeBlock);
            });

            // Configure ProcessingLog
            modelBuilder.Entity<ProcessingLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.LogLevel);
                entity.HasIndex(e => e.Source);
            });

            // Configure AuditLog
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.TableName);
                entity.HasIndex(e => e.RecordId);
            });

            // Configure CsvProcessingHistory
            modelBuilder.Entity<CsvProcessingHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.ProcessedDate).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.FileName);
                entity.HasIndex(e => e.TimeBlock);
                entity.HasIndex(e => e.ProcessedDate);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            GenerateAuditEntries();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void GenerateAuditEntries()
        {
            var auditEntries = new List<AuditLog>();
            
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditLog
                {
                    TableName = entry.Entity.GetType().Name,
                    Timestamp = DateTime.UtcNow
                };

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.Operation = "INSERT";
                        auditEntry.NewValues = JsonSerializer.Serialize(GetEntityValues(entry));
                        break;
                    case EntityState.Modified:
                        auditEntry.Operation = "UPDATE";
                        auditEntry.OldValues = JsonSerializer.Serialize(GetOriginalValues(entry));
                        auditEntry.NewValues = JsonSerializer.Serialize(GetEntityValues(entry));
                        break;
                    case EntityState.Deleted:
                        auditEntry.Operation = "DELETE";
                        auditEntry.OldValues = JsonSerializer.Serialize(GetEntityValues(entry));
                        break;
                }

                // Get the primary key value
                var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                if (keyProperty != null && keyProperty.CurrentValue != null)
                {
                    if (Guid.TryParse(keyProperty.CurrentValue.ToString(), out Guid recordId))
                        auditEntry.RecordId = recordId;
                }

                auditEntry.Changes = GenerateChangeDescription(entry);
                auditEntries.Add(auditEntry);
            }

            if (auditEntries.Any())
            {
                AuditLogs.AddRange(auditEntries);
            }
        }

        private static Dictionary<string, object?> GetEntityValues(EntityEntry entry)
        {
            return entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        }

        private static Dictionary<string, object?> GetOriginalValues(EntityEntry entry)
        {
            return entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
        }

        private static string GenerateChangeDescription(EntityEntry entry)
        {
            var changes = new List<string>();
            
            foreach (var property in entry.Properties)
            {
                if (property.IsModified)
                {
                    changes.Add($"{property.Metadata.Name}: '{property.OriginalValue}' -> '{property.CurrentValue}'");
                }
            }
            
            return string.Join("; ", changes);
        }
    }
}
