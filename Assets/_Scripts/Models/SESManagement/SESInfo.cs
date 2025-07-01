using Amazon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace _Scripts.Models.SESManagement
{
    /// <summary>
    /// Clases de configuración y datos para AWS SES
    /// Implementa patrón Value Object para datos inmutables
    /// </summary>
    [System.Serializable]
    public static class SESInfo
    {
        /// <summary>
        /// Configuración de email para SES
        /// Patrón Builder para construcción flexible
        /// </summary>
        [System.Serializable]
        public class EmailConfiguration
        {
            public string SenderEmail { get; private set; }
            public string SenderName { get; private set; }
            public RegionEndpoint Region { get; private set; }

            // Constructor privado para forzar uso del Builder
            private EmailConfiguration() { }

            public class Builder
            {
                private readonly EmailConfiguration _config = new();

                public Builder WithSenderEmail(string senderEmail)
                {
                    if (string.IsNullOrWhiteSpace(senderEmail))
                        throw new ArgumentException("Sender email cannot be null or empty", nameof(senderEmail));
                    
                    if (!IsValidEmail(senderEmail))
                        throw new ArgumentException("Invalid email format", nameof(senderEmail));
                    
                    _config.SenderEmail = senderEmail;
                    return this;
                }

                public Builder WithSenderName(string senderName)
                {
                    if (string.IsNullOrWhiteSpace(senderName))
                        throw new ArgumentException("Sender name cannot be null or empty", nameof(senderName));
                    
                    _config.SenderName = senderName;
                    return this;
                }

                public Builder WithRegion(RegionEndpoint region)
                {
                    _config.Region = region ?? throw new ArgumentNullException(nameof(region));
                    return this;
                }

                public EmailConfiguration Build()
                {
                    if (string.IsNullOrEmpty(_config.SenderEmail))
                        throw new InvalidOperationException("Sender email is required");
                    
                    if (string.IsNullOrEmpty(_config.SenderName))
                        throw new InvalidOperationException("Sender name is required");
                    
                    if (_config.Region == null)
                        throw new InvalidOperationException("Region is required");

                    return _config;
                }

                private static bool IsValidEmail(string email)
                {
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
            }

            // Factory method para crear builder
            public static Builder Create() => new Builder();

            // Override para debugging
            public override string ToString()
            {
                return $"SES Config: {SenderName} <{SenderEmail}> in {Region?.SystemName}";
            }
        }

        /// <summary>
        /// Datos de un email individual
        /// Patrón Value Object - inmutable después de creación
        /// </summary>
        [System.Serializable]
        public class EmailData
        {
            public string ToEmail { get; }
            public string Subject { get; }
            public string TextBody { get; }
            public string HtmlBody { get; }
            public string MessageId { get; }
            public DateTime SentAt { get; }
            public EmailStatus Status { get; private set; }

            public EmailData(string toEmail, string subject, string textBody, string htmlBody = null, string messageId = null)
            {
                ToEmail = toEmail ?? throw new ArgumentNullException(nameof(toEmail));
                Subject = subject ?? throw new ArgumentNullException(nameof(subject));
                TextBody = textBody ?? throw new ArgumentNullException(nameof(textBody));
                HtmlBody = htmlBody;
                MessageId = messageId;
                SentAt = DateTime.UtcNow;
                Status = EmailStatus.Pending;
            }

            // Método para actualizar estado (única propiedad mutable)
            public void UpdateStatus(EmailStatus newStatus)
            {
                Status = newStatus;
            }

            public bool HasHtmlContent => !string.IsNullOrEmpty(HtmlBody);
            public bool IsSent => Status == EmailStatus.Sent;
            public bool HasFailed => Status == EmailStatus.Failed;

            public override string ToString()
            {
                return $"Email to {ToEmail}: '{Subject}' [{Status}]";
            }
        }

        /// <summary>
        /// Estado de verificación de emails
        /// Patrón Value Object con validación
        /// </summary>
        [System.Serializable]
        public class VerificationStatus
        {
            public IReadOnlyList<string> VerifiedEmails { get; }
            public bool IsVerified { get; }
            public DateTime LastChecked { get; }

            public VerificationStatus(List<string> verifiedEmails)
            {
                VerifiedEmails = verifiedEmails?.AsReadOnly() ?? new List<string>().AsReadOnly();
                IsVerified = VerifiedEmails.Count > 0;
                LastChecked = DateTime.UtcNow;
            }

            public bool IsEmailVerified(string email)
            {
                return !string.IsNullOrEmpty(email) && 
                       VerifiedEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
            }

            public int VerifiedCount => VerifiedEmails.Count;

            public override string ToString()
            {
                return $"Verification Status: {VerifiedCount} verified emails (checked at {LastChecked:yyyy-MM-dd HH:mm})";
            }
        }

        /// <summary>
        /// Permisos de usuario para operaciones de email
        /// Patrón Strategy para diferentes roles
        /// </summary>
        [System.Serializable]
        public class UserPermissions
        {
            public bool CanSendEmails { get; }
            public bool CanSendBulkEmails { get; }
            public int DailyEmailLimit { get; }
            public int BulkEmailLimit { get; }
            public UserRole Role { get; }

            public UserPermissions(UserRole role)
            {
                Role = role;
                (CanSendEmails, CanSendBulkEmails, DailyEmailLimit, BulkEmailLimit) = GetPermissionsForRole(role);
            }

            private static (bool canSend, bool canBulk, int daily, int bulk) GetPermissionsForRole(UserRole role)
            {
                return role switch
                {
                    UserRole.SuperAdmin => (true, true, 1000, 500),
                    UserRole.Operator => (true, true, 200, 100),
                    UserRole.Student => (true, false, 50, 0),
                    UserRole.BasicUser => (false, false, 0, 0),
                    _ => (false, false, 0, 0)
                };
            }

            public bool CanSendToRecipientCount(int recipientCount)
            {
                if (recipientCount <= 1)
                    return CanSendEmails;
                
                return CanSendBulkEmails && recipientCount <= BulkEmailLimit;
            }

            public override string ToString()
            {
                return $"Permissions [{Role}]: Send={CanSendEmails}, Bulk={CanSendBulkEmails}, Limit={DailyEmailLimit}/day";
            }
        }

        /// <summary>
        /// Roles de usuario del sistema
        /// Enum con valores claros y extensible
        /// </summary>
        public enum UserRole
        {
            SuperAdmin,
            Operator, 
            Student,
            BasicUser,
            Unknown
        }

        /// <summary>
        /// Estados posibles de un email
        /// Enum que representa el ciclo de vida del email
        /// </summary>
        public enum EmailStatus
        {
            Pending,
            Sent,
            Delivered,
            Failed,
            Bounced,
            Complained
        }

        /// <summary>
        /// Configuraciones predefinidas comunes
        /// Patrón Factory para configuraciones típicas
        /// </summary>
        public static class Presets
        {
            public static EmailConfiguration Development()
            {
                return EmailConfiguration.Create()
                    .WithSenderEmail("dev@twinnexus.local")
                    .WithSenderName("Twin Nexus Development")
                    .WithRegion(RegionEndpoint.USEast1)
                    .Build();
            }

            public static EmailConfiguration Production()
            {
                return EmailConfiguration.Create()
                    .WithSenderEmail("noreply@twinnexus.com")
                    .WithSenderName("Twin Nexus Platform")
                    .WithRegion(RegionEndpoint.USEast1)
                    .Build();
            }

            public static EmailConfiguration Testing()
            {
                return EmailConfiguration.Create()
                    .WithSenderEmail("test@twinnexus.local")
                    .WithSenderName("Twin Nexus Testing")
                    .WithRegion(RegionEndpoint.USEast1)
                    .Build();
            }
        }

        /// <summary>
        /// Utilidades de validación comunes
        /// Patrón Static Helper
        /// </summary>
        public static class Validation
        {
            public static bool IsValidEmail(string email)
            {
                if (string.IsNullOrWhiteSpace(email))
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

            public static bool IsValidSubject(string subject)
            {
                return !string.IsNullOrWhiteSpace(subject) && subject.Length <= 255;
            }

            public static bool IsValidBody(string body)
            {
                return !string.IsNullOrWhiteSpace(body) && body.Length <= 32768; // 32KB limit
            }

            public static UserRole ParseUserRole(string roleString)
            {
                return roleString?.ToLowerInvariant() switch
                {
                    "super-admin" => UserRole.SuperAdmin,
                    "operators" => UserRole.Operator,
                    "students" => UserRole.Student,
                    "usuarios-basicos" => UserRole.BasicUser,
                    _ => UserRole.Unknown
                };
            }
        }
    }
}