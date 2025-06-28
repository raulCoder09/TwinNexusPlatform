using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using _Scripts.Models.MQTTManagement;
using _Scripts.Models.CertificateManagement;
using _Scripts.Models.FileManagement;
using _Scripts.Models.Mqtt;

namespace _Scripts.devices
{
    public class TNPSGA52 : MonoBehaviour
    {
        [Header("Thing Configuration")]
        [SerializeField] private string thingName = "TNPSGA52";
        [SerializeField] private string thingDescription = "Twin Nexus Platform Device using Managers System";
        
        [Header("MQTT Topics")]
        [SerializeField] private string telemetryTopic = "tnp/TNPSGA52/telemetry";
        [SerializeField] private string commandsTopic = "tnp/TNPSGA52/commands";
        [SerializeField] private string statusTopic = "tnp/TNPSGA52/status";
        [SerializeField] private string shadowUpdateTopic = "$aws/things/TNPSGA52/shadow/update";
        
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool logReceivedMessages = true;
        
        // Input Actions - Mismas teclas que MqttxUnity
        private InputAction connectAction;
        private InputAction disconnectAction;
        private InputAction publishAction;
        private InputAction subscribeAction;
        private InputAction statusAction;
        private InputAction testCertAction;
        
        // State tracking
        private bool isSubscribedToCommands = false;
        private int messageCounter = 0;
        private bool managersInitialized = false;

        private void Awake()
        {
            LogDebug($"🚀 Initializing {thingName} using Managers System...");
            InitializeManagers();
        }

        private void OnEnable()
        {
            SetupInputActions();
            SubscribeToManagerEvents();
        }

        private void OnDisable()
        {
            DisableInputActions();
            UnsubscribeFromManagerEvents();
        }

        private async void OnDestroy()
        {
            await DisconnectFromAWS();
        }

        #region Manager Initialization

        private void InitializeManagers()
        {
            try
            {
                LogDebug("🔄 Initializing all managers...");
                
                // ✅ Inicializar FileManager
                if (!FileManager.Instance.Initialize())
                {
                    LogError("❌ Failed to initialize FileManager");
                    return;
                }
                LogDebug("✅ FileManager initialized");

                // ✅ Inicializar CertificateManager
                if (!CertificateManager.Instance.IsInitialized())
                {
                    CertificateManager.Instance.Initialize();
                }
                LogDebug("✅ CertificateManager initialized");

                // ✅ Inicializar MqttManager con configuración AWS IoT Core
                if (MqttManager.Instance != null)
                {
                    ConfigureMqttManager();
                    MqttManager.Instance.Initialize();
                    LogDebug("✅ MqttManager initialized with AWS IoT Core configuration");
                }
                else
                {
                    LogError("❌ MqttManager instance not available");
                    return;
                }

                managersInitialized = true;
                LogDebug("🎉 All managers initialized successfully!");
                
                // Mostrar configuración
                ShowManagersConfiguration();
            }
            catch (Exception ex)
            {
                LogError($"❌ Manager initialization error: {ex.Message}");
            }
        }

        private void ConfigureMqttManager()
        {
            // ✅ La configuración ya está en el MqttManager arreglado,
            // pero podemos verificar o ajustar desde aquí si es necesario
            var mqttManager = MqttManager.Instance;
            
            LogDebug("🔧 Verifying MQTT Manager configuration...");
            LogDebug($"  Broker: {mqttManager.BrokerAddress}");
            LogDebug($"  Port: {mqttManager.BrokerPort}");
            LogDebug($"  SSL: {mqttManager.UseSSL}");
            
            // ✅ Si necesitas ajustar algo específico, hazlo aquí
            // mqttManager.BrokerAddress = "tu-endpoint-específico";
        }

