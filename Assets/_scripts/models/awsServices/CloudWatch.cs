using System;
using System.Collections.Generic;
using System.Linq;
using _scripts.models.awsServices;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

namespace AmazonWebServices
{
    public sealed class CloudWatch
    {
        private static CloudWatch? _instance;
        private static readonly object Locker = new object();

        // Ajusta la región si lo necesitas.
        private static IAmazonCloudWatchLogs _client =
            new AmazonCloudWatchLogsClient(RegionEndpoint.USEast1);

        private CloudWatch() { }

        public static CloudWatch GetInstance()
        {
            if (_instance != null) return _instance;
            lock (Locker)
            {
                _instance ??= new CloudWatch();
            }
            return _instance;
        }
        
        internal static CloudWatch GetInstance(IAwsIdentityContext ctx)
        {
            if (_instance != null) return _instance;
            lock (Locker)
            {
                _instance ??= new CloudWatch();
                if (ctx != null)
                {
                    _client = new Amazon.CloudWatchLogs.AmazonCloudWatchLogsClient(
                        ctx.GetCredentials(), ctx.Region
                    );
                }
            }
            return _instance;
        }

        
        internal (bool ok, string error) Log(string logGroupName, string logStreamName, string message, int retentionInDays)
        {
            try
            {
                EnsureLogGroup(logGroupName,retentionInDays);
                EnsureLogStream(logGroupName, logStreamName);

                var describe = _client.DescribeLogStreamsAsync(new DescribeLogStreamsRequest
                {
                    LogGroupName = logGroupName,
                    LogStreamNamePrefix = logStreamName,
                    Limit = 1
                }).Result;

                var stream = describe.LogStreams.FirstOrDefault(s => s.LogStreamName == logStreamName);
                var sequenceToken = stream?.UploadSequenceToken;

                var putReq = new PutLogEventsRequest
                {
                    LogGroupName = logGroupName,
                    LogStreamName = logStreamName,
                    SequenceToken = sequenceToken,
                    LogEvents = new List<InputLogEvent>
                    {
                        new InputLogEvent
                        {
                            Message = message,
                            Timestamp = DateTime.UtcNow
                        }
                    }
                };

                try
                {
                    _ = _client.PutLogEventsAsync(putReq).Result;             
                    return (true, null);
                }
                catch (Exception e1)
                {
                    var ex1 = (e1 as AggregateException)?.InnerException ?? e1;

                    if (ex1 is InvalidSequenceTokenException badToken &&
                        !string.IsNullOrEmpty(badToken.ExpectedSequenceToken))
                    {
                        putReq.SequenceToken = badToken.ExpectedSequenceToken;
                        _ = _client.PutLogEventsAsync(putReq).Result;          
                        return (true, null);
                    }

                    return (false, $"error: {ex1.GetType().Name}: {ex1.Message}");
                }
            }
            catch (Exception e)
            {
                var ex = (e as AggregateException)?.InnerException ?? e;
                return (false, $"error: {ex.GetType().Name}: {ex.Message}");
            }
        }
        private void EnsureLogGroup(string name,  int retentionInDays)
        {
            try
            {
                _client.CreateLogGroupAsync(new CreateLogGroupRequest { LogGroupName = name }).Wait();
                _client.PutRetentionPolicyAsync(new PutRetentionPolicyRequest { LogGroupName = name, RetentionInDays = retentionInDays}).Wait();
            }
            catch (AggregateException ae) when (ae.InnerException is ResourceAlreadyExistsException) { }
            catch (ResourceAlreadyExistsException) { }
        }

        private void EnsureLogStream(string group, string stream)
        {
            try
            {
                _client.CreateLogStreamAsync(new CreateLogStreamRequest
                {
                    LogGroupName = group,
                    LogStreamName = stream
                }).Wait();
            }
            catch (AggregateException ae) when (ae.InnerException is ResourceAlreadyExistsException) { }
            catch (ResourceAlreadyExistsException) { }
        }
    }
}
