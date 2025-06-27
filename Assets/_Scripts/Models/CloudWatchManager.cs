using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon;

namespace _Scripts.Models
{
    public class CloudWatchManager : MonoBehaviour
    {
        [Header("CloudWatch Configuration")]
        [SerializeField] private string defaultNamespace = "TwinNexusPlatform";
        [SerializeField] private string defaultLogGroup = "/twin-nexus-platform";
        
        [Header("Test Configuration")]
        [SerializeField] private string testMetricName = "TestMetric";
        [SerializeField] private double testMetricValue = 42.0;
        
        // AWS CloudWatch clients
        private AmazonCloudWatchClient cloudWatchClient;
        private AmazonCloudWatchLogsClient cloudWatchLogsClient;
        private string currentSessionId;
        // Log stream name for this session
        private string currentLogStreamName;
        
        // Events for CloudWatch operations
        public event Action<bool, string, string> OnMetricPublished; // success, message, metricName
        public event Action<bool, string, List<MetricInfo>> OnMetricsListed; // success, message, metrics
        public event Action<bool, string, string> OnAlarmCreated; // success, message, alarmName
        public event Action<bool, string, string> OnLogSent; // success, message, logGroup
        #region Monitoring Log Data Classes

        [System.Serializable]
        public class MonitoringLogEntry
        {
            public string timestamp;
            public string logLevel;
            public string eventType;
            public string userId;
            public string sessionId;
            public string eventData;
            public DeviceInfo deviceInfo;
            public string unityVersion;
            public string platform;
            public int systemMemorySize;
            public int graphicsMemorySize;
        }

        [System.Serializable]
        public class DeviceInfo
        {
            public string deviceModel;
            public string deviceName;
            public string operatingSystem;
            public string processorType;
            public int processorCount;
            public string graphicsDeviceName;
            public string screenResolution;
            public float screenDPI;
        }

        #endregion
        
        // Singleton instance
        public static CloudWatchManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initializes CloudWatch clients with current AWS credentials
        /// </summary>
        private void InitializeCloudWatchClient()
        {
            try
            {
                if (CognitoManager.Instance == null || CognitoManager.Instance.CurrentAWSCredentials == null)
                {
                    Debug.LogError("No AWS credentials available. Please authenticate first.");
                    return;
                }

                var regionEndpoint = RegionEndpoint.USEast1; // Same region as other services
                
                cloudWatchClient = new AmazonCloudWatchClient(CognitoManager.Instance.CurrentAWSCredentials, regionEndpoint);
                cloudWatchLogsClient = new AmazonCloudWatchLogsClient(CognitoManager.Instance.CurrentAWSCredentials, regionEndpoint);
                
                // Initialize log stream name for this session
                var username = CognitoManager.Instance?.CurrentUsername ?? "unknown";
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                currentLogStreamName = $"unity-{username}-{timestamp}";
                
                Debug.Log("CloudWatch clients initialized successfully");
                Debug.Log($"Log stream name: {currentLogStreamName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize CloudWatch clients: {ex.Message}");
            }
        }

        /// <summary>
        /// Publishes a simple metric to CloudWatch
        /// </summary>
        public async Task<bool> PublishSimpleMetricAsync(string metricName, double value)
        {
            try
            {
                // Check if user is authenticated
                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to publish metrics");
                    OnMetricPublished?.Invoke(false, "User not authenticated", metricName);
                    return false;
                }

                // Initialize client if needed
                if (cloudWatchClient == null)
                {
                    InitializeCloudWatchClient();
                    if (cloudWatchClient == null)
                    {
                        OnMetricPublished?.Invoke(false, "Failed to initialize CloudWatch client", metricName);
                        return false;
                    }
                }

                Debug.Log($"📊 Publishing metric to CloudWatch...");
                Debug.Log($"Namespace: {defaultNamespace}");
                Debug.Log($"Metric: {metricName} = {value}");

                // Create metric data
                var metricData = new MetricDatum
                {
                    MetricName = metricName,
                    Value = value,
                    Unit = Amazon.CloudWatch.StandardUnit.Count,
                    Timestamp = DateTime.UtcNow
                };

                // Create put metric request
                var putMetricRequest = new PutMetricDataRequest
                {
                    Namespace = defaultNamespace,
                    MetricData = new List<MetricDatum> { metricData }
                };

                // Publish metric
                var response = await cloudWatchClient.PutMetricDataAsync(putMetricRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.Log($"✅ Metric published successfully: {metricName} = {value}");
                    OnMetricPublished?.Invoke(true, "Metric published successfully", metricName);
                    return true;
                }
                else
                {
                    Debug.LogError($"❌ Failed to publish metric: {response.HttpStatusCode}");
                    OnMetricPublished?.Invoke(false, $"Failed to publish metric: {response.HttpStatusCode}", metricName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"CloudWatch publish metric error: {ex.Message}");
                OnMetricPublished?.Invoke(false, ex.Message, metricName);
                return false;
            }
        }

