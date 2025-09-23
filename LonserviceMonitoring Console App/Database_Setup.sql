-- Lonservice Monitoring Database Setup Script
-- Run this script on AUTOOSJEBVGVRPW\SQLEXPRESS if automatic creation fails

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'LonserviceMonitoringDB')
BEGIN
    CREATE DATABASE LonserviceMonitoringDB;
END
GO

USE LonserviceMonitoringDB;
GO

-- Create CsvData table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CsvData' AND xtype='U')
BEGIN
    CREATE TABLE CsvData (
        Id uniqueidentifier NOT NULL DEFAULT NEWID() PRIMARY KEY,
        Contacted bit NOT NULL DEFAULT 0,
        Notes nvarchar(max) NOT NULL DEFAULT '',
        CreatedDate datetime2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate datetime2 NULL,
        SourceFileName nvarchar(255) NOT NULL DEFAULT '',
        TimeBlock nvarchar(50) NOT NULL DEFAULT '',
        Company nvarchar(255) NULL,
        Department nvarchar(255) NULL,
        Employee_ID nvarchar(50) NULL,
        Employee_Name nvarchar(255) NULL,
        Payroll_Error_Type nvarchar(255) NULL,
        Amount decimal(18,2) NULL,
        AdditionalData nvarchar(max) NOT NULL DEFAULT '{}'
    );
    
    CREATE INDEX IX_CsvData_SourceFileName ON CsvData(SourceFileName);
    CREATE INDEX IX_CsvData_TimeBlock ON CsvData(TimeBlock);
    CREATE INDEX IX_CsvData_CreatedDate ON CsvData(CreatedDate);
END
GO

-- Create ProcessingLogs table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProcessingLogs' AND xtype='U')
BEGIN
    CREATE TABLE ProcessingLogs (
        Id uniqueidentifier NOT NULL DEFAULT NEWID() PRIMARY KEY,
        Timestamp datetime2 NOT NULL DEFAULT GETUTCDATE(),
        LogLevel nvarchar(50) NOT NULL DEFAULT 'Info',
        Source nvarchar(255) NOT NULL DEFAULT '',
        Message nvarchar(max) NOT NULL DEFAULT '',
        FileName nvarchar(255) NULL,
        TimeBlock nvarchar(50) NULL,
        Exception nvarchar(max) NULL,
        AdditionalData nvarchar(max) NOT NULL DEFAULT '{}'
    );
    
    CREATE INDEX IX_ProcessingLogs_Timestamp ON ProcessingLogs(Timestamp);
    CREATE INDEX IX_ProcessingLogs_LogLevel ON ProcessingLogs(LogLevel);
    CREATE INDEX IX_ProcessingLogs_Source ON ProcessingLogs(Source);
END
GO

-- Create AuditLogs table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' AND xtype='U')
BEGIN
    CREATE TABLE AuditLogs (
        Id uniqueidentifier NOT NULL DEFAULT NEWID() PRIMARY KEY,
        Timestamp datetime2 NOT NULL DEFAULT GETUTCDATE(),
        TableName nvarchar(255) NOT NULL DEFAULT '',
        Operation nvarchar(50) NOT NULL DEFAULT '',
        RecordId uniqueidentifier NOT NULL,
        OldValues nvarchar(max) NULL,
        NewValues nvarchar(max) NULL,
        ModifiedBy nvarchar(255) NULL DEFAULT 'System',
        Changes nvarchar(max) NOT NULL DEFAULT ''
    );
    
    CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);
    CREATE INDEX IX_AuditLogs_TableName ON AuditLogs(TableName);
    CREATE INDEX IX_AuditLogs_RecordId ON AuditLogs(RecordId);
END
GO

-- Create CsvProcessingHistory table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CsvProcessingHistory' AND xtype='U')
BEGIN
    CREATE TABLE CsvProcessingHistory (
        Id uniqueidentifier NOT NULL DEFAULT NEWID() PRIMARY KEY,
        FileName nvarchar(255) NOT NULL DEFAULT '',
        TimeBlock nvarchar(50) NOT NULL DEFAULT '',
        ProcessedDate datetime2 NOT NULL DEFAULT GETUTCDATE(),
        Status nvarchar(50) NOT NULL DEFAULT '',
        RecordsProcessed int NOT NULL DEFAULT 0,
        RecordsSkipped int NOT NULL DEFAULT 0,
        ProcessingLog nvarchar(max) NOT NULL DEFAULT '',
        ErrorMessage nvarchar(max) NULL,
        SourcePath nvarchar(500) NOT NULL DEFAULT '',
        WorkPath nvarchar(500) NOT NULL DEFAULT '',
        LoadedPath nvarchar(500) NULL
    );
    
    CREATE INDEX IX_CsvProcessingHistory_FileName ON CsvProcessingHistory(FileName);
    CREATE INDEX IX_CsvProcessingHistory_TimeBlock ON CsvProcessingHistory(TimeBlock);
    CREATE INDEX IX_CsvProcessingHistory_ProcessedDate ON CsvProcessingHistory(ProcessedDate);
END
GO

PRINT 'Database setup completed successfully';
