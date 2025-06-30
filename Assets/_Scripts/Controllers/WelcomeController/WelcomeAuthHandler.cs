using System;
using System.Threading.Tasks;
using _Scripts.Models.CognitoManagement;
using UnityEngine;
using Amazon;

namespace _Scripts.Controllers.WelcomeController
{
    public class WelcomeAuthHandler : CognitoManager, IWelcomeOps
    {
        private readonly WelcomeInfo.AuthState _authState = new WelcomeInfo.AuthState();
        
        private string UserPoolId;
        private string ClientId;
        private string IdentityPoolId;
        private RegionEndpoint AwsRegion;

        internal string userPoolId
        {
            get => UserPoolId;
            set => UserPoolId = value;
        }

        internal string clientId
        {
            get => ClientId;
            set => ClientId = value;
        }

        internal string identityPoolId
        {
            get => IdentityPoolId;
            set => IdentityPoolId = value;
        }

        internal RegionEndpoint awsRegion
        {
            get => AwsRegion;
            set => AwsRegion = value;
        }

        // Eventos específicos de Welcome (además de los eventos de CognitoManager)
        public event Action OnAuthenticationSuccess;
        public event Action<string> OnAuthenticationFailure;
        public event Action<string> OnRegistrationSuccess;
        public event Action<string> OnRegistrationFailure;
        public event Action<string> OnEmailVerificationSuccess;
        public event Action<string> OnEmailVerificationFailure;
        public event Action<string> OnPasswordRecoverySuccess;
        public event Action<string> OnPasswordRecoveryFailure;
        public event Action<string> OnResendVerificationSuccess;
        public event Action<string> OnResendVerificationFailure;

        private new void Awake() // 'new' para sobrescribir el Awake de CognitoManager
        {
            // Configurar valores específicos de Welcome antes de inicializar CognitoManager
            if (!string.IsNullOrEmpty(UserPoolId))
                _userPoolId = UserPoolId;
            if (!string.IsNullOrEmpty(ClientId))
                _clientId = ClientId;
            if (!string.IsNullOrEmpty(IdentityPoolId))
                _identityPoolId = IdentityPoolId;
            if (AwsRegion != null)
                _regionEndpoint = AwsRegion;

            // Suscribirse a eventos de CognitoManager
            OnAuthenticationComplete += HandleCognitoAuthenticationComplete;
            OnRegistrationComplete += HandleCognitoRegistrationComplete;
            OnEmailVerificationComplete += HandleCognitoEmailVerificationComplete;
            OnPasswordRecoveryComplete += HandleCognitoPasswordRecoveryComplete;
            OnResendVerificationComplete += HandleCognitoResendVerificationComplete;
            OnAWSCredentialsObtained += HandleCognitoAWSCredentialsObtained;

            Debug.Log("[WelcomeAuthHandler] Initialized with Welcome-specific configuration");
        }

        private new void OnDestroy()
        {
            // Desuscribirse de eventos
            OnAuthenticationComplete -= HandleCognitoAuthenticationComplete;
            OnRegistrationComplete -= HandleCognitoRegistrationComplete;
            OnEmailVerificationComplete -= HandleCognitoEmailVerificationComplete;
            OnPasswordRecoveryComplete -= HandleCognitoPasswordRecoveryComplete;
            OnResendVerificationComplete -= HandleCognitoResendVerificationComplete;
            OnAWSCredentialsObtained -= HandleCognitoAWSCredentialsObtained;

            // Llamar al OnDestroy de la clase base
            base.OnDestroy();
        }

        #region Manejo de eventos de CognitoManager
        private void HandleCognitoAuthenticationComplete(bool success, string message)
        {
            Debug.Log($"[WelcomeAuthHandler] Authentication: {(success ? "Success" : "Failed")} - {message}");
            
            if (success)
            {
                OnAuthenticationSuccess?.Invoke();
            }
            else
            {
                OnAuthenticationFailure?.Invoke(message);
            }
        }

        private void HandleCognitoRegistrationComplete(bool success, string message)
        {
            Debug.Log($"[WelcomeAuthHandler] Registration: {(success ? "Success" : "Failed")} - {message}");
            
            if (success)
            {
                OnRegistrationSuccess?.Invoke(message);
            }
            else
            {
                OnRegistrationFailure?.Invoke(message);
            }
        }

        private void HandleCognitoEmailVerificationComplete(bool success, string message)
        {
            Debug.Log($"[WelcomeAuthHandler] Email Verification: {(success ? "Success" : "Failed")} - {message}");
            
            if (success)
            {
                OnEmailVerificationSuccess?.Invoke(message);
                // Después de verificar email exitosamente, también disparar OnAuthenticationSuccess
                OnAuthenticationSuccess?.Invoke();
            }
            else
            {
                OnEmailVerificationFailure?.Invoke(message);
            }
        }

        private void HandleCognitoPasswordRecoveryComplete(bool success, string message)
        {
            Debug.Log($"[WelcomeAuthHandler] Password Recovery: {(success ? "Success" : "Failed")} - {message}");
            
            if (success)
            {
                OnPasswordRecoverySuccess?.Invoke(message);
            }
            else
            {
                OnPasswordRecoveryFailure?.Invoke(message);
            }
        }

