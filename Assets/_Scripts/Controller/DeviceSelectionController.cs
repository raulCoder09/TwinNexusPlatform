using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class DeviceSelectionController : MonoBehaviour
    {
        private static VisualElement _body;
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private Button _menuButton;
        private VisualElement _navigationMenuPanel;
        private Button _hideMenuButton;
        private VisualElement _scrim;
        private Button _dashboardButton;

        private void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _menuButton = root.Q<Button>("MenuButton");
            _menuButton.RegisterCallback<ClickEvent>(ShowMenu);
            _subpanelsAndSmokeMaskContainer=root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _navigationMenuPanel = root.Q<VisualElement>("NavigationMenuPanel");
            _hideMenuButton = root.Q<Button>("HideMenuButton");
            _hideMenuButton.RegisterCallback<ClickEvent>(HideMenu);
            _scrim = root.Q<VisualElement>("Scrim");
            _navigationMenuPanel.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);
            _dashboardButton= root.Q<Button>("DashboardButton");
            _dashboardButton.RegisterCallback<ClickEvent>(StartDashboard);
        }
        

        private void Start()
        {
            HideUi();
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
        }

        internal static void ShowUi()
        {
            _body.style.display = DisplayStyle.Flex;
        }

        internal static void HideUi()
        {
            _body.style.display = DisplayStyle.None;
        }

        private void ShowMenu(ClickEvent evt)
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.Flex;
            _scrim.AddToClassList("Opaque");
            _navigationMenuPanel.AddToClassList("NavigationMenuPanelInMainScreen");
        }

        private void HideMenu(ClickEvent evt)
        {
            _navigationMenuPanel.RemoveFromClassList("NavigationMenuPanelInMainScreen");
            _scrim.RemoveFromClassList("Opaque");
        }
        
        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            if (!_navigationMenuPanel.ClassListContains("NavigationMenuPanelInMainScreen"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }
        
        private void StartDashboard(ClickEvent evt)
        {
            HideMenu(evt);
            _body.style.display = DisplayStyle.None;
            DashboardController.ShowUi();
        }
        
    }
}
