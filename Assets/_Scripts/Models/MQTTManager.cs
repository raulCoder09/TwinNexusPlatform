using System;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using System.Threading.Tasks;

namespace _Scripts.Models
{
    public class LocalIoT : MonoBehaviour
    {
        #region Private Fields
        private MqttFactory _factory;
        private IMqttClient _client;
        
        private string _brokerAddress;
        private int _brokerPort; 
        private string _clientId;
        private string _username;
        private string _password;
        private string _modeConnection;
        
        private const int DEFAULT_PORT = 1883;
        private const int CONNECTION_TIMEOUT = 30000;
        private const int DISCONNECT_TIMEOUT = 5000;
        private const int RECONNECT_DELAY = 5000;
        private const string TEST_TOPIC = "test/topic";
        #endregion

        #region Public Properties
        public string brokerAddress
        {
            get => _brokerAddress;
            set => _brokerAddress = value;
        }

        public int brokerPort
        {
            get => _brokerPort;
            set => _brokerPort = value;
        }

        public string username
        {
            get => _username;
            set => _username = value;
        }

        public string password
        {
            get => _password;
            set => _password = value;
        }

        public string clientId
        {
            get => _clientId;
            set => _clientId = value;
        }

        public string modeConnection
        {
            get => _modeConnection;
            set => _modeConnection = value;
        }
        #endregion

        #region Public Methods - API Principal
        /// <summary>
        /// Conecta al broker MQTT local
        /// </summary>
        /// <returns>True si la conexión fue exitosa</returns>
        public async Task<bool> ConnectToBroker()
        {
            try
            {
                if (!ValidateConnectionParameters())
                {
                    Debug.LogError("Invalid connection parameters");
                    return false;
                }

                if (!InitializeMqttClient())
                {
                    Debug.LogError("Failed to initialize MQTT client");
                    return false;
                }

                var options = BuildConnectionOptions();
                ConfigureEventHandlers();

                bool connected = await AttemptConnectionAsync(options);
                
                if (connected)
                {
                    Debug.Log($"Successfully connected to broker: {_brokerAddress}:{_brokerPort}");
                }
                else
                {
                    Debug.LogError("Failed to connect to broker");
                }

                return connected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error connecting to broker: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Desconecta del broker MQTT local
        /// </summary>
        /// <returns>True si la desconexión fue exitosa</returns>
        public async Task<bool> DisconnectFromBroker()
        {
            if (_client == null || !_client.IsConnected)
            {
                Debug.LogWarning("Client is not connected");
                return false;
            }

            try
            {
                await _client.DisconnectAsync();
                Debug.Log("Successfully disconnected from broker");
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
        /// <param name="qos">Quality of Service (0, 1, 2)</param>
        /// <returns>True si la suscripción fue exitosa</returns>
        public async Task<bool> SubscribeToTopic(string topic, int qos = 1)
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
                
                Debug.Log($"Successfully subscribed to topic: {topic}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error subscribing to topic '{topic}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Publica un mensaje en un topic específico
        /// </summary>
        /// <param name="topic">Topic de destino</param>
        /// <param name="message">Contenido del mensaje</param>
        /// <param name="qos">Quality of Service (0, 1, 2)</param>
        /// <returns>True si el mensaje fue enviado exitosamente</returns>
        public async Task<bool> PublishMessage(string topic, string message, int qos = 1)
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
                    .WithRetainFlag(false)
                    .Build();

                await _client.PublishAsync(mqttMessage);
                Debug.Log($"Message published to topic: {topic}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error publishing message to topic '{topic}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía un mensaje de prueba al topic de test
        /// </summary>
        /// <param name="message">Mensaje personalizado (opcional)</param>
        /// <returns>True si el mensaje fue enviado exitosamente</returns>
        public async Task<bool> SendTestMessage(string message = "Hello from twin nexus platform!")
        {
            var testPayload = new TestPayload
            {
                message = message,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                source = "Unity Android",
                clientId = _clientId
            };
    
            var json = JsonUtility.ToJson(testPayload);
            return await PublishMessage(TEST_TOPIC, json);
        }

        /// <summary>
        /// Verifica si el cliente está conectado al broker
        /// </summary>
        /// <returns>True si está conectado</returns>
        public bool IsConnected()
        {
            return _client != null && _client.IsConnected;
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

            if (_brokerPort <= 0)
            {
                Debug.LogWarning($"Invalid port {_brokerPort}, using default port {DEFAULT_PORT}");
                _brokerPort = DEFAULT_PORT;
            }

            if (string.IsNullOrEmpty(_clientId))
            {
                Debug.LogWarning("Client ID is empty, generating random ID");
                _clientId = $"Unity_Client_{Guid.NewGuid().ToString("N")[..8]}";
            }

            return true;
        }

        private bool InitializeMqttClient()
        {
            try
            {
                _factory = new MqttFactory();
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
                .WithTcpServer(_brokerAddress, _brokerPort)
                .WithCleanSession()
                .WithTimeout(TimeSpan.FromMilliseconds(CONNECTION_TIMEOUT));

            // Agregar credenciales solo si están configuradas
            if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
            {
                optionsBuilder.WithCredentials(_username, _password);
                Debug.Log("Using authentication credentials");
            }
            else
            {
                Debug.Log("Connecting without authentication");
            }

            return optionsBuilder.Build();
        }

        private void ConfigureEventHandlers()
        {
            _client.ConnectedAsync += async e =>
            {
                Debug.Log($"Connected to broker: {_brokerAddress}:{_brokerPort}");
            };

            _client.DisconnectedAsync += async e =>
            {
                Debug.Log($"Disconnected from broker. Reason: {e.Reason}");
                
                if (e.Exception != null)
                {
                    Debug.LogError($"Disconnection exception: {e.Exception.Message}");
                }

                // Auto-reconexión si está configurada
                if (e.Reason != MqttClientDisconnectReason.NormalDisconnection && 
                    _modeConnection == "Automatic connection")
                {
                    Debug.Log("Attempting automatic reconnection...");
                    await Task.Delay(RECONNECT_DELAY);
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
                var timeoutTask = Task.Delay(CONNECTION_TIMEOUT);
                
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
                Debug.Log("Client is already connected");
                return;
            }

            Debug.Log("Attempting to reconnect to broker...");
            
            try
            {
                var options = BuildConnectionOptions();
                bool reconnected = await AttemptConnectionAsync(options);
                
                if (reconnected)
                {
                    Debug.Log("Successfully reconnected to broker");
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

        private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                string topic = e.ApplicationMessage.Topic;
                string message = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                
                Debug.Log($"Message received on topic '{topic}': {message}");
                
                // Aquí puedes agregar lógica específica para manejar diferentes tipos de mensajes
                // Por ejemplo, parsear JSON, procesar comandos, etc.
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling received message: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }
        #endregion

        #region Unity Lifecycle
        private void OnDestroy()
        {
            if (_client != null && _client.IsConnected)
            {
                try
                {
                    _client.DisconnectAsync().Wait(DISCONNECT_TIMEOUT);
                    Debug.Log("MQTT client disconnected during cleanup");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error during cleanup: {ex.Message}");
                }
            }
        }
        #endregion

        #region Data Classes
        /// <summary>
        /// Estructura del payload para mensajes de prueba
        /// </summary>
        [System.Serializable]
        public class TestPayload
        {
            public string message;
            public string timestamp;
            public string source;
            public string clientId;
        }
        #endregion
    }
}