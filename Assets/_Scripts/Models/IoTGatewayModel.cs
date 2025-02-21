using System;
using System.Text;
using M2Mqtt;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace _Scripts.Models
{
    public sealed class IoTGatewayModel
    {
        private static IoTGatewayModel _instance = null;
        private readonly static object Padlock = new object();

        private MqttClient client;
        private string brokerAddress;
        private int brokerPort;
        private string clientId;
        private string username;
        private string password;

        public event Action<string, string> OnMessageReceived;
        public event Action OnConnected;
        public event Action OnDisconnected;

        
        private IoTGatewayModel(string brokerAddress, int brokerPort, string clientId, string username, string password)
        {
            this.brokerAddress = brokerAddress;
            this.brokerPort = brokerPort;
            this.clientId = clientId;
            this.username = username;
            this.password = password;
        }
        
        public static void Initialize(string brokerAddress, int brokerPort, string clientId, string username, string password)
        {
            lock (Padlock)
            {
                if (_instance == null)
                {
                    _instance = new IoTGatewayModel(brokerAddress, brokerPort, clientId, username, password);
                }
                else
                {
                    _instance.brokerAddress = brokerAddress;
                    _instance.brokerPort = brokerPort;
                    _instance.clientId = clientId;
                    _instance.username = username;
                    _instance.password = password;
                }
            }
        }
        
        public static IoTGatewayModel Instance
        {
            get
            {
                lock (Padlock)
                {
                    if (_instance == null)
                    {
                        throw new InvalidOperationException("IoTGatewayModel no ha sido inicializado. Llama a Initialize() primero.");
                    }
                    return _instance;
                }
            }
        }
        internal void Connect()
        {
            if (client != null && client.IsConnected)
            {
                Debug.Log("Ya está conectado a MQTT.");
                return;
            }

            try
            {
                client = new MqttClient(brokerAddress, brokerPort, false, null, null, MqttSslProtocols.None);
                client.MqttMsgPublishReceived += OnMessageReceivedHandler;
                client.ConnectionClosed += OnConnectionLost;

                client.Connect(clientId, username, password);
                Debug.Log("✅ Conectado a MQTT correctamente.");

                // 🔔 Lanza el evento de conexión exitosa
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError("❌ Error al conectar con el broker MQTT: " + ex.Message);
            }
        }


        internal void Disconnect()
        {
            if (client != null && client.IsConnected)
            {
                client.Disconnect();
                Debug.Log("🔌 Desconectado de MQTT.");

                // 🔔 Lanza el evento de desconexión
                OnDisconnected?.Invoke();
            }
        }


        internal void PublishMessage(string topic, string message)
        {
            if (client != null && client.IsConnected)
            {
                client.Publish(topic, Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                Debug.Log($"📤 Mensaje publicado en {topic}: {message}");
            }
            else
            {
                Debug.LogWarning("⚠️ No se pudo publicar el mensaje. Cliente MQTT no conectado.");
            }
        }

        internal void SubscribeToTopic(string topic)
        {
            if (client != null && client.IsConnected)
            {
                client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                Debug.Log($"📩 Suscrito al tema: {topic}");
            }
            else
            {
                Debug.LogWarning("⚠️ No se pudo suscribir. Cliente MQTT no conectado.");
            }
        }

        private void OnMessageReceivedHandler(object sender, MqttMsgPublishEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Message);
            string topic = e.Topic;
            Debug.Log($"📥 Mensaje recibido en {topic}: {message}");

            OnMessageReceived?.Invoke(topic, message);
        }

        private void OnConnectionLost(object sender, EventArgs e)
        {
            Debug.LogWarning("⚠️ Conexión perdida con MQTT. Intentando reconectar...");
            Reconnect();
        }

        private void Reconnect()
        {
            Disconnect();
            Connect();
        }
    }
}