        private void ShowManagersConfiguration()
        {
            LogDebug("=== MANAGERS CONFIGURATION ===");
            
            // FileManager info
            var storageInfo = FileManager.Instance.GetStorageInfo();
            LogDebug($"📁 FileManager: {(storageInfo.isInitialized ? "Ready" : "Not Ready")}");
            LogDebug($"   Base Path: {storageInfo.basePath}");
            LogDebug($"   Available Categories: {storageInfo.availableCategories.Count}");
            
            // CertificateManager info
            LogDebug($"🔐 CertificateManager: {(CertificateManager.Instance.IsInitialized() ? "Ready" : "Not Ready")}");
            
            // MqttManager info
            var connectionStatus = MqttManager.Instance.GetConnectionStatus();
            LogDebug($"📡 MqttManager: Ready");
            LogDebug($"   Broker: {connectionStatus.brokerAddress}:{connectionStatus.brokerPort}");
            LogDebug($"   Client ID: {connectionStatus.clientId}");
            LogDebug($"   Connected: {connectionStatus.isConnected}");
            
            LogDebug("==============================");
        }

        #endregion

        #region Input Actions

        private void SetupInputActions()
        {
            // ✅ Mismas teclas que MqttxUnity para consistencia
            connectAction = new InputAction("Connect", InputActionType.Button, "<Keyboard>/c");
            disconnectAction = new InputAction("Disconnect", InputActionType.Button, "<Keyboard>/d");
            publishAction = new InputAction("Publish", InputActionType.Button, "<Keyboard>/p");
            subscribeAction = new InputAction("Subscribe", InputActionType.Button, "<Keyboard>/s");
            statusAction = new InputAction("Status", InputActionType.Button, "<Keyboard>/i");
            testCertAction = new InputAction("TestCert", InputActionType.Button, "<Keyboard>/t");

            // Bind actions
            connectAction.performed += _ => ConnectToAWS();
            disconnectAction.performed += _ => DisconnectFromAWS();
            publishAction.performed += _ => PublishTelemetryData();
            subscribeAction.performed += _ => SubscribeToCommands();
            statusAction.performed += _ => ShowSystemStatus();
            testCertAction.performed += _ => TestCertificateSystem();

            // Enable actions
            connectAction.Enable();
            disconnectAction.Enable();
            publishAction.Enable();
            subscribeAction.Enable();
            statusAction.Enable();
            testCertAction.Enable();

            LogDebug("=== TNPSGA52 CONTROLS (Using Managers) ===");
            LogDebug("[C] Connect | [D] Disconnect | [P] Publish | [S] Subscribe");
            LogDebug("[I] System Status | [T] Test Certificate System");
            LogDebug("=========================================");
        }

        private void DisableInputActions()
        {
            connectAction?.Disable();
            disconnectAction?.Disable();
            publishAction?.Disable();
            subscribeAction?.Disable();
            statusAction?.Disable();
            testCertAction?.Disable();
        }

        #endregion

        #region Manager Events

        private void SubscribeToManagerEvents()
        {
            if (MqttManager.Instance != null)
            {
                MqttManager.Instance.SubscribeToConnected(OnMqttConnected);
                MqttManager.Instance.SubscribeToDisconnected(OnMqttDisconnected);
                MqttManager.Instance.SubscribeToMessageReceived(OnMqttMessageReceived);
                MqttManager.Instance.SubscribeToSubscribed(OnMqttSubscribed);
                MqttManager.Instance.SubscribeToConnectionFailed(OnMqttConnectionFailed);
                LogDebug("✅ Subscribed to MQTT Manager events");
            }

            if (CertificateManager.Instance != null)
            {
                CertificateManager.Instance.SubscribeToCertificateLoaded(OnCertificateLoaded);
                CertificateManager.Instance.SubscribeToValidationFailed(OnCertificateValidationFailed);
                LogDebug("✅ Subscribed to Certificate Manager events");
            }
        }

        private void UnsubscribeFromManagerEvents()
        {
            if (MqttManager.Instance != null)
            {
                MqttManager.Instance.UnsubscribeFromConnected(OnMqttConnected);
                MqttManager.Instance.UnsubscribeFromDisconnected(OnMqttDisconnected);
                MqttManager.Instance.UnsubscribeFromMessageReceived(OnMqttMessageReceived);
                MqttManager.Instance.UnsubscribeFromSubscribed(OnMqttSubscribed);
                MqttManager.Instance.UnsubscribeFromConnectionFailed(OnMqttConnectionFailed);
            }

            if (CertificateManager.Instance != null)
            {
                CertificateManager.Instance.UnsubscribeFromCertificateLoaded(OnCertificateLoaded);
                CertificateManager.Instance.UnsubscribeFromValidationFailed(OnCertificateValidationFailed);
            }
        }

