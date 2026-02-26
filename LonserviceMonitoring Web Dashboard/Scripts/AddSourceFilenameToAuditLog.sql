-- Add SourceFilename column to AuditLog table
-- This will capture the source file for each audit log entry

USE [LonserviceMonitoringDB];
GO

-- Check if SourceFilename column exists, if not add it
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[AuditLog]') 
    AND name = 'SourceFilename'
)
BEGIN
    ALTER TABLE [dbo].[AuditLog]
    ADD [SourceFilename] NVARCHAR(500) NULL;
    
    PRINT 'SourceFilename column added successfully to AuditLog table.';
END
ELSE
BEGIN
    PRINT 'SourceFilename column already exists in AuditLog table.';
END
GO

-- Create index on SourceFilename for faster filtering
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_AuditLog_SourceFilename' 
    AND object_id = OBJECT_ID(N'[dbo].[AuditLog]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AuditLog_SourceFilename]
    ON [dbo].[AuditLog] ([SourceFilename])
    WHERE [SourceFilename] IS NOT NULL;
    
    PRINT 'Index on SourceFilename column created successfully.';
END
ELSE
BEGIN
    PRINT 'Index on SourceFilename already exists.';
END
GO

-- Display current AuditLog structure
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AuditLog'
ORDER BY ORDINAL_POSITION;
GO

-- Display sample audit log data
SELECT TOP 10 
    Id, Timestamp, Action, [User], RecordId, Firmanr, SourceFilename
FROM [dbo].[AuditLog]
ORDER BY Timestamp DESC;
GO
