using Microsoft.AspNetCore.Mvc;
using Payment_System_Web_Application_.Services;
using Payment_System_Web_Application_.Models;

namespace Payment_System_Web_Application_.Controllers
{
    /// <summary>
    /// Controller responsible for token-related actions, such as displaying merchants and creating tokens.
    /// </summary>
    public class Token_Controller : Controller
    {
        private readonly Security_Service _securityService;

        /// <summary>
        /// Constructor that injects the security service.
        /// </summary>
        public Token_Controller(Security_Service securityService)
        {
            _securityService = securityService;
        }

        /// <summary>
        /// Displays the main token page, including the list of merchants.
        /// Redirects to login if the user is not authenticated.
        /// </summary>
        public IActionResult Index()
        {
            ViewData["Role"] = _securityService.Get_Role_From_Cookie();
            if (!(_securityService.Is_Logged_In()))
            {
                return RedirectToAction("Login_Required_To_View_Content", "Home");
            }

            // Retrieve the list of merchants for display
            var merchants = _securityService.Get_Merchants();
            ViewData["Merchants"] = merchants;

            return View("Index");
        }

        /// <summary>
        /// Handles the creation of a new token.
        /// If token creation fails, displays an error and reloads the index page.
        /// On success, redirects to the credit card history page.
        /// </summary>
        /// <param name="token">The token model containing transaction details.</param>
        public IActionResult Create_Token(Token_Model token)
        {
            if (!(_securityService.Create_Token(token)))
            {
                // Handle the case where setting the card as default failed
                ModelState.AddModelError("", "Failed to create token. Make sure a credit card is set as default.");
                return Index();
            }

            // Redirect to the order history page after successful token creation
            return RedirectToAction("History", "Credit_Card_Register_");
        }
    }
}
