using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Services;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Configuration
{
    public class AuthorizationConfigurationProviderTests
    {
        private const string UnencryptedAppSettings = @"{
                                                  ""ClientName"": ""hc"",
                                                  ""UseInMemoryStores"": false,
                                                  ""ElasticSearchSettings"": {
                                                    ""Scheme"": ""http"",
                                                    ""Server"": ""localhost"",
                                                    ""Port"": ""9200"",
                                                    ""Username"": ""test"",
                                                    ""Password"": ""test""
                                                  },
                                                  ""IdentityServerConfidentialClientSettings"": {
                                                    ""Authority"": ""http://localhost:5001/"",
                                                    ""ClientId"": ""authorization-api"",
                                                    ""ClientSecret"": ""secret"",
                                                    ""Scopes"": [
                                                      ""fabric/authorization.read"",
                                                      ""fabric/authorization.write"",
                                                      ""fabric/authorization.manageclients""
                                                    ]
                                                  },
                                                  ""CouchDbSettings"": {
                                                    ""Server"": ""http://localhost:5984/"",
                                                    ""DatabaseName"": ""authorization"",
                                                    ""Username"": ""test"",
                                                    ""Password"": ""test""
                                                  },
                                                  ""ApplicationInsights"": {
                                                    ""InstrumentationKey"": ""123456"",
                                                    ""Enabled"": false
                                                  },
                                                  ""EncryptionCertificateSettings"": {
                                                    ""EncryptionCertificateThumbprint"": ""test""
                                                  } 
                                                }";

        private const string DockerSecretDirectory = @"/run/secrets";

        [Fact]
        public void GetConfiguration_GetsUnencryptedConfigurationFromAppSettings()
        {
            //create the provider
            var certificateService = new Mock<ICertificateService>().Object;
            var configProvider = new AuthorizationConfigurationProvider(certificateService);

            WriteAppSettingsToFile(UnencryptedAppSettings);

            var config = configProvider.GetAppConfiguration(Directory.GetCurrentDirectory());
            Assert.Equal("hc", config.ClientName);
            Assert.False(config.ApplicationInsights.Enabled);
            Assert.Equal("test", config.EncryptionCertificateSettings.EncryptionCertificateThumbprint);
        }

        [Fact]
        public void GetConfiguration_GetsUnencryptedConfigurationFromDockerSecrets()
        {
            var certificateService = new Mock<ICertificateService>().Object;
            var configProvider = new AuthorizationConfigurationProvider(certificateService);

            WriteAppSettingsToFile(UnencryptedAppSettings);
            CreateDockerSecret("ApplicationInsights__InstrumentationKey", "56789");

            var config = configProvider.GetAppConfiguration(Directory.GetCurrentDirectory());
            DeleteDockerSecret("ApplicationInsights__InstrumentationKey");

            Assert.Equal("hc", config.ClientName);
            Assert.False(config.ApplicationInsights.Enabled);
            Assert.Equal("56789", config.ApplicationInsights.InstrumentationKey);
            Assert.Equal("test", config.EncryptionCertificateSettings.EncryptionCertificateThumbprint);
        }

        private void WriteAppSettingsToFile(string settings)
        {
            var appSettingsPath = "appsettings.json";
            var appSettings = File.Create(appSettingsPath);
            using (var writer = new StreamWriter(appSettings))
            {
                writer.Write(settings);
            }
        }

        private void CreateDockerSecret(string name, string value)
        {
            if (!Directory.Exists(DockerSecretDirectory))
            {
                Directory.CreateDirectory(DockerSecretDirectory);
            }
            var secretFile = Path.Combine(DockerSecretDirectory, name);
            var setting = File.Create(secretFile);
            using (var writer = new StreamWriter(setting))
            {
                writer.Write(value);
            }
        }

        private void DeleteDockerSecret(string name)
        {
            var secretsFile = Path.Combine(DockerSecretDirectory, name);
            if (!File.Exists(secretsFile))
            {
                return;
            }
            File.Delete(secretsFile);
        }

        private X509Certificate2 GetCertificate()
        {
            var assembly = typeof(AuthorizationConfigurationProviderTests).GetTypeInfo().Assembly;
            using (var certStream =
                assembly.GetManifestResourceStream("Fabric.Authorization.UnitTests.Configuration.identity.test.pfx"))
            using (var memoryStream = new MemoryStream())
            {
                certStream.CopyTo(memoryStream);
                var cert = new X509Certificate2(memoryStream.ToArray(), "identity");
                return cert;
            }
        }
    }
}
