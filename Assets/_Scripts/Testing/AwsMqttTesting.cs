namespace _Scripts.Models.MQTTManagement
{
    using System.IO;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using _Scripts.Models.FileManagement;
    using _Scripts.Models.MQTTManagement;

    public class AwsMqttTesting : MqttManager
    {
        [Header("Input Configuration")]
        [SerializeField] private string testKeyConnect = "c"; // Tecla para conectar a AWS IoT Core
        [SerializeField] private string testKeyPublish = "p"; // Tecla para publicar un mensaje
        [SerializeField] private string testKeySubscribe = "s"; // Tecla para suscribirse al topic
        [SerializeField] private string testKeyDisconnect = "d"; // Tecla para desconectar

        [Header("Test Configuration")]
        [SerializeField] private string awsBrokerAddress = "aqloxhiemdroo-ats.iot.us-east-1.amazonaws.com"; // Endpoint de AWS IoT Core
        [SerializeField] private int awsBrokerPort = 8883; // Puerto SSL
        [SerializeField] private string awsClientId = "TNPSGA52"; // Thing name
        [SerializeField] private string certificatePath = "certificates"; // Subcarpeta en persistentDataPath o StreamingAssets
        [SerializeField] private string certificateFileName = "aws-iot-core.pfx"; // Nombre del archivo de certificado
        [SerializeField] private string certificatePassword = "5859"; // Contraseña del certificado
        [SerializeField] private string topic = "tnp/TNPSGA52/test"; // Topic para publicar y suscribirse
        [SerializeField] private string message = "{\"message\": \"Hello from Unity MQTT!\", \"timestamp\": \"2025-06-29T11:12:00Z\"}"; // Mensaje en formato JSON

        private InputAction actionConnect;
        private InputAction actionPublish;
        private InputAction actionSubscribe;
        private InputAction actionDisconnect;

        private void OnEnable()
        {
            // Configurar acciones de entrada
            actionConnect = new InputAction("connect", InputActionType.Button, $"<Keyboard>/{testKeyConnect}");
            actionConnect.performed += OnConnectPerformed;
            actionConnect.Enable();

            actionPublish = new InputAction("publish", InputActionType.Button, $"<Keyboard>/{testKeyPublish}");
            actionPublish.performed += OnPublishPerformed;
            actionPublish.Enable();

            actionSubscribe = new InputAction("subscribe", InputActionType.Button, $"<Keyboard>/{testKeySubscribe}");
            actionSubscribe.performed += OnSubscribePerformed;
            actionSubscribe.Enable();

            actionDisconnect = new InputAction("disconnect", InputActionType.Button, $"<Keyboard>/{testKeyDisconnect}");
            actionDisconnect.performed += OnDisconnectPerformed;
            actionDisconnect.Enable();
        }

        private void OnDisable()
        {
            // Desactivar acciones - verificar que no sean null antes de desuscribirse
            if (actionConnect != null)
            {
                actionConnect.performed -= OnConnectPerformed;
                actionConnect.Disable();
                actionConnect.Dispose(); // Liberar recursos
            }

            if (actionPublish != null)
            {
                actionPublish.performed -= OnPublishPerformed;
                actionPublish.Disable();
                actionPublish.Dispose(); // Liberar recursos
            }

            if (actionSubscribe != null)
            {
                actionSubscribe.performed -= OnSubscribePerformed;
                actionSubscribe.Disable();
                actionSubscribe.Dispose(); // Liberar recursos
            }

            if (actionDisconnect != null)
            {
                actionDisconnect.performed -= OnDisconnectPerformed;
                actionDisconnect.Disable();
                actionDisconnect.Dispose(); // Liberar recursos
            }
        }

        private async void OnConnectPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[AwsMqttTesting] Testing connect to AWS IoT Core...");
            BrokerAddress = awsBrokerAddress;
            BrokerPort = awsBrokerPort;
            UseSSL = true;
            var connected = await ConnectAsync(awsBrokerAddress, awsBrokerPort, awsClientId, "", "", true, certificatePath, certificateFileName, certificatePassword);
            Debug.Log($"[AwsMqttTesting] Connect to AWS IoT Core: {(connected ? "Success" : "Failed")}");
        }

        private async void OnPublishPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[AwsMqttTesting] Testing publish message...");
            if (IsConnected())
            {
                var jsonMessage = $"{{\"message\": \"{message}\", \"timestamp\": \"{System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}\"}}";
                var published = await PublishAsync(topic, jsonMessage, MqttInfo.QoSLevel.AtLeastOnce);
                Debug.Log($"[AwsMqttTesting] Publish to {topic}: {(published ? "Success" : "Failed")}");
                Debug.Log($"[AwsMqttTesting] Message sent: {jsonMessage}");
            }
            else
            {
                Debug.Log("[AwsMqttTesting] Cannot publish: Not connected to AWS IoT Core");
            }
        }

        private async void OnSubscribePerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[AwsMqttTesting] Testing subscribe to topic...");
            if (IsConnected())
            {
                var subscribed = await SubscribeAsync(topic, MqttInfo.QoSLevel.AtLeastOnce, (receivedTopic, receivedMessage) =>
                {
                    Debug.Log($"[AwsMqttTesting] Message received on {receivedTopic}: {receivedMessage}");
                });
                Debug.Log($"[AwsMqttTesting] Subscribe to {topic}: {(subscribed ? "Success" : "Failed")}");
            }
            else
            {
                Debug.Log("[AwsMqttTesting] Cannot subscribe: Not connected to AWS IoT Core");
            }
        }

        private async void OnDisconnectPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[AwsMqttTesting] Testing disconnect from AWS IoT Core...");
            if (IsConnected())
            {
                var disconnected = await DisconnectAsync();
                Debug.Log($"[AwsMqttTesting] Disconnect from AWS IoT Core: {(disconnected ? "Success" : "Failed")}");
            }
            else
            {
                Debug.Log("[AwsMqttTesting] Cannot disconnect: Not connected to AWS IoT Core");
            }
        }
    }
}