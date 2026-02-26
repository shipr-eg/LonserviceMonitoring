-- Rename Firmanr column to CompanyDetails in AuditLog table
-- This will store the full composite key "Firmanr | Koncernnr_"

USE [LonserviceMonitoringDB];
GO

-- Check if Firmanr column exists and CompanyDetails doesn't
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[AuditLog]') 
    AND name = 'Firmanr'
)
AND NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[AuditLog]') 
    AND name = 'CompanyDetails'
)
BEGIN
    -- Rename the column
    EXEC sp_rename 'dbo.AuditLog.Firmanr', 'CompanyDetails', 'COLUMN';
    
    PRINT 'Firmanr column renamed to CompanyDetails successfully.';
END
ELSE
BEGIN
    PRINT 'Column rename operation skipped - either Firmanr does not exist or CompanyDetails already exists.';
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
    Id, Timestamp, Action, [User], RecordId, CompanyDetails, SourceFilename
FROM [dbo].[AuditLog]
ORDER BY Timestamp DESC;
GO
