using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace _scripts.models.awsServices
{
    public sealed class SimpleEmailService
    {
        private readonly IAmazonSimpleEmailService _client;
        private SimpleEmailService(RegionEndpoint region = null)
        {
            region ??= RegionEndpoint.USEast1;
            _client = new AmazonSimpleEmailServiceClient(region);
        }
        private SimpleEmailService(IAwsIdentityContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            _client = new AmazonSimpleEmailServiceClient(ctx.GetCredentials(), ctx.Region ?? RegionEndpoint.USEast1);
        }
        
        
        private static SimpleEmailService? _instance;
        private static readonly object Locker = new object();
        
        public static SimpleEmailService GetInstance(RegionEndpoint region = null)
        {
            if (_instance != null) return _instance;
            lock (Locker)
            {
                _instance ??= new SimpleEmailService(region);
            }
            return _instance;
        }
        
        public static SimpleEmailService GetInstance(IAwsIdentityContext ctx)
        {
            if (_instance != null) return _instance;
            lock (Locker)
            {
                _instance ??= new SimpleEmailService(ctx);
            }
            return _instance;
        }
        internal (bool ok, string error, string messageId) SendEmail(
            string from,
            string to,
            string subject,
            string htmlBody,
            string textBody = null,
            string replyTo = null,
            string configurationSet = null,
            IEnumerable<MessageTag> tags = null)
        {
            var req = new SendEmailRequest
            {
                Source = from,
                Destination = new Destination { ToAddresses = new List<string> { to } },
                Message = new Message
                {
                    Subject = new Content(subject) { Charset = "UTF-8" },
                    Body = new Body
                    {
                        Html = new Content(htmlBody) { Charset = "UTF-8" },
                        Text = string.IsNullOrEmpty(textBody) ? null : new Content(textBody) { Charset = "UTF-8" }
                    }
                }
            };

            if (!string.IsNullOrWhiteSpace(replyTo))
                req.ReplyToAddresses = new List<string> { replyTo };

            if (!string.IsNullOrWhiteSpace(configurationSet))
                req.ConfigurationSetName = configurationSet;

            if (tags != null)
                req.Tags = tags.ToList();

            try
            {
                var resp = _client.SendEmailAsync(req).Result;
                return (true, null, resp.MessageId);
            }
            catch (AggregateException ae)
            {
                var ex = ae.InnerException ?? ae;
                return (false, $"error: {ex.GetType().Name}: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"error: {ex.GetType().Name}: {ex.Message}", null);
            }
        }
        
        internal (bool ok, string error, List<(string email, string messageId, string status)> results)
            SendEmailBulk(string from, IEnumerable<string> recipients, string subject, string htmlBody, string textBody = null)
        {
            if (recipients == null) return (false, "recipients is null", null);

            var list = recipients as IList<string> ?? recipients.ToList();
            if (list.Count == 0) return (false, "recipients is empty", null);

            var results = new List<(string email, string messageId, string status)>();

            foreach (var to in list)
            {
                var one = SendEmail(from, to, subject, htmlBody, textBody);
                if (one.ok) results.Add((to, one.messageId, "Success"));
                else results.Add((to, null, $"Failed: {one.error}"));
            }

            var anyOk = results.Any(r => r.status == "Success");
            return (anyOk, anyOk ? null : "All deliveries failed", results);
        }
    }
}
