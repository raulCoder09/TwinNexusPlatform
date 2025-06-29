using System.IO;

namespace _Scripts.Models.CertificateManagement
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using UnityEngine;
    using _Scripts.Models.FileManagement;

    public class CertificateLoader : ICertificateOperations
    {
        private readonly FileManager _fileManager;
        private readonly CertificateEventManager _eventManager;
        private readonly bool _enableDebugLogs = true;

        public CertificateLoader(FileManager fileManager, CertificateEventManager eventManager)
        {
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        }

        public async Task<X509Certificate2> LoadX509CertificateAsync(string path, string fileName, string password, Action<CertificateInfo> callback)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(fileName))
                {
                    LogError("Path or fileName is empty");
                    _eventManager.TriggerValidationFailed(path, "Path or fileName is empty");
                    callback?.Invoke(null);
                    return null;
                }

                LogDebug($"Loading certificate: {fileName} from {path}");

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
                        callback?.Invoke(null);
                        return null;
                    }

                    // Leer desde persistentDataPath
                    path = targetPath;
                }

                _fileManager.ReadFileBytesAsync(path, fileName, bytes => certBytes = bytes);
                await Task.Delay(100); // Simula espera asíncrona para FileManager

                if (certBytes == null || certBytes.Length == 0)
                {
                    LogError($"Failed to read certificate {fileName} from {path}");
                    _eventManager.TriggerValidationFailed(path, "Failed to read certificate");
                    callback?.Invoke(null);
                    return null;
                }

                try
                {
                    var certificate = new X509Certificate2(certBytes, password,
                        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

                    var certInfo = CertificateInfo.FromCertificate(certificate);
                    _eventManager.TriggerCertificateLoaded(path, certInfo);
                    callback?.Invoke(certInfo);

                    LogDebug($"X509Certificate2 loaded successfully: {certificate.Subject}");
                    return certificate;
                }
                catch (Exception ex)
                {
                    LogError($"Failed to create X509Certificate2 from {fileName}: {ex.Message}");
                    _eventManager.TriggerValidationFailed(path, ex.Message);
                    callback?.Invoke(null);
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error loading certificate {fileName} from {path}: {ex.Message}");
                _eventManager.TriggerValidationFailed(path, ex.Message);
                callback?.Invoke(null);
                return null;
            }
        }

        public Task<bool> ValidateCertificateAsync(string path, string fileName, Action<bool> callback)
        {
            throw new NotImplementedException("Validation handled by CertificateValidator");
        }

        public bool ValidateServerCertificate(X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            throw new NotImplementedException("Server validation handled by CertificateValidator");
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
                Debug.Log($"[CertificateLoader] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[CertificateLoader] {message}");
        }
    }
}