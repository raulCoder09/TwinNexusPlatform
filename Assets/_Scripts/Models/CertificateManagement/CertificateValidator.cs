using System.IO;

namespace _Scripts.Models.CertificateManagement
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using UnityEngine;
    using _Scripts.Models.FileManagement;

    public class CertificateValidator : ICertificateOperations
    {
        private readonly FileManager _fileManager;
        private readonly CertificateEventManager _eventManager;
        private readonly bool _allowUntrustedCertificates;
        private readonly bool _ignoreCertificateChainErrors;
        private readonly bool _ignoreCertificateRevocationErrors;
        private readonly bool _enableDebugLogs = true;
        private readonly string _defaultPassword = "5859"; // Contraseña del .pfx

        public CertificateValidator(FileManager fileManager, CertificateEventManager eventManager, bool allowUntrusted = false, bool ignoreChainErrors = false, bool ignoreRevocationErrors = true)
        {
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            _allowUntrustedCertificates = allowUntrusted;
            _ignoreCertificateChainErrors = ignoreChainErrors;
            _ignoreCertificateRevocationErrors = ignoreRevocationErrors;
        }

        public async Task<bool> ValidateCertificateAsync(string path, string fileName, Action<bool> callback)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(fileName))
                {
                    LogError("Path or fileName is empty");
                    _eventManager.TriggerValidationFailed(path, "Path or fileName is empty");
                    callback?.Invoke(false);
                    return false;
                }

                if (!fileName.EndsWith(".pfx"))
                {
                    LogError("Certificate file must be a .pfx file");
                    _eventManager.TriggerValidationFailed(path, "Invalid file extension");
                    callback?.Invoke(false);
                    return false;
                }

                byte[] certBytes = null;
                if (path.StartsWith(_fileManager.GetBasePath("streaming")))
                {
                    // Copiar desde StreamingAssets a persistentDataPath
                    string targetPath = Path.Combine(_fileManager.GetBasePath("persistent"), "certificates");
                    bool copySuccess = false;
                    _fileManager.CopyFileAsync(path, targetPath, fileName, success => copySuccess = success);
                    await Task.Delay(100); // Espera asíncrona para la copia
                    if (!copySuccess)
                    {
                        LogError($"Failed to copy {fileName} from {path} to {targetPath}");
                        _eventManager.TriggerValidationFailed(path, "Failed to copy certificate");
                        callback?.Invoke(false);
                        return false;
                    }
                    path = targetPath;
                }

                _fileManager.ReadFileBytesAsync(path, fileName, bytes => certBytes = bytes);
                await Task.Delay(100); // Simula espera asíncrona para FileManager

                if (certBytes == null || certBytes.Length == 0)
                {
                    LogError($"Failed to read certificate {fileName} from {path}");
                    _eventManager.TriggerValidationFailed(path, "Failed to read certificate");
                    callback?.Invoke(false);
                    return false;
                }

                try
                {
                    var certificate = new X509Certificate2(certBytes, _defaultPassword,
                        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

                    if (!certificate.HasPrivateKey)
                    {
                        LogError("Certificate must have private key");
                        _eventManager.TriggerValidationFailed(path, "Certificate missing private key");
                        callback?.Invoke(false);
                        return false;
                    }

                    DateTime now = DateTime.UtcNow;
                    if (now < certificate.NotBefore || now > certificate.NotAfter)
                    {
                        LogError($"Certificate validity period invalid: {certificate.NotBefore} to {certificate.NotAfter}");
                        _eventManager.TriggerValidationFailed(path, "Certificate validity period invalid");
                        callback?.Invoke(false);
                        return false;
                    }

                    _eventManager.TriggerCertificateValidated(path, true);
                    LogDebug($"Certificate validation successful: {fileName}");
                    callback?.Invoke(true);
                    return true;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to validate certificate {fileName} from {path}: {ex.Message}");
                    _eventManager.TriggerValidationFailed(path, ex.Message);
                    callback?.Invoke(false);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error validating certificate {fileName} from {path}: {ex.Message}");
                _eventManager.TriggerValidationFailed(path, ex.Message);
                callback?.Invoke(false);
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
                    _eventManager.TriggerServerCertificateValidated(true, "No errors");
                    return true;
                }

                if (_allowUntrustedCertificates && sslPolicyErrors.HasFlag(System.Net.Security.SslPolicyErrors.RemoteCertificateNotAvailable))
                {
                    LogDebug("Server certificate validation: Allowing untrusted certificate");
                    _eventManager.TriggerServerCertificateValidated(true, "Allowing untrusted certificate");
                    return true;
                }

                if (_ignoreCertificateChainErrors && sslPolicyErrors.HasFlag(System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors))
                {
                    LogDebug("Server certificate validation: Ignoring chain errors");
                    _eventManager.TriggerServerCertificateValidated(true, "Ignoring chain errors");
                    return true;
                }

                if (_ignoreCertificateRevocationErrors && sslPolicyErrors.HasFlag(System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch))
                {
                    LogDebug("Server certificate validation: Ignoring revocation errors");
                    _eventManager.TriggerServerCertificateValidated(true, "Ignoring revocation errors");
                    return true;
                }

                LogError($"Server certificate validation failed: {sslPolicyErrors}");
                _eventManager.TriggerServerCertificateValidated(false, $"Validation failed: {sslPolicyErrors}");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error validating server certificate: {ex.Message}");
                _eventManager.TriggerServerCertificateValidated(false, ex.Message);
                return false;
            }
        }

        public Task<X509Certificate2> LoadX509CertificateAsync(string path, string fileName, string password, Action<CertificateInfo> callback)
        {
            throw new NotImplementedException("Loading handled by CertificateLoader");
        }

        public string GetCertificateInfo(X509Certificate2 certificate)
        {
            try
            {
                if (certificate == null)
                {
                    LogError("Certificate is null");
                    return "Certificate is null";
                }

                return CertificateInfo.FromCertificate(certificate).GetCertificateInfo();
            }
            catch (Exception ex)
            {
                LogError($"Error getting certificate info: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[CertificateValidator] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[CertificateValidator] {message}");
        }
    }
}