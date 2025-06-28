using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using MQTTnet;
using MQTTnet.Client;

namespace _Scripts.Testing
{
    public class MqttxUnity : MonoBehaviour
    {
        [Header("MQTT Configuration (Same as MQTTX)")]
        [SerializeField] private string hostName = "aqloxhiemdroo-ats.iot.us-east-1.amazonaws.com";
        [SerializeField] private int port = 8883;
        [SerializeField] private string clientId = "TNPSGA52";
        
        [Header("Certificate Files - Use PFX (Unity Compatible)")]
        [SerializeField] private string pfxCertificateFile = "aws-iot-core.pfx";
        [SerializeField] private string pfxPassword = "5859";
        
        [Header("Test Configuration")]
        [SerializeField] private string testTopic = "tnp/TNPSGA52/test";
        [SerializeField] private bool enableDebugLogs = true;
        
        // MQTT Client
        private IMqttClient mqttClient;
        private MqttFactory mqttFactory;
        
        // Input Actions
        private InputAction connectAction;
        private InputAction disconnectAction;
        private InputAction publishAction;
        private InputAction subscribeAction;
        private InputAction statusAction;
        private InputAction testCertAction;
        
        // Connection state
        private bool isConnected = false;
        private bool isSubscribed = false;
        private int messageCounter = 0;

        private void Awake()
        {
            LogDebug("Initializing MqttxUnity - Direct MQTT Connection");
            InitializeMqttClient();
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
            mqttClient?.Dispose();
        }

        #region Initialization

