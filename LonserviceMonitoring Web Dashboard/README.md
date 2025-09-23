# Lonservice Monitoring Web Dashboard

A comprehensive .NET Core web application for monitoring and managing service data with real-time updates, advanced filtering, and professional UI.

## Features

### 🎯 Core Features
- **Responsive Bootstrap Dashboard** - 95% width container with optimal padding
- **Dynamic Column Support** - Automatically detects and displays all database columns
- **Grouped Data Display** - Data grouped by Company with sorting by CreatedDate
- **Frozen Columns** - Notes and Contacted columns frozen on the right side
- **Pagination** - 10 records per page with smooth navigation
- **Real-time Counts** - Live display of Total, Contacted, and Not Contacted records

### 🔍 Advanced Filtering
- **Global Search** - Search across all data from the top search bar
- **Group-level Search** - Search within each company group
- **Column-specific Filters** - Smart filters in table headers
  - Text inputs for regular columns
  - Dropdown selects for boolean fields (Contacted)
- **Multi-filter Support** - Apply multiple filters simultaneously with AND logic
- **Visual Feedback** - Active filters highlighted with blue border

### 💾 Smart Save System
- **Real-time Change Tracking** - Tracks checkbox and notes changes instantly
- **Professional Toast Notifications** - Bottom-right corner with animations
- **Bulk Save Operations** - Save multiple changes with one click
- **Loading States** - Visual feedback during save operations
- **Success Messages** - Confirmation with row count saved

### 🔐 Admin Panel
- **Secure Authentication** - Username: `LonserviceAdmin`, Password: `Lonservice$123#`
- **Audit Log Viewer** - Complete history of all changes
- **Search Functionality** - Search through audit logs
- **Session Management** - Secure login/logout system

### 🔔 Real-time Updates
- **New Data Detection** - Checks for new data every 30 seconds
- **Non-intrusive Banner** - Top banner notification for new data
- **Save & Refresh** - Option to save current changes and reload new data
- **Manual Close** - Banner stays until manually closed

### 📱 Responsive Design
- **Mobile Friendly** - Optimized for all screen sizes
- **Bootstrap 5** - Modern UI components and styling
- **Font Awesome Icons** - Professional iconography
- **Smooth Animations** - Enhanced user experience

## Technical Stack

- **.NET 8.0** - Latest .NET framework
- **ASP.NET Core MVC** - Web framework
- **SQL Server** - Database (Express edition supported)
- **Bootstrap 5** - Frontend framework
- **Font Awesome 6** - Icon library
- **JavaScript ES6+** - Modern client-side functionality

## Prerequisites

1. **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **SQL Server** - Express, Developer, or Standard edition
3. **Modern Web Browser** - Chrome, Firefox, Safari, or Edge

## Installation & Setup

### 1. Database Setup

1. **Create Database** (if not exists):
   ```sql
   CREATE DATABASE LonserviceMonitoringDB;
   ```

2. **Run Database Script**:
   ```bash
   # Navigate to the Scripts folder
   cd "Scripts"
   
   # Run the SQL script using sqlcmd (adjust connection details as needed)
   sqlcmd -S "AUTOOSJEBVGVRPW\SQLEXPRESS" -d "LonserviceMonitoringDB" -i "DatabaseSetup.sql"
   ```

   Or run the script manually in SQL Server Management Studio.

### 2. Application Setup

1. **Clone/Download** the project files
2. **Navigate** to the project directory:
   ```bash
   cd "c:\Temp\LonserviceMonitoring\LonserviceMonitoring Project\LonserviceMonitoring Web Dashboard"
   ```

3. **Update Connection String** (if needed) in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=LonserviceMonitoringDB;Trusted_Connection=true;TrustServerCertificate=true;"
     }
   }
   ```

4. **Restore Dependencies**:
   ```bash
   dotnet restore
   ```

5. **Build Application**:
   ```bash
   dotnet build
   ```

6. **Run Application**:
   ```bash
   dotnet run
   ```

### 3. Access Dashboard

1. Open your browser and navigate to: **http://localhost:8080**
2. The dashboard will load automatically without credentials
3. For admin access, click the **Admin** button and use:
   - **Username**: `LonserviceAdmin`
   - **Password**: `Lonservice$123#`

