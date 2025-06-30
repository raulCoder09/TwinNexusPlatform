using Amazon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _Scripts.Models.CognitoManagement;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.Runtime;
using UnityEngine;
using Newtonsoft.Json;

namespace _Scripts.Models.SESManagement
{
    /// <summary>
    /// Maneja la creación y uso de templates de email en SES
    /// Permite emails más profesionales y reutilizables
    /// </summary>
    public class SESTemplateManager
    {
        private readonly AmazonSimpleEmailServiceClient _sesClient;
        private readonly SESInfo.EmailConfiguration _config;
        private readonly bool _enableDebugLogs = true;

        // Templates predefinidos comunes
        private readonly Dictionary<string, EmailTemplate> _predefinedTemplates = new()
        {
            ["welcome"] = new EmailTemplate
            {
                Name = "welcome-template",
                Subject = "Welcome to {{platformName}}!",
                HtmlBody = @"
                    <h2>Welcome {{username}}!</h2>
                    <p>Thank you for joining <strong>{{platformName}}</strong>.</p>
                    <p>Your account details:</p>
                    <ul>
                        <li>Username: {{username}}</li>
                        <li>Role: {{userRole}}</li>
                        <li>Registration Date: {{registrationDate}}</li>
                    </ul>
                    <p>Best regards,<br>The {{platformName}} Team</p>",
                TextBody = @"Welcome {{username}}!
                
Thank you for joining {{platformName}}.

Your account details:
- Username: {{username}}
- Role: {{userRole}}
- Registration Date: {{registrationDate}}

Best regards,
The {{platformName}} Team"
            },
            ["system-alert"] = new EmailTemplate
            {
                Name = "system-alert-template",
                Subject = "[{{alertType}}] {{subject}}",
                HtmlBody = @"
                    <div style='background-color: {{backgroundColor}}; padding: 20px; border-radius: 5px;'>
                        <h2 style='color: {{textColor}};'>{{alertType}} Alert</h2>
                        <h3>{{subject}}</h3>
                        <p>{{message}}</p>
                        <hr>
                        <small>
                            <strong>System Information:</strong><br>
                            Time: {{timestamp}}<br>
                            User: {{username}}<br>
                            Platform: {{platform}}
                        </small>
                    </div>",
                TextBody = @"{{alertType}} Alert: {{subject}}

{{message}}

---
System Information:
Time: {{timestamp}}
User: {{username}}
Platform: {{platform}}"
            },
            ["password-reset"] = new EmailTemplate
            {
                Name = "password-reset-template",
                Subject = "Password Reset Request - {{platformName}}",
                HtmlBody = @"
                    <h2>Password Reset Request</h2>
                    <p>Hello {{username}},</p>
                    <p>We received a request to reset your password. If you made this request, please use the following information:</p>
                    <div style='background-color: #f5f5f5; padding: 15px; margin: 10px 0;'>
                        <strong>Reset Code:</strong> {{resetCode}}<br>
                        <strong>Expires:</strong> {{expirationTime}}
                    </div>
                    <p>If you didn't request this reset, please ignore this email.</p>
                    <p>Best regards,<br>{{platformName}} Security Team</p>",
                TextBody = @"Password Reset Request

Hello {{username}},

We received a request to reset your password. If you made this request, please use the following information:

Reset Code: {{resetCode}}
Expires: {{expirationTime}}

If you didn't request this reset, please ignore this email.

Best regards,
{{platformName}} Security Team"
            }
        };

        public SESTemplateManager(SESInfo.EmailConfiguration config, AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sesClient = new AmazonSimpleEmailServiceClient(credentials, regionEndpoint);
        }

