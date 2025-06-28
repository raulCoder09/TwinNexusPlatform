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

        /// <summary>
        /// ✅ Método optimizado para cargar certificados usando tu FileManager
        /// </summary>
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

                LogDebug($"🔄 Loading certificate: {pfxPath} from category: {category}");

                if (category == StorageInfo.StorageCategory.Resources)
                {
                    // ✅ OPTIMIZACIÓN: Lectura directa desde StreamingAssets usando tu FileManager
                    // Evitamos la copia innecesaria, pero mantenemos tu arquitectura
                    
                    LogDebug($"📁 Reading certificate directly from StreamingAssets...");
                    
                    // Usar el path directo para StreamingAssets (más eficiente)
                    string fileName = Path.GetFileName(pfxPath);
                    string fullPath = Path.Combine(Application.streamingAssetsPath, "resources", fileName);
                    
                    LogDebug($"📍 Full path: {fullPath}");
                    
                    if (!File.Exists(fullPath))
                    {
                        LogError($"❌ Certificate file not found: {fullPath}");
                        _certificateEventManager.TriggerValidationFailed(pfxPath, "Certificate file not found");
                        return null;
                    }
                    
                    // ✅ Lectura directa usando File.ReadAllBytesAsync (más eficiente para binarios)
                    byte[] certBytes = await File.ReadAllBytesAsync(fullPath);
                    
                    if (certBytes == null || certBytes.Length == 0)
                    {
                        LogError($"❌ Failed to read certificate or file is empty");
                        _certificateEventManager.TriggerValidationFailed(pfxPath, "Failed to read certificate or file is empty");
                        return null;
                    }

                    LogDebug($"✅ Certificate loaded successfully. Size: {certBytes.Length} bytes");
                    return certBytes;
                }
                else
                {
                    // ✅ Para otras categorías, usar tu FileManager como está diseñado
                    LogDebug($"📁 Using FileManager for category: {category}");
                    
                    byte[] certBytes = fileManager.ReadFileBytes(category, Path.GetFileName(pfxPath));
                    if (certBytes == null)
                    {
                        LogError($"❌ Failed to load certificate from {pfxPath} in {category}");
                        _certificateEventManager.TriggerValidationFailed(pfxPath, "Failed to load certificate");
                        return null;
                    }

                    LogDebug($"✅ Certificate loaded via FileManager. Size: {certBytes.Length} bytes");
                    return certBytes;
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Error loading certificate {pfxPath}: {ex.Message}");
                _certificateEventManager.TriggerValidationFailed(pfxPath, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// ✅ Método mejorado para crear certificados X.509 con mejor manejo de errores
        /// </summary>
        public X509Certificate2 CreateX509Certificate(byte[] certBytes, string password = "")
        {
            try
            {
                if (certBytes == null || certBytes.Length == 0)
                {
                    LogError("❌ Certificate bytes are null or empty");
                    _certificateEventManager.TriggerValidationFailed("", "Certificate bytes are null or empty");
                    return null;
                }

                LogDebug($"🔄 Creating X509Certificate2 from {certBytes.Length} bytes with password...");

                try
                {
                    // ✅ Usar flags específicos para AWS IoT Core
                    var certificate = new X509Certificate2(certBytes, password, 
                        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                    
                    // ✅ Validar que el certificado tiene private key (requerido para AWS IoT Core)
                    if (!certificate.HasPrivateKey)
                    {
                        LogError("❌ Certificate does not have a private key (required for AWS IoT Core)");
                        _certificateEventManager.TriggerValidationFailed("", "Certificate missing private key");
                        return null;
                    }
                    
                    // ✅ Validar fechas del certificado
                    DateTime now = DateTime.UtcNow;
                    if (now < certificate.NotBefore)
                    {
                        LogError($"❌ Certificate is not yet valid (valid from: {certificate.NotBefore})");
                        _certificateEventManager.TriggerValidationFailed("", "Certificate not yet valid");
                        return null;
                    }
                    
                    if (now > certificate.NotAfter)
                    {
                        LogError($"❌ Certificate has expired (expired: {certificate.NotAfter})");
                        _certificateEventManager.TriggerValidationFailed("", "Certificate expired");
                        return null;
                    }
                    
                    var certInfo = CertificateInfo.FromCertificate(certificate);
                    _certificateEventManager.TriggerCertificateLoaded("", certInfo);
                    
                    LogDebug("✅ X509Certificate2 created successfully");
                    LogDebug($"  Subject: {certificate.Subject}");
                    LogDebug($"  Issuer: {certificate.Issuer}");
                    LogDebug($"  Valid: {certificate.NotBefore} to {certificate.NotAfter}");
                    LogDebug($"  Has Private Key: {certificate.HasPrivateKey}");
                    
                    return certificate;
                }
                catch (CryptographicException ex)
                {
                    if (ex.Message.Contains("password") || ex.Message.Contains("invalid") || ex.Message.Contains("incorrect"))
                    {
                        LogError($"❌ Invalid password or certificate format: {ex.Message}");
                        _certificateEventManager.TriggerValidationFailed("", "Invalid password or certificate format");
                    }
                    else if (ex.Message.Contains("unsupported"))
                    {
                        LogError($"❌ Unsupported certificate format (try regenerating PFX with -legacy flag): {ex.Message}");
                        _certificateEventManager.TriggerValidationFailed("", "Unsupported certificate format - try legacy PFX");
                    }
                    else
                    {
                        LogError($"❌ Cryptographic error: {ex.Message}");
                        _certificateEventManager.TriggerValidationFailed("", $"Cryptographic error - {ex.Message}");
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Failed to create X509Certificate2: {ex.Message}");
                _certificateEventManager.TriggerValidationFailed("", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// ✅ Método principal optimizado que combina carga y creación
        /// </summary>
        public async Task<X509Certificate2> LoadX509CertificateAsync(string pfxPath, StorageInfo.StorageCategory category, string password = "")
        {
            try
            {
                LogDebug($"🔄 Loading X509 certificate: {pfxPath}");
                
                var certBytes = await LoadCertificateBytesAsync(pfxPath, category);
                if (certBytes == null)
                {
                    LogError("❌ Failed to load certificate bytes");
                    return null;
                }

                var certificate = CreateX509Certificate(certBytes, password);
                if (certificate == null)
                {
                    LogError("❌ Failed to create X509Certificate2");
                    return null;
                }
                
                LogDebug($"✅ X509Certificate2 loaded successfully: {certificate.Subject}");
                return certificate;
            }
            catch (Exception ex)
            {
                LogError($"❌ Failed to load X509Certificate from {pfxPath}: {ex.Message}");
                _certificateEventManager.TriggerValidationFailed(pfxPath, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// ✅ Método helper para validar certificados antes de usar
        /// </summary>
        public bool ValidateCertificateForAwsIoT(X509Certificate2 certificate)
        {
            try
            {
                if (certificate == null)
                {
                    LogError("❌ Certificate is null");
                    return false;
                }

                // Validaciones específicas para AWS IoT Core
                if (!certificate.HasPrivateKey)
                {
                    LogError("❌ Certificate must have private key for AWS IoT Core");
                    return false;
                }

                DateTime now = DateTime.UtcNow;
                if (now < certificate.NotBefore || now > certificate.NotAfter)
                {
                    LogError($"❌ Certificate validity period invalid: {certificate.NotBefore} to {certificate.NotAfter}");
                    return false;
                }

                LogDebug($"✅ Certificate validation passed for AWS IoT Core");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"❌ Certificate validation error: {ex.Message}");
                return false;
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