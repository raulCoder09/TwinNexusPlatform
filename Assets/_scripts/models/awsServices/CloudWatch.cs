using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

namespace _scripts.models.awsServices
{
    public sealed class CloudWatch
    {
        private static CloudWatch? _instance;
        private static readonly object Locker = new();

        private IAmazonCloudWatchLogs _client = new AmazonCloudWatchLogsClient(RegionEndpoint.USEast1);

        private string _logGroup;
        private string _logStream;
        private string _sequenceToken; // cache

        private CloudWatch() { }

        public static CloudWatch GetInstance(IAwsIdentityContext ctx)
        {
            if (_instance != null) return _instance;
            lock (Locker)
            {
                _instance ??= new CloudWatch();
                if (ctx != null)
                    _instance._client = new AmazonCloudWatchLogsClient(ctx.GetCredentials(), ctx.Region);
            }
            return _instance;
        }
        
        public async Task InitAsync(string logGroupName, string logStreamName, int retentionInDays)
        {
            _logGroup = logGroupName;
            _logStream = logStreamName;

            // Crear grupo si no existe
            try { await _client.CreateLogGroupAsync(new CreateLogGroupRequest { LogGroupName = _logGroup }); }
            catch (ResourceAlreadyExistsException) { }

            // Retención
            await _client.PutRetentionPolicyAsync(new PutRetentionPolicyRequest
            {
                LogGroupName = _logGroup,
                RetentionInDays = retentionInDays
            });
            
            try
            {
                await _client.CreateLogStreamAsync(new CreateLogStreamRequest
                {
                    LogGroupName = _logGroup,
                    LogStreamName = _logStream
                });
            }
            catch (ResourceAlreadyExistsException) { }
            
            var describe = await _client.DescribeLogStreamsAsync(new DescribeLogStreamsRequest
            {
                LogGroupName = _logGroup,
                LogStreamNamePrefix = _logStream,
                Limit = 1
            });

            _sequenceToken = describe.LogStreams
                .FirstOrDefault(s => s.LogStreamName == _logStream)
                ?.UploadSequenceToken;
        }
        
        public async Task<(bool ok, string error)> LogAsync(string message)
        {
            try
            {
                var putReq = new PutLogEventsRequest
                {
                    LogGroupName = _logGroup,
                    LogStreamName = _logStream,
                    SequenceToken = _sequenceToken,
                    LogEvents = new List<InputLogEvent>
                    {
                        new InputLogEvent { Message = message, Timestamp = DateTime.UtcNow }
                    }
                };

                try
                {
                    var resp = await _client.PutLogEventsAsync(putReq);
                    _sequenceToken = resp.NextSequenceToken;
                    return (true, null);
                }
                catch (InvalidSequenceTokenException ex)
                {
                    putReq.SequenceToken = ex.ExpectedSequenceToken;
                    var retry = await _client.PutLogEventsAsync(putReq);
                    _sequenceToken = retry.NextSequenceToken;
                    return (true, null);
                }
            }
            catch (Exception e)
            {
                return (false, $"error: {e.GetType().Name}: {e.Message}");
            }
        }
    }
}
