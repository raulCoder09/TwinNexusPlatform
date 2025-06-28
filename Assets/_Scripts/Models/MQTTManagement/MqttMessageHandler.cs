using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _Scripts.Models.Mqtt;
using MQTTnet;
using MQTTnet.Client;
using UnityEngine;

namespace _Scripts.Models.MQTTManagement
{
    public class MqttMessageHandler
    {
        private readonly MqttConnectionHandler connectionHandler;
        private readonly MqttEventManager eventManager;
        private readonly bool enableDebugLogs = true;
        private readonly bool logReceivedMessages = true;
        private readonly Dictionary<string, Action<string, string>> topicHandlers = new Dictionary<string, Action<string, string>>();

        public MqttMessageHandler(MqttConnectionHandler connectionHandler, MqttEventManager eventManager)
        {
            this.connectionHandler = connectionHandler;
            this.eventManager = eventManager;
        }

        public async Task<bool> SubscribeToTopicAsync(string topic, MqttInfo.QoSLevel qos = MqttInfo.QoSLevel.AtLeastOnce, Action<string, string> messageHandler = null)
        {
            if (string.IsNullOrEmpty(topic))
            {
                LogError("Topic cannot be null or empty");
                return false;
            }

            if (!connectionHandler.IsConnected())
            {
                LogWarning("Cannot subscribe: Client not connected");
                return false;
            }

            try
            {
                IMqttClient client = connectionHandler.GetClient();
                await client.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .Build());

                if (messageHandler != null)
                {
                    topicHandlers[topic] = messageHandler;
                }

                LogDebug($"Successfully subscribed to topic: {topic}");
                eventManager.TriggerSubscribed(topic);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error subscribing to topic '{topic}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UnsubscribeFromTopicAsync(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                LogError("Topic cannot be null or empty");
                return false;
            }

            if (!connectionHandler.IsConnected())
            {
                LogWarning("Cannot unsubscribe: Client not connected");
                return false;
            }

            try
            {
                IMqttClient client = connectionHandler.GetClient();
                await client.UnsubscribeAsync(topic);

                if (topicHandlers.ContainsKey(topic))
                {
                    topicHandlers.Remove(topic);
                }

                LogDebug($"Successfully unsubscribed from topic: {topic}");
                eventManager.TriggerUnsubscribed(topic);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error unsubscribing from topic '{topic}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PublishMessageAsync(string topic, string message, MqttInfo.QoSLevel qos = MqttInfo.QoSLevel.AtLeastOnce, bool retain = false)
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

            if (!connectionHandler.IsConnected())
            {
                LogWarning("Cannot publish message: Client not connected");
                return false;
            }

            try
            {
                IMqttClient client = connectionHandler.GetClient();
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

        public async Task<bool> PublishObjectAsync<T>(string topic, T obj, MqttInfo.QoSLevel qos = MqttInfo.QoSLevel.AtLeastOnce, bool retain = false)
        {
            try
            {
                string json = JsonUtility.ToJson(obj);
                return await PublishMessageAsync(topic, json, qos, retain);
            }
            catch (Exception ex)
            {
                LogError($"Error serializing object for topic '{topic}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendTestMessageAsync(string testTopic, string customMessage = null)
        {
            var testPayload = MqttInfo.CreateTestPayload(customMessage, connectionHandler.GetClient()?.Options.ClientId);
            if (testPayload == null)
            {
                LogError("Failed to create test payload");
                return false;
            }

            return await PublishObjectAsync(testTopic, testPayload, MqttInfo.QoSLevel.AtLeastOnce);
        }

        public void HandleReceivedMessage(string topic, string message)
        {
            try
            {
                if (logReceivedMessages)
                {
                    LogDebug($"Message received on topic '{topic}': {message}");
                }

                if (topicHandlers.ContainsKey(topic))
                {
                    topicHandlers[topic]?.Invoke(topic, message);
                }

                eventManager.TriggerMessageReceived(topic, message);
            }
            catch (Exception ex)
            {
                LogError($"Error handling received message: {ex.Message}");
            }
        }

        public void ProcessMqttMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                string topic = e.ApplicationMessage.Topic;
                string message = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                HandleReceivedMessage(topic, message);
            }
            catch (Exception ex)
            {
                LogError($"Error processing MQTT message: {ex.Message}");
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[MqttMessageHandler] {message}");
        }

        private void LogWarning(string message)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[MqttMessageHandler] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[MqttMessageHandler] {message}");
        }
    }
}