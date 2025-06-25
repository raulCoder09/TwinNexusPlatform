using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Amazon.IoT;
using Amazon;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using Amazon.IotData;
using Amazon.IotData.Model;
using UnityEngine.Serialization;

namespace _Scripts.Models
{
    [System.Serializable]
    public class IoTDeviceInfo
    {
        public string thingName;
        public string thingArn;
        public string thingType;
        public string status;
        public DateTime lastSeen;
        public Dictionary<string, object> attributes;

        public IoTDeviceInfo(string name, string arn = "", string type = "Unity-Device")
        {
            thingName = name;
            thingArn = arn;
            thingType = type;
            status = "offline";
            lastSeen = DateTime.UtcNow;
            attributes = new Dictionary<string, object>();
        }
    }

    [System.Serializable]
    public class IoTMessage
    {
        public string topic;
        public string payload;
        public DateTime timestamp;
        public string source;
        public string messageId;

        public IoTMessage(string topic, string payload, string source = "Unity")
        {
            this.topic = topic;
            this.payload = payload;
            this.timestamp = DateTime.UtcNow;
            this.source = source;
            this.messageId = Guid.NewGuid().ToString("N")[..8];
        }
    }

    [System.Serializable]
    public class TelemetryData
    {
        public string deviceId;
        public string deviceType;
        public Dictionary<string, object> sensors;
        public Dictionary<string, object> status;
        public string location;
        public DateTime timestamp;
        public string userRole;

        public TelemetryData()
        {
            sensors = new Dictionary<string, object>();
            status = new Dictionary<string, object>();
            timestamp = DateTime.UtcNow;
        }
    }

    public class IoTCoreManager : MonoBehaviour
    {
        [Header("IoT Core Configuration")]
        [SerializeField] private string thingName = "unity-device-001";
        [SerializeField] private string deviceType = "Unity-Mobile-Device";
        [SerializeField] private bool enableHeartbeat = true;
        [SerializeField] private float heartbeatInterval = 30f; // seconds
        
        [Header("Topic Configuration")]
        [SerializeField] private string baseTopic = "twin-nexus";
        [SerializeField] private string telemetryTopic = "telemetry";
        [SerializeField] private string commandTopic = "commands";
        [SerializeField] private string statusTopic = "status";
        
        [Header("Device Shadow Configuration")]
        [SerializeField] private bool enableDeviceShadow = true;
        [SerializeField] private string shadowUpdateTopic = "$aws/things/{thingName}/shadow/update";
        [SerializeField] private string shadowGetTopic = "$aws/things/{thingName}/shadow/get";
        
        [Header("Domain configuration details")] 
        [SerializeField] private string domainName = "aqloxhiemdroo-ats.iot.us-east-1.amazonaws.com";
        [SerializeField] private string domainArn = "arn:aws:iot:us-east-1:156041417101:domainconfiguration/iot:Data-ATS";
        // AWS IoT clients
        private AmazonIoTClient iotClient;
        private AmazonIotDataClient iotDataClient;
        
        // Connection state
        public bool IsConnectedToIoTCore { get; private set; }
        public IoTDeviceInfo CurrentDevice { get; private set; }
        
        // Subscribed topics
        private List<string> subscribedTopics = new List<string>();
        
        // Heartbeat timer
        private float nextHeartbeat;
        
        // Events for IoT operations
        public event Action<bool, string> OnIoTConnectionComplete;
        public event Action<bool, string, string> OnMessagePublished; // success, topic, messageId
        public event Action<string, string, DateTime> OnMessageReceived; // topic, payload, timestamp
        public event Action<bool, string> OnSubscriptionComplete; // success, topic
        public event Action<bool, string, object> OnDeviceShadowUpdated; // success, topic, shadowData
        public event Action<bool, string> OnTelemetryPublished; // success, message
        
