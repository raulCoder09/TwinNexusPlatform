using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Models;

namespace _Scripts.Controller
{
    public class WelcomeControllerUI : MonoBehaviour
    {
        #region UI Components - Main
        private VisualElement _body;
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private VisualElement _scrim;
        private Button _launchButton;
        private Button _exitAppButton;
        private Label _messageLabel;
        #endregion

        #region UI Components - Panels
        private VisualElement _loginPanel;
        private VisualElement _registerPanel;
        private VisualElement _recoverPasswordPanel;
        #endregion

        #region UI Components - Login Panel
        private Button _closeLoginPanelButton;
        private Button _registerLoginButton;
        private Button _recoverPasswordLoginButton;
        private Button _loginButton;
        private TextField _usernameLoginField;
        private TextField _passwordLoginField;
        #endregion

        #region UI Components - Register Panel
        private Button _closeRegisterPanelButton;
        private Button _registerAndLoginButton;
        private Button _backToLoginPanelFromRegisterButton;
        private TextField _usernameRegisterField;
        private TextField _emailRegisterField;
        private TextField _passwordRegisterField;
        private TextField _repeatPasswordRegisterField;
        #endregion

        #region UI Components - Recover Password Panel
        private Button _closeRecoverPasswordButton;
        private Button _recoverPasswordButton;
        private Button _backToLoginPanelFromRecoverPasswordButton;
        private TextField _emailRecoverField;
        #endregion

        #region Dependencies
        private DashboardController _dashboardController;
        private CognitoManager _cognitoManager;
        #endregion

        #region Panel Management
        private Dictionary<PanelType, PanelInfo> _panels;
        private PanelType _currentActivePanel = PanelType.None;
        #endregion

        #region Constants
        private const string SCRIM_FADEIN_CLASS = "ScrimFadein";
        #endregion

        #region Enums
        public enum PanelType
        {
            None,
            Login,
            Register,
            RecoverPassword
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            InitializeUI();
            SubscribeToCognitoEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromCognitoEvents();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Muestra la interfaz de bienvenida
        /// </summary>
        public void ShowUi()
        {
            _body.style.display = DisplayStyle.Flex;
            Debug.Log("Welcome UI shown");
        }

        /// <summary>
        /// Oculta la interfaz de bienvenida
        /// </summary>
        public void HideUi()
        {
            _body.style.display = DisplayStyle.None;
            Debug.Log("Welcome UI hidden");
        }

        /// <summary>
        /// Abre un panel específico
        /// </summary>
        /// <param name="panelType">Tipo de panel a abrir</param>
        public void OpenPanel(PanelType panelType)
        {
            if (panelType == PanelType.None)
            {
                Debug.LogWarning("Cannot open panel of type 'None'");
                return;
            }

            CloseCurrentPanel();
            ShowPanel(panelType);
        }

        /// <summary>
        /// Cierra el panel actualmente activo
        /// </summary>
        public void CloseCurrentPanel()
        {
            if (_currentActivePanel != PanelType.None)
            {
                HidePanel(_currentActivePanel);
            }
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            GetUiComponents();
            InitializePanelSystem();
            FindDependencies();
            RegisterAllEvents();
        }

        private void InitializeUI()
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            ShowUi();
            ClearMessage();
        }

        private void InitializePanelSystem()
        {
            _panels = new Dictionary<PanelType, PanelInfo>
            {
                {
                    PanelType.Login,
                    new PanelInfo
                    {
                        Panel = _loginPanel,
                        ShowClass = "LoginPanelMoveB",
                        HideClass = "LoginPanelMoveA"
                    }
                },
                {
                    PanelType.Register,
                    new PanelInfo
                    {
                        Panel = _registerPanel,
                        ShowClass = "RegisterPanelInMainScreen",
                        HideClass = "RegisterPanelOutMainScreen"
                    }
                },
                {
                    PanelType.RecoverPassword,
                    new PanelInfo
                    {
                        Panel = _recoverPasswordPanel,
                        ShowClass = "RecoverPasswordPanelInMainScreen",
                        HideClass = "RecoverPasswordPanelOutMainScreen"
                    }
                }
            };

            // Registrar eventos de transición para cada panel
            foreach (var panelInfo in _panels.Values)
            {
                panelInfo.Panel.RegisterCallback<TransitionEndEvent>(OnPanelTransitionComplete);
            }
        }

