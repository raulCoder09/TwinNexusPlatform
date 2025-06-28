using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Models.Mqtt
{
    [System.Serializable]
    public class MqttInfo
    {
        public enum QoSLevel
        {
            AtMostOnce = 0,
            AtLeastOnce = 1,
            ExactlyOnce = 2
        }

        public enum ConnectionMode
        {
            Manual,
            AutomaticOnStart,
            AutomaticWithReconnection
        }

        public class TestPayload
        {
            public string message;
            public string timestamp;
            public string source;
            public string clientId;
            public DeviceInfo deviceInfo;
        }

        [System.Serializable]
        public class DeviceInfo
        {
            public string model;
            public string os;
            public string processor;
            public int memory;
        }

        [System.Serializable]
        public class ConnectionStatus
        {
            public bool isConnected;
            public string brokerAddress;
            public int brokerPort;
            public string clientId;
            public string connectionMode;
            public List<string> subscribedTopics;
        }

        public static TestPayload CreateTestPayload(string customMessage, string clientId)
        {
            try
            {
                return new TestPayload
                {
                    message = customMessage ?? "Hello from Unity MQTT!",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    source = $"Unity {Application.platform}",
                    clientId = clientId,
                    deviceInfo = new DeviceInfo
                    {
                        model = SystemInfo.deviceModel,
                        os = SystemInfo.operatingSystem,
                        processor = SystemInfo.processorType,
                        memory = SystemInfo.systemMemorySize
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MqttInfo] Error creating TestPayload: {ex.Message}");
                return null;
            }
        }

        public static string FormatConnectionStatus(ConnectionStatus status)
        {
            try
            {
                if (status == null)
                    return "Connection status is null";

                return $"Connected: {status.isConnected}\n" +
                       $"Broker: {status.brokerAddress}:{status.brokerPort}\n" +
                       $"Client ID: {status.clientId}\n" +
                       $"Connection Mode: {status.connectionMode}\n" +
                       $"Subscribed Topics: {(status.subscribedTopics.Count > 0 ? string.Join(", ", status.subscribedTopics) : "None")}";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MqttInfo] Error formatting ConnectionStatus: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }
}