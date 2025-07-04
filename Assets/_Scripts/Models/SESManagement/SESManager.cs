using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using UnityEngine;
using _Scripts.Models.CognitoManagement;

namespace _Scripts.Models.SESManagement
{
    /// <summary>
    /// Orquestador principal del sistema SES para producción
    /// Implementa patrón Facade + Singleton para centralizar operaciones de email
    /// Responsabilidad: Coordinar todos los servicios de email y exponer API unificada
    /// </summary>
    public class SESManager : MonoBehaviour, ISESAdvancedOps
    {
        #region Singleton Pattern
        
        private static SESManager _instance;
        private static readonly object _lock = new object();

        public static SESManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject("SESManager");
                            _instance = go.AddComponent<SESManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Configuration Fields

        [Header("SES Configuration")]
        [SerializeField] internal string _senderEmail = "mechar09@gmail.com";
        [SerializeField] internal string _senderName = "Twin Nexus Platform";
        [SerializeField] internal string _platformName = "Twin Nexus Platform"; 
        [SerializeField] internal string _defaultRecipient = "mechar09@gmail.com";
        [SerializeField] internal RegionEndpoint _region = RegionEndpoint.USEast1;
        
        [Header("Production Settings")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _autoInitializeTemplates = true;
        [SerializeField] private int _maxRetryAttempts = 3;
        [SerializeField] private float _retryDelaySeconds = 2.0f;

        #endregion

        #region Private Fields

        private SESEmailHandler _emailHandler;
        private SESInfo.EmailConfiguration _configuration;
        private ILogger _logger;
        private bool _isInitialized = false;
        private bool _isInitializing = false;

        #endregion

        #region Events

        /// <summary>
        /// Eventos para notificar resultados de operaciones
        /// Patrón Observer para comunicación desacoplada
        /// </summary>
        public event Action<SESOperationResult> OnEmailOperationCompleted;
        public event Action<bool, string> OnInitializationCompleted;
        public event Action<SESQuotaInfo> OnQuotaUpdated;
        public event Action<string, string> OnEmailVerificationRequested;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton enforcement
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            _logger = new UnityLogger("SESManager");
            _logger.LogDebug("SES Manager awakened");
        }

        private void Start()
        {
            // NO auto-inicializar - esperar a que ServiceController nos active tras autenticación
            print("SES Manager started - waiting for ServiceController activation");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _emailHandler?.Dispose();
                _logger?.LogDebug("SES Manager destroyed and resources cleaned up");
                _instance = null;
            }
        }

        #endregion

        #region Initialization
        /// <summary>
        /// Verifica si SESManager puede inicializarse (tiene credenciales AWS)
        /// </summary>
        public bool CanInitialize()
        {
            var cognitoManager = CognitoManager.Instance;
            return cognitoManager != null && 
                   cognitoManager.IsUserAuthenticated && 
                   cognitoManager.CurrentAWSCredentials != null;
        }

