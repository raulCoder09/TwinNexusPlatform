using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using UnityEngine;
using _Scripts.Models.FileManagement;

namespace _Scripts.Models.CertificateManagement
{
    public class CertificateValidator
    {
        private readonly CertificateEventManager eventManager;
        private readonly bool enableDebugLogs = true;
        private readonly bool allowUntrustedCertificates;
        private readonly bool ignoreCertificateChainErrors;
        private readonly bool ignoreCertificateRevocationErrors;

        public CertificateValidator(CertificateEventManager eventManager, bool allowUntrusted = false, bool ignoreChainErrors = false, bool ignoreRevocationErrors = true)
        {
            this.eventManager = eventManager;
            this.allowUntrustedCertificates = allowUntrusted;
            this.ignoreCertificateChainErrors = ignoreChainErrors;
            this.ignoreCertificateRevocationErrors = ignoreRevocationErrors;
        }

        public async Task<bool> ValidateCertificatesAsync(string pfxPath, StorageInfo.StorageCategory category)
        {
            try
            {
                if (string.IsNullOrEmpty(pfxPath))
                {
                    LogError("Certificate path is empty");
                    eventManager.TriggerValidationFailed(pfxPath, "Certificate path is empty");
                    return false;
                }

                if (!pfxPath.EndsWith(".pfx"))
                {
                    LogError("Certificate file must be a .pfx file");
                    eventManager.TriggerValidationFailed(pfxPath, "Invalid file extension");
                    return false;
                }

                if (category == StorageInfo.StorageCategory.Resources)
                {
                    LogDebug($"Assuming certificate exists in {category}: {pfxPath}");
                }
                else
                {
                    string fullPath = Path.Combine(FileManager.Instance.GetStorageInfo().basePath, "certificates", Path.GetFileName(pfxPath));
                    if (!File.Exists(fullPath))
                    {
                        LogError($"Certificate file not found: {fullPath}");
                        eventManager.TriggerValidationFailed(pfxPath, "File not found");
                        return false;
                    }
                }

                eventManager.TriggerCertificateValidated(pfxPath, true);
                LogDebug($"Certificate validation successful: {pfxPath}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Certificate validation failed for {pfxPath}: {ex.Message}");
                eventManager.TriggerValidationFailed(pfxPath, ex.Message);
                return false;
            }
        }

        public bool ValidateServerCertificate(X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            try
            {
                if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                {
                    LogDebug("Server certificate validation: No errors");
                    eventManager.TriggerServerCertificateValidated(true, "No errors");
                    return true;
                }

                if (allowUntrustedCertificates && sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateNotAvailable)
                {
                    LogDebug("Server certificate validation: Allowing untrusted certificate");
                    eventManager.TriggerServerCertificateValidated(true, "Allowing untrusted certificate");
                    return true;
                }

                if (ignoreCertificateChainErrors && sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    LogDebug("Server certificate validation: Ignoring chain errors");
                    eventManager.TriggerServerCertificateValidated(true, "Ignoring chain errors");
                    return true;
                }

                if (ignoreCertificateRevocationErrors && sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch)
                {
                    LogDebug("Server certificate validation: Ignoring revocation errors");
                    eventManager.TriggerServerCertificateValidated(true, "Ignoring revocation errors");
                    return true;
                }

                LogError($"Server certificate validation failed: {sslPolicyErrors}");
                eventManager.TriggerServerCertificateValidated(false, $"Validation failed: {sslPolicyErrors}");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error validating server certificate: {ex.Message}");
                eventManager.TriggerServerCertificateValidated(false, ex.Message);
                return false;
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[CertificateValidator] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[CertificateValidator] {message}");
        }
    }
}