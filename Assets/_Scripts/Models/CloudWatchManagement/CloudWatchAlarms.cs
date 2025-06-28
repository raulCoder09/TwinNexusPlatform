using System;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using UnityEngine;

namespace _Scripts.Models.CloudWatchManagement
{
    public class CloudWatchAlarms
    {
        [SerializeField] private double defaultThreshold = 30.0;
        [SerializeField] private string defaultComparisonType = "LessThanThreshold";

        private readonly AmazonCloudWatchClient cloudWatchClient;
        private readonly string defaultNamespace;

        public event Action<bool, string, string> OnAlarmCreated;

        public CloudWatchAlarms(AmazonCloudWatchClient client, string defaultNamespace)
        {
            this.cloudWatchClient = client ?? throw new ArgumentNullException(nameof(client));
            this.defaultNamespace = defaultNamespace ?? throw new ArgumentNullException(nameof(defaultNamespace));
        }

        public async Task<bool> CreateSimpleAlarmAsync(string alarmName, string metricName, double threshold, string comparisonType = null)
        {
            try
            {
                comparisonType = comparisonType ?? defaultComparisonType;
                Debug.Log($"Creating alarm: {alarmName}");
                Debug.Log($"Metric: {metricName} {comparisonType} {threshold}, Namespace: {defaultNamespace}");

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

                var request = new PutMetricAlarmRequest
                {
                    AlarmName = alarmName,
                    AlarmDescription = $"Alarm for {metricName} created from Unity",
                    MetricName = metricName,
                    Namespace = defaultNamespace,
                    Statistic = Statistic.Average,
                    Period = 300,
                    EvaluationPeriods = 1,
                    Threshold = threshold,
                    ComparisonOperator = comparisonOperator,
                    TreatMissingData = "notBreaching"
                };

                var response = await cloudWatchClient.PutMetricAlarmAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.Log($"Alarm created successfully: {alarmName}");
                    OnAlarmCreated?.Invoke(true, "Alarm created successfully", alarmName);
                    return true;
                }

                Debug.LogError($"Failed to create alarm: {response.HttpStatusCode}");
                OnAlarmCreated?.Invoke(false, $"Failed to create alarm: {response.HttpStatusCode}", alarmName);
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"CloudWatch create alarm error: {ex.Message}");
                OnAlarmCreated?.Invoke(false, ex.Message, alarmName);
                return false;
            }
        }

        public async Task<bool> CreateTestAlarmAsync(string testMetricName)
        {
            try
            {
                Debug.Log("Creating test alarm...");
                var testAlarmName = $"Unity-{testMetricName}-LowAlert";
                return await CreateSimpleAlarmAsync(testAlarmName, testMetricName, defaultThreshold, defaultComparisonType);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test alarm creation failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TriggerTestAlarmAsync(string testMetricName, CloudWatchMetrics metrics)
        {
            try
            {
                Debug.Log($"Triggering test alarm: Publishing {testMetricName} = 25 (below threshold of {defaultThreshold})");
                return await metrics.PublishSimpleMetricAsync(testMetricName, 25.0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test alarm trigger failed: {ex.Message}");
                return false;
            }
        }
    }
}