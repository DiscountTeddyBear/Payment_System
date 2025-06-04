using Microsoft.AspNetCore.Mvc;
using Payment_System_Web_Application_.Models;
using Payment_System_Web_Application_.Services;

namespace Payment_System_Web_Application_.Controllers
{
    /// <summary>
    /// Controller responsible for displaying user account details.
    /// </summary>
    public class User_Account_Details_Controller : Controller
    {
        private readonly Security_Service _securityService;

        /// <summary>
        /// Constructor that injects the security service.
        /// </summary>
        public User_Account_Details_Controller(Security_Service securityService)
        {
            _securityService = securityService;
        }

        /// <summary>
        /// Displays the user account details page if the user is logged in.
        /// Otherwise, redirects to the login required page.
        /// </summary>
        public IActionResult Index()
        {
            if (_securityService.Is_Logged_In())
            {
                ViewData["Username"] = _securityService.Get_Username_From_Cookie();
                ViewData["Role"] = _securityService.Get_Role_From_Cookie();
                return View();
            }
            else
            {
                return RedirectToAction("Login_Required_To_View_Content", "Home");
            }
        }
    }
}
