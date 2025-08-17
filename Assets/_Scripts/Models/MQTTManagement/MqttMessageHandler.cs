namespace _Scripts.Models.MQTTManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MQTTnet;
    using MQTTnet.Client;
    using UnityEngine;

    public class MqttMessageHandler : IMqttOperations
    {
        private readonly MqttConnectionHandler _connectionHandler;
        private readonly MqttEventManager _eventManager;
        private readonly bool _enableDebugLogs = true;
        private readonly Dictionary<string, Action<string, string>> _topicHandlers = new Dictionary<string, Action<string, string>>();

        public MqttMessageHandler(MqttConnectionHandler connectionHandler, MqttEventManager eventManager)
        {
            _connectionHandler = connectionHandler ?? throw new ArgumentNullException(nameof(connectionHandler));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));

            // CORREGIDO: Suscribirse al evento de conexión para configurar el callback de mensajes
            _eventManager.SubscribeToConnected(() => {
                ConfigureMessageHandler();
            });
        }
        
        /// <summary>
        /// Configura el manejador de mensajes después de la conexión
        /// </summary>
        private void ConfigureMessageHandler()
        {
            try
            {
                IMqttClient client = _connectionHandler.GetClient();
                if (client != null)
                {
                    LogDebug("Configuring message handler for connected client");
                    client.ApplicationMessageReceivedAsync += e =>
                    {
                        ProcessMqttMessage(e);
                        return Task.CompletedTask;
                    };
                    LogDebug("Message handler configured successfully");
                }
                else
                {
                    LogError("Cannot configure message handler - client is null");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error configuring message handler: {ex.Message}");
            }
        }

        public async Task<bool> SubscribeAsync(string topic, MqttInfo.QoSLevel qos, Action<string, string> messageHandler)
        {
            if (string.IsNullOrEmpty(topic))
            {
                LogError("Topic cannot be null or empty");
                return false;
            }

            if (!_connectionHandler.IsConnected())
            {
                LogWarning("Cannot subscribe: Client not connected");
                return false;
            }

            try
            {
                IMqttClient client = _connectionHandler.GetClient();
                await client.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .Build());

                if (messageHandler != null)
                {
                    _topicHandlers[topic] = messageHandler;
                }

                LogDebug($"Successfully subscribed to topic: {topic}");
                _eventManager.TriggerSubscribed(topic);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error subscribing to topic '{topic}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UnsubscribeAsync(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                LogError("Topic cannot be null or empty");
                return false;
            }

            if (!_connectionHandler.IsConnected())
            {
                LogWarning("Cannot unsubscribe: Client not connected");
                return false;
            }

            try
            {
                IMqttClient client = _connectionHandler.GetClient();
                await client.UnsubscribeAsync(topic);

                if (_topicHandlers.ContainsKey(topic))
                {
                    _topicHandlers.Remove(topic);
                }

                LogDebug($"Successfully unsubscribed from topic: {topic}");
                _eventManager.TriggerUnsubscribed(topic);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error unsubscribing from topic '{topic}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PublishAsync(string topic, string message, MqttInfo.QoSLevel qos, bool retain)
        {
            if (string.IsNullOrEmpty(topic))
            {
                LogError("Topic cannot be null or empty");
                return false;
            }

            if (string.IsNullOrEmpty(message))
            {
                LogError("Message cannot be null or empty");
                return false;
            }

            if (!_connectionHandler.IsConnected())
            {
                LogWarning("Cannot publish message: Client not connected");
                return false;
            }

            try
            {
                IMqttClient client = _connectionHandler.GetClient();
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(message)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .WithRetainFlag(retain)
                    .Build();

                await client.PublishAsync(mqttMessage);
                LogDebug($"Message published to topic: {topic}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error publishing message to topic '{topic}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PublishObjectAsync<T>(string topic, T obj, MqttInfo.QoSLevel qos, bool retain)
        {
            try
            {
                string json = JsonUtility.ToJson(obj);
                return await PublishAsync(topic, json, qos, retain);
            }
            catch (Exception ex)
            {
                LogError($"Error serializing object for topic '{topic}': {ex.Message}");
                return false;
            }
        }

        public Task<bool> ConnectAsync(string brokerAddress, int brokerPort, string clientId, string username, string password, bool useSSL, string certificatePath, string certificateFileName, string certificatePassword)
        {
            throw new NotImplementedException("Connection handled by MqttConnectionHandler");
        }

        public Task<bool> DisconnectAsync()
        {
            throw new NotImplementedException("Disconnection handled by MqttConnectionHandler");
        }

        public Task<bool> ReconnectAsync()
        {
            throw new NotImplementedException("Reconnection handled by MqttConnectionHandler");
        }

        public bool IsConnected()
        {
            return _connectionHandler.IsConnected();
        }

        private void ProcessMqttMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                string topic = e.ApplicationMessage.Topic;
                string message = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                if (_topicHandlers.ContainsKey(topic))
                {
                    _topicHandlers[topic]?.Invoke(topic, message);
                }
                _eventManager.TriggerMessageReceived(topic, message);
                LogDebug($"Message received on topic '{topic}': {message}");
            }
            catch (Exception ex)
            {
                LogError($"Error processing MQTT message: {ex.Message}");
            }
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[MqttMessageHandler] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[MqttMessageHandler] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[MqttMessageHandler] {message}");
        }
    }
}