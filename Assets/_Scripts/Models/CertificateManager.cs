using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using UnityEngine;

namespace _Scripts.Models
{
    public class CertificateManager : MonoBehaviour
    {
        #region Singleton Pattern

        private static CertificateManager _instance;

        public static CertificateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CertificateManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CertificateManager");
                        _instance = go.AddComponent<CertificateManager>();
                        DontDestroyOnLoad(go);
                    }
                }

                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Certificate Configuration")] [SerializeField]
        private string _defaultPfxFileName = "aws-iot.pfx";

        [SerializeField] private bool _allowUntrustedCertificates = false;
        [SerializeField] private bool _ignoreCertificateChainErrors = false;
        [SerializeField] private bool _ignoreCertificateRevocationErrors = true;

        [SerializeField]
        private System.Security.Authentication.SslProtocols _sslProtocol =
            System.Security.Authentication.SslProtocols.Tls12;

        [Header("Certificate Paths")] [SerializeField]
        private string _caFilePath;

        [SerializeField] private string _clientCertPath;
        [SerializeField] private string _clientKeyPath;
        [SerializeField] private string _pfxFilePath;

        [Header("Debug Settings")] [SerializeField]
        private bool _enableDebugLogs = true;

        #endregion

        #region Public Properties

        public string DefaultPfxFileName
        {
            get => _defaultPfxFileName;
            set => _defaultPfxFileName = value;
        }

        public bool AllowUntrustedCertificates
        {
            get => _allowUntrustedCertificates;
            set => _allowUntrustedCertificates = value;
        }

        public bool IgnoreCertificateChainErrors
        {
            get => _ignoreCertificateChainErrors;
            set => _ignoreCertificateChainErrors = value;
        }

        public bool IgnoreCertificateRevocationErrors
        {
            get => _ignoreCertificateRevocationErrors;
            set => _ignoreCertificateRevocationErrors = value;
        }

        public System.Security.Authentication.SslProtocols SslProtocol
        {
            get => _sslProtocol;
            set => _sslProtocol = value;
        }

        public string CaFilePath
        {
            get => _caFilePath;
            set => _caFilePath = value;
        }

        public string ClientCertPath
        {
            get => _clientCertPath;
            set => _clientCertPath = value;
        }

        public string ClientKeyPath
        {
            get => _clientKeyPath;
            set => _clientKeyPath = value;
        }

        public string PfxFilePath
        {
            get => _pfxFilePath;
            set => _pfxFilePath = value;
        }

        public bool EnableDebugLogs
        {
            get => _enableDebugLogs;
            set => _enableDebugLogs = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Public Certificate Methods

        /// <summary>
        /// Valida que los certificados existan y sean válidos
        /// </summary>
        /// <returns>True si la validación fue exitosa</returns>
        public async Task<bool> ValidateCertificatesAsync()
        {
            try
            {
                string pfxPath = GetEffectivePfxPath();

#if UNITY_ANDROID && !UNITY_EDITOR
                if (string.IsNullOrEmpty(pfxPath))
                    pfxPath = _defaultPfxFileName;
                
                pfxPath = Path.GetFileName(pfxPath);
                
                var testBytes = await LoadCertificateBytesAsync(pfxPath);
                if (testBytes == null)
                {
                    LogError($"Certificate file not found in StreamingAssets: {pfxPath}");
                    return false;
                }
#else
                if (string.IsNullOrEmpty(pfxPath) || !File.Exists(pfxPath))
                {
                    LogError("Certificate file not found or path is empty");
                    return false;
                }

                if (!pfxPath.EndsWith(".pfx"))
                {
                    LogError("Certificate file must be a .pfx file");
                    return false;
                }
#endif

                _pfxFilePath = pfxPath;
                Log("Certificate validation successful");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Certificate validation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Carga los bytes de un certificado desde el archivo especificado
        /// </summary>
        /// <param name="pfxPath">Ruta del archivo PFX</param>
        /// <returns>Bytes del certificado o null si falla</returns>
        public async Task<byte[]> LoadCertificateBytesAsync(string pfxPath = null)
        {
            try
            {
                string effectivePath = pfxPath ?? GetEffectivePfxPath();

#if UNITY_ANDROID && !UNITY_EDITOR
                string streamingPath = Path.Combine(Application.streamingAssetsPath, Path.GetFileName(effectivePath));
                
                using (var request = UnityEngine.Networking.UnityWebRequest.Get(streamingPath))
                {
                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Delay(50);
                    }
                    
                    if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        Log($"Certificate loaded successfully. Size: {request.downloadHandler.data.Length} bytes");
                        return request.downloadHandler.data;
                    }
                    else
                    {
                        LogError($"Failed to load certificate: {request.error}");
                        return null;
                    }
                }
#else
                if (File.Exists(effectivePath))
                {
                    var bytes = await File.ReadAllBytesAsync(effectivePath);
                    Log($"Certificate loaded successfully from: {effectivePath}");
                    return bytes;
                }
                else
                {
                    LogError($"Certificate file not found: {effectivePath}");
                    return null;
                }
#endif
            }
            catch (Exception ex)
            {
                LogError($"Error loading certificate: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Crea un certificado X509Certificate2 desde los bytes proporcionados
        /// </summary>
        /// <param name="certBytes">Bytes del certificado</param>
        /// <param name="password">Contraseña del certificado (opcional)</param>
        /// <returns>Certificado X509Certificate2 o null si falla</returns>
        public X509Certificate2 CreateX509Certificate(byte[] certBytes, string password = "")
        {
            try
            {
                if (certBytes == null || certBytes.Length == 0)
                {
                    LogError("Certificate bytes are null or empty");
                    return null;
                }

                var certificate = new X509Certificate2(certBytes, password, X509KeyStorageFlags.Exportable);
                Log("X509Certificate2 created successfully");
                return certificate;
            }
            catch (Exception ex)
            {
                LogError($"Failed to create X509Certificate2: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Carga y crea un certificado X509Certificate2 en un solo paso
        /// </summary>
        /// <param name="pfxPath">Ruta del archivo PFX (opcional)</param>
        /// <param name="password">Contraseña del certificado (opcional)</param>
        /// <returns>Certificado X509Certificate2 o null si falla</returns>
        public async Task<X509Certificate2> LoadX509CertificateAsync(string pfxPath = null, string password = "")
        {
            try
            {
                var certBytes = await LoadCertificateBytesAsync(pfxPath);
                if (certBytes == null)
                    return null;

                return CreateX509Certificate(certBytes, password);
            }
            catch (Exception ex)
            {
                LogError($"Failed to load X509Certificate: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Valida un certificado del servidor
        /// </summary>
        /// <param name="certificate">Certificado a validar</param>
        /// <param name="chain">Cadena de certificados</param>
        /// <param name="sslPolicyErrors">Errores de política SSL</param>
        /// <returns>True si el certificado es válido</returns>
        public bool ValidateServerCertificate(X509Certificate certificate, X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            // Certificado válido sin errores
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            {
                Log("Server certificate validation: No errors");
                return true;
            }

            // Permitir certificados no confiables si está configurado
            if (_allowUntrustedCertificates &&
                sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateNotAvailable)
            {
                Log("Server certificate validation: Allowing untrusted certificate");
                return true;
            }

            // Permitir errores de cadena si está configurado
            if (_ignoreCertificateChainErrors &&
                sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors)
            {
                Log("Server certificate validation: Ignoring chain errors");
                return true;
            }

            LogError($"Server certificate validation failed: {sslPolicyErrors}");
            return false;
        }

        /// <summary>
        /// Obtiene información detallada de un certificado
        /// </summary>
        /// <param name="certificate">Certificado a analizar</param>
        /// <returns>Información del certificado como string</returns>
        public string GetCertificateInfo(X509Certificate2 certificate)
        {
            if (certificate == null)
                return "Certificate is null";

            try
            {
                return $"Subject: {certificate.Subject}\n" +
                       $"Issuer: {certificate.Issuer}\n" +
                       $"Serial Number: {certificate.SerialNumber}\n" +
                       $"Not Before: {certificate.NotBefore}\n" +
                       $"Not After: {certificate.NotAfter}\n" +
                       $"Thumbprint: {certificate.Thumbprint}\n" +
                       $"Has Private Key: {certificate.HasPrivateKey}";
            }
            catch (Exception ex)
            {
                return $"Error getting certificate info: {ex.Message}";
            }
        }

        #endregion

        #region Private Helper Methods

        private string GetEffectivePfxPath()
        {
            if (!string.IsNullOrEmpty(_pfxFilePath))
                return _pfxFilePath;

            if (!string.IsNullOrEmpty(_clientCertPath))
                return _clientCertPath;

            return _defaultPfxFileName;
        }

        private void Log(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[CertificateManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[CertificateManager] {message}");
        }

        #endregion

        #region Public Configuration Methods

        /// <summary>
        /// Configura todas las rutas de certificados de una vez
        /// </summary>
        /// <param name="caPath">Ruta del CA</param>
        /// <param name="certPath">Ruta del certificado del cliente</param>
        /// <param name="keyPath">Ruta de la clave privada</param>
        /// <param name="pfxPath">Ruta del archivo PFX</param>
        public void SetCertificatePaths(string caPath = null, string certPath = null, string keyPath = null,
            string pfxPath = null)
        {
            if (!string.IsNullOrEmpty(caPath))
                _caFilePath = caPath;

            if (!string.IsNullOrEmpty(certPath))
                _clientCertPath = certPath;

            if (!string.IsNullOrEmpty(keyPath))
                _clientKeyPath = keyPath;

            if (!string.IsNullOrEmpty(pfxPath))
                _pfxFilePath = pfxPath;

            Log("Certificate paths updated");
        }

        /// <summary>
        /// Configura las opciones de validación de certificados
        /// </summary>
        /// <param name="allowUntrusted">Permitir certificados no confiables</param>
        /// <param name="ignoreChainErrors">Ignorar errores de cadena</param>
        /// <param name="ignoreRevocationErrors">Ignorar errores de revocación</param>
        public void SetValidationOptions(bool allowUntrusted, bool ignoreChainErrors, bool ignoreRevocationErrors)
        {
            _allowUntrustedCertificates = allowUntrusted;
            _ignoreCertificateChainErrors = ignoreChainErrors;
            _ignoreCertificateRevocationErrors = ignoreRevocationErrors;

            Log("Certificate validation options updated");
        }

        #endregion
    }
}