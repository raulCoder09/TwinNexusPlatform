using System.IO;

namespace _Scripts.Models.MQTTManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using _Scripts.Models.CertificateManagement;
    using _Scripts.Models.FileManagement;
    using _Scripts.Models.MQTTManagement;

    public class MqttManager : MonoBehaviour
    {
        #region Singleton Pattern
        private static MqttManager _instance;
        public static MqttManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MqttManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MqttManager");
                        _instance = go.AddComponent<MqttManager>();
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

        private async void OnDestroy()
        {
            if (_instance == this)
            {
                await DisconnectAsync();
                _instance = null;
            }
        }
        #endregion

        // Configuración básica
        private string _brokerAddress = "";
        private int _brokerPort = 0;
        private string _clientId = "";
        private string _username = "";
        private string _password = "";
        private bool _useSSL = false;
        private bool _cleanSession = true;
        private int _connectionTimeoutMs = 30000;
        private int _keepAliveSeconds = 60;
        private int _reconnectDelayMs = 5000;

        // Configuración general
        private MqttInfo.ConnectionMode _connectionMode = MqttInfo.ConnectionMode.Manual;
        private List<string> _autoSubscribeTopics = new List<string>();
        private string _testTopic = "tnp/TNPSGA52/test";
        private bool _enableDebugLogs = true;

        private MqttConnectionHandler _connectionHandler;
        private MqttMessageHandler _messageHandler;
        private MqttEventManager _eventManager;
        private bool _isInitialized = false;

        public string BrokerAddress
        {
            get => _brokerAddress;
            set
            {
                _brokerAddress = value;
                if (_isInitialized)
                {
                    Initialize();
                }
            }
        }

        public int BrokerPort
        {
            get => _brokerPort;
            set
            {
                _brokerPort = value;
                if (_isInitialized)
                {
                    Initialize();
                }
            }
        }

        public bool UseSSL
        {
            get => _useSSL;
            set
            {
                _useSSL = value;
                if (_isInitialized)
                {
                    Initialize();
                }
            }
        }

        private void Start()
        {
            if (_connectionMode == MqttInfo.ConnectionMode.AutomaticOnStart || 
                _connectionMode == MqttInfo.ConnectionMode.AutomaticWithReconnection)
            {
                _ = ConnectAsync("", 0, "", "", "", false, "", "", "");
            }
        }

        private async void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus && _connectionMode == MqttInfo.ConnectionMode.AutomaticWithReconnection && !_connectionHandler.IsConnected())
            {
                LogDebug("Application resumed, attempting reconnection...");
                await _connectionHandler.ReconnectAsync();
            }
        }

        public void Initialize()
        {
            try
            {
                if (_isInitialized)
                {
                    LogDebug("MqttManager already initialized");
                    return;
                }

                LogDebug("Initializing MqttManager...");
                _eventManager = new MqttEventManager();
                _connectionHandler = new MqttConnectionHandler(CertificateManager.Instance, _eventManager);
                _messageHandler = new MqttMessageHandler(_connectionHandler, _eventManager);

                if (_autoSubscribeTopics.Count > 0)
                {
                    foreach (var topic in _autoSubscribeTopics)
                    {
                        _ = SubscribeAsync(topic, MqttInfo.QoSLevel.AtLeastOnce, null);
                    }
                }

                _isInitialized = true;
                LogDebug("MqttManager initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"Initialization error: {ex.Message}");
            }
        }

        public async Task<bool> ConnectAsync(string brokerAddress, int brokerPort, string clientId, string username, string password, bool useSSL, string certificatePath, string certificateFileName, string certificatePassword)
        {
            if (!_isInitialized) Initialize();
            string path = Path.Combine(FileManager.Instance.GetBasePath(useSSL ? "streaming" : "persistent"), certificatePath);
            return await _connectionHandler.ConnectAsync(brokerAddress, brokerPort, clientId, username, password, useSSL, path, certificateFileName, certificatePassword);
        }

        public async Task<bool> DisconnectAsync()
        {
            if (!_isInitialized) return false;
            return await _connectionHandler.DisconnectAsync();
        }

        public async Task<bool> ReconnectAsync()
        {
            if (!_isInitialized) return false;
            return await _connectionHandler.ReconnectAsync();
        }

        public async Task<bool> SubscribeAsync(string topic, MqttInfo.QoSLevel qos = MqttInfo.QoSLevel.AtLeastOnce, Action<string, string> messageHandler = null)
        {
            if (!_isInitialized) Initialize();
            bool subscribed = await _messageHandler.SubscribeAsync(topic, qos, messageHandler);
            if (subscribed && !_autoSubscribeTopics.Contains(topic))
            {
                _autoSubscribeTopics.Add(topic);
                LogDebug($"Added {topic} to auto-subscribe list");
            }
            return subscribed;
        }

        public async Task<bool> UnsubscribeAsync(string topic)
        {
            if (!_isInitialized) Initialize();
            bool unsubscribed = await _messageHandler.UnsubscribeAsync(topic);
            if (unsubscribed && _autoSubscribeTopics.Contains(topic))
            {
                _autoSubscribeTopics.Remove(topic);
                LogDebug($"Removed {topic} from auto-subscribe list");
            }
            return unsubscribed;
        }

        public async Task<bool> PublishAsync(string topic, string message, MqttInfo.QoSLevel qos = MqttInfo.QoSLevel.AtLeastOnce, bool retain = false)
        {
            if (!_isInitialized) Initialize();
            return await _messageHandler.PublishAsync(topic, message, qos, retain);
        }

        public async Task<bool> PublishObjectAsync<T>(string topic, T obj, MqttInfo.QoSLevel qos = MqttInfo.QoSLevel.AtLeastOnce, bool retain = false)
        {
            if (!_isInitialized) Initialize();
            return await _messageHandler.PublishObjectAsync(topic, obj, qos, retain);
        }

        public async Task<bool> SendTestMessageAsync(string customMessage = null)
        {
            if (!_isInitialized) Initialize();
            var testPayload = MqttInfo.CreateTestPayload(customMessage, _connectionHandler.GetClient()?.Options.ClientId);
            if (testPayload == null) return false;
            return await PublishObjectAsync(_testTopic, testPayload, MqttInfo.QoSLevel.AtLeastOnce);
        }

        public bool IsConnected()
        {
            return _connectionHandler != null && _connectionHandler.IsConnected();
        }

        public MqttInfo.ConnectionStatus GetConnectionStatus()
        {
            if (!_isInitialized) Initialize();
            return new MqttInfo.ConnectionStatus
            {
                isConnected = _connectionHandler.IsConnected(),
                brokerAddress = _brokerAddress,
                brokerPort = _brokerPort,
                clientId = _connectionHandler.GetClient()?.Options.ClientId,
                connectionMode = _connectionMode.ToString(),
                subscribedTopics = new List<string>(_autoSubscribeTopics)
            };
        }

        public void SubscribeToConnected(Action callback) => _eventManager.SubscribeToConnected(callback);
        public void UnsubscribeFromConnected(Action callback) => _eventManager.UnsubscribeFromConnected(callback);
        public void SubscribeToDisconnected(Action<string> callback) => _eventManager.SubscribeToDisconnected(callback);
        public void UnsubscribeFromDisconnected(Action<string> callback) => _eventManager.UnsubscribeFromDisconnected(callback);
        public void SubscribeToMessageReceived(Action<string, string> callback) => _eventManager.SubscribeToMessageReceived(callback);
        public void UnsubscribeFromMessageReceived(Action<string, string> callback) => _eventManager.UnsubscribeFromMessageReceived(callback);
        public void SubscribeToSubscribed(Action<string> callback) => _eventManager.SubscribeToSubscribed(callback);
        public void UnsubscribeFromSubscribed(Action<string> callback) => _eventManager.UnsubscribeFromSubscribed(callback);
        public void SubscribeToUnsubscribed(Action<string> callback) => _eventManager.SubscribeToUnsubscribed(callback);
        public void UnsubscribeFromUnsubscribed(Action<string> callback) => _eventManager.UnsubscribeFromUnsubscribed(callback);
        public void SubscribeToConnectionFailed(Action<string> callback) => _eventManager.SubscribeToConnectionFailed(callback);
        public void UnsubscribeFromConnectionFailed(Action<string> callback) => _eventManager.UnsubscribeFromConnectionFailed(callback);

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[MqttManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[MqttManager] {message}");
        }
    }
}