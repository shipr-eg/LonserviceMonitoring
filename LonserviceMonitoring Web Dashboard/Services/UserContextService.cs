using System.Security.Principal;
using System.Net;

namespace LonserviceMonitoring.Services
{
    public class UserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetCurrentUserForAudit()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "System";

            // Get authenticated user from session (your custom auth)
            var sessionUser = context.Session.GetString("AdminUser");
            
            // Get system/Windows user information
            var systemUser = GetSystemUserInfo();
            
            // Get client information
            var clientInfo = GetClientInfo(context);
            
            // Combine all information for comprehensive auditing
            if (!string.IsNullOrEmpty(sessionUser))
            {
                return $"{sessionUser} ({systemUser}) [{clientInfo}]";
            }
            else
            {
                return $"Anonymous ({systemUser}) [{clientInfo}]";
            }
        }

        private string GetSystemUserInfo()
        {
            try
            {
                // Get Windows/System user
                var identity = WindowsIdentity.GetCurrent();
                var domainUser = identity?.Name ?? Environment.UserName;
                var machineName = Environment.MachineName;
                
                return $"{domainUser}@{machineName}";
            }
            catch
            {
                return $"{Environment.UserName}@{Environment.MachineName}";
            }
        }

        private string GetClientInfo(HttpContext context)
        {
            try
            {
                // Get client IP address
                var ipAddress = GetClientIpAddress(context);
                var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
                
                // Extract browser info from user agent
                var browserInfo = ExtractBrowserInfo(userAgent);
                
                return $"IP:{ipAddress}, {browserInfo}";
            }
            catch
            {
                return "Unknown Client";
            }
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded IP first (if behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check X-Real-IP header
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to remote IP address
            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string ExtractBrowserInfo(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown Browser";

            // Simple browser detection
            if (userAgent.Contains("Chrome")) return "Chrome";
            if (userAgent.Contains("Firefox")) return "Firefox";
            if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome")) return "Safari";
            if (userAgent.Contains("Edge")) return "Edge";
            if (userAgent.Contains("Opera")) return "Opera";

            return "Unknown Browser";
        }

        public string GetSimpleUser()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "System";

            var sessionUser = context.Session.GetString("AdminUser");
            if (!string.IsNullOrEmpty(sessionUser))
            {
                return sessionUser;
            }

            // Fallback to system user
            return Environment.UserName;
        }
    }
}