        /// <summary>
        /// Inicialización asíncrona del sistema SES
        /// Patrón Async Initialization para Unity
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized || _isInitializing)
            {
                return _isInitialized;
            }

            _isInitializing = true;

            try
            {
                _logger.LogDebug("Initializing SES Manager...");

                // Validar configuración
                if (!ValidateConfiguration())
                {
                    throw new InvalidOperationException("Invalid SES configuration");
                }

                // Esperar a que Cognito esté listo
                await WaitForCognitoInitialization();

                // Crear configuración
                _configuration = CreateConfiguration();

                // Obtener credenciales
                var credentials = GetAWSCredentials();
                if (credentials == null)
                {
                    throw new InvalidOperationException("AWS credentials not available");
                }

                // Inicializar handler principal
                _emailHandler = new SESEmailHandler(_configuration, credentials, _region);

                // Inicializar templates si está habilitado
                if (_autoInitializeTemplates)
                {
                    var templatesInitialized = await _emailHandler.InitializeTemplatesAsync();
                    _logger.LogDebug($"Templates initialization: {(templatesInitialized ? "Success" : "Failed")}");
                }

                _isInitialized = true;
                _logger.LogDebug("SES Manager initialized successfully");
                
                OnInitializationCompleted?.Invoke(true, "SES Manager initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize SES Manager: {ex.Message}");
                OnInitializationCompleted?.Invoke(false, $"Initialization failed: {ex.Message}");
                return false;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        /// <summary>
        /// Reinicializa el manager con nueva configuración
        /// </summary>
        public async Task<bool> ReinitializeAsync(string newSenderEmail, string newSenderName)
        {
            _logger.LogDebug("Reinitializing SES Manager with new configuration...");
            
            _senderEmail = newSenderEmail;
            _senderName = newSenderName;
            
            _isInitialized = false;
            _emailHandler?.Dispose();
            _emailHandler = null;
            
            return await InitializeAsync();
        }

        #endregion

        #region ISESOps Implementation

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string textBody, string htmlBody = null)
        {
            var result = await ExecuteWithRetry(async () =>
            {
                EnsureInitialized();
                return await _emailHandler.SendEmailAsync(toEmail, subject, textBody, htmlBody);
            });

            OnEmailOperationCompleted?.Invoke(new SESOperationResult
            {
                Success = result,
                Message = result ? "Email sent successfully" : "Email sending failed",
                TotalProcessed = 1,
                SuccessCount = result ? 1 : 0,
                FailureCount = result ? 0 : 1
            });

            return result;
        }

        public async Task<bool> SendEmailWithUserContextAsync(string toEmail, string subject, string message)
        {
            var result = await ExecuteWithRetry(async () =>
            {
                EnsureInitialized();
                return await _emailHandler.SendEmailWithUserContextAsync(toEmail, subject, message);
            });

            OnEmailOperationCompleted?.Invoke(CreateResultFromBool(result, "Email with context"));
            return result;
        }

        public async Task<bool> SendSystemNotificationAsync(string subject, string message, NotificationType type = NotificationType.Info)
        {
            var result = await ExecuteWithRetry(async () =>
            {
                EnsureInitialized();
                return await _emailHandler.SendSystemNotificationAsync(subject, message, type);
            });

            OnEmailOperationCompleted?.Invoke(CreateResultFromBool(result, "System notification"));
            return result;
        }

        public async Task<bool> SendTestEmailAsync()
        {
            var result = await ExecuteWithRetry(async () =>
            {
                EnsureInitialized();
                return await _emailHandler.SendTestEmailAsync();
            });

            OnEmailOperationCompleted?.Invoke(CreateResultFromBool(result, "Test email"));
            return result;
        }

        public async Task<List<string>> GetVerifiedEmailsAsync()
        {
            return await ExecuteWithRetry(async () =>
            {
                EnsureInitialized();
                return await _emailHandler.GetVerifiedEmailsAsync();
            });
        }

        public async Task<bool> RequestEmailVerificationAsync(string emailToVerify)
        {
            var result = await ExecuteWithRetry(async () =>
            {
                EnsureInitialized();
                return await _emailHandler.RequestEmailVerificationAsync(emailToVerify);
            });

            OnEmailVerificationRequested?.Invoke(emailToVerify, result ? "Success" : "Failed");
            return result;
        }

        public bool CanUserSendEmails()
        {
            if (!_isInitialized) return false;
            return _emailHandler.CanUserSendEmails();
        }

        public int GetDailyEmailLimit()
        {
            if (!_isInitialized) return 0;
            return _emailHandler.GetDailyEmailLimit();
        }

        #endregion

        #region ISESAdvancedOps Implementation

        public async Task<bool> SendTemplatedEmailAsync(string toEmail, string templateName, Dictionary<string, string> templateData)
        {
            var result = await ExecuteWithRetry(async () =>
            {
                EnsureInitialized();
                return await _emailHandler.SendTemplatedEmailAsync(toEmail, templateName, templateData);
            });

            OnEmailOperationCompleted?.Invoke(CreateResultFromBool(result, $"Templated email ({templateName})"));
            return result;
        }

        public async Task<bool> InitializeTemplatesAsync()
        {
            EnsureInitialized();
            return await _emailHandler.InitializeTemplatesAsync();
        }

        public async Task<SESOperationResult> SendBulkEmailAsync(List<string> recipients, string subject, string textBody, string htmlBody = null)
        {
            var result = await ExecuteWithRetry(async () =>
            {
                EnsureInitialized();
                return await _emailHandler.SendBulkEmailAsync(recipients, subject, textBody, htmlBody);
            });

            OnEmailOperationCompleted?.Invoke(result);
            return result;
        }

        public void TrackEmailSent(string messageId, string recipient, string subject)
        {
            if (_isInitialized)
            {
                _emailHandler.TrackEmailSent(messageId, recipient, subject);
            }
        }

        public async Task<SESQuotaInfo> GetSendQuotaAsync()
        {
            var quota = await ExecuteWithRetry(async () =>
            {
                EnsureInitialized();
                return await _emailHandler.GetSendQuotaAsync();
            });

            OnQuotaUpdated?.Invoke(quota);
            return quota;
        }

        #endregion

        #region Production Helper Methods

        /// <summary>
        /// Envía email de bienvenida para nuevos usuarios
        /// API de alto nivel para casos de uso comunes
        /// </summary>
        public async Task<bool> SendWelcomeEmailAsync(string userEmail, string username)
        {
            try
            {
                var templateData = new Dictionary<string, string>
                {
                    ["username"] = username,
                    ["userRole"] = CognitoManager.Instance?.GetUserRole() ?? "Unknown",
                    ["platformName"] = "Twin Nexus Platform",
                    ["registrationDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                };

                return await SendTemplatedEmailAsync(userEmail, "welcome-template", templateData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending welcome email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Envía notificación crítica del sistema
        /// Método de conveniencia para alertas importantes
        /// </summary>
        public async Task<bool> SendCriticalAlertAsync(string subject, string message)
        {
            return await SendSystemNotificationAsync($"CRITICAL: {subject}", message, NotificationType.Error);
        }

        /// <summary>
        /// Verifica el estado de salud del sistema SES
        /// Útil para monitoring y health checks
        /// </summary>
        public async Task<SESHealthStatus> GetHealthStatusAsync()
        {
            try
            {
                EnsureInitialized();
                
                var quota = await GetSendQuotaAsync();
                var verifiedEmails = await GetVerifiedEmailsAsync();
                
                return new SESHealthStatus
                {
                    IsHealthy = true,
                    QuotaUsagePercentage = quota.UsagePercentage,
                    VerifiedEmailCount = verifiedEmails.Count,
                    CanSendEmails = CanUserSendEmails(),
                    DailyLimit = GetDailyEmailLimit(),
                    LastChecked = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Health check failed: {ex.Message}");
                return new SESHealthStatus
                {
                    IsHealthy = false,
                    ErrorMessage = ex.Message,
                    LastChecked = DateTime.UtcNow
                };
            }
        }

        #endregion

        #region Private Helper Methods

        private bool ValidateConfiguration()
        {
            if (!SESInfo.Validation.IsValidEmail(_senderEmail))
            {
                _logger.LogError("Invalid sender email configuration");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_senderName))
            {
                _logger.LogError("Sender name cannot be empty");
                return false;
            }

            return true;
        }

        private async Task WaitForCognitoInitialization()
        {
            const int maxWaitSeconds = 30;
            const float checkInterval = 0.5f;
            float elapsed = 0;

            while (elapsed < maxWaitSeconds)
            {
                if (CognitoManager.Instance != null && CognitoManager.Instance.IsUserAuthenticated)
                {
                    return;
                }

                await Task.Delay((int)(checkInterval * 1000));
                elapsed += checkInterval;
            }

            throw new TimeoutException("Cognito initialization timeout");
        }

        private SESInfo.EmailConfiguration CreateConfiguration()
        {
            return SESInfo.EmailConfiguration.Create()
                .WithSenderEmail(_senderEmail)
                .WithSenderName(_senderName)
                .WithRegion(_region)
                .Build();
        }

        private AWSCredentials GetAWSCredentials()
        {
            var cognitoManager = CognitoManager.Instance;
            if (cognitoManager == null || !cognitoManager.IsUserAuthenticated)
            {
                _logger.LogError("User not authenticated - cannot get AWS credentials");
                return null;
            }

            // CAMBIO: Usar credenciales reales de CognitoManager
            var credentials = cognitoManager.CurrentAWSCredentials;
            if (credentials != null)
            {
                _logger.LogDebug("AWS credentials obtained from CognitoManager");
                return credentials;
            }
    
            _logger.LogError("CognitoManager has no AWS credentials available");
            return null;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("SES Manager not initialized. Call InitializeAsync() first.");
            }
        }

        private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation)
        {
            for (int attempt = 1; attempt <= _maxRetryAttempts; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    if (attempt == _maxRetryAttempts)
                    {
                        _logger.LogError($"Operation failed after {_maxRetryAttempts} attempts: {ex.Message}");
                        throw;
                    }

                    _logger.LogWarning($"Operation attempt {attempt} failed, retrying: {ex.Message}");
                    await Task.Delay((int)(_retryDelaySeconds * 1000 * attempt)); // Exponential backoff
                }
            }

            return default(T);
        }

        private SESOperationResult CreateResultFromBool(bool success, string operation)
        {
            return new SESOperationResult
            {
                Success = success,
                Message = $"{operation}: {(success ? "Success" : "Failed")}",
                TotalProcessed = 1,
                SuccessCount = success ? 1 : 0,
                FailureCount = success ? 0 : 1
            };
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Estado de salud del sistema SES
    /// </summary>
    public class SESHealthStatus
    {
        public bool IsHealthy { get; set; }
        public double QuotaUsagePercentage { get; set; }
        public int VerifiedEmailCount { get; set; }
        public bool CanSendEmails { get; set; }
        public int DailyLimit { get; set; }
        public DateTime LastChecked { get; set; }
        public string ErrorMessage { get; set; }

        public string GetStatusSummary()
        {
            if (!IsHealthy)
                return $"UNHEALTHY: {ErrorMessage}";

            var status = QuotaUsagePercentage > 90 ? "WARNING" : "HEALTHY";
            return $"{status}: {VerifiedEmailCount} verified emails, {QuotaUsagePercentage:F1}% quota used";
        }
    }

    /// <summary>
    /// Extensiones para SESOperationResult
    /// </summary>
    public static class SESOperationResultExtensions
    {
        // Removido - ahora es método privado en SESManager
    }

    #endregion
}