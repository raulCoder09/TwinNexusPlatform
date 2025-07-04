using Amazon;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.Runtime;
using UnityEngine;
using _Scripts.Models.CognitoManagement;

namespace _Scripts.Models.SESManagement
{
    /// <summary>
    /// Manejador principal de emails SES
    /// Implementa patrón Repository + Unit of Work para operaciones de email
    /// Responsabilidad: Operaciones básicas de envío y gestión de emails
    /// </summary>
    public class SESEmailHandler : ISESAdvancedOps, IDisposable
    {
        #region Private Fields
        
        private readonly AmazonSimpleEmailServiceClient _sesClient;
        private readonly SESInfo.EmailConfiguration _config;
        private readonly ILogger _logger;
        private readonly Dictionary<string, SESInfo.EmailData> _emailRepository;
        private readonly SESTemplateManager _templateManager;
        private readonly SESBulkManager _bulkManager;
        
        private bool _disposed = false;
        private bool _templatesInitialized = false;

        #endregion

        #region Constructor & Initialization

        public SESEmailHandler(SESInfo.EmailConfiguration config, AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = new UnityLogger("SESEmailHandler");
            _emailRepository = new Dictionary<string, SESInfo.EmailData>();
            
            try
            {
                _sesClient = new AmazonSimpleEmailServiceClient(credentials, regionEndpoint);
                _templateManager = new SESTemplateManager(_config, credentials, regionEndpoint);
                _bulkManager = new SESBulkManager(_config, credentials, regionEndpoint);
                
                _logger.LogDebug("SES Email Handler initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize SES Email Handler: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region ISESOps Implementation

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string textBody, string htmlBody = null)
        {
            try
            {
                // Validaciones usando SESInfo.Validation
                if (!SESInfo.Validation.IsValidEmail(toEmail))
                {
                    _logger.LogError("Invalid recipient email address");
                    return false;
                }

                if (!SESInfo.Validation.IsValidSubject(subject))
                {
                    _logger.LogError("Invalid email subject");
                    return false;
                }

                if (!SESInfo.Validation.IsValidBody(textBody))
                {
                    _logger.LogError("Invalid email body");
                    return false;
                }

                // Verificar permisos
                if (!CanUserSendEmails())
                {
                    _logger.LogError("User does not have permission to send emails");
                    return false;
                }

                // Crear datos del email
                var emailData = new SESInfo.EmailData(toEmail, subject, textBody, htmlBody);
                
                // Preparar el request
                var sendRequest = CreateSendEmailRequest(emailData);
                
                // Enviar
                var response = await _sesClient.SendEmailAsync(sendRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Actualizar estado y trackear
                    emailData.UpdateStatus(SESInfo.EmailStatus.Sent);
                    _emailRepository[response.MessageId] = emailData;
                    TrackEmailSent(response.MessageId, toEmail, subject);
                    
                    _logger.LogDebug($"Email sent successfully to {toEmail}, MessageId: {response.MessageId}");
                    return true;
                }
                else
                {
                    emailData.UpdateStatus(SESInfo.EmailStatus.Failed);
                    _logger.LogError($"Email sending failed with status: {response.HttpStatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SES email sending error: {ex.Message}");
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
                    _logger.LogError("User must be authenticated to send emails with context");
                    return false;
                }

                var contextualMessage = BuildUserContextMessage(message, cognitoManager);
                return await SendEmailAsync(toEmail, subject, contextualMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email with user context: {ex.Message}");
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
                    _logger.LogError("User must be authenticated to send system notifications");
                    return false;
                }

                // Usar template si está disponible, sino formato tradicional
                if (_templatesInitialized)
                {
                    return await _templateManager.SendSystemAlertEmailAsync(
                        GetDefaultRecipient(), 
                        subject, 
                        message, 
                        type);
                }
                else
                {
                    var formattedSubject = FormatNotificationSubject(subject, type);
                    var systemMessage = BuildSystemNotificationMessage(message, type, cognitoManager);
                    return await SendEmailAsync(GetDefaultRecipient(), formattedSubject, systemMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending system notification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendTestEmailAsync()
        {
            try
            {
                var defaultRecipient = GetDefaultRecipient();
                if (string.IsNullOrEmpty(defaultRecipient))
                {
                    _logger.LogWarning("No test recipient email configured");
                    return false;
                }

                var testMessage = BuildTestMessage();
                return await SendEmailAsync(defaultRecipient, "Test Email from Unity", testMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Test email failed: {ex.Message}");
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

                _logger.LogDebug($"Found {verifiedEmails.Count} verified email addresses");
                return verifiedEmails;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking verified emails: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<bool> RequestEmailVerificationAsync(string emailToVerify)
        {
            try
            {
                if (!SESInfo.Validation.IsValidEmail(emailToVerify))
                {
                    _logger.LogError("Invalid email address for verification");
                    return false;
                }

                var request = new VerifyEmailIdentityRequest { EmailAddress = emailToVerify };
                var response = await _sesClient.VerifyEmailIdentityAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogDebug($"Verification email sent to: {emailToVerify}");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to send verification email: {response.HttpStatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error requesting email verification: {ex.Message}");
                return false;
            }
        }

        public bool CanUserSendEmails()
        {
            var permissions = GetCurrentUserPermissions();
            return permissions.CanSendEmails;
        }

        public int GetDailyEmailLimit()
        {
            var permissions = GetCurrentUserPermissions();
            return permissions.DailyEmailLimit;
        }

        #endregion

        #region ISESAdvancedOps Implementation

        public async Task<bool> SendTemplatedEmailAsync(string toEmail, string templateName, Dictionary<string, string> templateData)
        {
            try
            {
                if (!_templatesInitialized)
                {
                    _logger.LogWarning("Templates not initialized. Call InitializeTemplatesAsync() first.");
                    return false;
                }

                return await _templateManager.SendTemplatedEmailAsync(toEmail, templateName, templateData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending templated email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> InitializeTemplatesAsync()
        {
            try
            {
                _templatesInitialized = await _templateManager.InitializePredefinedTemplatesAsync();
                if (_templatesInitialized)
                {
                    _logger.LogDebug("Email templates initialized successfully");
                }
                return _templatesInitialized;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error initializing templates: {ex.Message}");
                return false;
            }
        }

        public async Task<SESOperationResult> SendBulkEmailAsync(List<string> recipients, string subject, string textBody, string htmlBody = null)
        {
            try
            {
                var permissions = GetCurrentUserPermissions();
                if (!permissions.CanSendToRecipientCount(recipients.Count))
                {
                    return SESOperationResult.FailureResult(
                        $"User cannot send to {recipients.Count} recipients. Bulk limit: {permissions.BulkEmailLimit}");
                }

                var bulkResult = await _bulkManager.SendBulkEmailAsync(recipients, subject, textBody, htmlBody);
                
                return new SESOperationResult
                {
                    Success = bulkResult.Success,
                    Message = bulkResult.Message,
                    OperationId = bulkResult.JobId,
                    TotalProcessed = bulkResult.TotalRecipients,
                    SuccessCount = bulkResult.SuccessfulSends.Count,
                    FailureCount = bulkResult.FailedSends.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in bulk email operation: {ex.Message}");
                return SESOperationResult.FailureResult(ex.Message);
            }
        }

        public void TrackEmailSent(string messageId, string recipient, string subject)
        {
            try
            {
                // Aquí podrías integrar con analytics o logging avanzado
                _logger.LogDebug($"Email tracked - ID: {messageId}, To: {recipient}, Subject: {subject}");
                
                // Si tuvieras un sistema de analytics, lo llamarías aquí
                // _analytics?.TrackEmailSent(messageId, recipient, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error tracking email: {ex.Message}");
            }
        }

        public async Task<SESQuotaInfo> GetSendQuotaAsync()
        {
            try
            {
                var request = new GetSendQuotaRequest();
                var response = await _sesClient.GetSendQuotaAsync(request);

                return new SESQuotaInfo
                {
                    Max24HourSend = response.Max24HourSend ?? 0,
                    MaxSendRate = response.MaxSendRate ?? 0,
                    SentLast24Hours = response.SentLast24Hours ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting send quota: {ex.Message}");
                return new SESQuotaInfo();
            }
        }

        #endregion

        #region Private Helper Methods

        private SendEmailRequest CreateSendEmailRequest(SESInfo.EmailData emailData)
        {
            var body = new Body();
            
            if (!string.IsNullOrEmpty(emailData.TextBody))
            {
                body.Text = new Content { Charset = "UTF-8", Data = emailData.TextBody };
            }
            
            if (!string.IsNullOrEmpty(emailData.HtmlBody))
            {
                body.Html = new Content { Charset = "UTF-8", Data = emailData.HtmlBody };
            }

            return new SendEmailRequest
            {
                Source = $"{_config.SenderName} <{_config.SenderEmail}>",
                Destination = new Destination { ToAddresses = new List<string> { emailData.ToEmail } },
                Message = new Message
                {
                    Subject = new Content { Charset = "UTF-8", Data = emailData.Subject },
                    Body = body
                }
            };
        }

        private SESInfo.UserPermissions GetCurrentUserPermissions()
        {
            var cognitoManager = CognitoManager.Instance;
            if (cognitoManager == null || !cognitoManager.IsUserAuthenticated)
            {
                return new SESInfo.UserPermissions(SESInfo.UserRole.Unknown);
            }

            var roleString = cognitoManager.GetUserRole();
            var role = SESInfo.Validation.ParseUserRole(roleString);
            return new SESInfo.UserPermissions(role);
        }

        private string BuildUserContextMessage(string message, CognitoManager cognitoManager)
        {
            return $@"{message}

---
User Context:
Username: {cognitoManager.CurrentUsername ?? "Unknown"}
Role: {cognitoManager.GetUserRole() ?? "Unknown"}
Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
Platform: Unity Game Engine
System: Twin Nexus Platform";
        }

        private string BuildSystemNotificationMessage(string message, NotificationType type, CognitoManager cognitoManager)
        {
            return $@"System Notification - Twin Nexus Platform

{message}

---
System Information:
Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
Platform: Unity
Cloud: AWS
Triggered by: {cognitoManager.CurrentUsername ?? "System"}
User Role: {cognitoManager.GetUserRole() ?? "Unknown"}
Notification Type: {type}";
        }

        private string FormatNotificationSubject(string subject, NotificationType type)
        {
            var prefix = type switch
            {
                NotificationType.Success => "SUCCESS",
                NotificationType.Warning => "WARNING", 
                NotificationType.Error => "ERROR",
                NotificationType.Info => "INFO",
                _ => "NOTIFICATION"
            };

            return $"[{prefix}] {subject}";
        }

        private string BuildTestMessage()
        {
            var cognitoManager = CognitoManager.Instance;
            return $@"This is a test message from Unity using AWS SES.

---
Test Details:
Sent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
User: {cognitoManager?.CurrentUsername ?? "Unknown"}
Role: {cognitoManager?.GetUserRole() ?? "Unknown"}
Test ID: {Guid.NewGuid()}";
        }

        private string GetDefaultRecipient()
        {
            // En una implementación real, esto vendría de configuración
            return "mechar09@gmail.com";
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _sesClient?.Dispose();
                // _templateManager?.Dispose();  // Comentado hasta que implementemos IDisposable
                // _bulkManager?.Dispose();      // Comentado hasta que implementemos IDisposable
                _disposed = true;
                _logger.LogDebug("SES Email Handler disposed");
            }
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Logger interface para desacoplar el logging
    /// Patrón Adapter para Unity Debug
    /// </summary>
    public interface ILogger
    {
        void LogDebug(string message);
        void LogError(string message);
        void LogWarning(string message);
    }

    /// <summary>
    /// Implementación del logger para Unity
    /// </summary>
    public class UnityLogger : ILogger
    {
        private readonly string _prefix;

        public UnityLogger(string prefix)
        {
            _prefix = prefix;
        }

        public void LogDebug(string message)
        {
            Debug.Log($"[{_prefix}] {message}");
        }

        public void LogError(string message)
        {
            Debug.LogError($"[{_prefix}] {message}");
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning($"[{_prefix}] {message}");
        }
    }

    #endregion
}