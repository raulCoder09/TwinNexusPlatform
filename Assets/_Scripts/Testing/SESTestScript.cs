using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using _Scripts.Models.SESManagement;
using _Scripts.Controller;
using _Scripts.Controllers.ServiceManagement;

namespace _Scripts.Testing
{
    /// <summary>
    /// Script de pruebas simplificado para SES usando la nueva arquitectura
    /// Solo necesita referenciar SESManager.Instance - sin configuraciones complejas
    /// </summary>
    public class SESTestScript : MonoBehaviour
    {
        [Header("Input Actions - Control por Teclado")] [SerializeField]
        private KeyCode testKeyTestEmail = KeyCode.T;

        [SerializeField] private KeyCode testKeyWelcomeEmail = KeyCode.W;
        [SerializeField] private KeyCode testKeyBulkEmail = KeyCode.B;
        [SerializeField] private KeyCode testKeyCriticalAlert = KeyCode.C;
        [SerializeField] private KeyCode testKeyHealth = KeyCode.H;
        [SerializeField] private KeyCode testKeyQuota = KeyCode.Q;
        [SerializeField] private KeyCode testKeyCustomEmail = KeyCode.M;
        [SerializeField] private KeyCode testKeyPermissions = KeyCode.P;
        [SerializeField] private KeyCode testKeyReconfigure = KeyCode.R;

        // Input Actions
        private InputAction actionTestEmail;
        private InputAction actionWelcomeEmail;
        private InputAction actionBulkEmail;
        private InputAction actionCriticalAlert;
        private InputAction actionHealth;
        private InputAction actionQuota;
        private InputAction actionCustomEmail;
        private InputAction actionPermissions;
        private InputAction actionReconfigure;

        [Header("UI References")] [SerializeField]
        private Button _sendTestEmailButton;

        [SerializeField] private Button _sendWelcomeEmailButton;
        [SerializeField] private Button _sendBulkEmailButton;
        [SerializeField] private Button _sendCriticalAlertButton;
        [SerializeField] private Button _checkHealthButton;
        [SerializeField] private Button _getQuotaButton;

        [Header("Test Configuration - USAR EMAILS VERIFICADOS")] [SerializeField]
        private string _testEmail = "mechar09@gmail.com"; // ✅ Email verificado

        [SerializeField] private List<string> _bulkTestEmails = new List<string>
        {
            "mechar09@gmail.com", // ✅ Email verificado
            "mechar09@outlook.com", // ✅ Email verificado
            "mechar09@gmail.com" // ✅ Repetir para prueba masiva
        };

        [Header("Modo Básico (sin permisos avanzados)")] [SerializeField]
        private bool _useBasicMode = true; // ✅ Evitar funciones que requieren permisos avanzados

        [Header("Status Display")] [SerializeField]
        private Text _statusText;

        [SerializeField] private Text _quotaText;
        [SerializeField] private Text _healthText;

        private void Start()
        {
            // Configurar Input Actions
            SetupInputActions();

            // Configurar botones UI
            SetupButtons();

            // Mostrar estado inicial
            UpdateStatusDisplay();

            // Suscribirse a eventos de ServiceController para monitorear estado
            if (ServiceController.Instance != null)
            {
                ServiceController.Instance.OnServiceActivated += OnServiceActivated;
                ServiceController.Instance.OnServiceError += OnServiceError;
            }

            // Mostrar controles en consola
            ShowControlsHelp();
        }

