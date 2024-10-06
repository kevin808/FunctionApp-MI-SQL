using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Azure.Identity;

namespace FunctionApp_MI_SQL
{
    public class SqlServerTimerTrigger
    {
        [FunctionName("SqlServerTimerTrigger")]
        public async Task RunAsync([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            string sqlServerName = Environment.GetEnvironmentVariable("SqlServerName");
            string databaseName = Environment.GetEnvironmentVariable("DatabaseName");
            string userAssignedClientId = Environment.GetEnvironmentVariable("UserAssignedClientId");

            try
            {
                // Get access token using User-Assigned Managed Identity
                string accessToken = await GetAccessTokenAsync(userAssignedClientId);

                // Create connection string using access token
                string connectionString = $"Data Source={sqlServerName};Initial Catalog={databaseName};Authentication=ActiveDirectoryMsi";

                // Connect to SQL Server and execute query
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.AccessToken = accessToken;
                    await connection.OpenAsync();

                    // Example query
                    string query = "SELECT COUNT(*) FROM dbo.users";
                    using (var command = new SqlCommand(query, connection))
                    {
                        int count = (int)await command.ExecuteScalarAsync();
                        log.LogInformation($"Number of records in YourTable: {count}");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error connecting to SQL Server: {ex.Message}");
            }
        }

        private async Task<string> GetAccessTokenAsync(string userAssignedClientId)
        {
            // Use DefaultAzureCredential with User-Assigned Managed Identity Client ID
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = userAssignedClientId
            });
            var token = await credential.GetTokenAsync(
                new Azure.Core.TokenRequestContext(new[] { "https://database.windows.net/.default" }));

            return token.Token;
        }
    }
}
