using System;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        private Button _arscaraButton;
        private Button _robotKit1Button;
        private Button _robotKit2Button;
        private GameManager _gameManager;
        private DashboardController _dashboardController;

        
        private void Awake()
        {
            GetUiComponents();
            RegisterEvents();
            FindObjects();
        }

        
        private void LaunchDevice(ClickEvent evt)
        {
            HideUi();
            if (evt.currentTarget is Button button)
                switch (button.name)
                {
                    case "ARSCARAButton":
                        _gameManager.deviceSelected = button.name;
                        break;
                    case "RobotKit1Button":
                        _gameManager.deviceSelected = button.name;
                        break;
                    case "RobotKit2Button":
                        _gameManager.deviceSelected = button.name;
                        break;
                    default:
                        break;
                }

            switch (_gameManager.modeSelected)
            {
                case "OperationsButton":
                    SceneManager.LoadScene("Operations");
                break;
                case "TrainingButton":
                    SceneManager.LoadScene("Training");
                    break;
            }
            
            
        }


        private void Start()
        {
            FindObjects();
            HideUi();
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
        }

        internal void ShowUi()
        {
            _body.style.display = DisplayStyle.Flex;
        }

        internal void HideUi()
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
            _dashboardController.ShowUi();
        }

        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _menuButton = root.Q<Button>("MenuButton");
            _subpanelsAndSmokeMaskContainer=root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _navigationMenuPanel = root.Q<VisualElement>("NavigationMenuPanel");
            _hideMenuButton = root.Q<Button>("HideMenuButton");
            _scrim = root.Q<VisualElement>("Scrim");
            _dashboardButton= root.Q<Button>("DashboardButton");
            _robotKit2Button= root.Q<Button>("RobotKit2Button");
            _robotKit1Button= root.Q<Button>("RobotKit1Button");
            _arscaraButton= root.Q<Button>("ARSCARAButton");
        }

        private void RegisterEvents()
        {
            _menuButton.RegisterCallback<ClickEvent>(ShowMenu);
            _hideMenuButton.RegisterCallback<ClickEvent>(HideMenu);
            _navigationMenuPanel.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);
            _dashboardButton.RegisterCallback<ClickEvent>(StartDashboard);
            _arscaraButton.RegisterCallback<ClickEvent>(LaunchDevice);
            _robotKit1Button.RegisterCallback<ClickEvent>(LaunchDevice);
            _robotKit2Button.RegisterCallback<ClickEvent>(LaunchDevice);
        }
        private void FindObjects()
        {
            _gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
            _dashboardController=GameObject.FindGameObjectWithTag("Dashboard").GetComponent<DashboardController>();
            
        }
    }
}
