using Microsoft.AspNetCore.Mvc;
using LonserviceMonitoring.Models;
using LonserviceMonitoring.Services;

namespace LonserviceMonitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataService _dataService;
        private readonly IConfiguration _configuration;

        public AdminController(DataService dataService, IConfiguration configuration)
        {
            _dataService = dataService;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public ActionResult Login([FromBody] AdminLoginRequest request)
        {
            var adminUsername = _configuration["AdminCredentials:Username"];
            var adminPassword = _configuration["AdminCredentials:Password"];

            if (request.Username == adminUsername && request.Password == adminPassword)
            {
                HttpContext.Session.SetString("AdminUser", request.Username);
                return Ok(new { success = true, message = "Login successful" });
            }

            return Unauthorized(new { success = false, message = "Invalid credentials" });
        }

        [HttpPost("logout")]
        public ActionResult Logout()
        {
            HttpContext.Session.Remove("AdminUser");
            return Ok(new { success = true, message = "Logout successful" });
        }

        [HttpGet("audit-logs")]
        public async Task<ActionResult<List<AuditLogModel>>> GetAuditLogs([FromQuery] string searchTerm = "")
        {
            try
            {
                // Allow any logged-in employee — admin panel is accessible to all users
                var isAuthenticated = HttpContext.Session.GetString("EmployeeInitials") != null
                                   || HttpContext.Session.GetString("AdminUser") != null;
                if (!isAuthenticated)
                {
                    return Unauthorized(new { message = "Login required" });
                }

                var logs = await _dataService.GetAuditLogsAsync(searchTerm);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving audit logs", error = ex.Message });
            }
        }

        [HttpGet("is-admin")]
        public ActionResult IsAdmin()
        {
            var isAdmin = HttpContext.Session.GetString("AdminUser") != null;
            return Ok(new { isAdmin = isAdmin });
        }

        [HttpGet("csv-history")]
        public async Task<ActionResult<List<CsvProcessingHistory>>> GetCsvHistory()
        {
            try
            {
                var history = await _dataService.GetCsvProcessingHistoryAsync();
                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving CSV history", error = ex.Message });
            }
        }

        [HttpGet("processing-logs")]
        public async Task<ActionResult<List<ProcessingLog>>> GetProcessingLogs([FromQuery] string fileName, [FromQuery] string timeBlock)
        {
            try
            {
                var logs = await _dataService.GetProcessingLogsByFileAsync(fileName, timeBlock);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving processing logs", error = ex.Message });
            }
        }

        [HttpGet("employee/next-id")]
        public async Task<ActionResult<int>> GetNextEmployeeId()
        {
            try
            {
                var nextId = await _dataService.GetNextEmployeeIdAsync();
                return Ok(new { nextId = nextId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error getting next employee ID", error = ex.Message });
            }
        }

        [HttpPost("employee")]
        public async Task<ActionResult> AddEmployee([FromBody] EmployeeList employee)
        {
            try
            {
                var success = await _dataService.AddEmployeeAsync(employee);
                return success ? Ok(new { message = "Employee added successfully" }) : 
                    StatusCode(500, new { message = "Failed to add employee" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding employee", error = ex.Message });
            }
        }

        [HttpPut("employee")]
        public async Task<ActionResult> UpdateEmployee([FromBody] EmployeeList employee)
        {
            try
            {
                var success = await _dataService.UpdateEmployeeAsync(employee);
                return success ? Ok(new { message = "Employee updated successfully" }) : 
                    StatusCode(500, new { message = "Failed to update employee" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating employee", error = ex.Message });
            }
        }

        [HttpGet("employee/can-delete/{employeeId}")]
        public async Task<ActionResult> CanDeleteEmployee(string employeeId)
        {
            try
            {
                var canDelete = await _dataService.CanDeleteEmployeeAsync(employeeId);
                return Ok(new { canDelete = canDelete });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking employee deletion", error = ex.Message });
            }
        }

        [HttpDelete("employee/{guid}")]
        public async Task<ActionResult> DeleteEmployee(Guid guid)
        {
            try
            {
                var success = await _dataService.DeleteEmployeeAsync(guid);
                if (!success)
                {
                    return BadRequest(new { message = "Cannot delete employee with non-processed assigned records" });
                }
                return Ok(new { message = "Employee deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting employee", error = ex.Message });
            }
        }

        // ── Allowed Koncernnr_ endpoints ─────────────────────────────────────────

        [HttpGet("allowed-koncernnr")]
        public async Task<ActionResult> GetAllowedKoncernnr()
        {
            try
            {
                var list = await _dataService.GetAllowedKoncernnrAsync();
                var filterEnabled = await _dataService.GetKoncernnrFilterEnabledAsync();
                return Ok(new { filterEnabled, items = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving allowed koncernnr list", error = ex.Message });
            }
        }

        [HttpPost("allowed-koncernnr")]
        public async Task<ActionResult> AddAllowedKoncernnr([FromBody] AllowedKoncernnr item)
        {
            try
            {
                var user = HttpContext.Session.GetString("AdminUser") ?? "Admin";
                if (string.IsNullOrWhiteSpace(item.KoncernnrValue))
                    return BadRequest(new { message = "KoncernnrValue is required" });

                var success = await _dataService.AddAllowedKoncernnrAsync(item, user);
                return success ? Ok(new { message = "Koncernnr_ added successfully" })
                               : StatusCode(500, new { message = "Failed to add koncernnr_" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding koncernnr_", error = ex.Message });
            }
        }

        [HttpPut("allowed-koncernnr/{id}")]
        public async Task<ActionResult> UpdateAllowedKoncernnr(int id, [FromBody] AllowedKoncernnr item)
        {
            try
            {
                item.Id = id;
                var user = HttpContext.Session.GetString("AdminUser") ?? "Admin";
                var success = await _dataService.UpdateAllowedKoncernnrAsync(item, user);
                return success ? Ok(new { message = "Koncernnr_ updated successfully" })
                               : StatusCode(500, new { message = "Failed to update koncernnr_" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating koncernnr_", error = ex.Message });
            }
        }

        [HttpDelete("allowed-koncernnr/{id}")]
        public async Task<ActionResult> DeleteAllowedKoncernnr(int id)
        {
            try
            {
                var success = await _dataService.DeleteAllowedKoncernnrAsync(id);
                return success ? Ok(new { message = "Koncernnr_ removed successfully" })
                               : StatusCode(500, new { message = "Failed to remove koncernnr_" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error removing koncernnr_", error = ex.Message });
            }
        }

        [HttpPut("allowed-koncernnr/filter-enabled")]
        public async Task<ActionResult> SetKoncernnrFilterEnabled([FromBody] bool enabled)
        {
            try
            {
                var user = HttpContext.Session.GetString("AdminUser") ?? "Admin";
                var success = await _dataService.SetKoncernnrFilterEnabledAsync(enabled, user);
                return success ? Ok(new { message = $"Koncernnr_ filtering {(enabled ? "enabled" : "disabled")}" })
                               : StatusCode(500, new { message = "Failed to update filter setting" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating filter setting", error = ex.Message });
            }
        }
    }
}