        private void SetupInputActions()
        {
            // Crear Input Actions siguiendo tu patrón
            actionTestEmail = new InputAction("testEmail", InputActionType.Button, $"<Keyboard>/{testKeyTestEmail}");
            actionWelcomeEmail =
                new InputAction("welcomeEmail", InputActionType.Button, $"<Keyboard>/{testKeyWelcomeEmail}");
            actionBulkEmail = new InputAction("bulkEmail", InputActionType.Button, $"<Keyboard>/{testKeyBulkEmail}");
            actionCriticalAlert = new InputAction("criticalAlert", InputActionType.Button,
                $"<Keyboard>/{testKeyCriticalAlert}");
            actionHealth = new InputAction("health", InputActionType.Button, $"<Keyboard>/{testKeyHealth}");
            actionQuota = new InputAction("quota", InputActionType.Button, $"<Keyboard>/{testKeyQuota}");
            actionCustomEmail =
                new InputAction("customEmail", InputActionType.Button, $"<Keyboard>/{testKeyCustomEmail}");
            actionPermissions =
                new InputAction("permissions", InputActionType.Button, $"<Keyboard>/{testKeyPermissions}");
            actionReconfigure =
                new InputAction("reconfigure", InputActionType.Button, $"<Keyboard>/{testKeyReconfigure}");

            // Suscribirse a eventos
            actionTestEmail.performed += _ => SendTestEmail();
            actionWelcomeEmail.performed += _ => SendWelcomeEmail();
            actionBulkEmail.performed += _ => SendBulkEmail();
            actionCriticalAlert.performed += _ => SendCriticalAlert();
            actionHealth.performed += _ => CheckHealth();
            actionQuota.performed += _ => GetQuota();
            actionCustomEmail.performed += _ => SendCustomEmailPrompt();
            actionPermissions.performed += _ => CheckUserPermissions();
            actionReconfigure.performed += _ => ReconfigureSESWithVerifiedEmails();

            // Habilitar todas las acciones
            actionTestEmail.Enable();
            actionWelcomeEmail.Enable();
            actionBulkEmail.Enable();
            actionCriticalAlert.Enable();
            actionHealth.Enable();
            actionQuota.Enable();
            actionCustomEmail.Enable();
            actionPermissions.Enable();
            actionReconfigure.Enable();
        }

        private void ShowControlsHelp()
        {
            var controlsInfo = "🎮 SES Test Controls:\n" +
                               $"[{testKeyTestEmail}] - Enviar Email de Prueba\n" +
                               $"[{testKeyWelcomeEmail}] - Enviar Email de Bienvenida\n" +
                               $"[{testKeyBulkEmail}] - Enviar Emails Masivos\n" +
                               $"[{testKeyCriticalAlert}] - Enviar Alerta Crítica\n" +
                               $"[{testKeyHealth}] - Verificar Estado de Salud\n" +
                               $"[{testKeyQuota}] - Obtener Información de Cuota\n" +
                               $"[{testKeyCustomEmail}] - Enviar Email Personalizado\n" +
                               $"[{testKeyPermissions}] - Verificar Permisos\n" +
                               $"[{testKeyReconfigure}] - Reconfigurar SES con Emails Verificados";

            Debug.Log($"[SESTest] {controlsInfo}");
        }

        private void SetupButtons()
        {
            if (_sendTestEmailButton != null)
                _sendTestEmailButton.onClick.AddListener(SendTestEmail);

            if (_sendWelcomeEmailButton != null)
                _sendWelcomeEmailButton.onClick.AddListener(SendWelcomeEmail);

            if (_sendBulkEmailButton != null)
                _sendBulkEmailButton.onClick.AddListener(SendBulkEmail);

            if (_sendCriticalAlertButton != null)
                _sendCriticalAlertButton.onClick.AddListener(SendCriticalAlert);

            if (_checkHealthButton != null)
                _checkHealthButton.onClick.AddListener(CheckHealth);

            if (_getQuotaButton != null)
                _getQuotaButton.onClick.AddListener(GetQuota);
        }

        #region Test Methods - Súper Simples

