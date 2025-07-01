using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using UnityEngine;

namespace _Scripts.Models.CloudWatchManagement
{
    public class CloudWatchMetrics
    {
        [SerializeField] private string testMetricName = "TestMetric";
        [SerializeField] private double testMetricValue = 42.0;

        private readonly AmazonCloudWatchClient cloudWatchClient;
        private readonly string defaultNamespace;

        public event Action<bool, string, string> OnMetricPublished;
        public event Action<bool, string, List<MetricInfo>> OnMetricsListed;

        public CloudWatchMetrics(AmazonCloudWatchClient client, string defaultNamespace)
        {
            this.cloudWatchClient = client ?? throw new ArgumentNullException(nameof(client));
            this.defaultNamespace = defaultNamespace ?? throw new ArgumentNullException(nameof(defaultNamespace));
        }

        public async Task<bool> PublishSimpleMetricAsync(string metricName, double value)
        {
            try
            {
                Debug.Log($"📊 Publishing metric to CloudWatch...");
                Debug.Log($"Namespace: {defaultNamespace}, Metric: {metricName} = {value}");

                var metricData = new MetricDatum
                {
                    MetricName = metricName,
                    Value = value,
                    Unit = StandardUnit.Count,
                    Timestamp = DateTime.UtcNow
                };

                var request = new PutMetricDataRequest
                {
                    Namespace = defaultNamespace,
                    MetricData = new List<MetricDatum> { metricData }
                };

                var response = await cloudWatchClient.PutMetricDataAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.Log($"✅ Metric published: {metricName} = {value}");
                    OnMetricPublished?.Invoke(true, "Metric published successfully", metricName);
                    return true;
                }

                Debug.LogError($"❌ Failed to publish metric: {response.HttpStatusCode}");
                OnMetricPublished?.Invoke(false, $"Failed to publish metric: {response.HttpStatusCode}", metricName);
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"CloudWatch publish metric error: {ex.Message}");
                OnMetricPublished?.Invoke(false, ex.Message, metricName);
                return false;
            }
        }

        public async Task<List<MetricInfo>> ListMetricsAsync()
        {
            try
            {
                Debug.Log($"📋 Listing metrics from namespace: {defaultNamespace}");

                var request = new ListMetricsRequest
                {
                    Namespace = defaultNamespace
                };

                var response = await cloudWatchClient.ListMetricsAsync(request);
                var metricInfoList = new List<MetricInfo>();

                foreach (var metric in response.Metrics)
                {
                    var metricInfo = new MetricInfo
                    {
                        Name = metric.MetricName,
                        Namespace = metric.Namespace,
                        DimensionsInfo = metric.Dimensions?.Count > 0
                            ? string.Join("; ", metric.Dimensions.Select(d => $"{d.Name}={d.Value}"))
                            : string.Empty
                    };
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
    }
}