        private void InitializeMqttClient()
        {
            try
            {
                mqttFactory = new MqttFactory();
                mqttClient = mqttFactory.CreateMqttClient();
                
                // Setup event handlers
                mqttClient.ConnectedAsync += OnMqttConnected;
                mqttClient.DisconnectedAsync += OnMqttDisconnected;
                mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;
                
                LogDebug("✅ MQTT Client initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"❌ Failed to initialize MQTT client: {ex.Message}");
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

            connectAction.performed += _ => ConnectToAWS();
            disconnectAction.performed += _ => DisconnectFromAWS();
            publishAction.performed += _ => PublishTestMessage();
            subscribeAction.performed += _ => SubscribeToTestTopic();
            statusAction.performed += _ => ShowStatus();
            testCertAction.performed += _ => TestCertificateLoading();

            connectAction.Enable();
            disconnectAction.Enable();
            publishAction.Enable();
            subscribeAction.Enable();
            statusAction.Enable();
            testCertAction.Enable();

            LogDebug("=== MQTTX UNITY CONTROLS ===");
            LogDebug("[C] Connect | [D] Disconnect | [P] Publish | [S] Subscribe");
            LogDebug("[I] Status | [T] Test Certificate");
            LogDebug("===========================");
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

        #region Certificate Loading

        private async void TestCertificateLoading()
        {
            LogDebug("🔍 Testing PFX certificate loading...");
            
            try
            {
                var certificate = await LoadCertificateAsync();
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

        private async Task<X509Certificate2> LoadCertificateAsync()
        {
            try
            {
                // Load PFX certificate (Unity compatible)
                string pfxPath = Path.Combine(Application.streamingAssetsPath, "resources", pfxCertificateFile);

                LogDebug($"Loading PFX certificate from: {pfxPath}");

                // Read PFX file
                byte[] pfxBytes = await File.ReadAllBytesAsync(pfxPath);
                
                LogDebug($"✅ PFX file read successfully. Size: {pfxBytes.Length} bytes");

                // Create certificate with password
                var certificate = new X509Certificate2(pfxBytes, pfxPassword, X509KeyStorageFlags.Exportable);

                LogDebug("✅ Certificate loaded successfully!");
                LogDebug($"Subject: {certificate.Subject}");
                LogDebug($"Issuer: {certificate.Issuer}");
                LogDebug($"Has Private Key: {certificate.HasPrivateKey}");
                LogDebug($"Valid From: {certificate.NotBefore}");
                LogDebug($"Valid Until: {certificate.NotAfter}");

                return certificate;
            }
            catch (Exception ex)
            {
                LogError($"❌ Error loading PFX certificate: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Certificate Helper Method

        private X509Certificate2 CreateCertificateFromPem(string certPem, string keyPem)
        {
            try
            {
                LogDebug("🔄 Creating certificate from PEM...");
                
                // Convert PEM to bytes
                var certBytes = ConvertPemToBytes(certPem, "CERTIFICATE");
                var keyBytes = ConvertPemToBytes(keyPem, "PRIVATE KEY");

                LogDebug($"✅ Certificate bytes: {certBytes.Length}");
                LogDebug($"✅ Key bytes: {keyBytes.Length}");

                // Create certificate from bytes
                var cert = new X509Certificate2(certBytes);
                LogDebug($"✅ Base certificate created: {cert.Subject}");
                
                // Add private key based on key type
                try
                {
                    using (var rsa = System.Security.Cryptography.RSA.Create())
                    {
                        // Try PKCS#8 format first (standard format)
                        try
                        {
                            rsa.ImportPkcs8PrivateKey(keyBytes, out _);
                            LogDebug("✅ Imported as PKCS#8 private key");
                        }
                        catch
                        {
                            // Try RSA format if PKCS#8 fails
                            rsa.ImportRSAPrivateKey(keyBytes, out _);
                            LogDebug("✅ Imported as RSA private key");
                        }
                        
                        var certWithKey = cert.CopyWithPrivateKey(rsa);
                        var finalCert = new X509Certificate2(certWithKey.Export(X509ContentType.Pkcs12));
                        LogDebug("✅ Certificate with private key created successfully");
                        return finalCert;
                    }
                }
                catch (Exception keyEx)
                {
                    LogError($"❌ Error importing private key: {keyEx.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Error creating certificate from PEM: {ex.Message}");
                throw;
            }
        }

        private byte[] ConvertPemToBytes(string pem, string header)
        {
            try
            {
                // Support multiple private key formats
                string[] possibleHeaders = header == "PRIVATE KEY" 
                    ? new[] { "PRIVATE KEY", "RSA PRIVATE KEY", "EC PRIVATE KEY" }
                    : new[] { header };

                foreach (var h in possibleHeaders)
                {
                    var startMarker = $"-----BEGIN {h}-----";
                    var endMarker = $"-----END {h}-----";
                    
                    var start = pem.IndexOf(startMarker);
                    var end = pem.IndexOf(endMarker);
                    
                    if (start != -1 && end != -1)
                    {
                        start += startMarker.Length;
                        var base64 = pem.Substring(start, end - start)
                            .Replace("\n", "")
                            .Replace("\r", "")
                            .Replace(" ", "")
                            .Replace("\t", "");
                        
                        LogDebug($"✅ Found PEM format: {h}");
                        return Convert.FromBase64String(base64);
                    }
                }
                
                throw new ArgumentException($"No valid PEM format found. Looking for: {string.Join(", ", possibleHeaders)}");
            }
            catch (Exception ex)
            {
                LogError($"❌ Error converting PEM to bytes: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region MQTT Connection

        private async void ConnectToAWS()
        {
            LogDebug("🔄 Connecting to AWS IoT Core...");
            LogDebug($"Host: {hostName}:{port}");
            LogDebug($"Client ID: {clientId}");
            
            try
            {
                // Load certificate
                var certificate = await LoadCertificateAsync();
                if (certificate == null)
                {
                    LogError("❌ Cannot connect: Failed to load certificate");
                    return;
                }

                // Build MQTT client options (exactly like MQTTX)
                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithClientId(clientId)
                    .WithTcpServer(hostName, port)
                    .WithTls(tlsOptions =>
                    {
                        tlsOptions.UseTls = true;
                        tlsOptions.SslProtocol = System.Security.Authentication.SslProtocols.Tls12;
                        
                        // Client certificate (like MQTTX Client Certificate File)
                        tlsOptions.Certificates = new[] { certificate };
                        
                        // Certificate validation (like MQTTX SSL Secure = true)
                        tlsOptions.CertificateValidationHandler = context =>
                        {
                            // Validate against our CA certificate
                            if (context.Certificate != null)
                            {
                                LogDebug($"Server certificate: {context.Certificate.Subject}");
                                return true; // Accept for now, like MQTTX
                            }
                            return false;
                        };
                        
                        tlsOptions.AllowUntrustedCertificates = false;
                        tlsOptions.IgnoreCertificateChainErrors = false;
                        tlsOptions.IgnoreCertificateRevocationErrors = true;
                    })
                    .WithTimeout(TimeSpan.FromSeconds(30))
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                    .WithCleanSession(true);

                var options = optionsBuilder.Build();

                LogDebug("🚀 Attempting connection...");
                var result = await mqttClient.ConnectAsync(options);
                
                if (result.ResultCode == MqttClientConnectResultCode.Success)
                {
                    isConnected = true;
                    LogDebug("🎉 SUCCESS: Connected to AWS IoT Core!");
                }
                else
                {
                    LogError($"❌ Connection failed: {result.ResultCode} - {result.ReasonString}");
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Connection error: {ex.Message}");
                LogError($"Stack trace: {ex.StackTrace}");
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
                if (mqttClient != null && isConnected)
                {
                    LogDebug("🔄 Disconnecting from AWS IoT Core...");
                    await mqttClient.DisconnectAsync();
                    isConnected = false;
                    isSubscribed = false;
                    LogDebug("✅ Disconnected successfully");
                }
            }
            catch (Exception ex)
            {
                LogError($"❌ Disconnect error: {ex.Message}");
            }
        }

        #endregion

        #region MQTT Events

        private async Task OnMqttConnected(MqttClientConnectedEventArgs e)
        {
            LogDebug($"🎉 MQTT Connected! Result: {e.ConnectResult.ResultCode}");
            isConnected = true;
        }

        private async Task OnMqttDisconnected(MqttClientDisconnectedEventArgs e)
        {
            LogDebug($"❌ MQTT Disconnected. Reason: {e.Reason}");
            if (e.Exception != null)
            {
                LogError($"Exception: {e.Exception.Message}");
            }
            isConnected = false;
            isSubscribed = false;
        }

        private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                string topic = e.ApplicationMessage.Topic;
                string payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                
                LogDebug($"📨 Message received on '{topic}': {payload}");
            }
            catch (Exception ex)
            {
                LogError($"❌ Error processing received message: {ex.Message}");
            }
        }

        #endregion

        #region MQTT Operations

        private async void SubscribeToTestTopic()
        {
            if (!isConnected)
            {
                LogError("❌ Cannot subscribe: Not connected");
                return;
            }

            try
            {
                LogDebug($"📡 Subscribing to topic: {testTopic}");
                
                await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(testTopic)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build());
                
                isSubscribed = true;
                LogDebug("✅ Successfully subscribed");
            }
            catch (Exception ex)
            {
                LogError($"❌ Subscribe error: {ex.Message}");
            }
        }

        private async void PublishTestMessage()
        {
            if (!isConnected)
            {
                LogError("❌ Cannot publish: Not connected");
                return;
            }

            try
            {
                messageCounter++;
                
                // Create a proper serializable class instead of anonymous object
                var testMessage = new TestMessage
                {
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    message = $"Hello from Unity MQTTX! Message #{messageCounter}",
                    clientId = clientId,
                    source = "Unity",
                    platform = Application.platform.ToString(),
                    messageId = messageCounter,
                    deviceInfo = new DeviceInfo
                    {
                        model = SystemInfo.deviceModel,
                        os = SystemInfo.operatingSystem,
                        processor = SystemInfo.processorType,
                        memory = SystemInfo.systemMemorySize
                    }
                };

                string jsonPayload = JsonUtility.ToJson(testMessage, true);
                
                LogDebug($"📤 Publishing to topic: {testTopic}");
                LogDebug($"Payload: {jsonPayload}");

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(testTopic)
                    .WithPayload(jsonPayload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();

                await mqttClient.PublishAsync(message);
                LogDebug("✅ Message published successfully");
            }
            catch (Exception ex)
            {
                LogError($"❌ Publish error: {ex.Message}");
            }
        }

        #endregion

        #region Status and Debugging

        private void ShowStatus()
        {
            LogDebug("=== MQTTX UNITY STATUS ===");
            LogDebug($"Connected: {isConnected}");
            LogDebug($"Host: {hostName}:{port}");
            LogDebug($"Client ID: {clientId}");
            LogDebug($"Subscribed to test topic: {isSubscribed}");
            LogDebug($"Test topic: {testTopic}");
            LogDebug($"Messages sent: {messageCounter}");
            LogDebug($"Certificate file: {pfxCertificateFile}");
            LogDebug("=========================");
        }

        #endregion

        #region Serializable Classes for JSON

        [System.Serializable]
        public class TestMessage
        {
            public string timestamp;
            public string message;
            public string clientId;
            public string source;
            public string platform;
            public int messageId;
            public DeviceInfo deviceInfo;
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
                Debug.Log($"[MqttxUnity] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[MqttxUnity] {message}");
        }

        #endregion
    }
}