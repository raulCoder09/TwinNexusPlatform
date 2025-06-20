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
        
        // Properties
        internal string endpoint
        {
            get => _endpoint;
            set => _endpoint = value;
        }

        internal string clientId
        {
            get => _clientId;
            set => _clientId = value;
        }

        internal string thingName
        {
            get => _thingName;
            set => _thingName = value;
        }

        public string port
        {
            get => _port;
            set => _port = value;
        }

        internal string caFilePath
        {
            get => _caFilePath;
            set => _caFilePath = value;
        }

        internal string clientCertPath
        {
            get => _clientCertPath;
            set => _clientCertPath = value;
        }

        internal string clientKeyPath
        {
            get => _clientKeyPath;
            set => _clientKeyPath = value;
        }
        
        public string pfxFilePath
        {
            get => _pfxFilePath;
            set => _pfxFilePath = value;
        }

        internal string modeConnection
        {
            get => _modeConnection;
            set => _modeConnection = value;
        }
        
        // Métodos de conexión
        internal async Task<bool> ConnectToAwsIoT()
        {
            try
            {
                _factory = new MqttFactory();
                _client = _factory.CreateMqttClient();
                
                if (!await LoadCertificatesAsync())
                {
                    return false;
                }
                
                // Determinar qué archivo PFX usar
                string pfxPath = !string.IsNullOrEmpty(_pfxFilePath) ? _pfxFilePath : _clientCertPath;
                
                if (string.IsNullOrEmpty(pfxPath))
                {
                    return false;
                }
                
                MqttClientOptionsBuilderTlsParameters tlsParams;
                
                try
                {
                    byte[] certBytes = await LoadCertificateBytesAsync(pfxPath);
                    if (certBytes == null)
                    {
                        return false;
                    }
                    
                    var clientCert = new X509Certificate2(certBytes, "", X509KeyStorageFlags.Exportable);
                    
                    tlsParams = new MqttClientOptionsBuilderTlsParameters
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
                    if (ex.Message.Contains("password"))
                    {
                    }
                    return false;
                }
                
                var options = new MqttClientOptionsBuilder()
                    .WithClientId(_thingName)
                    .WithTcpServer(_endpoint, int.Parse(_port ?? "8883"))
                    .WithProtocolVersion(MqttProtocolVersion.V311)
                    .WithTls(tlsParams)
                    .WithCleanSession()
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                    .WithTimeout(TimeSpan.FromSeconds(30)) // Timeout más largo para Android
                    .Build();
                
                ConfigureEventHandlers();
                
                var connectTask = _client.ConnectAsync(options);
                var timeoutTask = Task.Delay(45000);
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    return false;
                }
                
                await connectTask; 
                
                if (_client.IsConnected)
                {
                    await SubscribeToDefaultTopics();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (MqttCommunicationException mqttEx)
            {
                return false;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                }
                
                return false;
            }
        }
        
        private async Task<byte[]> LoadCertificateBytesAsync(string pfxPath)
        {
            try
            {
               #if UNITY_ANDROID && !UNITY_EDITOR
                string streamingPath = Path.Combine(Application.streamingAssetsPath, Path.GetFileName(pfxPath));
                Debug.Log($"Cargando certificado desde StreamingAssets: {streamingPath}");
                
                using (var request = UnityEngine.Networking.UnityWebRequest.Get(streamingPath))
                {
                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Delay(50);
                    }
                    
                    if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"Certificado cargado exitosamente. Tamaño: {request.downloadHandler.data.Length} bytes");
                        return request.downloadHandler.data;
                    }
                    else
                    {
                        Debug.LogError($"Error al cargar certificado: {request.error}");
                        return null;
                    }
                }
                #else
                if (File.Exists(pfxPath))
                {
                    var bytes = await File.ReadAllBytesAsync(pfxPath);
                    return bytes;
                }
                else
                {
                    return null;
                }
