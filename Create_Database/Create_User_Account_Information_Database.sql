CREATE DATABASE User_Account_Information_Database;
GO
USE User_Account_Information_Database;
GO

CREATE TABLE User_Accounts (
    Id INT PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Password NVARCHAR(512) NOT NULL,
    Role NVARCHAR(50) NOT NULL
);

CREATE TABLE Order_History (
    Transaction_Id INT PRIMARY KEY,
    Money INT NOT NULL,
    User_Id INT NOT NULL,
    Card_Id INT NOT NULL,
    Date DATETIME NOT NULL,
    Card_Nickname NVARCHAR(100) NULL,
    Merchant_Name NVARCHAR(100) NOT NULL
);
