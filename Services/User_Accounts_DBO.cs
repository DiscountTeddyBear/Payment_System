//using System.Data.SqlClient;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Payment_System_Web_App.Controllers;
using Payment_System_Web_Application_.Models;

namespace Payment_System_Web_Application_.Services
{
    /// <summary>
    /// Data access object for user account operations, including authentication, registration, and user queries.
    /// </summary>
    public class User_Accounts_DBO
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes the User_Accounts_DBO with the provided HTTP context accessor.
        /// </summary>
        public User_Accounts_DBO(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Connection string for the user account database, loaded from environment variable.
        string user_account_database_connection_string = Environment.GetEnvironmentVariable("USER_ACCOUNT_INFORMATION_DATABASE_CONNECTION_STRING") ?? throw new InvalidOperationException("User Account Information Database connection string not set");
        // See comments above for how to set this environment variable in different environments.

        /// <summary>
        /// Checks if the provided user's password matches the stored hash in the database.
        /// </summary>
        /// <param name="user">The user model with username and plain password.</param>
        /// <returns>True if the password is valid; otherwise, false.</returns>
        private bool Check_Hashed_Password(User_Model user)
        {
            // Retrieve the stored user from the database by username
            User_Model? stored_user = GetUserByUsername(user.Username);
            if (stored_user == null)
                return false; // User not found

            // Split the stored hash into salt and hash
            var parts = stored_user.Password.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid password format.");
            }
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] stored_hash = Convert.FromBase64String(parts[1]);

            // Derive the hash from the provided password using the same salt
            using var pbkdf2 = new Rfc2898DeriveBytes(user.Password, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] input_hash = pbkdf2.GetBytes(32);

