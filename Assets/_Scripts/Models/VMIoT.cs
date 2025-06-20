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
                
                _client.ConnectedAsync += OnConnectedAsync;
                _client.DisconnectedAsync += OnDisconnectedAsync;
                _client.ApplicationMessageReceivedAsync += HandleReceivedMessage;
                
                await _client.ConnectAsync(options);
                return true;
            }
            catch (Exception ex)
            {
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
        
        /// <summary>
        /// Suscribirse a un topic específico
        /// </summary>
        public async Task<bool> SubscribeToTopic(string topic)
        {
            if (_client == null || !_client.IsConnected)
            {
                return false;
            }

            try
            {
                await _client.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .Build());
                return true;
            }
            catch (Exception ex)
            {
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
                return true;
            }
            catch (Exception ex)
            {
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
        }
        public async Task<bool> SendTestMessageToEC2(string message = "Hello from twin nexus platform to EC2!")
        {
            var testPayload = new TestPayload
            {
                message = message,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                source = "Unity Android"
            };
    
            var json = JsonUtility.ToJson(testPayload, true);
            return await PublishMessage("test/topic", json);
        }
        
        
        private Task OnConnectedAsync(MqttClientConnectedEventArgs e)
        {
            
            return Task.CompletedTask;
        }
        
        private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            if (e.Reason != MqttClientDisconnectReason.NormalDisconnection)
            {
            }
            
            return Task.CompletedTask;
        }
        
        private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;
            string message = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            
            switch (topic)
            {
                case var t when t.StartsWith("ec2/commands/"):

                    break;
            }
            
            return Task.CompletedTask;
        }
        
        
        private async void OnDestroy()
        {
            if (_client != null && _client.IsConnected)
            {
                try
                {
                    await _client.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    
                }
            }
        }
        
        
    }
}