        // MQTT Events
        private void OnMqttConnected()
        {
            LogDebug("🎉 SUCCESS: Connected to AWS IoT Core using Managers System!");
        }

        private void OnMqttDisconnected(string reason)
        {
            LogDebug($"❌ Disconnected from AWS IoT Core. Reason: {reason}");
            isSubscribedToCommands = false;
        }

        private void OnMqttMessageReceived(string topic, string message)
        {
            if (logReceivedMessages)
            {
                LogDebug($"📨 Message received on '{topic}': {message}");
                
                // Handle different message types
                if (topic.Contains("commands"))
                {
                    HandleCommandMessage(topic, message);
                }
                else if (topic.Contains("shadow"))
                {
                    HandleShadowMessage(topic, message);
                }
            }
        }

        private void OnMqttSubscribed(string topic)
        {
            LogDebug($"📡 Subscribed to topic: {topic}");
            if (topic == commandsTopic)
            {
                isSubscribedToCommands = true;
            }
        }

        private void OnMqttConnectionFailed(string error)
        {
            LogError($"❌ MQTT Connection failed: {error}");
        }

        // Certificate Events
        private void OnCertificateLoaded(string pfxPath, _Scripts.Models.CertificateManagement.CertificateInfo certificateInfo)
        {
            LogDebug($"🔐 Certificate loaded: {pfxPath}");
            LogDebug($"   Subject: {certificateInfo.Subject}");
            LogDebug($"   Valid until: {certificateInfo.NotAfter}");
            LogDebug($"   Has Private Key: {certificateInfo.HasPrivateKey}");
        }

        private void OnCertificateValidationFailed(string pfxPath, string errorMessage)
        {
            LogError($"❌ Certificate validation failed: {pfxPath} - {errorMessage}");
        }

        #endregion

        #region Connection Methods

