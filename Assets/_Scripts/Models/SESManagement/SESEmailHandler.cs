using Amazon;

namespace _Scripts.Models.SESManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Amazon.SimpleEmail;
    using Amazon.SimpleEmail.Model;
    using Amazon.Runtime;
    using UnityEngine;
    using _Scripts.Models.CognitoManagement;

    public class SESEmailHandler : ISESOps
    {
        private readonly AmazonSimpleEmailServiceClient _sesClient;
        private readonly SESInfo.EmailConfiguration _config;
        private readonly bool _enableDebugLogs = true;

        public SESEmailHandler(SESInfo.EmailConfiguration config, AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sesClient = new AmazonSimpleEmailServiceClient(credentials, regionEndpoint);
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string textBody, string htmlBody = null)
        {
            try
            {
                if (!IsValidEmail(toEmail))
                {
                    LogError("Invalid recipient email address");
                    return false;
                }

                if (!CanUserSendEmails())
                {
                    LogError("User does not have permission to send emails");
                    return false;
                }

                var body = new Body();
                if (!string.IsNullOrEmpty(textBody))
                {
                    body.Text = new Content { Charset = "UTF-8", Data = textBody };
                }
                if (!string.IsNullOrEmpty(htmlBody))
                {
                    body.Html = new Content { Charset = "UTF-8", Data = htmlBody };
                }

                var sendRequest = new SendEmailRequest
                {
                    Source = $"{_config.SenderName} <{_config.SenderEmail}>",
                    Destination = new Destination { ToAddresses = new List<string> { toEmail } },
                    Message = new Message
                    {
                        Subject = new Content { Charset = "UTF-8", Data = subject },
                        Body = body
                    }
                };

                var response = await _sesClient.SendEmailAsync(sendRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    LogDebug($"Email sent successfully to {toEmail}, MessageId: {response.MessageId}");
                    return true;
                }
                else
                {
                    LogError($"Email sending failed with status: {response.HttpStatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"SES email sending error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendEmailWithUserContextAsync(string toEmail, string subject, string message)
        {
            try
            {
                var cognitoManager = CognitoManager.Instance;
                if (cognitoManager == null || !cognitoManager.IsUserAuthenticated)
                {
                    LogError("User must be authenticated to send emails with context");
                    return false;
                }

                var contextualMessage = $"{message}\n\n---\nUser Context:\nUsername: {cognitoManager.CurrentUsername ?? "Unknown"}\nRole: {cognitoManager.GetUserRole() ?? "Unknown"}\nTimestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\nPlatform: Unity Game Engine\nSystem: Twin Nexus Platform";
                return await SendEmailAsync(toEmail, subject, contextualMessage);
            }
            catch (Exception ex)
            {
                LogError($"Error sending email with user context: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSystemNotificationAsync(string subject, string message, NotificationType type = NotificationType.Info)
        {
            try
            {
                var cognitoManager = CognitoManager.Instance;
                if (cognitoManager == null || !cognitoManager.IsUserAuthenticated)
                {
                    LogError("User must be authenticated to send system notifications");
                    return false;
                }

                var formattedSubject = $"{(type == NotificationType.Success ? "Success" : type == NotificationType.Warning ? "Warning" : type == NotificationType.Error ? "Error" : "Info")} - {subject}";
                var systemMessage = $"System Notification - Twin Nexus Platform\n\n{message}\n\n---\nSystem Information:\nTimestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\nPlatform: Unity\nCloud: AWS\nTriggered by: {cognitoManager.CurrentUsername ?? "System"}\nUser Role: {cognitoManager.GetUserRole() ?? "Unknown"}\nNotification Type: {type}";

                return await SendEmailAsync(GetDefaultRecipient(), formattedSubject, systemMessage);
            }
            catch (Exception ex)
            {
                LogError($"Error sending system notification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendTestEmailAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(GetDefaultRecipient()))
                {
                    LogWarning("No test recipient email configured");
                    return false;
                }

                var testMessageWithDetails = $"{testMessage}\n\n---\nTest Details:\nSent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\nUser: {CognitoManager.Instance?.CurrentUsername ?? "Unknown"}\nRole: {CognitoManager.Instance?.GetUserRole() ?? "Unknown"}\nTest ID: {Guid.NewGuid()}";
                return await SendEmailAsync(GetDefaultRecipient(), "Test Email from Unity", testMessageWithDetails);
            }
            catch (Exception ex)
            {
                LogError($"Test email failed: {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetVerifiedEmailsAsync()
        {
            try
            {
                var request = new ListIdentitiesRequest();
                var response = await _sesClient.ListIdentitiesAsync(request);

                var verifiedEmails = new List<string>();
                foreach (var identity in response.Identities)
                {
                    if (identity.Contains("@"))
                    {
                        verifiedEmails.Add(identity);
                    }
                }

                LogDebug($"Found {verifiedEmails.Count} verified email addresses");
                return verifiedEmails;
            }
            catch (Exception ex)
            {
                LogError($"Error checking verified emails: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<bool> RequestEmailVerificationAsync(string emailToVerify)
        {
            try
            {
                if (!IsValidEmail(emailToVerify))
                {
                    LogError("Invalid email address for verification");
                    return false;
                }

                var request = new VerifyEmailIdentityRequest { EmailAddress = emailToVerify };
                var response = await _sesClient.VerifyEmailIdentityAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    LogDebug($"Verification email sent to: {emailToVerify}");
                    return true;
                }
                else
                {
                    LogError($"Failed to send verification email: {response.HttpStatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error requesting email verification: {ex.Message}");
                return false;
            }
        }

        public bool CanUserSendEmails()
        {
            var cognitoManager = CognitoManager.Instance;
            if (cognitoManager == null || !cognitoManager.IsUserAuthenticated)
            {
                return false;
            }

            var userRole = cognitoManager.GetUserRole();
            return userRole switch
            {
                "super-admin" => true,
                "operators" => true,
                "students" => true,
                "usuarios-basicos" => false,
                _ => false
            };
        }

        public int GetDailyEmailLimit()
        {
            var cognitoManager = CognitoManager.Instance;
            if (cognitoManager == null || !cognitoManager.IsUserAuthenticated)
            {
                return 0;
            }

            var userRole = cognitoManager.GetUserRole();
            return userRole switch
            {
                "super-admin" => 1000,
                "operators" => 200,
                "students" => 50,
                "usuarios-basicos" => 0,
                _ => 0
            };
        }

        private string GetDefaultRecipient()
        {
            return string.IsNullOrEmpty(testRecipientEmail) ? "default@example.com" : testRecipientEmail;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private string testRecipientEmail; // Campo privado para uso interno
        private string testMessage; // Campo privado para uso interno

        private void Awake()
        {
            testRecipientEmail = string.IsNullOrEmpty(testRecipientEmail) ? "default@example.com" : testRecipientEmail;
            testMessage = string.IsNullOrEmpty(testMessage) ? "This is a test message from Unity using AWS SES." : testMessage;
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[SESEmailHandler] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[SESEmailHandler] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[SESEmailHandler] {message}");
        }
    }
}