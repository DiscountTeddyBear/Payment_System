using System.Security.Claims;
using Payment_System_Web_Application_.Models;

namespace Payment_System_Web_Application_.Services
{
    /// <summary>
    /// Provides security-related operations such as authentication, user and card management, and token handling.
    /// Acts as a facade to coordinate between data access and business logic services.
    /// </summary>
    public class Security_Service
    {
        // Data access object for user accounts
        private readonly User_Accounts_DBO user_accounts_dbo;
        // Data access object for credit cards
        private readonly Credit_Card_DBO credit_card_dbo;
        // Service for handling token-related operations
        private readonly Token_Service token_service;
        // Used to access the current HTTP context (for retrieving user claims/cookies)
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes the Security_Service and its dependencies using the provided HTTP context accessor.
        /// </summary>
        public Security_Service(IHttpContextAccessor httpContextAccessor)
        {
            user_accounts_dbo = new User_Accounts_DBO(httpContextAccessor);
            credit_card_dbo = new Credit_Card_DBO(httpContextAccessor);
            token_service = new Token_Service(httpContextAccessor);
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Validates user credentials against the database.
        /// </summary>
        public bool Is_Valid(User_Model user)
        {
            return user_accounts_dbo.Find_User_By_Name_And_Password(user);
        }

        /// <summary>
        /// Registers a new user in the system.
        /// </summary>
        public bool Register_New_User(User_Model user)
        {
            return user_accounts_dbo.Store_New_User(user);
        }

        /// <summary>
        /// Registers a new credit card and associates it with the current user.
        /// </summary>
        public bool Register_New_Credit_Card(Credit_Card_Model card)
        {
            return credit_card_dbo.Store_New_Credit_Card(card) && credit_card_dbo.Associate_User_Id_With_Credit_Card_Id(card);
        }

        /// <summary>
        /// Checks if the current user is logged in by validating the authentication cookie.
        /// </summary>
        public bool Is_Logged_In()
        {
            return user_accounts_dbo.Find_User_By_Cookie();
        }

        /// <summary>
        /// Retrieves a user by username from the database.
        /// </summary>
        public User_Model GetUserByUsername(string username)
        {
            // Returns a User_Model object if found, otherwise null
            return user_accounts_dbo.GetUserByUsername(username);
        }

        /// <summary>
        /// Retrieves all credit cards associated with the currently logged-in user.
        /// </summary>
        public List<Credit_Card_Model> Get_Credit_Cards_For_User()
        {
            return credit_card_dbo.Get_Credit_Cards_For_User();
        }

        /// <summary>
        /// Retrieves the order history for the currently logged-in user.
        /// </summary>
        public List<Token_Model> Get_Order_History_For_User()
        {
            return user_accounts_dbo.Get_Order_History_For_User();
        }

        /// <summary>
        /// Retrieves all users with the "merchant" role.
        /// </summary>
        public List<User_Model> Get_Merchants()
        {
            return user_accounts_dbo.Get_Users_By_Role("merchant");
        }

        /// <summary>
        /// Gets the username of the currently logged-in user from the authentication cookie.
        /// </summary>
        public string Get_Username_From_Cookie()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;
        }

        /// <summary>
        /// Gets the user ID of the currently logged-in user from the authentication cookie.
        /// Returns -1 if not found or invalid.
        /// </summary>
        public int Get_User_Id_From_Cookie()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            // Return a default value or throw an exception if not found/invalid
            return -1; // or throw new InvalidOperationException("User ID claim not found or invalid.");
        }

        /// <summary>
        /// Gets the role of the currently logged-in user from the authentication cookie.
        /// Defaults to "user" if not found.
        /// </summary>
        public string Get_Role_From_Cookie()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? "user"; // Default to "user" if role not found
        }

        /// <summary>
        /// Creates a new token, encrypts it, decodes it, and stores it in the order history.
        /// </summary>
        /// <param name="token">The token model containing transaction details.</param>
        /// <returns>True if both token creation and storage succeed; otherwise, false.</returns>
        public bool Create_Token(Token_Model token)
        {
            bool task1 = token_service.Create_Token(token);
            if (!task1)
            {
                return false; // If token creation fails, return false
            }
            string encrpted_token = token_service.Create_JWE_Token(token);
            Console.WriteLine($"encrypted token: {encrpted_token}");
            Token_Model decoded_token = token_service.Decode_JWE_Token(encrpted_token);
            bool task2 = token_service.Store_Token_In_Order_History(decoded_token);

            return task1 && task2;
        }

        /// <summary>
        /// Sets the specified card as the default card for the current user.
        /// </summary>
        /// <param name="card_id">The unique identifier of the card to set as default.</param>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        public bool Set_Card_As_Default(int card_id)
        {
            return credit_card_dbo.Set_Card_As_Default(card_id);
        }

        /// <summary>
        /// Retrieves and decrypts a specific credit card by its ID for the current user.
        /// </summary>
        /// <param name="card_id">The unique identifier of the card to retrieve.</param>
        /// <returns>The decrypted <see cref="Credit_Card_Model"/> if found; otherwise, a default model.</returns>
        public Credit_Card_Model Get_Decrypted_Credit_Card_By_Id(int card_id)
        {
            return credit_card_dbo.Get_Decrypted_Credit_Card_By_Id(card_id);
        }

        /// <summary>
        /// Updates an existing credit card's details for the current user.
        /// </summary>
        /// <param name="card">The credit card model with updated details.</param>
        /// <returns>True if the update succeeds; otherwise, false.</returns>
        public bool Edit_Existing_Credit_Card(Credit_Card_Model card)
        {
            return credit_card_dbo.Edit_Existing_Credit_Card(card);
        }

        /// <summary>
        /// Deletes a credit card for the current user.
        /// </summary>
        /// <param name="card_id">The unique identifier of the card to delete.</param>
        /// <returns>True if the deletion succeeds; otherwise, false.</returns>
        public bool Delete_Credit_Card(int card_id)
        {
            return credit_card_dbo.Delete_Credit_Card(card_id);
        }
    }
}