## Usage Guide

### Basic Operations

1. **View Data**: Data is automatically grouped by company and sorted by creation date
2. **Search**: Use the global search bar to find records across all companies
3. **Filter**: Use column filters to narrow down specific data
4. **Contact Status**: Check/uncheck the "Contacted" boxes as needed
5. **Add Notes**: Type in the Notes field to add comments
6. **Save Changes**: Click "SAVE" in the toast notification to persist changes

### Advanced Features

#### Multi-Filter Usage
1. Enter text in any column filter
2. Use dropdowns for boolean fields
3. Combine with global and group searches
4. Filters work together with AND logic

#### Pagination
- Use arrow buttons to navigate pages
- 10 records shown per page by default
- Page numbers displayed for easy jumping

#### Admin Functions
1. Click "Admin" button (top-right)
2. Login with admin credentials
3. View audit logs of all changes
4. Search through audit history
5. Monitor user activity

### Browser Recommendations

For optimal security, the system recommends using **incognito/private mode**:
- **Chrome**: Ctrl+Shift+N
- **Firefox**: Ctrl+Shift+P
- **Safari**: Cmd+Shift+N
- **Edge**: Ctrl+Shift+N

## Configuration

### Database Schema

The system dynamically reads table columns, but requires these core fields:
- `Id` (UNIQUEIDENTIFIER) - Primary key
- `Company` (NVARCHAR) - Grouping column
- `CreatedDate` (DATETIME2) - Sorting column
- `Contacted` (BIT) - Boolean flag
- `Notes` (NVARCHAR(MAX)) - Text field

Additional columns are automatically detected and displayed.

### Customization Options

#### Change Grouping Column
Update the grouping logic in `DataService.cs`:
```csharp
// Change "Company" to your desired grouping column
const string groupingColumn = "YourColumnName";
```

#### Adjust Pagination
Modify records per page in `dashboard.js`:
```javascript
const recordsPerPage = 20; // Change from 10 to desired number
```

#### Update Check Interval
Modify new data check frequency in `dashboard.js`:
```javascript
setInterval(checkForNewData, 60000); // Change from 30000 (30s) to 60000 (60s)
```

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/data` | GET | Retrieve all data |
| `/api/data/save` | POST | Save changes |
| `/api/data/columns` | GET | Get column names |
| `/api/data/check-updates` | GET | Check for new data |
| `/api/admin/login` | POST | Admin authentication |
| `/api/admin/logout` | POST | Admin logout |
| `/api/admin/audit-logs` | GET | Retrieve audit logs |

## Troubleshooting

### Common Issues

1. **Connection Failed**
   - Verify SQL Server is running
   - Check connection string accuracy
   - Ensure database exists

2. **Port Already in Use**
   ```bash
   # Change port in Program.cs
   app.Urls.Add("http://localhost:8081");
   ```

3. **JavaScript Errors**
   - Ensure all static files are served correctly
   - Check browser console for specific errors
   - Verify Bootstrap and Font Awesome CDN links

4. **Data Not Loading**
   - Check database connection
   - Verify CsvData table exists and has data
   - Review server logs for errors

### Development Mode

To run in development mode with detailed error information:
```bash
dotnet run --environment Development
```

## Security Considerations

- **Admin Credentials**: Change default admin password in production
- **Connection String**: Use environment variables for sensitive data
- **HTTPS**: Enable HTTPS in production environments
- **SQL Injection**: Parameterized queries used throughout
- **Session Management**: Secure session configuration implemented

## Performance Optimization

- **Database Indexes**: Created on frequently queried columns
- **Pagination**: Limits data transfer and rendering
- **Debounced Search**: Prevents excessive API calls
- **Bulk Operations**: Efficient multi-row updates
- **Caching**: Static file caching enabled

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review browser console for JavaScript errors
3. Check application logs for server-side issues
4. Verify database connectivity and permissions

## License

This project is developed for Lonservice monitoring purposes. All rights reserved.