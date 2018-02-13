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
        public ConnectionStrings ConnectionStrings { get; }

        public SqlServerIntegrationTestsFixture()
        {
            DatabaseNameSuffix = GetDatabaseNameSuffix();
            ConnectionStrings = GetSqlServerConnection(DatabaseNameSuffix);
            CreateSqlServerDatabase();
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
                AuthorizationDatabase = $"Server={sqlServerHost};Database=Authorization-{databaseNameSuffix};{sqlServerSecurityString};MultipleActiveResultSets=true"
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

        private void CreateSqlServerDatabase()
        {
            var targetDbName = $"Authorization-{DatabaseNameSuffix}";

            var connection =
                ConnectionStrings.AuthorizationDatabase.Replace(targetDbName, "master");
            var file = new FileInfo("Fabric.Authorization.SqlServer_Create.sql");

            var createDbScript = file.OpenText()
                .ReadToEnd()
                .Replace("$(DatabaseName)", targetDbName);

            var splitter = new[] {"GO\r\n"};
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
                            command.ExecuteNonQuery();
                            break;
                        }
                    }
                }
            }

            // establish a connection to the newly created Identity DB
            using (var conn = new SqlConnection(ConnectionStrings.AuthorizationDatabase))
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

                        command.CommandText = commandText.Replace("HCFabricAuthorizationData1", "PRIMARY")
                            .Replace("HCFabricAuthorizationIndex1", "PRIMARY")
                            .TrimEnd(Environment.NewLine.ToCharArray());
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public void Dispose()
        {
            var targetDbName = $"Authorization-{DatabaseNameSuffix}";

            var connection =
                ConnectionStrings.AuthorizationDatabase.Replace(targetDbName, "master");
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
