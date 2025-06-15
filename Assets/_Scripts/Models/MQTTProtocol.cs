using System;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using System.Threading.Tasks;

namespace _Scripts.Models
{
    public class MQTTProtocol : MonoBehaviour
    {
        private MqttFactory _factory;
        
        private IMqttClient _client;
        
        private string _brokerAddress;
        
        private int _brokerPort; 
        private string _clientId;
        
        private string _username;
        
        private string _password;
        private string _modeConnection;
        
        

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
                // print("Conectado al broker MQTT en " + _brokerAddress);
            };
            
            try
            {
                await _client.ConnectAsync(options);
                connectionSuccess = true;
            }
            catch (Exception ex)
            {
                // print("Error al conectar al broker: " + ex.Message);
                connectionSuccess = false;
            }
            return connectionSuccess;
        }
        

        internal async Task<bool> DisconnectFromBroker()
        {
            var disconnectionSuccess = false;
            if (_client == null || !_client.IsConnected)
            {
                // print("No se puede desconectar: El cliente no está conectado.");
                disconnectionSuccess = false;
            }

            try
            {
                await _client.DisconnectAsync();
                // print("Desconectado del broker MQTT manualmente");
                disconnectionSuccess = true;
            }
            catch (Exception ex)
            {
                // print("Error al desconectar del broker: " + ex.Message);
                disconnectionSuccess = false;
            }
            return disconnectionSuccess;
        }
        
        private async Task ReconnectToBroker()
        {
            if (_client == null)
            {
                print("No se puede reconectar: El cliente no está inicializado.");
                return;
            }

            if (_client.IsConnected)
            {
                print("El cliente ya está conectado, no se necesita reconexión.");
                return;
            }

            print("Intentando reconectar al broker...");
            var options = new MqttClientOptionsBuilder()
                .WithClientId(_clientId)
                .WithTcpServer(_brokerAddress, _brokerPort)
                .WithCredentials(_username, _password)
                .Build();

            try
            {
                await _client.ConnectAsync(options);
                print("Reconectado al broker MQTT en " + _brokerAddress);
            }
            catch (Exception ex)
            {
                print("Error al reconectar al broker: " + ex.Message);
            }
        }
        
        private async Task SubscribeToTopic(string subscribeTopic)
        {
            await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(subscribeTopic).Build());
            print("Suscrito al tema: " + subscribeTopic);
        }
        
        private async Task PublishMessage(string topic, string message)
        {
            if (_client == null || !_client.IsConnected)
            {
                print("No se puede publicar: El cliente no está conectado.");
                return;
            }

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            try
            {
                await _client.PublishAsync(mqttMessage);
                print("Mensaje publicado en " + topic + ": " + message);
            }
            catch (Exception ex)
            {
                print("Error al publicar el mensaje: " + ex.Message);
            }
        }
        
        private Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            string message = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            print("Mensaje recibido en " + e.ApplicationMessage.Topic + ": " + message);
            return Task.CompletedTask;
        }
        
        private void OnDestroy()
        {
            if (_client != null && _client.IsConnected)
            {
                _client.DisconnectAsync().Wait();
                print("Cliente MQTT desconectado");
            }
        }
    }
}