        private void FindDependencies()
        {
            _dashboardController = FindComponentByTag<DashboardController>("Dashboard");
            _cognitoManager = FindComponentByTag<CognitoManager>("CognitoManager");
            
            if (_cognitoManager == null)
            {
                Debug.LogError("CognitoManager not found in scene. Make sure CognitoManager is attached to a GameObject with tag 'CognitoManager'.");
            }
        }

        private T FindComponentByTag<T>(string tag) where T : Component
        {
            var gameObject = GameObject.FindGameObjectWithTag(tag);
            if (gameObject == null)
            {
                Debug.LogError($"GameObject with tag '{tag}' not found");
                return null;
            }
            
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"Component '{typeof(T).Name}' not found on GameObject with tag '{tag}'");
            }
            
            return component;
        }

        private void SubscribeToCognitoEvents()
        {
            if (_cognitoManager != null)
            {
                _cognitoManager.OnAuthenticationComplete += OnCognitoAuthenticationComplete;
                _cognitoManager.OnRegistrationComplete += OnCognitoRegistrationComplete;
                _cognitoManager.OnPasswordRecoveryComplete += OnCognitoPasswordRecoveryComplete;
            }
        }

        private void UnsubscribeFromCognitoEvents()
        {
            if (_cognitoManager != null)
            {
                _cognitoManager.OnAuthenticationComplete -= OnCognitoAuthenticationComplete;
                _cognitoManager.OnRegistrationComplete -= OnCognitoRegistrationComplete;
                _cognitoManager.OnPasswordRecoveryComplete -= OnCognitoPasswordRecoveryComplete;
            }
        }
        #endregion

        #region Panel Management
        private void ShowPanel(PanelType panelType)
        {
            if (!_panels.TryGetValue(panelType, out var panelInfo))
            {
                Debug.LogError($"Panel type {panelType} not found");
                return;
            }

            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.Flex;
            panelInfo.Panel.AddToClassList(panelInfo.ShowClass);
            _scrim.AddToClassList(SCRIM_FADEIN_CLASS);
            
            _currentActivePanel = panelType;
            ClearMessage();
            Debug.Log($"Panel {panelType} opened");
        }

        private void HidePanel(PanelType panelType)
        {
            if (!_panels.TryGetValue(panelType, out var panelInfo))
            {
                Debug.LogError($"Panel type {panelType} not found");
                return;
            }

            panelInfo.Panel.RemoveFromClassList(panelInfo.ShowClass);
            panelInfo.Panel.AddToClassList(panelInfo.HideClass);
            _scrim.RemoveFromClassList(SCRIM_FADEIN_CLASS);
            
            _currentActivePanel = PanelType.None;
            ClearInputFields();
            Debug.Log($"Panel {panelType} closed");
        }

        private void SwitchPanel(PanelType fromPanel, PanelType toPanel)
        {
            if (fromPanel != PanelType.None)
            {
                HidePanel(fromPanel);
            }
            
            if (toPanel != PanelType.None)
            {
                ShowPanel(toPanel);
            }
        }

        private bool IsAnyPanelVisible()
        {
            foreach (var panelInfo in _panels.Values)
            {
                if (panelInfo.Panel.ClassListContains(panelInfo.ShowClass))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Authentication Methods
        /// <summary>
        /// Maneja el proceso de autenticación con AWS Cognito
        /// </summary>
        private async void HandleAuthentication()
        {
            if (_cognitoManager == null)
            {
                ShowMessage("Authentication service not available", true);
                return;
            }

            string username = _usernameLoginField.value?.Trim();
            string password = _passwordLoginField.value;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowMessage("Please enter both username and password", true);
                return;
            }

            try
            {
                ShowMessage("Authenticating...", false);
                SetLoginButtonEnabled(false);
                
                bool success = await _cognitoManager.SignInAsync(username, password);
                
                if (!success)
                {
                    ShowMessage("Authentication failed. Please check your credentials.", true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Authentication error: {ex.Message}");
                ShowMessage($"Authentication failed: {ex.Message}", true);
            }
            finally
            {
                SetLoginButtonEnabled(true);
            }
        }

        /// <summary>
        /// Maneja el proceso de registro con AWS Cognito
        /// </summary>
        private async void HandleRegisterAndLogin()
        {
            if (_cognitoManager == null)
            {
                ShowMessage("Registration service not available", true);
                return;
            }

            string username = _usernameRegisterField.value?.Trim();
            string email = _emailRegisterField.value?.Trim();
            string password = _passwordRegisterField.value;
            string repeatPassword = _repeatPasswordRegisterField.value;

            // Validaciones
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(repeatPassword))
            {
                ShowMessage("Please fill all fields", true);
                return;
            }

            if (!_cognitoManager.IsValidEmail(email))
            {
                ShowMessage("Please enter a valid email address", true);
                return;
            }

            if (password != repeatPassword)
            {
                ShowMessage("Passwords do not match", true);
                return;
            }

            if (!_cognitoManager.IsValidPassword(password))
            {
                ShowMessage("Password must be at least 8 characters with uppercase, lowercase, and number", true);
                return;
            }

            try
            {
                ShowMessage("Creating account...", false);
                SetRegisterButtonEnabled(false);
                
                bool success = await _cognitoManager.SignUpAsync(username, password, email);
                
                if (!success)
                {
                    ShowMessage("Registration failed. Please try again.", true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Registration error: {ex.Message}");
                ShowMessage($"Registration failed: {ex.Message}", true);
            }
            finally
            {
                SetRegisterButtonEnabled(true);
            }
        }

        /// <summary>
        /// Maneja el proceso de recuperación de contraseña con AWS Cognito
        /// </summary>
        private async void HandlePasswordRecovery()
        {
            if (_cognitoManager == null)
            {
                ShowMessage("Password recovery service not available", true);
                return;
            }

            string email = _emailRecoverField.value?.Trim();

            if (string.IsNullOrEmpty(email))
            {
                ShowMessage("Please enter your email address", true);
                return;
            }

            if (!_cognitoManager.IsValidEmail(email))
            {
                ShowMessage("Please enter a valid email address", true);
                return;
            }

            try
            {
                ShowMessage("Sending recovery email...", false);
                SetRecoverButtonEnabled(false);
                
                bool success = await _cognitoManager.ForgotPasswordAsync(email);
                
                if (!success)
                {
                    ShowMessage("Failed to send recovery email", true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Password recovery error: {ex.Message}");
                ShowMessage($"Password recovery failed: {ex.Message}", true);
            }
            finally
            {
                SetRecoverButtonEnabled(true);
            }
        }
        #endregion

        #region Cognito Event Handlers
        private void OnCognitoAuthenticationComplete(bool success, string message)
        {
            if (success)
            {
                OnAuthenticationSuccess();
            }
            else
            {
                OnAuthenticationFailure(message);
            }
        }

        private void OnCognitoRegistrationComplete(bool success, string message)
        {
            if (success)
            {
                OnRegistrationSuccess(message);
            }
            else
            {
                OnRegistrationFailure(message);
            }
        }

        private void OnCognitoPasswordRecoveryComplete(bool success, string message)
        {
            if (success)
            {
                OnPasswordRecoverySuccess();
            }
            else
            {
                OnPasswordRecoveryFailure(message);
            }
        }

        private void OnAuthenticationSuccess()
        {
            Debug.Log("Authentication successful - navigating to dashboard");
            ShowMessage("Login successful!", false);
            
            // Delay before switching to dashboard
            Invoke(nameof(NavigateToDashboard), 1f);
        }

        private void NavigateToDashboard()
        {
            CloseCurrentPanel();
            HideUi();
            _dashboardController?.ShowUi();
        }

        private void OnAuthenticationFailure(string errorMessage)
        {
            Debug.LogError($"Authentication failed: {errorMessage}");
            ShowMessage($"Login failed: {errorMessage}", true);
        }

        private void OnRegistrationSuccess(string message)
        {
            Debug.Log($"Registration successful: {message}");
            ShowMessage("Registration successful! Please check your email to verify your account.", false);
            
            // Switch to login panel after delay
            Invoke(nameof(SwitchToLoginFromRegister), 2f);
        }

        private void SwitchToLoginFromRegister()
        {
            SwitchPanel(PanelType.Register, PanelType.Login);
        }

        private void OnRegistrationFailure(string errorMessage)
        {
            Debug.LogError($"Registration failed: {errorMessage}");
            ShowMessage($"Registration failed: {errorMessage}", true);
        }

        private void OnPasswordRecoverySuccess()
        {
            Debug.Log("Password recovery email sent successfully");
            ShowMessage("Recovery email sent! Please check your inbox.", false);
            
            // Switch to login panel after delay
            Invoke(nameof(SwitchToLoginFromRecover), 2f);
        }

        private void SwitchToLoginFromRecover()
        {
            SwitchPanel(PanelType.RecoverPassword, PanelType.Login);
        }

        private void OnPasswordRecoveryFailure(string errorMessage)
        {
            Debug.LogError($"Password recovery failed: {errorMessage}");
            ShowMessage($"Recovery failed: {errorMessage}", true);
        }
        #endregion

        #region UI Helper Methods
        private void ShowMessage(string message, bool isError = false)
        {
            if (_messageLabel != null)
            {
                _messageLabel.text = message;
                _messageLabel.style.color = isError ? Color.red : Color.green;
                _messageLabel.style.display = DisplayStyle.Flex;
            }
            
            Debug.Log($"{(isError ? "Error" : "Info")}: {message}");
        }

        private void ClearMessage()
        {
            if (_messageLabel != null)
            {
                _messageLabel.text = "";
                _messageLabel.style.display = DisplayStyle.None;
            }
        }

        private void ClearInputFields()
        {
            // Clear login fields
            if (_usernameLoginField != null) _usernameLoginField.value = "";
            if (_passwordLoginField != null) _passwordLoginField.value = "";
            
            // Clear register fields
            if (_usernameRegisterField != null) _usernameRegisterField.value = "";
            if (_emailRegisterField != null) _emailRegisterField.value = "";
            if (_passwordRegisterField != null) _passwordRegisterField.value = "";
            if (_repeatPasswordRegisterField != null) _repeatPasswordRegisterField.value = "";
            
            // Clear recover field
            if (_emailRecoverField != null) _emailRecoverField.value = "";
        }

        private void SetLoginButtonEnabled(bool enabled)
        {
            if (_loginButton != null)
            {
                _loginButton.SetEnabled(enabled);
                _loginButton.text = enabled ? "Login" : "Logging in...";
            }
        }

        private void SetRegisterButtonEnabled(bool enabled)
        {
            if (_registerAndLoginButton != null)
            {
                _registerAndLoginButton.SetEnabled(enabled);
                _registerAndLoginButton.text = enabled ? "Register and Login" : "Creating account...";
            }
        }

        private void SetRecoverButtonEnabled(bool enabled)
        {
            if (_recoverPasswordButton != null)
            {
                _recoverPasswordButton.SetEnabled(enabled);
                _recoverPasswordButton.text = enabled ? "Recover" : "Sending email...";
            }
        }
        #endregion

        #region Event Handlers - Main
        private void OnLaunchButtonClicked(ClickEvent evt)
        {
            OpenPanel(PanelType.Login);
        }

        private void OnExitApplicationClicked(ClickEvent evt)
        {
            Debug.Log("Application exit requested");
            Application.Quit();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        private void OnPanelTransitionComplete(TransitionEndEvent evt)
        {
            if (!IsAnyPanelVisible())
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
                Debug.Log("All panels closed - hiding container");
            }
        }
        #endregion

        #region Event Handlers - Login Panel
        private void OnLoginButtonClicked(ClickEvent evt)
        {
            HandleAuthentication();
        }

        private void OnCloseLoginPanelClicked(ClickEvent evt)
        {
            HidePanel(PanelType.Login);
        }

        private void OnRegisterFromLoginClicked(ClickEvent evt)
        {
            SwitchPanel(PanelType.Login, PanelType.Register);
        }

        private void OnRecoverPasswordFromLoginClicked(ClickEvent evt)
        {
            SwitchPanel(PanelType.Login, PanelType.RecoverPassword);
        }
        #endregion

        #region Event Handlers - Register Panel
        private void OnRegisterAndLoginClicked(ClickEvent evt)
        {
            HandleRegisterAndLogin();
        }

        private void OnCloseRegisterPanelClicked(ClickEvent evt)
        {
            HidePanel(PanelType.Register);
        }

        private void OnBackToLoginFromRegisterClicked(ClickEvent evt)
        {
            SwitchPanel(PanelType.Register, PanelType.Login);
        }
        #endregion

        #region Event Handlers - Recover Password Panel
        private void OnRecoverPasswordClicked(ClickEvent evt)
        {
            HandlePasswordRecovery();
        }

        private void OnCloseRecoverPasswordPanelClicked(ClickEvent evt)
        {
            HidePanel(PanelType.RecoverPassword);
        }

        private void OnBackToLoginFromRecoverPasswordClicked(ClickEvent evt)
        {
            SwitchPanel(PanelType.RecoverPassword, PanelType.Login);
        }
        #endregion

        #region Event Registration
        private void RegisterAllEvents()
        {
            RegisterMainEvents();
            RegisterLoginPanelEvents();
            RegisterRegisterPanelEvents();
            RegisterRecoverPasswordPanelEvents();
        }

        private void RegisterMainEvents()
        {
            _launchButton.RegisterCallback<ClickEvent>(OnLaunchButtonClicked);
            _exitAppButton.RegisterCallback<ClickEvent>(OnExitApplicationClicked);
        }

        private void RegisterLoginPanelEvents()
        {
            _loginButton.RegisterCallback<ClickEvent>(OnLoginButtonClicked);
            _closeLoginPanelButton.RegisterCallback<ClickEvent>(OnCloseLoginPanelClicked);
            _registerLoginButton.RegisterCallback<ClickEvent>(OnRegisterFromLoginClicked);
            _recoverPasswordLoginButton.RegisterCallback<ClickEvent>(OnRecoverPasswordFromLoginClicked);
        }

        private void RegisterRegisterPanelEvents()
        {
            _registerAndLoginButton.RegisterCallback<ClickEvent>(OnRegisterAndLoginClicked);
            _closeRegisterPanelButton.RegisterCallback<ClickEvent>(OnCloseRegisterPanelClicked);
            _backToLoginPanelFromRegisterButton.RegisterCallback<ClickEvent>(OnBackToLoginFromRegisterClicked);
        }

        private void RegisterRecoverPasswordPanelEvents()
        {
            _recoverPasswordButton.RegisterCallback<ClickEvent>(OnRecoverPasswordClicked);
            _closeRecoverPasswordButton.RegisterCallback<ClickEvent>(OnCloseRecoverPasswordPanelClicked);
            _backToLoginPanelFromRecoverPasswordButton.RegisterCallback<ClickEvent>(OnBackToLoginFromRecoverPasswordClicked);
        }
        #endregion

        #region UI Component Retrieval
        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            
            GetMainComponents(root);
            GetPanelComponents(root);
            GetLoginPanelComponents(root);
            GetRegisterPanelComponents(root);
            GetRecoverPasswordPanelComponents(root);
        }

        private void GetMainComponents(VisualElement root)
        {
            _body = root.Q<VisualElement>("Body");
            _subpanelsAndSmokeMaskContainer = root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _scrim = root.Q<VisualElement>("Scrim");
            _launchButton = root.Q<Button>("LaunchButton");
            _exitAppButton = root.Q<Button>("ExitButton");
            _messageLabel = root.Q<Label>("MessageLabel"); // Opcional: para mostrar mensajes
        }

        private void GetPanelComponents(VisualElement root)
        {
            _loginPanel = root.Q<VisualElement>("LoginPanel");
            _registerPanel = root.Q<VisualElement>("RegisterPanel");
            _recoverPasswordPanel = root.Q<VisualElement>("RecoverPasswordPanel");
        }

        private void GetLoginPanelComponents(VisualElement root)
        {
            _closeLoginPanelButton = root.Q<Button>("CloseLoginButton");
            _registerLoginButton = root.Q<Button>("RegisterLoginButton");
            _recoverPasswordLoginButton = root.Q<Button>("RecoverPasswordLoginButton");
            _loginButton = root.Q<Button>("LoginButton");
            _usernameLoginField = root.Q<TextField>("UsernameLoginField");
            _passwordLoginField = root.Q<TextField>("PasswordLoginField");
        }

        private void GetRegisterPanelComponents(VisualElement root)
        {
            _closeRegisterPanelButton = root.Q<Button>("CloseRegisterPanelButton");
            _registerAndLoginButton = root.Q<Button>("RegisterAndLoginButton");
            _backToLoginPanelFromRegisterButton = root.Q<Button>("BackToLoginPanelFromRegisterButton");
            _usernameRegisterField = root.Q<TextField>("UsernameRegisterField");
            _emailRegisterField = root.Q<TextField>("EmailRegisterField");
            _passwordRegisterField = root.Q<TextField>("PasswordRegisterField");
            _repeatPasswordRegisterField = root.Q<TextField>("RepeatPasswordRegisterField");
        }

        private void GetRecoverPasswordPanelComponents(VisualElement root)
        {
            _closeRecoverPasswordButton = root.Q<Button>("CloseRecoverPasswordButton");
            _recoverPasswordButton = root.Q<Button>("RecoverPasswordButton");
            _backToLoginPanelFromRecoverPasswordButton = root.Q<Button>("BackToLoginPanelFromRecoverPasswordButton");
            _emailRecoverField = root.Q<TextField>("EmailRecoverField");
        }
        #endregion

        #region Data Classes
        [System.Serializable]
        private class PanelInfo
        {
            public VisualElement Panel;
            public string ShowClass;
            public string HideClass;
        }
        #endregion
    }
}