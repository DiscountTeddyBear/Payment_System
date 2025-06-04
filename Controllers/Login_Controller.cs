using Microsoft.AspNetCore.Mvc;
using Payment_System_Web_Application_.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Payment_System_Web_Application_.Services;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Payment_System_Web_App.Controllers
{
    /// <summary>
    /// Controller responsible for user login, registration, and authentication actions.
    /// </summary>
    public class Login_Controller : Controller
    {
        private readonly Security_Service _securityService;

        /// <summary>
        /// Constructor that injects the security service.
        /// </summary>
        public Login_Controller(Security_Service securityService)
        {
            _securityService = securityService;
        }

        /// <summary>
        /// Displays the login page.
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Displays the user registration page.
        /// </summary>
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Processes the login form submission.
        /// Validates user credentials, sets authentication cookie, and redirects to user account details.
        /// </summary>
        /// <param name="user">The user credentials submitted from the login form.</param>
        public async Task<IActionResult> Process_Login(User_Model user)
        {
            // Validate user credentials
            if (!(_securityService.Is_Valid(user)))
            {
                ModelState.AddModelError("", "Invalid account credentials. Check to make sure your credentials are correct.");
                return View("Index");
            }

            // Retrieve the user from the database to get the correct ID and role
            User_Model dbUser = _securityService.GetUserByUsername(user.Username);
            if (dbUser == null)
            {
                return View("Index");
            }

            // Create user claims for authentication
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, dbUser.Username),
                    new Claim(ClaimTypes.NameIdentifier, dbUser.Id.ToString()),
                    new Claim(ClaimTypes.Role, dbUser.Role.ToString())
                };

            var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Persistent cookie (remains after browser is closed)
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Cookie expiration
            };

            // Sign in the user with the created claims and authentication properties
            await HttpContext.SignInAsync(
                "MyCookieAuth",
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Redirect to the user account details page after successful login
            return RedirectToAction("Index", "User_Account_Details_");
        }

        /// <summary>
        /// Processes the user registration form submission.
        /// Registers a new user and logs them in if successful.
        /// </summary>
        /// <param name="user">The user registration data submitted from the form.</param>
        public async Task<IActionResult> Register_User(User_Model user)
        {
            // Attempt to register the new user
            if (!(_securityService.Register_New_User(user)))
            {
                ViewBag.ErrorMessage = "Username is already taken. Please choose another.";
                return View("Register");
            }

            // Automatically log in the user after successful registration
            return await Process_Login(user);
        }
    }
}
