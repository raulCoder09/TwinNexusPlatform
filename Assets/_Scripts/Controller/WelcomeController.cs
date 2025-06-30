using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Models;

namespace _Scripts.Controller
{
    public class WelcomeController : MonoBehaviour
    {
        #region UI Components - Main
        private VisualElement _body;
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private VisualElement _scrim;
        private Button _launchButton;
        private Button _exitAppButton;
        private Label _messageLabel;
        private Label _mainTitle; 
        #endregion

        #region UI Components - Panels
        private VisualElement _loginPanel;
        private VisualElement _registerPanel;
        private VisualElement _recoverPasswordPanel;
        private VisualElement _emailVerificationPanel;
        private Label _loginTitle; // NEW: Reference to login panel title
        private Label _registerTitle; // NEW: Reference to register panel title
        private Label _recoverPasswordTitle; // NEW: Reference to recover password title
        private Label _emailVerificationTitle; // NEW: Reference to email verification title
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
        private TextField _phoneRegisterField;
        private TextField _passwordRegisterField;
        private TextField _repeatPasswordRegisterField;
        #endregion

        #region UI Components - Recover Password Panel
        private Button _closeRecoverPasswordButton;
        private Button _recoverPasswordButton;
        private Button _backToLoginPanelFromRecoverPasswordButton;
        private TextField _emailRecoverField;
        #endregion

        #region UI Components - Email Verification Panel
        // NEW: Added UI components for email verification panel
        private Button _closeEmailVerificationButton;
        private Button _verifyEmailButton;
        private Button _resendVerificationCodeButton;
        private Button _backToLoginFromVerificationButton;
        private TextField _verificationCodeField;
        private Label _emailVerificationEmail;
        #endregion

        #region Dependencies
        private DashboardController _dashboardController;
        private OldCognitoManager _oldCognitoManager;
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
            RecoverPassword,
            EmailVerification // NEW: Added for email verification panel
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
            StartGlitchEffect();
        }

        private void OnDestroy()
        {
            UnsubscribeFromCognitoEvents();
            StopAllCoroutines();
        }
        #endregion

        #region Public Methods
        public void ShowUi()
        {
            _body.style.display = DisplayStyle.Flex;
            Debug.Log("Welcome UI shown");
        }

