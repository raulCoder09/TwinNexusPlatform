using System;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using System.Threading.Tasks;

namespace _Scripts.Models
{
    public class VMIoT : MonoBehaviour
    {
        private MqttFactory _factory;
        private IMqttClient _client;
        
        // Configuración específica para EC2
        private string _ipOrHostname;
        private int _port;
        private string _clientId;
        private string _username;
        private string _password;
        private string _modeConnection;

        internal string ipOrHostname
        {
            get => _ipOrHostname;
            set => _ipOrHostname = value;
        }

        internal int port
        {
            get => _port;
            set => _port = value;
        }

        internal string clientId
        {
            get => _clientId;
            set => _clientId = value;
        }

        internal string username
        {
            get => _username;
            set => _username = value;
        }

        internal string password
        {
            get => _password;
            set => _password = value;
        }

        internal string modeConnection
        {
            get => _modeConnection;
            set => _modeConnection = value;
        }

        // Clase para el payload específico de EC2
        [System.Serializable]
        public class EC2Payload
        {
            internal string Message;
            internal string Timestamp;
            internal string Source;
            internal string DeviceType;
            internal string Location;
        }

        // Properties públicas para monitoreo
        internal bool isConnected => _client?.IsConnected ?? false;

        
        internal async Task<bool> ConnectToEC2Broker()
        {
            try
            {
                _factory = new MqttFactory();
                _client = _factory.CreateMqttClient();
                
                var options = new MqttClientOptionsBuilder()
                    .WithClientId(_clientId)
                    .WithTcpServer(_ipOrHostname, _port)
                    .WithCredentials(_username, _password) // Autenticación habilitada
                    .WithCleanSession(true)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                    .Build();
                
                // Eventos de conexión
                _client.ConnectedAsync += OnConnectedAsync;
                _client.DisconnectedAsync += OnDisconnectedAsync;
                _client.ApplicationMessageReceivedAsync += HandleReceivedMessage;
                
                await _client.ConnectAsync(options);
                Debug.Log($"✅ Conectado exitosamente al broker EC2: {_ipOrHostname}:{_port}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error al conectar al broker EC2: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Desconecta del broker EC2
        /// </summary>
        public async Task<bool> DisconnectFromEC2Broker()
        {
            if (_client == null || !_client.IsConnected)
            {
                Debug.LogWarning("⚠️ No se puede desconectar: El cliente no está conectado.");
                return false;
            }

            try
            {
                await _client.DisconnectAsync();
                Debug.Log("✅ Desconectado del broker EC2 exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error al desconectar del broker EC2: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Suscribirse a un topic específico
        /// </summary>
        public async Task<bool> SubscribeToTopic(string topic)
        {
            if (_client == null || !_client.IsConnected)
            {
                Debug.LogWarning("⚠️ No se puede suscribir: El cliente no está conectado.");
                return false;
            }

            try
            {
                await _client.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .Build());
                Debug.Log($"✅ Suscrito al topic: {topic}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error al suscribirse al topic {topic}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Publicar mensaje en un topic
        /// </summary>
        public async Task<bool> PublishMessage(string topic, string message)
        {
            if (_client == null || !_client.IsConnected)
            {
                Debug.LogWarning("⚠️ No se puede publicar: El cliente no está conectado.");
                return false;
            }

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

            try
            {
                await _client.PublishAsync(mqttMessage);
                Debug.Log($"✅ Mensaje publicado en {topic}: {message}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error al publicar en {topic}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Envía un mensaje de prueba estructurado al EC2
        /// </summary>
        ///
        [Serializable]
        public class TestPayload
        {
            public string message;
            public string timestamp;
            public string source;
            public string thingName;
        }
        public async Task<bool> SendTestMessageToEC2(string message = "Hello from Unity to EC2!")
        {
            var testPayload = new TestPayload
            {
                message = message,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                source = "Unity Android"
            };
    
            var json = JsonUtility.ToJson(testPayload, true);
            Debug.Log($"📤 Enviando mensaje de prueba a EC2: {json}");
            
            return await PublishMessage("test/topic", json);
        }
        
        /// <summary>
        /// Envía datos de telemetría simulados
        /// </summary>
        public async Task<bool> SendTelemetryData(float temperature, float humidity, string sensorId)
        {
            var telemetryData = new
            {
                sensorId = sensorId,
                temperature = temperature,
                humidity = humidity,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                source = "Unity Sensor Simulation"
            };
            
            var json = JsonUtility.ToJson(telemetryData);
            return await PublishMessage($"unity/sensors/{sensorId}", json);
        }
        
        /// <summary>
        /// Suscribirse a topics comunes para tu aplicación
        /// </summary>
        public async Task SubscribeToCommonTopics()
        {
            await SubscribeToTopic("ec2/commands/#");     // Comandos desde EC2
            await SubscribeToTopic("matlab/data/#");      // Datos de MATLAB
            await SubscribeToTopic("unity/broadcast");    // Broadcast entre clientes Unity
            await SubscribeToTopic("system/status");      // Estado del sistema
        }
        
        // Eventos privados
        private Task OnConnectedAsync(MqttClientConnectedEventArgs e)
        {
            Debug.Log($"🔗 Cliente MQTT conectado al EC2. Resultado: {e.ConnectResult.ResultCode}");
            
            // Auto-suscribirse a topics comunes al conectar
            _ = Task.Run(async () => await SubscribeToCommonTopics());
            
            return Task.CompletedTask;
        }
        
        private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            Debug.LogWarning($"🔌 Cliente MQTT desconectado del EC2. Razón: {e.Reason}");
            
            // Aquí podrías implementar reconexión automática si es necesario
            if (e.Reason != MqttClientDisconnectReason.NormalDisconnection)
            {
                Debug.Log("🔄 Desconexión inesperada, podrías implementar reconexión automática aquí");
            }
            
            return Task.CompletedTask;
        }
        
        private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;
            string message = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            
            Debug.Log($"📥 Mensaje recibido de EC2 en [{topic}]: {message}");
            
            // Aquí puedes añadir lógica específica según el topic
            switch (topic)
            {
                case var t when t.StartsWith("ec2/commands/"):
                    HandleEC2Command(message);
                    break;
                case var t when t.StartsWith("matlab/data/"):
                    HandleMatlabData(message);
                    break;
                case "system/status":
                    HandleSystemStatus(message);
                    break;
                default:
                    Debug.Log($"📋 Mensaje genérico recibido: {message}");
                    break;
            }
            
            return Task.CompletedTask;
        }
        
        // Manejadores específicos para diferentes tipos de mensajes
        private void HandleEC2Command(string command)
        {
            Debug.Log($"🎯 Comando desde EC2: {command}");
            // Implementar lógica de comandos
        }
        
        private void HandleMatlabData(string data)
        {
            Debug.Log($"📊 Datos desde MATLAB: {data}");
            // Implementar procesamiento de datos de MATLAB
        }
        
        private void HandleSystemStatus(string status)
        {
            Debug.Log($"⚡ Estado del sistema: {status}");
            // Implementar lógica de estado del sistema
        }
        
        // Cleanup automático al destruir el objeto
        private async void OnDestroy()
        {
            if (_client != null && _client.IsConnected)
            {
                try
                {
                    await _client.DisconnectAsync();
                    Debug.Log("🧹 Cliente MQTT desconectado automáticamente en OnDestroy");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Error al desconectar en OnDestroy: {ex.Message}");
                }
            }
        }
        
        // Método para testing en el Inspector
        [ContextMenu("Test Connection to EC2")]
        private async void TestConnection()
        {
            if (await ConnectToEC2Broker())
            {
                await SendTestMessageToEC2("Test desde Inspector!");
            }
        }
        
        [ContextMenu("Send Test Telemetry")]
        private async void TestTelemetry()
        {
            if (isConnected)
            {
                await SendTelemetryData(UnityEngine.Random.Range(20f, 30f), 
                                      UnityEngine.Random.Range(40f, 60f), 
                                      "unity_sensor_01");
            }
        }
    }
}