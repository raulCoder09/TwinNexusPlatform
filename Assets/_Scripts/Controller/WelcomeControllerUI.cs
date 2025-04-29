using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class WelcomeControllerUI : MonoBehaviour
    {
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private Button _launchButton;
        private Button _closeLoginPanelButton;
        private Button _closeRegisterPanelButton;
        private Button _exitAppButton;
        private VisualElement _loginPanel;
        private VisualElement _scrim;
        private VisualElement _registerPanel;

        private Button _registerLoginButton;
        private Button _recoverPasswordLoginButton;
        private Button _loginButton;

        private Button _backToLoginPanelButton;

        
        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _subpanelsAndSmokeMaskContainer = root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _launchButton = root.Q<Button>("LaunchButton");
            _closeLoginPanelButton = root.Q<Button>("CloseLoginButton");
            _exitAppButton=root.Q<Button>("ExitButton");
            _loginPanel = root.Q<VisualElement>("LoginPanel");
            _registerPanel= root.Q<VisualElement>("RegisterPanel");
            _scrim = root.Q<VisualElement>("Scrim");
            _registerLoginButton=root.Q<Button>("RegisterLoginButton");
            _recoverPasswordLoginButton=root.Q<Button>("RecoverPasswordLoginButton");
            _loginButton=root.Q<Button>("LoginButton");
            _backToLoginPanelButton=root.Q<Button>("BackToLoginPanelButton");
            _closeRegisterPanelButton=root.Q<Button>("CloseRegisterPanelButton");

            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;

            _launchButton.RegisterCallback<ClickEvent>(OpenLoginPanel);
            _closeLoginPanelButton.RegisterCallback<ClickEvent>(CloseLoginPanel);
            _exitAppButton.RegisterCallback<ClickEvent>(ExitAplication);
            _registerLoginButton.RegisterCallback<ClickEvent>(CloseLoginPanelAndOpenRegisterPanel);
            _recoverPasswordLoginButton.RegisterCallback<ClickEvent>(CloseLoginPanel);
            _loginButton.RegisterCallback<ClickEvent>(CloseLoginPanel);
            _closeRegisterPanelButton.RegisterCallback<ClickEvent>(CloseRegisterPanel);
                
            
            _backToLoginPanelButton.RegisterCallback<ClickEvent>(CloseRegisterPanelAndOpenLoginPanel);
            _loginPanel.RegisterCallback<TransitionEndEvent>(OnLoginPanelTransitionComplete);
            _registerPanel.RegisterCallback<TransitionEndEvent>(OnLoginRegisterTransitionComplete);
        }

        private void OnLoginRegisterTransitionComplete(TransitionEndEvent evt)
        {
            if (!_loginPanel.ClassListContains("LoginPanelMoveB") && !_registerPanel.ClassListContains("RegisterPanelInMainScreen"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }


        private void CloseRegisterPanelAndOpenLoginPanel(ClickEvent evt)
        {
            CloseRegisterPanel(evt);
            OpenLoginPanel(evt);
        }

        private void CloseLoginPanelAndOpenRegisterPanel(ClickEvent evt)
        {
            CloseLoginPanel(evt);
            OpenRegisterPanel();
        }

        private void ExitAplication(ClickEvent evt)
        {
            Application.Quit();
        }

        private void OpenLoginPanel(ClickEvent evt)
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.Flex;
            _loginPanel.AddToClassList("LoginPanelMoveB");
            _scrim.AddToClassList("ScrimFadein");
        }

        private void CloseLoginPanel(ClickEvent evt)
        {
            _loginPanel.RemoveFromClassList("LoginPanelMoveB");
            _loginPanel.AddToClassList("LoginPanelMoveA"); 
            _scrim.RemoveFromClassList("ScrimFadein");
        }

        private void OnLoginPanelTransitionComplete(TransitionEndEvent evt) 
        {

            if (!_loginPanel.ClassListContains("LoginPanelMoveB") && !_registerPanel.ClassListContains("RegisterPanelInMainScreen"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }

        private void OpenRegisterPanel()
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.Flex;
            _registerPanel.AddToClassList("RegisterPanelInMainScreen");
            _scrim.AddToClassList("ScrimFadein");
        }

        private void CloseRegisterPanel(ClickEvent evt)
        {
            _registerPanel.RemoveFromClassList("RegisterPanelInMainScreen");
            _registerPanel.AddToClassList("RegisterPanelOutMainScreen"); 
            _scrim.RemoveFromClassList("ScrimFadein");
        }
        
    }
}