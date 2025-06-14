using System;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using System.Threading.Tasks;

namespace _Scripts.Models
{
    public class MQTTProtocol : MonoBehaviour
    {
        // Fábrica para crear el cliente MQTT
        private MqttFactory _factory;
        
        private IMqttClient _client;
        
        private string _brokerAddress = "192.168.8.94"; // IP del broker
        
        private int _brokerPort = 1883; // Puerto estándar de MQTT
        
        private string _username = "ruloCoder09"; // Usuario configurado en Mosquitto
        
        private string _password = "5859"; // Contraseña del usuario
        
        private string _clientId = "UnityClient"; // ID del cliente en Unity
        
        private string _subscribeTopic = "test/topic"; // Tema para suscribirse
        
        private async Task ConnectToBroker()
        {
            _factory = new MqttFactory();
            _client = _factory.CreateMqttClient();
            
            var options = new MqttClientOptionsBuilder()
                .WithClientId(_clientId)
                .WithTcpServer(_brokerAddress, _brokerPort)
                .WithCredentials(_username, _password)
                .Build();
            
            _client.ConnectedAsync += async e =>
            {
                Debug.Log("Conectado al broker MQTT en " + _brokerAddress);
                await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(_subscribeTopic).Build());
                Debug.Log("Suscrito al tema: " + _subscribeTopic);
            };
            
            _client.DisconnectedAsync += async e =>
            {
                Debug.Log("Desconectado del broker MQTT");
            };

            _client.ApplicationMessageReceivedAsync += e =>
            {
                string message = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                Debug.Log("Mensaje recibido en " + e.ApplicationMessage.Topic + ": " + message);
                return Task.CompletedTask;
            };

            try
            {
                await _client.ConnectAsync(options);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error al conectar al broker: " + ex.Message);
            }
        }
        
        async void Start()
        {
            try
            {
                await ConnectToBroker();
            }
            catch (Exception e)
            {
                throw; // TODO handle exception
            }
        }

        private void OnDestroy()
        {
            if (_client != null && _client.IsConnected)
            {
                _client.DisconnectAsync().Wait();
                Debug.Log("Cliente MQTT desconectado");
            }
        }
    }
}