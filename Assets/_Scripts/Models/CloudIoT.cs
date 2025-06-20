using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using System.Net.Security;
using MQTTnet.Exceptions;

namespace _Scripts.Models
{
    public class CloudIoT : MonoBehaviour
    {
        #region Private Fields
        private string _endpoint;
        private string _clientId;
        private string _thingName;
        private string _caFilePath;
        private string _clientCertPath;
        private string _clientKeyPath;
        private string _pfxFilePath;
        private string _modeConnection;
        private string _port;
        
        private MqttFactory _factory;
        private IMqttClient _client;
        
        private const int DEFAULT_PORT = 8883;
        private const int CONNECTION_TIMEOUT = 45000;
        private const int DISCONNECT_TIMEOUT = 5000;
        private const int KEEP_ALIVE_SECONDS = 60;
        private const int CLIENT_TIMEOUT_SECONDS = 30;
        private const int RECONNECT_DELAY = 5000;
        private const string DEFAULT_PFX_FILENAME = "aws-iot.pfx";
        private const string TEST_TOPIC = "test/topic";
        #endregion

        #region Public Properties
        public string endpoint
        {
            get => _endpoint;
            set => _endpoint = value;
        }

        public string clientId
        {
            get => _clientId;
            set => _clientId = value;
        }

        public string thingName
        {
            get => _thingName;
            set => _thingName = value;
        }

        public string port
        {
            get => _port;
            set => _port = value;
        }

        public string caFilePath
        {
            get => _caFilePath;
            set => _caFilePath = value;
        }

        public string clientCertPath
        {
            get => _clientCertPath;
            set => _clientCertPath = value;
        }

        public string clientKeyPath
        {
            get => _clientKeyPath;
            set => _clientKeyPath = value;
        }
        
        public string pfxFilePath
        {
            get => _pfxFilePath;
            set => _pfxFilePath = value;
        }

        public string modeConnection
        {
            get => _modeConnection;
            set => _modeConnection = value;
        }
        #endregion

