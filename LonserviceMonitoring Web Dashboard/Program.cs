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

app.Run();