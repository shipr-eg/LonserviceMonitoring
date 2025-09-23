@echo off
echo ================================================
echo Lonservice Monitoring - Quick Task Manager
echo ================================================
echo.
echo 1. Setup Windows Scheduler
echo 2. Start Service
echo 3. Stop Service  
echo 4. Check Status
echo 5. View Logs
echo 6. Remove Service
echo 7. Exit
echo.
set /p choice="Enter your choice (1-7): "

if "%choice%"=="1" (
    echo Setting up Windows Scheduler...
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0Setup-WindowsScheduler.ps1"
    pause
    goto menu
)

if "%choice%"=="2" (
    echo Starting Lonservice Monitoring...
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0Manage-Task.ps1" -Action Start
    pause
    goto menu
)

if "%choice%"=="3" (
    echo Stopping Lonservice Monitoring...
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0Manage-Task.ps1" -Action Stop
    pause
    goto menu
)

if "%choice%"=="4" (
    echo Checking status...
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0Manage-Task.ps1" -Action Status
    pause
    goto menu
)

if "%choice%"=="5" (
    echo Viewing recent logs...
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0Manage-Task.ps1" -Action Logs
    pause
    goto menu
)

if "%choice%"=="6" (
    echo Removing service...
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0Manage-Task.ps1" -Action Remove
    pause
    goto menu
)

if "%choice%"=="7" (
    echo Goodbye!
    exit
)

echo Invalid choice. Please try again.
pause

:menu
cls
goto :eof
