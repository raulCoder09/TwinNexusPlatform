namespace _Scripts.Controllers.WelcomeController
{
    using System;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class WelcomeEventManager
    {
        private WelcomeUIManager _uiManager;
        private Action _onExitApplication;
        private Action<IWelcomeOps.PanelType> _onPanelTransitionComplete;
        private WelcomeOrchestrator _orchestrator;

        // ✅ AGREGADO: Referencias para poder desregistrar eventos
        private UIDocument _uiDocument;
        private VisualElement _root;

        public WelcomeEventManager(WelcomeUIManager uiManager, Action onExitApplication, Action<IWelcomeOps.PanelType> onPanelTransitionComplete, WelcomeOrchestrator orchestrator)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _onExitApplication = onExitApplication ?? throw new ArgumentNullException(nameof(onExitApplication));
            _onPanelTransitionComplete = onPanelTransitionComplete ?? throw new ArgumentNullException(nameof(onPanelTransitionComplete));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        public void RegisterEvents(UIDocument uiDocument)
        {
            // ✅ AGREGADO: Guardar referencias para desregistro posterior
            _uiDocument = uiDocument;
            _root = uiDocument.rootVisualElement;

            // Eventos principales
            _root.Q<Button>("LaunchButton")?.RegisterCallback<ClickEvent>(OnLaunchButtonClicked);
            _root.Q<Button>("ExitButton")?.RegisterCallback<ClickEvent>(OnExitApplicationClicked);
            _root.Q<Button>("SettingsCognitoParametersButtonButton")?.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);

            // Eventos de autenticación - AHORA CONECTADOS
            _root.Q<Button>("LoginButton")?.RegisterCallback<ClickEvent>(OnLoginButtonClicked);
            _root.Q<Button>("RegisterAndLoginButton")?.RegisterCallback<ClickEvent>(OnRegisterAndLoginClicked);
            _root.Q<Button>("RecoverPasswordButton")?.RegisterCallback<ClickEvent>(OnRecoverPasswordClicked);
            _root.Q<Button>("VerifyEmailButton")?.RegisterCallback<ClickEvent>(OnVerifyEmailClicked);
            _root.Q<Button>("ResendVerificationCodeButton")?.RegisterCallback<ClickEvent>(OnResendVerificationCodeClicked);

            // Eventos de navegación
            _root.Q<Button>("CloseLoginButton")?.RegisterCallback<ClickEvent>(OnCloseLoginPanelClicked);
            _root.Q<Button>("RegisterLoginButton")?.RegisterCallback<ClickEvent>(OnRegisterFromLoginClicked);
            _root.Q<Button>("RecoverPasswordLoginButton")?.RegisterCallback<ClickEvent>(OnRecoverPasswordFromLoginClicked);
            _root.Q<Button>("CloseRegisterPanelButton")?.RegisterCallback<ClickEvent>(OnCloseRegisterPanelClicked);
            _root.Q<Button>("BackToLoginPanelFromRegisterButton")?.RegisterCallback<ClickEvent>(OnBackToLoginFromRegisterClicked);
            _root.Q<Button>("CloseRecoverPasswordButton")?.RegisterCallback<ClickEvent>(OnCloseRecoverPasswordPanelClicked);
            _root.Q<Button>("BackToLoginPanelFromRecoverPasswordButton")?.RegisterCallback<ClickEvent>(OnBackToLoginFromRecoverPasswordClicked);
            _root.Q<Button>("CloseEmailVerificationButton")?.RegisterCallback<ClickEvent>(OnCloseEmailVerificationPanelClicked);
            _root.Q<Button>("BackToLoginFromVerificationButton")?.RegisterCallback<ClickEvent>(OnBackToLoginFromVerificationClicked);
            _root.Q<Button>("CloseSettingsCognitoParametersButton")?.RegisterCallback<ClickEvent>(OnCloseSettingsPanelClicked);
            _root.Q<Button>("SaveButton")?.RegisterCallback<ClickEvent>(OnSaveSettingsButtonClicked);

            // Registrar eventos de Enter key para campos de texto
            RegisterEnterKeyEvents(_root);

            Debug.Log("[WelcomeEventManager] All events registered successfully");
        }

        // ✅ AGREGADO: Método para desregistrar eventos
        public void UnregisterEvents()
        {
            try
            {
                Debug.Log("[WelcomeEventManager] Unregistering Welcome events...");

                if (_root == null)
                {
                    Debug.LogWarning("[WelcomeEventManager] Root element is null - cannot unregister events");
                    return;
                }

                // Eventos principales
                _root.Q<Button>("LaunchButton")?.UnregisterCallback<ClickEvent>(OnLaunchButtonClicked);
                _root.Q<Button>("ExitButton")?.UnregisterCallback<ClickEvent>(OnExitApplicationClicked);
                _root.Q<Button>("SettingsCognitoParametersButtonButton")?.UnregisterCallback<ClickEvent>(OnSettingsButtonClicked);

                // Eventos de autenticación
                _root.Q<Button>("LoginButton")?.UnregisterCallback<ClickEvent>(OnLoginButtonClicked);
                _root.Q<Button>("RegisterAndLoginButton")?.UnregisterCallback<ClickEvent>(OnRegisterAndLoginClicked);
                _root.Q<Button>("RecoverPasswordButton")?.UnregisterCallback<ClickEvent>(OnRecoverPasswordClicked);
                _root.Q<Button>("VerifyEmailButton")?.UnregisterCallback<ClickEvent>(OnVerifyEmailClicked);
                _root.Q<Button>("ResendVerificationCodeButton")?.UnregisterCallback<ClickEvent>(OnResendVerificationCodeClicked);

                // Eventos de navegación
                _root.Q<Button>("CloseLoginButton")?.UnregisterCallback<ClickEvent>(OnCloseLoginPanelClicked);
                _root.Q<Button>("RegisterLoginButton")?.UnregisterCallback<ClickEvent>(OnRegisterFromLoginClicked);
                _root.Q<Button>("RecoverPasswordLoginButton")?.UnregisterCallback<ClickEvent>(OnRecoverPasswordFromLoginClicked);
                _root.Q<Button>("CloseRegisterPanelButton")?.UnregisterCallback<ClickEvent>(OnCloseRegisterPanelClicked);
                _root.Q<Button>("BackToLoginPanelFromRegisterButton")?.UnregisterCallback<ClickEvent>(OnBackToLoginFromRegisterClicked);
                _root.Q<Button>("CloseRecoverPasswordButton")?.UnregisterCallback<ClickEvent>(OnCloseRecoverPasswordPanelClicked);
                _root.Q<Button>("BackToLoginPanelFromRecoverPasswordButton")?.UnregisterCallback<ClickEvent>(OnBackToLoginFromRecoverPasswordClicked);
                _root.Q<Button>("CloseEmailVerificationButton")?.UnregisterCallback<ClickEvent>(OnCloseEmailVerificationPanelClicked);
                _root.Q<Button>("BackToLoginFromVerificationButton")?.UnregisterCallback<ClickEvent>(OnBackToLoginFromVerificationClicked);
                _root.Q<Button>("CloseSettingsCognitoParametersButton")?.UnregisterCallback<ClickEvent>(OnCloseSettingsPanelClicked);
                _root.Q<Button>("SaveButton")?.UnregisterCallback<ClickEvent>(OnSaveSettingsButtonClicked);

                // Desregistrar eventos de Enter key
                UnregisterEnterKeyEvents(_root);

                Debug.Log("[WelcomeEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WelcomeEventManager] Error unregistering events: {ex.Message}");
            }
        }

        // ✅ AGREGADO: Método Cleanup consistente con DashboardEventManager
        public void Cleanup()
        {
            UnregisterEvents();
            
            _uiManager = null;
            _orchestrator = null;
            _uiDocument = null;
            _root = null;
            _onExitApplication = null;
            _onPanelTransitionComplete = null;

            Debug.Log("[WelcomeEventManager] Event Manager cleaned up");
        }

        private void RegisterEnterKeyEvents(VisualElement root)
        {
            // Login panel - Enter en password ejecuta login
            var loginPasswordField = root.Q<TextField>("PasswordLoginField");
            loginPasswordField?.RegisterCallback<KeyDownEvent>(OnLoginPasswordKeyDown);

            // Register panel - Enter en repeat password ejecuta registro
            var registerRepeatPasswordField = root.Q<TextField>("RepeatPasswordRegisterField");
            registerRepeatPasswordField?.RegisterCallback<KeyDownEvent>(OnRegisterRepeatPasswordKeyDown);

            // Recovery panel - Enter en email ejecuta recovery
            var recoveryEmailField = root.Q<TextField>("EmailRecoverField");
            recoveryEmailField?.RegisterCallback<KeyDownEvent>(OnRecoveryEmailKeyDown);

            // Verification panel - Enter en código ejecuta verificación
            var verificationCodeField = root.Q<TextField>("VerificationCodeField");
            verificationCodeField?.RegisterCallback<KeyDownEvent>(OnVerificationCodeKeyDown);
        }

        // ✅ AGREGADO: Método para desregistrar eventos de teclado
        private void UnregisterEnterKeyEvents(VisualElement root)
        {
            var loginPasswordField = root.Q<TextField>("PasswordLoginField");
            loginPasswordField?.UnregisterCallback<KeyDownEvent>(OnLoginPasswordKeyDown);

            var registerRepeatPasswordField = root.Q<TextField>("RepeatPasswordRegisterField");
            registerRepeatPasswordField?.UnregisterCallback<KeyDownEvent>(OnRegisterRepeatPasswordKeyDown);

            var recoveryEmailField = root.Q<TextField>("EmailRecoverField");
            recoveryEmailField?.UnregisterCallback<KeyDownEvent>(OnRecoveryEmailKeyDown);

            var verificationCodeField = root.Q<TextField>("VerificationCodeField");
            verificationCodeField?.UnregisterCallback<KeyDownEvent>(OnVerificationCodeKeyDown);
        }

        // ✅ AGREGADO: Métodos específicos de eventos de teclado para poder desregistrarlos
        private void OnLoginPasswordKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                _orchestrator.HandleLoginButtonClick();
            }
        }

        private void OnRegisterRepeatPasswordKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                _orchestrator.HandleRegisterButtonClick();
            }
        }

        private void OnRecoveryEmailKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                _orchestrator.HandleRecoverPasswordButtonClick();
            }
        }

        private void OnVerificationCodeKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                _orchestrator.HandleVerifyEmailButtonClick();
            }
        }

        #region Navigation Events
        private void OnLaunchButtonClicked(ClickEvent evt)
        {
            _uiManager.NavigateToPanel(IWelcomeOps.PanelType.Login);
        }

        private void OnExitApplicationClicked(ClickEvent evt)
        {
            _onExitApplication?.Invoke();
        }

        private void OnCloseLoginPanelClicked(ClickEvent evt)
        {
            _uiManager.CloseCurrentPanel();
        }

        private void OnRegisterFromLoginClicked(ClickEvent evt)
        {
            _uiManager.SwitchPanel(IWelcomeOps.PanelType.Login, IWelcomeOps.PanelType.Register);
        }

        private void OnRecoverPasswordFromLoginClicked(ClickEvent evt)
        {
            _uiManager.SwitchPanel(IWelcomeOps.PanelType.Login, IWelcomeOps.PanelType.RecoverPassword);
        }

        private void OnCloseRegisterPanelClicked(ClickEvent evt)
        {
            _uiManager.CloseCurrentPanel();
        }

        private void OnBackToLoginFromRegisterClicked(ClickEvent evt)
        {
            _uiManager.SwitchPanel(IWelcomeOps.PanelType.Register, IWelcomeOps.PanelType.Login);
        }

        private void OnCloseRecoverPasswordPanelClicked(ClickEvent evt)
        {
            _uiManager.CloseCurrentPanel();
        }

        private void OnBackToLoginFromRecoverPasswordClicked(ClickEvent evt)
        {
            _uiManager.SwitchPanel(IWelcomeOps.PanelType.RecoverPassword, IWelcomeOps.PanelType.Login);
        }

        private void OnCloseEmailVerificationPanelClicked(ClickEvent evt)
        {
            _uiManager.CloseCurrentPanel();
        }

        private void OnBackToLoginFromVerificationClicked(ClickEvent evt)
        {
            _uiManager.SwitchPanel(IWelcomeOps.PanelType.EmailVerification, IWelcomeOps.PanelType.Login);
        }
        private void OnSettingsButtonClicked(ClickEvent evt)
        {
            _uiManager.NavigateToPanel(IWelcomeOps.PanelType.SettingsCognito);
        }

        private void OnCloseSettingsPanelClicked(ClickEvent evt)
        {
            _uiManager.CloseCurrentPanel();
        }
        private void OnSaveSettingsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Save Settings button clicked");
            _orchestrator.HandleSaveSettingsButtonClick();
        }
        #endregion

        #region Authentication Events - CONECTADOS CON COGNITO
        private void OnLoginButtonClicked(ClickEvent evt)
        {
            Debug.Log("Login button clicked - executing authentication");
            _orchestrator.HandleLoginButtonClick();
        }

        private void OnRegisterAndLoginClicked(ClickEvent evt)
        {
            Debug.Log("Register button clicked - executing registration");
            _orchestrator.HandleRegisterButtonClick();
        }

        private void OnRecoverPasswordClicked(ClickEvent evt)
        {
            Debug.Log("Recover password button clicked - executing recovery");
            _orchestrator.HandleRecoverPasswordButtonClick();
        }

        private void OnVerifyEmailClicked(ClickEvent evt)
        {
            Debug.Log("Verify email button clicked - executing verification");
            _orchestrator.HandleVerifyEmailButtonClick();
        }

        private void OnResendVerificationCodeClicked(ClickEvent evt)
        {
            Debug.Log("Resend verification code button clicked - executing resend");
            _orchestrator.HandleResendCodeButtonClick();
        }
        #endregion

        private void OnPanelTransitionComplete(IWelcomeOps.PanelType panelType)
        {
            _onPanelTransitionComplete?.Invoke(panelType);
        }

        #region Public Properties

        /// <summary>
        /// UI Manager asociado
        /// </summary>
        public WelcomeUIManager UIManager => _uiManager;

        /// <summary>
        /// Orchestrator asociado
        /// </summary>
        public WelcomeOrchestrator Orchestrator => _orchestrator;

        #endregion
    }
}