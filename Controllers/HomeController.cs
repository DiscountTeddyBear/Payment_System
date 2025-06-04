using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Payment_System_Web_Application_.Models;

namespace Payment_System_Web_Application_.Controllers
{
    /// <summary>
    /// Controller for handling general site navigation and status pages.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Constructor that injects the logger for this controller.
        /// </summary>
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Displays the home page.
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Displays the privacy policy page.
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Displays a generic success page.
        /// </summary>
        public IActionResult Success()
        {
            return View();
        }

        /// <summary>
        /// Displays a generic failure page.
        /// </summary>
        public IActionResult Failed()
        {
            return View();
        }

        /// <summary>
        /// Displays a page informing the user that login is required to view the requested content.
        /// </summary>
        public IActionResult Login_Required_To_View_Content()
        {
            return View();
        }

        /// <summary>
        /// Displays the error page with diagnostic information.
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
