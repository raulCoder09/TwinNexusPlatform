using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatchLogs;
using UnityEngine;

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

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            try
            {
                if (CognitoManager.Instance == null || CognitoManager.Instance.CurrentAWSCredentials == null)
                {
                    Debug.LogError("No AWS credentials available. Please authenticate first.");
                    return;
                }

                var regionEndpoint = Amazon.RegionEndpoint.USEast1;
                cloudWatchClient = new AmazonCloudWatchClient(CognitoManager.Instance.CurrentAWSCredentials, regionEndpoint);
                cloudWatchLogsClient = new AmazonCloudWatchLogsClient(CognitoManager.Instance.CurrentAWSCredentials, regionEndpoint);

                Metrics = new CloudWatchMetrics(cloudWatchClient, defaultNamespace);
                Alarms = new CloudWatchAlarms(cloudWatchClient, defaultNamespace);
                Logs = new CloudWatchLogs(cloudWatchLogsClient, defaultLogGroup, CognitoManager.Instance?.CurrentUsername);

                Debug.Log("CloudWatchManager initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize CloudWatchManager: {ex.Message}");
            }
        }

        public async Task<bool> PublishMetric(string metricName, double value)
        {
            if (Metrics == null)
            {
                Debug.LogError("Metrics not initialized");
                return false;
            }
            return await Metrics.PublishSimpleMetricAsync(metricName, value);
        }

        public async Task<List<MetricInfo>> ListMetrics()
        {
            if (Metrics == null)
            {
                Debug.LogError("Metrics not initialized");
                return new List<MetricInfo>();
            }
            return await Metrics.ListMetricsAsync();
        }

        public async Task<bool> CreateAlarm(string alarmName, string metricName, double threshold, string comparisonType = null)
        {
            if (Alarms == null)
            {
                Debug.LogError("Alarms not initialized");
                return false;
            }
            return await Alarms.CreateSimpleAlarmAsync(alarmName, metricName, threshold, comparisonType);
        }

        public async Task<bool> SendLog(string message, string logLevel = null)
        {
            if (Logs == null)
            {
                Debug.LogError("Logs not initialized");
                return false;
            }
            return await Logs.SendLogAsync(message, logLevel);
        }

        public async Task<bool> SendAppMonitoringLog(string eventType, string eventData, string userId = null, string logLevel = null)
        {
            if (Logs == null)
            {
                Debug.LogError("Logs not initialized");
                return false;
            }
            return await Logs.SendAppMonitoringLogAsync(eventType, eventData, userId, logLevel);
        }

        private void OnDestroy()
        {
            cloudWatchClient?.Dispose();
            cloudWatchLogsClient?.Dispose();
        }
    }
}