using System;
using UnityEngine;

namespace _Scripts.Models.MQTTManagement
{
    public class MqttEventManager
    {
        private readonly bool enableDebugLogs = true;

        // Events
        public event Action OnConnected;
        public event Action<string> OnDisconnected; // reason
        public event Action<string, string> OnMessageReceived; // topic, message
        public event Action<string> OnSubscribed; // topic
        public event Action<string> OnUnsubscribed; // topic
        public event Action<string> OnConnectionFailed; // errorMessage

        public void SubscribeToConnected(Action callback)
        {
            OnConnected += callback;
            LogDebug("Subscribed to OnConnected");
        }

        public void UnsubscribeFromConnected(Action callback)
        {
            OnConnected -= callback;
            LogDebug("Unsubscribed from OnConnected");
        }

        public void SubscribeToDisconnected(Action<string> callback)
        {
            OnDisconnected += callback;
            LogDebug("Subscribed to OnDisconnected");
        }

        public void UnsubscribeFromDisconnected(Action<string> callback)
        {
            OnDisconnected -= callback;
            LogDebug("Unsubscribed from OnDisconnected");
        }

        public void SubscribeToMessageReceived(Action<string, string> callback)
        {
            OnMessageReceived += callback;
            LogDebug("Subscribed to OnMessageReceived");
        }

        public void UnsubscribeFromMessageReceived(Action<string, string> callback)
        {
            OnMessageReceived -= callback;
            LogDebug("Unsubscribed from OnMessageReceived");
        }

        public void SubscribeToSubscribed(Action<string> callback)
        {
            OnSubscribed += callback;
            LogDebug("Subscribed to OnSubscribed");
        }

        public void UnsubscribeFromSubscribed(Action<string> callback)
        {
            OnSubscribed -= callback;
            LogDebug("Unsubscribed from OnSubscribed");
        }

        public void SubscribeToUnsubscribed(Action<string> callback)
        {
            OnUnsubscribed += callback;
            LogDebug("Subscribed to OnUnsubscribed");
        }

        public void UnsubscribeFromUnsubscribed(Action<string> callback)
        {
            OnUnsubscribed -= callback;
            LogDebug("Unsubscribed from OnUnsubscribed");
        }

        public void SubscribeToConnectionFailed(Action<string> callback)
        {
            OnConnectionFailed += callback;
            LogDebug("Subscribed to OnConnectionFailed");
        }

        public void UnsubscribeFromConnectionFailed(Action<string> callback)
        {
            OnConnectionFailed -= callback;
            LogDebug("Unsubscribed from OnConnectionFailed");
        }

        public void TriggerConnected()
        {
            try
            {
                OnConnected?.Invoke();
                LogDebug("Triggered OnConnected");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnConnected: {ex.Message}");
            }
        }

        public void TriggerDisconnected(string reason)
        {
            try
            {
                OnDisconnected?.Invoke(reason);
                LogDebug($"Triggered OnDisconnected: {reason}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnDisconnected: {ex.Message}");
            }
        }

        public void TriggerMessageReceived(string topic, string message)
        {
            try
            {
                OnMessageReceived?.Invoke(topic, message);
                LogDebug($"Triggered OnMessageReceived: Topic: {topic}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnMessageReceived: {ex.Message}");
            }
        }

        public void TriggerSubscribed(string topic)
        {
            try
            {
                OnSubscribed?.Invoke(topic);
                LogDebug($"Triggered OnSubscribed: {topic}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnSubscribed: {ex.Message}");
            }
        }

        public void TriggerUnsubscribed(string topic)
        {
            try
            {
                OnUnsubscribed?.Invoke(topic);
                LogDebug($"Triggered OnUnsubscribed: {topic}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnUnsubscribed: {ex.Message}");
            }
        }

        public void TriggerConnectionFailed(string errorMessage)
        {
            try
            {
                OnConnectionFailed?.Invoke(errorMessage);
                LogDebug($"Triggered OnConnectionFailed: {errorMessage}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnConnectionFailed: {ex.Message}");
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[MqttEventManager] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[MqttEventManager] {message}");
        }
    }
}