using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon;

namespace _Scripts.Models
{
    public class SESManager : MonoBehaviour
    {
        [Header("SES Configuration")]
        [SerializeField] private string senderEmail = "noreply@tudominio.com";
        [SerializeField] private string senderName = "Twin Nexus Platform";
        
        [Header("Test Configuration")]
        [SerializeField] private string testRecipientEmail = "";
        [SerializeField] private string testSubject = "Test Email from Unity";
        [SerializeField] private string testMessage = "¡Hola! Este es un email de prueba desde Unity usando AWS SES.";
        
        // SES client
        private AmazonSimpleEmailServiceClient sesClient;
        
        // Events for SES operations
        public event Action<bool, string, string> OnEmailSent; // success, message, messageId
        public event Action<bool, string, List<string>> OnVerificationStatusChecked; // success, message, verifiedEmails
        public event Action<bool, string> OnEmailVerificationSent; // success, message
        
        // Singleton instance
        public static SESManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initializes SES client with current AWS credentials
        /// </summary>
        private void InitializeSESClient()
        {
            try
            {
                if (OldCognitoManager.Instance == null || OldCognitoManager.Instance.CurrentAWSCredentials == null)
                {
                    Debug.LogError("No AWS credentials available. Please authenticate first.");
                    return;
                }

                var regionEndpoint = RegionEndpoint.USEast1; // Same region as other services
                sesClient = new AmazonSimpleEmailServiceClient(OldCognitoManager.Instance.CurrentAWSCredentials, regionEndpoint);
                
                Debug.Log("SES client initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize SES client: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a simple text email
        /// </summary>
        public async Task<bool> SendSimpleEmailAsync(string toEmail, string subject, string textBody, string htmlBody = null)
        {
            try
            {
                // Check if user is authenticated
                if (OldCognitoManager.Instance == null || !OldCognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to send emails");
                    OnEmailSent?.Invoke(false, "User not authenticated", null);
                    return false;
                }

                // Initialize SES client if needed
                if (sesClient == null)
                {
                    InitializeSESClient();
                    if (sesClient == null)
                    {
                        OnEmailSent?.Invoke(false, "Failed to initialize SES client", null);
                        return false;
                    }
                }

                Debug.Log($"📧 Sending email...");
                Debug.Log($"From: {senderName} <{senderEmail}>");
                Debug.Log($"To: {toEmail}");
                Debug.Log($"Subject: {subject}");

                // Create email body
                var body = new Body();
                
                if (!string.IsNullOrEmpty(textBody))
                {
                    body.Text = new Content
                    {
                        Charset = "UTF-8",
                        Data = textBody
                    };
                }

                if (!string.IsNullOrEmpty(htmlBody))
                {
                    body.Html = new Content
                    {
                        Charset = "UTF-8",
                        Data = htmlBody
                    };
                }

                // Create the email request
                var sendRequest = new SendEmailRequest
                {
                    Source = $"{senderName} <{senderEmail}>",
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { toEmail }
                    },
                    Message = new Message
                    {
                        Subject = new Content
                        {
                            Charset = "UTF-8",
                            Data = subject
                        },
                        Body = body
                    }
                };

                // Send the email
                var response = await sesClient.SendEmailAsync(sendRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.Log($"✅ Email sent successfully!");
                    Debug.Log($"📧 Message ID: {response.MessageId}");
                    OnEmailSent?.Invoke(true, "Email sent successfully", response.MessageId);
                    return true;
                }
                else
                {
                    Debug.LogError($"❌ Email sending failed with status: {response.HttpStatusCode}");
                    OnEmailSent?.Invoke(false, $"Email sending failed: {response.HttpStatusCode}", null);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"SES email sending error: {ex.Message}");
                OnEmailSent?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        /// <summary>
        /// Sends an email with user context (includes user info)
        /// </summary>
        public async Task<bool> SendEmailWithUserContextAsync(string toEmail, string subject, string message)
        {
            try
            {
                var userInfo = OldCognitoManager.Instance;
                var contextualMessage = $@"{message}

---
📊 User Context:
👤 Username: {userInfo?.CurrentUsername ?? "Unknown"}
🛡️ Role: {userInfo?.GetUserRole() ?? "Unknown"}
⏰ Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
🌐 Platform: Unity Game Engine
🏢 System: Twin Nexus Platform";

                return await SendSimpleEmailAsync(toEmail, subject, contextualMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending email with user context: {ex.Message}");
                OnEmailSent?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        /// <summary>
        /// Sends a system notification email to administrators
        /// </summary>
        public async Task<bool> SendSystemNotificationAsync(string subject, string message, NotificationType type = NotificationType.Info)
        {
            try
            {
                string emoji = type switch
                {
                    NotificationType.Success => "✅",
                    NotificationType.Warning => "⚠️",
                    NotificationType.Error => "❌",
                    NotificationType.Info => "ℹ️",
                    _ => "📢"
                };

                var formattedSubject = $"{emoji} {subject}";
                var systemMessage = $@"🤖 System Notification - Twin Nexus Platform

{message}

---
📊 System Information:
⏰ Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
🎮 Platform: Unity
☁️ Cloud: AWS
👤 Triggered by: {OldCognitoManager.Instance?.CurrentUsername ?? "System"}
🛡️ User Role: {OldCognitoManager.Instance?.GetUserRole() ?? "Unknown"}
📧 Notification Type: {type}";

                // In a real implementation, you'd have a list of admin emails
                // For now, using the configured test email
                return await SendSimpleEmailAsync(testRecipientEmail, formattedSubject, systemMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending system notification: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sends test email using configured parameters
        /// </summary>
        public async Task<bool> SendTestEmailAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(testRecipientEmail))
                {
                    Debug.LogWarning("No test recipient email configured in inspector");
                    OnEmailSent?.Invoke(false, "No test recipient email configured", null);
                    return false;
                }

                Debug.Log("📧 Sending test email...");

                var testMessageWithDetails = $@"{testMessage}

---
🧪 Test Details:
⏰ Sent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
👤 User: {OldCognitoManager.Instance?.CurrentUsername ?? "Unknown"}
🛡️ Role: {OldCognitoManager.Instance?.GetUserRole() ?? "Unknown"}
🆔 Test ID: {Guid.NewGuid()}";

                return await SendSimpleEmailAsync(testRecipientEmail, testSubject, testMessageWithDetails);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test email failed: {ex.Message}");
                OnEmailSent?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        /// <summary>
        /// Checks verification status of email addresses
        /// </summary>
        public async Task<List<string>> GetVerifiedEmailsAsync()
        {
            try
            {
                // Initialize SES client if needed
                if (sesClient == null)
                {
                    InitializeSESClient();
                    if (sesClient == null)
                    {
                        OnVerificationStatusChecked?.Invoke(false, "Failed to initialize SES client", new List<string>());
                        return new List<string>();
                    }
                }

                Debug.Log("🔍 Checking verified email addresses...");

                var request = new ListIdentitiesRequest();
                var response = await sesClient.ListIdentitiesAsync(request);

                var verifiedEmails = new List<string>();
                foreach (var identity in response.Identities)
                {
                    // Check if it's an email (not a domain)
                    if (identity.Contains("@"))
                    {
                        verifiedEmails.Add(identity);
                    }
                }

                Debug.Log($"📧 Found {verifiedEmails.Count} verified email addresses:");
                foreach (var email in verifiedEmails)
                {
                    Debug.Log($"   ✅ {email}");
                }

                OnVerificationStatusChecked?.Invoke(true, $"Found {verifiedEmails.Count} verified emails", verifiedEmails);
                return verifiedEmails;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error checking verified emails: {ex.Message}");
                OnVerificationStatusChecked?.Invoke(false, ex.Message, new List<string>());
                return new List<string>();
            }
        }

        /// <summary>
        /// Requests email verification for a new email address
        /// </summary>
        public async Task<bool> RequestEmailVerificationAsync(string emailToVerify)
        {
            try
            {
                // Initialize SES client if needed
                if (sesClient == null)
                {
                    InitializeSESClient();
                    if (sesClient == null)
                    {
                        OnEmailVerificationSent?.Invoke(false, "Failed to initialize SES client");
                        return false;
                    }
                }

                Debug.Log($"📧 Requesting verification for email: {emailToVerify}");

                var request = new VerifyEmailIdentityRequest
                {
                    EmailAddress = emailToVerify
                };

                var response = await sesClient.VerifyEmailIdentityAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.Log($"✅ Verification email sent to: {emailToVerify}");
                    Debug.Log("📬 Please check the email inbox and click the verification link");
                    OnEmailVerificationSent?.Invoke(true, "Verification email sent successfully");
                    return true;
                }
                else
                {
                    Debug.LogError($"❌ Failed to send verification email: {response.HttpStatusCode}");
                    OnEmailVerificationSent?.Invoke(false, $"Failed to send verification: {response.HttpStatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error requesting email verification: {ex.Message}");
                OnEmailVerificationSent?.Invoke(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gets user's role-based permissions for email operations
        /// </summary>
        public bool CanUserSendEmails()
        {
            if (OldCognitoManager.Instance == null || !OldCognitoManager.Instance.IsUserAuthenticated)
                return false;

            var userRole = OldCognitoManager.Instance.GetUserRole();

            // Define email permissions based on user roles
            return userRole switch
            {
                "super-admin" => true, // Super admin can send all types of emails
                "operators" => true,   // Operators can send operational emails
                "students" => true,    // Students can send basic emails
                "usuarios-basicos" => false, // Basic users cannot send emails (receive only)
                _ => false
            };
        }

        /// <summary>
        /// Gets maximum emails per day based on user role
        /// </summary>
        public int GetDailyEmailLimit()
        {
            if (OldCognitoManager.Instance == null || !OldCognitoManager.Instance.IsUserAuthenticated)
                return 0;

            var userRole = OldCognitoManager.Instance.GetUserRole();

            return userRole switch
            {
                "super-admin" => 1000,  // High limit for admin operations
                "operators" => 200,     // Moderate limit for operational needs
                "students" => 50,       // Lower limit for student activities
                "usuarios-basicos" => 0, // No sending capability
                _ => 0
            };
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        public bool IsValidEmail(string email)
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

        /// <summary>
        /// Test method to verify SES connectivity and permissions
        /// </summary>
        public async Task<bool> TestSESConnectivityAsync()
        {
            try
            {
                Debug.Log("🧪 Testing SES connectivity...");
                
                if (!CanUserSendEmails())
                {
                    Debug.LogWarning("⚠️ Current user role cannot send emails");
                    OnEmailSent?.Invoke(false, "User role cannot send emails", null);
                    return false;
                }

                // Get verified emails to ensure we can send
                var verifiedEmails = await GetVerifiedEmailsAsync();
                
                if (verifiedEmails.Count == 0)
                {
                    Debug.LogWarning("⚠️ No verified email addresses found. Please verify an email first.");
                    return false;
                }

                Debug.Log("✅ SES connectivity test completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"SES connectivity test failed: {ex.Message}");
                return false;
            }
        }

        public enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error
        }

        private void OnDestroy()
        {
            sesClient?.Dispose();
        }
    }
}