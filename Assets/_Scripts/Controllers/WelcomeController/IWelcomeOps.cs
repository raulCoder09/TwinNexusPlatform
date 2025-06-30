using System;
using System.Threading.Tasks;

namespace _Scripts.Controllers.WelcomeController
{
    public interface IWelcomeOps
    {
        Task<bool> AuthenticateUserAsync(string username, string password);
        Task<bool> RegisterUserAsync(string username, string password, string email, string phoneNumber = null);
        Task<bool> VerifyEmailAsync(string confirmationCode);
        Task<bool> RecoverPasswordAsync(string username);
        Task<bool> ResendVerificationCodeAsync();
        void NavigateToPanel(PanelType panelType);
        event Action OnAuthenticationSuccess;

        // Movemos el enum dentro de la interfaz
        public enum PanelType
        {
            None,
            Login,
            Register,
            RecoverPassword,
            EmailVerification
        }
    }
}