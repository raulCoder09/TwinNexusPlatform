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
                Debug.Log("Iniciando conexión a AWS IoT Core...");
                Debug.Log($"Endpoint: {_endpoint}");
                Debug.Log($"Thing Name: {_thingName}");
                Debug.Log($"Puerto: {_port}");
                
                _factory = new MqttFactory();
                _client = _factory.CreateMqttClient();
                
                // Cargar certificados
                if (!LoadCertificates())
                {
                    Debug.LogError("Error al cargar los certificados");
                    return false;
                }
                
                // Determinar qué archivo PFX usar
                string pfxPath = !string.IsNullOrEmpty(_pfxFilePath) ? _pfxFilePath : _clientCertPath;
                
                if (string.IsNullOrEmpty(pfxPath) || !pfxPath.EndsWith(".pfx"))
                {
                    Debug.LogError("Se requiere un archivo PFX para conectar con AWS IoT Core");
                    return false;
                }
                
                Debug.Log($"Usando certificado PFX: {pfxPath}");
                
                MqttClientOptionsBuilderTlsParameters tlsParams;
                
                try
                {
                    var clientCert = new X509Certificate2(pfxPath, "");
                    Debug.Log($"Certificado cargado: {clientCert.Subject}");
                    
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
                    Debug.LogError($"Error al cargar certificado PFX: {ex.Message}");
                    if (ex.Message.Contains("password"))
                    {
                        Debug.LogError("💡 Tip: Si tu PFX tiene contraseña, cámbiala en el código o crea uno sin contraseña");
                    }
                    throw;
                }
                
                // Configurar opciones de conexión
                var options = new MqttClientOptionsBuilder()
                    .WithClientId(_thingName)
                    .WithTcpServer(_endpoint, int.Parse(_port ?? "8883"))
                    .WithProtocolVersion(MqttProtocolVersion.V311)
                    .WithTls(tlsParams)
                    .WithCleanSession()
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                    .Build();
                
                // Configurar eventos antes de conectar
                ConfigureEventHandlers();
                
                Debug.Log("Conectando al broker...");
                
                // Conectar
                await _client.ConnectAsync(options);
                
                if (_client.IsConnected)
                {
                    Debug.Log($"Conectado exitosamente a AWS IoT Core: {_endpoint}");
                    await SubscribeToDefaultTopics();
                    return true;
                }
                else
                {
                    Debug.LogError("No se pudo establecer la conexión");
                    return false;
                }
            }
            catch (MqttCommunicationException mqttEx)
            {
                Debug.LogError($"Error de comunicación MQTT: {mqttEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error al conectar con AWS IoT: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Debug.LogError($"Error interno: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }
        
        internal async Task<bool> DisconnectFromAwsIoT()
        {
            if (_client == null || !_client.IsConnected)
            {
                Debug.LogWarning("Cliente no conectado a AWS IoT");
                return false;
            }
            
            try
            {
                await _client.DisconnectAsync();
                Debug.Log("Desconectado de AWS IoT Core");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error al desconectar de AWS IoT: {ex.Message}");
                return false;
            }
        }
        
        private bool ValidateServerCertificate(MqttClientCertificateValidationEventArgs args)
        {
            if (args.SslPolicyErrors == SslPolicyErrors.None)
            {
                Debug.Log("Certificado del servidor validado correctamente");
                return true;
            }
            
            Debug.LogWarning($"Errores de certificado SSL: {args.SslPolicyErrors}");
            
            return true;
        }
        
        private void ConfigureEventHandlers()
        {
            _client.ConnectedAsync += async e =>
            {
                Debug.Log("¡Conexión establecida con AWS IoT Core!");
                Debug.Log($"Cliente conectado como: {_thingName}");
            };
            
            _client.DisconnectedAsync += async e =>
            {
                Debug.Log($"Desconectado de AWS IoT Core. Razón: {e.Reason}");
                
                if (e.Exception != null)
                {
                    Debug.LogError($"Excepción: {e.Exception.Message}");
                }
                
                if (e.Reason != MqttClientDisconnectReason.NormalDisconnection && 
                    _modeConnection == "Automatic connection")
                {
                    Debug.Log("Esperando 5 segundos antes de reconectar...");
                    await Task.Delay(5000);
                    Debug.Log("Intentando reconexión automática...");
                    await ConnectToAwsIoT();
                }
            };
            
            _client.ApplicationMessageReceivedAsync += HandleReceivedMessage;
        }
        
        private bool LoadCertificates()
        {
            try
            {
                // Primero verificar si hay un archivo PFX en pfxFilePath
                if (!string.IsNullOrEmpty(_pfxFilePath))
                {
                    if (!File.Exists(_pfxFilePath))
                    {
                        Debug.LogError($"No se encuentra el archivo PFX: {_pfxFilePath}");
                        Debug.Log($"Ruta completa buscada: {Path.GetFullPath(_pfxFilePath)}");
                        return false;
                    }
                    
                    Debug.Log($"Archivo PFX encontrado: {_pfxFilePath}");
                    return true;
                }
                // Si no hay PFX en pfxFilePath, verificar el clientCertPath
                else if (!string.IsNullOrEmpty(_clientCertPath))
                {
                    if (!File.Exists(_clientCertPath))
                    {
                        Debug.LogError($"No se encuentra el archivo: {_clientCertPath}");
                        Debug.Log($"Ruta completa buscada: {Path.GetFullPath(_clientCertPath)}");
                        return false;
                    }
                    
                    if (_clientCertPath.EndsWith(".pfx"))
                    {
                        Debug.Log($"Archivo PFX encontrado en clientCertPath: {_clientCertPath}");
                        return true;
                    }
                    else
                    {
                        Debug.LogError("El archivo debe ser .pfx para AWS IoT Core");
                        Debug.Log("Convierte tus certificados PEM a PFX con:");
                        Debug.Log("openssl pkcs12 -export -out aws-iot.pfx -inkey private.pem.key -in certificate.pem.crt -passout pass:");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError("No se ha especificado ningún archivo de certificado");
                    Debug.Log("Configura el campo 'Cloud Pfx File Path' o 'Client Certificate File'");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error al verificar certificados: {ex.Message}");
                return false;
            }
        }
        
        
        
        
        
        // Publicación y suscripción
        internal async Task<bool> PublishMessage(string topic, string payload, int qos = 1)
        {
            if (_client == null || !_client.IsConnected)
            {
                Debug.LogWarning("No conectado a AWS IoT - No se puede publicar");
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
                Debug.Log($"Mensaje publicado en {topic}: {payload}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error al publicar mensaje: {ex.Message}");
                return false;
            }
        }
        
        internal async Task<bool> SubscribeToTopic(string topic, int qos = 1)
        {
            if (_client == null || !_client.IsConnected)
            {
                Debug.LogWarning("No conectado a AWS IoT - No se puede suscribir");
                return false;
            }
            
            try
            {
                await _client.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .Build());
                
                Debug.Log($"📥 Suscrito al topic: {topic}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error al suscribirse: {ex.Message}");
                return false;
            }
        }
        
        private async Task SubscribeToDefaultTopics()
        {
            try
            {
                // Suscribirse a topics predeterminados de AWS IoT Shadow
                if (!string.IsNullOrEmpty(_thingName))
                {
                    await SubscribeToTopic($"$aws/things/{_thingName}/shadow/update/accepted");
                    await SubscribeToTopic($"$aws/things/{_thingName}/shadow/update/rejected");
                    await SubscribeToTopic($"$aws/things/{_thingName}/shadow/update/delta");
                    await SubscribeToTopic($"$aws/things/{_thingName}/shadow/get/accepted");
                    await SubscribeToTopic($"$aws/things/{_thingName}/shadow/get/rejected");
                    
                    // Topic de prueba
                    await SubscribeToTopic("test/topic");
                    
                    Debug.Log("Suscrito a todos los topics predeterminados");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error al suscribirse a topics predeterminados: {ex.Message}");
            }
        }
        
        private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                
                Debug.Log($"Mensaje recibido en {topic}: {payload}");
                
                if (topic.Contains("/shadow/"))
                {
                    HandleShadowMessage(topic, payload);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error al procesar mensaje recibido: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }
        
        private void HandleShadowMessage(string topic, string payload)
        {
            Debug.Log($"🔄 Shadow update en {topic}");
            
            if (topic.Contains("/accepted"))
            {
                Debug.Log("Operación de Shadow aceptada");
            }
            else if (topic.Contains("/rejected"))
            {
                Debug.LogWarning($"Operación de Shadow rechazada: {payload}");
            }
            else if (topic.Contains("/delta"))
            {
                Debug.Log("Cambios detectados en el Shadow");
            }
        }
        
        internal async Task<bool> UpdateDeviceShadow(string state)
        {
            var shadowTopic = $"$aws/things/{_thingName}/shadow/update";
            var shadowPayload = $"{{\"state\": {{\"reported\": {state}}}}}";
            Debug.Log($"Actualizando Shadow: {shadowPayload}");
            return await PublishMessage(shadowTopic, shadowPayload);
        }
        
        internal async Task<bool> GetDeviceShadow()
        {
            var shadowTopic = $"$aws/things/{_thingName}/shadow/get";
            Debug.Log("Solicitando estado del Shadow");
            return await PublishMessage(shadowTopic, "");
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
                source = "Unity",
                thingName = _thingName
            };
    
            var json = JsonUtility.ToJson(testPayload);
            Debug.Log($"🧪 Enviando mensaje de prueba: {json}");
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
                Debug.Log("🔌 Desconectando cliente AWS IoT en OnDestroy...");
                _client.DisconnectAsync().Wait(5000); // Timeout de 5 segundos
            }
        }
    }
}