        /// <summary>
        /// Lists all metrics in the default namespace
        /// </summary>
        public async Task<List<MetricInfo>> ListMetricsAsync()
        {
            try
            {
                // Initialize client if needed
                if (cloudWatchClient == null)
                {
                    InitializeCloudWatchClient();
                    if (cloudWatchClient == null)
                    {
                        OnMetricsListed?.Invoke(false, "Failed to initialize CloudWatch client", new List<MetricInfo>());
                        return new List<MetricInfo>();
                    }
                }
                
                Debug.Log($"📋 Listing metrics from namespace: {defaultNamespace}");

                var listMetricsRequest = new ListMetricsRequest
                {
                    Namespace = defaultNamespace
                };

                var response = await cloudWatchClient.ListMetricsAsync(listMetricsRequest);
                var metricInfoList = new List<MetricInfo>();

                foreach (var metric in response.Metrics)
                {
                    var metricInfo = new MetricInfo
                    {
                        Name = metric.MetricName,
                        Namespace = metric.Namespace
                    };

                    // Add dimensions info if available
                    if (metric.Dimensions != null && metric.Dimensions.Count > 0)
                    {
                        metricInfo.DimensionsInfo = "";
                        foreach (var dimension in metric.Dimensions)
                        {
                            metricInfo.DimensionsInfo += $"{dimension.Name}={dimension.Value}; ";
                        }
                    }

                    metricInfoList.Add(metricInfo);
                }

                Debug.Log($"📊 Found {metricInfoList.Count} metrics in namespace {defaultNamespace}");
                OnMetricsListed?.Invoke(true, $"Found {metricInfoList.Count} metrics", metricInfoList);
                return metricInfoList;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error listing metrics: {ex.Message}");
                OnMetricsListed?.Invoke(false, ex.Message, new List<MetricInfo>());
                return new List<MetricInfo>();
            }
        }

