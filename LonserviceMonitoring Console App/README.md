# Lonservice Monitoring System

A comprehensive C# console application for monitoring and processing CSV files from Office 365 emails or local file system.

## Features

- **Dual Monitoring Modes**: 
  - Physical Path monitoring (default)
  - Office 365 email monitoring with Microsoft Graph API
- **Dynamic CSV Processing**: Handles varying column structures
- **Comprehensive Logging**: Full audit trail of all operations
- **Database Integration**: SQL Server with Entity Framework Core
- **Automatic File Management**: Organized folder structure with timestamping

## Configuration

### Database Setup
- Server: `AUTOOSJEBVGVRPW\SQLEXPRESS`
- Database: `LonserviceMonitoringDB`
- The application will automatically create the database and tables on first run

### Folder Structure
The application automatically creates the following folders:
- `C:\Temp\LonserviceMonitoring\SourceFiles\` - Source CSV files
- `C:\Temp\LonserviceMonitoring\WorkFolder\` - Processing workspace
- `C:\Temp\LonserviceMonitoring\WorkFolder\Loaded\` - Successfully processed files

### Configuration Options (appsettings.json)

```json
{
  "MonitoringSettings": {
    "MonitoringType": "PHYSICAL_PATH", // or "EMAIL"
    "CheckIntervalMinutes": 5,
    "SourcePath": "C:\\Temp\\LonserviceMonitoring\\SourceFiles\\",
    "WorkFolder": "C:\\Temp\\LonserviceMonitoring\\WorkFolder\\",
    "LoadedFolder": "C:\\Temp\\LonserviceMonitoring\\WorkFolder\\Loaded\\"
  },
  "EmailSettings": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id", 
    "ClientSecret": "your-client-secret",
    "MailboxEmail": "your-email@domain.com",
    "FolderName": "Inbox"
  }
}
```

## Expected CSV Columns

The system is designed to handle these default columns:
- Company
- Department  
- Employee_ID
- Employee_Name
- Payroll_Error_Type
- Amount

**Note**: The system gracefully handles:
- Missing columns (logged as warnings)
- Additional columns (stored in AdditionalData field)
- Varying column orders
- Data type mismatches

## How It Works

1. **Initialization**: Creates required folders and database tables
2. **Monitoring**: Watches for new CSV files (via folder monitoring or email)
3. **Processing**: 
   - Moves files to timestamped work folders
   - Parses CSV data with dynamic column handling
   - Stores data in database with full audit logging
   - Moves processed files to "Loaded" folder
4. **Audit Trail**: Every operation is logged for compliance and debugging

## Running the Application

1. Ensure SQL Server Express is running
2. Update `appsettings.json` with your configuration
3. Run the application: `dotnet run`
4. The application runs continuously until stopped (Ctrl+C)

## Database Tables

- **CsvData**: Main data table with ID, Contacted, Notes, and CSV columns
- **ProcessingLogs**: System operation logs
- **AuditLogs**: Database change audit trail  
- **CsvProcessingHistory**: Complete processing history per file

## Office 365 Setup (Optional)

To use email monitoring:
1. Register an application in Azure AD
2. Grant necessary Graph API permissions
3. Update EmailSettings in appsettings.json
4. Change MonitoringType to "EMAIL"

## Troubleshooting

- Check logs in the `logs/` directory
- Verify database connectivity
- Ensure source folders exist and are accessible
- For email monitoring, verify Azure AD app permissions

## Acceptance Criteria Compliance

✅ AC1: Email/Folder Monitoring System
✅ AC1.1: Database structure with audit logging
✅ AC2: CSV Data Processing with dynamic column handling
✅ Comprehensive error handling and logging
✅ Automatic folder creation and file management
