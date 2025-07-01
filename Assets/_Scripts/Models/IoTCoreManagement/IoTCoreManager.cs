using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Amazon;
using Amazon.Runtime;

namespace _Scripts.Models.IoTCoreManagement
{
    public class IoTCoreManager : MonoBehaviour
    {
        #region Singleton Pattern
        private static IoTCoreManager _instance;
        private static readonly object _lock = new object();

        public static IoTCoreManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            GameObject iotObject = new GameObject("IoTCoreManager");
                            _instance = iotObject.AddComponent<IoTCoreManager>();
                            DontDestroyOnLoad(iotObject);
                        }
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
                LogDebug("Another IoTCoreManager instance exists. Destroying this one.");
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                clientHandler?.DisposeClient();
                _instance = null;
            }
        }
        #endregion

        [Header("AWS IoT Core Configuration")]
        [SerializeField] private string domainName = "d06815421so7uq8jzy8ln-ats.iot.us-east-1.amazonaws.com";
        [SerializeField] private string domainArn = "arn:aws:iot:us-east-1:156041417101:domainconfiguration/TwinNexusPlatformDomainConfiguration/axnbn";
        [SerializeField] private string newThingName = "Unity-Test-Device";
        [SerializeField] private string newThingType = "";
        [SerializeField] private string thingNameToGet = "";
        [SerializeField] private bool setAsActive = true;
        [SerializeField] private string certificateIdToAttach = "";
        [SerializeField] private string thingNameForCertificate = "";
        [SerializeField] private string newPolicyName = "Unity-IoT-Policy";
        [SerializeField] private string policyDocument = "";
        [SerializeField] private string policyNameToAttach = "";
        [SerializeField] private string certificateIdForPolicy = "";
        [SerializeField] private bool enableDebugLogs = true;

        private IoTInfo ioTInfo;
        private IoTClientHandler clientHandler;
        private IoTEventManager eventManager;
        private IoTThingHandler thingHandler;
        private IoTCertificateHandler certificateHandler;
        private IoTPolicyHandler policyHandler;
        private bool _isInitialized = false;

        // Evento para notificar la inicialización
        public event Action<bool, string> OnInitializationCompleted;

        public string DomainName => domainName;
        public string DomainArn => domainArn;

        public async Task<bool> InitializeAsync(AWSCredentials credentials, RegionEndpoint regionEndpoint, string accountId)
        {
            if (_isInitialized) return true;

            try
            {
                LogDebug("Initializing IoTCoreManager...");
                
                ioTInfo = new IoTInfo();
                eventManager = new IoTEventManager();
                clientHandler = new IoTClientHandler();
                thingHandler = new IoTThingHandler(clientHandler, eventManager, ioTInfo);
                certificateHandler = new IoTCertificateHandler(clientHandler, eventManager);
                policyHandler = new IoTPolicyHandler(clientHandler, eventManager, ioTInfo);

                // Inicializar el cliente IoT con las credenciales proporcionadas
                if (!await clientHandler.InitializeIoTClientAsync(credentials, regionEndpoint))
                {
                    LogError("Failed to initialize IoT client");
                    OnInitializationCompleted?.Invoke(false, "Failed to initialize IoT client");
                    return false;
                }

                _isInitialized = true;
                LogDebug("IoTCoreManager initialized successfully");
                OnInitializationCompleted?.Invoke(true, "IoTCoreManager initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Initialization error: {ex.Message}");
                OnInitializationCompleted?.Invoke(false, ex.Message);
                return false;
            }
        }

        public async Task<bool> TestIoTConnectivityAsync()
        {
            if (!_isInitialized) await InitializeAsync(null, null, null); // Usar valores por defecto si no inicializado
            try
            {
                LogDebug("Testing IoT Core connectivity...");
                var things = await thingHandler.ListThingsAsync();
                LogDebug($"IoT Core connectivity test {(things.Count >= 0 ? "successful" : "failed")}");
                return things.Count >= 0;
            }
            catch (Exception ex)
            {
                LogError($"IoT connectivity test failed: {ex.Message}");
                return false;
            }
        }

        public async Task<List<IoTInfo.ThingInfo>> ListThingsAsync()
        {
            if (!_isInitialized) await InitializeAsync(null, null, null); // Usar valores por defecto si no inicializado
            return await thingHandler.ListThingsAsync();
        }

        public async Task<bool> CreateThingAsync(string thingName = null, string thingType = null)
        {
            if (!_isInitialized) await InitializeAsync(null, null, null); // Usar valores por defecto si no inicializado
            return await thingHandler.CreateThingAsync(thingName ?? newThingName, thingType ?? newThingType);
        }

        public async Task<IoTInfo.ThingInfo> GetThingAsync(string thingName = null)
        {
            if (!_isInitialized) await InitializeAsync(null, null, null); // Usar valores por defecto si no inicializado
            return await thingHandler.GetThingAsync(thingName ?? thingNameToGet);
        }

        public async Task<IoTInfo.CertificateData> CreateThingCertificateAsync()
        {
            if (!_isInitialized) await InitializeAsync(null, null, null); // Usar valores por defecto si no inicializado
            return await certificateHandler.CreateThingCertificateAsync();
        }

        public async Task<bool> AttachCertificateToThingAsync(string certificateId = null, string thingName = null)
        {
            if (!_isInitialized) await InitializeAsync(null, null, null); // Usar valores por defecto si no inicializado
            return await certificateHandler.AttachCertificateToThingAsync(certificateId ?? certificateIdToAttach, thingName ?? thingNameForCertificate);
        }

        public async Task<List<string>> ListThingCertificatesAsync(string thingName = null)
        {
            if (!_isInitialized) await InitializeAsync(null, null, null); // Usar valores por defecto si no inicializado
            return await certificateHandler.ListThingCertificatesAsync(thingName ?? thingNameForCertificate);
        }

        public async Task<bool> CreatePolicyAsync(string policyName = null, string customPolicyDocument = null)
        {
            if (!_isInitialized) await InitializeAsync(null, null, null); // Usar valores por defecto si no inicializado
            return await policyHandler.CreatePolicyAsync(policyName ?? newPolicyName, customPolicyDocument ?? policyDocument);
        }

        public async Task<bool> AttachPolicyAsync(string policyName = null, string certificateId = null)
        {
            if (!_isInitialized) await InitializeAsync(null, null, null); // Usar valores por defecto si no inicializado
            return await policyHandler.AttachPolicyAsync(policyName ?? policyNameToAttach, certificateId ?? certificateIdForPolicy);
        }

        public void SubscribeToThingsListed(Action<bool, string, List<IoTInfo.ThingInfo>> callback) => eventManager.SubscribeToThingsListed(callback);
        public void UnsubscribeFromThingsListed(Action<bool, string, List<IoTInfo.ThingInfo>> callback) => eventManager.UnsubscribeFromThingsListed(callback);
        public void SubscribeToThingCreated(Action<bool, string, string> callback) => eventManager.SubscribeToThingCreated(callback);
        public void UnsubscribeFromThingCreated(Action<bool, string, string> callback) => eventManager.UnsubscribeFromThingCreated(callback);
        public void SubscribeToThingRetrieved(Action<bool, string, IoTInfo.ThingInfo> callback) => eventManager.SubscribeToThingRetrieved(callback);
        public void UnsubscribeFromThingRetrieved(Action<bool, string, IoTInfo.ThingInfo> callback) => eventManager.UnsubscribeFromThingRetrieved(callback);
        public void SubscribeToCertificateCreated(Action<bool, string, IoTInfo.CertificateData> callback) => eventManager.SubscribeToCertificateCreated(callback);
        public void UnsubscribeFromCertificateCreated(Action<bool, string, IoTInfo.CertificateData> callback) => eventManager.UnsubscribeFromCertificateCreated(callback);
        public void SubscribeToCertificateAttached(Action<bool, string, string, string> callback) => eventManager.SubscribeToCertificateAttached(callback);
        public void UnsubscribeFromCertificateAttached(Action<bool, string, string, string> callback) => eventManager.UnsubscribeFromCertificateAttached(callback);
        public void SubscribeToThingCertificatesListed(Action<bool, string, string, List<string>> callback) => eventManager.SubscribeToThingCertificatesListed(callback);
        public void UnsubscribeFromThingCertificatesListed(Action<bool, string, string, List<string>> callback) => eventManager.UnsubscribeFromThingCertificatesListed(callback);
        public void SubscribeToPolicyCreated(Action<bool, string, string> callback) => eventManager.SubscribeToPolicyCreated(callback);
        public void UnsubscribeFromPolicyCreated(Action<bool, string, string> callback) => eventManager.UnsubscribeFromPolicyCreated(callback);
        public void SubscribeToPolicyAttached(Action<bool, string, string, string> callback) => eventManager.SubscribeToPolicyAttached(callback);
        public void UnsubscribeFromPolicyAttached(Action<bool, string, string, string> callback) => eventManager.UnsubscribeFromPolicyAttached(callback);

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[IoTCoreManager] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[IoTCoreManager] {message}");
        }
    }
}