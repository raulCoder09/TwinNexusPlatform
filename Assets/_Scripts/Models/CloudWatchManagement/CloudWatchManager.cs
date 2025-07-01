using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatchLogs;
using UnityEngine;
using _Scripts.Models.CognitoManagement;
using _Scripts.Models.SESManagement;
using Amazon;
using Amazon.Runtime;

namespace _Scripts.Models.CloudWatchManagement
{
    public class CloudWatchManager : MonoBehaviour
    {
        [SerializeField] private string defaultNamespace = "TwinNexusPlatform";
        [SerializeField] private string defaultLogGroup = "/twin-nexus-platform";

        private AmazonCloudWatchClient cloudWatchClient;
        private AmazonCloudWatchLogsClient cloudWatchLogsClient;

        public CloudWatchMetrics Metrics { get; private set; }
        public CloudWatchAlarms Alarms { get; private set; }
        public CloudWatchLogs Logs { get; private set; }

        public static CloudWatchManager Instance { get; private set; }

        private bool _isInitialized = false;

        private void Awake()
        {
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

        public async Task<bool> InitializeAsync(AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            if (_isInitialized) return true;

            try
            {
                if (credentials == null)
                {
                    Debug.LogError("No AWS credentials available. Please authenticate first.");
                    return false;
                }

                cloudWatchClient = new AmazonCloudWatchClient(credentials, regionEndpoint);
                cloudWatchLogsClient = new AmazonCloudWatchLogsClient(credentials, regionEndpoint);

                Metrics = new CloudWatchMetrics(cloudWatchClient, defaultNamespace);
                Alarms = new CloudWatchAlarms(cloudWatchClient, defaultNamespace);
                Logs = new CloudWatchLogs(cloudWatchLogsClient, defaultLogGroup, CognitoManager.Instance?.CurrentUsername);

                _isInitialized = true;
                Debug.Log("CloudWatchManager initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize CloudWatchManager: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PublishMetric(string metricName, double value)
        {
            if (!_isInitialized || Metrics == null)
            {
                Debug.LogError("CloudWatchManager not initialized or Metrics not available");
                return false;
            }
            return await Metrics.PublishSimpleMetricAsync(metricName, value);
        }

        public async Task<List<MetricInfo>> ListMetrics()
        {
            if (!_isInitialized || Metrics == null)
            {
                Debug.LogError("CloudWatchManager not initialized or Metrics not available");
                return new List<MetricInfo>();
            }
            return await Metrics.ListMetricsAsync();
        }

        public async Task<bool> CreateAlarm(string alarmName, string metricName, double threshold, string comparisonType = null)
        {
            if (!_isInitialized || Alarms == null)
            {
                Debug.LogError("CloudWatchManager not initialized or Alarms not available");
                return false;
            }
            return await Alarms.CreateSimpleAlarmAsync(alarmName, metricName, threshold, comparisonType);
        }

        public async Task<bool> SendLog(string message, string logLevel = null)
        {
            if (!_isInitialized || Logs == null)
            {
                Debug.LogError("CloudWatchManager not initialized or Logs not available");
                return false;
            }
            return await Logs.SendLogAsync(message, logLevel);
        }

        public async Task<bool> SendAppMonitoringLog(string eventType, string eventData, string userId = null, string logLevel = null)
        {
            if (!_isInitialized || Logs == null)
            {
                Debug.LogError("CloudWatchManager not initialized or Logs not available");
                return false;
            }
            return await Logs.SendAppMonitoringLogAsync(eventType, eventData, userId, logLevel);
        }

        public async Task<bool> LogUIEvent(string eventDescription)
        {
            if (!_isInitialized || Logs == null)
            {
                Debug.LogError("CloudWatchManager not initialized or Logs not available");
                return false;
            }
            string userId = CognitoManager.Instance?.CurrentUsername ?? "unknown";
            return await Logs.SendAppMonitoringLogAsync("UIEvent", eventDescription, userId, "INFO");
        }

        private void OnDestroy()
        {
            cloudWatchClient?.Dispose();
            cloudWatchLogsClient?.Dispose();
        }
    }
}