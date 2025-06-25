using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts.Models;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.Testing
{
    public class IoTCoreTesting : MonoBehaviour
    {
        private InputAction connectAction;
        private InputAction disconnectAction;
        private InputAction testConnectivityAction;
        private InputAction publishTelemetryAction;
        private InputAction sendHeartbeatAction;
        private InputAction sendStatusAction;
        private InputAction updateShadowAction;
        private InputAction getShadowAction;
        private InputAction listTopicsAction;
        private InputAction simulateMessageAction;

        void Start()
        {
            // Initialize input actions
            connectAction = new InputAction("Connect", InputActionType.Button, "<Keyboard>/c");
            disconnectAction = new InputAction("Disconnect", InputActionType.Button, "<Keyboard>/q");
            testConnectivityAction = new InputAction("TestConnectivity", InputActionType.Button, "<Keyboard>/t");
            publishTelemetryAction = new InputAction("PublishTelemetry", InputActionType.Button, "<Keyboard>/p");
            sendHeartbeatAction = new InputAction("SendHeartbeat", InputActionType.Button, "<Keyboard>/h");
            sendStatusAction = new InputAction("SendStatus", InputActionType.Button, "<Keyboard>/s");
            updateShadowAction = new InputAction("UpdateShadow", InputActionType.Button, "<Keyboard>/u");
            getShadowAction = new InputAction("GetShadow", InputActionType.Button, "<Keyboard>/g");
            listTopicsAction = new InputAction("ListTopics", InputActionType.Button, "<Keyboard>/l");
            simulateMessageAction = new InputAction("SimulateMessage", InputActionType.Button, "<Keyboard>/m");
            
            // Bind events
            connectAction.performed += OnConnectPressed;
            disconnectAction.performed += OnDisconnectPressed;
            testConnectivityAction.performed += OnTestConnectivityPressed;
            publishTelemetryAction.performed += OnPublishTelemetryPressed;
            sendHeartbeatAction.performed += OnSendHeartbeatPressed;
            sendStatusAction.performed += OnSendStatusPressed;
            updateShadowAction.performed += OnUpdateShadowPressed;
            getShadowAction.performed += OnGetShadowPressed;
            listTopicsAction.performed += OnListTopicsPressed;
            simulateMessageAction.performed += OnSimulateMessagePressed;
            
            // Enable actions
            connectAction.Enable();
            disconnectAction.Enable();
            testConnectivityAction.Enable();
            publishTelemetryAction.Enable();
            sendHeartbeatAction.Enable();
            sendStatusAction.Enable();
            updateShadowAction.Enable();
            getShadowAction.Enable();
            listTopicsAction.Enable();
            simulateMessageAction.Enable();
            
            // Subscribe to IoT events
            if (IoTCoreManager.Instance != null)
            {
                IoTCoreManager.Instance.OnIoTConnectionComplete += OnConnectionResult;
                IoTCoreManager.Instance.OnMessagePublished += OnMessagePublishedResult;
                IoTCoreManager.Instance.OnMessageReceived += OnMessageReceivedResult;
                IoTCoreManager.Instance.OnSubscriptionComplete += OnSubscriptionResult;
                IoTCoreManager.Instance.OnDeviceShadowUpdated += OnDeviceShadowResult;
                IoTCoreManager.Instance.OnTelemetryPublished += OnTelemetryResult;
            }
            
            // Show instructions
            Debug.Log("🚀 IoT Core Testing iniciado:");
            Debug.Log("🔌 Presiona 'C' para conectar a IoT Core");
            Debug.Log("❌ Presiona 'Q' para desconectar de IoT Core");
            Debug.Log("🔍 Presiona 'T' para probar conectividad");
            Debug.Log("📊 Presiona 'P' para publicar telemetría de prueba");
            Debug.Log("💓 Presiona 'H' para enviar heartbeat manual");
            Debug.Log("📱 Presiona 'S' para enviar estado del dispositivo");
            Debug.Log("🌟 Presiona 'U' para actualizar Device Shadow");
            Debug.Log("🔍 Presiona 'G' para obtener Device Shadow");
            Debug.Log("📋 Presiona 'L' para listar topics disponibles");
            Debug.Log("💬 Presiona 'M' para simular mensaje recibido");
            Debug.Log("─────────────────────────────────────────");
        }

        void OnDestroy()
        {
            // Cleanup input actions
            if (connectAction != null)
            {
                connectAction.performed -= OnConnectPressed;
                connectAction.Disable();
                connectAction.Dispose();
            }
            
            if (disconnectAction != null)
            {
                disconnectAction.performed -= OnDisconnectPressed;
                disconnectAction.Disable();
                disconnectAction.Dispose();
            }
            
            if (testConnectivityAction != null)
            {
                testConnectivityAction.performed -= OnTestConnectivityPressed;
                testConnectivityAction.Disable();
                testConnectivityAction.Dispose();
            }
            
            if (publishTelemetryAction != null)
            {
                publishTelemetryAction.performed -= OnPublishTelemetryPressed;
                publishTelemetryAction.Disable();
                publishTelemetryAction.Dispose();
            }
            
            if (sendHeartbeatAction != null)
            {
                sendHeartbeatAction.performed -= OnSendHeartbeatPressed;
                sendHeartbeatAction.Disable();
                sendHeartbeatAction.Dispose();
            }
            
            if (sendStatusAction != null)
            {
                sendStatusAction.performed -= OnSendStatusPressed;
                sendStatusAction.Disable();
                sendStatusAction.Dispose();
            }
            
            if (updateShadowAction != null)
            {
                updateShadowAction.performed -= OnUpdateShadowPressed;
                updateShadowAction.Disable();
                updateShadowAction.Dispose();
            }
            
            if (getShadowAction != null)
            {
                getShadowAction.performed -= OnGetShadowPressed;
                getShadowAction.Disable();
                getShadowAction.Dispose();
            }
            
            if (listTopicsAction != null)
            {
                listTopicsAction.performed -= OnListTopicsPressed;
                listTopicsAction.Disable();
                listTopicsAction.Dispose();
            }
            
            if (simulateMessageAction != null)
            {
                simulateMessageAction.performed -= OnSimulateMessagePressed;
                simulateMessageAction.Disable();
                simulateMessageAction.Dispose();
            }
        }

        #region Input Action Handlers

        private void OnConnectPressed(InputAction.CallbackContext context)
        {
            ExecuteConnectTest();
        }

        private void OnDisconnectPressed(InputAction.CallbackContext context)
        {
            ExecuteDisconnectTest();
        }

        private void OnTestConnectivityPressed(InputAction.CallbackContext context)
        {
            ExecuteConnectivityTest();
        }

        private void OnPublishTelemetryPressed(InputAction.CallbackContext context)
        {
            ExecutePublishTelemetryTest();
        }

        private void OnSendHeartbeatPressed(InputAction.CallbackContext context)
        {
            ExecuteSendHeartbeatTest();
        }

        private void OnSendStatusPressed(InputAction.CallbackContext context)
        {
            ExecuteSendStatusTest();
        }

        private void OnUpdateShadowPressed(InputAction.CallbackContext context)
        {
            ExecuteUpdateShadowTest();
        }

        private void OnGetShadowPressed(InputAction.CallbackContext context)
        {
            ExecuteGetShadowTest();
        }

        private void OnListTopicsPressed(InputAction.CallbackContext context)
        {
            ExecuteListTopicsTest();
        }

        private void OnSimulateMessagePressed(InputAction.CallbackContext context)
        {
            ExecuteSimulateMessageTest();
        }

        #endregion

        #region Test Execution Methods

        async void ExecuteConnectTest()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }

            if (IoTCoreManager.Instance.IsConnectedToIoTCore)
            {
                Debug.LogWarning("⚠️ Ya está conectado a IoT Core");
                return;
            }
            
            Debug.Log("🔌 Iniciando conexión a IoT Core...");
            bool success = await IoTCoreManager.Instance.ConnectToIoTCoreAsync();
            Debug.Log(success ? "✅ Conexión completada" : "❌ Conexión falló");
        }

        async void ExecuteDisconnectTest()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }

            if (!IoTCoreManager.Instance.IsConnectedToIoTCore)
            {
                Debug.LogWarning("⚠️ No está conectado a IoT Core");
                return;
            }
            
            Debug.Log("❌ Iniciando desconexión de IoT Core...");
            bool success = await IoTCoreManager.Instance.DisconnectFromIoTCoreAsync();
            Debug.Log(success ? "✅ Desconexión completada" : "❌ Desconexión falló");
        }

        async void ExecuteConnectivityTest()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }
            
            Debug.Log("🔍 Iniciando test de conectividad IoT Core...");
            bool success = await IoTCoreManager.Instance.TestIoTConnectivityAsync();
            Debug.Log(success ? "✅ Test de conectividad exitoso" : "❌ Test de conectividad falló");
        }

        async void ExecutePublishTelemetryTest()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }
            
            Debug.Log("📊 Publicando telemetría de prueba...");
            bool success = await IoTCoreManager.Instance.PublishSampleTelemetryAsync();
            Debug.Log(success ? "✅ Telemetría publicada exitosamente" : "❌ Falló la publicación de telemetría");
        }

        async void ExecuteSendHeartbeatTest()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }
            
            Debug.Log("💓 Enviando heartbeat manual...");
            bool success = await IoTCoreManager.Instance.SendHeartbeatAsync();
            Debug.Log(success ? "✅ Heartbeat enviado exitosamente" : "❌ Falló el envío de heartbeat");
        }

        async void ExecuteSendStatusTest()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }
            
            Debug.Log("📱 Enviando estado del dispositivo...");
            bool success = await IoTCoreManager.Instance.SendDeviceStatusAsync();
            Debug.Log(success ? "✅ Estado enviado exitosamente" : "❌ Falló el envío de estado");
        }

        async void ExecuteUpdateShadowTest()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }
            
            Debug.Log("🌟 Actualizando Device Shadow...");
            
            var testShadowState = new
            {
                state = new
                {
                    desired = new
                    {
                        temperature_threshold = UnityEngine.Random.Range(20f, 30f),
                        humidity_threshold = UnityEngine.Random.Range(40f, 70f),
                        alert_enabled = UnityEngine.Random.value > 0.5f,
                        last_test_update = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        test_mode = true
                    }
                }
            };
            
            bool success = await IoTCoreManager.Instance.UpdateDeviceShadowAsync(testShadowState);
            Debug.Log(success ? "✅ Device Shadow actualizado exitosamente" : "❌ Falló la actualización del Device Shadow");
        }

        async void ExecuteGetShadowTest()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }
            
            Debug.Log("🔍 Obteniendo Device Shadow...");
            bool success = await IoTCoreManager.Instance.GetDeviceShadowAsync();
            Debug.Log(success ? "✅ Solicitud de Device Shadow enviada" : "❌ Falló la solicitud de Device Shadow");
        }

        void ExecuteListTopicsTest()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }
            
            Debug.Log("📋 Listando topics disponibles...");
            
            var availableTopics = IoTCoreManager.Instance.GetAvailableTopics();
            
            Debug.Log($"📊 Total de topics disponibles: {availableTopics.Count}");
            
            if (availableTopics.Count > 0)
            {
                Debug.Log("📁 Topics disponibles para tu rol:");
                for (int i = 0; i < availableTopics.Count; i++)
                {
                    Debug.Log($"   {i + 1}. {availableTopics[i]}");
                }
            }
            else
            {
                Debug.Log("📭 No hay topics disponibles para tu rol actual");
            }
            
            // Show connection status
            Debug.Log($"🔌 Estado de conexión: {(IoTCoreManager.Instance.IsConnectedToIoTCore ? "Conectado" : "Desconectado")}");
            
            // Show device info
            if (IoTCoreManager.Instance.CurrentDevice != null)
            {
                Debug.Log($"📱 Dispositivo actual: {IoTCoreManager.Instance.CurrentDevice.thingName}");
                Debug.Log($"📊 Estado: {IoTCoreManager.Instance.CurrentDevice.status}");
                Debug.Log($"🕐 Última vez visto: {IoTCoreManager.Instance.CurrentDevice.lastSeen:HH:mm:ss}");
            }
        }

        void ExecuteSimulateMessageTest()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }
            
            Debug.Log("💬 Simulando mensaje recibido...");
            
            var simulatedPayload = new
            {
                messageType = "test_command",
                command = "simulate_sensor_reading",
                parameters = new
                {
                    sensor_id = "temp_001",
                    value = UnityEngine.Random.Range(18f, 35f),
                    unit = "celsius"
                },
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                source = "IoT_Testing_Simulation"
            };
            
            string topic = $"twin-nexus/simulation/commands";
            string payload = Newtonsoft.Json.JsonConvert.SerializeObject(simulatedPayload, Newtonsoft.Json.Formatting.None);
            
            IoTCoreManager.Instance.SimulateMessageReceived(topic, payload);
            Debug.Log("✅ Mensaje simulado generado exitosamente");
        }

        #endregion

        #region Event Handlers

        void OnConnectionResult(bool success, string message)
        {
            if (success)
            {
                Debug.Log($"🎉 ¡Conexión a IoT Core exitosa!");
                Debug.Log($"💬 Mensaje: {message}");
                Debug.Log($"📱 Dispositivo: {IoTCoreManager.Instance.CurrentDevice?.thingName}");
            }
            else
            {
                Debug.LogError($"❌ Error en conexión IoT: {message}");
            }
        }

        void OnMessagePublishedResult(bool success, string topic, string messageId)
        {
            if (success)
            {
                Debug.Log($"📤 ¡Mensaje publicado exitosamente!");
                Debug.Log($"📍 Topic: {topic}");
                Debug.Log($"🆔 Message ID: {messageId}");
            }
            else
            {
                Debug.LogError($"❌ Error publicando mensaje en topic: {topic}");
            }
        }

        void OnMessageReceivedResult(string topic, string payload, DateTime timestamp)
        {
            Debug.Log($"📨 ¡Mensaje recibido!");
            Debug.Log($"📍 Topic: {topic}");
            Debug.Log($"🕐 Timestamp: {timestamp:HH:mm:ss}");
            Debug.Log($"📄 Payload: {payload}");
            
            // Try to parse and show structured info if it's JSON
            try
            {
                var jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(payload);
                if (jsonObject.ContainsKey("messageType"))
                {
                    Debug.Log($"📋 Tipo de mensaje: {jsonObject["messageType"]}");
                }
                if (jsonObject.ContainsKey("command"))
                {
                    Debug.Log($"⚡ Comando: {jsonObject["command"]}");
                }
            }
            catch (Exception)
            {
                // If it's not JSON, just show the raw payload (already logged above)
            }
        }

        void OnSubscriptionResult(bool success, string topic)
        {
            if (success)
            {
                Debug.Log($"📧 ¡Suscripción exitosa!");
                Debug.Log($"📍 Topic: {topic}");
            }
            else
            {
                Debug.LogError($"❌ Error en suscripción al topic: {topic}");
            }
        }

        void OnDeviceShadowResult(bool success, string topic, object shadowData)
        {
            if (success)
            {
                Debug.Log($"🌟 ¡Device Shadow actualizado exitosamente!");
                Debug.Log($"📍 Topic: {topic}");
                
                if (shadowData != null)
                {
                    try
                    {
                        string shadowJson = Newtonsoft.Json.JsonConvert.SerializeObject(shadowData, Newtonsoft.Json.Formatting.Indented);
                        Debug.Log($"📄 Shadow Data:\n{shadowJson}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"No se pudo serializar shadow data: {ex.Message}");
                    }
                }
            }
            else
            {
                Debug.LogError($"❌ Error en Device Shadow para topic: {topic}");
            }
        }

        void OnTelemetryResult(bool success, string message)
        {
            if (success)
            {
                Debug.Log($"📊 ¡Telemetría publicada exitosamente!");
                Debug.Log($"💬 Mensaje: {message}");
            }
            else
            {
                Debug.LogError($"❌ Error en telemetría: {message}");
            }
        }

        #endregion

        #region Helper Methods

        void Update()
        {
            // Opcional: Mostrar información de estado en tiempo real cada 10 segundos
            if (Time.time % 10f < 0.1f && IoTCoreManager.Instance != null)
            {
                ShowPeriodicStatus();
            }
        }

        private void ShowPeriodicStatus()
        {
            // Solo mostrar cada 10 segundos para no spamear la consola
            if (IoTCoreManager.Instance.IsConnectedToIoTCore)
            {
                var history = IoTCoreManager.Instance.GetMessageHistory();
                if (history.Count > 0)
                {
                    var lastMessage = history.Last();
                    Debug.Log($"🔄 Status: Conectado | Último mensaje: {lastMessage.timestamp:HH:mm:ss} | Topic: {lastMessage.topic}");
                }
            }
        }

        #endregion
    }
}