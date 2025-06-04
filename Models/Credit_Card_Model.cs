using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;

namespace Payment_System_Web_Application_.Models
{
    /// <summary>
    /// Model representing a credit card, including validation, encryption, and decryption logic.
    /// </summary>
    public class Credit_Card_Model
    {
        /// <summary>
        /// Unique identifier for the credit card.
        /// </summary>
        public required int Card_Id { get; set; }

        /// <summary>
        /// The full credit card number (16 digits). Encrypted before storage.
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "Card number is required.")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Card number must be 16 digits.")]
        public required string Card_Number { get; set; }

        /// <summary>
        /// The card's CVV (3 or 4 digits). Encrypted before storage.
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "CVV is required.")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits.")]
        public required string Card_CVV { get; set; }

        /// <summary>
        /// The expiration month (01-12). Encrypted before storage.
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "Expiration month is required.")]
        [RegularExpression(@"^(0[1-9]|1[0-2])$", ErrorMessage = "Expiration month must be 01-12.")]
        public required string Card_Expiration_Month { get; set; }

        /// <summary>
        /// The expiration year (2 digits). Encrypted before storage.
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "Expiration year is required.")]
        [RegularExpression(@"^\d{2}$", ErrorMessage = "Expiration year must be 2 digits.")]
        public required string Card_Expiration_Year { get; set; }

        /// <summary>
        /// Cardholder's first name. Encrypted before storage.
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "First name is required.")]
        public required string Card_First_Name { get; set; }

        /// <summary>
        /// Cardholder's last name. Encrypted before storage.
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "Last name is required.")]
        public required string Card_Last_Name { get; set; }

        /// <summary>
        /// Last four digits of the card number (for display purposes).
        /// </summary>
        [BindProperty]
        public required string Card_Last_Four_Digits { get; set; }

        /// <summary>
        /// User-defined nickname for the card.
        /// </summary>
        [BindProperty]
        [Required(ErrorMessage = "Nickname is required.")]
        public required string Card_Nickname { get; set; }

        /// <summary>
        /// Indicates if this card is the default card for the user.
        /// </summary>
        [BindProperty]
        public required bool Is_Default { get; set; } = false; // Default value set to false

        /// <summary>
        /// Optional message for UI feedback.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Flags for validation errors (used in UI logic).
        /// </summary>
        public bool Is_Card_Number_Invalid { get; set; }
        public bool Is_Card_CVV_Invalid { get; set; }
        public bool Is_Card_First_Name_Invalid { get; set; }
        public bool Is_Card_Last_Name_Invalid { get; set; }

        /// <summary>
        /// AES encryption key, loaded from the CARD_ENCRYPTION_KEY environment variable.
        /// Must be 32 characters for AES-256.
        /// </summary>
        private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes(
            Environment.GetEnvironmentVariable("CARD_ENCRYPTION_KEY") ?? throw new InvalidOperationException("Encryption key not set"));

        // To set the encryption key, use an environment variable named "CARD_ENCRYPTION_KEY" with a 32-character string.
        // Windows (Powershell): $env:CARD_ENCRYPTION_KEY = "Your32CharLongEncryptionKey!1234"
        // Linux/macOS: export CARD_ENCRYPTION_KEY="Your32CharLongEncryptionKey!1234"
        // In Visual Studio, set this in project debug settings under Environment variables using the following steps:
        // 1. Right-click on the project in Solution Explorer.
        // 2. Select "Properties".
        // 3. Go to the "Debug" tab.
        // 4. Under "Environment variables", add a new variable with the name "CARD_ENCRYPTION_KEY" and the value as your 32-character key.

        /// <summary>
        /// Encrypts a plain text string using AES-256 with a random IV.
        /// </summary>
        private string EncryptString(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = EncryptionKey;
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Combine IV and encrypted data
            var result = new byte[iv.Length + encryptedBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Encrypts all sensitive credit card fields in the model.
        /// </summary>
        /// <returns>True if encryption succeeds, false otherwise.</returns>
        public bool Encrypt_Credit_Card()
        {
            try
            {
                this.Card_Number = EncryptString(this.Card_Number);
                this.Card_CVV = EncryptString(this.Card_CVV);
                this.Card_Expiration_Month = EncryptString(this.Card_Expiration_Month);
                this.Card_Expiration_Year = EncryptString(this.Card_Expiration_Year);
                this.Card_First_Name = EncryptString(this.Card_First_Name);
                this.Card_Last_Name = EncryptString(this.Card_Last_Name);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Encryption failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Decrypts an AES-encrypted string using the stored key.
        /// </summary>
        private string DecryptString(string cipherText)
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            // Extract IV (first 16 bytes for AES)
            byte[] iv = new byte[16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);

            // Extract encrypted data
            int encryptedDataLength = fullCipher.Length - iv.Length;
            byte[] encryptedData = new byte[encryptedDataLength];
            Buffer.BlockCopy(fullCipher, iv.Length, encryptedData, 0, encryptedDataLength);

            using var aes = Aes.Create();
            aes.Key = EncryptionKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// Decrypts all sensitive credit card fields in the model.
        /// </summary>
        /// <returns>True if decryption succeeds, false otherwise.</returns>
        public bool Decrypt_Credit_Card()
        {
            try
            {
                this.Card_Number = DecryptString(this.Card_Number);
                this.Card_CVV = DecryptString(this.Card_CVV);
                this.Card_Expiration_Month = DecryptString(this.Card_Expiration_Month);
                this.Card_Expiration_Year = DecryptString(this.Card_Expiration_Year);
                this.Card_First_Name = DecryptString(this.Card_First_Name);
                this.Card_Last_Name = DecryptString(this.Card_Last_Name);
                return true; // Decryption successful
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption failed: {ex.Message}");
                return false; // Decryption failed
            }
        }
    }
}
