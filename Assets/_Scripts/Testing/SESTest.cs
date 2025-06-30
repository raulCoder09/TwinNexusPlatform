using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using _Scripts.Models;
using _Scripts.Models.SESManagement;

namespace _Scripts.Testing
{
    public class SESTest : MonoBehaviour
    {
        [Header("Test Email Configuration")]
        [SerializeField] private string testToEmail = ""; // Email donde enviar las pruebas
        [SerializeField] private string verifyEmail = ""; // Email para verificar
        
        private InputAction sendTestEmailAction;
        private InputAction sendContextEmailAction;
        private InputAction sendNotificationAction;
        private InputAction listVerifiedEmailsAction;
        private InputAction verifyEmailAction;
        private InputAction testConnectivityAction;
        private InputAction sendHtmlEmailAction;

        void Start()
        {
            // Initialize input actions
            sendTestEmailAction = new InputAction("SendTestEmail", InputActionType.Button, "<Keyboard>/t");
            sendContextEmailAction = new InputAction("SendContextEmail", InputActionType.Button, "<Keyboard>/c");
            sendNotificationAction = new InputAction("SendNotification", InputActionType.Button, "<Keyboard>/n");
            listVerifiedEmailsAction = new InputAction("ListVerifiedEmails", InputActionType.Button, "<Keyboard>/l");
            verifyEmailAction = new InputAction("VerifyEmail", InputActionType.Button, "<Keyboard>/v");
            testConnectivityAction = new InputAction("TestConnectivity", InputActionType.Button, "<Keyboard>/k");
            sendHtmlEmailAction = new InputAction("SendHtmlEmail", InputActionType.Button, "<Keyboard>/h");
            
            // Register callbacks
            sendTestEmailAction.performed += OnSendTestEmailPressed;
            sendContextEmailAction.performed += OnSendContextEmailPressed;
            sendNotificationAction.performed += OnSendNotificationPressed;
            listVerifiedEmailsAction.performed += OnListVerifiedEmailsPressed;
            verifyEmailAction.performed += OnVerifyEmailPressed;
            testConnectivityAction.performed += OnTestConnectivityPressed;
            sendHtmlEmailAction.performed += OnSendHtmlEmailPressed;
            
            // Enable actions
            sendTestEmailAction.Enable();
            sendContextEmailAction.Enable();
            sendNotificationAction.Enable();
            listVerifiedEmailsAction.Enable();
            verifyEmailAction.Enable();
            testConnectivityAction.Enable();
            sendHtmlEmailAction.Enable();
            
            // Subscribe to SES events
            if (SESManager.Instance != null)
            {
                SESManager.Instance.OnEmailSent += OnEmailSentResult;
                SESManager.Instance.OnVerificationStatusChecked += OnVerificationStatusResult;
                SESManager.Instance.OnEmailVerificationSent += OnEmailVerificationResult;
            }
            
            Debug.Log("🚀 SESTest iniciado:");
            Debug.Log("📧 Presiona 'T' para enviar email de prueba");
            Debug.Log("👤 Presiona 'C' para enviar email con contexto de usuario");
            Debug.Log("🔔 Presiona 'N' para enviar notificación del sistema");
            Debug.Log("📋 Presiona 'L' para listar emails verificados");
            Debug.Log("✅ Presiona 'V' para verificar nuevo email");
            Debug.Log("🔗 Presiona 'K' para test de conectividad");
            Debug.Log("🎨 Presiona 'H' para enviar email HTML");
            Debug.Log("⚙️ Configura 'testToEmail' y 'verifyEmail' en el Inspector");
        }

        void OnDestroy()
        {
            // Cleanup input actions
            if (sendTestEmailAction != null)
            {
                sendTestEmailAction.performed -= OnSendTestEmailPressed;
                sendTestEmailAction.Disable();
                sendTestEmailAction.Dispose();
            }
            
            if (sendContextEmailAction != null)
            {
                sendContextEmailAction.performed -= OnSendContextEmailPressed;
                sendContextEmailAction.Disable();
                sendContextEmailAction.Dispose();
            }
            
            if (sendNotificationAction != null)
            {
                sendNotificationAction.performed -= OnSendNotificationPressed;
                sendNotificationAction.Disable();
                sendNotificationAction.Dispose();
            }
            
            if (listVerifiedEmailsAction != null)
            {
                listVerifiedEmailsAction.performed -= OnListVerifiedEmailsPressed;
                listVerifiedEmailsAction.Disable();
                listVerifiedEmailsAction.Dispose();
            }
            
            if (verifyEmailAction != null)
            {
                verifyEmailAction.performed -= OnVerifyEmailPressed;
                verifyEmailAction.Disable();
                verifyEmailAction.Dispose();
            }
            
            if (testConnectivityAction != null)
            {
                testConnectivityAction.performed -= OnTestConnectivityPressed;
                testConnectivityAction.Disable();
                testConnectivityAction.Dispose();
            }
            
            if (sendHtmlEmailAction != null)
            {
                sendHtmlEmailAction.performed -= OnSendHtmlEmailPressed;
                sendHtmlEmailAction.Disable();
                sendHtmlEmailAction.Dispose();
            }
        }

