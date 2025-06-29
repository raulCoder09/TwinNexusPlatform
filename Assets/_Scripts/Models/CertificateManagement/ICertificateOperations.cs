namespace _Scripts.Models.CertificateManagement
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    public interface ICertificateOperations
    {
        Task<X509Certificate2> LoadX509CertificateAsync(string path, string fileName, string password, Action<CertificateInfo> callback);
        Task<bool> ValidateCertificateAsync(string path, string fileName, Action<bool> callback);
        bool ValidateServerCertificate(X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors);
        string GetCertificateInfo(X509Certificate2 certificate);
    }
}