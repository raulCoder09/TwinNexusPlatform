using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using UnityEngine;
using _Scripts.Models.FileManagement;

namespace _Scripts.Models.CertificateManagement
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

        [Header("Certificate Configuration")]
        [SerializeField] private string defaultPfxFileName = "aws-iot-core.pfx";
        [SerializeField] private bool allowUntrustedCertificates = false;
        [SerializeField] private bool ignoreCertificateChainErrors = false;
        [SerializeField] private bool ignoreCertificateRevocationErrors = true;
        [SerializeField] private bool enableDebugLogs = true;

        private CertificateLoader certificateLoader;
        private CertificateValidator certificateValidator;
        private CertificateEventManager eventManager;
        private bool isInitialized = false;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            try
            {
                if (isInitialized)
                {
                    LogDebug("CertificateManager already initialized");
                    return;
                }

                LogDebug("Initializing CertificateManager...");
                eventManager = new CertificateEventManager();
                certificateLoader = new CertificateLoader(FileManager.Instance, eventManager);
                certificateValidator = new CertificateValidator(eventManager, allowUntrustedCertificates, 
                    ignoreCertificateChainErrors, ignoreCertificateRevocationErrors);
                isInitialized = true;
                eventManager.TriggerStorageInitialized(true, "Initialization successful");
                LogDebug("CertificateManager initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"Initialization error: {ex.Message}");
                eventManager.TriggerStorageInitialized(false, ex.Message);
            }
        }

        public bool IsInitialized()
        {
            return isInitialized;
        }

        public async Task<bool> ValidateCertificatesAsync(string pfxPath = null, StorageInfo.StorageCategory category = StorageInfo.StorageCategory.Certificates)
        {
            if (!isInitialized) Initialize();
            pfxPath = string.IsNullOrEmpty(pfxPath) ? defaultPfxFileName : pfxPath;
            return await certificateValidator.ValidateCertificatesAsync(pfxPath, category);
        }

        public async Task<X509Certificate2> LoadX509CertificateAsync(string pfxPath = null, string password = "", StorageInfo.StorageCategory category = StorageInfo.StorageCategory.Certificates)
        {
            if (!isInitialized) Initialize();
            pfxPath = string.IsNullOrEmpty(pfxPath) ? defaultPfxFileName : pfxPath;
            return await certificateLoader.LoadX509CertificateAsync(pfxPath, category, password);
        }

        public bool ValidateServerCertificate(X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            if (!isInitialized) Initialize();
            return certificateValidator.ValidateServerCertificate(certificate, chain, sslPolicyErrors);
        }

        public string GetCertificateInfo(X509Certificate2 certificate)
        {
            if (!isInitialized) Initialize();
            if (certificate == null)
            {
                LogError("Certificate is null");
                return "Certificate is null";
            }
            return CertificateInfo.FromCertificate(certificate).GetCertificateInfo();
        }

        public void SetValidationOptions(bool allowUntrusted, bool ignoreChainErrors, bool ignoreRevocationErrors)
        {
            allowUntrustedCertificates = allowUntrusted;
            ignoreCertificateChainErrors = ignoreChainErrors;
            ignoreCertificateRevocationErrors = ignoreRevocationErrors;
            certificateValidator = new CertificateValidator(eventManager, allowUntrustedCertificates, 
                ignoreCertificateChainErrors, ignoreCertificateRevocationErrors);
            LogDebug("Certificate validation options updated");
        }

        public void SubscribeToCertificateLoaded(Action<string, CertificateInfo> callback)
        {
            if (!isInitialized) Initialize();
            eventManager.SubscribeToCertificateLoaded(callback);
        }

        public void UnsubscribeFromCertificateLoaded(Action<string, CertificateInfo> callback)
        {
            if (!isInitialized) Initialize();
            eventManager.UnsubscribeFromCertificateLoaded(callback);
        }

        public void SubscribeToCertificateValidated(Action<string, bool> callback)
        {
            if (!isInitialized) Initialize();
            eventManager.SubscribeToCertificateValidated(callback);
        }

        public void UnsubscribeFromCertificateValidated(Action<string, bool> callback)
        {
            if (!isInitialized) Initialize();
            eventManager.UnsubscribeFromCertificateValidated(callback);
        }

        public void SubscribeToValidationFailed(Action<string, string> callback)
        {
            if (!isInitialized) Initialize();
            eventManager.SubscribeToValidationFailed(callback);
        }

        public void UnsubscribeFromValidationFailed(Action<string, string> callback)
        {
            if (!isInitialized) Initialize();
            eventManager.UnsubscribeFromValidationFailed(callback);
        }

        public void SubscribeToServerCertificateValidated(Action<bool, string> callback)
        {
            if (!isInitialized) Initialize();
            eventManager.SubscribeToServerCertificateValidated(callback);
        }

        public void UnsubscribeFromServerCertificateValidated(Action<bool, string> callback)
        {
            if (!isInitialized) Initialize();
            eventManager.UnsubscribeFromServerCertificateValidated(callback);
        }

        public void SubscribeToStorageInitialized(Action<bool, string> callback)
        {
            if (!isInitialized) Initialize();
            eventManager.SubscribeToStorageInitialized(callback);
        }

        public void UnsubscribeFromStorageInitialized(Action<bool, string> callback)
        {
            if (!isInitialized) Initialize();
            eventManager.UnsubscribeFromStorageInitialized(callback);
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[CertificateManager] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[CertificateManager] {message}");
        }
    }
}