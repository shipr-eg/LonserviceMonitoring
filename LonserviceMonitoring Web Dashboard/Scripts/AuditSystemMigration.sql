-- Migration script to add audit functionality and update CompanyDetails table
-- Execute this script on your database to add the new audit features

-- 1. Add new columns to CompanyDetails table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CompanyDetails]') AND name = 'TotalRows')
BEGIN
    ALTER TABLE CompanyDetails ADD TotalRows INT NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CompanyDetails]') AND name = 'TotalRowsProcessed')
BEGIN
    ALTER TABLE CompanyDetails ADD TotalRowsProcessed INT NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CompanyDetails]') AND name = 'LastModified')
BEGIN
    ALTER TABLE CompanyDetails ADD LastModified DATETIME2 NULL DEFAULT GETUTCDATE();
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CompanyDetails]') AND name = 'LastModifiedBy')
BEGIN
    ALTER TABLE CompanyDetails ADD LastModifiedBy NVARCHAR(255) NULL;
END

-- 2. Create CsvDataAudit table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CsvDataAudit]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CsvDataAudit](
        [Id] [uniqueidentifier] NOT NULL DEFAULT NEWID(),
        [RecordId] [uniqueidentifier] NOT NULL,
        [Action] [nvarchar](50) NOT NULL,
        [ColumnName] [nvarchar](255) NOT NULL,
        [OldValue] [nvarchar](max) NULL,
        [NewValue] [nvarchar](max) NULL,
        [ModifiedBy] [nvarchar](255) NOT NULL,
        [Timestamp] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_CsvDataAudit] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    CREATE INDEX [IX_CsvDataAudit_RecordId_Timestamp] ON [dbo].[CsvDataAudit] ([RecordId], [Timestamp]);
    CREATE INDEX [IX_CsvDataAudit_ColumnName] ON [dbo].[CsvDataAudit] ([ColumnName]);
END

-- 3. Create CompanyDetailsAudit table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CompanyDetailsAudit]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CompanyDetailsAudit](
        [Id] [uniqueidentifier] NOT NULL DEFAULT NEWID(),
        [RecordId] [uniqueidentifier] NOT NULL,
        [Action] [nvarchar](50) NOT NULL,
        [ColumnName] [nvarchar](255) NOT NULL,
        [OldValue] [nvarchar](max) NULL,
        [NewValue] [nvarchar](max) NULL,
        [ModifiedBy] [nvarchar](255) NOT NULL,
        [Timestamp] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_CompanyDetailsAudit] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    CREATE INDEX [IX_CompanyDetailsAudit_RecordId_Timestamp] ON [dbo].[CompanyDetailsAudit] ([RecordId], [Timestamp]);
    CREATE INDEX [IX_CompanyDetailsAudit_ColumnName] ON [dbo].[CompanyDetailsAudit] ([ColumnName]);
END

-- 4. Create EmployeeListAudit table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeListAudit]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EmployeeListAudit](
        [Id] [uniqueidentifier] NOT NULL DEFAULT NEWID(),
        [RecordId] [uniqueidentifier] NOT NULL,
        [Action] [nvarchar](50) NOT NULL,
        [ColumnName] [nvarchar](255) NOT NULL,
        [OldValue] [nvarchar](max) NULL,
        [NewValue] [nvarchar](max) NULL,
        [ModifiedBy] [nvarchar](255) NOT NULL,
        [Timestamp] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_EmployeeListAudit] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    CREATE INDEX [IX_EmployeeListAudit_RecordId_Timestamp] ON [dbo].[EmployeeListAudit] ([RecordId], [Timestamp]);
    CREATE INDEX [IX_EmployeeListAudit_ColumnName] ON [dbo].[EmployeeListAudit] ([ColumnName]);
END

-- 5. Add Confirmed column to CsvData if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CsvData]') AND name = 'Confirmed')
BEGIN
    -- Check if Contacted column exists and use it as basis
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CsvData]') AND name = 'Contacted')
    BEGIN
        -- Add Confirmed column and copy values from Contacted
        ALTER TABLE CsvData ADD Confirmed BIT NOT NULL DEFAULT 0;
        UPDATE CsvData SET Confirmed = Contacted;
        PRINT 'Added Confirmed column and copied values from Contacted column.';
    END
    ELSE
    BEGIN
        -- Just add the Confirmed column
        ALTER TABLE CsvData ADD Confirmed BIT NOT NULL DEFAULT 0;
        PRINT 'Added Confirmed column to CsvData table.';
    END
END

-- 6. Update existing CompanyDetails records with initial counts
UPDATE cd 
SET 
    TotalRows = (
        SELECT COUNT(*) 
        FROM CsvData csv 
        WHERE csv.Firmanr = cd.Firmanr
    ),
    TotalRowsProcessed = (
        SELECT COUNT(*) 
        FROM CsvData csv 
        WHERE csv.Firmanr = cd.Firmanr 
        AND csv.Confirmed = 1  -- Individual records marked as confirmed/completed
    ),
    LastModified = GETUTCDATE(),
    LastModifiedBy = 'System Migration'
FROM CompanyDetails cd
WHERE cd.TotalRows = 0 OR cd.TotalRows IS NULL;

PRINT 'Updated CompanyDetails with initial counts based on individual record completion status.';

