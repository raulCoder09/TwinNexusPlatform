namespace _Scripts.Models.CertificateManagement
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using UnityEngine;

    [System.Serializable]
    public class CertificateInfo
    {
        public string Subject;
        public string Issuer;
        public string SerialNumber;
        public DateTime NotBefore;
        public DateTime NotAfter;
        public string Thumbprint;
        public bool HasPrivateKey;

        public static CertificateInfo FromCertificate(X509Certificate2 certificate)
        {
            try
            {
                if (certificate == null)
                {
                    Debug.LogError("[CertificateInfo] Certificate is null");
                    return new CertificateInfo();
                }

                return new CertificateInfo
                {
                    Subject = certificate.Subject,
                    Issuer = certificate.Issuer,
                    SerialNumber = certificate.SerialNumber,
                    NotBefore = certificate.NotBefore,
                    NotAfter = certificate.NotAfter,
                    Thumbprint = certificate.Thumbprint,
                    HasPrivateKey = certificate.HasPrivateKey
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CertificateInfo] Error creating CertificateInfo: {ex.Message}");
                return new CertificateInfo();
            }
        }

        public string GetCertificateInfo()
        {
            try
            {
                if (string.IsNullOrEmpty(Subject))
                    return "Certificate info is empty";

                return $"Subject: {Subject}\n" +
                       $"Issuer: {Issuer}\n" +
                       $"Serial Number: {SerialNumber}\n" +
                       $"Not Before: {NotBefore}\n" +
                       $"Not After: {NotAfter}\n" +
                       $"Thumbprint: {Thumbprint}\n" +
                       $"Has Private Key: {HasPrivateKey}";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CertificateInfo] Error formatting certificate info: {ex.Message}");
                return $"Error getting certificate info: {ex.Message}";
            }
        }
    }
}