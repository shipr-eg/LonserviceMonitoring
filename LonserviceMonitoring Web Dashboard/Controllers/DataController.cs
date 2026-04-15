using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using LonserviceMonitoring.Models;
using LonserviceMonitoring.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LonserviceMonitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly DataService _dataService;
        private readonly DashboardConfiguration _dashboardConfig;
        private readonly ILogger<DataController> _logger;

        public DataController(DataService dataService, IOptions<DashboardConfiguration> dashboardConfig, ILogger<DataController> logger)
        {
            _dataService = dataService;
            _dashboardConfig = dashboardConfig.Value;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<CsvDataModel>>> GetData()
        {
            try
            {
                var data = await _dataService.GetAllDataAsync();
                data.ForEach(d => d.ProcessedStatus = d.ProcessedStatus ?? "Not Started");
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving data", error = ex.Message });
            }
        }

        [HttpPost("save")]
        public async Task<ActionResult> SaveChanges([FromBody] SaveChangesRequest request)
        {
            try
            {
                // Priority order for user identification:
                // 1. Employee initials from session (primary method)
                // 2. User from request body (fallback)
                // 3. Windows authenticated user
                // 4. Session admin user
                // 5. Anonymous
                var user = HttpContext.Session.GetString("EmployeeInitials");
                
                if (string.IsNullOrEmpty(user))
                {
                    user = request.User;
                }
                
                if (string.IsNullOrEmpty(user))
                {
                    user = HttpContext.User?.Identity?.Name;
                }
                
                if (string.IsNullOrEmpty(user))
                {
                    user = HttpContext.Session.GetString("AdminUser");
                }
                
                if (string.IsNullOrEmpty(user))
                {
                    user = "Anonymous";
                }
                
                _logger.LogInformation("Save request received with {RowCount} rows from user {User}", request.Changes.Count, user);
                
                var success = await _dataService.SaveChangesAsync(request.Changes, user);
                
                if (success)
                {
                    _logger.LogInformation("Successfully saved {RowCount} rows", request.Changes.Count);
                    return Ok(new { message = $"{request.Changes.Count} rows saved successfully!" });
                }
                else
                {
                    _logger.LogWarning("Failed to save {RowCount} rows - returned false from DataService", request.Changes.Count);
                    return StatusCode(500, new { message = "Failed to save changes. Check server logs for details." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while saving changes: {ErrorMessage}", ex.Message);
                return StatusCode(500, new { message = "Error saving changes", error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        [HttpGet("columns")]
        public async Task<ActionResult<List<string>>> GetColumns()
        {
            try
            {
                var columns = await _dataService.GetColumnNamesAsync();
                return Ok(columns);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving columns", error = ex.Message });
            }
        }

        [HttpGet("check-updates")]
        public async Task<ActionResult<DataUpdateNotification>> CheckForUpdates()
        {
            try
            {
                var notification = await _dataService.CheckForNewDataAsync();
                return Ok(notification);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking for updates", error = ex.Message });
            }
        }

        [HttpGet("configuration")]
        public ActionResult<DashboardConfiguration> GetConfiguration()
        {
            try
            {
                return Ok(_dashboardConfig);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving configuration", error = ex.Message });
            }
        }

        [HttpGet("employees")]
        public async Task<ActionResult<List<EmployeeList>>> GetAllEmployees()
        {
            try
            {
                var employees = await _dataService.GetAllEmployeesAsync();
                return Ok(employees);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving employees", error = ex.Message });
            }
        }

        [HttpGet("companies")]
        public async Task<ActionResult<List<CompanyDetails>>> GetAllCompanies()
        {
            try
            {
                var companies = await _dataService.GetAllCompanyDetailsAsync();
                return Ok(companies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving companies", error = ex.Message });
            }
        }

        [HttpGet("companies/{name}")]
        public async Task<ActionResult<List<CompanyDetails>>> GetCompanyByName(string name)
        {
            try
            {
                var companies = await _dataService.GetCompanyDetailsByNameAsync(name);
                if (companies == null || !companies.Any())
                {
                    return NotFound(new { message = $"No companies found with name '{name}'" });
                }
                return Ok(companies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving companies", error = ex.Message });
            }
        }

        [HttpGet("companies/{firmanr}/history")]
        public async Task<ActionResult<List<CompanyHistoryModel>>> GetCompanyHistory(string firmanr)
        {
            try
            {
                var history = await _dataService.GetCompanyHistoryAsync(firmanr);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving company history", error = ex.Message });
            }
        }

        [HttpPost("companies")]
        public async Task<ActionResult<CompanyDetails>> CreateCompany([FromBody] JsonElement payload)
        {
            try
            {
                string? GetStringProp(string name)
                {
                    if (payload.TryGetProperty(name, out var p) && p.ValueKind != JsonValueKind.Null)
                    {
                        return p.ToString();
                    }
                    return null;
                }

                bool HasProp(string name) => payload.TryGetProperty(name, out _);

                var companyName = GetStringProp("Company") ?? GetStringProp("company");
                var status = GetStringProp("ProcessedStatus") ?? GetStringProp("processedStatus");
                var assigneeRaw = GetStringProp("Assignee") ?? GetStringProp("assignee");
                var hasAssigneeField = HasProp("Assignee") || HasProp("assignee");
                var hasStatusField = HasProp("ProcessedStatus") || HasProp("processedStatus");

                if (string.IsNullOrWhiteSpace(companyName))
                {
                    return BadRequest(new { message = "Company name is required" });
                }

                int? assignee = null;
                if (!string.IsNullOrWhiteSpace(assigneeRaw))
                {
                    if (int.TryParse(assigneeRaw, out var parsedAssignee))
                    {
                        assignee = parsedAssignee;
                    }
                    else
                    {
                        return BadRequest(new { message = "Assignee must be a numeric employee ID or null" });
                    }
                }

                var company = new CompanyDetails
                {
                    Company = companyName.Trim(),
                    Assignee = assignee,
                    ProcessedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim()
                };

                var user = HttpContext.Session.GetString("EmployeeInitials")
                        ?? HttpContext.Session.GetString("AdminUser")
                        ?? "Unknown";
                await _dataService.InsertCompanyDetailsAsync(company, user, hasAssigneeField, hasStatusField);
                return Ok(company);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating company", error = ex.Message });
            }
        }

        [HttpPost("validate-initials")]
        public async Task<ActionResult> ValidateInitials([FromBody] EmployeeLoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Initials))
                {
                    return Ok(new { isValid = false, message = "Please enter your initials" });
                }

                var employee = await _dataService.ValidateEmployeeInitialsAsync(request.Initials);
                
                if (employee != null)
                {
                    // Store employee info in session
                    HttpContext.Session.SetString("EmployeeInitials", employee.Initials);
                    HttpContext.Session.SetString("EmployeeName", employee.FullName);
                    HttpContext.Session.SetString("EmployeeID", employee.EmployeeID);
                    
                    _logger.LogInformation("User {Initials} ({Name}) logged in successfully", employee.Initials, employee.FullName);
                    
                    return Ok(new { isValid = true, employee = new { employee.Initials, employee.FullName } });
                }
                else
                {
                    _logger.LogWarning("Failed login attempt with initials: {Initials}", request.Initials);
                    return Ok(new { isValid = false, message = "Invalid initials. Please check and try again." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating initials: {ErrorMessage}", ex.Message);
                return StatusCode(500, new { message = "Error validating initials", error = ex.Message });
            }
        }
    }
}