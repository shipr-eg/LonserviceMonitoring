# Lonservice Monitoring - Windows Task Scheduler Setup Script
# Run this script as Administrator

param(
    [string]$TaskName = "LonserviceMonitoring",
    [string]$ApplicationPath = $PSScriptRoot,
    [string]$LogPath = "$PSScriptRoot\logs"
)

Write-Host "=== Lonservice Monitoring - Task Scheduler Setup ===" -ForegroundColor Green

# Verify we're running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator. Please run PowerShell as Administrator and try again."
    exit 1
}

# Build the application first
Write-Host "`nBuilding application..." -ForegroundColor Yellow
Set-Location $ApplicationPath
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Please fix compilation errors and try again."
    exit 1
}

# Define the executable path
$ExePath = Join-Path $ApplicationPath "bin\Release\net8.0\LonserviceMonitoring.exe"
if (-not (Test-Path $ExePath)) {
    Write-Error "Executable not found at: $ExePath"
    exit 1
}

Write-Host "Application built successfully at: $ExePath" -ForegroundColor Green

# Create the scheduled task
Write-Host "`nCreating scheduled task: $TaskName" -ForegroundColor Yellow

try {
    # Remove existing task if it exists
    if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
        Write-Host "Removing existing task..." -ForegroundColor Yellow
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    }

    # Create new task action
    $Action = New-ScheduledTaskAction -Execute $ExePath -WorkingDirectory $ApplicationPath

    # Create trigger to start at system startup
    $Trigger = New-ScheduledTaskTrigger -AtStartup

    # Create task settings
    $Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -RunOnlyIfNetworkAvailable -DontStopOnIdleEnd

    # Create principal to run as SYSTEM with highest privileges
    $Principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

    # Register the task
    Register-ScheduledTask -TaskName $TaskName -Action $Action -Trigger $Trigger -Settings $Settings -Principal $Principal -Description "Lonservice CSV Monitoring Application - Monitors and processes CSV files automatically"

    Write-Host "✅ Scheduled task created successfully!" -ForegroundColor Green
    Write-Host "Task Name: $TaskName" -ForegroundColor Cyan
    Write-Host "Executable: $ExePath" -ForegroundColor Cyan
    Write-Host "Trigger: At system startup" -ForegroundColor Cyan
    Write-Host "Run as: SYSTEM account" -ForegroundColor Cyan

    # Start the task immediately for testing
    Write-Host "`nStarting task for immediate testing..." -ForegroundColor Yellow
    Start-ScheduledTask -TaskName $TaskName
    
    Start-Sleep -Seconds 3
    
    # Check task status
    $Task = Get-ScheduledTask -TaskName $TaskName
    $TaskInfo = Get-ScheduledTaskInfo -TaskName $TaskName
    
    Write-Host "`nTask Status:" -ForegroundColor Cyan
    Write-Host "State: $($Task.State)" -ForegroundColor White
    Write-Host "Last Run Time: $($TaskInfo.LastRunTime)" -ForegroundColor White
    Write-Host "Last Result: $($TaskInfo.LastTaskResult)" -ForegroundColor White
    Write-Host "Next Run Time: $($TaskInfo.NextRunTime)" -ForegroundColor White

    Write-Host "`n=== Setup Complete ===" -ForegroundColor Green
    Write-Host "The Lonservice Monitoring application is now configured to:" -ForegroundColor Yellow
    Write-Host "• Start automatically when Windows boots" -ForegroundColor White
    Write-Host "• Run continuously in the background" -ForegroundColor White
    Write-Host "• Monitor C:\Temp\LonserviceMonitoring\SourceFiles\ for CSV files" -ForegroundColor White
    Write-Host "• Process files and store data in SQL Server database" -ForegroundColor White
    Write-Host "• Log all activities to $LogPath" -ForegroundColor White
    
    Write-Host "`nTo manage the task:" -ForegroundColor Yellow
    Write-Host "• View: Get-ScheduledTask -TaskName '$TaskName'" -ForegroundColor White
    Write-Host "• Start: Start-ScheduledTask -TaskName '$TaskName'" -ForegroundColor White
    Write-Host "• Stop: Stop-ScheduledTask -TaskName '$TaskName'" -ForegroundColor White
    Write-Host "• Remove: Unregister-ScheduledTask -TaskName '$TaskName'" -ForegroundColor White

} catch {
    Write-Error "Failed to create scheduled task: $($_.Exception.Message)"
    exit 1
}
