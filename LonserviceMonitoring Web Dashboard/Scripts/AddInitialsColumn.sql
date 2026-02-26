-- Add Initials column to EmployeeList table
-- This column will store 5-character employee initials for login

USE [LonserviceMonitoringDB];
GO

-- Check if Initials column exists, if not add it
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeList]') 
    AND name = 'Initials'
)
BEGIN
    ALTER TABLE [dbo].[EmployeeList]
    ADD [Initials] NVARCHAR(5) NULL;
    
    PRINT 'Initials column added successfully.';
END
ELSE
BEGIN
    PRINT 'Initials column already exists.';
END
GO

-- Optionally, update existing records with initials
-- You can customize this based on your convention
-- Example: First 2 chars of FirstName + First 3 chars of LastName
/*
UPDATE [dbo].[EmployeeList]
SET Initials = UPPER(LEFT(FirstName, 2) + LEFT(LastName, 3))
WHERE Initials IS NULL;
GO
*/

-- Create index on Initials for faster login lookups
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_EmployeeList_Initials' 
    AND object_id = OBJECT_ID(N'[dbo].[EmployeeList]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EmployeeList_Initials]
    ON [dbo].[EmployeeList] ([Initials])
    WHERE [Initials] IS NOT NULL;
    
    PRINT 'Index on Initials column created successfully.';
END
ELSE
BEGIN
    PRINT 'Index on Initials already exists.';
END
GO

-- Display current EmployeeList data
SELECT EmployeeID, FirstName, LastName, Initials, IsActive, IsAdmin
FROM [dbo].[EmployeeList]
ORDER BY FirstName, LastName;
GO