            // Compare the derived hash with the stored hash
            if (Convert.ToBase64String(input_hash) == Convert.ToBase64String(stored_hash))
            {
                user.Password = stored_user.Password; // Store the hashed password in the user model
                return true;
            }
            else
            {
                return false; // Passwords do not match
            }
        }

        /// <summary>
        /// Finds a user by username and password (hashed) in the database.
        /// </summary>
        /// <param name="user">The user model with username and plain password.</param>
        /// <returns>True if the user is found and password matches; otherwise, false.</returns>
        public bool Find_User_By_Name_And_Password(User_Model user)
        {
            string sql_statement = "SELECT * FROM User_Accounts WHERE Username = @Username AND Password = @Password";

            if (!(Check_Hashed_Password(user)))
            {
                return false; // Password does not match
            }

            using (SqlConnection connection = new SqlConnection(user_account_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Username", user.Username);
                command.Parameters.AddWithValue("@Password", user.Password);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        return true; // User found
                    }
                    else
                    {
                        return false; // User not found
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return false; // Error occurred
                }
            }
        }

        /// <summary>
        /// Checks if a user ID is unique in the User_Accounts table.
        /// </summary>
        /// <param name="user_id">The user ID to check.</param>
        /// <returns>True if the user ID is unique; otherwise, false.</returns>
        public bool Is_Unique_User_Id(int user_id)
        {
            string sql_statement = "SELECT COUNT(*) FROM User_Accounts WHERE Id = @Id";
            using (SqlConnection connection = new SqlConnection(user_account_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Id", user_id);
                try
                {
                    connection.Open();
                    int count = (int)command.ExecuteScalar();
                    return !(count == 0); // Returns true if the user ID is unique
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return false; // Error occurred
                }
            }
        }

        /// <summary>
        /// Hashes a plain text password using PBKDF2 with SHA-256 and a random salt.
        /// </summary>
        /// <param name="password">The plain text password.</param>
        /// <returns>The hashed password in the format "salt:hash".</returns>
        private string Hash_Password(string password)
        {
            // Generate a 128-bit salt using a secure PRNG
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            // Derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            // Store both salt and hash (e.g., as base64)
            string salt_hash = Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);

            // Return as base64 string
            return salt_hash;
        }

        /// <summary>
        /// Stores a new user in the database after hashing the password and assigning a unique ID.
        /// </summary>
        /// <param name="user">The user model with registration details.</param>
        /// <returns>True if the user is successfully stored; otherwise, false.</returns>
        public bool Store_New_User(User_Model user)
        {
            // Check if the username already exists
            if (GetUserByUsername(user.Username) != null)
            {
                // Username is taken; inform the caller
                return false;
            }

            int user_id;
            string sql_statement = "INSERT INTO User_Accounts (Id, Username, Password, Role) VALUES (@Id, @Username, @Password, @Role)";

            // Generate a unique user ID
            do
            {
                user_id = new Random().Next(1, 1000000000);
            } while (Is_Unique_User_Id(user_id));

            user.Id = user_id; // Assign the unique user ID to the user model

            user.Password = Hash_Password(user.Password); // Hash the password before storing

            using (SqlConnection connection = new SqlConnection(user_account_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Id", user.Id);
                command.Parameters.AddWithValue("@Username", user.Username);
                command.Parameters.AddWithValue("@Password", user.Password);
                command.Parameters.AddWithValue("@Role", user.Role);

                try
                {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0; // Returns true if insert was successful
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return false; // Error occurred
                }
            }
        }

        /// <summary>
        /// Checks if a user exists in the database based on the authentication cookie (claims).
        /// </summary>
        /// <returns>True if the user is found; otherwise, false.</returns>
        public bool Find_User_By_Cookie()
        {
            string sql_statement = "SELECT * FROM User_Accounts WHERE Id = @Id AND Username = @Username";

            var username = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;
            var user_id = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Return false if the cookie (claims) doesn't exist
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(user_id))
            {
                return false;
            }

            using (SqlConnection connection = new SqlConnection(user_account_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Id", user_id);
                command.Parameters.AddWithValue("@Username", username);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        return true; // User found
                    }
                    else
                    {
                        return false; // User not found
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return false; // Error occurred
                }
            }
        }

        /// <summary>
        /// Retrieves a user by username from the database.
        /// </summary>
        /// <param name="username">The username to search for.</param>
        /// <returns>The <see cref="User_Model"/> if found; otherwise, null.</returns>
        public User_Model? GetUserByUsername(string username)
        {
            string sql_statement = "SELECT Id, Username, Password, Role FROM User_Accounts WHERE Username = @Username";

            using (SqlConnection connection = new SqlConnection(user_account_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Username", username);

                try
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User_Model
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Username = reader.GetString(reader.GetOrdinal("Username")),
                                Password = reader.GetString(reader.GetOrdinal("Password")),
                                Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? "user" : reader.GetString(reader.GetOrdinal("Role"))
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves the order history for the currently logged-in user.
        /// </summary>
        /// <returns>A list of <see cref="Token_Model"/> representing the user's order history.</returns>
        public List<Token_Model> Get_Order_History_For_User()
        {
            List<Token_Model> orders = new List<Token_Model>();
            string sql_statement = @"SELECT User_Id, Card_Id, Transaction_Id, Money, Date, Card_Nickname, Merchant_Name
                                         FROM Order_History
                                         WHERE User_Id = @User_Id";
            var user_id = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            using (SqlConnection connection = new SqlConnection(user_account_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@User_Id", user_id);

                try
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Token_Model order = new Token_Model
                            {
                                User_Id = reader.GetInt32(reader.GetOrdinal("User_Id")),
                                Card_Id = reader.GetInt32(reader.GetOrdinal("Card_Id")),
                                Transaction_Id = reader.GetInt32(reader.GetOrdinal("Transaction_Id")),
                                Money = reader.GetInt32(reader.GetOrdinal("Money")),
                                IssuedAt = reader.GetDateTime(reader.GetOrdinal("Date")),
                                ExpiresAt = reader.GetDateTime(reader.GetOrdinal("Date")).AddMinutes(30),
                                Card_Nickname = reader.IsDBNull(reader.GetOrdinal("Card_Nickname")) ? null : reader.GetString(reader.GetOrdinal("Card_Nickname")),
                                Merchant_Name = reader.GetString(reader.GetOrdinal("Merchant_Name"))
                            };
                            orders.Add(order);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return orders; // Return what was found so far, or an empty list
                }
            }

            return orders;
        }

        /// <summary>
        /// Retrieves all users with the specified role.
        /// </summary>
        /// <param name="role">The role to filter users by (e.g., "merchant").</param>
        /// <returns>A list of <see cref="User_Model"/> with the specified role.</returns>
        public List<User_Model> Get_Users_By_Role(string role)
        {
            var users = new List<User_Model>();
            using (var connection = new SqlConnection(user_account_database_connection_string))
            {
                string sql = "SELECT Id, Username, Password, Role FROM User_Accounts WHERE Role = @Role";
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Role", role);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User_Model
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Username = reader.GetString(reader.GetOrdinal("Username")),
                            Password = reader.GetString(reader.GetOrdinal("Password")),
                            Role = reader.GetString(reader.GetOrdinal("Role"))
                        });
                    }
                }
            }
            return users;
        }
    }
}