        // Message history for debugging
        private Queue<IoTMessage> messageHistory = new Queue<IoTMessage>();
        private const int MAX_MESSAGE_HISTORY = 50;
        
        // Singleton instance
        public static IoTCoreManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDevice();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Handle heartbeat if enabled and connected
            if (enableHeartbeat && IsConnectedToIoTCore && Time.time >= nextHeartbeat)
            {
                _ = SendHeartbeatAsync();
                nextHeartbeat = Time.time + heartbeatInterval;
            }
        }

        #region Initialization

        /// <summary>
        /// Initializes IoT Core clients with current AWS credentials
        /// </summary>
        private void InitializeIoTClients()
        {
            try
            {
                if (CognitoManager.Instance == null || CognitoManager.Instance.CurrentAWSCredentials == null)
                {
                    Debug.LogError("No AWS credentials available. Please authenticate first.");
                    return;
                }

                var regionEndpoint = RegionEndpoint.USEast1; // Same region as other services

                // Configure IoT client
                iotClient = new AmazonIoTClient(CognitoManager.Instance.CurrentAWSCredentials, regionEndpoint);

                // Configure IoT Data client with AmazonIotDataConfig
                var iotDataConfig = new AmazonIotDataConfig
                {
                    RegionEndpoint = regionEndpoint,
                    // Optionally set the service URL for IoT Data endpoint if needed
                     ServiceURL = $"https://{domainName}" 
                };
                iotDataClient = new AmazonIotDataClient(CognitoManager.Instance.CurrentAWSCredentials, iotDataConfig);

                Debug.Log("IoT Core clients initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize IoT Core clients: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes device information
        /// </summary>
        private void InitializeDevice()
        {
            try
            {
                // Create device info with user-specific naming
                string userRole = CognitoManager.Instance?.GetUserRole() ?? "basic";
                string username = CognitoManager.Instance?.CurrentUsername ?? "unknown";
                
                string deviceName = $"{thingName}-{userRole}-{username}";
                CurrentDevice = new IoTDeviceInfo(deviceName, "", deviceType);
                
                // Add device attributes
                CurrentDevice.attributes["platform"] = Application.platform.ToString();
                CurrentDevice.attributes["version"] = Application.version;
                CurrentDevice.attributes["userRole"] = userRole;
                CurrentDevice.attributes["username"] = username;
                
                Debug.Log($"Device initialized: {CurrentDevice.thingName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize device: {ex.Message}");
            }
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// Connects to AWS IoT Core
        /// </summary>
        public async Task<bool> ConnectToIoTCoreAsync()
        {
            try
            {
                // Check if user is authenticated
                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to connect to IoT Core");
                    OnIoTConnectionComplete?.Invoke(false, "User not authenticated");
                    return false;
                }

                // Initialize IoT clients if needed
                if (iotClient == null || iotDataClient == null)
                {
                    InitializeIoTClients();
                    if (iotClient == null || iotDataClient == null)
                    {
                        OnIoTConnectionComplete?.Invoke(false, "Failed to initialize IoT clients");
                        return false;
                    }
                }

                Debug.Log($"Connecting to IoT Core with device: {CurrentDevice.thingName}");

                // Test connectivity by trying to publish a connection message
                var connectionPayload = new
                {
                    deviceId = CurrentDevice.thingName,
                    action = "connect",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    userRole = CognitoManager.Instance.GetUserRole(),
                    platform = Application.platform.ToString()
                };

                string topic = GetUserSpecificTopic(statusTopic);
                bool testPublish = await PublishToTopicAsync(topic, connectionPayload);

                if (testPublish)
                {
                    IsConnectedToIoTCore = true;
                    CurrentDevice.status = "online";
                    CurrentDevice.lastSeen = DateTime.UtcNow;

                    Debug.Log("Successfully connected to IoT Core");
                    
                    // Subscribe to user-specific topics
                    await SubscribeToUserTopicsAsync();
                    
                    // Update device shadow if enabled
                    if (enableDeviceShadow)
                    {
                        await UpdateDeviceShadowAsync(new { state = new { reported = connectionPayload } });
                    }
                    
                    OnIoTConnectionComplete?.Invoke(true, "Connected to IoT Core successfully");
                    return true;
                }
                else
                {
                    Debug.LogError("Failed to connect to IoT Core");
                    OnIoTConnectionComplete?.Invoke(false, "Connection test failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"IoT Core connection error: {ex.Message}");
                OnIoTConnectionComplete?.Invoke(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Disconnects from AWS IoT Core
        /// </summary>
        public async Task<bool> DisconnectFromIoTCoreAsync()
        {
            try
            {
                if (!IsConnectedToIoTCore)
                {
                    Debug.LogWarning("Not connected to IoT Core");
                    return false;
                }

                // Send disconnect message
                var disconnectionPayload = new
                {
                    deviceId = CurrentDevice.thingName,
                    action = "disconnect",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    lastSession = CurrentDevice.lastSeen
                };

                string topic = GetUserSpecificTopic(statusTopic);
                await PublishToTopicAsync(topic, disconnectionPayload);

                // Update status
                IsConnectedToIoTCore = false;
                CurrentDevice.status = "offline";
                subscribedTopics.Clear();

                Debug.Log("Disconnected from IoT Core");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"IoT Core disconnection error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Message Publishing

        /// <summary>
        /// Publishes a message to a specific IoT topic
        /// </summary>
        public async Task<bool> PublishToTopicAsync(string topic, object payload)
        {
            try
            {
                if (!IsConnectedToIoTCore && !topic.Contains(statusTopic))
                {
                    Debug.LogWarning("Not connected to IoT Core");
                    OnMessagePublished?.Invoke(false, topic, null);
                    return false;
                }

                if (iotDataClient == null)
                {
                    Debug.LogError("IoT Data client not initialized");
                    OnMessagePublished?.Invoke(false, topic, null);
                    return false;
                }

                string jsonPayload = JsonConvert.SerializeObject(payload, Formatting.None);
                var messageId = Guid.NewGuid().ToString("N")[..8];

                Debug.Log($"Publishing to topic: {topic}");
                Debug.Log($"Payload: {jsonPayload}");

                var publishRequest = new PublishRequest
                {
                    Topic = topic,
                    Payload = new MemoryStream(Encoding.UTF8.GetBytes(jsonPayload)),
                    Qos = 1
                };

                var response = await iotDataClient.PublishAsync(publishRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Store message in history
                    var message = new IoTMessage(topic, jsonPayload, "Unity");
                    StoreMessageInHistory(message);

                    Debug.Log($"Message published successfully to: {topic}");
                    OnMessagePublished?.Invoke(true, topic, messageId);
                    return true;
                }
                else
                {
                    Debug.LogError($"Failed to publish message. Status: {response.HttpStatusCode}");
                    OnMessagePublished?.Invoke(false, topic, null);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error publishing message to topic '{topic}': {ex.Message}");
                OnMessagePublished?.Invoke(false, topic, null);
                return false;
            }
        }

        /// <summary>
        /// Publishes telemetry data from device sensors
        /// </summary>
        public async Task<bool> PublishTelemetryAsync(Dictionary<string, object> sensorData)
        {
            try
            {
                var telemetry = new TelemetryData
                {
                    deviceId = CurrentDevice.thingName,
                    deviceType = CurrentDevice.thingType,
                    sensors = sensorData ?? new Dictionary<string, object>(),
                    userRole = CognitoManager.Instance?.GetUserRole() ?? "unknown",
                    location = "Unity-App"
                };

                // Add device status info
                telemetry.status["battery"] = SystemInfo.batteryLevel;
                telemetry.status["platform"] = Application.platform.ToString();
                telemetry.status["memory"] = SystemInfo.systemMemorySize;
                telemetry.status["fps"] = Application.targetFrameRate;

                string topic = GetUserSpecificTopic(telemetryTopic);
                bool success = await PublishToTopicAsync(topic, telemetry);

                if (success)
                {
                    OnTelemetryPublished?.Invoke(true, "Telemetry data published successfully");
                    Debug.Log("Telemetry data published successfully");
                }
                else
                {
                    OnTelemetryPublished?.Invoke(false, "Failed to publish telemetry data");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error publishing telemetry: {ex.Message}");
                OnTelemetryPublished?.Invoke(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sends device heartbeat/status update
        /// </summary>
        public async Task<bool> SendHeartbeatAsync()
        {
            try
            {
                var heartbeat = new
                {
                    deviceId = CurrentDevice.thingName,
                    action = "heartbeat",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    uptime = Time.realtimeSinceStartup,
                    status = "online",
                    batteryLevel = SystemInfo.batteryLevel,
                    memoryUsage = GC.GetTotalMemory(false)
                };

                string topic = GetUserSpecificTopic($"{statusTopic}/heartbeat");
                return await PublishToTopicAsync(topic, heartbeat);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending heartbeat: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends device status update
        /// </summary>
        public async Task<bool> SendDeviceStatusAsync()
        {
            try
            {
                var deviceStatus = new
                {
                    deviceId = CurrentDevice.thingName,
                    deviceType = CurrentDevice.thingType,
                    action = "status_update",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    attributes = CurrentDevice.attributes,
                    systemInfo = new
                    {
                        platform = Application.platform.ToString(),
                        version = Application.version,
                        systemMemory = SystemInfo.systemMemorySize,
                        processorType = SystemInfo.processorType,
                        deviceModel = SystemInfo.deviceModel,
                        batteryLevel = SystemInfo.batteryLevel
                    },
                    networkInfo = new
                    {
                        internetReachability = Application.internetReachability.ToString(),
                        hasInternet = Application.internetReachability != NetworkReachability.NotReachable
                    }
                };

                string topic = GetUserSpecificTopic(statusTopic);
                return await PublishToTopicAsync(topic, deviceStatus);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending device status: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Topic Subscription

        /// <summary>
        /// Subscribes to a specific IoT topic (Note: actual subscription happens in AWS console/backend)
        /// </summary>
        public async Task<bool> SubscribeToTopicAsync(string topic)
        {
            try
            {
                if (!IsConnectedToIoTCore)
                {
                    Debug.LogWarning("Cannot subscribe: Not connected to IoT Core");
                    OnSubscriptionComplete?.Invoke(false, topic);
                    return false;
                }

                if (subscribedTopics.Contains(topic))
                {
                    Debug.LogWarning($"Already subscribed to topic: {topic}");
                    OnSubscriptionComplete?.Invoke(true, topic);
                    return true;
                }

                // Note: In AWS IoT Core, actual subscriptions are typically handled by:
                // 1. IoT Rules for routing messages
                // 2. WebSocket connections for real-time updates
                // 3. Lambda triggers for processing
                // For Unity, we register interest in topics and handle via polling or webhooks

                subscribedTopics.Add(topic);
                Debug.Log($"Subscribed to topic: {topic}");
                OnSubscriptionComplete?.Invoke(true, topic);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error subscribing to topic '{topic}': {ex.Message}");
                OnSubscriptionComplete?.Invoke(false, topic);
                return false;
            }
        }

        /// <summary>
        /// Subscribes to user-specific topics based on role
        /// </summary>
        public async Task<bool> SubscribeToUserTopicsAsync()
        {
            try
            {
                var userRole = CognitoManager.Instance?.GetUserRole() ?? "usuarios-basicos";
                var topics = GetAvailableTopics();

                bool allSuccessful = true;
                foreach (var topic in topics)
                {
                    bool success = await SubscribeToTopicAsync(topic);
                    if (!success) allSuccessful = false;
                }

                Debug.Log($"Subscribed to {topics.Count} user-specific topics");
                return allSuccessful;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error subscribing to user topics: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unsubscribes from a specific topic
        /// </summary>
        public async Task<bool> UnsubscribeFromTopicAsync(string topic)
        {
            try
            {
                if (subscribedTopics.Contains(topic))
                {
                    subscribedTopics.Remove(topic);
                    Debug.Log($"Unsubscribed from topic: {topic}");
                    return true;
                }
                
                Debug.LogWarning($"Not subscribed to topic: {topic}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error unsubscribing from topic '{topic}': {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Device Shadow

        /// <summary>
        /// Updates device shadow with desired state
        /// </summary>
        public async Task<bool> UpdateDeviceShadowAsync(object desiredState)
        {
            try
            {
                if (!enableDeviceShadow)
                {
                    Debug.LogWarning("Device shadow is disabled");
                    return false;
                }

                var shadowUpdate = new
                {
                    state = desiredState,
                    metadata = new
                    {
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        deviceId = CurrentDevice.thingName,
                        source = "Unity"
                    }
                };

                string shadowTopic = shadowUpdateTopic.Replace("{thingName}", CurrentDevice.thingName);
                bool success = await PublishToTopicAsync(shadowTopic, shadowUpdate);

                if (success)
                {
                    OnDeviceShadowUpdated?.Invoke(true, shadowTopic, shadowUpdate);
                    Debug.Log("Device shadow updated successfully");
                }
                else
                {
                    OnDeviceShadowUpdated?.Invoke(false, shadowTopic, null);
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating device shadow: {ex.Message}");
                OnDeviceShadowUpdated?.Invoke(false, "", null);
                return false;
            }
        }

        /// <summary>
        /// Gets current device shadow state
        /// </summary>
        public async Task<bool> GetDeviceShadowAsync()
        {
            try
            {
                if (!enableDeviceShadow)
                {
                    Debug.LogWarning("Device shadow is disabled");
                    return false;
                }

                var shadowRequest = new
                {
                    deviceId = CurrentDevice.thingName,
                    action = "get_shadow",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                string shadowTopic = shadowGetTopic.Replace("{thingName}", CurrentDevice.thingName);
                return await PublishToTopicAsync(shadowTopic, shadowRequest);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting device shadow: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets user-specific topic with role prefix
        /// </summary>
        private string GetUserSpecificTopic(string topicSuffix)
        {
            var userRole = CognitoManager.Instance?.GetUserRole() ?? "basic";
            var username = CognitoManager.Instance?.CurrentUsername ?? "unknown";
            
            return $"{baseTopic}/{userRole}/{username}/{topicSuffix}";
        }

        /// <summary>
        /// Gets available topics for current user based on their role
        /// </summary>
        public List<string> GetAvailableTopics()
        {
            if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                return new List<string>();

            var userRole = CognitoManager.Instance.GetUserRole();
            var topics = new List<string>();

            // Base topics for all users
            topics.Add(GetUserSpecificTopic(telemetryTopic));
            topics.Add(GetUserSpecificTopic(statusTopic));

            switch (userRole)
            {
                case "super-admin":
                    topics.AddRange(new[]
                    {
                        $"{baseTopic}/admin/system",
                        $"{baseTopic}/admin/devices",
                        $"{baseTopic}/admin/users",
                        $"{baseTopic}/*/commands", // Can send commands to all roles
                        GetUserSpecificTopic(commandTopic)
                    });
                    break;

                case "operadores":
                    topics.AddRange(new[]
                    {
                        $"{baseTopic}/industrial/sensors",
                        $"{baseTopic}/industrial/controls",
                        $"{baseTopic}/operational/alerts",
                        GetUserSpecificTopic(commandTopic)
                    });
                    break;

                case "estudiantes":
                    topics.AddRange(new[]
                    {
                        $"{baseTopic}/educational/lab-sensors",
                        $"{baseTopic}/educational/experiments",
                        $"{baseTopic}/learning/assignments",
                        GetUserSpecificTopic(commandTopic)
                    });
                    break;

                case "usuarios-basicos":
                default:
                    topics.AddRange(new[]
                    {
                        $"{baseTopic}/public/announcements",
                        $"{baseTopic}/public/weather",
                        GetUserSpecificTopic("notifications")
                    });
                    break;
            }

            return topics;
        }

        /// <summary>
        /// Checks if user can access a specific topic
        /// </summary>
        public bool CanUserAccessTopic(string topic)
        {
            var availableTopics = GetAvailableTopics();
            return availableTopics.Any(t => topic.StartsWith(t.Replace("*", "")));
        }

        /// <summary>
        /// Simulates receiving a message (for testing purposes)
        /// </summary>
        public void SimulateMessageReceived(string topic, string payload)
        {
            try
            {
                Debug.Log($"Simulated message received on topic '{topic}': {payload}");
                
                var message = new IoTMessage(topic, payload, "Simulated");
                StoreMessageInHistory(message);
                
                OnMessageReceived?.Invoke(topic, payload, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error simulating message: {ex.Message}");
            }
        }

        /// <summary>
        /// Stores message in history for debugging
        /// </summary>
        private void StoreMessageInHistory(IoTMessage message)
        {
            messageHistory.Enqueue(message);
            
            while (messageHistory.Count > MAX_MESSAGE_HISTORY)
            {
                messageHistory.Dequeue();
            }
        }

        /// <summary>
        /// Gets recent message history
        /// </summary>
        public List<IoTMessage> GetMessageHistory()
        {
            return messageHistory.ToList();
        }

        /// <summary>
        /// Test method to verify IoT Core connectivity
        /// </summary>
        public async Task<bool> TestIoTConnectivityAsync()
        {
            try
            {
                Debug.Log("Testing IoT Core connectivity...");
                
                var testPayload = new
                {
                    test = true,
                    message = "Connectivity test from Unity",
                    deviceId = CurrentDevice.thingName,
                    userRole = CognitoManager.Instance?.GetUserRole() ?? "unknown",
                    timestamp = DateTime.UtcNow,
                    platform = Application.platform.ToString()
                };

                string testTopic = GetUserSpecificTopic("test");
                return await PublishToTopicAsync(testTopic, testPayload);
            }
            catch (Exception ex)
            {
                Debug.LogError($"IoT connectivity test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Publishes sample sensor data for testing
        /// </summary>
        public async Task<bool> PublishSampleTelemetryAsync()
        {
            try
            {
                var sampleSensors = new Dictionary<string, object>
                {
                    {"temperature", UnityEngine.Random.Range(18.0f, 35.0f)},
                    {"humidity", UnityEngine.Random.Range(30.0f, 80.0f)},
                    {"pressure", UnityEngine.Random.Range(990.0f, 1030.0f)},
                    {"light", UnityEngine.Random.Range(0.0f, 1000.0f)},
                    {"motion", UnityEngine.Random.value > 0.7f},
                    {"sound_level", UnityEngine.Random.Range(30.0f, 90.0f)}
                };

                return await PublishTelemetryAsync(sampleSensors);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error publishing sample telemetry: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (IsConnectedToIoTCore)
            {
                _ = DisconnectFromIoTCoreAsync();
            }
            
            iotClient?.Dispose();
            iotDataClient?.Dispose();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // App is being paused
                if (IsConnectedToIoTCore)
                {
                    _ = SendDeviceStatusAsync();
                }
            }
            else
            {
                // App is being resumed
                if (IsConnectedToIoTCore)
                {
                    _ = SendDeviceStatusAsync();
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && IsConnectedToIoTCore)
            {
                _ = SendDeviceStatusAsync();
            }
        }

        #endregion
    }
}