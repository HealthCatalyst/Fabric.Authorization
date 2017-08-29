using System.Security.Cryptography.X509Certificates;
using Fabric.Authorization.API.Configuration;

namespace Fabric.Authorization.API.Services
{
    public interface ICertificateService
    {
        X509Certificate2 GetCertificate(EncryptionCertificateSettings settings);
    }
}
