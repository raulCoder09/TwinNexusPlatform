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

        public WelcomeEventManager(WelcomeUIManager uiManager, Action onExitApplication, Action<IWelcomeOps.PanelType> onPanelTransitionComplete)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _onExitApplication = onExitApplication ?? throw new ArgumentNullException(nameof(onExitApplication));
            _onPanelTransitionComplete = onPanelTransitionComplete ?? throw new ArgumentNullException(nameof(onPanelTransitionComplete));
        }

        public void RegisterEvents(UIDocument uiDocument)
        {
            var root = uiDocument.rootVisualElement;

            root.Q<Button>("LaunchButton")?.RegisterCallback<ClickEvent>(OnLaunchButtonClicked);
            root.Q<Button>("ExitButton")?.RegisterCallback<ClickEvent>(OnExitApplicationClicked);
            root.Q<Button>("LoginButton")?.RegisterCallback<ClickEvent>(OnLoginButtonClicked);
            root.Q<Button>("CloseLoginButton")?.RegisterCallback<ClickEvent>(OnCloseLoginPanelClicked);
            root.Q<Button>("RegisterLoginButton")?.RegisterCallback<ClickEvent>(OnRegisterFromLoginClicked);
            root.Q<Button>("RecoverPasswordLoginButton")?.RegisterCallback<ClickEvent>(OnRecoverPasswordFromLoginClicked);
            root.Q<Button>("RegisterAndLoginButton")?.RegisterCallback<ClickEvent>(OnRegisterAndLoginClicked);
            root.Q<Button>("CloseRegisterPanelButton")?.RegisterCallback<ClickEvent>(OnCloseRegisterPanelClicked);
            root.Q<Button>("BackToLoginPanelFromRegisterButton")?.RegisterCallback<ClickEvent>(OnBackToLoginFromRegisterClicked);
            root.Q<Button>("RecoverPasswordButton")?.RegisterCallback<ClickEvent>(OnRecoverPasswordClicked);
            root.Q<Button>("CloseRecoverPasswordButton")?.RegisterCallback<ClickEvent>(OnCloseRecoverPasswordPanelClicked);
            root.Q<Button>("BackToLoginPanelFromRecoverPasswordButton")?.RegisterCallback<ClickEvent>(OnBackToLoginFromRecoverPasswordClicked);
            root.Q<Button>("VerifyEmailButton")?.RegisterCallback<ClickEvent>(OnVerifyEmailClicked);
            root.Q<Button>("ResendVerificationCodeButton")?.RegisterCallback<ClickEvent>(OnResendVerificationCodeClicked);
            root.Q<Button>("CloseEmailVerificationButton")?.RegisterCallback<ClickEvent>(OnCloseEmailVerificationPanelClicked);
            root.Q<Button>("BackToLoginFromVerificationButton")?.RegisterCallback<ClickEvent>(OnBackToLoginFromVerificationClicked);

            foreach (var panelData in _uiManager.GetPanelData())
            {
                panelData.Panel?.RegisterCallback<TransitionEndEvent>(evt => OnPanelTransitionComplete(_uiManager.CurrentActivePanel));
            }
        }

        private void OnLaunchButtonClicked(ClickEvent evt)
        {
            _uiManager.NavigateToPanel(IWelcomeOps.PanelType.Login);
        }

        private void OnExitApplicationClicked(ClickEvent evt)
        {
            _onExitApplication?.Invoke();
        }

        private void OnLoginButtonClicked(ClickEvent evt)
        {
            // Este evento será manejado por WelcomeOrchestrator para autenticación
            Debug.LogWarning("Login button clicked - handle authentication in orchestrator");
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

        private void OnRegisterAndLoginClicked(ClickEvent evt)
        {
            // Este evento será manejado por WelcomeOrchestrator para registro
            Debug.LogWarning("Register and Login button clicked - handle registration in orchestrator");
        }

        private void OnCloseRegisterPanelClicked(ClickEvent evt)
        {
            _uiManager.CloseCurrentPanel();
        }

        private void OnBackToLoginFromRegisterClicked(ClickEvent evt)
        {
            _uiManager.SwitchPanel(IWelcomeOps.PanelType.Register, IWelcomeOps.PanelType.Login);
        }

        private void OnRecoverPasswordClicked(ClickEvent evt)
        {
            // Este evento será manejado por WelcomeOrchestrator para recuperación
            Debug.LogWarning("Recover Password button clicked - handle recovery in orchestrator");
        }

        private void OnCloseRecoverPasswordPanelClicked(ClickEvent evt)
        {
            _uiManager.CloseCurrentPanel();
        }

        private void OnBackToLoginFromRecoverPasswordClicked(ClickEvent evt)
        {
            _uiManager.SwitchPanel(IWelcomeOps.PanelType.RecoverPassword, IWelcomeOps.PanelType.Login);
        }

        private void OnVerifyEmailClicked(ClickEvent evt)
        {
            // Este evento será manejado por WelcomeOrchestrator para verificación
            Debug.LogWarning("Verify Email button clicked - handle verification in orchestrator");
        }

        private void OnResendVerificationCodeClicked(ClickEvent evt)
        {
            // Este evento será manejado por WelcomeOrchestrator para reenvío
            Debug.LogWarning("Resend Verification Code button clicked - handle resend in orchestrator");
        }

        private void OnCloseEmailVerificationPanelClicked(ClickEvent evt)
        {
            _uiManager.CloseCurrentPanel();
        }

        private void OnBackToLoginFromVerificationClicked(ClickEvent evt)
        {
            _uiManager.SwitchPanel(IWelcomeOps.PanelType.EmailVerification, IWelcomeOps.PanelType.Login);
        }

        private void OnPanelTransitionComplete(IWelcomeOps.PanelType panelType)
        {
            if (!_uiManager.IsAnyPanelVisible())
            {
                _uiManager.HideUi();
            }
            _onPanelTransitionComplete?.Invoke(panelType);
        }
    }
}