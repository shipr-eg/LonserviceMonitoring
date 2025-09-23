@echo off
echo ========================================
echo  Lonservice Monitoring Dashboard
echo ========================================
echo.

echo Checking .NET installation...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET is not installed or not in PATH
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo .NET version:
dotnet --version
echo.

echo Restoring dependencies...
dotnet restore
if errorlevel 1 (
    echo ERROR: Failed to restore dependencies
    pause
    exit /b 1
)

echo.
echo Building application...
dotnet build
if errorlevel 1 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo ========================================
echo  Starting Lonservice Monitoring Dashboard
echo ========================================
echo.
echo Dashboard will be available at:
echo  ► http://localhost:8080
echo.
echo Admin Credentials:
echo  ► Username: LonserviceAdmin
echo  ► Password: Lonservice$123#
echo.
echo Press Ctrl+C to stop the application
echo ========================================
echo.

dotnet run

pause