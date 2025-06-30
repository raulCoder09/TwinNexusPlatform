using System;
using System.Threading.Tasks;
using _Scripts.Models.CognitoManagement;
using UnityEngine;

namespace _Scripts.Controllers.WelcomeController
{
    public class WelcomeAuthHandler : IWelcomeOps
    {
        private readonly WelcomeInfo.AuthState _authState = new WelcomeInfo.AuthState();
        private readonly CognitoManager _cognitoManager;

        public WelcomeAuthHandler()
        {
            _cognitoManager = CognitoManager.Instance;
            if (_cognitoManager != null)
            {
                _cognitoManager.OnAuthenticationComplete += OnAuthenticationComplete;
                _cognitoManager.OnRegistrationComplete += OnRegistrationComplete;
                _cognitoManager.OnEmailVerificationComplete += OnEmailVerificationComplete;
                _cognitoManager.OnPasswordRecoveryComplete += OnPasswordRecoveryComplete;
                _cognitoManager.OnResendVerificationComplete += OnResendVerificationComplete;
                _cognitoManager.OnAWSCredentialsObtained += OnAWSCredentialsObtained;
            }
        }

        public event Action OnAuthenticationSuccess;

        public async Task<bool> AuthenticateUserAsync(string username, string password)
        {
            if (_cognitoManager == null)
            {
                Debug.LogError("CognitoManager not available");
                return false;
            }

            return await _cognitoManager.SignInAsync(username, password);
        }

        public async Task<bool> RegisterUserAsync(string username, string password, string email, string phoneNumber = null)
        {
            if (_cognitoManager == null)
            {
                Debug.LogError("CognitoManager not available");
                return false;
            }

            return await _cognitoManager.SignUpAsync(username, password, email, phoneNumber);
        }

        public async Task<bool> VerifyEmailAsync(string confirmationCode)
        {
            if (_cognitoManager == null)
            {
                Debug.LogError("CognitoManager not available");
                return false;
            }

            return await _cognitoManager.ConfirmSignUpAsync(_cognitoManager.PendingUsername, confirmationCode);
        }

        public async Task<bool> RecoverPasswordAsync(string username)
        {
            if (_cognitoManager == null)
            {
                Debug.LogError("CognitoManager not available");
                return false;
            }

            return await _cognitoManager.ForgotPasswordAsync(username);
        }

        public async Task<bool> ResendVerificationCodeAsync()
        {
            if (_cognitoManager == null)
            {
                Debug.LogError("CognitoManager not available");
                return false;
            }

            return await _cognitoManager.ResendConfirmationCodeAsync();
        }

        public void NavigateToPanel(IWelcomeOps.PanelType panelType)
        {
            // Este método será manejado por WelcomeOrchestrator, aquí solo se define como placeholder
            Debug.LogWarning("NavigateToPanel should be implemented by WelcomeOrchestrator");
        }

        private void OnAuthenticationComplete(bool success, string message)
        {
            if (success)
            {
                OnAuthenticationSuccess?.Invoke();
            }
            // No se usa OnAuthenticationFailure aquí, ya que WelcomeOrchestrator manejará los mensajes
        }

        private void OnRegistrationComplete(bool success, string message)
        {
            // Se deja como placeholder para manejo en WelcomeOrchestrator
        }

        private void OnEmailVerificationComplete(bool success, string message)
        {
            // Se deja como placeholder para manejo en WelcomeOrchestrator
        }

        private void OnPasswordRecoveryComplete(bool success, string message)
        {
            // Se deja como placeholder para manejo en WelcomeOrchestrator
        }

        private void OnResendVerificationComplete(bool success, string message)
        {
            // Se deja como placeholder para manejo en WelcomeOrchestrator
        }

        private void OnAWSCredentialsObtained(bool success, string message)
        {
            if (success)
            {
                OnAuthenticationSuccess?.Invoke();
            }
        }
    }
}