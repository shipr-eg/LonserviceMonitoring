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
    }
}