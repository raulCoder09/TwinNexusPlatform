using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using UnityEngine;

namespace _Scripts.Models.CloudWatchManagement
{
    public class CloudWatchLogs
    {
        [SerializeField] private string defaultLogLevel = "INFO";
        [SerializeField] private string logStreamPrefix = "unity";

        private readonly AmazonCloudWatchLogsClient cloudWatchLogsClient;
        public string DefaultLogGroup { get; private set; }
        public string CurrentLogStreamName { get; private set; }
        private string currentSessionId;

        public event Action<bool, string, string> OnLogSent;

        public CloudWatchLogs(AmazonCloudWatchLogsClient client, string defaultLogGroup, string username)
        {
            this.cloudWatchLogsClient = client ?? throw new ArgumentNullException(nameof(client));
            this.DefaultLogGroup = defaultLogGroup ?? throw new ArgumentNullException(nameof(defaultLogGroup));
            InitializeLogStream(username);
        }

        private void InitializeLogStream(string username)
        {
            username = string.IsNullOrEmpty(username) ? "unknown" : username;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            CurrentLogStreamName = $"{logStreamPrefix}-{username}-{timestamp}";
            Debug.Log($"Log stream initialized: {CurrentLogStreamName}");
        }

        public async Task<bool> SendLogAsync(string message, string logLevel = null)
        {
            try
            {
                logLevel = logLevel ?? defaultLogLevel;
                Debug.Log($"Sending log to CloudWatch: Group: {DefaultLogGroup}, Stream: {CurrentLogStreamName}, Level: {logLevel}");

                await EnsureLogGroupAndStreamExist();

                var username = OldCognitoManager.Instance?.CurrentUsername ?? "unknown";
                var formattedMessage = $"[{logLevel}] [{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{username}] {message}";

                var logEvent = new InputLogEvent
                {
                    Message = formattedMessage,
                    Timestamp = DateTime.UtcNow
                };

                var request = new PutLogEventsRequest
                {
                    LogGroupName = DefaultLogGroup,
                    LogStreamName = CurrentLogStreamName,
                    LogEvents = new List<InputLogEvent> { logEvent }
                };

                var response = await cloudWatchLogsClient.PutLogEventsAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.Log($"Log sent successfully to {DefaultLogGroup}");
                    OnLogSent?.Invoke(true, "Log sent successfully", DefaultLogGroup);
                    return true;
                }

                Debug.LogError($"Failed to send log: {response.HttpStatusCode}");
                OnLogSent?.Invoke(false, $"Failed to send log: {response.HttpStatusCode}", DefaultLogGroup);
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"CloudWatch send log error: {ex.Message}");
                OnLogSent?.Invoke(false, ex.Message, DefaultLogGroup);
                return false;
            }
        }

        public async Task<bool> SendAppMonitoringLogAsync(string eventType, string eventData, string userId = null, string logLevel = null)
        {
            try
            {
                logLevel = logLevel ?? defaultLogLevel;
                userId = userId ?? OldCognitoManager.Instance?.CurrentUsername ?? "unknown";
                Debug.Log($"Sending app monitoring log: Event Type: {eventType}, User ID: {userId}, Level: {logLevel}");

                await EnsureLogGroupAndStreamExist();

                var timestamp = DateTime.UtcNow;
                var sessionId = GetSessionId(userId);

                var deviceInfo = new DeviceInfo
                {
                    deviceModel = SystemInfo.deviceModel,
                    deviceName = SystemInfo.deviceName,
                    operatingSystem = SystemInfo.operatingSystem,
                    processorType = SystemInfo.processorType,
                    processorCount = SystemInfo.processorCount,
                    graphicsDeviceName = SystemInfo.graphicsDeviceName,
                    screenResolution = $"{Screen.width}x{Screen.height}",
                    screenDPI = Screen.dpi
                };

                var logEntry = new MonitoringLogEntry
                {
                    timestamp = timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    logLevel = logLevel,
                    eventType = eventType,
                    userId = userId,
                    sessionId = sessionId,
                    eventData = eventData,
                    deviceInfo = deviceInfo,
                    unityVersion = Application.unityVersion,
                    platform = Application.platform.ToString(),
                    systemMemorySize = SystemInfo.systemMemorySize,
                    graphicsMemorySize = SystemInfo.graphicsMemorySize
                };

                var jsonMessage = JsonUtility.ToJson(logEntry, true);
                Debug.Log($"JSON Log Preview: {jsonMessage.Substring(0, Mathf.Min(200, jsonMessage.Length))}...");

                var logEvent = new InputLogEvent
                {
                    Message = jsonMessage,
                    Timestamp = timestamp
                };

                var request = new PutLogEventsRequest
                {
                    LogGroupName = DefaultLogGroup,
                    LogStreamName = CurrentLogStreamName,
                    LogEvents = new List<InputLogEvent> { logEvent }
                };

                var response = await cloudWatchLogsClient.PutLogEventsAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.Log($"App monitoring log sent: {eventType} for user {userId}");
                    OnLogSent?.Invoke(true, $"Monitoring log sent: {eventType}", DefaultLogGroup);
                    return true;
                }

                Debug.LogError($"Failed to send monitoring log: {response.HttpStatusCode}");
                OnLogSent?.Invoke(false, $"Failed to send monitoring log: {response.HttpStatusCode}", DefaultLogGroup);
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"CloudWatch app monitoring log error: {ex.Message}");
                OnLogSent?.Invoke(false, ex.Message, DefaultLogGroup);
                return false;
            }
        }

        private async Task<bool> EnsureLogGroupAndStreamExist()
        {
            try
            {
                var describeLogGroupsRequest = new DescribeLogGroupsRequest
                {
                    LogGroupNamePrefix = DefaultLogGroup
                };
                var logGroupsResponse = await cloudWatchLogsClient.DescribeLogGroupsAsync(describeLogGroupsRequest);

                bool logGroupExists = logGroupsResponse.LogGroups.Any(lg => lg.LogGroupName == DefaultLogGroup);
                if (!logGroupExists)
                {
                    await cloudWatchLogsClient.CreateLogGroupAsync(new CreateLogGroupRequest
                    {
                        LogGroupName = DefaultLogGroup
                    });
                    Debug.Log($"Created log group: {DefaultLogGroup}");
                }

                var describeLogStreamsRequest = new DescribeLogStreamsRequest
                {
                    LogGroupName = DefaultLogGroup,
                    LogStreamNamePrefix = CurrentLogStreamName
                };
                var logStreamsResponse = await cloudWatchLogsClient.DescribeLogStreamsAsync(describeLogStreamsRequest);

                bool logStreamExists = logStreamsResponse.LogStreams.Any(ls => ls.LogStreamName == CurrentLogStreamName);
                if (!logStreamExists)
                {
                    await cloudWatchLogsClient.CreateLogStreamAsync(new CreateLogStreamRequest
                    {
                        LogGroupName = DefaultLogGroup,
                        LogStreamName = CurrentLogStreamName
                    });
                    Debug.Log($"Created log stream: {CurrentLogStreamName}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error ensuring log group/stream exist: {ex.Message}");
                return false;
            }
        }

        private string GetSessionId(string username)
        {
            if (string.IsNullOrEmpty(currentSessionId))
            {
                username = string.IsNullOrEmpty(username) ? "unknown" : username;
                var sessionStart = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                currentSessionId = $"unity-session-{username}-{sessionStart}";
            }
            return currentSessionId;
        }

        public async Task<bool> SendTestLogAsync()
        {
            try
            {
                Debug.Log("Sending test log...");
                var testMessage = $"Test log message from Unity - Session started at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                return await SendLogAsync(testMessage, "INFO");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test log failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendTestMonitoringLogAsync()
        {
            try
            {
                Debug.Log("Sending test monitoring logs...");
                bool success1 = await SendAppMonitoringLogAsync("UserLogin", "User authenticated successfully");
                await Task.Delay(100);
                bool success2 = await SendAppMonitoringLogAsync("UserAction", "User pressed test button", null, "INFO");
                await Task.Delay(100);
                bool success3 = await SendAppMonitoringLogAsync("Performance", $"FPS: {1.0f / Time.deltaTime:F1}, Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB", null, "INFO");

                bool allSuccess = success1 && success2 && success3;
                Debug.Log(allSuccess ? "All test monitoring logs sent" : "Some monitoring logs failed");
                return allSuccess;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test monitoring logs failed: {ex.Message}");
                return false;
            }
        }
    }
}