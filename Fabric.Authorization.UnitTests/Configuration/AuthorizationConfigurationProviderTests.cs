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

        [Fact]
        public void GetConfiguration_GetsEncryptedConfiguration()
        {
            var certificateService = new Mock<ICertificateService>();
            certificateService
                .Setup(certService => certService.GetCertificate(It.IsAny<EncryptionCertificateSettings>()))
                .Returns((EncryptionCertificateSettings settings) => GetCertificate());
            var configProvider = new AuthorizationConfigurationProvider(certificateService.Object);

            CreateDockerSecret("CouchDbSettings__Password", "!!enc!!:gczwrq53rqqMwA6y+4z0ThqU7DQLP9J2Lu+Yy0OpayMZrh73aOZLwS4WAqZ00HUcPsrNQWGxFel1UEuPYx4c8hztddYI6XRpbDgCYvDvJiIxMVnrsekpHwFv2vmVWbxXRw306Rv13oA9xdTbFMvavy2LYT5GgkOpJLAcAxX9b4F1svmiNDeroL6+tNIxN3lX8i2o9GzR94HJltQi+c1pgN3WRDy0Jrgk4dA174xkJuo47CiS0N3ePZMaHz98ok9sUio7lQ8OzL25xp+1mLLVXoywUcKzpJJ+PFTltMXamEzkeH4hNIu5HCngiNqDB09bQ22WElzESIA6dw3iVVjH6g==");

            var config = configProvider.GetAppConfiguration(Directory.GetCurrentDirectory());
            DeleteDockerSecret("CouchDbSettings__Password");

            Assert.Equal("password", config.CouchDbSettings.Password);

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
