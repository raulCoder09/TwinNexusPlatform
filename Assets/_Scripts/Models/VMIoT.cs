using System;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using System.Threading.Tasks;

namespace _Scripts.Models
{
    public class VMIoT : MonoBehaviour
    {
        #region Private Fields
        private MqttFactory _factory;
        private IMqttClient _client;
        
        private string _ipOrHostname;
        private int _port;
        private string _clientId;
        private string _username;
        private string _password;
        private string _modeConnection;
        
        private const int DEFAULT_PORT = 1883;
        private const int CONNECTION_TIMEOUT = 30000;
        private const int DISCONNECT_TIMEOUT = 5000;
        private const int KEEP_ALIVE_SECONDS = 60;
        private const int RECONNECT_DELAY = 5000;
        private const string TEST_TOPIC = "test/topic";
        private const string EC2_COMMANDS_TOPIC_PREFIX = "ec2/commands/";
        #endregion

        #region Public Properties
        public string ipOrHostname
        {
            get => _ipOrHostname;
            set => _ipOrHostname = value;
        }

        public int port
        {
            get => _port;
            set => _port = value;
        }

        public string clientId
        {
            get => _clientId;
            set => _clientId = value;
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

        public string modeConnection
        {
            get => _modeConnection;
            set => _modeConnection = value;
        }
        #endregion

        #region Public Methods - API Principal
        /// <summary>
        /// Conecta al broker MQTT en la VM/EC2
        /// </summary>
        /// <returns>True si la conexión fue exitosa</returns>
        public async Task<bool> ConnectToEC2Broker()
        {
            try
            {
                if (!ValidateConnectionParameters())
                {
                    Debug.LogError("Invalid connection parameters for VM/EC2");
                    return false;
                }

                if (!InitializeMqttClient())
                {
                    Debug.LogError("Failed to initialize MQTT client for VM/EC2");
                    return false;
                }

                var options = BuildConnectionOptions();
                ConfigureEventHandlers();

                bool connected = await AttemptConnectionAsync(options);
                
                if (connected)
                {
                    Debug.Log($"Successfully connected to VM/EC2 broker: {_ipOrHostname}:{_port}");
                    await SubscribeToDefaultTopics();
                }
                else
                {
                    Debug.LogError("Failed to connect to VM/EC2 broker");
                }

                return connected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error connecting to VM/EC2 broker: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Desconecta del broker MQTT en la VM/EC2
        /// </summary>
        /// <returns>True si la desconexión fue exitosa</returns>
        public async Task<bool> DisconnectFromEC2Broker()
        {
            if (_client == null || !_client.IsConnected)
            {
                Debug.LogWarning("VM/EC2 client is not connected");
                return false;
            }

            try
            {
                await _client.DisconnectAsync();
                Debug.Log("Successfully disconnected from VM/EC2 broker");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error disconnecting from VM/EC2 broker: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Se suscribe a un topic específico en la VM/EC2
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
                Debug.LogWarning("Cannot subscribe: VM/EC2 client not connected");
                return false;
            }

            try
            {
                await _client.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .Build());
                
                Debug.Log($"Successfully subscribed to VM/EC2 topic: {topic}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error subscribing to VM/EC2 topic '{topic}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Publica un mensaje en un topic específico de la VM/EC2
        /// </summary>
        /// <param name="topic">Topic de destino</param>
        /// <param name="message">Contenido del mensaje</param>
        /// <param name="qos">Quality of Service (0, 1, 2)</param>
        /// <param name="retain">Si el mensaje debe ser retenido por el broker</param>
        /// <returns>True si el mensaje fue enviado exitosamente</returns>
        public async Task<bool> PublishMessage(string topic, string message, int qos = 1, bool retain = false)
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
                Debug.LogWarning("Cannot publish message: VM/EC2 client not connected");
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
                Debug.Log($"Message published to VM/EC2 topic: {topic}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error publishing message to VM/EC2 topic '{topic}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía un mensaje de prueba estructurado a la VM/EC2
        /// </summary>
        /// <param name="message">Mensaje personalizado (opcional)</param>
        /// <returns>True si el mensaje fue enviado exitosamente</returns>
        public async Task<bool> SendTestMessageToEC2(string message = "Hello from twin nexus platform to EC2!")
        {
            var testPayload = new TestPayload
            {
                message = message,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                source = "Unity Android",
                clientId = _clientId,
                destination = "VM/EC2"
            };
    
            var json = JsonUtility.ToJson(testPayload, true);
            return await PublishMessage(TEST_TOPIC, json);
        }

        /// <summary>
        /// Envía un payload específico de EC2 con información extendida
        /// </summary>
        /// <param name="message">Mensaje principal</param>
        /// <param name="deviceType">Tipo de dispositivo</param>
        /// <param name="location">Ubicación del dispositivo</param>
        /// <returns>True si el mensaje fue enviado exitosamente</returns>
        public async Task<bool> SendEC2Payload(string message, string deviceType = "Unity Device", string location = "Mobile")
        {
            var ec2Payload = new EC2Payload
            {
                Message = message,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                Source = "Unity Android",
                DeviceType = deviceType,
                Location = location,
                ClientId = _clientId
            };

            var json = JsonUtility.ToJson(ec2Payload, true);
            return await PublishMessage("ec2/data", json);
        }

        /// <summary>
        /// Verifica si el cliente está conectado a la VM/EC2
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
            if (string.IsNullOrEmpty(_ipOrHostname))
            {
                Debug.LogError("VM/EC2 IP or hostname is required");
                return false;
            }

            if (_port <= 0)
            {
                Debug.LogWarning($"Invalid port {_port}, using default port {DEFAULT_PORT}");
                _port = DEFAULT_PORT;
            }

            if (string.IsNullOrEmpty(_clientId))
            {
                Debug.LogWarning("Client ID is empty, generating random ID for VM/EC2");
                _clientId = $"VM_Client_{Guid.NewGuid().ToString("N")[..8]}";
            }

            // Para VM/EC2, las credenciales son típicamente requeridas
            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
            {
                Debug.LogWarning("VM/EC2 connection without credentials may fail");
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
                Debug.LogError($"Failed to initialize MQTT client for VM/EC2: {ex.Message}");
                return false;
            }
        }

        private MqttClientOptions BuildConnectionOptions()
        {
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(_clientId)
                .WithTcpServer(_ipOrHostname, _port)
                .WithCleanSession(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(KEEP_ALIVE_SECONDS))
                .WithTimeout(TimeSpan.FromMilliseconds(CONNECTION_TIMEOUT));

            // Agregar credenciales si están configuradas
            if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
            {
                optionsBuilder.WithCredentials(_username, _password);
                Debug.Log("Using authentication credentials for VM/EC2");
            }
            else
            {
                Debug.Log("Connecting to VM/EC2 without authentication");
            }

            return optionsBuilder.Build();
        }

        private void ConfigureEventHandlers()
        {
            _client.ConnectedAsync += OnConnectedAsync;
            _client.DisconnectedAsync += OnDisconnectedAsync;
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
                    Debug.LogError("VM/EC2 connection timeout");
                    return false;
                }
                
                await connectTask;
                return _client.IsConnected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"VM/EC2 connection attempt failed: {ex.Message}");
                return false;
            }
        }

        private async Task SubscribeToDefaultTopics()
        {
            try
            {
                // Suscribirse a topics por defecto relevantes para VM/EC2
                await SubscribeToTopic(TEST_TOPIC);
                await SubscribeToTopic($"{EC2_COMMANDS_TOPIC_PREFIX}+"); // Wildcard para todos los comandos
                await SubscribeToTopic("ec2/status");
                
                Debug.Log("Subscribed to default VM/EC2 topics");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error subscribing to default VM/EC2 topics: {ex.Message}");
            }
        }

        private async Task ReconnectToEC2Broker()
        {
            if (_client == null)
            {
                Debug.LogError("Cannot reconnect: VM/EC2 client is null");
                return;
            }

            if (_client.IsConnected)
            {
                Debug.Log("VM/EC2 client is already connected");
                return;
            }

            Debug.Log("Attempting to reconnect to VM/EC2 broker...");
            
            try
            {
                var options = BuildConnectionOptions();
                bool reconnected = await AttemptConnectionAsync(options);
                
                if (reconnected)
                {
                    Debug.Log("Successfully reconnected to VM/EC2 broker");
                    await SubscribeToDefaultTopics();
                }
                else
                {
                    Debug.LogError("Failed to reconnect to VM/EC2 broker");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during VM/EC2 reconnection: {ex.Message}");
            }
        }
        #endregion

        #region Event Handlers
        private Task OnConnectedAsync(MqttClientConnectedEventArgs e)
        {
            Debug.Log($"Connected to VM/EC2 broker: {_ipOrHostname}:{_port}");
            return Task.CompletedTask;
        }
        
        private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            Debug.Log($"Disconnected from VM/EC2 broker. Reason: {e.Reason}");
            
            if (e.Exception != null)
            {
                Debug.LogError($"VM/EC2 disconnection exception: {e.Exception.Message}");
            }

            // Auto-reconexión si está configurada
            if (e.Reason != MqttClientDisconnectReason.NormalDisconnection && 
                _modeConnection == "Automatic connection")
            {
                Debug.Log("Attempting automatic reconnection to VM/EC2...");
                await Task.Delay(RECONNECT_DELAY);
                await ReconnectToEC2Broker();
            }
        }
        
        private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                string topic = e.ApplicationMessage.Topic;
                string message = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                
                Debug.Log($"VM/EC2 message received on topic '{topic}': {message}");
                
                // Procesar diferentes tipos de mensajes basados en el topic
                switch (topic)
                {
                    case var t when t.StartsWith(EC2_COMMANDS_TOPIC_PREFIX):
                        ProcessEC2Command(topic, message);
                        break;
                    
                    case "ec2/status":
                        ProcessStatusMessage(message);
                        break;
                    
                    case TEST_TOPIC:
                        ProcessTestMessage(message);
                        break;
                    
                    default:
                        Debug.Log($"Unhandled topic: {topic}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling VM/EC2 received message: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }

        private void ProcessEC2Command(string topic, string message)
        {
            Debug.Log($"Processing EC2 command from topic: {topic}");
            // Aquí puedes agregar lógica específica para comandos de EC2
        }

        private void ProcessStatusMessage(string message)
        {
            Debug.Log($"Processing EC2 status message: {message}");
            // Aquí puedes agregar lógica para procesar mensajes de estado
        }

        private void ProcessTestMessage(string message)
        {
            Debug.Log($"Processing test message: {message}");
            // Aquí puedes agregar lógica para mensajes de prueba
        }
        #endregion

        #region Unity Lifecycle
        private async void OnDestroy()
        {
            if (_client != null && _client.IsConnected)
            {
                try
                {
                    await _client.DisconnectAsync();
                    Debug.Log("VM/EC2 MQTT client disconnected during cleanup");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error during VM/EC2 cleanup: {ex.Message}");
                }
            }
        }
        #endregion

        #region Data Classes
        /// <summary>
        /// Estructura del payload para mensajes de prueba generales
        /// </summary>
        [System.Serializable]
        public class TestPayload
        {
            public string message;
            public string timestamp;
            public string source;
            public string clientId;
            public string destination;
        }

        /// <summary>
        /// Estructura del payload específico para EC2 con información extendida
        /// </summary>
        [System.Serializable]
        public class EC2Payload
        {
            public string Message;
            public string Timestamp;
            public string Source;
            public string DeviceType;
            public string Location;
            public string ClientId;
        }
        #endregion
    }
}