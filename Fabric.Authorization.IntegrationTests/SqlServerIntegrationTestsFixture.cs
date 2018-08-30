using System;
using System.Data.SqlClient;
using System.IO;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Xunit;

namespace Fabric.Authorization.IntegrationTests
{
    public class SqlServerIntegrationTestsFixture : IDisposable
    {
        private string DatabaseNameSuffix { get; }
        private static readonly string AuthorizationDatabaseName = "Authorization";
        private static readonly string EdwAdminDatabaseName = "EDWAdmin";
        public ConnectionStrings ConnectionStrings { get; }

        public SqlServerIntegrationTestsFixture()
        {
            DatabaseNameSuffix = GetDatabaseNameSuffix();
            ConnectionStrings = GetSqlServerConnection(DatabaseNameSuffix);
            CreateAuthorizationSqlServerDatabase();
            CreateEdwAdminSqlServerDatabase();
        }

        private static readonly string SqlServerEnvironmentVariable = "SQLSERVERSETTINGS__SERVER";
        private static readonly string SqlServerUsernameEnvironmentVariable = "SQLSERVERSETTINGS__USERNAME";
        private static readonly string SqlServerPasswordEnvironmentVariable = "SQLSERVERSETTINGS__PASSWORD";

        private string GetDatabaseNameSuffix()
        {
            var id = Guid.NewGuid().ToString().Replace("-", "");
            return id;
        }

        private ConnectionStrings GetSqlServerConnection(string databaseNameSuffix)
        {
            var sqlServerHost = Environment.GetEnvironmentVariable(SqlServerEnvironmentVariable) ?? ".";
            var sqlServerSecurityString = GetSqlServerSecurityString();
            var connectionString = new ConnectionStrings
            {
                AuthorizationDatabase = $"Server={sqlServerHost};Database={AuthorizationDatabaseName}-{databaseNameSuffix};{sqlServerSecurityString};MultipleActiveResultSets=true",
                EDWAdminDatabase = $"Server={sqlServerHost};Database={EdwAdminDatabaseName}-{databaseNameSuffix};{sqlServerSecurityString};MultipleActiveResultSets=true"
            };

            return connectionString;
        }

        private string GetSqlServerSecurityString()
        {
            var sqlServerUserName = Environment.GetEnvironmentVariable(SqlServerUsernameEnvironmentVariable);
            var sqlServerPassword = Environment.GetEnvironmentVariable(SqlServerPasswordEnvironmentVariable);
            var securityString = "Trusted_Connection=True";
            if (!string.IsNullOrEmpty(sqlServerUserName) && !string.IsNullOrEmpty(sqlServerPassword))
            {
                securityString = $"User Id={sqlServerUserName};Password={sqlServerPassword}";
            }
            return securityString;
        }

        private void CreateEdwAdminSqlServerDatabase()
        {
            var targetDbName = $"{EdwAdminDatabaseName}-{DatabaseNameSuffix}";
            var file = new FileInfo("EDWAdmin.SqlServer_Create.sql");
            var dataFileGroup = "HCEDWAdminData1";
            var indexFileGroup = "HCEDWAdminIndex1";
            CreateSqlServerDatabase(targetDbName, file, dataFileGroup, indexFileGroup, ConnectionStrings.EDWAdminDatabase);
        }

        private void CreateAuthorizationSqlServerDatabase()
        {
            var targetDbName = $"{AuthorizationDatabaseName}-{DatabaseNameSuffix}";
            var file = new FileInfo("Fabric.Authorization.SqlServer_Create.sql");
            var dataFileGroup = "HCFabricAuthorizationData1";
            var indexFileGroup = "HCFabricAuthorizationIndex1";
            CreateSqlServerDatabase(targetDbName, file, dataFileGroup, indexFileGroup, ConnectionStrings.AuthorizationDatabase);
        }


        private void CreateSqlServerDatabase(string targetDbName, FileInfo file, string dataFileGroup, string indexFileGroup, string connectionString)
        {
            var connection =
                connectionString.Replace(targetDbName, "master");

            var createDbScript = file.OpenText()
                .ReadToEnd()
                .Replace("$(DatabaseName)", targetDbName);

            var splitter = new[] {$"GO{Environment.NewLine}"};
            var commandTexts = createDbScript.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            int x;
            using (var conn = new SqlConnection(connection))
            {
                conn.Open();
                using (var command = new SqlCommand("query", conn))
                {
                    for (x = 0; x < commandTexts.Length; x++)
                    {
                        var commandText = commandTexts[x];

                        // break if we just created the Identity DB
                        if (commandText.StartsWith("CREATE DATABASE"))
                        {
                            var commandParts = commandText.Split(
                                new[] {" ON "},
                                StringSplitOptions.RemoveEmptyEntries);

                            command.CommandText = commandParts[0];
                            Console.WriteLine($"Executing CREATE DATATBASE with: {command.CommandText}");
                            command.ExecuteNonQuery();
                            break;
                        }
                    }
                }
            }

            // establish a connection to the newly created Identity DB
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var command = new SqlCommand("query", conn))
                {
                    for (x = x + 1; x < commandTexts.Length; x++)
                    {
                        var commandText = commandTexts[x];

                        // skip generated SqlPackage commands and comments
                        if (commandText.StartsWith(":") || commandText.StartsWith("/*"))
                        {
                            continue;
                        }

                        command.CommandText = commandText.Replace(dataFileGroup, "PRIMARY")
                            .Replace(indexFileGroup, "PRIMARY")
                            .TrimEnd(Environment.NewLine.ToCharArray());
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public void Dispose()
        {
            DeleteDatabase($"{AuthorizationDatabaseName}-{DatabaseNameSuffix}", ConnectionStrings.AuthorizationDatabase);
            DeleteDatabase($"{EdwAdminDatabaseName}-{DatabaseNameSuffix}", ConnectionStrings.EDWAdminDatabase);
        }

        public void DeleteDatabase(string targetDbName, string connectionString)
        {
            var connection =
                connectionString.Replace(targetDbName, "master");
            using (var conn = new SqlConnection(connection))
            {
                conn.Open();
                using (var command = new SqlCommand($"ALTER DATABASE [{targetDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", conn))
                {
                    command.ExecuteNonQuery();
                }
                using (var command = new SqlCommand($"DROP DATABASE [{targetDbName}]", conn))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

    }

    [CollectionDefinition("SqlServerTests")]
    public class SqlServerTestsCollection : ICollectionFixture<SqlServerIntegrationTestsFixture>
    { }
}
