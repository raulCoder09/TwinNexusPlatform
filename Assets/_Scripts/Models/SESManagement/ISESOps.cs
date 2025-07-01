using System.Collections.Generic;
using System.Threading.Tasks;

namespace _Scripts.Models.SESManagement
{
    /// <summary>
    /// Interfaz principal para operaciones de AWS SES
    /// Define el contrato básico que deben cumplir todos los manejadores de email
    /// </summary>
    public interface ISESOps
    {
        // Operaciones básicas de envío
        Task<bool> SendEmailAsync(string toEmail, string subject, string textBody, string htmlBody = null);
        Task<bool> SendEmailWithUserContextAsync(string toEmail, string subject, string message);
        Task<bool> SendSystemNotificationAsync(string subject, string message, NotificationType type = NotificationType.Info);
        Task<bool> SendTestEmailAsync();
        
        // Gestión de verificación
        Task<List<string>> GetVerifiedEmailsAsync();
        Task<bool> RequestEmailVerificationAsync(string emailToVerify);
        
        // Control de permisos y límites
        bool CanUserSendEmails();
        int GetDailyEmailLimit();
    }

    /// <summary>
    /// Interfaz extendida para funcionalidades avanzadas de SES
    /// Separa responsabilidades: básico vs avanzado
    /// </summary>
    public interface ISESAdvancedOps : ISESOps
    {
        // Templates
        Task<bool> SendTemplatedEmailAsync(string toEmail, string templateName, Dictionary<string, string> templateData);
        Task<bool> InitializeTemplatesAsync();
        
        // Bulk operations
        Task<SESOperationResult> SendBulkEmailAsync(List<string> recipients, string subject, string textBody, string htmlBody = null);
        
        // Analytics y tracking
        void TrackEmailSent(string messageId, string recipient, string subject);
        Task<SESQuotaInfo> GetSendQuotaAsync();
    }

    /// <summary>
    /// Tipos de notificación para clasificar emails del sistema
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Resultado de operaciones SES que pueden ser complejas
    /// Implementa patrón Result para mejor manejo de errores
    /// </summary>
    public class SESOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string OperationId { get; set; }
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
        
        public static SESOperationResult SuccessResult(string message, string operationId = null)
        {
            return new SESOperationResult
            {
                Success = true,
                Message = message,
                OperationId = operationId ?? System.Guid.NewGuid().ToString()
            };
        }
        
        public static SESOperationResult FailureResult(string message, string operationId = null)
        {
            return new SESOperationResult
            {
                Success = false,
                Message = message,
                OperationId = operationId ?? System.Guid.NewGuid().ToString()
            };
        }
    }

    /// <summary>
    /// Información de cuota de SES
    /// Encapsula datos de límites de envío
    /// </summary>
    public class SESQuotaInfo
    {
        public double Max24HourSend { get; set; }
        public double MaxSendRate { get; set; }
        public double SentLast24Hours { get; set; }
        public double RemainingQuota => Max24HourSend - SentLast24Hours;
        public double UsagePercentage => Max24HourSend > 0 ? (SentLast24Hours / Max24HourSend) * 100 : 0;
        public bool IsNearLimit => UsagePercentage > 80;
    }
}