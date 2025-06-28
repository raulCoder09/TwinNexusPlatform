using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using UnityEngine;
using _Scripts.Models.FileManagement;

namespace _Scripts.Models.CertificateManagement
{
    public class CertificateLoader
    {
        private readonly FileManager fileManager;
        private readonly CertificateEventManager _certificateEventManager;
        private readonly bool enableDebugLogs = true;

        public CertificateLoader(FileManager fileManager, CertificateEventManager certificateEventManager)
        {
            this.fileManager = fileManager;
            this._certificateEventManager = certificateEventManager;
        }

        public async Task<byte[]> LoadCertificateBytesAsync(string pfxPath, StorageInfo.StorageCategory category)
        {
            try
            {
                if (string.IsNullOrEmpty(pfxPath))
                {
                    LogError("Certificate path is empty");
                    _certificateEventManager.TriggerValidationFailed(pfxPath, "Certificate path is empty");
                    return null;
                }

                if (category == StorageInfo.StorageCategory.Resources)
                {
                    byte[] certBytes = null;
                    bool readSuccess = false;
                    fileManager.ReadFileAsync(category, Path.GetFileName(pfxPath), content =>
                    {
                        if (content != null)
                        {
                            certBytes = System.Text.Encoding.UTF8.GetBytes(content);
                            readSuccess = true;
                        }
                    });

                    while (!readSuccess && certBytes == null)
                    {
                        await Task.Delay(50);
                    }

                    if (certBytes == null)
                    {
                        LogError($"Failed to load certificate from {pfxPath} in {category}");
                        _certificateEventManager.TriggerValidationFailed(pfxPath, "Failed to load certificate");
                        return null;
                    }

                    LogDebug($"Certificate loaded successfully. Size: {certBytes.Length} bytes");
                    return certBytes;
                }
                else
                {
                    byte[] certBytes = fileManager.ReadFileBytes(category, Path.GetFileName(pfxPath));
                    if (certBytes == null)
                    {
                        LogError($"Failed to load certificate from {pfxPath} in {category}");
                        _certificateEventManager.TriggerValidationFailed(pfxPath, "Failed to load certificate");
                        return null;
                    }

                    LogDebug($"Certificate loaded successfully from {pfxPath}. Size: {certBytes.Length} bytes");
                    return certBytes;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error loading certificate {pfxPath}: {ex.Message}");
                _certificateEventManager.TriggerValidationFailed(pfxPath, ex.Message);
                return null;
            }
        }

        public X509Certificate2 CreateX509Certificate(byte[] certBytes, string password = "")
        {
            try
            {
                if (certBytes == null || certBytes.Length == 0)
                {
                    LogError("Certificate bytes are null or empty");
                    _certificateEventManager.TriggerValidationFailed("", "Certificate bytes are null or empty");
                    return null;
                }

                try
                {
                    var certificate = new X509Certificate2(certBytes, password, X509KeyStorageFlags.Exportable);
                    var certInfo = CertificateInfo.FromCertificate(certificate);
                    _certificateEventManager.TriggerCertificateLoaded("", certInfo);
                    LogDebug("X509Certificate2 created successfully");
                    return certificate;
                }
                catch (CryptographicException ex)
                {
                    if (ex.Message.Contains("password"))
                    {
                        LogError($"Failed to create X509Certificate2: Invalid password");
                        _certificateEventManager.TriggerValidationFailed("", "Invalid password");
                    }
                    else
                    {
                        LogError($"Failed to create X509Certificate2: Invalid certificate format - {ex.Message}");
                        _certificateEventManager.TriggerValidationFailed("", $"Invalid certificate format - {ex.Message}");
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to create X509Certificate2: {ex.Message}");
                _certificateEventManager.TriggerValidationFailed("", ex.Message);
                return null;
            }
        }

        public async Task<X509Certificate2> LoadX509CertificateAsync(string pfxPath, StorageInfo.StorageCategory category, string password = "")
        {
            try
            {
                var certBytes = await LoadCertificateBytesAsync(pfxPath, category);
                if (certBytes == null)
                    return null;

                return CreateX509Certificate(certBytes, password);
            }
            catch (Exception ex)
            {
                LogError($"Failed to load X509Certificate from {pfxPath}: {ex.Message}");
                _certificateEventManager.TriggerValidationFailed(pfxPath, ex.Message);
                return null;
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[CertificateLoader] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[CertificateLoader] {message}");
        }
    }
}