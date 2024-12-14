using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider;
using Azure.Identity;
using System.Threading.Tasks;
using DotNetEnv;

internal class Program
{
    private static void Main(string[] args)
    {
        // Load environment variables from .env file
        Env.Load();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // Add configuration to access environment variables
        builder.Configuration.AddEnvironmentVariables();

        // Register the Azure Key Vault provider once
        RegisterAzureKeyVaultProvider(builder.Configuration);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseAuthorization();

        app.MapRazorPages();
        app.MapGet("/", async context =>
        {
            var results = await AlwaysEncryptedTest(builder.Configuration);
            await context.Response.WriteAsJsonAsync(results);
        });

        app.Run();
    }

    private static void RegisterAzureKeyVaultProvider(IConfiguration configuration)
    {
        var clientSecretCredential = new ClientSecretCredential(
            tenantId: configuration["AZURE_TENANT_ID"], // Tenant ID
            clientId: configuration["AZURE_CLIENT_ID"], // Entra application Client ID
            clientSecret: configuration["AZURE_CLIENT_SECRET"]  // Entra application Client Secret
        );

        SqlColumnEncryptionAzureKeyVaultProvider azureKeyVaultProvider = new SqlColumnEncryptionAzureKeyVaultProvider(clientSecretCredential);
        SqlConnection.RegisterColumnEncryptionKeyStoreProviders(
            new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>(1, StringComparer.OrdinalIgnoreCase)
            {
                { SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, azureKeyVaultProvider }
            });
    }

    private static async Task<List<string>> AlwaysEncryptedTest(IConfiguration configuration)
    {
        var results = new List<string>();

        try
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = configuration["SQL_SERVER_NAME"], // Server FQDN
                InitialCatalog = configuration["SQL_DATABASE_NAME"], // Database name
                ColumnEncryptionSetting = SqlConnectionColumnEncryptionSetting.Enabled, // Enable Always Encrypted setting
                Authentication = SqlAuthenticationMethod.ActiveDirectoryServicePrincipal,
                UserID = configuration["AZURE_CLIENT_ID"], // Entra application Client ID
                Password = configuration["AZURE_CLIENT_SECRET"] // Entra application Client Secret
            };

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                await connection.OpenAsync();

                string sql = "select EmployeeID, SSN from [HR].[Employees]";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                int employeeId = reader.GetInt32(0);
                                string ssn = reader.GetString(1);

                                results.Add($"EmployeeID: {employeeId}, SSN: {ssn}");
                            }
                        }
                        else
                        {
                            results.Add("No data found.");
                        }
                    }
                }
            }
        }
        catch (SqlException e)
        {
            results.Add(e.ToString());
        }

        return results;
    }
}
