using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.S3SettingsController
{
    public class S3SettingsEventManager
    {
        private S3SettingsUIManager _uiManager;
        private Action _onReturnToDashboard;
        private Action<IS3SettingsOps.PanelType> _onPanelTransitionComplete;
        private S3SettingsOrchestrator _orchestrator;

        private UIDocument _uiDocument;
        private VisualElement _root;

        public S3SettingsEventManager(
            S3SettingsUIManager uiManager, 
            Action onReturnToDashboard, 
            Action<IS3SettingsOps.PanelType> onPanelTransitionComplete, 
            S3SettingsOrchestrator orchestrator)
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

            // Botones de navegación principal
            _root.Q<Button>("MenuButton")?.RegisterCallback<ClickEvent>(OnMenuButtonClicked);
            _root.Q<Button>("HideMenuButton")?.RegisterCallback<ClickEvent>(OnHideMenuButtonClicked);

            _root.Q<Button>("OperationsButton")?.RegisterCallback<ClickEvent>(OnOperationsButtonClicked);
            _root.Q<Button>("TrainingButton")?.RegisterCallback<ClickEvent>(OnTrainingButtonClicked);
            _root.Q<Button>("ReportsButton")?.RegisterCallback<ClickEvent>(OnReportsButtonClicked);
            _root.Q<Button>("SupportButton")?.RegisterCallback<ClickEvent>(OnSupportButtonClicked);
            _root.Q<Button>("SettingsButton")?.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);
            _root.Q<Button>("LogoutButton")?.RegisterCallback<ClickEvent>(OnLogoutButtonClicked);

            // Botones Dashboard
            var dashboardButtons = _root.Query<Button>().Where(btn => btn.text == "Dashboard").ToList();
            foreach (var dashboardButton in dashboardButtons)
            {
                dashboardButton.RegisterCallback<ClickEvent>(OnDashboardButtonClicked);
            }

            // Botones específicos de S3
            _root.Q<Button>("UploadFileButton")?.RegisterCallback<ClickEvent>(OnUploadFileButtonClicked);
            _root.Q<Button>("DownloadFileButton")?.RegisterCallback<ClickEvent>(OnDownloadFileButtonClicked);
            _root.Q<Button>("ListFilesButton")?.RegisterCallback<ClickEvent>(OnListFilesButtonClicked);
            _root.Q<Button>("DeleteFileButton")?.RegisterCallback<ClickEvent>(OnDeleteFileButtonClicked);
            _root.Q<Button>("RefreshFilesButton")?.RegisterCallback<ClickEvent>(OnRefreshFilesButtonClicked);
            _root.Q<Button>("TestConnectionButton")?.RegisterCallback<ClickEvent>(OnTestConnectionButtonClicked);
            _root.Q<Button>("EnableDisableServiceButton")?.RegisterCallback<ClickEvent>(OnEnableDisableServiceButtonClicked);

            // Evento de transición del menú de navegación
            var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
            navigationMenuPanel?.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

            // Evento de clic en el scrim
            var scrim = _root.Q<VisualElement>("Scrim");
            scrim?.RegisterCallback<ClickEvent>(OnScrimClicked);

            // Eventos de teclado
            RegisterKeyboardEvents(_root);

            Debug.Log("[S3SettingsEventManager] All events registered successfully");
        }

        public void UnregisterEvents()
        {
            try
            {
                Debug.Log("[S3SettingsEventManager] Unregistering S3 Settings events...");

                if (_root == null)
                {
                    Debug.LogWarning("[S3SettingsEventManager] Root element is null - cannot unregister events");
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

                _root.Q<Button>("UploadFileButton")?.UnregisterCallback<ClickEvent>(OnUploadFileButtonClicked);
                _root.Q<Button>("DownloadFileButton")?.UnregisterCallback<ClickEvent>(OnDownloadFileButtonClicked);
                _root.Q<Button>("ListFilesButton")?.UnregisterCallback<ClickEvent>(OnListFilesButtonClicked);
                _root.Q<Button>("DeleteFileButton")?.UnregisterCallback<ClickEvent>(OnDeleteFileButtonClicked);
                _root.Q<Button>("RefreshFilesButton")?.UnregisterCallback<ClickEvent>(OnRefreshFilesButtonClicked);
                _root.Q<Button>("TestConnectionButton")?.UnregisterCallback<ClickEvent>(OnTestConnectionButtonClicked);
                _root.Q<Button>("EnableDisableServiceButton")?.UnregisterCallback<ClickEvent>(OnEnableDisableServiceButtonClicked);

                var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
                navigationMenuPanel?.UnregisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

                var scrim = _root.Q<VisualElement>("Scrim");
                scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);

                UnregisterKeyboardEvents(_root);

                Debug.Log("[S3SettingsEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[S3SettingsEventManager] Error unregistering events: {ex.Message}");
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
            Debug.Log("[S3SettingsEventManager] Event Manager cleaned up");
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
            if (_uiManager.CurrentActivePanel != IS3SettingsOps.PanelType.None)
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
                context: "s3_settings_main"
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
            await _orchestrator.HandleDashboardClick();
        }

        private async void OnOperationsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Operations button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "OperationsButton",
                context: "navigation_menu"
            );
            await _orchestrator.HandleOperationsClick();
        }

        private async void OnTrainingButtonClicked(ClickEvent evt)
        {
            Debug.Log("Training button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "TrainingButton",
                context: "navigation_menu"
            );
            await _orchestrator.HandleTrainingClick();
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
            await _orchestrator.HandleSupportClick();
        }

        private async void OnLogoutButtonClicked(ClickEvent evt)
        {
            Debug.Log("Logout button clicked - executing logout");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "LogoutButton",
                context: "navigation_menu"
            );
            await _orchestrator.HandleLogoutClick();
        }

        private async void OnReportsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Reports button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "ReportsButton",
                context: "navigation_menu"
            );
            await _orchestrator.HandleReportsClick();
        }

        #endregion

        #region S3 Settings Specific Events

        private async void OnUploadFileButtonClicked(ClickEvent evt)
        {
            Debug.Log("Upload File button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("UploadFileButton", "S3Settings");
            await _orchestrator.UploadFile();
        }

        private async void OnDownloadFileButtonClicked(ClickEvent evt)
        {
            Debug.Log("Download File button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("DownloadFileButton", "S3Settings");
            await _orchestrator.DownloadFile();
        }

        private async void OnListFilesButtonClicked(ClickEvent evt)
        {
            Debug.Log("List Files button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("ListFilesButton", "S3Settings");
            await _orchestrator.ListFiles();
        }

        private async void OnDeleteFileButtonClicked(ClickEvent evt)
        {
            Debug.Log("Delete File button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("DeleteFileButton", "S3Settings");
            await _orchestrator.DeleteFile();
        }

        private async void OnRefreshFilesButtonClicked(ClickEvent evt)
        {
            Debug.Log("Refresh Files button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("RefreshFilesButton", "S3Settings");
            await _orchestrator.ListFiles();
        }

        private async void OnTestConnectionButtonClicked(ClickEvent evt)
        {
            Debug.Log("Test Connectivity button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("TestConnectionButton", "S3Settings");
            await _orchestrator.TestConnectionAsync();
        }

        private async void OnEnableDisableServiceButtonClicked(ClickEvent evt)
        {
            Debug.Log("Enable/Disable Service button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick("EnableDisableServiceButton", "S3Settings");
            await _orchestrator.ToggleService();
        }

        #endregion

        #region Transition Events

        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            _onPanelTransitionComplete?.Invoke(_uiManager.CurrentActivePanel);
        }

        #endregion

        #region Public Properties

        public S3SettingsUIManager UIManager => _uiManager;
        public S3SettingsOrchestrator Orchestrator => _orchestrator;

        #endregion
    }
}