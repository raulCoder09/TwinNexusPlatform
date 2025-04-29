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
        private Button _closeRecoverPasswordButton;
        private Button _exitAppButton;
        private VisualElement _loginPanel;
        private VisualElement _scrim;
        private VisualElement _registerPanel;
        private VisualElement _recoverPasswordPanel;
        
        

        private Button _registerLoginButton;
        private Button _recoverPasswordLoginButton;
        private Button _loginButton;
        private Button _registerAndLoginButton;
        private Button _recoverPasswordButton;

        private Button _backToLoginPanelFromRegisterButton;
        private Button _backToLoginPanelFromRecoverPasswordButton;

        
        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _subpanelsAndSmokeMaskContainer = root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _launchButton = root.Q<Button>("LaunchButton");
            _closeLoginPanelButton = root.Q<Button>("CloseLoginButton");
            _exitAppButton=root.Q<Button>("ExitButton");
            _loginPanel = root.Q<VisualElement>("LoginPanel");
            _registerPanel= root.Q<VisualElement>("RegisterPanel");
            _recoverPasswordPanel=root.Q<VisualElement>("RecoverPasswordPanel");
            _scrim = root.Q<VisualElement>("Scrim");
            _registerLoginButton=root.Q<Button>("RegisterLoginButton");
            _recoverPasswordLoginButton=root.Q<Button>("RecoverPasswordLoginButton");
            _loginButton=root.Q<Button>("LoginButton");
            _backToLoginPanelFromRegisterButton=root.Q<Button>("BackToLoginPanelFromRegisterButton");
            _backToLoginPanelFromRecoverPasswordButton=root.Q<Button>("BackToLoginPanelFromRecoverPasswordButton");
            _closeRegisterPanelButton=root.Q<Button>("CloseRegisterPanelButton");
            _closeRecoverPasswordButton=root.Q<Button>("CloseRecoverPasswordButton");
            _registerAndLoginButton=root.Q<Button>("RegisterAndLoginButton");
            _recoverPasswordButton=root.Q<Button>("RecoverPasswordButton");

            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;

            _launchButton.RegisterCallback<ClickEvent>(OpenLoginPanel);
            _closeLoginPanelButton.RegisterCallback<ClickEvent>(CloseLoginPanel);
            _exitAppButton.RegisterCallback<ClickEvent>(ExitAplication);
            _registerLoginButton.RegisterCallback<ClickEvent>(CloseLoginPanelAndOpenRegisterPanel);
            _recoverPasswordLoginButton.RegisterCallback<ClickEvent>(CloseLoginPanelAndOpenRecoverPasswordPanel);
            _loginButton.RegisterCallback<ClickEvent>(Authentication);
            _closeRegisterPanelButton.RegisterCallback<ClickEvent>(CloseRegisterPanel);
            _closeRecoverPasswordButton.RegisterCallback<ClickEvent>(CloseRecoverPasswordPanel);
            _registerAndLoginButton.RegisterCallback<ClickEvent>(RegisterAndLogin);
            _recoverPasswordButton.RegisterCallback<ClickEvent>(RecoverPassword);
            
            _backToLoginPanelFromRegisterButton.RegisterCallback<ClickEvent>(CloseRegisterPanelAndOpenLoginPanel);
            _backToLoginPanelFromRecoverPasswordButton.RegisterCallback<ClickEvent>(CloseRecoverPasswordPanelAndOpenLoginPanel);
            _loginPanel.RegisterCallback<TransitionEndEvent>(OnLoginPanelTransitionComplete);
            _registerPanel.RegisterCallback<TransitionEndEvent>(OnRegisterTransitionComplete);
            _recoverPasswordPanel.RegisterCallback<TransitionEndEvent>(OnRecoverPasswordTransitionComplete);
        }

        private void RecoverPassword(ClickEvent evt)
        {
            print("recover password");
            // todo
        }

        private void RegisterAndLogin(ClickEvent evt)
        {
            // todo
            print("se hace el registro y se inicia sesion");
            Authentication(evt);
            
        }

        private void Authentication(ClickEvent evt)
        {
            // todo
            print("Logica para autentificar e ir al dashboard o mandar error de incio de sesion");
        }

        private void OnRecoverPasswordTransitionComplete(TransitionEndEvent evt)
        {
            if (!_loginPanel.ClassListContains("LoginPanelMoveB") &&
                !_registerPanel.ClassListContains("RegisterPanelInMainScreen")&&
                !_recoverPasswordPanel.ClassListContains("RecoverPasswordPanelInMainScreen"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }


        private void OnRegisterTransitionComplete(TransitionEndEvent evt)
        {
            if (!_loginPanel.ClassListContains("LoginPanelMoveB") &&
                !_registerPanel.ClassListContains("RegisterPanelInMainScreen")&&
                !_recoverPasswordPanel.ClassListContains("RecoverPasswordPanelInMainScreen"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }


        private void CloseRegisterPanelAndOpenLoginPanel(ClickEvent evt)
        {
            CloseRegisterPanel(evt);
            OpenLoginPanel(evt);
            
        }
        
        private void CloseRecoverPasswordPanelAndOpenLoginPanel(ClickEvent evt)
        {
            CloseRecoverPasswordPanel(evt);
            OpenLoginPanel(evt);
        }



        private void CloseLoginPanelAndOpenRegisterPanel(ClickEvent evt)
        {
            CloseLoginPanel(evt);
            OpenRegisterPanel();
        }
        private void CloseLoginPanelAndOpenRecoverPasswordPanel(ClickEvent evt)
        {
            CloseLoginPanel(evt);
            OpenRecoverPasswordPanel();
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

            if (!_loginPanel.ClassListContains("LoginPanelMoveB") &&
                !_registerPanel.ClassListContains("RegisterPanelInMainScreen") &&
                !_recoverPasswordPanel.ClassListContains("RecoverPasswordPanelInMainScreen"))
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
        
        private void OpenRecoverPasswordPanel()
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.Flex;
            _recoverPasswordPanel.AddToClassList("RecoverPasswordPanelInMainScreen");
            _scrim.AddToClassList("ScrimFadein");
        }
        
        private void CloseRecoverPasswordPanel(ClickEvent evt)
        {
            _recoverPasswordPanel.RemoveFromClassList("RecoverPasswordPanelInMainScreen");
            _recoverPasswordPanel.AddToClassList("RecoverPasswordPanelOutMainScreen"); 
            _scrim.RemoveFromClassList("ScrimFadein");
        }
        
    }
}