        #region Public Methods - API Principal
        /// <summary>
        /// Conecta a AWS IoT Core usando certificados
        /// </summary>
        /// <returns>True si la conexión fue exitosa</returns>
        public async Task<bool> ConnectToAwsIoT()
        {
            try
            {
                if (!await InitializeClientAsync())
                    return false;

                if (!await ValidateCertificatesAsync())
                    return false;

                var tlsParams = await CreateTlsParametersAsync();
                if (tlsParams == null)
                    return false;

                var options = BuildConnectionOptions(tlsParams);
                ConfigureEventHandlers();

                bool connected = await AttemptConnectionAsync(options);
                
                if (connected)
                {
                    await SubscribeToDefaultTopics();
                }

                return connected;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error connecting to AWS IoT: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Desconecta de AWS IoT Core
        /// </summary>
        /// <returns>True si la desconexión fue exitosa</returns>
        public async Task<bool> DisconnectFromAwsIoT()
        {
            if (_client == null || !_client.IsConnected)
                return false;
            
            try
            {
                await _client.DisconnectAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error disconnecting from AWS IoT: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Publica un mensaje en un topic específico
        /// </summary>
        /// <param name="topic">Topic de destino</param>
        /// <param name="payload">Contenido del mensaje</param>
        /// <param name="qos">Quality of Service (0, 1, 2)</param>
        /// <returns>True si el mensaje fue enviado exitosamente</returns>
        public async Task<bool> PublishMessage(string topic, string payload, int qos = 1)
        {
            if (!IsConnected())
            {
                Debug.LogWarning("Cannot publish message: Client not connected");
                return false;
            }
            
            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .WithRetainFlag(false)
                    .Build();
                
                await _client.PublishAsync(message);
                Debug.Log($"Message published to topic: {topic}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error publishing message: {ex.Message}");
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
                
                Debug.Log($"Subscribed to topic: {topic}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error subscribing to topic: {ex.Message}");
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
                thingName = _thingName
            };
    
            var json = JsonUtility.ToJson(testPayload);
            return await PublishMessage(TEST_TOPIC, json);
        }

        /// <summary>
        /// Verifica si el cliente está conectado
        /// </summary>
        /// <returns>True si está conectado</returns>
        public bool IsConnected()
        {
            return _client != null && _client.IsConnected;
        }
        #endregion

        #region Private Helper Methods
        private async Task<bool> InitializeClientAsync()
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

        private async Task<bool> ValidateCertificatesAsync()
        {
            try
            {
                string pfxPath = string.IsNullOrEmpty(_pfxFilePath) ? _clientCertPath : _pfxFilePath;

#if UNITY_ANDROID && !UNITY_EDITOR
                if (string.IsNullOrEmpty(pfxPath))
                    pfxPath = DEFAULT_PFX_FILENAME;
                
                pfxPath = Path.GetFileName(pfxPath);
                
                var testBytes = await LoadCertificateBytesAsync(pfxPath);
                if (testBytes == null)
                {
                    Debug.LogError($"Certificate file not found in StreamingAssets: {pfxPath}");
                    return false;
                }
#else
                if (string.IsNullOrEmpty(pfxPath) || !File.Exists(pfxPath))
                {
                    Debug.LogError("Certificate file not found or path is empty");
                    return false;
                }

                if (!pfxPath.EndsWith(".pfx"))
                {
                    Debug.LogError("Certificate file must be a .pfx file");
                    return false;
                }
#endif
                
                _pfxFilePath = pfxPath;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Certificate validation failed: {ex.Message}");
                return false;
            }
        }

        private async Task<MqttClientOptionsBuilderTlsParameters> CreateTlsParametersAsync()
        {
            try
            {
                string pfxPath = !string.IsNullOrEmpty(_pfxFilePath) ? _pfxFilePath : _clientCertPath;
                
                if (string.IsNullOrEmpty(pfxPath))
                    return null;
                
                byte[] certBytes = await LoadCertificateBytesAsync(pfxPath);
                if (certBytes == null)
                    return null;
                
                var clientCert = new X509Certificate2(certBytes, "", X509KeyStorageFlags.Exportable);
                
                return new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    Certificates = new[] { clientCert },
                    SslProtocol = System.Security.Authentication.SslProtocols.Tls12,
                    CertificateValidationHandler = ValidateServerCertificate,
                    AllowUntrustedCertificates = false,
                    IgnoreCertificateChainErrors = false,
                    IgnoreCertificateRevocationErrors = true
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create TLS parameters: {ex.Message}");
                return null;
            }
        }

        private MqttClientOptions BuildConnectionOptions(MqttClientOptionsBuilderTlsParameters tlsParams)
        {
            int portNumber = int.TryParse(_port, out int p) ? p : DEFAULT_PORT;
            
            return new MqttClientOptionsBuilder()
                .WithClientId(_thingName)
                .WithTcpServer(_endpoint, portNumber)
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .WithTls(tlsParams)
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(KEEP_ALIVE_SECONDS))
                .WithTimeout(TimeSpan.FromSeconds(CLIENT_TIMEOUT_SECONDS))
                .Build();
        }

        private async Task<bool> AttemptConnectionAsync(MqttClientOptions options)
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
            
            if (_client.IsConnected)
            {
                Debug.Log("Successfully connected to AWS IoT Core");
                return true;
            }
            
            Debug.LogError("Failed to connect to AWS IoT Core");
            return false;
        }

        private async Task<byte[]> LoadCertificateBytesAsync(string pfxPath)
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                string streamingPath = Path.Combine(Application.streamingAssetsPath, Path.GetFileName(pfxPath));
                
                using (var request = UnityEngine.Networking.UnityWebRequest.Get(streamingPath))
                {
                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Delay(50);
                    }
                    
                    if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"Certificate loaded successfully. Size: {request.downloadHandler.data.Length} bytes");
                        return request.downloadHandler.data;
                    }
                    else
                    {
                        Debug.LogError($"Failed to load certificate: {request.error}");
                        return null;
                    }
                }
#else
                if (File.Exists(pfxPath))
                {
                    return await File.ReadAllBytesAsync(pfxPath);
                }
                else
                {
                    Debug.LogError($"Certificate file not found: {pfxPath}");
                    return null;
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading certificate: {ex.Message}");
                return null;
            }
        }

        private bool ValidateServerCertificate(MqttClientCertificateValidationEventArgs args)
        {
            // Permitir certificados válidos y aquellos con errores de cadena solamente
            return args.SslPolicyErrors == SslPolicyErrors.None || 
                   args.SslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors;
        }

        private void ConfigureEventHandlers()
        {
            _client.ConnectedAsync += async e =>
            {
                Debug.Log("Connected to AWS IoT Core");
            };
            
            _client.DisconnectedAsync += async e =>
            {
                Debug.Log($"Disconnected from AWS IoT Core. Reason: {e.Reason}");
                
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
                    await ConnectToAwsIoT();
                }
            };
            
            _client.ApplicationMessageReceivedAsync += HandleReceivedMessage;
        }

        private async Task SubscribeToDefaultTopics()
        {
            try
            {
                if (!string.IsNullOrEmpty(_thingName))
                {
                    await SubscribeToTopic(TEST_TOPIC);
                    Debug.Log("Subscribed to default topics");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error subscribing to default topics: {ex.Message}");
            }
        }

        private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                
                Debug.Log($"Message received on topic '{topic}': {payload}");
                
                if (topic.Contains("/shadow/"))
                {
                    Debug.Log("Shadow message received");
                    // Aquí puedes agregar lógica específica para mensajes de shadow
                }
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
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error during cleanup: {ex.Message}");
                }
            }
        }
        #endregion

        #region Data Classes
        [Serializable]
        public class TestPayload
        {
            public string message;
            public string timestamp;
            public string source;
            public string thingName;
        }
        #endregion
    }
}