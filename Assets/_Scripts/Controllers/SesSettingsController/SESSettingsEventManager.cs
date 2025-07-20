using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.SESSettingsController
{
    public class SESSettingsEventManager
    {
        private SESSettingsUIManager _uiManager;
        private Action _onReturnToDashboard;
        private Action<ISESSettingsOps.PanelType> _onPanelTransitionComplete;
        private SESSettingsOrchestrator _orchestrator;

        // Referencias para poder desregistrar eventos
        private UIDocument _uiDocument;
        private VisualElement _root;

        public SESSettingsEventManager(
            SESSettingsUIManager uiManager, 
            Action onReturnToDashboard, 
            Action<ISESSettingsOps.PanelType> onPanelTransitionComplete, 
            SESSettingsOrchestrator orchestrator)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _onReturnToDashboard = onReturnToDashboard ?? throw new ArgumentNullException(nameof(onReturnToDashboard));
            _onPanelTransitionComplete = onPanelTransitionComplete ?? throw new ArgumentNullException(nameof(onPanelTransitionComplete));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        public void RegisterEvents(UIDocument uiDocument)
        {
            _uiDocument = uiDocument;
            _root = uiDocument.rootVisualElement;

            _root.Q<Button>("MenuButton")?.RegisterCallback<ClickEvent>(OnMenuButtonClicked);
            _root.Q<Button>("HideMenuButton")?.RegisterCallback<ClickEvent>(OnHideMenuButtonClicked);

            _root.Q<Button>("OperationsButton")?.RegisterCallback<ClickEvent>(OnOperationsButtonClicked);
            _root.Q<Button>("TrainingButton")?.RegisterCallback<ClickEvent>(OnTrainingButtonClicked);
            _root.Q<Button>("ReportsButton")?.RegisterCallback<ClickEvent>(OnReportsButtonClicked);
            _root.Q<Button>("SupportButton")?.RegisterCallback<ClickEvent>(OnSupportButtonClicked);
            _root.Q<Button>("SettingsButton")?.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);
            _root.Q<Button>("LogoutButton")?.RegisterCallback<ClickEvent>(OnLogoutButtonClicked);

            var dashboardButtons = _root.Query<Button>().Where(btn => btn.text == "Dashboard").ToList();
            foreach (var dashboardButton in dashboardButtons)
            {
                dashboardButton.RegisterCallback<ClickEvent>(OnDashboardButtonClicked);
            }

            _root.Q<Button>("SaveSenderConfigButton")?.RegisterCallback<ClickEvent>(OnSaveSenderConfigButtonClicked);
            _root.Q<Button>("CreateTemplateButton")?.RegisterCallback<ClickEvent>(OnCreateTemplateButtonClicked);
            _root.Q<Button>("VerifyEmailButton")?.RegisterCallback<ClickEvent>(OnVerifyEmailButtonClicked);
            _root.Q<Button>("TestConnectionButton")?.RegisterCallback<ClickEvent>(OnTestConnectionButtonClicked);
            _root.Q<Button>("EnableDisableServiceButton")?.RegisterCallback<ClickEvent>(OnEnableDisableServiceButtonClicked);

            var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
            navigationMenuPanel?.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

            var scrim = _root.Q<VisualElement>("Scrim");
            scrim?.RegisterCallback<ClickEvent>(OnScrimClicked);

            RegisterKeyboardEvents(_root);

            Debug.Log("[SESSettingsEventManager] All events registered successfully");
        }

        public void UnregisterEvents()
        {
            try
            {
                Debug.Log("[SESSettingsEventManager] Unregistering AWS Settings events...");

                if (_root == null)
                {
                    Debug.LogWarning("[SESSettingsEventManager] Root element is null - cannot unregister events");
                    return;
                }

                _root.Q<Button>("MenuButton")?.UnregisterCallback<ClickEvent>(OnMenuButtonClicked);
                _root.Q<Button>("HideMenuButton")?.UnregisterCallback<ClickEvent>(OnHideMenuButtonClicked);

                _root.Q<Button>("OperationsButton")?.UnregisterCallback<ClickEvent>(OnOperationsButtonClicked);
                _root.Q<Button>("TrainingButton")?.UnregisterCallback<ClickEvent>(OnTrainingButtonClicked);
                _root.Q<Button>("ReportsButton")?.UnregisterCallback<ClickEvent>(OnReportsButtonClicked);
                _root.Q<Button>("SupportButton")?.UnregisterCallback<ClickEvent>(OnSupportButtonClicked);
                _root.Q<Button>("SettingsButton")?.UnregisterCallback<ClickEvent>(OnSettingsButtonClicked);
                _root.Q<Button>("LogoutButton")?.UnregisterCallback<ClickEvent>(OnLogoutButtonClicked);

                var dashboardButtons = _root.Query<Button>().Where(btn => btn.text == "Dashboard").ToList();
                foreach (var dashboardButton in dashboardButtons)
                {
                    dashboardButton.UnregisterCallback<ClickEvent>(OnDashboardButtonClicked);
                }

                _root.Q<Button>("SaveSenderConfigButton")?.UnregisterCallback<ClickEvent>(OnSaveSenderConfigButtonClicked);
                _root.Q<Button>("CreateTemplateButton")?.UnregisterCallback<ClickEvent>(OnCreateTemplateButtonClicked);
                _root.Q<Button>("VerifyEmailButton")?.UnregisterCallback<ClickEvent>(OnVerifyEmailButtonClicked);
                _root.Q<Button>("TestConnectionButton")?.UnregisterCallback<ClickEvent>(OnTestConnectionButtonClicked);
                _root.Q<Button>("EnableDisableServiceButton")?.UnregisterCallback<ClickEvent>(OnEnableDisableServiceButtonClicked);

                var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
                navigationMenuPanel?.UnregisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

                var scrim = _root.Q<VisualElement>("Scrim");
                scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);

                UnregisterKeyboardEvents(_root);

                Debug.Log("[SESSettingsEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SESSettingsEventManager] Error unregistering events: {ex.Message}");
            }
        }

        public void Cleanup()
        {
            UnregisterEvents();
            
            _uiManager = null;
            _orchestrator = null;
            _uiDocument = null;
            _root = null;
            _onReturnToDashboard = null;
            _onPanelTransitionComplete = null;

            Debug.Log("[SESSettingsEventManager] Event Manager cleaned up");
        }

        #region Keyboard Events

        private void RegisterKeyboardEvents(VisualElement root)
        {
            root.RegisterCallback<KeyDownEvent>(OnGlobalKeyDown);
        }

        private void UnregisterKeyboardEvents(VisualElement root)
        {
            root?.UnregisterCallback<KeyDownEvent>(OnGlobalKeyDown);
        }

        private async void OnGlobalKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    HandleEscapeKey();
                    break;
                    
                case KeyCode.M when evt.ctrlKey: // Ctrl+M para toggle menú
                    _uiManager.ToggleNavigationMenu();
                    var action = _uiManager.NavigationMenuOpen ? "menu_opened" : "menu_closed";
                    await UIAnalyticsManager.Instance?.TrackMenuEvent(
                        action: action,
                        menuItem: null,
                        context: "keyboard_shortcut_ctrl_m"
                    );
                    break;
            }
        }

        private async void HandleEscapeKey()
        {
            if (_uiManager.CurrentActivePanel != ISESSettingsOps.PanelType.None)
            {
                if (_uiManager.NavigationMenuOpen)
                {
                    _uiManager.HideNavigationMenu();
                    await UIAnalyticsManager.Instance?.TrackMenuEvent(
                        action: "menu_closed",
                        menuItem: null,
                        context: "escape_key"
                    );
                }
                else
                {
                    _uiManager.CloseCurrentPanel();
                    await UIAnalyticsManager.Instance?.TrackPanelTransition(
                        fromPanel: _uiManager.CurrentActivePanel.ToString(),
                        toPanel: "none",
                        transitionType: "escape_key"
                    );
                }
            }
        }

        #endregion

        #region Main Navigation Events

        private async void OnMenuButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowNavigationMenu();
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_opened",
                menuItem: null,
                context: "aws_settings_main"
            );
        }

        private async void OnHideMenuButtonClicked(ClickEvent evt)
        {
            _uiManager.HideNavigationMenu();
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_closed",
                menuItem: null,
                context: "hide_button_click"
            );
        }

        private async void OnScrimClicked(ClickEvent evt)
        {
            if (evt.target == evt.currentTarget)
            {
                _uiManager.CloseCurrentPanel();
                await UIAnalyticsManager.Instance?.TrackMenuEvent(
                    action: "menu_closed",
                    menuItem: null,
                    context: "scrim_click"
                );
            }
        }

        #endregion

        #region Navigation Action Events

        private async void OnDashboardButtonClicked(ClickEvent evt)
        {
            Debug.Log("Dashboard button clicked - returning to Dashboard");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "DashboardButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleDashboardClick();
        }

        private async void OnOperationsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Operations button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "OperationsButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleOperationsClick();
        }

        private async void OnTrainingButtonClicked(ClickEvent evt)
        {
            Debug.Log("Training button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "TrainingButton", 
                context: "navigation_menu"
            );
            _orchestrator.HandleTrainingClick();
        }

        private async void OnSettingsButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "SettingsButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleSettingsClick();
        }

        private async void OnSupportButtonClicked(ClickEvent evt)
        {
            Debug.Log("Support button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "SupportButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleSupportClick();
        }

        private async void OnLogoutButtonClicked(ClickEvent evt)
        {
            Debug.Log("Logout button clicked - executing logout");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "LogoutButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleLogoutClick();
        }
        
        private async void OnReportsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Reports button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "ReportsButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleReportsClick();
        }

        #endregion

        #region SES Settings Specific Events

        private async void OnSaveSenderConfigButtonClicked(ClickEvent evt)
        {
            Debug.Log("Save Sender Config button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("SaveSenderConfigButton", "SESSettings");
            _orchestrator.SaveConfiguration();
        }

        private async void OnCreateTemplateButtonClicked(ClickEvent evt)
        {
            Debug.Log("Create Template button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("CreateTemplateButton", "SESSettings");
            _orchestrator.CreateNewTemplate();
        }

        private async void OnVerifyEmailButtonClicked(ClickEvent evt)
        {
            Debug.Log("Verify Email button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("VerifyEmailButton", "SESSettings");
            await _orchestrator.RequestEmailVerification();
        }

        private async void OnTestConnectionButtonClicked(ClickEvent evt)
        {
            Debug.Log("Test Connection button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("TestConnectionButton", "SESSettings");
            _orchestrator.TestConnection();
        }

        private async void OnEnableDisableServiceButtonClicked(ClickEvent evt)
        {
            Debug.Log("Enable/Disable Service button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("EnableDisableServiceButton", "SESSettings");
            _orchestrator.ToggleService();
        }

        #endregion

        #region Transition Events

        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            _onPanelTransitionComplete?.Invoke(_uiManager.CurrentActivePanel);
        }

        #endregion

        public SESSettingsUIManager UIManager => _uiManager;
        public SESSettingsOrchestrator Orchestrator => _orchestrator;
    }
}