        public void HideUi()
        {
            _body.style.display = DisplayStyle.None;
            Debug.Log("Welcome UI hidden");
        }

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
                },
                // NEW: Added email verification panel
                {
                    PanelType.EmailVerification,
                    new PanelInfo
                    {
                        Panel = _emailVerificationPanel,
                        ShowClass = "EmailVerificationPanelInMainScreen",
                        HideClass = "EmailVerificationPanelOutMainScreen"
                    }
                }
            };

            foreach (var panelInfo in _panels.Values)
            {
                panelInfo.Panel.RegisterCallback<TransitionEndEvent>(OnPanelTransitionComplete);
            }
        }

        private void FindDependencies()
        {
            _dashboardController = FindComponentByTag<DashboardController>("Dashboard");
            _oldCognitoManager = FindComponentByTag<OldCognitoManager>("CognitoManager");
            
            if (_oldCognitoManager == null)
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
            if (_oldCognitoManager != null)
            {
                _oldCognitoManager.OnAuthenticationComplete += OnOldCognitoAuthenticationComplete;
                _oldCognitoManager.OnRegistrationComplete += OnOldCognitoRegistrationComplete;
                _oldCognitoManager.OnPasswordRecoveryComplete += OnOldCognitoPasswordRecoveryComplete;
                // NEW: Subscribe to email verification events
                _oldCognitoManager.OnEmailVerificationComplete += OnOldCognitoEmailVerificationComplete;
                _oldCognitoManager.OnResendVerificationComplete += OnOldCognitoResendVerificationComplete;
            }
        }

        private void UnsubscribeFromCognitoEvents()
        {
            if (_oldCognitoManager != null)
            {
                _oldCognitoManager.OnAuthenticationComplete -= OnOldCognitoAuthenticationComplete;
                _oldCognitoManager.OnRegistrationComplete -= OnOldCognitoRegistrationComplete;
                _oldCognitoManager.OnPasswordRecoveryComplete -= OnOldCognitoPasswordRecoveryComplete;
                // NEW: Unsubscribe from email verification events
                _oldCognitoManager.OnEmailVerificationComplete -= OnOldCognitoEmailVerificationComplete;
                _oldCognitoManager.OnResendVerificationComplete -= OnOldCognitoResendVerificationComplete;
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
            
            // NEW: Update email verification panel with pending email
            if (panelType == PanelType.EmailVerification && _oldCognitoManager != null)
            {
                _emailVerificationEmail.text = _oldCognitoManager.PendingEmail ?? "Unknown email";
            }
            
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
        private async void HandleAuthentication()
        {
            if (_oldCognitoManager == null)
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
                
                bool success = await _oldCognitoManager.SignInAsync(username, password);
                
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

        private async void HandleRegisterAndLogin()
        {
            if (_oldCognitoManager == null)
            {
                ShowMessage("Registration service not available", true);
                return;
            }

            string username = _usernameRegisterField.value?.Trim();
            string email = _emailRegisterField.value?.Trim();
            string phone = _phoneRegisterField.value?.Trim();
            string password = _passwordRegisterField.value;
            string repeatPassword = _repeatPasswordRegisterField.value;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(repeatPassword))
            {
                ShowMessage("Please fill all required fields (username, email, password)", true);
                return;
            }

            if (!_oldCognitoManager.IsValidEmail(email))
            {
                ShowMessage("Please enter a valid email address", true);
                return;
            }

            if (!string.IsNullOrEmpty(phone) && !_oldCognitoManager.IsValidPhoneNumber(phone))
            {
                ShowMessage("Please enter a valid phone number (include country code, e.g., +52 for Mexico)", true);
                return;
            }

            if (password != repeatPassword)
            {
                ShowMessage("Passwords do not match", true);
                return;
            }

            if (!_oldCognitoManager.IsValidPassword(password))
            {
                ShowMessage("Password must be at least 8 characters with uppercase, lowercase, and number", true);
                return;
            }

            try
            {
                ShowMessage("Creating account...", false);
                SetRegisterButtonEnabled(false);
                
                bool success;
                if (!string.IsNullOrEmpty(phone))
                {
                    success = await _oldCognitoManager.SignUpAsync(username, password, email, phone);
                }
                else
                {
                    success = await _oldCognitoManager.SignUpAsync(username, password, email);
                }
                
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

        private async void HandlePasswordRecovery()
        {
            if (_oldCognitoManager == null)
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

            if (!_oldCognitoManager.IsValidEmail(email))
            {
                ShowMessage("Please enter a valid email address", true);
                return;
            }

            try
            {
                ShowMessage("Sending recovery email...", false);
                SetRecoverButtonEnabled(false);
                
                bool success = await _oldCognitoManager.ForgotPasswordAsync(email);
                
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

        // NEW: Handle email verification code submission
        private async void HandleEmailVerification()
        {
            if (_oldCognitoManager == null)
            {
                ShowMessage("Verification service not available", true);
                return;
            }

            string verificationCode = _verificationCodeField.value?.Trim();

            if (string.IsNullOrEmpty(verificationCode))
            {
                ShowMessage("Please enter the verification code", true);
                return;
            }

            if (!_oldCognitoManager.IsValidVerificationCode(verificationCode))
            {
                ShowMessage("Please enter a valid 6-digit verification code", true);
                return;
            }

            try
            {
                ShowMessage("Verifying email...", false);
                SetVerifyEmailButtonEnabled(false);
                
                bool success = await _oldCognitoManager.ConfirmSignUpAsync(_oldCognitoManager.CleanVerificationCode(verificationCode));
                
                if (!success)
                {
                    ShowMessage("Email verification failed. Please try again.", true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Email verification error: {ex.Message}");
                ShowMessage($"Verification failed: {ex.Message}", true);
            }
            finally
            {
                SetVerifyEmailButtonEnabled(true);
            }
        }

        // NEW: Handle resending verification code
        private async void HandleResendVerificationCode()
        {
            if (_oldCognitoManager == null)
            {
                ShowMessage("Verification service not available", true);
                return;
            }

            try
            {
                ShowMessage("Resending verification code...", false);
                SetResendCodeButtonEnabled(false);
                
                bool success = await _oldCognitoManager.ResendConfirmationCodeAsync();
                
                if (!success)
                {
                    ShowMessage("Failed to resend verification code", true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Resend verification code error: {ex.Message}");
                ShowMessage($"Resend failed: {ex.Message}", true);
            }
            finally
            {
                SetResendCodeButtonEnabled(true);
            }
        }
        #endregion

        #region Cognito Event Handlers
        private void OnOldCognitoAuthenticationComplete(bool success, string message)
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

        private void OnOldCognitoRegistrationComplete(bool success, string message)
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

        private void OnOldCognitoPasswordRecoveryComplete(bool success, string message)
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

        // NEW: Handle email verification completion
        private void OnOldCognitoEmailVerificationComplete(bool success, string message)
        {
            if (success)
            {
                OnEmailVerificationSuccess(message);
            }
            else
            {
                OnEmailVerificationFailure(message);
            }
        }

        // NEW: Handle resend verification code completion
        private void OnOldCognitoResendVerificationComplete(bool success, string message)
        {
            if (success)
            {
                OnResendVerificationSuccess(message);
            }
            else
            {
                OnResendVerificationFailure(message);
            }
        }

        private void OnAuthenticationSuccess()
        {
            Debug.Log("Authentication successful - navigating to dashboard");
            ShowMessage("Login successful!", false);
            
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
            ShowMessage("Registration successful! Please verify your email.", false);
            
            // NEW: Switch to email verification panel if verification is pending
            if (_oldCognitoManager.HasPendingVerification())
            {
                Invoke(nameof(SwitchToEmailVerification), 2f);
            }
            else
            {
                Invoke(nameof(SwitchToLoginFromRegister), 2f);
            }
        }

        // NEW: Switch to email verification panel
        private void SwitchToEmailVerification()
        {
            SwitchPanel(PanelType.Register, PanelType.EmailVerification);
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

        // NEW: Handle successful email verification
        private void OnEmailVerificationSuccess(string message)
        {
            Debug.Log($"Email verification successful: {message}");
            ShowMessage("Email verified successfully! You can now log in.", false);
            
            Invoke(nameof(SwitchToLoginFromVerification), 2f);
        }

        // NEW: Switch to login panel from verification
        private void SwitchToLoginFromVerification()
        {
            SwitchPanel(PanelType.EmailVerification, PanelType.Login);
        }

        // NEW: Handle email verification failure
        private void OnEmailVerificationFailure(string errorMessage)
        {
            Debug.LogError($"Email verification failed: {errorMessage}");
            ShowMessage($"Verification failed: {errorMessage}", true);
        }

        // NEW: Handle successful resend verification code
        private void OnResendVerificationSuccess(string message)
        {
            Debug.Log($"Resend verification code successful: {message}");
            ShowMessage("Verification code resent! Please check your email.", false);
        }

        // NEW: Handle resend verification code failure
        private void OnResendVerificationFailure(string errorMessage)
        {
            Debug.LogError($"Resend verification code failed: {errorMessage}");
            ShowMessage($"Resend failed: {errorMessage}", true);
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
            if (_usernameLoginField != null) _usernameLoginField.value = "";
            if (_passwordLoginField != null) _passwordLoginField.value = "";
            
            if (_usernameRegisterField != null) _usernameRegisterField.value = "";
            if (_emailRegisterField != null) _emailRegisterField.value = "";
            if (_phoneRegisterField != null) _phoneRegisterField.value = "";
            if (_passwordRegisterField != null) _passwordRegisterField.value = "";
            if (_repeatPasswordRegisterField != null) _repeatPasswordRegisterField.value = "";
            
            if (_emailRecoverField != null) _emailRecoverField.value = "";
            
            // NEW: Clear verification code field
            if (_verificationCodeField != null) _verificationCodeField.value = "";
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

        // NEW: Enable/disable verify email button
        private void SetVerifyEmailButtonEnabled(bool enabled)
        {
            if (_verifyEmailButton != null)
            {
                _verifyEmailButton.SetEnabled(enabled);
                _verifyEmailButton.text = enabled ? "Verify Email" : "Verifying...";
            }
        }

        // NEW: Enable/disable resend code button
        private void SetResendCodeButtonEnabled(bool enabled)
        {
            if (_resendVerificationCodeButton != null)
            {
                _resendVerificationCodeButton.SetEnabled(enabled);
                _resendVerificationCodeButton.text = enabled ? "Resend Code" : "Resending...";
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

        #region Event Handlers - Email Verification Panel
        // NEW: Event handlers for email verification panel
        private void OnVerifyEmailClicked(ClickEvent evt)
        {
            HandleEmailVerification();
        }

        private void OnResendVerificationCodeClicked(ClickEvent evt)
        {
            HandleResendVerificationCode();
        }

        private void OnCloseEmailVerificationPanelClicked(ClickEvent evt)
        {
            HidePanel(PanelType.EmailVerification);
        }

        private void OnBackToLoginFromVerificationClicked(ClickEvent evt)
        {
            SwitchPanel(PanelType.EmailVerification, PanelType.Login);
        }
        #endregion

        #region Event Registration
        private void RegisterAllEvents()
        {
            RegisterMainEvents();
            RegisterLoginPanelEvents();
            RegisterRegisterPanelEvents();
            RegisterRecoverPasswordPanelEvents();
            RegisterEmailVerificationPanelEvents(); // NEW: Register email verification events
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

        // NEW: Register events for email verification panel
        private void RegisterEmailVerificationPanelEvents()
        {
            _verifyEmailButton.RegisterCallback<ClickEvent>(OnVerifyEmailClicked);
            _resendVerificationCodeButton.RegisterCallback<ClickEvent>(OnResendVerificationCodeClicked);
            _closeEmailVerificationButton.RegisterCallback<ClickEvent>(OnCloseEmailVerificationPanelClicked);
            _backToLoginFromVerificationButton.RegisterCallback<ClickEvent>(OnBackToLoginFromVerificationClicked);
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
            GetEmailVerificationPanelComponents(root); // NEW: Retrieve email verification components
        }

        private void GetMainComponents(VisualElement root)
        {
            _body = root.Q<VisualElement>("Body");
            _subpanelsAndSmokeMaskContainer = root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _scrim = root.Q<VisualElement>("Scrim");
            _launchButton = root.Q<Button>("LaunchButton");
            _exitAppButton = root.Q<Button>("ExitButton");
            _messageLabel = root.Q<Label>("MessageLabel");
            _mainTitle = root.Q<Label>("Title");
        }

        private void GetPanelComponents(VisualElement root)
        {
            _loginPanel = root.Q<VisualElement>("LoginPanel");
            _registerPanel = root.Q<VisualElement>("RegisterPanel");
            _recoverPasswordPanel = root.Q<VisualElement>("RecoverPasswordPanel");
            _emailVerificationPanel = root.Q<VisualElement>("EmailVerificationPanel"); 
            _loginTitle = root.Q<Label>("TitleLogin"); // NEW: Get login title
            _registerTitle = root.Q<Label>("TitleRegister"); // NEW: Get register title
            _recoverPasswordTitle = root.Q<Label>("TitleRecoverPassword"); // NEW: Get recover title
            _emailVerificationTitle = root.Q<Label>("TitleEmailVerification"); // NEW: Get verification title// NEW: Retrieve email verification panel
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
            _phoneRegisterField = root.Q<TextField>("PhoneRegisterField");
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

        // NEW: Retrieve components for email verification panel
        private void GetEmailVerificationPanelComponents(VisualElement root)
        {
            _closeEmailVerificationButton = root.Q<Button>("CloseEmailVerificationButton");
            _verifyEmailButton = root.Q<Button>("VerifyEmailButton");
            _resendVerificationCodeButton = root.Q<Button>("ResendVerificationCodeButton");
            _backToLoginFromVerificationButton = root.Q<Button>("BackToLoginFromVerificationButton");
            _verificationCodeField = root.Q<TextField>("VerificationCodeField");
            _emailVerificationEmail = root.Q<Label>("EmailVerificationEmail");
        }
        #endregion
        #region Glitch Effect
        private const string GLITCH_CLASS = "glitch"; // NEW: Constant for glitch class

        private void StartGlitchEffect()
        {
            StartCoroutine(GlitchTitle(_mainTitle, 3f)); // Glitch for main title
            StartCoroutine(GlitchTitle(_loginTitle, 4f)); // Glitch for login title
            StartCoroutine(GlitchTitle(_registerTitle, 4f)); // Glitch for register title
            StartCoroutine(GlitchTitle(_recoverPasswordTitle, 4f)); // Glitch for recover title
            StartCoroutine(GlitchTitle(_emailVerificationTitle, 4f)); // Glitch for verification title
        }

        private IEnumerator GlitchTitle(Label title, float interval)
        {
            while (true)
            {
                if (title != null)
                {
                    title.AddToClassList(GLITCH_CLASS);
                    yield return new WaitForSeconds(0.2f); // Duration of glitch
                    title.RemoveFromClassList(GLITCH_CLASS);
                }
                yield return new WaitForSeconds(interval); // Wait before next glitch
            }
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