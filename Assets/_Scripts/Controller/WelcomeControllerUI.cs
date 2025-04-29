using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class WelcomeControllerUI : MonoBehaviour
    {
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private Button _launchButton;
        private Button _closeLoginPanelButton;
        private Button _exitAppButton;
        private VisualElement _loginPanel;
        private VisualElement _scrim;

        private Button _registerLoginButton;
        private Button _recoverPasswordLoginButton;
        private Button _loginButton;
        
        // RegisterLoginButton
        //     RecoverPasswordLoginButton
        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _subpanelsAndSmokeMaskContainer = root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _launchButton = root.Q<Button>("LaunchButton");
            _closeLoginPanelButton = root.Q<Button>("CloseButton");
            _exitAppButton=root.Q<Button>("ExitButton");
            _loginPanel = root.Q<VisualElement>("LoginPanel");
            _scrim = root.Q<VisualElement>("Scrim");
            _registerLoginButton=root.Q<Button>("RegisterLoginButton");
            _recoverPasswordLoginButton=root.Q<Button>("RecoverPasswordLoginButton");
            _loginButton=root.Q<Button>("LoginButton");
            

            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;

            _launchButton.RegisterCallback<ClickEvent>(OpenLoginPanel);
            _closeLoginPanelButton.RegisterCallback<ClickEvent>(CloseLoginPanel);
            _exitAppButton.RegisterCallback<ClickEvent>(ExitAplication);
            _registerLoginButton.RegisterCallback<ClickEvent>(CloseLoginPanelAndOpenRegisterPanel);
            _recoverPasswordLoginButton.RegisterCallback<ClickEvent>(CloseLoginPanel);
            _loginButton.RegisterCallback<ClickEvent>(CloseLoginPanel);
                
            _loginPanel.RegisterCallback<TransitionEndEvent>(OnLoginPanelTransitionComplete);
        }

        private void CloseLoginPanelAndOpenRegisterPanel(ClickEvent evt)
        {
            CloseLoginPanel(evt);
            // si se detecto que la animacion del loginpanel ya termino
            //     se debe abrir el panel register es decir si se dio click en el boton register y la animacion de closeloginpanel termino 
            //         el metodo OpenRegisterPanel(evt); debe ejecutarse

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

        private void OnLoginPanelTransitionComplete(TransitionEndEvent evt)  //revisar es el nombe mas adecuado
        {

            if (!_loginPanel.ClassListContains("LoginPanelMoveB"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }

        void Update()
        {
        }
    }
}