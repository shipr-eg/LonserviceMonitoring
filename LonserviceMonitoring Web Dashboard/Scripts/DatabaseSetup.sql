-- Database Setup Script for Lonservice Monitoring
-- Run this script against your SQL Server to create the required tables

USE LonserviceMonitoringDB;
GO

-- Create CsvData table (main data table)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CsvData' AND xtype='U')
BEGIN
    CREATE TABLE CsvData (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Company NVARCHAR(255) NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        Contacted BIT NOT NULL DEFAULT 0,
        Notes NVARCHAR(MAX) NULL,
        
        -- Add additional columns as needed for your CSV data
        -- Example columns (customize based on your actual CSV structure):
        CustomerName NVARCHAR(255) NULL,
        Email NVARCHAR(255) NULL,
        Phone NVARCHAR(50) NULL,
        Address NVARCHAR(500) NULL,
        Status NVARCHAR(100) NULL,
        Priority NVARCHAR(50) NULL,
        AssignedTo NVARCHAR(255) NULL,
        Category NVARCHAR(100) NULL,
        Value DECIMAL(18,2) NULL,
        LastModified DATETIME2 DEFAULT GETDATE()
    );
    
    -- Create index for better performance
    CREATE INDEX IX_CsvData_Company_CreatedDate ON CsvData (Company, CreatedDate DESC);
    CREATE INDEX IX_CsvData_Contacted ON CsvData (Contacted);
    CREATE INDEX IX_CsvData_CreatedDate ON CsvData (CreatedDate DESC);
    
    PRINT 'CsvData table created successfully';
END
ELSE
BEGIN
    PRINT 'CsvData table already exists';
END
GO

-- Create AuditLog table for tracking changes
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLog' AND xtype='U')
BEGIN
    CREATE TABLE AuditLog (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Timestamp DATETIME2 NOT NULL DEFAULT GETDATE(),
        Action NVARCHAR(50) NOT NULL,
        [User] NVARCHAR(255) NOT NULL,
        RecordId NVARCHAR(50) NOT NULL,
        Changes NVARCHAR(MAX) NULL
    );
    
    -- Create index for better performance
    CREATE INDEX IX_AuditLog_Timestamp ON AuditLog (Timestamp DESC);
    CREATE INDEX IX_AuditLog_RecordId ON AuditLog (RecordId);
    CREATE INDEX IX_AuditLog_User ON AuditLog ([User]);
    
    PRINT 'AuditLog table created successfully';
END
ELSE
BEGIN
    PRINT 'AuditLog table already exists';
END
GO

-- Insert sample data for testing (optional)
IF (SELECT COUNT(*) FROM CsvData) = 0
BEGIN
    INSERT INTO CsvData (Company, CreatedDate, Contacted, Notes, CustomerName, Email, Phone, Status, Priority, Category, Value)
    VALUES 
    ('Tech Solutions Inc', DATEADD(day, -5, GETDATE()), 0, 'Initial contact needed', 'John Smith', 'john@techsolutions.com', '555-1234', 'New', 'High', 'Software', 15000.00),
    ('Tech Solutions Inc', DATEADD(day, -3, GETDATE()), 1, 'Follow-up scheduled', 'Jane Doe', 'jane@techsolutions.com', '555-1235', 'In Progress', 'Medium', 'Hardware', 8500.00),
    ('Global Services LLC', DATEADD(day, -7, GETDATE()), 0, '', 'Bob Johnson', 'bob@globalservices.com', '555-2345', 'New', 'Low', 'Consulting', 5000.00),
    ('Global Services LLC', DATEADD(day, -2, GETDATE()), 1, 'Proposal sent', 'Alice Wilson', 'alice@globalservices.com', '555-2346', 'Proposal', 'High', 'Training', 12000.00),
    ('Innovative Corp', DATEADD(day, -1, GETDATE()), 0, 'Needs immediate attention', 'Charlie Brown', 'charlie@innovative.com', '555-3456', 'Urgent', 'Critical', 'Support', 25000.00),
    ('Innovative Corp', DATEADD(day, -4, GETDATE()), 1, 'Contract signed', 'Diana Prince', 'diana@innovative.com', '555-3457', 'Closed', 'High', 'Implementation', 35000.00);
    
    PRINT 'Sample data inserted successfully';
END
ELSE
BEGIN
    PRINT 'Sample data already exists or table is not empty';
END
GO

-- Create a view for easy data access with calculated fields
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CsvDataSummary')
BEGIN
    EXEC('
    CREATE VIEW vw_CsvDataSummary AS
    SELECT 
        Company,
        COUNT(*) as TotalRecords,
        SUM(CASE WHEN Contacted = 1 THEN 1 ELSE 0 END) as ContactedCount,
        SUM(CASE WHEN Contacted = 0 THEN 1 ELSE 0 END) as NotContactedCount,
        AVG(CASE WHEN Value IS NOT NULL THEN Value ELSE 0 END) as AverageValue,
        MIN(CreatedDate) as FirstRecord,
        MAX(CreatedDate) as LastRecord
    FROM CsvData
    GROUP BY Company
    ');
    
    PRINT 'Summary view created successfully';
END
ELSE
BEGIN
    PRINT 'Summary view already exists';
END
GO

-- Create stored procedure for bulk updates (optional, for performance)
IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_BulkUpdateCsvData')
BEGIN
    EXEC('
    CREATE PROCEDURE sp_BulkUpdateCsvData
        @Changes NVARCHAR(MAX)
    AS
    BEGIN
        SET NOCOUNT ON;
        
        -- Parse JSON and update records
        UPDATE c
        SET 
            Contacted = j.Contacted,
            Notes = j.Notes,
            LastModified = GETDATE()
        FROM CsvData c
        INNER JOIN OPENJSON(@Changes) 
        WITH (
            Id UNIQUEIDENTIFIER ''$.id'',
            Contacted BIT ''$.contacted'',
            Notes NVARCHAR(MAX) ''$.notes''
        ) j ON c.Id = j.Id;
        
        -- Return affected row count
        SELECT @@ROWCOUNT as AffectedRows;
    END
    ');
    
    PRINT 'Bulk update stored procedure created successfully';
END
ELSE
BEGIN
    PRINT 'Bulk update stored procedure already exists';
END
GO

PRINT 'Database setup completed successfully!';
PRINT 'You can now run the Lonservice Monitoring Dashboard application.';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Update the connection string in appsettings.json if needed';
PRINT '2. Run the application using: dotnet run';
PRINT '3. Navigate to: http://localhost:8080';
PRINT '4. Use admin credentials: LonserviceAdmin / Lonservice$123#';