using Microsoft.Data.SqlClient;
using System.Data;
using LonserviceMonitoring.Services;
using LonserviceMonitoring.Models;

var builder = WebApplication.CreateBuilder(args);

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

// Add HttpContextAccessor for user context
builder.Services.AddHttpContextAccessor();

// Add UserContextService
builder.Services.AddScoped<UserContextService>();

// Add AuditService first
builder.Services.AddScoped<AuditService>(provider =>
{
    var configuration = provider.GetService<IConfiguration>();
    var connectionString = configuration?.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("DefaultConnection string not found");
    return new AuditService(connectionString);
});

// Add DataService with proper dependencies
builder.Services.AddScoped<DataService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var auditService = provider.GetRequiredService<AuditService>();
    return new DataService(configuration, auditService);
});

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

// Set default port to 8080
app.Urls.Add("http://localhost:8080");

app.Run();