#endif
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        
        internal async Task<bool> DisconnectFromAwsIoT()
        {
            if (_client == null || !_client.IsConnected)
            {
                return false;
            }
            
            try
            {
                await _client.DisconnectAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        private bool ValidateServerCertificate(MqttClientCertificateValidationEventArgs args)
        {
            if (args.SslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }
            
            
            if (args.SslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                return true;
            }
            
            return args.SslPolicyErrors == SslPolicyErrors.None;
        }
        
        private void ConfigureEventHandlers()
        {
            _client.ConnectedAsync += async e =>
            {
            };
            
            _client.DisconnectedAsync += async e =>
            {
                
                if (e.Exception != null)
                {
                }
                
                if (e.Reason != MqttClientDisconnectReason.NormalDisconnection && 
                    _modeConnection == "Automatic connection")
                {
                    await Task.Delay(5000);
                    await ConnectToAwsIoT();
                }
            };
            
            _client.ApplicationMessageReceivedAsync += HandleReceivedMessage;
        }
        
        private async Task<bool> LoadCertificatesAsync()
        {
            try
            {
                string pfxPath = string.IsNullOrEmpty(_pfxFilePath) ? _clientCertPath : _pfxFilePath;

                #if UNITY_ANDROID && !UNITY_EDITOR
                if (string.IsNullOrEmpty(pfxPath))
                {
                    pfxPath = "aws-iot.pfx"; 
                }
                
                pfxPath = Path.GetFileName(pfxPath);
                
                string streamingPath = Path.Combine(Application.streamingAssetsPath, pfxPath);
                Debug.Log($"Verificando certificado en StreamingAssets: {streamingPath}");
                
                var testBytes = await LoadCertificateBytesAsync(pfxPath);
                if (testBytes == null)
                {
                    Debug.LogError($"No se encuentra el archivo PFX en StreamingAssets: {pfxPath}");
                    Debug.LogError("Asegúrate de que el archivo .pfx esté en la carpeta StreamingAssets");
                    return false;
                }
                #else
                if (!string.IsNullOrEmpty(pfxPath))
                {
                    if (!File.Exists(pfxPath))
                    {
                        return false;
                    }

                    if (!pfxPath.EndsWith(".pfx"))
                    {

                        return false;
                    }
                    
                }
                else
                {
                    return false;
                }
            #endif
                
                _pfxFilePath = pfxPath;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        internal async Task<bool> PublishMessage(string topic, string payload, int qos = 1)
        {
            if (_client == null || !_client.IsConnected)
            {
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
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        internal async Task<bool> SubscribeToTopic(string topic, int qos = 1)
        {
            if (_client == null || !_client.IsConnected)
            {
                return false;
            }
            
            try
            {
                await _client.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .Build());
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        private async Task SubscribeToDefaultTopics()
        {
            try
            {
                if (!string.IsNullOrEmpty(_thingName))
                {

                    // Topic de prueba
                    await SubscribeToTopic("test/topic");
                    
                }
            }
            catch (Exception ex)
            {
            }
        }
        
        private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                
                
                if (topic.Contains("/shadow/"))
                {

                }
            }
            catch (Exception ex)
            {
            }
            
            return Task.CompletedTask;
        }
        
        
        
        [Serializable]
        public class TestPayload
        {
            public string message;
            public string timestamp;
            public string source;
            public string thingName;
        }

        internal async Task<bool> SendTestMessage(string message = "Hello from twin nexus platform!")
        {
            var testPayload = new TestPayload
            {
                message = message,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                source = "Unity Android",
                thingName = _thingName
            };
    
            var json = JsonUtility.ToJson(testPayload);
            return await PublishMessage("test/topic", json);
        }
        
        internal bool IsConnected()
        {
            return _client != null && _client.IsConnected;
        }
        
        private void OnDestroy()
        {
            if (_client != null && _client.IsConnected)
            {
                try
                {
                    _client.DisconnectAsync().Wait(5000); // Timeout de 5 segundos
                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}