        private async void ConnectToAWS()
        {
            if (!managersInitialized)
            {
                LogError("❌ Managers not initialized");
                return;
            }

            LogDebug("🔄 Connecting to AWS IoT Core using Managers System...");
            
            try
            {
                bool success = await MqttManager.Instance.ConnectAsync();
                if (success)
                {
                    LogDebug("✅ Connection attempt completed using managers");
                }
                else
                {
                    LogError("❌ Connection attempt failed");
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Connection error: {ex.Message}");
            }
        }

        private async Task DisconnectFromAWS()
        {
            LogDebug("🔄 Disconnecting from AWS IoT Core...");
            await DisconnectAsync();
        }

        private async Task DisconnectAsync()
        {
            try
            {
                if (MqttManager.Instance != null)
                {
                    bool success = await MqttManager.Instance.DisconnectAsync();
                    LogDebug(success ? "✅ Disconnected successfully" : "❌ Disconnect failed");
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Disconnect error: {ex.Message}");
            }
        }

        #endregion

        #region MQTT Operations

        private async void SubscribeToCommands()
        {
            if (!MqttManager.Instance.IsConnected())
            {
                LogError("❌ Cannot subscribe: Not connected");
                return;
            }

            LogDebug($"📡 Subscribing to commands: {commandsTopic}");
            bool success = await MqttManager.Instance.SubscribeToTopicAsync(commandsTopic, MqttInfo.QoSLevel.AtLeastOnce);
            LogDebug(success ? "✅ Subscribed to commands" : "❌ Failed to subscribe");
        }

        private async void PublishTelemetryData()
        {
            if (!MqttManager.Instance.IsConnected())
            {
                LogError("❌ Cannot publish: Not connected");
                return;
            }

            try
            {
                messageCounter++;
                
                // ✅ Crear telemetría usando el mismo formato que MqttxUnity
                var telemetryData = new TelemetryMessage
                {
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    message = $"Telemetry from {thingName} using Managers System! Message #{messageCounter}",
                    thingName = thingName,
                    messageId = messageCounter,
                    source = "Unity-Managers-System",
                    platform = Application.platform.ToString(),
                    data = new SensorData
                    {
                        temperature = UnityEngine.Random.Range(20f, 30f),
                        humidity = UnityEngine.Random.Range(40f, 60f),
                        battery = UnityEngine.Random.Range(75f, 100f),
                        status = "active"
                    },
                    deviceInfo = new DeviceInfo
                    {
                        model = SystemInfo.deviceModel,
                        os = SystemInfo.operatingSystem,
                        processor = SystemInfo.processorType,
                        memory = SystemInfo.systemMemorySize
                    }
                };

                LogDebug($"📤 Publishing telemetry to: {telemetryTopic}");
                bool success = await MqttManager.Instance.PublishObjectAsync(telemetryTopic, telemetryData, MqttInfo.QoSLevel.AtLeastOnce);
                LogDebug(success ? "✅ Telemetry published using managers" : "❌ Failed to publish telemetry");
            }
            catch (Exception ex)
            {
                LogError($"❌ Publish error: {ex.Message}");
            }
        }

        #endregion

        #region Message Handlers

        private void HandleCommandMessage(string topic, string message)
        {
            LogDebug($"🎯 Processing command from managers system: {message}");
            // TODO: Parse and execute commands
        }

        private void HandleShadowMessage(string topic, string message)
        {
            LogDebug($"👤 Processing shadow update from managers system: {message}");
            // TODO: Parse and handle shadow updates
        }

        #endregion

        #region System Testing

        private async void TestCertificateSystem()
        {
            LogDebug("🔍 Testing Certificate System using Managers...");
            
            try
            {
                // ✅ Probar el sistema completo de certificados
                bool validated = await CertificateManager.Instance.ValidateCertificatesAsync(
                    "aws-iot-core.pfx", 
                    StorageInfo.StorageCategory.Resources);
                    
                if (validated)
                {
                    LogDebug("✅ Certificate validation successful using managers");
                    
                    // Cargar el certificado
                    var certificate = await CertificateManager.Instance.LoadX509CertificateAsync(
                        "aws-iot-core.pfx", 
                        "5859", 
                        StorageInfo.StorageCategory.Resources);
                        
                    if (certificate != null)
                    {
                        LogDebug("✅ Certificate loaded successfully using managers");
                        string info = CertificateManager.Instance.GetCertificateInfo(certificate);
                        LogDebug($"Certificate Info:\n{info}");
                    }
                    else
                    {
                        LogError("❌ Failed to load certificate using managers");
                    }
                }
                else
                {
                    LogError("❌ Certificate validation failed using managers");
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Certificate test error: {ex.Message}");
            }
        }

        private void ShowSystemStatus()
        {
            LogDebug("=== TNPSGA52 SYSTEM STATUS (Using Managers) ===");
            
            // Manager status
            LogDebug($"📁 FileManager: {(FileManager.Instance.GetStorageInfo().isInitialized ? "Ready" : "Not Ready")}");
            LogDebug($"🔐 CertificateManager: {(CertificateManager.Instance.IsInitialized() ? "Ready" : "Not Ready")}");
            
            // MQTT status
            if (MqttManager.Instance != null)
            {
                var status = MqttManager.Instance.GetConnectionStatus();
                LogDebug($"📡 MQTT Connected: {status.isConnected}");
                LogDebug($"📡 Broker: {status.brokerAddress}:{status.brokerPort}");
                LogDebug($"📡 Client ID: {status.clientId}");
            }
            
            // Thing status
            LogDebug($"🏷️ Thing Name: {thingName}");
            LogDebug($"📨 Messages Sent: {messageCounter}");
            LogDebug($"📡 Subscribed to Commands: {isSubscribedToCommands}");
            LogDebug($"📊 Topics:");
            LogDebug($"   Telemetry: {telemetryTopic}");
            LogDebug($"   Commands: {commandsTopic}");
            LogDebug($"   Status: {statusTopic}");
            
            LogDebug("============================================");
        }

        #endregion

        #region Serializable Classes

        [System.Serializable]
        public class TelemetryMessage
        {
            public string timestamp;
            public string message;
            public string thingName;
            public int messageId;
            public string source;
            public string platform;
            public SensorData data;
            public DeviceInfo deviceInfo;
        }

        [System.Serializable]
        public class SensorData
        {
            public float temperature;
            public float humidity;
            public float battery;
            public string status;
        }

        [System.Serializable]
        public class DeviceInfo
        {
            public string model;
            public string os;
            public string processor;
            public int memory;
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[{thingName}] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[{thingName}] {message}");
        }

        #endregion
    }
}