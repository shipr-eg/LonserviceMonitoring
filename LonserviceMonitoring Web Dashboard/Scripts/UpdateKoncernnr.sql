-- Script to populate empty Koncernnr_ values in CsvData table
-- This will set Koncernnr_ = Firmanr for all records where Koncernnr_ is NULL or empty

USE LonserviceMonitoringDB;
GO

-- First check how many records have empty Koncernnr_
SELECT 
    COUNT(*) AS TotalRecordsWithEmptyKoncernnr,
    COUNT(DISTINCT Firmanr) AS DistinctFirmanr
FROM CsvData
WHERE Koncernnr_ IS NULL OR Koncernnr_ = '';
GO

-- Update empty Koncernnr_ values to match Firmanr
UPDATE CsvData
SET Koncernnr_ = Firmanr
WHERE Koncernnr_ IS NULL OR Koncernnr_ = '';
GO

-- Verify the update
SELECT 
    COUNT(*) AS TotalRecordsWithEmptyKoncernnr
FROM CsvData
WHERE Koncernnr_ IS NULL OR Koncernnr_ = '';
GO

-- Show sample of updated records
SELECT TOP 10
    Id,
    Firmanr,
    Koncernnr_,
    SourceFileName,
    TimeBlock
FROM CsvData
ORDER BY ModifiedDate DESC;
GO
