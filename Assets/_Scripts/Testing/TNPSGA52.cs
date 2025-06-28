using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using _Scripts.Models.MQTTManagement;
using _Scripts.Models.IoTCoreManagement;
using _Scripts.Models.Mqtt;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace _Scripts.Models.Devices
{
    public class TNPSGA52 : MonoBehaviour
    {
        [Header("Device Configuration")]
        [SerializeField] private string deviceName = "TNPSGA52";
        [SerializeField] private string thingName = "TNPSGA52";
        [SerializeField] private string certificateId = "4cce0d28d072481fd01a0abe563f33b89690281b2c9b1160dc3c95c84c63f0e4";
        [SerializeField] private string policyName = "TNPSGA52";
        
        [Header("MQTT Configuration")]
        [SerializeField] private string brokerEndpoint = "d06815421so7uq8jzy8ln-ats.iot.us-east-1.amazonaws.com";
        [SerializeField] private int brokerPort = 8883;
        [SerializeField] private string certificateFile = "aws-iot-core.pfx";
        [SerializeField] private string certificatePassword = "5859";
        
        [Header("Topics")]
        [SerializeField] private string publishTopic = "tnp/TNPSGA52/data";
        [SerializeField] private string subscribeTopic = "tnp/TNPSGA52/commands";
        [SerializeField] private string testTopic = "tnp/TNPSGA52/test";
        
        [Header("Device Status")]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private bool autoConnect = true;
        [SerializeField] private bool sendPeriodicData = true;
        [SerializeField] private float dataInterval = 10f; // segundos
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool logReceivedMessages = true;
        
        [Header("Keyboard Controls")]
        [SerializeField] private bool enableKeyboardControls = true;
        [SerializeField] private KeyCode connectKey = KeyCode.C;
        [SerializeField] private KeyCode disconnectKey = KeyCode.D;
        [SerializeField] private KeyCode testMessageKey = KeyCode.T;
        [SerializeField] private KeyCode deviceDataKey = KeyCode.S;
        [SerializeField] private KeyCode togglePeriodicKey = KeyCode.P;
        [SerializeField] private KeyCode helpKey = KeyCode.H;
        
        private bool isInitialized = false;
        private Coroutine periodicDataCoroutine;
        private int messageCounter = 0;

        private void Start()
        {
            Initialize();
            
            // Mostrar controles disponibles
            if (enableKeyboardControls)
            {
                ShowKeyboardControls();
            }
        }

        private void Update()
        {
            if (enableKeyboardControls && isInitialized)
            {
                HandleKeyboardInput();
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private async void Initialize()
        {
            if (isInitialized)
            {
                LogDebug("TNPSGA52 already initialized");
                return;
            }

            try
            {
                LogDebug("Initializing TNPSGA52 device...");
                
                // Configurar MQTT Manager
                await ConfigureMqttManager();
                
                // Verificar Thing en IoT Core
                await VerifyIoTCoreThing();
                
                // Suscribirse a eventos
                SubscribeToEvents();
                
                // Auto conectar si está habilitado
                if (autoConnect)
                {
                    LogDebug("Auto-connect is enabled. Use 'C' to connect manually or disable auto-connect.");
                    await ConnectToAWS();
                }
                else
                {
                    LogDebug("Auto-connect disabled. Press 'C' to connect manually.");
                }
                
                isInitialized = true;
                LogDebug("TNPSGA52 device initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"Error initializing TNPSGA52: {ex.Message}");
            }
        }

        private async Task ConfigureMqttManager()
        {
            try
            {
                LogDebug("Configuring MQTT Manager...");
                
                // Configurar parámetros de conexión
                MqttManager.Instance.BrokerAddress = brokerEndpoint;
                MqttManager.Instance.BrokerPort = brokerPort;
                MqttManager.Instance.UseSSL = true;
                
                LogDebug($"MQTT configured - Broker: {brokerEndpoint}:{brokerPort} (SSL)");
            }
            catch (Exception ex)
            {
                LogError($"Error configuring MQTT Manager: {ex.Message}");
                throw;
            }
        }

        private async Task VerifyIoTCoreThing()
        {
            try
            {
                LogDebug("Verifying IoT Core Thing...");
                
                var thingInfo = await IoTCoreManager.Instance.GetThingAsync(thingName);
                if (thingInfo != null)
                {
                    LogDebug($"Thing verified: {thingInfo.ThingName} (ARN: {thingInfo.ThingArn})");
                }
                else
                {
                    LogWarning($"Thing {thingName} not found or not accessible");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error verifying IoT Core Thing: {ex.Message}");
            }
        }

        private void SubscribeToEvents()
        {
            try
            {
                LogDebug("Subscribing to MQTT events...");
                
                // Eventos de conexión
                MqttManager.Instance.SubscribeToConnected(OnMqttConnected);
                MqttManager.Instance.SubscribeToDisconnected(OnMqttDisconnected);
                MqttManager.Instance.SubscribeToConnectionFailed(OnMqttConnectionFailed);
                
                // Eventos de mensajes
                MqttManager.Instance.SubscribeToMessageReceived(OnMessageReceived);
                MqttManager.Instance.SubscribeToSubscribed(OnTopicSubscribed);
                
                LogDebug("Event subscriptions configured");
            }
            catch (Exception ex)
            {
                LogError($"Error subscribing to events: {ex.Message}");
            }
        }

        public async Task<bool> ConnectToAWS()
        {
            try
            {
                LogDebug("Attempting to connect to AWS IoT Core...");
                
                bool connected = await MqttManager.Instance.ConnectAsync();
                if (connected)
                {
                    LogDebug("Successfully connected to AWS IoT Core");
                    
                    // Suscribirse a tópicos
                    await SubscribeToTopics();
                    
                    // Enviar mensaje de conexión
                    await SendConnectionMessage();
                    
                    return true;
                }
                else
                {
                    LogError("Failed to connect to AWS IoT Core");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error connecting to AWS: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DisconnectFromAWS()
        {
            try
            {
                LogDebug("Disconnecting from AWS IoT Core...");
                
                // Detener envío periódico
                StopPeriodicData();
                
                // Enviar mensaje de desconexión
                await SendDisconnectionMessage();
                
                // Desconectar
                bool disconnected = await MqttManager.Instance.DisconnectAsync();
                
                LogDebug($"Disconnection {(disconnected ? "successful" : "failed")}");
                return disconnected;
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from AWS: {ex.Message}");
                return false;
            }
        }

        private async Task SubscribeToTopics()
        {
            try
            {
                LogDebug("Subscribing to MQTT topics...");
                
                // Suscribirse a comandos
                await MqttManager.Instance.SubscribeToTopicAsync(subscribeTopic, MqttInfo.QoSLevel.AtLeastOnce);
                
                // Suscribirse a topic de test
                await MqttManager.Instance.SubscribeToTopicAsync(testTopic, MqttInfo.QoSLevel.AtLeastOnce);
                
                LogDebug($"Subscribed to topics: {subscribeTopic}, {testTopic}");
            }
            catch (Exception ex)
            {
                LogError($"Error subscribing to topics: {ex.Message}");
            }
        }

        private async Task SendConnectionMessage()
        {
            try
            {
                var connectionData = new
                {
                    device = deviceName,
                    status = "connected",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    platform = Application.platform.ToString(),
                    version = Application.version,
                    sessionId = SystemInfo.deviceUniqueIdentifier
                };

                await MqttManager.Instance.PublishObjectAsync(publishTopic, connectionData, MqttInfo.QoSLevel.AtLeastOnce);
                LogDebug($"Connection message sent to {publishTopic}");
            }
            catch (Exception ex)
            {
                LogError($"Error sending connection message: {ex.Message}");
            }
        }

        private async Task SendDisconnectionMessage()
        {
            try
            {
                var disconnectionData = new
                {
                    device = deviceName,
                    status = "disconnected",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    reason = "normal_shutdown"
                };

                await MqttManager.Instance.PublishObjectAsync(publishTopic, disconnectionData, MqttInfo.QoSLevel.AtLeastOnce);
                LogDebug($"Disconnection message sent to {publishTopic}");
            }
            catch (Exception ex)
            {
                LogError($"Error sending disconnection message: {ex.Message}");
            }
        }

        public async Task SendTestMessage(string customMessage = null)
        {
            try
            {
                messageCounter++;
                var testData = new
                {
                    device = deviceName,
                    message = customMessage ?? $"Test message #{messageCounter} from Unity",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    messageId = messageCounter,
                    deviceInfo = new
                    {
                        model = SystemInfo.deviceModel,
                        os = SystemInfo.operatingSystem,
                        processor = SystemInfo.processorType,
                        memory = SystemInfo.systemMemorySize
                    }
                };

                await MqttManager.Instance.PublishObjectAsync(testTopic, testData, MqttInfo.QoSLevel.AtLeastOnce);
                LogDebug($"Test message #{messageCounter} sent to {testTopic}");
            }
            catch (Exception ex)
            {
                LogError($"Error sending test message: {ex.Message}");
            }
        }

        public async Task SendDeviceData()
        {
            try
            {
                var deviceData = new
                {
                    device = deviceName,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    data = new
                    {
                        frameRate = (int)(1f / Time.deltaTime),
                        memoryUsage = GetMemoryUsage(),
                        connectedTime = Time.time,
                        messagesSent = messageCounter,
                        platform = Application.platform.ToString()
                    }
                };

                await MqttManager.Instance.PublishObjectAsync(publishTopic, deviceData, MqttInfo.QoSLevel.AtLeastOnce);
                LogDebug($"Device data sent to {publishTopic}");
            }
            catch (Exception ex)
            {
                LogError($"Error sending device data: {ex.Message}");
            }
        }

        private void StartPeriodicData()
        {
            if (sendPeriodicData && periodicDataCoroutine == null)
            {
                periodicDataCoroutine = StartCoroutine(SendPeriodicDataCoroutine());
                LogDebug($"Started periodic data transmission (interval: {dataInterval}s)");
            }
        }

        private void StopPeriodicData()
        {
            if (periodicDataCoroutine != null)
            {
                StopCoroutine(periodicDataCoroutine);
                periodicDataCoroutine = null;
                LogDebug("Stopped periodic data transmission");
            }
        }

        private IEnumerator SendPeriodicDataCoroutine()
        {
            while (isConnected)
            {
                yield return new WaitForSeconds(dataInterval);
                if (isConnected)
                {
                    _ = SendDeviceData();
                }
            }
        }

        #region Event Handlers

        private void OnMqttConnected()
        {
            isConnected = true;
            LogDebug("MQTT Connected event received");
            
            if (sendPeriodicData)
            {
                StartPeriodicData();
            }
        }

        private void OnMqttDisconnected(string reason)
        {
            isConnected = false;
            StopPeriodicData();
            LogDebug($"MQTT Disconnected event received: {reason}");
        }

        private void OnMqttConnectionFailed(string error)
        {
            isConnected = false;
            LogError($"MQTT Connection Failed: {error}");
        }

        private void OnMessageReceived(string topic, string message)
        {
            if (logReceivedMessages)
            {
                LogDebug($"Message received on {topic}: {message}");
            }

            // Procesar comandos recibidos
            if (topic == subscribeTopic)
            {
                ProcessCommand(message);
            }
        }

        private void OnTopicSubscribed(string topic)
        {
            LogDebug($"Successfully subscribed to topic: {topic}");
        }

        #endregion

        #region Helper Methods

        private long GetMemoryUsage()
        {
            try
            {
                // Compatibilidad con diferentes versiones de Unity
                #if UNITY_2020_2_OR_NEWER
                    return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory();
                #else
                    return GC.GetTotalMemory(false);
                #endif
            }
            catch
            {
                // Fallback si hay problemas
                return GC.GetTotalMemory(false);
            }
        }

        #endregion

        #region Keyboard Controls

        private void HandleKeyboardInput()
        {
            try
            {
                #if ENABLE_INPUT_SYSTEM
                // Nuevo Input System
                var keyboard = Keyboard.current;
                if (keyboard == null) return;

                // Conectar
                if (keyboard[Key.C].wasPressedThisFrame)
                {
                    HandleConnectInput();
                }

                // Desconectar
                if (keyboard[Key.D].wasPressedThisFrame)
                {
                    HandleDisconnectInput();
                }

                // Enviar mensaje de test
                if (keyboard[Key.T].wasPressedThisFrame)
                {
                    HandleTestMessageInput();
                }

                // Enviar datos del dispositivo
                if (keyboard[Key.S].wasPressedThisFrame)
                {
                    HandleDeviceDataInput();
                }

                // Toggle envío periódico
                if (keyboard[Key.P].wasPressedThisFrame)
                {
                    HandleTogglePeriodicInput();
                }

                // Mostrar ayuda
                if (keyboard[Key.H].wasPressedThisFrame)
                {
                    ShowKeyboardControls();
                }

                #else
                // Input System clásico
                // Conectar
                if (Input.GetKeyDown(connectKey))
                {
                    HandleConnectInput();
                }

                // Desconectar
                if (Input.GetKeyDown(disconnectKey))
                {
                    HandleDisconnectInput();
                }

                // Enviar mensaje de test
                if (Input.GetKeyDown(testMessageKey))
                {
                    HandleTestMessageInput();
                }

                // Enviar datos del dispositivo
                if (Input.GetKeyDown(deviceDataKey))
                {
                    HandleDeviceDataInput();
                }

                // Toggle envío periódico
                if (Input.GetKeyDown(togglePeriodicKey))
                {
                    HandleTogglePeriodicInput();
                }

                // Mostrar ayuda
                if (Input.GetKeyDown(helpKey))
                {
                    ShowKeyboardControls();
                }
                #endif
            }
            catch (Exception ex)
            {
                LogError($"Error handling keyboard input: {ex.Message}");
            }
        }

        private void HandleConnectInput()
        {
            if (!isConnected)
            {
                LogDebug("[C] - Connecting to AWS IoT Core...");
                _ = ConnectToAWS();
            }
            else
            {
                LogWarning("[C] - Already connected!");
            }
        }

        private void HandleDisconnectInput()
        {
            if (isConnected)
            {
                LogDebug("[D] - Disconnecting from AWS IoT Core...");
                _ = DisconnectFromAWS();
            }
            else
            {
                LogWarning("[D] - Not connected!");
            }
        }

        private void HandleTestMessageInput()
        {
            if (isConnected)
            {
                LogDebug("[T] - Sending test message...");
                _ = SendTestMessage("Manual test message triggered by T key");
            }
            else
            {
                LogWarning("[T] - Not connected! Connect first with 'C'");
            }
        }

        private void HandleDeviceDataInput()
        {
            if (isConnected)
            {
                LogDebug("[S] - Sending device data...");
                _ = SendDeviceData();
            }
            else
            {
                LogWarning("[S] - Not connected! Connect first with 'C'");
            }
        }

        private void HandleTogglePeriodicInput()
        {
            sendPeriodicData = !sendPeriodicData;
            LogDebug($"[P] - Periodic data transmission: {(sendPeriodicData ? "ENABLED" : "DISABLED")}");
            
            if (sendPeriodicData && isConnected)
            {
                StartPeriodicData();
            }
            else
            {
                StopPeriodicData();
            }
        }

        private void ShowKeyboardControls()
        {
            LogDebug("=== TNPSGA52 KEYBOARD CONTROLS ===");
            #if ENABLE_INPUT_SYSTEM
            LogDebug("[C] - Connect to AWS IoT Core");
            LogDebug("[D] - Disconnect from AWS IoT Core");
            LogDebug("[T] - Send test message");
            LogDebug("[S] - Send device status data");
            LogDebug("[P] - Toggle periodic data transmission");
            LogDebug("[H] - Show this help");
            LogDebug("(Using New Input System)");
            #else
            LogDebug($"[{connectKey}] - Connect to AWS IoT Core");
            LogDebug($"[{disconnectKey}] - Disconnect from AWS IoT Core");
            LogDebug($"[{testMessageKey}] - Send test message");
            LogDebug($"[{deviceDataKey}] - Send device status data");
            LogDebug($"[{togglePeriodicKey}] - Toggle periodic data transmission");
            LogDebug($"[{helpKey}] - Show this help");
            LogDebug("(Using Legacy Input System)");
            #endif
            LogDebug("================================");
            LogDebug($"Current Status: {(isConnected ? "CONNECTED" : "DISCONNECTED")}");
            LogDebug($"Periodic Data: {(sendPeriodicData ? "ENABLED" : "DISABLED")}");
            LogDebug($"Auto Connect: {(autoConnect ? "ENABLED" : "DISABLED")}");
        }

        public void ShowStatus()
        {
            LogDebug("=== TNPSGA52 DEVICE STATUS ===");
            LogDebug($"Device Name: {deviceName}");
            LogDebug($"Thing Name: {thingName}");
            LogDebug($"Broker: {brokerEndpoint}:{brokerPort}");
            LogDebug($"Connected: {isConnected}");
            LogDebug($"Messages Sent: {messageCounter}");
            LogDebug($"Periodic Data: {sendPeriodicData} (Interval: {dataInterval}s)");
            LogDebug($"Topics:");
            LogDebug($"  - Publish: {publishTopic}");
            LogDebug($"  - Subscribe: {subscribeTopic}");
            LogDebug($"  - Test: {testTopic}");
            LogDebug("=============================");
        }

        #endregion

        private async void ProcessCommand(string command)
        {
            try
            {
                LogDebug($"Processing command: {command}");
                
                switch (command.ToLower())
                {
                    case "test":
                        await SendTestMessage("Command test response");
                        break;
                    case "status":
                        await SendDeviceData();
                        break;
                    case "ping":
                        await SendTestMessage("pong");
                        break;
                    default:
                        LogDebug($"Unknown command: {command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error processing command: {ex.Message}");
            }
        }

        private void Cleanup()
        {
            try
            {
                // Unsubscribe from events
                if (MqttManager.Instance != null)
                {
                    MqttManager.Instance.UnsubscribeFromConnected(OnMqttConnected);
                    MqttManager.Instance.UnsubscribeFromDisconnected(OnMqttDisconnected);
                    MqttManager.Instance.UnsubscribeFromConnectionFailed(OnMqttConnectionFailed);
                    MqttManager.Instance.UnsubscribeFromMessageReceived(OnMessageReceived);
                    MqttManager.Instance.UnsubscribeFromSubscribed(OnTopicSubscribed);
                }
                
                StopPeriodicData();
                LogDebug("TNPSGA52 cleanup completed");
            }
            catch (Exception ex)
            {
                LogError($"Error during cleanup: {ex.Message}");
            }
        }

        #region Public Methods for Inspector/Testing

        [ContextMenu("Connect to AWS")]
        public async void ConnectToAWSFromInspector()
        {
            await ConnectToAWS();
        }

        [ContextMenu("Disconnect from AWS")]
        public async void DisconnectFromAWSFromInspector()
        {
            await DisconnectFromAWS();
        }

        [ContextMenu("Send Test Message")]
        public async void SendTestMessageFromInspector()
        {
            await SendTestMessage();
        }

        [ContextMenu("Send Device Data")]
        public async void SendDeviceDataFromInspector()
        {
            await SendDeviceData();
        }

        [ContextMenu("Show Status")]
        public void ShowStatusFromInspector()
        {
            ShowStatus();
        }

        [ContextMenu("Show Controls")]
        public void ShowControlsFromInspector()
        {
            ShowKeyboardControls();
        }

        [ContextMenu("Toggle Periodic Data")]
        public void TogglePeriodicDataFromInspector()
        {
            sendPeriodicData = !sendPeriodicData;
            LogDebug($"Periodic data transmission: {(sendPeriodicData ? "ENABLED" : "DISABLED")}");
            
            if (sendPeriodicData && isConnected)
            {
                StartPeriodicData();
            }
            else
            {
                StopPeriodicData();
            }
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[TNPSGA52] {message}");
        }

        private void LogWarning(string message)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[TNPSGA52] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[TNPSGA52] {message}");
        }

        #endregion
    }
}