using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Payment_System_Web_Application_.Models
{
    /// <summary>
    /// Model representing a transaction token, used for order history and token-based operations.
    /// </summary>
    public class Token_Model
    {
        /// <summary>
        /// The unique identifier of the user associated with this token.
        /// </summary>
        public required int User_Id { get; set; }

        /// <summary>
        /// The unique identifier of the credit card used in the transaction.
        /// </summary>
        public required int Card_Id { get; set; }

        /// <summary>
        /// The unique identifier for this transaction.
        /// </summary>
        public required int Transaction_Id { get; set; }

        /// <summary>
        /// The amount of money involved in the transaction.
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "Money is required.")]
        [RegularExpression(@"^\d{1,9}$", ErrorMessage = "Money must be less than 10 digits.")]
        public required int Money { get; set; }

        /// <summary>
        /// The nickname of the card used, if provided. Nullable for cases where a nickname is not set.
        /// </summary>
        public required string? Card_Nickname { get; set; }

        /// <summary>
        /// The name of the merchant involved in the transaction.
        /// </summary>
        public required string Merchant_Name { get; set; }

        /// <summary>
        /// The UTC date and time when the token was issued.
        /// </summary>
        public DateTime IssuedAt { get; set; }

        /// <summary>
        /// The UTC date and time when the token expires.
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
}
