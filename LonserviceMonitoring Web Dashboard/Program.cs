using Microsoft.Data.SqlClient;
using System.Data;
using LonserviceMonitoring.Services;
using LonserviceMonitoring.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for file-based logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        path: Path.Combine(AppContext.BaseDirectory, "logs", "app-.txt"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure dashboard settings
builder.Services.Configure<DashboardConfiguration>(
    builder.Configuration.GetSection("DashboardConfiguration"));

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add connection string
builder.Services.AddScoped<IDbConnection>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new SqlConnection(connectionString);
});

// Add DataService
builder.Services.AddScoped<DataService>();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();

app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Note: When running under IIS, URLs are managed by IIS, not by the app
// For development, configure URLs in appsettings.json or launchSettings.json

// Ensure AuditLog table has required columns (migration for CompanyDetails and SourceFilename)
try
{
    using var conn = new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
    await conn.OpenAsync();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLog' AND COLUMN_NAME = 'CompanyDetails')
            ALTER TABLE AuditLog ADD CompanyDetails NVARCHAR(500) NULL;
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLog' AND COLUMN_NAME = 'SourceFilename')
            ALTER TABLE AuditLog ADD SourceFilename NVARCHAR(500) NULL;
        -- Backfill CompanyDetails from old Firmanr column for any rows that predate the migration
        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AuditLog' AND COLUMN_NAME = 'Firmanr')
            UPDATE AuditLog SET CompanyDetails = Firmanr WHERE CompanyDetails IS NULL AND Firmanr IS NOT NULL;
    ";
    await cmd.ExecuteNonQueryAsync();
    Log.Information("AuditLog schema migration completed.");
}
catch (Exception ex)
{
    Log.Warning(ex, "AuditLog schema migration failed (non-fatal): {Message}", ex.Message);
}

app.Run();