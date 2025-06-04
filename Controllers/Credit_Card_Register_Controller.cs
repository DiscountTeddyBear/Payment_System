using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Payment_System_Web_App.Controllers;
using Payment_System_Web_Application_.Models;
using Payment_System_Web_Application_.Services;

namespace Payment_System_Web_Application_.Controllers
{
    /// <summary>
    /// Controller for managing credit card registration, editing, deletion, and related actions.
    /// </summary>
    public class Credit_Card_Register_Controller : Controller
    {
        private readonly Security_Service _securityService;

        /// <summary>
        /// Constructor that injects the security service.
        /// </summary>
        public Credit_Card_Register_Controller(Security_Service securityService)
        {
            _securityService = securityService;
        }

        /// <summary>
        /// Displays the list of credit cards for the logged-in user.
        /// Redirects to login if the user is not authenticated.
        /// </summary>
        public IActionResult Index()
        {
            if (!(_securityService.Is_Logged_In()))
            {
                return RedirectToAction("Login_Required_To_View_Content", "Home");
            }

            List<Credit_Card_Model> cards = _securityService.Get_Credit_Cards_For_User();
            return View("Index", cards);
        }

        /// <summary>
        /// Handles the registration of a new credit card.
        /// If registration fails, returns the registration view for correction.
        /// </summary>
        public IActionResult Register_Credit_Card(Credit_Card_Model card)
        {
            if (!(_securityService.Register_New_Credit_Card(card)))
            {
                return View("Credit_Card_Registration");
            }

            return Index();
        }

        /// <summary>
        /// Displays the credit card registration form.
        /// </summary>
        public IActionResult Credit_Card_Registration(Credit_Card_Model card)
        {
            return View();
        }

        /// <summary>
        /// Displays the edit form for a specific credit card.
        /// Redirects to login if the user is not authenticated.
        /// </summary>
        public IActionResult Edit_Credit_Card(int card_id)
        {
            if (!(_securityService.Is_Logged_In()))
            {
                return RedirectToAction("Login_Required_To_View_Content", "Home");
            }

            Credit_Card_Model card = _securityService.Get_Decrypted_Credit_Card_By_Id(card_id);
            return View("Edit_Credit_Card", card);
        }

        /// <summary>
        /// Handles the submission of edited credit card details.
        /// If editing fails, returns the edit view with an error message.
        /// </summary>
        public IActionResult Edit_Credit_Card_Action(Credit_Card_Model card)
        {
            if (!(_securityService.Edit_Existing_Credit_Card(card)))
            {
                ModelState.AddModelError("", "Invalid credit card details. Check to make sure your credit card information is correct.");
                return View("Edit_Credit_Card", card.Card_Id);
            }

            return Index();
        }

        /// <summary>
        /// Displays the transaction/order history for the logged-in user.
        /// Redirects to login if the user is not authenticated.
        /// </summary>
        public IActionResult History()
        {
            if (!(_securityService.Is_Logged_In()))
            {
                return RedirectToAction("Login_Required_To_View_Content", "Home");
            }
            // Retrieve the order history for the logged-in user
            List<Token_Model> orders = _securityService.Get_Order_History_For_User();
            return View(orders);
        }

        /// <summary>
        /// Sets the specified card as the default for the user.
        /// If the operation fails, returns the index view with an error message.
        /// Redirects to login if the user is not authenticated.
        /// </summary>
        public IActionResult Set_Card_As_Default(int card_id)
        {
            if (!(_securityService.Is_Logged_In()))
            {
                return RedirectToAction("Login_Required_To_View_Content", "Home");
            }
            if (!(_securityService.Set_Card_As_Default(card_id)))
            {
                // Handle the case where setting the card as default failed
                ModelState.AddModelError("", "Failed to set card as default.");
                return Index();
            }
            return Index();
        }

        /// <summary>
        /// Deletes the specified credit card.
        /// If the operation fails, returns the index view with an error message.
        /// Redirects to login if the user is not authenticated.
        /// </summary>
        public IActionResult Delete_Credit_Card(int card_id)
        {
            if (!(_securityService.Is_Logged_In()))
            {
                return RedirectToAction("Login_Required_To_View_Content", "Home");
            }

            if (!(_securityService.Delete_Credit_Card(card_id)))
            {
                // Handle the case where deleting the credit card failed
                ModelState.AddModelError("", "Failed to delete credit card.");
                return Index();
            }

            return Index();

        }
    }
}