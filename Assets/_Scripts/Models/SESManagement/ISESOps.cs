namespace _Scripts.Models.SESManagement
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISESOps
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string textBody, string htmlBody = null);
        Task<bool> SendEmailWithUserContextAsync(string toEmail, string subject, string message);
        Task<bool> SendSystemNotificationAsync(string subject, string message, NotificationType type = NotificationType.Info);
        Task<bool> SendTestEmailAsync();
        Task<List<string>> GetVerifiedEmailsAsync();
        Task<bool> RequestEmailVerificationAsync(string emailToVerify);
        bool CanUserSendEmails();
        int GetDailyEmailLimit();
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}