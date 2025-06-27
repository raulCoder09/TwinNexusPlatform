using System;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace _Scripts.Models
{
    public class MQTTManager : MonoBehaviour
    {
        #region Singleton Implementation
        private static MQTTManager _instance;
        private static readonly object _lock = new object();

        public static MQTTManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // Buscar instancia existente en la escena
                            _instance = FindObjectOfType<MQTTManager>();
                            
                            if (_instance == null)
                            {
                                // Crear nuevo GameObject con MQTTManager
                                GameObject mqttObject = new GameObject("MQTTManager");
                                _instance = mqttObject.AddComponent<MQTTManager>();
                                DontDestroyOnLoad(mqttObject);
                            }
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Serialized Configuration Fields
        [Header("MQTT Broker Configuration")]
        [SerializeField] private string _brokerAddress = "localhost";
        [SerializeField] private int _brokerPort = 1883;
        [SerializeField] private string _clientId = "";
        
        [Header("Authentication (Optional)")]
        [SerializeField] private string _username = "";
        [SerializeField] private string _password = "";
        
        [Header("Connection Settings")]
        [SerializeField] private ConnectionMode _connectionMode = ConnectionMode.Manual;
        [SerializeField] private bool _useSSL = false;
        [SerializeField] private bool _cleanSession = true;
        [SerializeField] private int _connectionTimeoutMs = 30000;
        [SerializeField] private int _keepAliveSeconds = 60;
        [SerializeField] private int _reconnectDelayMs = 5000;
        
        [Header("Default Topics")]
        [SerializeField] private string _testTopic = "test/topic";
        [SerializeField] private List<string> _autoSubscribeTopics = new List<string>();
        
        [Header("Debug Settings")]
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private bool _logReceivedMessages = true;
        #endregion

        #region Private Fields
        private MqttFactory _factory;
        private IMqttClient _client;
        private bool _isInitialized = false;
        private Dictionary<string, System.Action<string, string>> _topicHandlers = new Dictionary<string, System.Action<string, string>>();
        
        private const int DISCONNECT_TIMEOUT = 5000;
        #endregion

        #region Enums
        public enum ConnectionMode
        {
            Manual,
            AutomaticOnStart,
            AutomaticWithReconnection
        }

        public enum QoSLevel
        {
            AtMostOnce = 0,
            AtLeastOnce = 1,
            ExactlyOnce = 2
        }
        #endregion

        #region Events
        public event System.Action OnConnected;
        public event System.Action<string> OnDisconnected;
        public event System.Action<string, string> OnMessageReceived;
        public event System.Action<string> OnSubscribed;
        public event System.Action<string> OnUnsubscribed;
        public event System.Action<string> OnConnectionFailed;
        #endregion

        #region Public Properties
        public string BrokerAddress
        {
            get => _brokerAddress;
            set => _brokerAddress = value;
        }

        public int BrokerPort
        {
            get => _brokerPort;
            set => _brokerPort = value;
        }

        public string Username
        {
            get => _username;
            set => _username = value;
        }

        public string Password
        {
            get => _password;
            set => _password = value;
        }

        public string ClientId
        {
            get => _clientId;
            set => _clientId = value;
        }

        public ConnectionMode Mode
        {
            get => _connectionMode;
            set => _connectionMode = value;
        }

        public bool UseSSL
        {
            get => _useSSL;
            set => _useSSL = value;
        }

        public bool IsInitialized => _isInitialized;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Implementar Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManager();
            }
            else if (_instance != this)
            {
                LogDebug("Another MQTTManager instance already exists. Destroying this one.");
                Destroy(gameObject);
            }
        }

        private async void Start()
        {
            if (_connectionMode == ConnectionMode.AutomaticOnStart || 
                _connectionMode == ConnectionMode.AutomaticWithReconnection)
            {
                await ConnectToBroker();
            }
        }

        private async void OnDestroy()
        {
            if (_instance == this)
            {
                await CleanupConnection();
                _instance = null;
            }
        }

        private async void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                LogDebug("Application paused - maintaining MQTT connection");
            }
            else
            {
                LogDebug("Application resumed");
                if (_connectionMode == ConnectionMode.AutomaticWithReconnection && !IsConnected())
                {
                    await ReconnectToBroker();
                }
            }
        }
        #endregion

        #region Public Methods - Main API
        /// <summary>
        /// Inicializa el manager MQTT
        /// </summary>
        public void InitializeManager()
        {
            if (_isInitialized) return;

            try
            {
                _factory = new MqttFactory();
                _topicHandlers.Clear();
                
                // Generar Client ID si está vacío
                if (string.IsNullOrEmpty(_clientId))
                {
                    _clientId = $"Unity_{SystemInfo.deviceModel}_{Guid.NewGuid().ToString("N")[..8]}";
                }

                _isInitialized = true;
                LogDebug("MQTT Manager initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize MQTT Manager: {ex.Message}");
            }
        }

        /// <summary>
        /// Conecta al broker MQTT
        /// </summary>
        /// <returns>True si la conexión fue exitosa</returns>
        public async Task<bool> ConnectToBroker()
        {
            if (!_isInitialized)
            {
                InitializeManager();
            }

            try
            {
                if (!ValidateConnectionParameters())
                {
                    OnConnectionFailed?.Invoke("Invalid connection parameters");
                    return false;
                }

                if (!InitializeMqttClient())
                {
                    OnConnectionFailed?.Invoke("Failed to initialize MQTT client");
                    return false;
                }

                var options = BuildConnectionOptions();
                ConfigureEventHandlers();

                bool connected = await AttemptConnectionAsync(options);
                
                if (connected)
                {
                    LogDebug($"Successfully connected to broker: {_brokerAddress}:{_brokerPort}");
                    OnConnected?.Invoke();
                    
                    // Auto-suscribirse a topics configurados
                    await SubscribeToConfiguredTopics();
                }
                else
                {
                    OnConnectionFailed?.Invoke("Failed to connect to broker");
                }

                return connected;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error connecting to broker: {ex.Message}";
                Debug.LogError(errorMsg);
                OnConnectionFailed?.Invoke(errorMsg);
                return false;
            }
        }

        /// <summary>
        /// Desconecta del broker MQTT
        /// </summary>
        /// <returns>True si la desconexión fue exitosa</returns>
        public async Task<bool> DisconnectFromBroker()
        {
            if (_client == null || !_client.IsConnected)
            {
                LogDebug("Client is not connected");
                return false;
            }

            try
            {
                await _client.DisconnectAsync();
                LogDebug("Successfully disconnected from broker");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error disconnecting from broker: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Se suscribe a un topic específico
        /// </summary>
        /// <param name="topic">Topic al cual suscribirse</param>
        /// <param name="qos">Quality of Service</param>
        /// <param name="messageHandler">Handler opcional para mensajes de este topic</param>
        /// <returns>True si la suscripción fue exitosa</returns>
        public async Task<bool> SubscribeToTopic(string topic, QoSLevel qos = QoSLevel.AtLeastOnce, System.Action<string, string> messageHandler = null)
        {
            if (string.IsNullOrEmpty(topic))
            {
                Debug.LogError("Topic cannot be null or empty");
                return false;
            }

            if (!IsConnected())
            {
                Debug.LogWarning("Cannot subscribe: Client not connected");
                return false;
            }

            try
            {
                await _client.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .Build());
                
                // Registrar handler específico si se proporciona
                if (messageHandler != null)
                {
                    _topicHandlers[topic] = messageHandler;
                }
                
                LogDebug($"Successfully subscribed to topic: {topic}");
                OnSubscribed?.Invoke(topic);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error subscribing to topic '{topic}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Se desuscribe de un topic específico
        /// </summary>
        /// <param name="topic">Topic del cual desuscribirse</param>
        /// <returns>True si la desuscripción fue exitosa</returns>
        public async Task<bool> UnsubscribeFromTopic(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                Debug.LogError("Topic cannot be null or empty");
                return false;
            }

            if (!IsConnected())
            {
                Debug.LogWarning("Cannot unsubscribe: Client not connected");
                return false;
            }

            try
            {
                await _client.UnsubscribeAsync(topic);
                
                // Remover handler específico
                if (_topicHandlers.ContainsKey(topic))
                {
                    _topicHandlers.Remove(topic);
                }
                
                LogDebug($"Successfully unsubscribed from topic: {topic}");
                OnUnsubscribed?.Invoke(topic);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error unsubscribing from topic '{topic}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Publica un mensaje en un topic específico
        /// </summary>
        /// <param name="topic">Topic de destino</param>
        /// <param name="message">Contenido del mensaje</param>
        /// <param name="qos">Quality of Service</param>
        /// <param name="retain">Si el mensaje debe ser retenido</param>
        /// <returns>True si el mensaje fue enviado exitosamente</returns>
        public async Task<bool> PublishMessage(string topic, string message, QoSLevel qos = QoSLevel.AtLeastOnce, bool retain = false)
        {
            if (string.IsNullOrEmpty(topic))
            {
                Debug.LogError("Topic cannot be null or empty");
                return false;
            }

            if (string.IsNullOrEmpty(message))
            {
                Debug.LogError("Message cannot be null or empty");
                return false;
            }

            if (!IsConnected())
            {
                Debug.LogWarning("Cannot publish message: Client not connected");
                return false;
            }

            try
            {
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(message)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .WithRetainFlag(retain)
                    .Build();

                await _client.PublishAsync(mqttMessage);
                LogDebug($"Message published to topic: {topic}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error publishing message to topic '{topic}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Publica un objeto como JSON
        /// </summary>
        /// <param name="topic">Topic de destino</param>
        /// <param name="obj">Objeto a serializar</param>
        /// <param name="qos">Quality of Service</param>
        /// <param name="retain">Si el mensaje debe ser retenido</param>
        /// <returns>True si el mensaje fue enviado exitosamente</returns>
        public async Task<bool> PublishObject<T>(string topic, T obj, QoSLevel qos = QoSLevel.AtLeastOnce, bool retain = false)
        {
            try
            {
                string json = JsonUtility.ToJson(obj);
                return await PublishMessage(topic, json, qos, retain);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error serializing object for topic '{topic}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía un mensaje de prueba al topic de test
        /// </summary>
        /// <param name="customMessage">Mensaje personalizado (opcional)</param>
        /// <returns>True si el mensaje fue enviado exitosamente</returns>
        public async Task<bool> SendTestMessage(string customMessage = null)
        {
            var testPayload = new TestPayload
            {
                message = customMessage ?? "Hello from Unity MQTT Manager!",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                source = $"Unity {Application.platform}",
                clientId = _clientId,
                deviceInfo = new DeviceInfo
                {
                    model = SystemInfo.deviceModel,
                    os = SystemInfo.operatingSystem,
                    processor = SystemInfo.processorType,
                    memory = SystemInfo.systemMemorySize
                }
            };

            return await PublishObject(_testTopic, testPayload);
        }

        /// <summary>
        /// Verifica si el cliente está conectado al broker
        /// </summary>
        /// <returns>True si está conectado</returns>
        public bool IsConnected()
        {
            return _client != null && _client.IsConnected;
        }

        /// <summary>
        /// Obtiene estadísticas de conexión
        /// </summary>
        /// <returns>Información de estado de la conexión</returns>
        public ConnectionStatus GetConnectionStatus()
        {
            return new ConnectionStatus
            {
                isConnected = IsConnected(),
                brokerAddress = _brokerAddress,
                brokerPort = _brokerPort,
                clientId = _clientId,
                connectionMode = _connectionMode.ToString(),
                subscribedTopics = new List<string>(_topicHandlers.Keys)
            };
        }
        #endregion

        #region Private Helper Methods
        private bool ValidateConnectionParameters()
        {
            if (string.IsNullOrEmpty(_brokerAddress))
            {
                Debug.LogError("Broker address is required");
                return false;
            }

            if (_brokerPort <= 0 || _brokerPort > 65535)
            {
                Debug.LogWarning($"Invalid port {_brokerPort}, using default port 1883");
                _brokerPort = 1883;
            }

            return true;
        }

        private bool InitializeMqttClient()
        {
            try
            {
                if (_client != null)
                {
                    _client.Dispose();
                }
                
                _client = _factory.CreateMqttClient();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize MQTT client: {ex.Message}");
                return false;
            }
        }

        private MqttClientOptions BuildConnectionOptions()
        {
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(_clientId)
                .WithTimeout(TimeSpan.FromMilliseconds(_connectionTimeoutMs))
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_keepAliveSeconds));

            // Configurar servidor (TCP o SSL)
            if (_useSSL)
            {
                // Para SSL/TLS, usar puerto 8883 por defecto si aún es 1883
                int sslPort = _brokerPort == 1883 ? 8883 : _brokerPort;
                optionsBuilder.WithTcpServer(_brokerAddress, sslPort)
                             .WithTls(); // Método simplificado para habilitar TLS
                LogDebug($"Using SSL/TLS connection on port {sslPort}");
            }
            else
            {
                optionsBuilder.WithTcpServer(_brokerAddress, _brokerPort);
                LogDebug($"Using TCP connection on port {_brokerPort}");
            }

            // Configurar sesión
            if (_cleanSession)
            {
                optionsBuilder.WithCleanSession();
            }

            // Agregar credenciales solo si están configuradas
            if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
            {
                optionsBuilder.WithCredentials(_username, _password);
                LogDebug("Using authentication credentials");
            }
            else
            {
                LogDebug("Connecting without authentication");
            }

            return optionsBuilder.Build();
        }

        private void ConfigureEventHandlers()
        {
            _client.ConnectedAsync += async e =>
            {
                LogDebug($"Connected to broker: {_brokerAddress}:{_brokerPort}");
            };

            _client.DisconnectedAsync += async e =>
            {
                string reason = e.Reason.ToString();
                LogDebug($"Disconnected from broker. Reason: {reason}");
                
                if (e.Exception != null)
                {
                    Debug.LogError($"Disconnection exception: {e.Exception.Message}");
                }

                OnDisconnected?.Invoke(reason);

                // Auto-reconexión si está configurada
                if (e.Reason != MqttClientDisconnectReason.NormalDisconnection && 
                    _connectionMode == ConnectionMode.AutomaticWithReconnection)
                {
                    LogDebug("Attempting automatic reconnection...");
                    await Task.Delay(_reconnectDelayMs);
                    await ReconnectToBroker();
                }
            };
            
            _client.ApplicationMessageReceivedAsync += HandleReceivedMessage;
        }

        private async Task<bool> AttemptConnectionAsync(MqttClientOptions options)
        {
            try
            {
                var connectTask = _client.ConnectAsync(options);
                var timeoutTask = Task.Delay(_connectionTimeoutMs);
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Debug.LogError("Connection timeout");
                    return false;
                }
                
                await connectTask;
                return _client.IsConnected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection attempt failed: {ex.Message}");
                return false;
            }
        }

        private async Task ReconnectToBroker()
        {
            if (_client == null)
            {
                Debug.LogError("Cannot reconnect: Client is null");
                return;
            }

            if (_client.IsConnected)
            {
                LogDebug("Client is already connected");
                return;
            }

            LogDebug("Attempting to reconnect to broker...");
            
            try
            {
                var options = BuildConnectionOptions();
                bool reconnected = await AttemptConnectionAsync(options);
                
                if (reconnected)
                {
                    LogDebug("Successfully reconnected to broker");
                    await SubscribeToConfiguredTopics();
                }
                else
                {
                    Debug.LogError("Failed to reconnect to broker");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during reconnection: {ex.Message}");
            }
        }

        private async Task SubscribeToConfiguredTopics()
        {
            foreach (string topic in _autoSubscribeTopics)
            {
                if (!string.IsNullOrEmpty(topic))
                {
                    await SubscribeToTopic(topic);
                }
            }
        }

        private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                string topic = e.ApplicationMessage.Topic;
                string message = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                
                if (_logReceivedMessages)
                {
                    LogDebug($"Message received on topic '{topic}': {message}");
                }

                // Invocar handler específico del topic si existe
                if (_topicHandlers.ContainsKey(topic))
                {
                    _topicHandlers[topic]?.Invoke(topic, message);
                }

                // Invocar evento general
                OnMessageReceived?.Invoke(topic, message);
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling received message: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }

        private async Task CleanupConnection()
        {
            if (_client != null && _client.IsConnected)
            {
                try
                {
                    await _client.DisconnectAsync();
                    LogDebug("MQTT client disconnected during cleanup");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error during cleanup: {ex.Message}");
                }
            }

            _client?.Dispose();
            _topicHandlers?.Clear();
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[MQTTManager] {message}");
            }
        }
        #endregion

        #region Data Classes
        [System.Serializable]
        public class TestPayload
        {
            public string message;
            public string timestamp;
            public string source;
            public string clientId;
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

        [System.Serializable]
        public class ConnectionStatus
        {
            public bool isConnected;
            public string brokerAddress;
            public int brokerPort;
            public string clientId;
            public string connectionMode;
            public List<string> subscribedTopics;
        }
        #endregion
    }
}