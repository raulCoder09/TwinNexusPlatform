using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class WelcomeControllerUI : MonoBehaviour
    {
        private VisualElement _bottomContainer;
        private Button _launchButton;
        private Button _closeButton;
        private Button _exitButton;
        private VisualElement _loginPanel;
        private VisualElement _scrim;

        void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _bottomContainer = root.Q<VisualElement>("BottomContainer");
            _launchButton = root.Q<Button>("LaunchButton");
            _closeButton = root.Q<Button>("CloseButton");
            _exitButton=root.Q<Button>("ExitButton");
            _loginPanel = root.Q<VisualElement>("LoginPanel");
            _scrim = root.Q<VisualElement>("Scrim");

            _bottomContainer.style.display = DisplayStyle.None;

            _launchButton.RegisterCallback<ClickEvent>(OpenSubMenu);
            _closeButton.RegisterCallback<ClickEvent>(CloseSubMenu);
            _exitButton.RegisterCallback<ClickEvent>(ExitAplication);
            _loginPanel.RegisterCallback<TransitionEndEvent>(OnBottomLoginDown);
        }

        private void ExitAplication(ClickEvent evt)
        {
            Application.Quit();
        }

        private void OpenSubMenu(ClickEvent evt)
        {
            _bottomContainer.style.display = DisplayStyle.Flex;
            _loginPanel.AddToClassList("LoginPanelMoveB");
            _scrim.AddToClassList("ScrimFadein");
        }

        private void CloseSubMenu(ClickEvent evt)
        {
            _loginPanel.RemoveFromClassList("LoginPanelMoveB");
            _loginPanel.AddToClassList("LoginPanelMoveA"); 
            _scrim.RemoveFromClassList("ScrimFadein");
        }

        private void OnBottomLoginDown(TransitionEndEvent evt)
        {
            if (!_loginPanel.ClassListContains("LoginPanelMoveB"))
            {
                _bottomContainer.style.display = DisplayStyle.None;
            }
        }

        void Update()
        {
        }
    }
}