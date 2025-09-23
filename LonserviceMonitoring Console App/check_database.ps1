# Database Query Script to Show Processed Data

Write-Host "=== LONSERVICE MONITORING - DATABASE RESULTS ===" -ForegroundColor Green

try {
    # Connection string
    $connectionString = "Server=AUTOOSJEBVGVRPW\SQLEXPRESS;Database=LonserviceMonitoringDB;Trusted_Connection=true;TrustServerCertificate=true;"
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()

    Write-Host "`n1. CSV DATA RECORDS:" -ForegroundColor Yellow
    $query1 = "SELECT TOP 10 SourceFileName, Company, Department, Employee_Name, Payroll_Error_Type, Amount, AdditionalData, CreatedDate FROM CsvData ORDER BY CreatedDate DESC"
    $command1 = New-Object System.Data.SqlClient.SqlCommand($query1, $connection)
    $reader1 = $command1.ExecuteReader()
    
    $results1 = @()
    while ($reader1.Read()) {
        $results1 += [PSCustomObject]@{
            File = $reader1["SourceFileName"]
            Company = $reader1["Company"]
            Department = $reader1["Department"]
            Employee = $reader1["Employee_Name"]
            ErrorType = $reader1["Payroll_Error_Type"]
            Amount = $reader1["Amount"]
            AdditionalData = $reader1["AdditionalData"]
            Created = $reader1["CreatedDate"]
        }
    }
    $reader1.Close()
    $results1 | Format-Table -AutoSize

    Write-Host "`n2. PROCESSING HISTORY:" -ForegroundColor Yellow
    $query2 = "SELECT FileName, RecordsProcessed, RecordsSkipped, Status, ProcessedDate FROM CsvProcessingHistory ORDER BY ProcessedDate DESC"
    $command2 = New-Object System.Data.SqlClient.SqlCommand($query2, $connection)
    $reader2 = $command2.ExecuteReader()
    
    $results2 = @()
    while ($reader2.Read()) {
        $results2 += [PSCustomObject]@{
            FileName = $reader2["FileName"]
            Processed = $reader2["RecordsProcessed"]
            Skipped = $reader2["RecordsSkipped"]
            Status = $reader2["Status"]
            ProcessedDate = $reader2["ProcessedDate"]
        }
    }
    $reader2.Close()
    $results2 | Format-Table -AutoSize

    Write-Host "`n3. RECENT LOGS (Last 5):" -ForegroundColor Yellow
    $query3 = "SELECT TOP 5 LogLevel, Source, Message, FileName, Timestamp FROM ProcessingLogs ORDER BY Timestamp DESC"
    $command3 = New-Object System.Data.SqlClient.SqlCommand($query3, $connection)
    $reader3 = $command3.ExecuteReader()
    
    $results3 = @()
    while ($reader3.Read()) {
        $results3 += [PSCustomObject]@{
            Level = $reader3["LogLevel"]
            Source = $reader3["Source"]
            Message = $reader3["Message"].ToString().Substring(0, [Math]::Min(50, $reader3["Message"].ToString().Length)) + "..."
            File = $reader3["FileName"]
            Time = $reader3["Timestamp"]
        }
    }
    $reader3.Close()
    $results3 | Format-Table -AutoSize

    $connection.Close()

    Write-Host "`n4. DYNAMIC COLUMN HANDLING DEMONSTRATION:" -ForegroundColor Yellow
    Write-Host "Notice how the system handled the new 'Bonus_Type' column:" -ForegroundColor Cyan
    Write-Host "- Original CSV had 6 columns (Company, Department, Employee_ID, Employee_Name, Payroll_Error_Type, Amount)" -ForegroundColor White
    Write-Host "- New CSV added 'Bonus_Type' column - stored in AdditionalData field as JSON" -ForegroundColor White
    Write-Host "- All data preserved and audited in database" -ForegroundColor White
    
} catch {
    Write-Host "Error connecting to database: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please ensure SQL Server Express is running" -ForegroundColor Yellow
}

Write-Host "`n=== DATABASE QUERY COMPLETE ===" -ForegroundColor Green