        /// <summary>
        /// Prueba básica - envío de email simple
        /// </summary>
        public async void SendTestEmail()
        {
            LogTest("Enviando email de prueba...");

            try
            {
                // ¡Así de simple! Solo usar la instancia
                var success = await SESManager.Instance.SendTestEmailAsync();

                if (success)
                {
                    LogTest("✅ Email de prueba enviado exitosamente");
                }
                else
                {
                    LogTest("❌ Error al enviar email de prueba");
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción: {ex.Message}");
            }
        }

        /// <summary>
        /// Prueba de email de bienvenida - MODO BÁSICO SIN TEMPLATES
        /// </summary>
        public async void SendWelcomeEmail()
        {
            LogTest("Enviando email de bienvenida (modo básico)...");

            try
            {
                if (_useBasicMode)
                {
                    // Usar envío simple sin templates
                    var subject = "Bienvenido a Twin Nexus Platform";
                    var message = $@"¡Hola Usuario Prueba!

Bienvenido a Twin Nexus Platform.

Detalles de tu cuenta:
- Usuario: Usuario Prueba
- Fecha de registro: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}
- Plataforma: Unity

Saludos,
El equipo de Twin Nexus";

                    var success = await SESManager.Instance.SendEmailAsync(_testEmail, subject, message);

                    if (success)
                    {
                        LogTest($"✅ Email de bienvenida básico enviado a {_testEmail}");
                    }
                    else
                    {
                        LogTest("❌ Error al enviar email de bienvenida básico");
                    }
                }
                else
                {
                    // Usar el método con templates (requiere permisos avanzados)
                    var success = await SESManager.Instance.SendWelcomeEmailAsync(_testEmail, "Usuario Prueba");

                    if (success)
                    {
                        LogTest($"✅ Email de bienvenida con template enviado a {_testEmail}");
                    }
                    else
                    {
                        LogTest("❌ Error al enviar email de bienvenida con template");
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción: {ex.Message}");
                if (ex.Message.Contains("not authorized"))
                {
                    LogTest("💡 Activando modo básico - sin templates");
                    _useBasicMode = true;
                }
            }
        }

        /// <summary>
        /// Prueba de envío masivo - MODO BÁSICO SIN BULK API
        /// </summary>
        public async void SendBulkEmail()
        {
            LogTest("Enviando emails masivos...");

            try
            {
                if (_useBasicMode)
                {
                    // Modo básico: enviar uno por uno
                    LogTest("Usando modo básico - enviando emails individualmente...");

                    int successCount = 0;
                    int failCount = 0;

                    foreach (var email in _bulkTestEmails)
                    {
                        var success = await SESManager.Instance.SendEmailAsync(
                            email,
                            "Email Individual de Prueba",
                            $"Email de prueba individual enviado a {email} el {System.DateTime.Now:HH:mm:ss}"
                        );

                        if (success)
                        {
                            successCount++;
                            LogTest($"  ✅ Email enviado a {email}");
                        }
                        else
                        {
                            failCount++;
                            LogTest($"  ❌ Falló email a {email}");
                        }

                        // Pequeño delay entre envíos para evitar rate limiting
                        await System.Threading.Tasks.Task.Delay(500);
                    }

                    LogTest($"✅ Envío individual completado - Éxito: {successCount}, Fallos: {failCount}");
                }
                else
                {
                    // Modo avanzado: usar bulk API
                    var result = await SESManager.Instance.SendBulkEmailAsync(
                        _bulkTestEmails,
                        "Email Masivo de Prueba",
                        "Este es un mensaje de prueba masivo desde Unity."
                    );

                    if (result.Success)
                    {
                        LogTest(
                            $"✅ Emails masivos enviados - Éxito: {result.SuccessCount}, Fallos: {result.FailureCount}");
                    }
                    else
                    {
                        LogTest($"❌ Error en envío masivo: {result.Message}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción: {ex.Message}");
                if (ex.Message.Contains("not authorized"))
                {
                    LogTest("💡 Activando modo básico - sin bulk API");
                    _useBasicMode = true;
                }
            }
        }

        /// <summary>
        /// Prueba de alerta crítica - MODO BÁSICO
        /// </summary>
        public async void SendCriticalAlert()
        {
            LogTest("Enviando alerta crítica...");

            try
            {
                if (_useBasicMode)
                {
                    // Modo básico: usar sendEmailAsync simple
                    var subject = "CRÍTICO: Sistema de Pruebas";
                    var message = $@"ALERTA CRÍTICA

Esta es una alerta crítica de prueba generada desde Unity.

Detalles:
- Tipo: Crítico
- Fecha: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss UTC}
- Plataforma: Unity
- Sistema: Twin Nexus Platform

Este es un mensaje de prueba.";

                    var success = await SESManager.Instance.SendEmailAsync(_testEmail, subject, message);

                    if (success)
                    {
                        LogTest("✅ Alerta crítica básica enviada exitosamente");
                    }
                    else
                    {
                        LogTest("❌ Error al enviar alerta crítica básica");
                    }
                }
                else
                {
                    // Método avanzado con templates
                    var success = await SESManager.Instance.SendCriticalAlertAsync(
                        "Sistema de Pruebas",
                        "Esta es una alerta crítica de prueba generada desde Unity."
                    );

                    if (success)
                    {
                        LogTest("✅ Alerta crítica con template enviada exitosamente");
                    }
                    else
                    {
                        LogTest("❌ Error al enviar alerta crítica con template");
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción: {ex.Message}");
                if (ex.Message.Contains("not authorized"))
                {
                    LogTest("💡 Activando modo básico - sin templates");
                    _useBasicMode = true;
                }
            }
        }

        /// <summary>
        /// Verifica el estado de salud del sistema SES - MODO BÁSICO
        /// </summary>
        public async void CheckHealth()
        {
            LogTest("Verificando estado de salud...");

            try
            {
                if (_useBasicMode)
                {
                    // Modo básico: verificar solo lo que podemos
                    LogTest("Verificando estado básico (sin permisos avanzados)...");

                    var canSend = SESManager.Instance.CanUserSendEmails();
                    var dailyLimit = SESManager.Instance.GetDailyEmailLimit();

                    // Intentar obtener emails verificados
                    List<string> verifiedEmails = new List<string>();
                    try
                    {
                        verifiedEmails = await SESManager.Instance.GetVerifiedEmailsAsync();
                    }
                    catch (System.Exception ex)
                    {
                        LogTest($"⚠️ No se pudieron obtener emails verificados: {ex.Message}");
                    }

                    var healthInfo = $"Estado de Salud Básico:\n" +
                                     $"Puede enviar: {(canSend ? "✅ Sí" : "❌ No")}\n" +
                                     $"Límite diario configurado: {dailyLimit}\n" +
                                     $"Emails verificados encontrados: {verifiedEmails.Count}\n" +
                                     $"Último check: {System.DateTime.Now:HH:mm:ss}";

                    LogTest(healthInfo);

                    if (_healthText != null)
                        _healthText.text = healthInfo;
                }
                else
                {
                    // Método avanzado con cuotas
                    var health = await SESManager.Instance.GetHealthStatusAsync();

                    var healthStatus = health.IsHealthy ? "✅ SALUDABLE" : "❌ NO SALUDABLE";
                    var healthInfo = $"{healthStatus}\n" +
                                     $"Emails verificados: {health.VerifiedEmailCount}\n" +
                                     $"Puede enviar: {health.CanSendEmails}\n" +
                                     $"Límite diario: {health.DailyLimit}\n" +
                                     $"Uso de cuota: {health.QuotaUsagePercentage:F1}%";

                    LogTest(healthInfo);

                    if (_healthText != null)
                        _healthText.text = healthInfo;
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Error verificando salud: {ex.Message}");
                if (ex.Message.Contains("not authorized"))
                {
                    LogTest("💡 Activando modo básico - sin acceso a cuotas");
                    _useBasicMode = true;
                }
            }
        }

        /// <summary>
        /// Obtiene información de cuota
        /// </summary>
        public async void GetQuota()
        {
            LogTest("Obteniendo información de cuota...");

            try
            {
                var quota = await SESManager.Instance.GetSendQuotaAsync();

                var quotaInfo = $"Cuota SES:\n" +
                                $"Máximo 24h: {quota.Max24HourSend:F0}\n" +
                                $"Enviados 24h: {quota.SentLast24Hours:F0}\n" +
                                $"Restante: {quota.RemainingQuota:F0}\n" +
                                $"Uso: {quota.UsagePercentage:F1}%\n" +
                                $"Tasa máxima: {quota.MaxSendRate:F1}/seg";

                LogTest(quotaInfo);

                if (_quotaText != null)
                    _quotaText.text = quotaInfo;
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Error obteniendo cuota: {ex.Message}");
            }
        }

        #endregion

        #region Métodos de Conveniencia

        /// <summary>
        /// Reconfigura SES con emails verificados
        /// </summary>
        public async void ReconfigureSESWithVerifiedEmails()
        {
            LogTest("🔧 Reconfigurando SES con emails verificados...");

            try
            {
                // Reinicializar SESManager con email verificado
                var success = await SESManager.Instance.ReinitializeAsync(
                    "mechar09@gmail.com", // Email verificado como remitente
                    "Mechar Testing" // Nombre del remitente
                );

                if (success)
                {
                    LogTest("✅ SES reconfigurado exitosamente con emails verificados");
                    LogTest($"✅ Remitente: Mechar Testing <mechar09@gmail.com>");
                    LogTest($"✅ Destinatario de prueba: {_testEmail}");
                }
                else
                {
                    LogTest("❌ Error al reconfigurar SES");
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción reconfigurando SES: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica emails disponibles en SES
        /// </summary>
        public async void CheckVerifiedEmails()
        {
            LogTest("📧 Verificando emails disponibles en SES...");

            try
            {
                var verifiedEmails = await SESManager.Instance.GetVerifiedEmailsAsync();

                if (verifiedEmails.Count > 0)
                {
                    LogTest($"✅ Emails verificados encontrados ({verifiedEmails.Count}):");
                    foreach (var email in verifiedEmails)
                    {
                        LogTest($"  ✅ {email}");
                    }
                }
                else
                {
                    LogTest("❌ No se encontraron emails verificados");
                    LogTest("💡 Asegúrate de verificar mechar09@gmail.com y mechar09@outlook.com en AWS SES");
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Error verificando emails: {ex.Message}");
            }
        }

        /// <summary>
        /// Envía un email personalizado - método de conveniencia
        /// </summary>
        public async void SendCustomEmail(string toEmail, string subject, string message)
        {
            LogTest($"Enviando email personalizado a {toEmail}...");

            try
            {
                var success = await SESManager.Instance.SendEmailAsync(toEmail, subject, message);

                if (success)
                {
                    LogTest($"✅ Email personalizado enviado a {toEmail}");
                }
                else
                {
                    LogTest($"❌ Error al enviar email personalizado a {toEmail}");
                    LogTest("💡 Verifica que el email remitente esté configurado correctamente");
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción: {ex.Message}");
                if (ex.Message.Contains("not verified"))
                {
                    LogTest("💡 Solución: Presiona [R] para reconfigurar SES con emails verificados");
                }
            }
        }

        /// <summary>
        /// Envía email personalizado con prompt - activado por teclado
        /// </summary>
        public void SendCustomEmailPrompt()
        {
            // Usar valores por defecto para prueba rápida
            var testEmail = string.IsNullOrEmpty(_testEmail) ? "test@example.com" : _testEmail;
            var subject = "Email Personalizado de Prueba";
            var message = $"Este es un email personalizado enviado desde Unity a las {System.DateTime.Now:HH:mm:ss}";

            LogTest($"Enviando email personalizado a {testEmail} con teclado...");
            SendCustomEmail(testEmail, subject, message);
        }

        /// <summary>
        /// Verifica si el usuario puede enviar emails
        /// </summary>
        public void CheckUserPermissions()
        {
            try
            {
                var canSend = SESManager.Instance.CanUserSendEmails();
                var dailyLimit = SESManager.Instance.GetDailyEmailLimit();

                var permissionInfo = $"Permisos del usuario:\n" +
                                     $"Puede enviar: {(canSend ? "✅ Sí" : "❌ No")}\n" +
                                     $"Límite diario: {dailyLimit}";

                LogTest(permissionInfo);
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Error verificando permisos: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private void OnServiceActivated(string serviceName)
        {
            if (serviceName == "SESManager")
            {
                LogTest("🚀 SESManager activado y listo para usar");
                UpdateStatusDisplay();
            }
        }

        private void OnServiceError(string serviceName, string error)
        {
            if (serviceName == "SESManager")
            {
                LogTest($"❌ Error en SESManager: {error}");
                UpdateStatusDisplay();
            }
        }

        #endregion

        #region UI Updates

        private void UpdateStatusDisplay()
        {
            if (_statusText == null) return;

            try
            {
                var serviceController = ServiceController.Instance;
                var isAvailable = serviceController?.IsServiceAvailable("SESManager") ?? false;
                var userInfo = serviceController?.GetUserInfo() ?? ("", "", false);

                var statusInfo = $"Estado SES: {(isAvailable ? "✅ Disponible" : "❌ No disponible")}\n" +
                                 $"Usuario: {userInfo.username}\n" +
                                 $"Grupo: {userInfo.userGroup}\n" +
                                 $"Autenticado: {(userInfo.isAuthenticated ? "✅ Sí" : "❌ No")}";

                _statusText.text = statusInfo;
            }
            catch (System.Exception ex)
            {
                _statusText.text = $"Error actualizando estado: {ex.Message}";
            }
        }

        #endregion

        #region Logging

        private void LogTest(string message)
        {
            Debug.Log($"[SESTest] {message}");

            // También mostrar en UI si hay text component
            if (_statusText != null)
            {
                _statusText.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Desuscribirse de eventos
            if (ServiceController.Instance != null)
            {
                ServiceController.Instance.OnServiceActivated -= OnServiceActivated;
                ServiceController.Instance.OnServiceError -= OnServiceError;
            }

            // Limpiar Input Actions
            CleanupInputActions();
        }

        private void CleanupInputActions()
        {
            // Deshabilitar y limpiar todas las acciones
            actionTestEmail?.Disable();
            actionWelcomeEmail?.Disable();
            actionBulkEmail?.Disable();
            actionCriticalAlert?.Disable();
            actionHealth?.Disable();
            actionQuota?.Disable();
            actionCustomEmail?.Disable();
            actionPermissions?.Disable();
            actionReconfigure?.Disable();

            actionTestEmail?.Dispose();
            actionWelcomeEmail?.Dispose();
            actionBulkEmail?.Dispose();
            actionCriticalAlert?.Dispose();
            actionHealth?.Dispose();
            actionQuota?.Dispose();
            actionCustomEmail?.Dispose();
            actionPermissions?.Dispose();
            actionReconfigure?.Dispose();
        }

        #endregion

        #region Editor Testing (opcional)

#if UNITY_EDITOR
        [Header("Editor Testing")] [SerializeField]
        private bool _enableEditorTesting = true;

        [ContextMenu("Test - Send Simple Email")]
        private void EditorTestSendEmail()
        {
            if (_enableEditorTesting && Application.isPlaying)
                SendTestEmail();
        }

        [ContextMenu("Test - Check Health")]
        private void EditorTestCheckHealth()
        {
            if (_enableEditorTesting && Application.isPlaying)
                CheckHealth();
        }

        [ContextMenu("Test - Get Quota")]
        private void EditorTestGetQuota()
        {
            if (_enableEditorTesting && Application.isPlaying)
                GetQuota();
        }
#endif

        #endregion
    }
}