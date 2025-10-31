-- LonserviceMonitoring Database Schema
-- This script shows the database structure that is automatically created by the application
-- The application uses Entity Framework Code First approach to create these tables automatically

-- Database: LonserviceMonitoringDB
-- Automatically created when the application starts

-- =============================================================================
-- 1. CsvData Table - Main CSV record storage
-- =============================================================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CsvData' AND xtype='U')
CREATE TABLE [dbo].[CsvData] (
    [Id] uniqueidentifier NOT NULL DEFAULT NEWID(),
    [Contacted] bit NOT NULL DEFAULT 0,
    [Notes] nvarchar(max) NOT NULL DEFAULT '',
    [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] datetime2 NULL,
    [SourceFileName] nvarchar(max) NOT NULL DEFAULT '',
    [TimeBlock] nvarchar(max) NOT NULL DEFAULT '',
    
    -- CSV Column Mappings (configurable in appsettings.json)
    [Company] nvarchar(max) NULL,
    [Department] nvarchar(max) NULL,
    [Employee_ID] nvarchar(max) NULL,
    [Employee_Name] nvarchar(max) NULL,
    [Payroll_Error_Type] nvarchar(max) NULL,
    [Amount] decimal(18,2) NULL,
    
    -- Additional dynamic columns storage (JSON format)
    [AdditionalData] nvarchar(max) NOT NULL DEFAULT '{}',
    
    CONSTRAINT [PK_CsvData] PRIMARY KEY ([Id])
);

-- Indexes for performance
CREATE NONCLUSTERED INDEX [IX_CsvData_SourceFileName] ON [CsvData] ([SourceFileName]);
CREATE NONCLUSTERED INDEX [IX_CsvData_TimeBlock] ON [CsvData] ([TimeBlock]);

-- =============================================================================
-- 2. ProcessingLogs Table - Application and processing logs
-- =============================================================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProcessingLogs' AND xtype='U')
CREATE TABLE [dbo].[ProcessingLogs] (
    [Id] uniqueidentifier NOT NULL DEFAULT NEWID(),
    [Timestamp] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [LogLevel] nvarchar(max) NOT NULL DEFAULT 'Info',
    [Source] nvarchar(max) NOT NULL DEFAULT '',
    [Message] nvarchar(max) NOT NULL DEFAULT '',
    [FileName] nvarchar(450) NULL,
    [TimeBlock] nvarchar(max) NULL,
    [Exception] nvarchar(max) NULL,
    [AdditionalData] nvarchar(max) NOT NULL DEFAULT '{}',
    
    CONSTRAINT [PK_ProcessingLogs] PRIMARY KEY ([Id])
);

-- Indexes for performance
CREATE NONCLUSTERED INDEX [IX_ProcessingLogs_Timestamp] ON [ProcessingLogs] ([Timestamp]);
CREATE NONCLUSTERED INDEX [IX_ProcessingLogs_LogLevel] ON [ProcessingLogs] ([LogLevel]);
CREATE NONCLUSTERED INDEX [IX_ProcessingLogs_Source] ON [ProcessingLogs] ([Source]);

-- =============================================================================
-- 3. AuditLogs Table - Automatic change tracking
-- =============================================================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' AND xtype='U')
CREATE TABLE [dbo].[AuditLogs] (
    [Id] uniqueidentifier NOT NULL DEFAULT NEWID(),
    [Timestamp] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [TableName] nvarchar(max) NOT NULL DEFAULT '',
    [Operation] nvarchar(max) NOT NULL DEFAULT '', -- INSERT, UPDATE, DELETE
    [RecordId] uniqueidentifier NOT NULL,
    [OldValues] nvarchar(max) NULL,
    [NewValues] nvarchar(max) NULL,
    [ModifiedBy] nvarchar(max) NULL DEFAULT 'System',
    [Changes] nvarchar(max) NOT NULL DEFAULT '',
    
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);

-- Indexes for performance
CREATE NONCLUSTERED INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);
CREATE NONCLUSTERED INDEX [IX_AuditLogs_TableName] ON [AuditLogs] ([TableName]);
CREATE NONCLUSTERED INDEX [IX_AuditLogs_RecordId] ON [AuditLogs] ([RecordId]);

-- =============================================================================
-- 4. CsvProcessingHistory Table - File processing history and metrics
-- =============================================================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CsvProcessingHistory' AND xtype='U')
CREATE TABLE [dbo].[CsvProcessingHistory] (
    [Id] uniqueidentifier NOT NULL DEFAULT NEWID(),
    [FileName] nvarchar(max) NOT NULL DEFAULT '',
    [TimeBlock] nvarchar(max) NOT NULL DEFAULT '',
    [ProcessedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [Status] nvarchar(max) NOT NULL DEFAULT '', -- SUCCESS, ERROR, PARTIAL
    [RecordsProcessed] int NOT NULL DEFAULT 0,
    [RecordsSkipped] int NOT NULL DEFAULT 0,
    [ProcessingLog] nvarchar(max) NOT NULL DEFAULT '', -- Detailed processing log
    [ErrorMessage] nvarchar(max) NULL,
    [SourcePath] nvarchar(max) NOT NULL DEFAULT '',
    [WorkPath] nvarchar(max) NOT NULL DEFAULT '',
    [LoadedPath] nvarchar(max) NULL,
    
    CONSTRAINT [PK_CsvProcessingHistory] PRIMARY KEY ([Id])
);

-- Indexes for performance
CREATE NONCLUSTERED INDEX [IX_CsvProcessingHistory_FileName] ON [CsvProcessingHistory] ([FileName]);
CREATE NONCLUSTERED INDEX [IX_CsvProcessingHistory_TimeBlock] ON [CsvProcessingHistory] ([TimeBlock]);
CREATE NONCLUSTERED INDEX [IX_CsvProcessingHistory_ProcessedDate] ON [CsvProcessingHistory] ([ProcessedDate]);

-- =============================================================================
-- Database Features Summary
-- =============================================================================
/*
This database schema provides:

1. AUTOMATIC CREATION: The application creates this database and all tables automatically
2. SEMICOLON CSV SUPPORT: Configured to handle semicolon-separated CSV files
3. DYNAMIC COLUMNS: Can handle CSV files with varying column structures
4. AUDIT TRAIL: Automatic tracking of all data changes
5. PROCESSING LOGS: Detailed logs of all file processing activities
6. PERFORMANCE: Proper indexing for fast queries
7. SCALABILITY: UUID primary keys and optimized structure

Key Configuration (in appsettings.json):
- CsvSettings.Delimiter = ";"
- CsvSettings.AutoDetectDelimiter = true
- DefaultColumns: Company, Department, Employee_ID, Employee_Name, Payroll_Error_Type, Amount

The application will:
- Detect semicolon delimiters automatically
- Create database tables on first run
- Process CSV files with Danish column names
- Track all changes with audit logs
- Provide detailed processing history
*/