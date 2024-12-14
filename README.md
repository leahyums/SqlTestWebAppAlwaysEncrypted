# Project Setup Guide

This is a test project created to demonstrate the use of Azure SQL Database Always Encrypted feature in a .NET Core web application. 
Please note that this project is intended for proof of concept (PoC) purposes and is not suitable for production use.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Database Setup](#database-setup)
- [Entra Application Setup](#entra-application-setup)
- [Database User Setup](#database-user-setup)
- [Azure Key Vault Access Policy](#azure-key-vault-access-policy)
- [Running the Project](#running-the-project)
- [Publish to Azure App Service](#publish-to-azure-app-service)

## Prerequisites

- .NET 8 SDK
- Azure SQL Database
- Azure Subscription

## Database Setup and always encrypted

1. Connect to your SQL Database.
2. Execute the following SQL script to create the schema and table:

```sql
CREATE SCHEMA [HR];
GO

CREATE TABLE [HR].[Employees]
(
    [EmployeeID] [int] IDENTITY(1,1) NOT NULL
    , [SSN] [char](11) NOT NULL
    , [FirstName] [nvarchar](50) NOT NULL
    , [LastName] [nvarchar](50) NOT NULL
    , [Salary] [money] NOT NULL
) ON [PRIMARY];
```

3. Follow [Tutorial: Getting started with Always Encrypted](https://learn.microsoft.com/en-us/sql/relational-databases/security/encryption/always-encrypted-tutorial-getting-started?view=sql-server-ver16&tabs=ssms) to encrypt [SSN] column.

## Entra Application Setup

1. Go to the Azure portal and navigate to "Microsoft Entra".
2. Select "App registrations" and click "New registration".
3. Enter a name for the application and click "Register".
4. Note the "Application (client) ID" and "Directory (tenant) ID".
5. Go to "Certificates & secrets" and create a new client secret. Note the value of the client secret.

Please see [Quickstart: Register an application with the Microsoft identity platform](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app?tabs=certificate) for more details.

## Database User Setup

1. Connect to your SQL Server instance.
2. Execute the following SQL script to create a database user for the service principal:

```sql
CREATE USER [your-entra-app-name] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [your-entra-app-name];
ALTER ROLE db_datawriter ADD MEMBER [your-entra-app-name];
```

## Azure Key Vault Access

1. Go to the Azure portal and navigate to your Key Vault.
2. Select "Access policies" and click "Add Access Policy".
3. Select the appropriate permissions for your application.
4. Under "Select principal", search for your Entra application and select it.
5. Click "Add" and then "Save".

## Running the Project

1. Open the project in Visual Studio 2022.
2. Ensure the `.env` file is created and populated with the necessary environment variables.

Create a `.env` file in the root of your project directory with the following content:

```plaintext
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret
SQL_SERVER_NAME=your-server-name.database.windows.net
SQL_DATABASE_NAME=your-database-name
```
Replace the placeholders with your actual values.

3. Build and run the project.

The application should now be running and accessible. It will connect to the SQL Server, retrieve data from the `HR.Employees` table, and use Azure Key Vault for encryption key management.

---

## Publish to Azure App Service

1. In the Solution Explorer, right-click project and select Publish.
2. Select Azure as your target and click Next.
3. Select Azure App Service (Windows) and click Next to create new Azure App Service resources.
4. Publish the application to Azure App Service.


----

*This README was drafted by GitHub Copilot.*
