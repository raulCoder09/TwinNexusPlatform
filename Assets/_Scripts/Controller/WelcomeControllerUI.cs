using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
        #endregion

        #region UI Components - Register Panel
        private Button _closeRegisterPanelButton;
        private Button _registerAndLoginButton;
        private Button _backToLoginPanelFromRegisterButton;
        #endregion

        #region UI Components - Recover Password Panel
        private Button _closeRecoverPasswordButton;
        private Button _recoverPasswordButton;
        private Button _backToLoginPanelFromRecoverPasswordButton;
        #endregion

        #region Dependencies
        private DashboardController _dashboardController;
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
        /// Maneja el proceso de autenticación
        /// </summary>
        private void HandleAuthentication()
        {
            try
            {
                // TODO: Implementar lógica de autenticación real
                Debug.Log("Authentication process started");
                
                // Simular autenticación exitosa
                bool authenticationSuccessful = true; // TODO: Reemplazar con lógica real
                
                if (authenticationSuccessful)
                {
                    OnAuthenticationSuccess();
                }
                else
                {
                    OnAuthenticationFailure("Invalid credentials");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Authentication error: {ex.Message}");
                OnAuthenticationFailure($"Authentication failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja el proceso de registro y login automático
        /// </summary>
        private void HandleRegisterAndLogin()
        {
            try
            {
                // TODO: Implementar lógica de registro real
                Debug.Log("Registration and login process started");
                
                // Simular registro exitoso
                bool registrationSuccessful = true; // TODO: Reemplazar con lógica real
                
                if (registrationSuccessful)
                {
                    Debug.Log("Registration successful, proceeding with automatic login");
                    OnAuthenticationSuccess();
                }
                else
                {
                    OnRegistrationFailure("Registration failed");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Registration error: {ex.Message}");
                OnRegistrationFailure($"Registration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja el proceso de recuperación de contraseña
        /// </summary>
        private void HandlePasswordRecovery()
        {
            try
            {
                // TODO: Implementar lógica de recuperación de contraseña real
                Debug.Log("Password recovery process started");
                
                // Simular envío de email de recuperación
                bool recoveryEmailSent = true; // TODO: Reemplazar con lógica real
                
                if (recoveryEmailSent)
                {
                    OnPasswordRecoverySuccess();
                }
                else
                {
                    OnPasswordRecoveryFailure("Failed to send recovery email");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Password recovery error: {ex.Message}");
                OnPasswordRecoveryFailure($"Password recovery failed: {ex.Message}");
            }
        }

        private void OnAuthenticationSuccess()
        {
            Debug.Log("Authentication successful - navigating to dashboard");
            CloseCurrentPanel();
            HideUi();
            _dashboardController?.ShowUi();
        }

        private void OnAuthenticationFailure(string errorMessage)
        {
            Debug.LogError($"Authentication failed: {errorMessage}");
            // TODO: Mostrar mensaje de error en UI
        }

        private void OnRegistrationFailure(string errorMessage)
        {
            Debug.LogError($"Registration failed: {errorMessage}");
            // TODO: Mostrar mensaje de error en UI
        }

        private void OnPasswordRecoverySuccess()
        {
            Debug.Log("Password recovery email sent successfully");
            // TODO: Mostrar mensaje de confirmación en UI
            SwitchPanel(PanelType.RecoverPassword, PanelType.Login);
        }

        private void OnPasswordRecoveryFailure(string errorMessage)
        {
            Debug.LogError($"Password recovery failed: {errorMessage}");
            // TODO: Mostrar mensaje de error en UI
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
        }

        private void GetRegisterPanelComponents(VisualElement root)
        {
            _closeRegisterPanelButton = root.Q<Button>("CloseRegisterPanelButton");
            _registerAndLoginButton = root.Q<Button>("RegisterAndLoginButton");
            _backToLoginPanelFromRegisterButton = root.Q<Button>("BackToLoginPanelFromRegisterButton");
        }

        private void GetRecoverPasswordPanelComponents(VisualElement root)
        {
            _closeRecoverPasswordButton = root.Q<Button>("CloseRecoverPasswordButton");
            _recoverPasswordButton = root.Q<Button>("RecoverPasswordButton");
            _backToLoginPanelFromRecoverPasswordButton = root.Q<Button>("BackToLoginPanelFromRecoverPasswordButton");
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