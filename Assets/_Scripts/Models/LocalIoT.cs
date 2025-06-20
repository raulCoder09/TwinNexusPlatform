using System;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using System.Threading.Tasks;

namespace _Scripts.Models
{
    public class LocalIoT : MonoBehaviour
    {
        private MqttFactory _factory;
        
        private IMqttClient _client;
        
        private string _brokerAddress;
        
        private int _brokerPort; 
        private string _clientId;
        
        private string _username;
        
        private string _password;
        private string _modeConnection;
        
        // Clase para el payload de prueba
        [System.Serializable]
        public class TestPayload
        {
            public string message;
            public string timestamp;
            public string source;
        }

        internal string brokerAddress
        {
            get => _brokerAddress;
            set => _brokerAddress = value;
        }

        internal int brokerPort
        {
            get => _brokerPort;
            set => _brokerPort = value;
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

        internal string clientId
        {
            get => _clientId;
            set => _clientId = value;
        }

        internal string modeConnection
        {
            get => _modeConnection;
            set => _modeConnection = value;
        }

        internal async Task<bool> ConnectToBroker()
        {
            _factory = new MqttFactory();
            _client = _factory.CreateMqttClient();
            var connectionSuccess = false;
            
            var options = new MqttClientOptionsBuilder()
                .WithClientId(_clientId)
                .WithTcpServer(_brokerAddress, _brokerPort)
                .WithCredentials(_username, _password)
                .Build();
            
            _client.ConnectedAsync += async e =>
            {

            };
            
            // Agregar handler para mensajes recibidos
            _client.ApplicationMessageReceivedAsync += HandleReceivedMessage;
            
            try
            {
                await _client.ConnectAsync(options);
                connectionSuccess = true;
            }
            catch (Exception ex)
            {
                connectionSuccess = false;
            }
            return connectionSuccess;
        }
        
        internal async Task<bool> DisconnectFromBroker()
        {
            var disconnectionSuccess = false;
            if (_client == null || !_client.IsConnected)
            {
                return false;
            }

            try
            {
                await _client.DisconnectAsync();
                disconnectionSuccess = true;
            }
            catch (Exception ex)
            {
                disconnectionSuccess = false;
            }
            return disconnectionSuccess;
        }
        
        private async Task ReconnectToBroker()
        {
            if (_client == null)
            {
                return;
            }

            if (_client.IsConnected)
            {
                return;
            }
            
            var options = new MqttClientOptionsBuilder()
                .WithClientId(_clientId)
                .WithTcpServer(_brokerAddress, _brokerPort)
                .WithCredentials(_username, _password)
                .Build();

            try
            {
                await _client.ConnectAsync(options);
            }
            catch (Exception ex)
            {
            }
        }
        
        internal async Task<bool> SubscribeToTopic(string subscribeTopic)
        {
            if (_client == null || !_client.IsConnected)
            {
                return false;
            }

            try
            {
                await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(subscribeTopic).Build());
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        internal async Task<bool> PublishMessage(string topic, string message)
        {
            if (_client == null || !_client.IsConnected)
            {
                return false;
            }

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
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
        
        private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            string message = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            return Task.CompletedTask;
        }

        internal async Task<bool> SendTestMessage(string message = "Hello from twin nexus platform!")
        {
            var testPayload = new TestPayload
            {
                message = message,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                source = "Unity Android"
            };
    
            var json = JsonUtility.ToJson(testPayload);
            return await PublishMessage("test/topic", json);
        }

        private void OnDestroy()
        {
            if (_client != null && _client.IsConnected)
            {
                _client.DisconnectAsync().Wait();
            }
        }
    }
}