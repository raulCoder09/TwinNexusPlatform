using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class DashboardController : MonoBehaviour
    {
        private VisualElement _body;
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private VisualElement _navigationMenuPanel;
        private VisualElement _scrim;
        
        private Button _menuButton;
        private Button _hideMenuButton;
        private Button _logoutButton;
        private Button _operationsButton;
        private Button _trainingButton;
        private Button _settingsButton;
        
        private WelcomeControllerUI _welcomeController;
        private DeviceSelectionController _deviceSelectionController;
        private SettingsController _settingsController;
        private GameManager _gameManager;

        private Label _localIoTStatusLabel;
        private Label _localIoTModeLabel;
        private Label _vMIoTStatusLabel;
        private Label _vMIoTModeLabel;
        private Label _cloudIoTStatusLabel;
        private Label _cloudlIoTModeLabel;

        public Label localIoTStatusLabel
        {
            get => _localIoTStatusLabel;
            set => _localIoTStatusLabel = value;
        }

        public Label localIoTModeLabel
        {
            get => _localIoTModeLabel;
            set => _localIoTModeLabel = value;
        }

        public Label vMIoTStatusLabel
        {
            get => _vMIoTStatusLabel;
            set => _vMIoTStatusLabel = value;
        }

        public Label vMIoTModeLabel
        {
            get => _vMIoTModeLabel;
            set => _vMIoTModeLabel = value;
        }

        public Label cloudIoTStatusLabel
        {
            get => _cloudIoTStatusLabel;
            set => _cloudIoTStatusLabel = value;
        }

        public Label cloudlIoTModeLabel
        {
            get => _cloudlIoTModeLabel;
            set => _cloudlIoTModeLabel = value;
        }

        private void Awake()
        {
            GetUiComponents();
            RegisterEvents();
            FindObjects();
        }
        
        private void Start()
        {
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

        private void StartSettings(ClickEvent evt)
        {
            HideUi();
            HideMenu(evt);
            _settingsController.ShowUi(); 
        }
        
        private void StartOperations(ClickEvent evt)
        {
            _gameManager.selectedModeUiName = "Devices available for operate";
            HideUi();
            HideMenu(evt);
            _deviceSelectionController.ShowUi(); 
            if (evt.currentTarget is Button button) _gameManager.modeSelected = button.name;
            
        }
        
        private void StartTraining(ClickEvent evt)
        {
            _gameManager.selectedModeUiName = "Devices available for learning";
            HideUi();
            HideMenu(evt);
            //todo necesito trabajar la maquina de estados para poder seleccionar de forma correcta la seleccion del dispisitivo
            _deviceSelectionController.ShowUi(); 
            if (evt.currentTarget is Button button) _gameManager.modeSelected = button.name;
            
        }

        private void Logout(ClickEvent evt)
        {
            HideMenu(evt);
            HideUi();
            _welcomeController.ShowUi();
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

        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _menuButton=root.Q<Button>("MenuButton");
            _logoutButton=root.Q<Button>("LogoutButton");
            _subpanelsAndSmokeMaskContainer=root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _navigationMenuPanel=root.Q<VisualElement>("NavigationMenuPanel");
            _scrim = root.Q<VisualElement>("Scrim");
            _hideMenuButton=root.Q<Button>("HideMenuButton");
            _operationsButton=root.Q<Button>("OperationsButton");
            _trainingButton=root.Q<Button>("TrainingButton");
            _settingsButton=root.Q<Button>("SettingsButton");
            _localIoTStatusLabel=root.Q<Label>("LocalIoTStatusLabel");
            _localIoTModeLabel=root.Q<Label>("LocalIoTModeLabel");
            _vMIoTStatusLabel=root.Q<Label>("VMIoTStatusLabel");
            _vMIoTModeLabel=root.Q<Label>("VMIoTModeLabel");
            _cloudIoTStatusLabel=root.Q<Label>("CloudIoTStatusLabel");
            _cloudlIoTModeLabel=root.Q<Label>("CloudlIoTModeLabel");
        }

        private void RegisterEvents()
        {
            _menuButton.RegisterCallback<ClickEvent>(ShowMenu);
            _operationsButton.RegisterCallback<ClickEvent>(StartOperations);
            _trainingButton.RegisterCallback<ClickEvent>(StartTraining);
            _hideMenuButton.RegisterCallback<ClickEvent>(HideMenu);
            _logoutButton.RegisterCallback<ClickEvent>(Logout);
            _navigationMenuPanel.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);
            _settingsButton.RegisterCallback<ClickEvent>(StartSettings);
        }

        private void FindObjects()
        {
            _welcomeController=GameObject.FindGameObjectWithTag("Welcome").GetComponent<WelcomeControllerUI>();
            _deviceSelectionController=GameObject.FindGameObjectWithTag("DeviceSelection").GetComponent<DeviceSelectionController>();
            _gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
            _settingsController=GameObject.FindGameObjectWithTag("Settings").GetComponent<SettingsController>();
        }


    }
}
