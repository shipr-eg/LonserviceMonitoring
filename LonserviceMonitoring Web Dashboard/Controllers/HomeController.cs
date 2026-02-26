using Microsoft.AspNetCore.Mvc;
using LonserviceMonitoring.Services;

namespace LonserviceMonitoring.Controllers
{
    public class HomeController : Controller
    {
        private readonly DataService _dataService;

        public HomeController(DataService dataService)
        {
            _dataService = dataService;
        }

        public IActionResult Index()
        {
            // Check if user is logged in
            var employeeInitials = HttpContext.Session.GetString("EmployeeInitials");
            
            if (string.IsNullOrEmpty(employeeInitials))
            {
                // Redirect to login if no session
                return RedirectToAction("Login");
            }
            
            // Pass employee info to view
            ViewBag.EmployeeInitials = employeeInitials;
            ViewBag.EmployeeName = HttpContext.Session.GetString("EmployeeName");
            
            return View();
        }

        public IActionResult Login()
        {
            // If already logged in, redirect to dashboard
            var employeeInitials = HttpContext.Session.GetString("EmployeeInitials");
            
            if (!string.IsNullOrEmpty(employeeInitials))
            {
                return RedirectToAction("Index");
            }
            
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult Admin()
        {
            return View();
        }
    }
}