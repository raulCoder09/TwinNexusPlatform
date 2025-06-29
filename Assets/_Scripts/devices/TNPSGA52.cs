using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using _Scripts.Models.MQTTManagement;
using _Scripts.Models.Mqtt;
using _Scripts.Models.CertificateManagement;
using _Scripts.Models.FileManagement;

namespace _Scripts.devices
{
    public class TNPSGA52 : MonoBehaviour
    {
        [Header("MQTT Configuration (Managed by MqttManager)")]
        [SerializeField] private string brokerAddress = "aqloxhiemdroo-ats.iot.us-east-1.amazonaws.com";
        [SerializeField] private int brokerPort = 8883;
        [SerializeField] private string clientId = "TNPSGA52";
        [SerializeField] private string testTopic = "tnp/TNPSGA52/test";
        [SerializeField] private bool enableDebugLogs = true;

        [Header("Certificate Test Configuration")]
        [SerializeField] private string pfxCertificateFile = "aws-iot-core.pfx";
        [SerializeField] private string pfxPassword = "5859";

        // Input Actions
        private InputAction connectAction;
        private InputAction disconnectAction;
        private InputAction publishAction;
        private InputAction subscribeAction;
        private InputAction statusAction;
        private InputAction testCertAction;
        private InputAction updateConfigAction;

        // Connection state
        private bool isConnected = false;
        private bool isSubscribed = false;
        private int messageCounter = 0;

        private MqttManager mqttManager;

        private void Awake()
        {
            LogDebug("Initializing TNPSGA52 - Using MqttManager");
            Initialize();
        }

