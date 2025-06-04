CREATE DATABASE Payment_Information_Database;
GO
USE Payment_Information_Database;
GO

CREATE TABLE Credit_Cards (
    Card_Id INT PRIMARY KEY,
    Card_Number NVARCHAR(100) NOT NULL,
    Card_CVV NVARCHAR(100) NOT NULL,
    Card_Expiration_Month NVARCHAR(100) NOT NULL,
    Card_Expiration_Year NVARCHAR(100) NOT NULL,
    Card_First_Name NVARCHAR(100) NOT NULL,
    Card_Last_Name NVARCHAR(100) NOT NULL,
    Card_Last_Four_Digits NVARCHAR(4) NOT NULL,
    Card_Nickname NVARCHAR(100) NOT NULL,
    Is_Default BIT NOT NULL
);

CREATE TABLE User_Id_Credit_Card_Id_Relation (
    User_Id INT NOT NULL,
    Card_Id INT NOT NULL,
    PRIMARY KEY (User_Id, Card_Id)
);