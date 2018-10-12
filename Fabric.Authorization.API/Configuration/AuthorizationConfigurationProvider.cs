﻿using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Fabric.Authorization.API.Services;
using Fabric.Platform.Shared.Configuration.Docker;
using Microsoft.Extensions.Configuration;

namespace Fabric.Authorization.API.Configuration
{
    public class AuthorizationConfigurationProvider
    {
        private static readonly string EncryptionPrefix = "!!enc!!:";
        private readonly ICertificateService _certificateService;

        public AuthorizationConfigurationProvider(ICertificateService certificateService)
        {
            _certificateService = certificateService ?? throw new ArgumentNullException(nameof(certificateService));
        }

        public IAppConfiguration GetAppConfiguration(string basePath)
        {
            var appConfig = BuildAppConfiguration(basePath);
            return appConfig;
        }

        private IAppConfiguration BuildAppConfiguration(string basePath)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddDockerSecrets(typeof(IAppConfiguration))
                .SetBasePath(basePath)
                .Build();

            var appConfig = new AppConfiguration();
            ConfigurationBinder.Bind(config, appConfig);
            DecryptEncryptedValues(appConfig);
            return appConfig;
        }

        private void DecryptEncryptedValues(IAppConfiguration appConfiguration)
        {
            if (appConfiguration.IdentityServerConfidentialClientSettings != null)
            {
                if (IsEncrypted(appConfiguration.IdentityServerConfidentialClientSettings.ClientSecret))
                {
                    appConfiguration.IdentityServerConfidentialClientSettings.ClientSecret =
                        DecryptString(appConfiguration.IdentityServerConfidentialClientSettings.ClientSecret,
                            appConfiguration);
                }
            }
        }

        private static bool IsEncrypted(string value)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith(EncryptionPrefix);
        }

        private string DecryptString(string encryptedString, IAppConfiguration appConfiguration)
        {
            var cert = _certificateService.GetCertificate(appConfiguration.EncryptionCertificateSettings);
            var encryptedPasswordAsBytes =
                Convert.FromBase64String(
                    encryptedString.Replace(EncryptionPrefix, string.Empty));
            var decryptedPasswordAsBytes = cert.GetRSAPrivateKey()
                .Decrypt(encryptedPasswordAsBytes, RSAEncryptionPadding.OaepSHA1);
            return System.Text.Encoding.UTF8.GetString(decryptedPasswordAsBytes);
        }
    }
}