        #region Input Action Callbacks
        
        private void OnSendTestEmailPressed(InputAction.CallbackContext context)
        {
            ExecuteSendTestEmail();
        }
        
        private void OnSendContextEmailPressed(InputAction.CallbackContext context)
        {
            ExecuteSendContextEmail();
        }
        
        private void OnSendNotificationPressed(InputAction.CallbackContext context)
        {
            ExecuteSendNotification();
        }
        
        private void OnListVerifiedEmailsPressed(InputAction.CallbackContext context)
        {
            ExecuteListVerifiedEmails();
        }
        
        private void OnVerifyEmailPressed(InputAction.CallbackContext context)
        {
            ExecuteVerifyEmail();
        }
        
        private void OnTestConnectivityPressed(InputAction.CallbackContext context)
        {
            ExecuteTestConnectivity();
        }
        
        private void OnSendHtmlEmailPressed(InputAction.CallbackContext context)
        {
            ExecuteSendHtmlEmail();
        }
        
        #endregion

        #region Test Execution Methods
        
        async void ExecuteSendTestEmail()
        {
            if (SESManager.Instance == null)
            {
                Debug.LogError("SESManager no disponible.");
                return;
            }
            
            Debug.Log("📧 Iniciando envío de email de prueba...");
            bool success = await SESManager.Instance.SendTestEmailAsync();
            Debug.Log(success ? "✅ Test email completado" : "❌ Test email falló");
        }
        
        async void ExecuteSendContextEmail()
        {
            if (SESManager.Instance == null)
            {
                Debug.LogError("SESManager no disponible.");
                return;
            }
            
            if (string.IsNullOrEmpty(testToEmail))
            {
                Debug.LogWarning("⚠️ No se ha configurado 'testToEmail' en el Inspector");
                return;
            }
            
            Debug.Log("👤 Iniciando envío de email con contexto de usuario...");
            
            string contextMessage = $@"¡Hola! Este es un email de prueba con contexto de usuario.

🎮 Enviado desde Unity usando AWS SES
🧪 Test ID: {System.Guid.NewGuid()}
⏰ Timestamp: {System.DateTime.UtcNow}

Este email incluye automáticamente el contexto del usuario autenticado.";

            bool success = await SESManager.Instance.SendEmailWithUserContextAsync(
                testToEmail, 
                "Email con Contexto de Usuario - Twin Nexus", 
                contextMessage
            );
            
            Debug.Log(success ? "✅ Context email completado" : "❌ Context email falló");
        }
        
        async void ExecuteSendNotification()
        {
            if (SESManager.Instance == null)
            {
                Debug.LogError("SESManager no disponible.");
                return;
            }
            
            Debug.Log("🔔 Iniciando envío de notificación del sistema...");
            
            string notificationMessage = $@"Se ha ejecutado una prueba del sistema de notificaciones.

📊 Detalles de la prueba:
• Tipo: Notificación de prueba
• Módulo: SES Testing
• Resultado: Exitoso
• Test ID: {System.Guid.NewGuid()}

Este es un ejemplo de cómo el sistema puede enviar notificaciones automáticas a los administradores.";

            bool success = await SESManager.Instance.SendSystemNotificationAsync(
                "Prueba del Sistema de Notificaciones",
                notificationMessage,
                SESManager.NotificationType.Info
            );
            
            Debug.Log(success ? "✅ System notification completado" : "❌ System notification falló");
        }
        
        async void ExecuteListVerifiedEmails()
        {
            if (SESManager.Instance == null)
            {
                Debug.LogError("SESManager no disponible.");
                return;
            }
            
            Debug.Log("📋 Iniciando listado de emails verificados...");
            var verifiedEmails = await SESManager.Instance.GetVerifiedEmailsAsync();
            Debug.Log($"✅ Listado completado. Se encontraron {verifiedEmails.Count} emails verificados.");
        }
        
        async void ExecuteVerifyEmail()
        {
            if (SESManager.Instance == null)
            {
                Debug.LogError("SESManager no disponible.");
                return;
            }
            
            if (string.IsNullOrEmpty(verifyEmail))
            {
                Debug.LogWarning("⚠️ No se ha configurado 'verifyEmail' en el Inspector");
                return;
            }
            
            if (!SESManager.Instance.IsValidEmail(verifyEmail))
            {
                Debug.LogError("❌ El email configurado no tiene un formato válido");
                return;
            }
            
            Debug.Log($"✅ Iniciando verificación de email: {verifyEmail}");
            bool success = await SESManager.Instance.RequestEmailVerificationAsync(verifyEmail);
            Debug.Log(success ? "✅ Verification request completado" : "❌ Verification request falló");
        }
        
