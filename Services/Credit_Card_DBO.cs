using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Microsoft.Data.SqlClient;
using Payment_System_Web_App.Controllers;
using Payment_System_Web_Application_.Models;
using System.Security.Cryptography;


namespace Payment_System_Web_Application_.Services
{
    /// <summary>
    /// Data access object for credit card operations, including storage, retrieval, update, and deletion.
    /// </summary>
    public class Credit_Card_DBO
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Connection string for the payment information database, loaded from environment variable.
        // See comments below for how to set this variable in different environments.
        string payment_inforamtion_database_connection_string = Environment.GetEnvironmentVariable("PAYMENT_INFORMATION_DATABASE_CONNECTION_STRING") ?? throw new InvalidOperationException("Payment Information Database connection string not set");
        // To set the database connection string, use an environment variable named "PAYMENT_INFORMATION_DATABASE_CONNECTION_STRING".
        // Windows (Powershell): $env:PAYMENT_INFORMATION_DATABASE_CONNECTION_STRING = "YourConnectionString"
        // Linux/macOS: export PAYMENT_INFORMATION_DATABASE_CONNECTION_STRING="YourConnectionString"
        // In Visual Studio, set this in project debug settings under Environment variables using the following steps:
        // 1. Right-click on the project in Solution Explorer.
        // 2. Select "Properties".
        // 3. Go to the "Debug" tab.
        // 4. Under "Environment variables", add a new variable with the name "PAYMENT_INFORMATION_DATABASE_CONNECTION_STRING" and the value as your connection string.

