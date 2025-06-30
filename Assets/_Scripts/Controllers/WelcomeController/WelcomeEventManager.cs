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

        public WelcomeEventManager(WelcomeUIManager uiManager, Action onExitApplication, Action<IWelcomeOps.PanelType> onPanelTransitionComplete, WelcomeOrchestrator orchestrator)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _onExitApplication = onExitApplication ?? throw new ArgumentNullException(nameof(onExitApplication));
            _onPanelTransitionComplete = onPanelTransitionComplete ?? throw new ArgumentNullException(nameof(onPanelTransitionComplete));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        public void RegisterEvents(UIDocument uiDocument)
        {
            var root = uiDocument.rootVisualElement;

            // Eventos principales
            root.Q<Button>("LaunchButton")?.RegisterCallback<ClickEvent>(OnLaunchButtonClicked);
            root.Q<Button>("ExitButton")?.RegisterCallback<ClickEvent>(OnExitApplicationClicked);

            // Eventos de autenticación - AHORA CONECTADOS
            root.Q<Button>("LoginButton")?.RegisterCallback<ClickEvent>(OnLoginButtonClicked);
            root.Q<Button>("RegisterAndLoginButton")?.RegisterCallback<ClickEvent>(OnRegisterAndLoginClicked);
            root.Q<Button>("RecoverPasswordButton")?.RegisterCallback<ClickEvent>(OnRecoverPasswordClicked);
            root.Q<Button>("VerifyEmailButton")?.RegisterCallback<ClickEvent>(OnVerifyEmailClicked);
            root.Q<Button>("ResendVerificationCodeButton")?.RegisterCallback<ClickEvent>(OnResendVerificationCodeClicked);

            // Eventos de navegación
            root.Q<Button>("CloseLoginButton")?.RegisterCallback<ClickEvent>(OnCloseLoginPanelClicked);
            root.Q<Button>("RegisterLoginButton")?.RegisterCallback<ClickEvent>(OnRegisterFromLoginClicked);
            root.Q<Button>("RecoverPasswordLoginButton")?.RegisterCallback<ClickEvent>(OnRecoverPasswordFromLoginClicked);
            root.Q<Button>("CloseRegisterPanelButton")?.RegisterCallback<ClickEvent>(OnCloseRegisterPanelClicked);
            root.Q<Button>("BackToLoginPanelFromRegisterButton")?.RegisterCallback<ClickEvent>(OnBackToLoginFromRegisterClicked);
            root.Q<Button>("CloseRecoverPasswordButton")?.RegisterCallback<ClickEvent>(OnCloseRecoverPasswordPanelClicked);
            root.Q<Button>("BackToLoginPanelFromRecoverPasswordButton")?.RegisterCallback<ClickEvent>(OnBackToLoginFromRecoverPasswordClicked);
            root.Q<Button>("CloseEmailVerificationButton")?.RegisterCallback<ClickEvent>(OnCloseEmailVerificationPanelClicked);
            root.Q<Button>("BackToLoginFromVerificationButton")?.RegisterCallback<ClickEvent>(OnBackToLoginFromVerificationClicked);

            // Registrar eventos de Enter key para campos de texto
            RegisterEnterKeyEvents(root);
        }

        private void RegisterEnterKeyEvents(VisualElement root)
        {
            // Login panel - Enter en password ejecuta login
            var loginPasswordField = root.Q<TextField>("PasswordLoginField");
            loginPasswordField?.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    _orchestrator.HandleLoginButtonClick();
                }
            });

            // Register panel - Enter en repeat password ejecuta registro
            var registerRepeatPasswordField = root.Q<TextField>("RepeatPasswordRegisterField");
            registerRepeatPasswordField?.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    _orchestrator.HandleRegisterButtonClick();
                }
            });

            // Recovery panel - Enter en email ejecuta recovery
            var recoveryEmailField = root.Q<TextField>("EmailRecoverField");
            recoveryEmailField?.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    _orchestrator.HandleRecoverPasswordButtonClick();
                }
            });

            // Verification panel - Enter en código ejecuta verificación
            var verificationCodeField = root.Q<TextField>("VerificationCodeField");
            verificationCodeField?.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    _orchestrator.HandleVerifyEmailButtonClick();
                }
            });
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
    }
}