using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class DashboardController : MonoBehaviour
    {
        private static VisualElement _body;
        private Button _menuButton;
        private Button _hideMenuButton;
        private Button _logoutButton;
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private VisualElement _navigationMenuPanel;
        private VisualElement _scrim;
        private UIDocument _welcomeUIDocumentdocument;
        private VisualElement _welcomeBody;
        private Button _operationsButton;
        

        private void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _menuButton=root.Q<Button>("MenuButton");
            _menuButton.RegisterCallback<ClickEvent>(ShowMenu);
            _logoutButton=root.Q<Button>("LogoutButton");
            _subpanelsAndSmokeMaskContainer=root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            
            _navigationMenuPanel=root.Q<VisualElement>("NavigationMenuPanel");
            _scrim = root.Q<VisualElement>("Scrim");
            
            _hideMenuButton=root.Q<Button>("HideMenuButton");
            _hideMenuButton.RegisterCallback<ClickEvent>(HideMenu);
            _logoutButton.RegisterCallback<ClickEvent>(Logout);
            
            _navigationMenuPanel.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);
            
            _welcomeUIDocumentdocument=GameObject.Find("Welcome").GetComponent<UIDocument>();
            var welcomeRoot = _welcomeUIDocumentdocument.rootVisualElement;
            _welcomeBody=welcomeRoot.Q<VisualElement>("Body");
            _operationsButton=root.Q<Button>("OperationsButton");
            _operationsButton.RegisterCallback<ClickEvent>(StartOperations);
        }

        private void StartOperations(ClickEvent evt)
        {
            _body.style.display = DisplayStyle.None;
            HideMenu(evt);
            DeviceSelectionController.ShowUi();

        }

        private void Logout(ClickEvent evt)
        {
            HideMenu(evt);
            _body.style.display = DisplayStyle.None;
            _welcomeBody.style.display = DisplayStyle.Flex;
        }

        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            if (!_navigationMenuPanel.ClassListContains("NavigationMenuPanelinMainScreen"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }

        private void HideMenu(ClickEvent evt)
        {
            _navigationMenuPanel.RemoveFromClassList("NavigationMenuPanelinMainScreen");
            _scrim.RemoveFromClassList("ScrimOpaque");
        }

        private void ShowMenu(ClickEvent evt)
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.Flex;
            _navigationMenuPanel.AddToClassList("NavigationMenuPanelinMainScreen");
            _scrim.AddToClassList("ScrimOpaque");
        }
        
        internal static void ShowUi()
        {
            _body.style.display = DisplayStyle.Flex;
        }

        private void Start()
        {
            _body.style.display = DisplayStyle.None;
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
        }
    }
}
