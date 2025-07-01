namespace _Scripts.Models.CertificateManagement
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using UnityEngine;
    using _Scripts.Models.FileManagement;

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

        private CertificateLoader _certificateLoader;
        private CertificateValidator _certificateValidator;
        private CertificateEventManager _eventManager;
        private FileManager _fileManager;
        private bool _isInitialized = false;

        private void Start()
        {
            Initialize();
        }

        public bool Initialize()
        {
            try
            {
                if (_isInitialized)
                {
                    LogDebug("CertificateManager already initialized");
                    return true;
                }

                LogDebug("Initializing CertificateManager...");
                _fileManager = FileManager.Instance;
                _eventManager = new CertificateEventManager();
                _certificateLoader = new CertificateLoader(_fileManager, _eventManager);
                _certificateValidator = new CertificateValidator(_fileManager, _eventManager, allowUntrustedCertificates,
                    ignoreCertificateChainErrors, ignoreCertificateRevocationErrors);

                _isInitialized = true;
                LogDebug("CertificateManager initialized successfully");
                _eventManager.TriggerStorageInitialized(true, "Initialization successful");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Initialization error: {ex.Message}");
                _eventManager.TriggerStorageInitialized(false, ex.Message);
                return false;
            }
        }

        public async Task<X509Certificate2> LoadX509CertificateAsync(string path, string fileName = null, string password = "")
        {
            if (!_isInitialized) Initialize();
            fileName = string.IsNullOrEmpty(fileName) ? defaultPfxFileName : fileName;
            return await _certificateLoader.LoadX509CertificateAsync(path, fileName, password, info => { });
        }

        public async Task<bool> ValidateCertificateAsync(string path, string fileName = null)
        {
            if (!_isInitialized) Initialize();
            fileName = string.IsNullOrEmpty(fileName) ? defaultPfxFileName : fileName;
            return await _certificateValidator.ValidateCertificateAsync(path, fileName, success => { });
        }

        public bool ValidateServerCertificate(X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            if (!_isInitialized) Initialize();
            return _certificateValidator.ValidateServerCertificate(certificate, chain, sslPolicyErrors);
        }

        public string GetCertificateInfo(X509Certificate2 certificate)
        {
            if (!_isInitialized) Initialize();
            return _certificateLoader.GetCertificateInfo(certificate);
        }

        public void SetValidationOptions(bool allowUntrusted, bool ignoreChainErrors, bool ignoreRevocationErrors)
        {
            if (!_isInitialized) Initialize();
            allowUntrustedCertificates = allowUntrusted;
            ignoreCertificateChainErrors = ignoreChainErrors;
            ignoreCertificateRevocationErrors = ignoreRevocationErrors;
            _certificateValidator = new CertificateValidator(_fileManager, _eventManager, allowUntrustedCertificates,
                ignoreCertificateChainErrors, ignoreCertificateRevocationErrors);
            LogDebug("Certificate validation options updated");
        }

        public void SubscribeToCertificateLoaded(Action<string, CertificateInfo> callback)
        {
            if (!_isInitialized) Initialize();
            _eventManager.SubscribeToCertificateLoaded(callback);
        }

        public void UnsubscribeFromCertificateLoaded(Action<string, CertificateInfo> callback)
        {
            if (!_isInitialized) Initialize();
            _eventManager.UnsubscribeFromCertificateLoaded(callback);
        }

        public void SubscribeToCertificateValidated(Action<string, bool> callback)
        {
            if (!_isInitialized) Initialize();
            _eventManager.SubscribeToCertificateValidated(callback);
        }

        public void UnsubscribeFromCertificateValidated(Action<string, bool> callback)
        {
            if (!_isInitialized) Initialize();
            _eventManager.UnsubscribeFromCertificateValidated(callback);
        }

        public void SubscribeToValidationFailed(Action<string, string> callback)
        {
            if (!_isInitialized) Initialize();
            _eventManager.SubscribeToValidationFailed(callback);
        }

        public void UnsubscribeFromValidationFailed(Action<string, string> callback)
        {
            if (!_isInitialized) Initialize();
            _eventManager.UnsubscribeFromValidationFailed(callback);
        }

        public void SubscribeToServerCertificateValidated(Action<bool, string> callback)
        {
            if (!_isInitialized) Initialize();
            _eventManager.SubscribeToServerCertificateValidated(callback);
        }

        public void UnsubscribeFromServerCertificateValidated(Action<bool, string> callback)
        {
            if (!_isInitialized) Initialize();
            _eventManager.UnsubscribeFromServerCertificateValidated(callback);
        }

        public void SubscribeToStorageInitialized(Action<bool, string> callback)
        {
            if (!_isInitialized) Initialize();
            _eventManager.SubscribeToStorageInitialized(callback);
        }

        public void UnsubscribeFromStorageInitialized(Action<bool, string> callback)
        {
            if (!_isInitialized) Initialize();
            _eventManager.UnsubscribeFromStorageInitialized(callback);
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