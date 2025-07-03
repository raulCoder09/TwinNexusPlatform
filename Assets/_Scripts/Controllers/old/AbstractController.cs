using _Scripts.Controllers.SettingsController;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class AbstractController : MonoBehaviour
    {
        private VisualElement _body;
        private DropdownField _menuLevelMedara;
        private GameManager _gameManager;
        #region menu

        private VisualElement _subpanelsAndSmokeMaskContainer;
        private VisualElement _navigationMenuPanel;
        private VisualElement _scrim;
        private Button _menuButton;
        private Button _hideMenuButton;
        // private OldWelcomeController _oldWelcomeController;
        // private OldDeviceSelectionController _oldDeviceSelectionController;
        private Button _operationsButton;
        private Button _logoutButton;
        private Button _trainingButton;
        private Button _settingsButton;
        #endregion
        private SettingsOrchestrator _oldSettingsController;
        private void Awake()
        {
            GetUiComponents();
            RegisterEvents();
            FindObjects();
        }
        private void Start()
        {
            #region menu

            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;

            #endregion
        }
        private void FindObjects()
        {
            _oldSettingsController=GameObject.FindGameObjectWithTag("Settings").GetComponent<SettingsOrchestrator>();
        }
        private void RegisterEvents()
        {
            
            #region menu

            _menuButton.RegisterCallback<ClickEvent>(ShowMenu);
            _hideMenuButton.RegisterCallback<ClickEvent>(HideMenu);
            _navigationMenuPanel.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);
            _operationsButton.RegisterCallback<ClickEvent>(StartOperations);
            _logoutButton.RegisterCallback<ClickEvent>(Logout);
            _settingsButton.RegisterCallback<ClickEvent>(StartSettings);
            #endregion
        }
        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _menuLevelMedara= root.Q<DropdownField>("MenuLevelMedaraDropdownField");
            #region menu

            _menuButton=root.Q<Button>("MenuButton");
            _subpanelsAndSmokeMaskContainer=root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _navigationMenuPanel=root.Q<VisualElement>("NavigationMenuPanel");
            _scrim = root.Q<VisualElement>("Scrim");
            _hideMenuButton=root.Q<Button>("HideMenuButton");
            _operationsButton=root.Q<Button>("OperationsButton");
            _settingsButton=root.Q<Button>("SettingsButton");
            _logoutButton=root.Q<Button>("LogoutButton");

            #endregion
        }
        #region menu
        private void StartSettings(ClickEvent evt)
        {
            HideUi();
            HideMenu(evt);
            _oldSettingsController.ShowUi(); 
        }

        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            if (!_navigationMenuPanel.ClassListContains("NavigationMenuPanelInMainScreen"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }

        private void HideMenu(ClickEvent evt)
        {
            _navigationMenuPanel.RemoveFromClassList("NavigationMenuPanelInMainScreen");
            _scrim.RemoveFromClassList("ScrimOpaque");
        }

        private void ShowMenu(ClickEvent evt)
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.Flex;
            _navigationMenuPanel.AddToClassList("NavigationMenuPanelInMainScreen");
            _scrim.AddToClassList("ScrimOpaque");
        }
        internal void ShowUi()
        {
            _body.style.display = DisplayStyle.Flex;
        }
        internal void HideUi()
        {
            _body.style.display = DisplayStyle.None;
        }
        
        private void StartOperations(ClickEvent evt)
        {
            _gameManager.selectedModeUiName = "Devices available for operate";
            HideUi();
            HideMenu(evt);
            // _oldDeviceSelectionController.ShowUi(); 
            if (evt.currentTarget is Button button) _gameManager.modeSelected = button.name;
            
        }
        
        private void Logout(ClickEvent evt)
        {
            HideMenu(evt);
            HideUi();
            // _oldWelcomeController.ShowUi();
        }

        #endregion
        internal void EnableLevel()
        {
            gameObject.SetActive(true);
        }

        internal void DisableLevel()
        {
            gameObject.SetActive(false);
        }
    }
}