        private void OnEnable()
        {
            SetupInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private async void OnDestroy()
        {
            await DisconnectAsync();
            UnsubscribeFromEvents();
        }

        #region Initialization

        private void Initialize()
        {
            try
            {
                mqttManager = MqttManager.Instance;
                mqttManager.Initialize();

                // Synchronize configuration with MqttManager
                mqttManager.BrokerAddress = brokerAddress;
                mqttManager.BrokerPort = brokerPort;
                // Note: ClientId is set in MqttManager's constructor, so we rely on its default or reinitialize if changed

                // Subscribe to MqttManager events
                mqttManager.SubscribeToConnected(OnMqttConnected);
                mqttManager.SubscribeToDisconnected(OnMqttDisconnected);
                mqttManager.SubscribeToMessageReceived(OnMessageReceived);
                mqttManager.SubscribeToSubscribed(OnTopicSubscribed);
                mqttManager.SubscribeToConnectionFailed(OnConnectionFailed);

                LogDebug("✅ MqttManager initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"❌ Failed to initialize: {ex.Message}");
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (mqttManager != null)
            {
                mqttManager.UnsubscribeFromConnected(OnMqttConnected);
                mqttManager.UnsubscribeFromDisconnected(OnMqttDisconnected);
                mqttManager.UnsubscribeFromMessageReceived(OnMessageReceived);
                mqttManager.UnsubscribeFromSubscribed(OnTopicSubscribed);
                mqttManager.UnsubscribeFromConnectionFailed(OnConnectionFailed);
            }
        }

        #endregion

        #region Input Actions

        private void SetupInputActions()
        {
            connectAction = new InputAction("Connect", InputActionType.Button, "<Keyboard>/c");
            disconnectAction = new InputAction("Disconnect", InputActionType.Button, "<Keyboard>/d");
            publishAction = new InputAction("Publish", InputActionType.Button, "<Keyboard>/p");
            subscribeAction = new InputAction("Subscribe", InputActionType.Button, "<Keyboard>/s");
            statusAction = new InputAction("Status", InputActionType.Button, "<Keyboard>/i");
            testCertAction = new InputAction("TestCert", InputActionType.Button, "<Keyboard>/t");
            updateConfigAction = new InputAction("UpdateConfig", InputActionType.Button, "<Keyboard>/u");

            connectAction.performed += _ => ConnectToAWS();
            disconnectAction.performed += _ => DisconnectFromAWS();
            publishAction.performed += _ => PublishTestMessage();
            subscribeAction.performed += _ => SubscribeToTestTopic();
            statusAction.performed += _ => ShowStatus();
            testCertAction.performed += _ => TestCertificateLoading();
            updateConfigAction.performed += _ => UpdateConfiguration();

            connectAction.Enable();
            disconnectAction.Enable();
            publishAction.Enable();
            subscribeAction.Enable();
            statusAction.Enable();
            testCertAction.Enable();
            updateConfigAction.Enable();

            LogDebug("=== TNPSGA52 CONTROLS ===");
            LogDebug("[C] Connect | [D] Disconnect | [P] Publish | [S] Subscribe");
            LogDebug("[I] Status | [T] Test Certificate | [U] Update Config");
            LogDebug("=========================");
        }

        private void DisableInputActions()
        {
            connectAction?.Disable();
            disconnectAction?.Disable();
            publishAction?.Disable();
            subscribeAction?.Disable();
            statusAction?.Disable();
            testCertAction?.Disable();
            updateConfigAction?.Disable();
        }

        #endregion

        #region Certificate Testing

        private async void TestCertificateLoading()
        {
            LogDebug("🔍 Testing PFX certificate loading...");

            try
            {
                var certificate = await CertificateManager.Instance.LoadX509CertificateAsync(
                    pfxCertificateFile,
                    pfxPassword,
                    StorageInfo.StorageCategory.Resources);

                if (certificate != null)
                {
                    LogDebug("✅ Certificate loaded successfully!");
                    LogDebug($"Subject: {certificate.Subject}");
                    LogDebug($"Issuer: {certificate.Issuer}");
                    LogDebug($"Valid Until: {certificate.NotAfter}");
                    LogDebug($"Has Private Key: {certificate.HasPrivateKey}");
                }
                else
                {
                    LogError("❌ Failed to load certificate");
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Certificate test error: {ex.Message}");
            }
        }

        #endregion

        #region Configuration Update

        private async void UpdateConfiguration()
        {
            LogDebug("🔄 Updating MQTT configuration...");

            try
            {
                // Update MqttManager configuration
                mqttManager.BrokerAddress = brokerAddress;
                mqttManager.BrokerPort = brokerPort;

                // If clientId has changed, we need to reinitialize MqttManager
                if (mqttManager.GetConnectionStatus().clientId != clientId)
                {
                    LogDebug($"Client ID changed to: {clientId}. Reinitializing MqttManager...");
                    await mqttManager.DisconnectAsync();
                    mqttManager.Initialize(); // Reinitialize to apply new clientId
                }

                // Update test topic if it depends on clientId
                if (!testTopic.Contains(clientId))
                {
                    testTopic = $"tnp/{clientId}/test";
                    LogDebug($"Updated test topic to: {testTopic}");
                }

                LogDebug("✅ Configuration updated successfully");
                ShowStatus();
            }
            catch (Exception ex)
            {
                LogError($"❌ Configuration update error: {ex.Message}");
            }
        }

        #endregion

        #region MQTT Operations

        private async void ConnectToAWS()
        {
            LogDebug("🔄 Connecting to AWS IoT Core...");
            try
            {
                // Ensure latest configuration is applied
                mqttManager.BrokerAddress = brokerAddress;
                mqttManager.BrokerPort = brokerPort;

                bool connected = await mqttManager.ConnectAsync();
                if (connected)
                {
                    LogDebug("🎉 SUCCESS: Connected to AWS IoT Core!");
                }
                else
                {
                    LogError("❌ Connection failed");
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Connection error: {ex.Message}");
            }
        }

        private async void DisconnectFromAWS()
        {
            await DisconnectAsync();
        }

        private async Task DisconnectAsync()
        {
            try
            {
                if (mqttManager.IsConnected())
                {
                    LogDebug("🔄 Disconnecting from AWS IoT Core...");
                    await mqttManager.DisconnectAsync();
                    LogDebug("✅ Disconnected successfully");
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Disconnect error: {ex.Message}");
            }
        }

        private async void SubscribeToTestTopic()
        {
            if (!mqttManager.IsConnected())
            {
                LogError("❌ Cannot subscribe: Not connected");
                return;
            }

            try
            {
                LogDebug($"📡 Subscribing to topic: {testTopic}");
                bool subscribed = await mqttManager.SubscribeToTopicAsync(
                    testTopic,
                    MqttInfo.QoSLevel.AtLeastOnce);
                if (subscribed)
                {
                    LogDebug("✅ Successfully subscribed");
                }
                else
                    {
                    LogError("❌ Subscription failed");
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Subscribe error: {ex.Message}");
            }
        }

        private async void PublishTestMessage()
        {
            if (!mqttManager.IsConnected())
            {
                LogError("❌ Cannot publish: Not connected");
                return;
            }

            try
            {
                messageCounter++;
                LogDebug($"📤 Publishing to topic: {testTopic}");
                bool published = await mqttManager.SendTestMessageAsync(
                    $"Hello from TNPSGA52! Message #{messageCounter}");
                if (published)
                {
                    LogDebug("✅ Message published successfully");
                }
                else
                {
                    LogError("❌ Publish failed");
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Publish error: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private void OnMqttConnected()
        {
            isConnected = true;
            LogDebug("🎉 MQTT Connected!");
        }

        private void OnMqttDisconnected(string reason)
        {
            isConnected = false;
            isSubscribed = false;
            LogDebug($"❌ MQTT Disconnected. Reason: {reason}");
        }

        private void OnMessageReceived(string topic, string message)
        {
            LogDebug($"📨 Message received on '{topic}': {message}");
        }

        private void OnTopicSubscribed(string topic)
        {
            if (topic == testTopic)
            {
                isSubscribed = true;
                LogDebug($"✅ Subscribed to topic: {topic}");
            }
        }

        private void OnConnectionFailed(string errorMessage)
        {
            LogError($"❌ Connection failed: {errorMessage}");
        }

        #endregion

        #region Status

        private void ShowStatus()
        {
            try
            {
                var status = mqttManager.GetConnectionStatus();
                LogDebug("=== TNPSGA52 STATUS ===");
                LogDebug($"Connected: {status.isConnected}");
                LogDebug($"Broker: {status.brokerAddress}:{status.brokerPort}");
                LogDebug($"Client ID: {status.clientId}");
                LogDebug($"Subscribed to test topic: {isSubscribed}");
                LogDebug($"Test topic: {testTopic}");
                LogDebug($"Messages sent: {messageCounter}");
                LogDebug($"Certificate file: {pfxCertificateFile}");
                LogDebug("======================");
            }
            catch (Exception ex)
            {
                LogError($"❌ Status error: {ex.Message}");
            }
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[TNPSGA52] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[TNPSGA52] {message}");
        }

        #endregion
    }
}