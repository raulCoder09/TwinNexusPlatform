using System;
using System.Threading.Tasks;
using _Scripts.Models.Mqtt;
using UnityEngine;
using UnityEngine.InputSystem;
using _Scripts.Models.MQTTManagement;

namespace _Scripts.Testing
{
    public class TNPSGA52 : MonoBehaviour
    {
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private string awsBrokerAddress = "a1b2c3d4.iot.us-east-1.amazonaws.com"; // Cambia al endpoint real
        [SerializeField] private int awsBrokerPort = 8883;
        [SerializeField] private string testTopic = "TNPSGA52/test";
        [SerializeField] private string testMessage = "Hello from TNPSGA52!";

        private InputAction connectAction;
        private InputAction disconnectAction;
        private InputAction subscribeAction;
        private InputAction unsubscribeAction;
        private InputAction publishAction;
        private InputAction statusAction;

        private void Awake()
        {
            MqttManager.Instance.Initialize();
            if (!MqttManager.Instance.IsConnected())
            {
                LogDebug("MqttManager initialized for TNPSGA52");
            }
        }

        private void OnEnable()
        {
            connectAction = new InputAction("Connect", InputActionType.Button, "<Keyboard>/c");
            disconnectAction = new InputAction("Disconnect", InputActionType.Button, "<Keyboard>/d");
            subscribeAction = new InputAction("Subscribe", InputActionType.Button, "<Keyboard>/s");
            unsubscribeAction = new InputAction("Unsubscribe", InputActionType.Button, "<Keyboard>/u");
            publishAction = new InputAction("Publish", InputActionType.Button, "<Keyboard>/p");
            statusAction = new InputAction("Status", InputActionType.Button, "<Keyboard>/t");

            connectAction.performed += _ => TestConnect();
            disconnectAction.performed += _ => TestDisconnect();
            subscribeAction.performed += _ => TestSubscribe();
            unsubscribeAction.performed += _ => TestUnsubscribe();
            publishAction.performed += _ => TestPublish();
            statusAction.performed += _ => TestStatus();

            connectAction.Enable();
            disconnectAction.Enable();
            subscribeAction.Enable();
            unsubscribeAction.Enable();
            publishAction.Enable();
            statusAction.Enable();

            MqttManager.Instance.SubscribeToConnected(OnConnected);
            MqttManager.Instance.SubscribeToDisconnected(OnDisconnected);
            MqttManager.Instance.SubscribeToMessageReceived(OnMessageReceived);
            MqttManager.Instance.SubscribeToSubscribed(OnSubscribed);
            MqttManager.Instance.SubscribeToUnsubscribed(OnUnsubscribed);
            MqttManager.Instance.SubscribeToConnectionFailed(OnConnectionFailed);
        }

        private void OnDisable()
        {
            MqttManager.Instance.UnsubscribeFromConnected(OnConnected);
            MqttManager.Instance.UnsubscribeFromDisconnected(OnDisconnected);
            MqttManager.Instance.UnsubscribeFromMessageReceived(OnMessageReceived);
            MqttManager.Instance.UnsubscribeFromSubscribed(OnSubscribed);
            MqttManager.Instance.UnsubscribeFromUnsubscribed(OnUnsubscribed);
            MqttManager.Instance.UnsubscribeFromConnectionFailed(OnConnectionFailed);

            connectAction.Disable();
            disconnectAction.Disable();
            subscribeAction.Disable();
            unsubscribeAction.Disable();
            publishAction.Disable();
            statusAction.Disable();
        }

        private async void TestConnect()
        {
            MqttManager.Instance.BrokerAddress = awsBrokerAddress;
            MqttManager.Instance.BrokerPort = awsBrokerPort;
            MqttManager.Instance.UseSSL = true;

            bool success = await MqttManager.Instance.ConnectAsync();
            LogDebug($"TestConnect (AWS IoT Core): {(success ? "Success" : "Failed")}");
        }

        private async void TestDisconnect()
        {
            bool success = await MqttManager.Instance.DisconnectAsync();
            LogDebug($"TestDisconnect: {(success ? "Success" : "Failed")}");
        }

        private async void TestSubscribe()
        {
            bool success = await MqttManager.Instance.SubscribeToTopicAsync(testTopic);
            LogDebug($"TestSubscribe to {testTopic}: {(success ? "Success" : "Failed")}");
        }

        private async void TestUnsubscribe()
        {
            bool success = await MqttManager.Instance.UnsubscribeFromTopicAsync(testTopic);
            LogDebug($"TestUnsubscribe from {testTopic}: {(success ? "Success" : "Failed")}");
        }

        private async void TestPublish()
        {
            bool success = await MqttManager.Instance.SendTestMessageAsync(testMessage);
            LogDebug($"TestPublish to {testTopic}: {(success ? "Success" : "Failed")}");
        }

        private void TestStatus()
        {
            var status = MqttManager.Instance.GetConnectionStatus();
            LogDebug($"TestStatus:\n{MqttInfo.FormatConnectionStatus(status)}");
        }

        private void OnConnected()
        {
            LogDebug("Event: Connected to AWS IoT Core");
        }

        private void OnDisconnected(string reason)
        {
            LogDebug($"Event: Disconnected from AWS IoT Core: {reason}");
        }

        private void OnMessageReceived(string topic, string message)
        {
            LogDebug($"Event: Message received on {topic}: {message}");
        }

        private void OnSubscribed(string topic)
        {
            LogDebug($"Event: Subscribed to {topic}");
        }

        private void OnUnsubscribed(string topic)
        {
            LogDebug($"Event: Unsubscribed from {topic}");
        }

        private void OnConnectionFailed(string error)
        {
            LogDebug($"Event: Connection failed: {error}");
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[TNPSGA52] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[TNPSGA52] {message}");
        }
    }
}