-- 7. Create stored procedure for automatic audit trail
CREATE OR ALTER PROCEDURE [dbo].[sp_LogCompanyDetailsAudit]
    @RecordId UNIQUEIDENTIFIER,
    @Action NVARCHAR(50),
    @ColumnName NVARCHAR(255),
    @OldValue NVARCHAR(MAX),
    @NewValue NVARCHAR(MAX),
    @ModifiedBy NVARCHAR(255)
AS
BEGIN
    INSERT INTO CompanyDetailsAudit (RecordId, Action, ColumnName, OldValue, NewValue, ModifiedBy, Timestamp)
    VALUES (@RecordId, @Action, @ColumnName, @OldValue, @NewValue, @ModifiedBy, GETUTCDATE());
END

-- 8. Create stored procedure for automatic CsvData audit trail
CREATE OR ALTER PROCEDURE [dbo].[sp_LogCsvDataAudit]
    @RecordId UNIQUEIDENTIFIER,
    @Action NVARCHAR(50),
    @ColumnName NVARCHAR(255),
    @OldValue NVARCHAR(MAX),
    @NewValue NVARCHAR(MAX),
    @ModifiedBy NVARCHAR(255)
AS
BEGIN
    INSERT INTO CsvDataAudit (RecordId, Action, ColumnName, OldValue, NewValue, ModifiedBy, Timestamp)
    VALUES (@RecordId, @Action, @ColumnName, @OldValue, @NewValue, @ModifiedBy, GETUTCDATE());
END

-- 9. Create stored procedure to mark all records as completed when company is processed
CREATE OR ALTER PROCEDURE [dbo].[sp_MarkCompanyRecordsAsCompleted]
    @Firmanr NVARCHAR(255),
    @ModifiedBy NVARCHAR(255)
AS
BEGIN
    DECLARE @UpdatedRecords INT = 0;
    
    -- Update all non-confirmed records for this company
    UPDATE CsvData 
    SET Confirmed = 1 
    WHERE Firmanr = @Firmanr 
    AND Confirmed = 0;
    
    SET @UpdatedRecords = @@ROWCOUNT;
    
    -- Log audit trail for each updated record
    INSERT INTO CsvDataAudit (RecordId, Action, ColumnName, OldValue, NewValue, ModifiedBy, Timestamp)
    SELECT 
        Id,
        'UPDATE',
        'Confirmed',
        '0',
        '1',
        @ModifiedBy,
        GETUTCDATE()
    FROM CsvData 
    WHERE Firmanr = @Firmanr 
    AND Confirmed = 1
    AND Id NOT IN (
        SELECT RecordId 
        FROM CsvDataAudit 
        WHERE ColumnName = 'Confirmed' 
        AND NewValue = '1' 
        AND ModifiedBy = @ModifiedBy
        AND Timestamp >= DATEADD(SECOND, -10, GETUTCDATE()) -- Within last 10 seconds
    );
    
    -- Update the company's TotalRowsProcessed count
    UPDATE CompanyDetails 
    SET TotalRowsProcessed = (
        SELECT COUNT(*) 
        FROM CsvData 
        WHERE Firmanr = @Firmanr 
        AND Confirmed = 1
    ),
    LastModified = GETUTCDATE(),
    LastModifiedBy = @ModifiedBy
    WHERE Firmanr = @Firmanr;
    
    PRINT 'Marked ' + CAST(@UpdatedRecords AS NVARCHAR(10)) + ' records as completed for company: ' + @Firmanr;
END

-- 10. Create trigger to automatically mark records when company status changes to Processed
CREATE OR ALTER TRIGGER [dbo].[tr_CompanyProcessedStatusUpdate]
ON [dbo].[CompanyDetails]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if ProcessedStatus was changed to 'Processed'
    IF UPDATE(ProcessedStatus)
    BEGIN
        DECLARE @Firmanr NVARCHAR(255);
        DECLARE @NewStatus NVARCHAR(255);
        DECLARE @OldStatus NVARCHAR(255);
        DECLARE @ModifiedBy NVARCHAR(255);
        
        -- Process each updated record
        DECLARE status_cursor CURSOR FOR
        SELECT 
            i.Firmanr,
            i.ProcessedStatus as NewStatus,
            d.ProcessedStatus as OldStatus,
            ISNULL(i.LastModifiedBy, 'System') as ModifiedBy
        FROM inserted i
        INNER JOIN deleted d ON i.Id = d.Id
        WHERE i.ProcessedStatus = 'Processed' 
        AND (d.ProcessedStatus IS NULL OR d.ProcessedStatus != 'Processed');
        
        OPEN status_cursor;
        FETCH NEXT FROM status_cursor INTO @Firmanr, @NewStatus, @OldStatus, @ModifiedBy;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Mark all records for this company as completed
            EXEC sp_MarkCompanyRecordsAsCompleted @Firmanr, @ModifiedBy;
            
            FETCH NEXT FROM status_cursor INTO @Firmanr, @NewStatus, @OldStatus, @ModifiedBy;
        END
        
        CLOSE status_cursor;
        DEALLOCATE status_cursor;
    END
END

PRINT 'Audit system migration completed successfully!';
PRINT 'Added automatic company processing functionality:';
PRINT '- When a company ProcessedStatus is set to "Processed", all CSV records for that company will be marked as Confirmed=1';
PRINT '- TotalRowsProcessed will be automatically updated';
PRINT '- Full audit trail will be maintained for all changes';