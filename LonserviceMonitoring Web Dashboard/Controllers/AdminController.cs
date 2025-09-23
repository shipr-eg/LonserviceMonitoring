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
    }
}