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
                var isAdmin = HttpContext.Session.GetString("AdminUser") != null;
                if (!isAdmin)
                {
                    return Unauthorized(new { message = "Admin access required" });
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
    }
}