        /// <summary>
        /// Constructor that injects the HTTP context accessor.
        /// </summary>
        public Credit_Card_DBO(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Retrieves the user ID from the authentication cookie.
        /// </summary>
        private int Get_User_Id_From_Cookie()
        {
            var user_id = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            return user_id != null ? int.Parse(user_id.Value) : 0; // Return 0 if no user ID found
        }

        /// <summary>
        /// Extracts the last four digits from a card number string.
        /// </summary>
        private string Get_Last_Four_Digits_From_Card_Number(string card_number)
        {
            if (card_number.Length < 4)
            {
                throw new ArgumentException("Card number must be at least 4 digits long.");
            }
            return card_number.Substring(card_number.Length - 4);
        }

        /// <summary>
        /// Checks if a card ID is unique in the database.
        /// </summary>
        private bool Is_Unique_Card_Id(int card_id)
        {
            string sql_statement = "SELECT COUNT(*) FROM Credit_Cards WHERE Card_Id = @Card_Id";
            using (SqlConnection connection = new SqlConnection(payment_inforamtion_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Card_Id", card_id);
                try
                {
                    connection.Open();
                    int count = (int)command.ExecuteScalar();
                    return !(count == 0); // Returns true if the card ID is unique
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return false; // Error occurred
                }
            }
        }

        /// <summary>
        /// Stores a new credit card in the database after encrypting its details.
        /// </summary>
        public bool Store_New_Credit_Card(Credit_Card_Model card)
        {
            int card_id;
            string sql_statement = @"INSERT INTO Credit_Cards   
                       (Card_Id, Card_Number, Card_CVV, Card_Expiration_Month, Card_Expiration_Year, Card_First_Name, Card_Last_Name, Card_Last_Four_Digits, Card_Nickname)   
                       VALUES (@Card_Id, @Card_Number, @Card_CVV, @Card_Expiration_Month, @Card_Expiration_Year, @Card_First_Name, @Card_Last_Name, @Card_Last_Four_Digits, @Card_Nickname)";

            // Generate a unique card ID
            do
            {
                card_id = new Random().Next(1, 1000000000);
            } while (Is_Unique_Card_Id(card_id));

            card.Card_Id = card_id;
            card.Card_Last_Four_Digits = Get_Last_Four_Digits_From_Card_Number(card.Card_Number);

            // Encrypt card details before storing
            if (!(card.Encrypt_Credit_Card()))
            {
                return false; // Encryption failed
            }

            using (SqlConnection connection = new SqlConnection(payment_inforamtion_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Card_Id", card.Card_Id);
                command.Parameters.AddWithValue("@Card_Number", card.Card_Number);
                command.Parameters.AddWithValue("@Card_CVV", card.Card_CVV);
                command.Parameters.AddWithValue("@Card_Expiration_Month", card.Card_Expiration_Month);
                command.Parameters.AddWithValue("@Card_Expiration_Year", card.Card_Expiration_Year);
                command.Parameters.AddWithValue("@Card_First_Name", card.Card_First_Name);
                command.Parameters.AddWithValue("@Card_Last_Name", card.Card_Last_Name);
                command.Parameters.AddWithValue("@Card_Last_Four_Digits", card.Card_Last_Four_Digits);
                command.Parameters.AddWithValue("@Card_Nickname", card.Card_Nickname);

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
        /// Associates a user ID with a credit card ID in the relation table.
        /// </summary>
        public bool Associate_User_Id_With_Credit_Card_Id(Credit_Card_Model card)
        {
            string sql_statement = @"INSERT INTO User_Id_Credit_Card_Id_Relation   
                       (Card_Id, User_Id)   
                       VALUES (@Card_Id, @User_Id)";

            int user_id = Get_User_Id_From_Cookie();
            if (user_id == 0)
            {
                Console.WriteLine("User ID not found in cookie.");
                return false;
            }

            using (SqlConnection connection = new SqlConnection(payment_inforamtion_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Card_Id", card.Card_Id);
                command.Parameters.AddWithValue("@User_Id", user_id);

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
        /// Retrieves all credit cards associated with the currently logged-in user.
        /// Only non-sensitive fields are returned.
        /// </summary>
        public List<Credit_Card_Model> Get_Credit_Cards_For_User()
        {
            List<Credit_Card_Model> cards = new List<Credit_Card_Model>();

            string sql_statement = @"SELECT cc.Card_Id AS Card_Id, cc.Card_Last_Four_Digits, cc.Card_Nickname, cc.Is_Default 
                                         FROM Credit_Cards cc
                                         JOIN User_Id_Credit_Card_Id_Relation uccr ON cc.Card_Id = uccr.Card_Id
                                         WHERE uccr.User_Id = @User_Id";

            int user_id = Get_User_Id_From_Cookie();
            if (user_id == 0)
            {
                Console.WriteLine("User ID not found in cookie.");
                return cards; // Return empty list if user ID is not found
            }

            using (SqlConnection connection = new SqlConnection(payment_inforamtion_database_connection_string))
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
                            Credit_Card_Model card = new Credit_Card_Model
                            {
                                Card_Id = reader.GetInt32(reader.GetOrdinal("Card_Id")),
                                Card_Number = "0",
                                Card_CVV = "0",
                                Card_Expiration_Month = "0",
                                Card_Expiration_Year = "0",
                                Card_First_Name = "0",
                                Card_Last_Name = "0",
                                Card_Last_Four_Digits = reader.GetString(reader.GetOrdinal("Card_Last_Four_Digits")),
                                Card_Nickname = reader.GetString(reader.GetOrdinal("Card_Nickname")),
                                Is_Default = reader.GetBoolean(reader.GetOrdinal("Is_Default"))
                            };

                            cards.Add(card);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return cards; // Error occurred
                }
            }

            return cards;
        }

        /// <summary>
        /// Sets the specified card as the default card for the current user.
        /// </summary>
        public bool Set_Card_As_Default(int card_id)
        {
            string sql_statement = @"
                    UPDATE cc
                    SET cc.Is_Default = 0
                    FROM Credit_Cards cc
                    INNER JOIN User_Id_Credit_Card_Id_Relation uccr ON cc.Card_Id = uccr.Card_Id
                    WHERE uccr.User_Id = @User_Id;

                    UPDATE cc
                    SET cc.Is_Default = 1
                    FROM Credit_Cards cc
                    INNER JOIN User_Id_Credit_Card_Id_Relation uccr ON cc.Card_Id = uccr.Card_Id
                    WHERE uccr.User_Id = @User_Id AND cc.Card_Id = @Card_Id;
                ";

            int user_id = Get_User_Id_From_Cookie();
            if (user_id == 0)
            {
                Console.WriteLine("User ID not found in cookie.");
                return false;
            }

            using (SqlConnection connection = new SqlConnection(payment_inforamtion_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Card_Id", card_id);
                command.Parameters.AddWithValue("@User_Id", user_id);
                try
                {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0; // Returns true if update was successful  
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return false; // Error occurred  
                }
            }
        }

        /// <summary>
        /// Retrieves and decrypts a specific credit card by its ID for the current user.
        /// </summary>
        public Credit_Card_Model Get_Decrypted_Credit_Card_By_Id(int card_id)
        {
            Credit_Card_Model card = new Credit_Card_Model
            {
                Card_Id = 0,
                Card_Number = string.Empty,
                Card_CVV = string.Empty,
                Card_Expiration_Month = string.Empty,
                Card_Expiration_Year = string.Empty,
                Card_First_Name = string.Empty,
                Card_Last_Name = string.Empty,
                Card_Last_Four_Digits = string.Empty,
                Card_Nickname = string.Empty,
                Is_Default = false
            };

            string sql_statement = @"SELECT cc.Card_Id, cc.Card_Number, cc.Card_CVV, cc.Card_Expiration_Month, cc.Card_Expiration_Year,  
                                               cc.Card_First_Name, cc.Card_Last_Name, cc.Card_Last_Four_Digits, cc.Card_Nickname, cc.Is_Default 
                                        FROM Credit_Cards cc
                                         JOIN User_Id_Credit_Card_Id_Relation uccr ON cc.Card_Id = uccr.Card_Id
                                         WHERE uccr.User_Id = @User_Id AND cc.Card_Id = @Card_Id";
            int user_id = Get_User_Id_From_Cookie();
            if (user_id == 0)
            {
                Console.WriteLine("User ID not found in cookie.");
                return card;
            }

            using (SqlConnection connection = new SqlConnection(payment_inforamtion_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Card_Id", card_id);
                command.Parameters.AddWithValue("@User_Id", user_id);
                try
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            card.Card_Id = reader.GetInt32(reader.GetOrdinal("Card_Id"));
                            card.Card_Number = reader.GetString(reader.GetOrdinal("Card_Number"));
                            card.Card_CVV = reader.GetString(reader.GetOrdinal("Card_CVV"));
                            card.Card_Expiration_Month = reader.GetString(reader.GetOrdinal("Card_Expiration_Month"));
                            card.Card_Expiration_Year = reader.GetString(reader.GetOrdinal("Card_Expiration_Year"));
                            card.Card_First_Name = reader.GetString(reader.GetOrdinal("Card_First_Name"));
                            card.Card_Last_Name = reader.GetString(reader.GetOrdinal("Card_Last_Name"));
                            card.Card_Last_Four_Digits = reader.GetString(reader.GetOrdinal("Card_Last_Four_Digits"));
                            card.Card_Nickname = reader.GetString(reader.GetOrdinal("Card_Nickname"));
                            card.Is_Default = reader.GetBoolean(reader.GetOrdinal("Is_Default"));
                            if (!card.Decrypt_Credit_Card())
                            {
                                Console.WriteLine("Decryption failed.");
                                return card;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return card;
                }
            }
            return card;
        }

        /// <summary>
        /// Updates an existing credit card's details for the current user.
        /// </summary>
        public bool Edit_Existing_Credit_Card(Credit_Card_Model card)
        {
            card.Card_Last_Four_Digits = Get_Last_Four_Digits_From_Card_Number(card.Card_Number);
            int user_id = Get_User_Id_From_Cookie();
            if (user_id == 0)
            {
                Console.WriteLine("User ID not found in cookie.");
                return false;
            }

            if (!(card.Encrypt_Credit_Card()))
            {
                return false; // Encryption failed
            }

            string sql_statement = @"UPDATE cc 
                                         SET cc.Card_Number = @Card_Number, 
                                             cc.Card_CVV = @Card_CVV, 
                                             cc.Card_Expiration_Month = @Card_Expiration_Month, 
                                             cc.Card_Expiration_Year = @Card_Expiration_Year, 
                                             cc.Card_First_Name = @Card_First_Name, 
                                             cc.Card_Last_Name = @Card_Last_Name, 
                                             cc.Card_Last_Four_Digits = @Card_Last_Four_Digits, 
                                             cc.Card_Nickname = @Card_Nickname 
                                         FROM Credit_Cards cc
                                         INNER JOIN User_Id_Credit_Card_Id_Relation uccr ON cc.Card_Id = uccr.Card_Id
                                         WHERE uccr.User_Id = @User_Id AND cc.Card_Id = @Card_Id";
            using (SqlConnection connection = new SqlConnection(payment_inforamtion_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Card_Id", card.Card_Id);
                command.Parameters.AddWithValue("@Card_Number", card.Card_Number);
                command.Parameters.AddWithValue("@Card_CVV", card.Card_CVV);
                command.Parameters.AddWithValue("@Card_Expiration_Month", card.Card_Expiration_Month);
                command.Parameters.AddWithValue("@Card_Expiration_Year", card.Card_Expiration_Year);
                command.Parameters.AddWithValue("@Card_First_Name", card.Card_First_Name);
                command.Parameters.AddWithValue("@Card_Last_Name", card.Card_Last_Name);
                command.Parameters.AddWithValue("@Card_Last_Four_Digits", card.Card_Last_Four_Digits);
                command.Parameters.AddWithValue("@Card_Nickname", card.Card_Nickname);
                command.Parameters.AddWithValue("@User_Id", user_id);
                try
                {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0; // Returns true if update was successful  
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return false; // Error occurred  
                }
            }
        }

        /// <summary>
        /// Deletes a credit card for the current user.
        /// </summary>
        public bool Delete_Credit_Card(int card_id)
        {
            string sql_statement = @"DELETE FROM Credit_Cards 
                                         WHERE Card_Id = @Card_Id AND Card_Id IN 
                                         (SELECT Card_Id FROM User_Id_Credit_Card_Id_Relation WHERE User_Id = @User_Id)";
            int user_id = Get_User_Id_From_Cookie();
            if (user_id == 0)
            {
                Console.WriteLine("User ID not found in cookie.");
                return false;
            }
            using (SqlConnection connection = new SqlConnection(payment_inforamtion_database_connection_string))
            {
                SqlCommand command = new SqlCommand(sql_statement, connection);
                command.Parameters.AddWithValue("@Card_Id", card_id);
                command.Parameters.AddWithValue("@User_Id", user_id);
                try
                {
                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0; // Returns true if delete was successful  
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return false; // Error occurred  
                }
            }
        }
    }
}
