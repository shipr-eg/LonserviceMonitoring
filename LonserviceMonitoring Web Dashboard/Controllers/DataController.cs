using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using LonserviceMonitoring.Models;
using LonserviceMonitoring.Services;

namespace LonserviceMonitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly DataService _dataService;
        private readonly DashboardConfiguration _dashboardConfig;

        public DataController(DataService dataService, IOptions<DashboardConfiguration> dashboardConfig)
        {
            _dataService = dataService;
            _dashboardConfig = dashboardConfig.Value;
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
                var user = HttpContext.Session.GetString("AdminUser") ?? "Anonymous";
                var success = await _dataService.SaveChangesAsync(request.Changes, user);
                
                if (success)
                {
                    return Ok(new { message = $"{request.Changes.Count} rows saved successfully!" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to save changes" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving changes", error = ex.Message });
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

        [HttpPost("companies")]
        public async Task<ActionResult<CompanyDetails>> CreateCompany([FromBody] CompanyDetails company)
        {
            try
            {
                // Check model validation
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(company.Company))
                {
                    return BadRequest(new { message = "Company name is required" });
                }
                await _dataService.InsertCompanyDetailsAsync(company);
                return Ok(company);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating company", error = ex.Message });
            }
        }

        [HttpPost("configuration/grouping")]
        public ActionResult UpdateGroupingConfiguration([FromBody] GroupingConfiguration groupingConfig)
        {
            try
            {
                // Update the current configuration
                _dashboardConfig.Grouping = groupingConfig;
                
                // In a real application, you might want to persist this to a database or file
                // For now, it's only updated in memory
                
                return Ok(new { message = "Grouping configuration updated successfully", configuration = groupingConfig });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating grouping configuration", error = ex.Message });
            }
        }
    }
}