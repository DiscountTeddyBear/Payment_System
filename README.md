# Payment System Web Application

A secure, modern payment system built with ASP.NET Core Razor Pages (.NET 8). This application allows users to register, log in, manage credit cards, and view transaction history. Merchants and admins have additional management capabilities.

> **DISCLAIMER:**  
> This is a sample project for educational purposes. It is not intended for production use and does not handle real payments.  
> **DO NOT ENTER REAL CREDIT CARD INFORMATION OR ANY OTHER SENSITIVE DATA.**  
> To make this web application secure enough for production, you must implement PCI DSS (Payment Card Industry Data Security Standard) requirements.  
> For more information, see the [official PCI DSS documentation (PDF)](https://docs-prv.pcisecuritystandards.org/PCI%20DSS/Standard/PCI-DSS-v4_0_1.pdf).

---

## Features

- User registration and authentication (with salted, hashed passwords)
- Credit card management (add, edit, delete, set default)
- Transaction/order history
- Role-based access (user, merchant, admin)
- Secure token generation and storage
- Razor Pages UI with responsive design

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)
- (Optional) [Visual Studio 2022](https://visualstudio.microsoft.com/) for development

---

## Getting Started

### 1. Clone the Repository

- Clone this repository to your local machine:
	- git clone https://github.com/DiscountTeddyBear/Payment_System.git
	- cd Payment_System

### 2. Set Up the Databases

- Open SQL Server Management Studio (SSMS) or Azure Data Studio.
- Run the scripts in the `Create_Database` folder:
  - `Create_User_Account_Information_Database.sql`
  - `Create_Payment_Information_Database.sql`
- These scripts will create the required databases and tables.

### 3. Configure Connection Strings and Encryption Keys

You should use environment variables to provide your connection strings and encryption keys, rather than storing them in `appsettings.json`. This is a best practice for security, especially if you plan to share or deploy your code.

#### How to Set Environment Variables

**On Windows (Command Prompt):**

- setx USER_ACCOUNT_INFORMATION_DATABASE_CONNECTION_STRING "Your_User_Account_DB_Connection_String_Here"
- setx PAYMENT_INFORMATION_DATABASE_CONNECTION_STRING "Your_Payment_Information_DB_Connection_String_Here"
- setx CARD_ENCRYPTION_KEY "Your32CharLongEncryptionKey!1234"

**On Windows (PowerShell):**

- $env:USER_ACCOUNT_INFORMATION_DATABASE_CONNECTION_STRING = "Your_User_Account_DB_Connection_String_Here" 
- $env:PAYMENT_INFORMATION_DATABASE_CONNECTION_STRING = "Your_Payment_Information_DB_Connection_String_Here"
- $env:CARD_ENCRYPTION_KEY = "Your32CharLongEncryptionKey!1234"

**On Linux/macOS (Bash):**

-export USER_ACCOUNT_INFORMATION_DATABASE_CONNECTION_STRING="Your_User_Account_DB_Connection_String_Here"
-export PAYMENT_INFORMATION_DATABASE_CONNECTION_STRING="Your_Payment_Information_DB_Connection_String_Here"
-export CARD_ENCRYPTION_KEY="Your32CharLongEncryptionKey!1234"

**Visual Studio**

- Right-click your project in Solution Explorer.
- Select "Properties".
- Go to the "Debug" tab.
- Under "Environment variables", add the following:
	- USER_ACCOUNT_INFORMATION_DATABASE_CONNECTION_STRING     Your_User_Account_DB_Connection_String_Here
	- PAYMENT_INFORMATION_DATABASE_CONNECTION_STRING     Your_Payment_Information_DB_Connection_String_Here
	- CARD_ENCRYPTION_KEY   Your32CharLongEncryptionKey!1234

> **Note:**  
> Replace "Your_User_Account_DB_Connection_String_Here" and "Your_Payment_Information_DB_Connection_String_Here" with your actual connection strings.
> Replace "Your32CharLongEncryptionKey!1234" with a secure, 32-character encryption key of your choice. This key is used to encrypt sensitive data like credit card information.

#### How the Application Uses Environment Variables

The application will automatically read these environment variables at runtime. You do not need to modify `appsettings.json` for connection strings.

**Best Practice:**  
Never commit real secrets or passwords to source control. Use environment variables for all sensitive configuration in development and production.

### 4. Restore and Build

- Restore NuGet packages and build the project:
	- dotnet restore 
	- dotnet build

### 5. Run the Application

- Start the application:
	- dotnet run
- The app will be available at `https://localhost:5001` (or the port shown in the console).

---

## Usage

- Register a new user or log in with an existing account.
- Add and manage credit cards.
- View your transaction history.
- Merchants can view and manage their own data.
- Admins can view all users and transactions.

---

## Database Schema

See the `Create_Database` folder for full SQL scripts.  
**Main tables:**
- `User_Accounts`: Stores user credentials and roles.
- `Order_History`: Stores transaction records.
- `Credit_Cards`: Stores credit card details.
- `User_Id_Credit_Card_Id_Relation`: Associates users with their cards.

---

## Security

- Passwords are salted and hashed using PBKDF2 with SHA-256.
- All database access uses parameterized queries.
- Sensitive data is never stored in source control.
- Role-based authorization is enforced throughout the app.

---

## Contributing

Contributions are welcome! Please open issues or submit pull requests.

---

## License

This project is licensed under the MIT License.

---

## Support

If you have questions or need help, please open an issue on GitHub.