namespace Payment_System_Web_Application_.Models
{
    /// <summary>
    /// Model representing a user account, including credentials and role information.
    /// </summary>
    public class User_Model
    {
        /// <summary>
        /// Unique identifier for the user (primary key).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The username for login and identification. Must be unique.
        /// </summary>
        public required string Username { get; set; }

        /// <summary>
        /// The user's password. Stored as a salted and hashed string.
        /// </summary>
        public required string Password { get; set; }

        /// <summary>
        /// The user's role (e.g., "user", "merchant"). Defaults to "user".
        /// </summary>
        public required string Role { get; set; } = "user";
    }
}