        private void HandleCognitoResendVerificationComplete(bool success, string message)
        {
            Debug.Log($"[WelcomeAuthHandler] Resend Verification: {(success ? "Success" : "Failed")} - {message}");
            
            if (success)
            {
                OnResendVerificationSuccess?.Invoke(message);
            }
            else
            {
                OnResendVerificationFailure?.Invoke(message);
            }
        }

        private void HandleCognitoAWSCredentialsObtained(bool success, string message)
        {
            Debug.Log($"[WelcomeAuthHandler] AWS Credentials: {(success ? "Success" : "Failed")} - {message}");
            
            if (success)
            {
                // Cuando se obtienen credenciales AWS exitosamente, también significa autenticación completa
                OnAuthenticationSuccess?.Invoke();
            }
        }
        #endregion

        #region IWelcomeOps Implementation - Delegando a CognitoManager
        public async Task<bool> AuthenticateUserAsync(string username, string password)
        {
            Debug.Log($"[WelcomeAuthHandler] Attempting authentication for user: {username}");
            
            // Usar el método directo de CognitoManager (clase base)
            return await SignInAsync(username, password);
        }

        public async Task<bool> RegisterUserAsync(string username, string password, string email, string phoneNumber = null)
        {
            Debug.Log($"[WelcomeAuthHandler] Attempting registration for user: {username}");
            
            // Usar el método directo de CognitoManager (clase base)
            return await SignUpAsync(username, password, email, phoneNumber);
        }

        public async Task<bool> VerifyEmailAsync(string confirmationCode)
        {
            Debug.Log($"[WelcomeAuthHandler] Attempting email verification");
            
            // Usar el método directo de CognitoManager (clase base)
            // Usar PendingUsername que mantiene CognitoManager
            return await ConfirmSignUpAsync(PendingUsername, confirmationCode);
        }

        public async Task<bool> RecoverPasswordAsync(string username)
        {
            Debug.Log($"[WelcomeAuthHandler] Attempting password recovery for user: {username}");
            
            // Usar el método directo de CognitoManager (clase base)
            return await ForgotPasswordAsync(username);
        }

        public async Task<bool> ResendVerificationCodeAsync()
        {
            Debug.Log($"[WelcomeAuthHandler] Attempting to resend verification code");
            
            // Usar el método directo de CognitoManager (clase base)
            return await ResendConfirmationCodeAsync();
        }

        public void NavigateToPanel(IWelcomeOps.PanelType panelType)
        {
            // Este método no aplica para WelcomeAuthHandler - es responsabilidad del Orchestrator
            Debug.LogWarning("[WelcomeAuthHandler] NavigateToPanel should be handled by WelcomeOrchestrator");
        }
        #endregion

        #region Métodos adicionales específicos de Welcome
        /// <summary>
        /// Verifica si el usuario está autenticado
        /// </summary>
        public bool IsAuthenticated => IsUserAuthenticated;

        /// <summary>
        /// Obtiene el nombre de usuario actual
        /// </summary>
        public string GetCurrentUsername() => CurrentUsername;

        /// <summary>
        /// Obtiene el email pendiente de verificación
        /// </summary>
        public string GetPendingEmail() => PendingEmail;

        /// <summary>
        /// Cierra sesión del usuario actual
        /// </summary>
        public void Logout()
        {
            Debug.Log("[WelcomeAuthHandler] Logging out user");
            SignOut();
        }

        /// <summary>
        /// Obtiene información del usuario logueado
        /// </summary>
        public (string username, string userGroup, bool isAuthenticated) GetUserInfo()
        {
            return (CurrentUsername, CurrentUserGroup, IsUserAuthenticated);
        }

        /// <summary>
        /// Verifica si el usuario pertenece a un grupo específico
        /// </summary>
        public bool IsUserInSpecificGroup(string groupName)
        {
            return IsUserInGroup(groupName);
        }
        #endregion

        #region Métodos de configuración
        /// <summary>
        /// Permite cambiar la configuración de Cognito en tiempo de ejecución
        /// </summary>
        public void UpdateCognitoConfiguration(string userPoolId, string clientId, string identityPoolId, RegionEndpoint region)
        {
            Debug.Log("[WelcomeAuthHandler] Updating Cognito configuration");
            
            _userPoolId = userPoolId;
            _clientId = clientId;
            _identityPoolId = identityPoolId;
            _regionEndpoint = region;
            
            // Reinicializar si es necesario
            if (_isInitialized)
            {
                Debug.Log("[WelcomeAuthHandler] Reinitializing with new configuration");
                _isInitialized = false;
                Initialize();
            }
        }

        /// <summary>
        /// Obtiene la configuración actual de Cognito
        /// </summary>
        public (string userPoolId, string clientId, string identityPoolId, RegionEndpoint region) GetCognitoConfiguration()
        {
            return (_userPoolId, _clientId, _identityPoolId, _regionEndpoint);
        }
        #endregion
    }
}