        async void ExecuteTestConnectivity()
        {
            if (SESManager.Instance == null)
            {
                Debug.LogError("SESManager no disponible.");
                return;
            }
            
            Debug.Log("🔗 Iniciando test de conectividad SES...");
            bool success = await SESManager.Instance.TestSESConnectivityAsync();
            Debug.Log(success ? "✅ Connectivity test completado" : "❌ Connectivity test falló");
        }
        
        async void ExecuteSendHtmlEmail()
        {
            if (SESManager.Instance == null)
            {
                Debug.LogError("SESManager no disponible.");
                return;
            }
            
            if (string.IsNullOrEmpty(testToEmail))
            {
                Debug.LogWarning("⚠️ No se ha configurado 'testToEmail' en el Inspector");
                return;
            }
            
            Debug.Log("🎨 Iniciando envío de email HTML...");
            
            string textBody = @"¡Hola! Este es un email de prueba con formato HTML.

Si no puedes ver el contenido HTML, aquí está la versión en texto plano:
- Twin Nexus Platform
- Email de prueba desde Unity
- Usando AWS SES
- Test exitoso";

            string htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #00ff00; color: #000; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; }}
        .footer {{ background-color: #f4f4f4; padding: 10px; font-size: 12px; text-align: center; }}
        .highlight {{ background-color: #e8f5e8; padding: 10px; border-left: 4px solid #00ff00; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🎮 Twin Nexus Platform</h1>
        <p>Email de Prueba HTML</p>
    </div>
    
    <div class='content'>
        <h2>¡Hola desde Unity! 👋</h2>
        <p>Este es un email de prueba con formato <strong>HTML</strong> enviado desde Unity usando AWS SES.</p>
        
        <div class='highlight'>
            <h3>📊 Detalles de la prueba:</h3>
            <ul>
                <li><strong>Usuario:</strong> {OldCognitoManager.Instance?.CurrentUsername ?? "Unknown"}</li>
                <li><strong>Rol:</strong> {OldCognitoManager.Instance?.GetUserRole() ?? "Unknown"}</li>
                <li><strong>Timestamp:</strong> {System.DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                <li><strong>Test ID:</strong> {System.Guid.NewGuid()}</li>
            </ul>
        </div>
        
        <p>✅ Si puedes leer este mensaje, significa que el sistema de emails HTML está funcionando correctamente.</p>
        
        <h3>🚀 Características del sistema:</h3>
        <ul>
            <li>✉️ Envío de emails desde Unity</li>
            <li>🔒 Autenticación con AWS Cognito</li>
            <li>🎨 Soporte para HTML y texto plano</li>
            <li>👥 Control de permisos por roles</li>
            <li>📊 Logging detallado</li>
        </ul>
    </div>
    
    <div class='footer'>
        <p>© 2025 Twin Nexus Platform - Enviado automáticamente desde Unity</p>
    </div>
</body>
</html>";

            bool success = await SESManager.Instance.SendSimpleEmailAsync(
                testToEmail,
                "🎨 Email HTML de Prueba - Twin Nexus Platform",
                textBody,
                htmlBody
            );
            
            Debug.Log(success ? "✅ HTML email completado" : "❌ HTML email falló");
        }
        
        #endregion

        #region Event Handlers
        
        void OnEmailSentResult(bool success, string message, string messageId)
        {
            if (success)
            {
                Debug.Log($"🎉 ¡Email enviado exitosamente!");
                Debug.Log($"📧 Message ID: {messageId}");
                Debug.Log($"💬 Mensaje: {message}");
            }
            else
            {
                Debug.LogError($"❌ Error enviando email: {message}");
            }
        }
        
        void OnVerificationStatusResult(bool success, string message, List<string> verifiedEmails)
        {
            if (success)
            {
                Debug.Log($"📋 ¡Verificación de emails exitosa!");
                Debug.Log($"📊 Total: {verifiedEmails?.Count ?? 0} emails verificados");
                Debug.Log($"💬 Mensaje: {message}");
                
                if (verifiedEmails != null && verifiedEmails.Count > 0)
                {
                    Debug.Log("✅ Emails verificados encontrados:");
                    for (int i = 0; i < Math.Min(verifiedEmails.Count, 5); i++)
                    {
                        Debug.Log($"   {i + 1}. {verifiedEmails[i]}");
                    }
                    
                    if (verifiedEmails.Count > 5)
                    {
                        Debug.Log($"   ... y {verifiedEmails.Count - 5} más");
                    }
                }
                else
                {
                    Debug.Log("⚠️ No se encontraron emails verificados");
                    Debug.Log("💡 Usa la tecla 'V' para verificar un nuevo email");
                }
            }
            else
            {
                Debug.LogError($"❌ Error verificando emails: {message}");
            }
        }
        
        void OnEmailVerificationResult(bool success, string message)
        {
            if (success)
            {
                Debug.Log($"✅ ¡Solicitud de verificación enviada!");
                Debug.Log($"💬 Mensaje: {message}");
                Debug.Log("📬 Revisa la bandeja de entrada del email para confirmar la verificación");
            }
            else
            {
                Debug.LogError($"❌ Error solicitando verificación: {message}");
            }
        }
        
        #endregion
    }
}