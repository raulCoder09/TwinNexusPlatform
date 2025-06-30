namespace _Scripts.Controllers.WelcomeController
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class WelcomeUIManager
    {
        private WelcomeInfo.UIConfiguration _uiConfig;
        private IWelcomeOps.PanelType _currentActivePanel = IWelcomeOps.PanelType.None;
        private const string SCRIM_FADEIN_CLASS = "ScrimFadein";

        public WelcomeUIManager(WelcomeInfo.UIConfiguration uiConfig)
        {
            _uiConfig = uiConfig ?? throw new ArgumentNullException(nameof(uiConfig));
        }

        public void InitializePanelSystem()
        {
            _uiConfig.SubpanelsContainer.style.display = DisplayStyle.None;
            ShowUi();
            ClearMessage();
        }

        public void ShowUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.Flex;
        }

        public void HideUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.None;
        }

        public void ShowPanel(IWelcomeOps.PanelType panelType)
        {
            if (!_uiConfig.Panels.TryGetValue(panelType, out var panelData))
            {
                Debug.LogError($"Panel type {panelType} not found");
                return;
            }

            _uiConfig.SubpanelsContainer.style.display = DisplayStyle.Flex;
            panelData.Panel.AddToClassList(panelData.ShowClass);
            _uiConfig.Scrim.AddToClassList(SCRIM_FADEIN_CLASS);

            if (panelType == IWelcomeOps.PanelType.EmailVerification)
            {
                var emailLabel = _uiConfig.Panels[panelType].Panel.Q<Label>("EmailVerificationEmail");
                if (emailLabel != null) emailLabel.text = "Unknown email"; // Placeholder, actualizar con datos reales
            }

            _currentActivePanel = panelType;
            Debug.Log($"Panel {panelType} opened");
        }

        public void HidePanel(IWelcomeOps.PanelType panelType)
        {
            if (!_uiConfig.Panels.TryGetValue(panelType, out var panelData))
            {
                Debug.LogError($"Panel type {panelType} not found");
                return;
            }

            panelData.Panel.RemoveFromClassList(panelData.ShowClass);
            panelData.Panel.AddToClassList(panelData.HideClass);
            _uiConfig.Scrim.RemoveFromClassList(SCRIM_FADEIN_CLASS);
            Debug.Log($"Panel {panelType} closed");
        }

        public void CloseCurrentPanel()
        {
            if (_uiConfig.Panels.ContainsKey(_currentActivePanel))
            {
                HidePanel(_currentActivePanel);
                _currentActivePanel = IWelcomeOps.PanelType.None;
                ClearInputFields();
            }
        }

        public void SwitchPanel(IWelcomeOps.PanelType fromPanel, IWelcomeOps.PanelType toPanel)
        {
            if (fromPanel != IWelcomeOps.PanelType.None)
            {
                HidePanel(fromPanel);
            }
            if (toPanel != IWelcomeOps.PanelType.None)
            {
                ShowPanel(toPanel);
            }
        }

        public bool IsAnyPanelVisible()
        {
            foreach (var panelData in _uiConfig.Panels.Values)
            {
                if (panelData.Panel.ClassListContains(panelData.ShowClass))
                {
                    return true;
                }
            }
            return false;
        }

        public void NavigateToPanel(IWelcomeOps.PanelType panelType)
        {
            ShowPanel(panelType);
        }

        public IEnumerable<WelcomeInfo.UIConfiguration.PanelData> GetPanelData()
        {
            return _uiConfig.Panels.Values;
        }

        public IWelcomeOps.PanelType CurrentActivePanel
        {
            get { return _currentActivePanel; }
            set { _currentActivePanel = value; }
        }

        public void ConfigureGlitchTitles()
        {
            _uiConfig.Panels[IWelcomeOps.PanelType.None].Panel.Q<Label>("Title")?.AddToClassList("glitch");
            _uiConfig.Panels[IWelcomeOps.PanelType.Login].Panel.Q<Label>("TitleLogin")?.AddToClassList("glitch");
            _uiConfig.Panels[IWelcomeOps.PanelType.Register].Panel.Q<Label>("TitleRegister")?.AddToClassList("glitch");
            _uiConfig.Panels[IWelcomeOps.PanelType.RecoverPassword].Panel.Q<Label>("TitleRecoverPassword")?.AddToClassList("glitch");
            _uiConfig.Panels[IWelcomeOps.PanelType.EmailVerification].Panel.Q<Label>("TitleEmailVerification")?.AddToClassList("glitch");
        }

        private void ShowMessage(string message, bool isError = false)
        {
            var messageLabel = _uiConfig.Body.Q<Label>("MessageLabel");
            if (messageLabel != null)
            {
                messageLabel.text = message;
                messageLabel.style.color = isError ? Color.red : Color.green;
                messageLabel.style.display = DisplayStyle.Flex;
            }
            Debug.Log($"{(isError ? "Error" : "Info")}: {message}");
        }

        private void ClearMessage()
        {
            var messageLabel = _uiConfig.Body.Q<Label>("MessageLabel");
            if (messageLabel != null)
            {
                messageLabel.text = "";
                messageLabel.style.display = DisplayStyle.None;
            }
        }

        private void ClearInputFields()
        {
            var root = _uiConfig.Body;
            root.Q<TextField>("UsernameLoginField")?.SetValueWithoutNotify("");
            root.Q<TextField>("PasswordLoginField")?.SetValueWithoutNotify("");
            root.Q<TextField>("UsernameRegisterField")?.SetValueWithoutNotify("");
            root.Q<TextField>("EmailRegisterField")?.SetValueWithoutNotify("");
            root.Q<TextField>("PhoneRegisterField")?.SetValueWithoutNotify("");
            root.Q<TextField>("PasswordRegisterField")?.SetValueWithoutNotify("");
            root.Q<TextField>("RepeatPasswordRegisterField")?.SetValueWithoutNotify("");
            root.Q<TextField>("EmailRecoverField")?.SetValueWithoutNotify("");
            root.Q<TextField>("VerificationCodeField")?.SetValueWithoutNotify("");
        }

        private void SetLoginButtonEnabled(bool enabled)
        {
            var button = _uiConfig.Body.Q<Button>("LoginButton");
            if (button != null)
            {
                button.SetEnabled(enabled);
                button.text = enabled ? "Login" : "Logging in...";
            }
        }

        private void SetRegisterButtonEnabled(bool enabled)
        {
            var button = _uiConfig.Body.Q<Button>("RegisterAndLoginButton");
            if (button != null)
            {
                button.SetEnabled(enabled);
                button.text = enabled ? "Register and Login" : "Creating account...";
            }
        }

        private void SetRecoverButtonEnabled(bool enabled)
        {
            var button = _uiConfig.Body.Q<Button>("RecoverPasswordButton");
            if (button != null)
            {
                button.SetEnabled(enabled);
                button.text = enabled ? "Recover" : "Sending email...";
            }
        }

        private void SetVerifyEmailButtonEnabled(bool enabled)
        {
            var button = _uiConfig.Body.Q<Button>("VerifyEmailButton");
            if (button != null)
            {
                button.SetEnabled(enabled);
                button.text = enabled ? "Verify Email" : "Verifying...";
            }
        }

        private void SetResendCodeButtonEnabled(bool enabled)
        {
            var button = _uiConfig.Body.Q<Button>("ResendVerificationCodeButton");
            if (button != null)
            {
                button.SetEnabled(enabled);
                button.text = enabled ? "Resend Code" : "Resending...";
            }
        }
    }
}