        /// <summary>
        /// Creates a simple CloudWatch alarm for a metric
        /// </summary>
        public async Task<bool> CreateSimpleAlarmAsync(string alarmName, string metricName, double threshold, string comparisonType = "LessThanThreshold")
        {
            try
            {
                // Check if user is authenticated
                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to create alarms");
                    OnAlarmCreated?.Invoke(false, "User not authenticated", alarmName);
                    return false;
                }

                // Initialize client if needed
                if (cloudWatchClient == null)
                {
                    InitializeCloudWatchClient();
                    if (cloudWatchClient == null)
                    {
                        OnAlarmCreated?.Invoke(false, "Failed to initialize CloudWatch client", alarmName);
                        return false;
                    }
                }

                Debug.Log($"🚨 Creating alarm: {alarmName}");
                Debug.Log($"Metric: {metricName} {comparisonType} {threshold}");
                Debug.Log($"Namespace: {defaultNamespace}");

                // Convert string comparison to enum
                ComparisonOperator comparisonOperator;
                switch (comparisonType)
                {
                    case "LessThanThreshold":
                        comparisonOperator = ComparisonOperator.LessThanThreshold;
                        break;
                    case "GreaterThanThreshold":
                        comparisonOperator = ComparisonOperator.GreaterThanThreshold;
                        break;
                    case "LessThanOrEqualToThreshold":
                        comparisonOperator = ComparisonOperator.LessThanOrEqualToThreshold;
                        break;
                    case "GreaterThanOrEqualToThreshold":
                        comparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold;
                        break;
                    default:
                        comparisonOperator = ComparisonOperator.LessThanThreshold;
                        break;
                }

                var putMetricAlarmRequest = new PutMetricAlarmRequest
                {
                    AlarmName = alarmName,
                    AlarmDescription = $"Alarm for {metricName} created from Unity",
                    MetricName = metricName,
                    Namespace = defaultNamespace,
                    Statistic = Statistic.Average,
                    Period = 300, // 5 minutes
                    EvaluationPeriods = 1,
                    Threshold = threshold,
                    ComparisonOperator = comparisonOperator,
                    TreatMissingData = "notBreaching"
                };

                var response = await cloudWatchClient.PutMetricAlarmAsync(putMetricAlarmRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.Log($"✅ Alarm created successfully: {alarmName}");
                    OnAlarmCreated?.Invoke(true, "Alarm created successfully", alarmName);
                    return true;
                }
                else
                {
                    Debug.LogError($"❌ Failed to create alarm: {response.HttpStatusCode}");
                    OnAlarmCreated?.Invoke(false, $"Failed to create alarm: {response.HttpStatusCode}", alarmName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"CloudWatch create alarm error: {ex.Message}");
                OnAlarmCreated?.Invoke(false, ex.Message, alarmName);
                return false;
            }
        }

