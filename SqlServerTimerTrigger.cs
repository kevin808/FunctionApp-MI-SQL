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

            string SqlConnectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            string userAssignedClientId = Environment.GetEnvironmentVariable("UserAssignedClientId");

            try
            {
                // Get access token using User-Assigned Managed Identity
                string accessToken = await GetAccessTokenAsync(userAssignedClientId);

                // Create connection string using access token
                string connectionString = SqlConnectionString;

                // Connect to SQL Server and execute queries
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.AccessToken = accessToken;
                    await connection.OpenAsync();

                    // Create table if it doesn't exist
                    string createTableQuery = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='newusers' AND xtype='U')
                    BEGIN
                        CREATE TABLE newusers (
                            id INT PRIMARY KEY IDENTITY(1,1),
                            username VARCHAR(50) NOT NULL,
                            email VARCHAR(100) NOT NULL,
                            created_at DATETIME DEFAULT GETDATE()
                        );
                    END";

                    using (var createTableCommand = new SqlCommand(createTableQuery, connection))
                    {
                        await createTableCommand.ExecuteNonQueryAsync();
                        log.LogInformation("Table 'newusers' created or already exists.");
                    }

                    // Insert data
                    string insertQuery = "INSERT INTO newusers (username, email) VALUES ('john_doe', 'john@example.com')";
                    using (var insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        await insertCommand.ExecuteNonQueryAsync();
                        log.LogInformation("Inserted a new user into the 'newusers' table.");
                    }

                    // Select data
                    string selectQuery = "SELECT COUNT(*) FROM newusers";
                    using (var selectCommand = new SqlCommand(selectQuery, connection))
                    {
                        int count = (int)await selectCommand.ExecuteScalarAsync();
                        log.LogInformation($"Number of records in 'newusers' table: {count}");
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