        /// <summary>
        /// Inicializa los templates predefinidos en SES
        /// </summary>
        public async Task<bool> InitializePredefinedTemplatesAsync()
        {
            try
            {
                LogDebug("Initializing predefined email templates...");
                
                foreach (var template in _predefinedTemplates.Values)
                {
                    await CreateOrUpdateTemplateAsync(template);
                }
                
                LogDebug("All predefined templates initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error initializing predefined templates: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Crea un nuevo template en SES
        /// </summary>
        public async Task<bool> CreateTemplateAsync(string templateName, string subject, string htmlBody, string textBody)
        {
            try
            {
                var template = new EmailTemplate
                {
                    Name = templateName,
                    Subject = subject,
                    HtmlBody = htmlBody,
                    TextBody = textBody
                };

                return await CreateOrUpdateTemplateAsync(template);
            }
            catch (Exception ex)
            {
                LogError($"Error creating template '{templateName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía un email usando un template con datos dinámicos
        /// </summary>
        public async Task<bool> SendTemplatedEmailAsync(string toEmail, string templateName, Dictionary<string, string> templateData)
        {
            try
            {
                if (!IsValidEmail(toEmail))
                {
                    LogError("Invalid recipient email address");
                    return false;
                }

                LogDebug($"Sending templated email using template '{templateName}' to {toEmail}");

                var request = new SendTemplatedEmailRequest
                {
                    Source = $"{_config.SenderName} <{_config.SenderEmail}>",
                    Destination = new Destination { ToAddresses = new List<string> { toEmail } },
                    Template = templateName,
                    TemplateData = JsonConvert.SerializeObject(templateData ?? new Dictionary<string, string>())
                };

                var response = await _sesClient.SendTemplatedEmailAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    LogDebug($"Templated email sent successfully. MessageId: {response.MessageId}");
                    return true;
                }
                else
                {
                    LogError($"Templated email sending failed with status: {response.HttpStatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error sending templated email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía email de bienvenida usando template predefinido
        /// </summary>
        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string username, string userRole)
        {
            var templateData = new Dictionary<string, string>
            {
                ["username"] = username,
                ["userRole"] = userRole,
                ["platformName"] = "Twin Nexus Platform",
                ["registrationDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
            };

            return await SendTemplatedEmailAsync(toEmail, "welcome-template", templateData);
        }

        /// <summary>
        /// Envía alerta del sistema usando template predefinido
        /// </summary>
        public async Task<bool> SendSystemAlertEmailAsync(string toEmail, string subject, string message, NotificationType alertType)
        {
            var (backgroundColor, textColor) = GetAlertColors(alertType);
            
            var templateData = new Dictionary<string, string>
            {
                ["alertType"] = alertType.ToString().ToUpper(),
                ["subject"] = subject,
                ["message"] = message,
                ["backgroundColor"] = backgroundColor,
                ["textColor"] = textColor,
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ["username"] = CognitoManager.Instance?.CurrentUsername ?? "System",
                ["platform"] = "Twin Nexus Platform"
            };

            return await SendTemplatedEmailAsync(toEmail, "system-alert-template", templateData);
        }

        /// <summary>
        /// Envía email de reset de contraseña
        /// </summary>
        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string username, string resetCode, DateTime expirationTime)
        {
            var templateData = new Dictionary<string, string>
            {
                ["username"] = username,
                ["resetCode"] = resetCode,
                ["expirationTime"] = expirationTime.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ["platformName"] = "Twin Nexus Platform"
            };

            return await SendTemplatedEmailAsync(toEmail, "password-reset-template", templateData);
        }

        /// <summary>
        /// Lista todos los templates disponibles
        /// </summary>
        public async Task<List<string>> GetAvailableTemplatesAsync()
        {
            try
            {
                var request = new ListTemplatesRequest();
                var response = await _sesClient.ListTemplatesAsync(request);
                
                var templateNames = response.TemplatesMetadata.Select(t => t.Name).ToList();
                LogDebug($"Found {templateNames.Count} available templates");
                
                return templateNames;
            }
            catch (Exception ex)
            {
                LogError($"Error listing templates: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Elimina un template
        /// </summary>
        public async Task<bool> DeleteTemplateAsync(string templateName)
        {
            try
            {
                var request = new DeleteTemplateRequest { TemplateName = templateName };
                var response = await _sesClient.DeleteTemplateAsync(request);
                
                LogDebug($"Template '{templateName}' deleted successfully");
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                LogError($"Error deleting template '{templateName}': {ex.Message}");
                return false;
            }
        }

        #region Private Methods

        private async Task<bool> CreateOrUpdateTemplateAsync(EmailTemplate template)
        {
            try
            {
                // Intentar crear el template
                var createRequest = new CreateTemplateRequest
                {
                    Template = new Template
                    {
                        TemplateName = template.Name,
                        SubjectPart = template.Subject,
                        HtmlPart = template.HtmlBody,
                        TextPart = template.TextBody
                    }
                };

                var response = await _sesClient.CreateTemplateAsync(createRequest);
                LogDebug($"Template '{template.Name}' created successfully");
                return true;
            }
            catch (Amazon.SimpleEmail.Model.AlreadyExistsException)
            {
                // Si ya existe, actualizarlo
                try
                {
                    var updateRequest = new UpdateTemplateRequest
                    {
                        Template = new Template
                        {
                            TemplateName = template.Name,
                            SubjectPart = template.Subject,
                            HtmlPart = template.HtmlBody,
                            TextPart = template.TextBody
                        }
                    };

                    await _sesClient.UpdateTemplateAsync(updateRequest);
                    LogDebug($"Template '{template.Name}' updated successfully");
                    return true;
                }
                catch (Exception updateEx)
                {
                    LogError($"Error updating template '{template.Name}': {updateEx.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error creating template '{template.Name}': {ex.Message}");
                return false;
            }
        }

        private (string backgroundColor, string textColor) GetAlertColors(NotificationType alertType)
        {
            return alertType switch
            {
                NotificationType.Success => ("#d4edda", "#155724"),
                NotificationType.Warning => ("#fff3cd", "#856404"),
                NotificationType.Error => ("#f8d7da", "#721c24"),
                NotificationType.Info => ("#d1ecf1", "#0c5460"),
                _ => ("#e2e3e5", "#383d41")
            };
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

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[SESTemplateManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[SESTemplateManager] {message}");
        }

        #endregion

        #region Helper Classes

        public class EmailTemplate
        {
            public string Name { get; set; }
            public string Subject { get; set; }
            public string HtmlBody { get; set; }
            public string TextBody { get; set; }
        }

        #endregion
    }
}