using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.IotCoreSettingsController
{
    public class IotCoreSettingsEventManager
    {
        private IotCoreSettingsUIManager _uiManager;
        private Action _onReturnToDashboard;
        private Action<IIotCoreSettingsOps.PanelType> _onPanelTransitionComplete;
        private IotCoreSettingsOrchestrator _orchestrator;

        private UIDocument _uiDocument;
        private VisualElement _root;

        public IotCoreSettingsEventManager(
            IotCoreSettingsUIManager uiManager, 
            Action onReturnToDashboard, 
            Action<IIotCoreSettingsOps.PanelType> onPanelTransitionComplete, 
            IotCoreSettingsOrchestrator orchestrator)
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

            _root.Q<Button>("CreateThingButton")?.RegisterCallback<ClickEvent>(OnCreateThingButtonClicked);
            _root.Q<Button>("CreatePolicyButton")?.RegisterCallback<ClickEvent>(OnCreatePolicyButtonClicked);
            _root.Q<Button>("CreateCertificateButton")?.RegisterCallback<ClickEvent>(OnCreateCertificateButtonClicked);
            _root.Q<Button>("AttachCertificateButton")?.RegisterCallback<ClickEvent>(OnAttachCertificateButtonClicked);
            _root.Q<Button>("AttachPolicyButton")?.RegisterCallback<ClickEvent>(OnAttachPolicyButtonClicked);
            _root.Q<Button>("RefreshThingsButton")?.RegisterCallback<ClickEvent>(OnRefreshThingsButtonClicked);
            _root.Q<Button>("TestConnectivityButton")?.RegisterCallback<ClickEvent>(OnTestConnectivityButtonClicked);
            _root.Q<Button>("EnableDisableServiceButton")?.RegisterCallback<ClickEvent>(OnEnableDisableServiceButtonClicked);

            var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
            navigationMenuPanel?.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

            var scrim = _root.Q<VisualElement>("Scrim");
            scrim?.RegisterCallback<ClickEvent>(OnScrimClicked);

            RegisterKeyboardEvents(_root);

            Debug.Log("[IotCoreSettingsEventManager] All events registered successfully");
        }

        public void UnregisterEvents()
        {
            try
            {
                Debug.Log("[IotCoreSettingsEventManager] Unregistering IoT Core Settings events...");

                if (_root == null)
                {
                    Debug.LogWarning("[IotCoreSettingsEventManager] Root element is null - cannot unregister events");
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

                _root.Q<Button>("CreateThingButton")?.UnregisterCallback<ClickEvent>(OnCreateThingButtonClicked);
                _root.Q<Button>("CreatePolicyButton")?.UnregisterCallback<ClickEvent>(OnCreatePolicyButtonClicked);
                _root.Q<Button>("CreateCertificateButton")?.UnregisterCallback<ClickEvent>(OnCreateCertificateButtonClicked);
                _root.Q<Button>("AttachCertificateButton")?.UnregisterCallback<ClickEvent>(OnAttachCertificateButtonClicked);
                _root.Q<Button>("AttachPolicyButton")?.UnregisterCallback<ClickEvent>(OnAttachPolicyButtonClicked);
                _root.Q<Button>("RefreshThingsButton")?.UnregisterCallback<ClickEvent>(OnRefreshThingsButtonClicked);
                _root.Q<Button>("TestConnectivityButton")?.UnregisterCallback<ClickEvent>(OnTestConnectivityButtonClicked);
                _root.Q<Button>("EnableDisableServiceButton")?.UnregisterCallback<ClickEvent>(OnEnableDisableServiceButtonClicked);

                var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
                navigationMenuPanel?.UnregisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

                var scrim = _root.Q<VisualElement>("Scrim");
                scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);

                UnregisterKeyboardEvents(_root);

                Debug.Log("[IotCoreSettingsEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IotCoreSettingsEventManager] Error unregistering events: {ex.Message}");
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
            Debug.Log("[IotCoreSettingsEventManager] Event Manager cleaned up");
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
                    await HandleEscapeKey();
                    break;
                case KeyCode.M when evt.ctrlKey:
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

        private async Task HandleEscapeKey()
        {
            if (_uiManager.CurrentActivePanel != IIotCoreSettingsOps.PanelType.None)
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
                context: "iot_core_settings_main"
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
            // Removido await - HandleDashboardClick devuelve Task, no necesita await aquí
            _ = _orchestrator.HandleDashboardClick();
        }

        private async void OnOperationsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Operations button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "OperationsButton",
                context: "navigation_menu"
            );
            // Removido await - HandleOperationsClick devuelve Task, no necesita await aquí
            _ = _orchestrator.HandleOperationsClick();
        }

        private async void OnTrainingButtonClicked(ClickEvent evt)
        {
            Debug.Log("Training button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "TrainingButton",
                context: "navigation_menu"
            );
            // Removido await - HandleTrainingClick devuelve Task, no necesita await aquí
            _ = _orchestrator.HandleTrainingClick();
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
            // Removido await - HandleSupportClick devuelve Task, no necesita await aquí
            _ = _orchestrator.HandleSupportClick();
        }

        private async void OnLogoutButtonClicked(ClickEvent evt)
        {
            Debug.Log("Logout button clicked - executing logout");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "LogoutButton",
                context: "navigation_menu"
            );
            // Removido await - HandleLogoutClick devuelve Task, no necesita await aquí
            _ = _orchestrator.HandleLogoutClick();
        }

        private async void OnReportsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Reports button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "ReportsButton",
                context: "navigation_menu"
            );
            // Removido await - HandleReportsClick devuelve Task, no necesita await aquí
            _ = _orchestrator.HandleReportsClick();
        }

        #endregion

        #region IoT Core Settings Specific Events

        private async void OnCreateThingButtonClicked(ClickEvent evt)
        {
            Debug.Log("Create Thing button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("CreateThingButton", "IotCoreSettings");
            await _orchestrator.CreateThing();
        }

        private async void OnCreatePolicyButtonClicked(ClickEvent evt)
        {
            Debug.Log("Create Policy button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("CreatePolicyButton", "IotCoreSettings");
            await _orchestrator.CreatePolicy();
        }

        private async void OnCreateCertificateButtonClicked(ClickEvent evt)
        {
            Debug.Log("Create Certificate button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("CreateCertificateButton", "IotCoreSettings");
            await _orchestrator.CreateCertificate();
        }

        private async void OnAttachCertificateButtonClicked(ClickEvent evt)
        {
            Debug.Log("Attach Certificate button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("AttachCertificateButton", "IotCoreSettings");
            await _orchestrator.AttachCertificate();
        }

        private async void OnAttachPolicyButtonClicked(ClickEvent evt)
        {
            Debug.Log("Attach Policy button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("AttachPolicyButton", "IotCoreSettings");
            await _orchestrator.AttachPolicy();
        }

        private async void OnRefreshThingsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Refresh Things button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("RefreshThingsButton", "IotCoreSettings");
            await _orchestrator.RefreshThings();
        }

        private async void OnTestConnectivityButtonClicked(ClickEvent evt)
        {
            Debug.Log("Test Connectivity button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("TestConnectivityButton", "IotCoreSettings");
            await _orchestrator.TestConnection();
        }

        private async void OnEnableDisableServiceButtonClicked(ClickEvent evt)
        {
            Debug.Log("Enable/Disable Service button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("EnableDisableServiceButton", "IotCoreSettings");
            await _orchestrator.ToggleService();
        }

        #endregion

        #region Transition Events

        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            _onPanelTransitionComplete?.Invoke(_uiManager.CurrentActivePanel);
        }

        #endregion

        public IotCoreSettingsUIManager UIManager => _uiManager;
        public IotCoreSettingsOrchestrator Orchestrator => _orchestrator;
    }
}