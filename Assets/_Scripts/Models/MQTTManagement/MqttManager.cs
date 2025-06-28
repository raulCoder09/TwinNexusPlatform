using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using _Scripts.Models.CertificateManagement;
using _Scripts.Models.FileManagement;
using _Scripts.Models.Mqtt;

namespace _Scripts.Models.MQTTManagement
{
    public class MqttManager : MonoBehaviour
    {
        #region Singleton Pattern
        private static MqttManager _instance;
        private static readonly object _lock = new object();

        public static MqttManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            GameObject mqttObject = new GameObject("MqttManager");
                            _instance = mqttObject.AddComponent<MqttManager>();
                            DontDestroyOnLoad(mqttObject);
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
                Initialize();
            }
            else if (_instance != this)
            {
                LogDebug("Another MqttManager instance exists. Destroying this one.");
                Destroy(gameObject);
            }
        }

        private async void OnDestroy()
        {
            if (_instance == this)
            {
                await DisconnectAsync();
                _instance = null;
            }
        }
        #endregion

        [Header("MQTT Broker Configuration")]
        [SerializeField] private string brokerAddress = "localhost";
        [SerializeField] private int brokerPort = 1883;
        [SerializeField] private string clientId = "";
        [SerializeField] private string username = "";
        [SerializeField] private string password = "";
        [SerializeField] private bool useSSL = false;
        [SerializeField] private bool cleanSession = true;
        [SerializeField] private int connectionTimeoutMs = 30000;
        [SerializeField] private int keepAliveSeconds = 60;
        [SerializeField] private int reconnectDelayMs = 5000;
        [SerializeField] private MqttInfo.ConnectionMode connectionMode = MqttInfo.ConnectionMode.Manual;
        [SerializeField] private List<string> autoSubscribeTopics = new List<string>();
        [SerializeField] private string testTopic = "test/topic";
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool logReceivedMessages = true;

        private MqttConnectionHandler connectionHandler;
        private MqttMessageHandler messageHandler;
        private MqttEventManager eventManager;
        private bool isInitialized = false;

        public string BrokerAddress
        {
            get => brokerAddress;
            set
            {
                brokerAddress = value;
                if (isInitialized)
                {
                    connectionHandler = new MqttConnectionHandler(brokerAddress, brokerPort, clientId, username, password,
                        useSSL, cleanSession, connectionTimeoutMs, keepAliveSeconds, reconnectDelayMs,
                        CertificateManager.Instance, eventManager);
                }
            }
        }

        public int BrokerPort
        {
            get => brokerPort;
            set
            {
                brokerPort = value;
                if (isInitialized)
                {
                    connectionHandler = new MqttConnectionHandler(brokerAddress, brokerPort, clientId, username, password,
                        useSSL, cleanSession, connectionTimeoutMs, keepAliveSeconds, reconnectDelayMs,
                        CertificateManager.Instance, eventManager);
                }
            }
        }

        public bool UseSSL
        {
            get => useSSL;
            set
            {
                useSSL = value;
                if (isInitialized)
                {
                    connectionHandler = new MqttConnectionHandler(brokerAddress, brokerPort, clientId, username, password,
                        useSSL, cleanSession, connectionTimeoutMs, keepAliveSeconds, reconnectDelayMs,
                        CertificateManager.Instance, eventManager);
                }
            }
        }

        private void Start()
        {
            if (connectionMode == MqttInfo.ConnectionMode.AutomaticOnStart || 
                connectionMode == MqttInfo.ConnectionMode.AutomaticWithReconnection)
            {
                _ = ConnectAsync();
            }
        }

        private async void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus && connectionMode == MqttInfo.ConnectionMode.AutomaticWithReconnection && !connectionHandler.IsConnected())
            {
                LogDebug("Application resumed, attempting reconnection...");
                await connectionHandler.ReconnectAsync();
            }
        }

        public void Initialize()
        {
            if (isInitialized)
            {
                LogDebug("MqttManager already initialized");
                return;
            }

            try
            {
                LogDebug("Initializing MqttManager...");
                eventManager = new MqttEventManager();
                connectionHandler = new MqttConnectionHandler(brokerAddress, brokerPort, clientId, username, password,
                    useSSL, cleanSession, connectionTimeoutMs, keepAliveSeconds, reconnectDelayMs,
                    CertificateManager.Instance, eventManager);
                messageHandler = new MqttMessageHandler(connectionHandler, eventManager);
                eventManager.SubscribeToMessageReceived(messageHandler.HandleReceivedMessage);
                isInitialized = true;
                LogDebug("MqttManager initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"Initialization error: {ex.Message}");
            }
        }

        public async Task<bool> ConnectAsync()
        {
            if (!isInitialized) Initialize();
            return await connectionHandler.ConnectAsync();
        }

        public async Task<bool> DisconnectAsync()
        {
            if (!isInitialized) return false;
            return await connectionHandler.DisconnectAsync();
        }

        public async Task<bool> SubscribeToTopicAsync(string topic, MqttInfo.QoSLevel qos = MqttInfo.QoSLevel.AtLeastOnce, Action<string, string> messageHandler = null)
        {
            if (!isInitialized) Initialize();
            bool subscribed = await this.messageHandler.SubscribeToTopicAsync(topic, qos, messageHandler);
            if (subscribed && !autoSubscribeTopics.Contains(topic))
            {
                autoSubscribeTopics.Add(topic);
                LogDebug($"Added {topic} to auto-subscribe list");
            }
            return subscribed;
        }

        public async Task<bool> UnsubscribeFromTopicAsync(string topic)
        {
            if (!isInitialized) Initialize();
            bool unsubscribed = await this.messageHandler.UnsubscribeFromTopicAsync(topic);
            if (unsubscribed && autoSubscribeTopics.Contains(topic))
            {
                autoSubscribeTopics.Remove(topic);
                LogDebug($"Removed {topic} from auto-subscribe list");
            }
            return unsubscribed;
        }

        public async Task<bool> PublishMessageAsync(string topic, string message, MqttInfo.QoSLevel qos = MqttInfo.QoSLevel.AtLeastOnce, bool retain = false)
        {
            if (!isInitialized) Initialize();
            return await this.messageHandler.PublishMessageAsync(topic, message, qos, retain);
        }

        public async Task<bool> PublishObjectAsync<T>(string topic, T obj, MqttInfo.QoSLevel qos = MqttInfo.QoSLevel.AtLeastOnce, bool retain = false)
        {
            if (!isInitialized) Initialize();
            return await this.messageHandler.PublishObjectAsync(topic, obj, qos, retain);
        }

        public async Task<bool> SendTestMessageAsync(string customMessage = null)
        {
            if (!isInitialized) Initialize();
            return await this.messageHandler.SendTestMessageAsync(testTopic, customMessage);
        }

        public bool IsConnected()
        {
            return connectionHandler != null && connectionHandler.IsConnected();
        }

        public MqttInfo.ConnectionStatus GetConnectionStatus()
        {
            if (!isInitialized) Initialize();
            return new MqttInfo.ConnectionStatus
            {
                isConnected = connectionHandler.IsConnected(),
                brokerAddress = brokerAddress,
                brokerPort = brokerPort,
                clientId = connectionHandler.GetClient()?.Options.ClientId,
                connectionMode = connectionMode.ToString(),
                subscribedTopics = new List<string>(autoSubscribeTopics)
            };
        }

        public void SubscribeToConnected(Action callback) => eventManager.SubscribeToConnected(callback);
        public void UnsubscribeFromConnected(Action callback) => eventManager.UnsubscribeFromConnected(callback);
        public void SubscribeToDisconnected(Action<string> callback) => eventManager.SubscribeToDisconnected(callback);
        public void UnsubscribeFromDisconnected(Action<string> callback) => eventManager.UnsubscribeFromDisconnected(callback);
        public void SubscribeToMessageReceived(Action<string, string> callback) => eventManager.SubscribeToMessageReceived(callback);
        public void UnsubscribeFromMessageReceived(Action<string, string> callback) => eventManager.UnsubscribeFromMessageReceived(callback);
        public void SubscribeToSubscribed(Action<string> callback) => eventManager.SubscribeToSubscribed(callback);
        public void UnsubscribeFromSubscribed(Action<string> callback) => eventManager.UnsubscribeFromSubscribed(callback);
        public void SubscribeToUnsubscribed(Action<string> callback) => eventManager.SubscribeToUnsubscribed(callback);
        public void UnsubscribeFromUnsubscribed(Action<string> callback) => eventManager.UnsubscribeFromUnsubscribed(callback);
        public void SubscribeToConnectionFailed(Action<string> callback) => eventManager.SubscribeToConnectionFailed(callback);
        public void UnsubscribeFromConnectionFailed(Action<string> callback) => eventManager.UnsubscribeFromConnectionFailed(callback);

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[MqttManager] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[MqttManager] {message}");
        }
    }
}