        /// <summary>
        /// Test method to create a test alarm using inspector values
        /// </summary>
        public async Task<bool> CreateTestAlarmAsync()
        {
            try
            {
                Debug.Log("🧪 Creating test alarm...");
                
                var testAlarmName = $"Unity-{testMetricName}-LowAlert";
                return await CreateSimpleAlarmAsync(testAlarmName, testMetricName, 30.0, "LessThanThreshold");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test alarm creation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends a log message to CloudWatch Logs
        /// </summary>
        public async Task<bool> SendLogAsync(string message, string logLevel = "INFO")
        {
            try
            {
                // Check if user is authenticated
                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to send logs");
                    OnLogSent?.Invoke(false, "User not authenticated", defaultLogGroup);
                    return false;
                }

                // Initialize clients if needed
                if (cloudWatchLogsClient == null)
                {
                    InitializeCloudWatchClient();
                    if (cloudWatchLogsClient == null)
                    {
                        OnLogSent?.Invoke(false, "Failed to initialize CloudWatch Logs client", defaultLogGroup);
                        return false;
                    }
                }

                Debug.Log($"📝 Sending log to CloudWatch...");
                Debug.Log($"Group: {defaultLogGroup}");
                Debug.Log($"Stream: {currentLogStreamName}");
                Debug.Log($"Level: {logLevel}");

                // Ensure log group and stream exist
                await EnsureLogGroupAndStreamExist();

                // Create log event
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var username = CognitoManager.Instance?.CurrentUsername ?? "unknown";
                var formattedMessage = $"[{logLevel}] [{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{username}] {message}";

                var logEvent = new InputLogEvent
                {
                    Message = formattedMessage,
                    Timestamp = DateTime.UtcNow
                };

                var putLogEventsRequest = new PutLogEventsRequest
                {
                    LogGroupName = defaultLogGroup,
                    LogStreamName = currentLogStreamName,
                    LogEvents = new List<InputLogEvent> { logEvent }
                };

                var response = await cloudWatchLogsClient.PutLogEventsAsync(putLogEventsRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.Log($"✅ Log sent successfully to {defaultLogGroup}");
                    OnLogSent?.Invoke(true, "Log sent successfully", defaultLogGroup);
                    return true;
                }
                else
                {
                    Debug.LogError($"❌ Failed to send log: {response.HttpStatusCode}");
                    OnLogSent?.Invoke(false, $"Failed to send log: {response.HttpStatusCode}", defaultLogGroup);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"CloudWatch send log error: {ex.Message}");
                OnLogSent?.Invoke(false, ex.Message, defaultLogGroup);
                return false;
            }
        }

        /// <summary>
        /// Ensures log group and stream exist, creates them if they don't
        /// </summary>
        private async Task<bool> EnsureLogGroupAndStreamExist()
        {
            try
            {
                // Check if log group exists
                try
                {
                    var describeLogGroupsRequest = new DescribeLogGroupsRequest
                    {
                        LogGroupNamePrefix = defaultLogGroup
                    };
                    var logGroupsResponse = await cloudWatchLogsClient.DescribeLogGroupsAsync(describeLogGroupsRequest);
                    
                    bool logGroupExists = logGroupsResponse.LogGroups.Any(lg => lg.LogGroupName == defaultLogGroup);
                    
                    if (!logGroupExists)
                    {
                        // Create log group
                        await cloudWatchLogsClient.CreateLogGroupAsync(new CreateLogGroupRequest
                        {
                            LogGroupName = defaultLogGroup
                        });
                        Debug.Log($"Created log group: {defaultLogGroup}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error with log group: {ex.Message}");
                }

                // Check if log stream exists
                try
                {
                    var describeLogStreamsRequest = new DescribeLogStreamsRequest
                    {
                        LogGroupName = defaultLogGroup,
                        LogStreamNamePrefix = currentLogStreamName
                    };
                    var logStreamsResponse = await cloudWatchLogsClient.DescribeLogStreamsAsync(describeLogStreamsRequest);
                    
                    bool logStreamExists = logStreamsResponse.LogStreams.Any(ls => ls.LogStreamName == currentLogStreamName);
                    
                    if (!logStreamExists)
                    {
                        // Create log stream
                        await cloudWatchLogsClient.CreateLogStreamAsync(new CreateLogStreamRequest
                        {
                            LogGroupName = defaultLogGroup,
                            LogStreamName = currentLogStreamName
                        });
                        Debug.Log($"Created log stream: {currentLogStreamName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error with log stream: {ex.Message}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error ensuring log group/stream exist: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test method to send a test log
        /// </summary>
        public async Task<bool> SendTestLogAsync()
        {
            try
            {
                Debug.Log("🧪 Sending test log...");
                
                var testMessage = $"Test log message from Unity - Session started at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
                return await SendLogAsync(testMessage, "INFO");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test log failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test method to trigger the test alarm by publishing a low value
        /// </summary>
        public async Task<bool> TriggerTestAlarmAsync()
        {
            try
            {
                Debug.Log("🔥 Triggering test alarm...");
                Debug.Log($"Publishing {testMetricName} = 25 (below threshold of 30)");
                
                // Publish a low value to trigger the alarm
                return await PublishSimpleMetricAsync(testMetricName, 25.0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test alarm trigger failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test method to publish test metric using inspector values
        /// </summary>
        public async Task<bool> PublishTestMetricAsync()
        {
            try
            {
                Debug.Log("🧪 Publishing test metric...");
                return await PublishSimpleMetricAsync(testMetricName, testMetricValue);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test metric failed: {ex.Message}");
                return false;
            }
        }
        

/// <summary>
/// Sends application monitoring logs with structured data for user activity tracking
/// </summary>
/// <param name="eventType">Type of event (UserLogin, UserAction, Error, Performance, etc.)</param>
/// <param name="eventData">Additional data about the event</param>
/// <param name="userId">User identifier (optional, will use current user if null)</param>
/// <param name="logLevel">Log level (INFO, WARN, ERROR)</param>
public async Task<bool> SendAppMonitoringLogAsync(string eventType, string eventData, string userId = null, string logLevel = "INFO")
{
    try
    {
        // Check if user is authenticated
        if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
        {
            Debug.LogError("User must be authenticated to send monitoring logs");
            OnLogSent?.Invoke(false, "User not authenticated", defaultLogGroup);
            return false;
        }

        // Initialize clients if needed
        if (cloudWatchLogsClient == null)
        {
            InitializeCloudWatchClient();
            if (cloudWatchLogsClient == null)
            {
                OnLogSent?.Invoke(false, "Failed to initialize CloudWatch Logs client", defaultLogGroup);
                return false;
            }
        }

        // Use current user if userId not provided
        if (string.IsNullOrEmpty(userId))
        {
            userId = CognitoManager.Instance?.CurrentUsername ?? "unknown";
        }

        Debug.Log($"📱 Sending app monitoring log to CloudWatch...");
        Debug.Log($"Event Type: {eventType}");
        Debug.Log($"User ID: {userId}");
        Debug.Log($"Log Level: {logLevel}");

        // Ensure log group and stream exist
        await EnsureLogGroupAndStreamExist();

        // Create structured log entry with proper serializable classes
        var timestamp = DateTime.UtcNow;
        var sessionId = GetSessionId();
        
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

        // Convert to JSON string using Unity's JsonUtility
        var jsonMessage = JsonUtility.ToJson(logEntry, true); // prettyPrint = true for better readability

        Debug.Log($"📝 JSON Log Preview: {jsonMessage.Substring(0, Mathf.Min(200, jsonMessage.Length))}...");

        var logEvent = new InputLogEvent
        {
            Message = jsonMessage,
            Timestamp = timestamp
        };

        var putLogEventsRequest = new PutLogEventsRequest
        {
            LogGroupName = defaultLogGroup,
            LogStreamName = currentLogStreamName,
            LogEvents = new List<InputLogEvent> { logEvent }
        };

        var response = await cloudWatchLogsClient.PutLogEventsAsync(putLogEventsRequest);

        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            Debug.Log($"✅ App monitoring log sent successfully");
            Debug.Log($"📊 Event: {eventType} for user {userId}");
            OnLogSent?.Invoke(true, $"Monitoring log sent: {eventType}", defaultLogGroup);
            return true;
        }
        else
        {
            Debug.LogError($"❌ Failed to send monitoring log: {response.HttpStatusCode}");
            OnLogSent?.Invoke(false, $"Failed to send monitoring log: {response.HttpStatusCode}", defaultLogGroup);
            return false;
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"CloudWatch app monitoring log error: {ex.Message}");
        OnLogSent?.Invoke(false, ex.Message, defaultLogGroup);
        return false;
    }
}

/// <summary>
/// Gets or creates a session ID for this Unity session
/// </summary>
private string GetSessionId()
{
    // Create a session ID if it doesn't exist
    if (string.IsNullOrEmpty(currentSessionId))
    {
        var username = CognitoManager.Instance?.CurrentUsername ?? "unknown";
        var sessionStart = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        currentSessionId = $"unity-session-{username}-{sessionStart}";
    }
    return currentSessionId;
}

/// <summary>
/// Test method to send various types of monitoring logs
/// </summary>
public async Task<bool> SendTestMonitoringLogAsync()
{
    try
    {
        Debug.Log("🧪 Sending test monitoring logs...");
        
        // Test different event types
        bool success1 = await SendAppMonitoringLogAsync("UserLogin", "User authenticated successfully");
        await Task.Delay(100); // Small delay between logs
        
        bool success2 = await SendAppMonitoringLogAsync("UserAction", "User pressed test button", null, "INFO");
        await Task.Delay(100);
        
        bool success3 = await SendAppMonitoringLogAsync("Performance", $"FPS: {1.0f / Time.deltaTime:F1}, Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB", null, "INFO");
        
        bool allSuccess = success1 && success2 && success3;
        Debug.Log(allSuccess ? "✅ All test monitoring logs sent" : "❌ Some monitoring logs failed");
        
        return allSuccess;
    }
    catch (Exception ex)
    {
        Debug.LogError($"Test monitoring logs failed: {ex.Message}");
        return false;
    }
}


        private void OnDestroy()
        {
            cloudWatchClient?.Dispose();
            cloudWatchLogsClient?.Dispose();
        }
    }

    #region Data Classes

    [System.Serializable]
    public class MetricInfo
    {
        public string Name;
        public string Namespace;
        public string DimensionsInfo;
    }

    #endregion
}
