# Lonservice Monitoring - Task Management Script
# Manage the Windows scheduled task for Lonservice Monitoring

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Start", "Stop", "Status", "Remove", "Logs")]
    [string]$Action,
    
    [string]$TaskName = "LonserviceMonitoring",
    [int]$LogLines = 50
)

Write-Host "=== Lonservice Monitoring - Task Manager ===" -ForegroundColor Green

function Show-TaskStatus {
    try {
        $Task = Get-ScheduledTask -TaskName $TaskName -ErrorAction Stop
        $TaskInfo = Get-ScheduledTaskInfo -TaskName $TaskName
        
        Write-Host "`nTask Information:" -ForegroundColor Cyan
        Write-Host "Name: $($Task.TaskName)" -ForegroundColor White
        Write-Host "State: $($Task.State)" -ForegroundColor $(if($Task.State -eq "Running") {"Green"} elseif($Task.State -eq "Ready") {"Yellow"} else {"Red"})
        Write-Host "Description: $($Task.Description)" -ForegroundColor White
        Write-Host "Last Run Time: $($TaskInfo.LastRunTime)" -ForegroundColor White
        Write-Host "Last Result: $($TaskInfo.LastTaskResult)" -ForegroundColor $(if($TaskInfo.LastTaskResult -eq 0) {"Green"} else {"Red"})
        Write-Host "Next Run Time: $($TaskInfo.NextRunTime)" -ForegroundColor White
        Write-Host "Number of Missed Runs: $($TaskInfo.NumberOfMissedRuns)" -ForegroundColor White
        
        # Get running processes
        $Processes = Get-Process -Name "LonserviceMonitoring" -ErrorAction SilentlyContinue
        if ($Processes) {
            Write-Host "`nRunning Processes:" -ForegroundColor Cyan
            $Processes | ForEach-Object {
                Write-Host "PID: $($_.Id) | CPU: $($_.CPU) | Memory: $([math]::Round($_.WorkingSet64/1MB, 2)) MB | Start Time: $($_.StartTime)" -ForegroundColor White
            }
        } else {
            Write-Host "`nNo LonserviceMonitoring processes currently running." -ForegroundColor Yellow
        }
        
    } catch {
        Write-Host "Task '$TaskName' not found or error occurred: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Start-Task {
    try {
        Write-Host "`nStarting task '$TaskName'..." -ForegroundColor Yellow
        Start-ScheduledTask -TaskName $TaskName
        Start-Sleep -Seconds 2
        Show-TaskStatus
    } catch {
        Write-Host "Failed to start task: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Stop-Task {
    try {
        Write-Host "`nStopping task '$TaskName'..." -ForegroundColor Yellow
        Stop-ScheduledTask -TaskName $TaskName
        
        # Also kill any running processes
        $Processes = Get-Process -Name "LonserviceMonitoring" -ErrorAction SilentlyContinue
        if ($Processes) {
            Write-Host "Terminating running processes..." -ForegroundColor Yellow
            $Processes | Stop-Process -Force
            Write-Host "✅ Processes terminated." -ForegroundColor Green
        }
        
        Start-Sleep -Seconds 2
        Show-TaskStatus
    } catch {
        Write-Host "Failed to stop task: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Remove-Task {
    try {
        Write-Host "`nRemoving task '$TaskName'..." -ForegroundColor Yellow
        
        # Stop first
        Stop-Task
        
        # Remove the task
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
        Write-Host "✅ Task removed successfully." -ForegroundColor Green
        
    } catch {
        Write-Host "Failed to remove task: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Show-Logs {
    $LogPath = Join-Path $PSScriptRoot "logs"
    
    if (Test-Path $LogPath) {
        $LogFiles = Get-ChildItem $LogPath -Filter "*.txt" | Sort-Object LastWriteTime -Descending
        
        if ($LogFiles) {
            $LatestLog = $LogFiles[0]
            Write-Host "`nShowing last $LogLines lines from: $($LatestLog.Name)" -ForegroundColor Cyan
            Write-Host "Full path: $($LatestLog.FullName)" -ForegroundColor Gray
            Write-Host "Last modified: $($LatestLog.LastWriteTime)" -ForegroundColor Gray
            Write-Host ("=" * 80) -ForegroundColor Gray
            
            Get-Content $LatestLog.FullName -Tail $LogLines | ForEach-Object {
                $color = "White"
                if ($_ -match "\[ERR\]") { $color = "Red" }
                elseif ($_ -match "\[WRN\]") { $color = "Yellow" }
                elseif ($_ -match "\[INF\].*detected|processed|added") { $color = "Green" }
                
                Write-Host $_ -ForegroundColor $color
            }
        } else {
            Write-Host "No log files found in $LogPath" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Log directory not found: $LogPath" -ForegroundColor Red
    }
}

# Execute the requested action
switch ($Action) {
    "Start" { Start-Task }
    "Stop" { Stop-Task }
    "Status" { Show-TaskStatus }
    "Remove" { Remove-Task }
    "Logs" { Show-Logs }
}

Write-Host "`nDone." -ForegroundColor Green
