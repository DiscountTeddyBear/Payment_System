using Microsoft.Data.SqlClient;
using Payment_System_Web_App.Controllers;
using Payment_System_Web_Application_.Models;
using System.Security.Claims;
using System.Text;
using Jose;
using System.Text;
using Payment_System_Web_Application_.Models;
using System.Security.Cryptography;

namespace Payment_System_Web_Application_.Services
{
    /// <summary>
    /// Service responsible for creating, encrypting, decoding, and storing transaction tokens.
    /// Handles token-related business logic and database operations.
    /// </summary>
    public class Token_Service
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Secret key for JWE encryption, generated at runtime (not persisted).
        string secretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); // Generates a 256-bit key

        /// <summary>
        /// Initializes the Token_Service with the provided HTTP context accessor.
        /// </summary>
        public Token_Service(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Connection strings loaded from environment variables.
        string user_account_database_connection_string = Environment.GetEnvironmentVariable("USER_ACCOUNT_INFORMATION_DATABASE_CONNECTION_STRING") ?? throw new InvalidOperationException("User Account Information Database connection string not set");
        string payment_inforamtion_database_connection_string = Environment.GetEnvironmentVariable("PAYMENT_INFORMATION_DATABASE_CONNECTION_STRING") ?? throw new InvalidOperationException("Payment Information Database connection string not set");
        // See comments above for how to set these environment variables in different environments.

        /// <summary>
        /// Checks if a transaction ID is unique in the Order_History table.
        /// </summary>
        private bool Is_Unique_Transaction_Id(int transaction_id)
        {
            string sql_statement = "SELECT COUNT(*) FROM Order_History WHERE Transaction_Id = @Transaction_Id";
            using (SqlConnection connection = new SqlConnection(user_account_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Transaction_Id", transaction_id);
                try
                {
                    connection.Open();
                    int count = (int)command.ExecuteScalar();
                    return !(count == 0); // Returns true if the transaction ID is unique
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return false; // Error occurred
                }
            }
        }

        /// <summary>
        /// Checks if the user has a default credit card set.
        /// </summary>
        private bool Check_If_Default_Card_Exists(int user_id)
        {
            string sql_statement = "SELECT COUNT(*) FROM Credit_Cards cc " +
                                   "JOIN User_Id_Credit_Card_Id_Relation uccr ON cc.Card_Id = uccr.Card_Id " +
                                   "WHERE cc.Is_Default = 1 AND uccr.User_Id = @User_Id";
            using (SqlConnection connection = new SqlConnection(payment_inforamtion_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@User_Id", user_id);
                try
                {
                    connection.Open();
                    int count = (int)command.ExecuteScalar();
                    return count > 0; // Returns true if a default card exists for the user
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return false; // Error occurred
                }
            }
        }

        /// <summary>
        /// Retrieves the default card's ID and nickname for the user and populates the token model.
        /// </summary>
        private bool Get_Default_Card_Information(Token_Model token)
        {
            string sql_statement = "SELECT cc.Card_Id, cc.Card_Nickname " +
                                  "FROM Credit_Cards cc " +
                                  "JOIN User_Id_Credit_Card_Id_Relation uccr ON cc.Card_Id = uccr.Card_Id " +
                                  "WHERE cc.Is_Default = 1 AND uccr.User_Id = @User_Id";
            using (SqlConnection connection = new SqlConnection(payment_inforamtion_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@User_Id", token.User_Id);
                try
                {
                    connection.Open();
                    if (!(Check_If_Default_Card_Exists(token.User_Id)))
                    {
                        return false; // No default card found for the user
                    }
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        reader.Read(); // Read the first row of the result set
                        token.Card_Id = reader.GetInt32(reader.GetOrdinal("Card_Id")); // Get the default card ID
                        token.Card_Nickname = reader.GetString(reader.GetOrdinal("Card_Nickname")); // Get the card nickname
                    }
                    return true; // Successfully retrieved the default card ID
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return false; // Error occurred or no default card found
                }
            }
        }

        /// <summary>
        /// Creates a new token for the current user, assigns a unique transaction ID, and sets issued/expiration times.
        /// Also populates the token with the user's default card information.
        /// </summary>
        /// <param name="token">The token model to populate.</param>
        /// <returns>True if the token is successfully created and populated; otherwise, false.</returns>
        public bool Create_Token(Token_Model token)
        {
            int transaction_id;
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!(int.TryParse(userIdClaim, out int userId)))
            {
                token.User_Id = -1; // Return a default value or throw an exception if not found/invalid
                return false; // User ID not found or invalid
            }
            token.User_Id = userId;
            token.IssuedAt = DateTime.UtcNow;
            token.ExpiresAt = token.IssuedAt.AddMinutes(30); // Set expiration time to 30 minutes from now

            if (!(Get_Default_Card_Information(token)))
            {
                return false; // Error occurred or no default card found
            }
            // Generate a unique transaction ID
            do
            {
                transaction_id = new Random().Next(1, 1000000000); // Generate a random user ID
            } while (Is_Unique_Transaction_Id(transaction_id));

            token.Transaction_Id = transaction_id; // Assign the unique transaction ID to the token model

            return true;
        }

        /// <summary>
        /// Stores the token in the Order_History table in the database.
        /// </summary>
        /// <param name="token">The token model to store.</param>
        /// <returns>True if the insert was successful; otherwise, false.</returns>
        public bool Store_Token_In_Order_History(Token_Model token)
        {
            string sql_statement = "INSERT INTO Order_History (Transaction_Id, Money, User_Id, Card_Id, Date, Card_Nickname, Merchant_Name) " +
                "VALUES (@Transaction_Id, @Money, @User_Id, @Card_Id, @Date, @Card_Nickname, @Merchant_Name)";

            using (SqlConnection connection = new SqlConnection(user_account_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Transaction_Id", token.Transaction_Id);
                command.Parameters.AddWithValue("@Money", token.Money);
                command.Parameters.AddWithValue("@User_Id", token.User_Id);
                command.Parameters.AddWithValue("@Card_Id", token.Card_Id);
                command.Parameters.AddWithValue("@Date", token.IssuedAt);
                command.Parameters.AddWithValue("@Card_Nickname", token.Card_Nickname);
                command.Parameters.AddWithValue("@Merchant_Name", token.Merchant_Name);

                try
                {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0; // Returns true if insert was successful
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred when storing token: {ex.Message}");
                    return false; // Error occurred
                }
            }
        }

        /// <summary>
        /// Creates a JWE (JSON Web Encryption) token from the provided token model.
        /// </summary>
        /// <param name="token">The token model to serialize and encrypt.</param>
        /// <returns>The encrypted JWE token as a string.</returns>
        public string Create_JWE_Token(Token_Model token)
        {
            // Serialize the token model to JSON
            var payload = System.Text.Json.JsonSerializer.Serialize(token);

            // Convert the secret key to bytes
            var key = Convert.FromBase64String(secretKey);

            // Encrypt the payload as a JWE token (using direct symmetric encryption with AES)
            string jwe = JWT.Encode(
                payload,
                key,
                JweAlgorithm.DIR,
                JweEncryption.A256GCM
            );

            return jwe;
        }

        /// <summary>
        /// Decodes and decrypts a JWE token string back into a Token_Model.
        /// </summary>
        /// <param name="jwe">The encrypted JWE token string.</param>
        /// <returns>The deserialized <see cref="Token_Model"/> if successful; otherwise, null.</returns>
        public Token_Model? Decode_JWE_Token(string jwe)
        {
            var key = Convert.FromBase64String(secretKey);
            string json = JWT.Decode(jwe, key, JweAlgorithm.DIR, JweEncryption.A256GCM);
            return System.Text.Json.JsonSerializer.Deserialize<Token_Model>(json);
        }
    }

}

