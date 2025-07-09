using System;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.ReportsController
{
    /// <summary>
    /// Maneja todos los eventos UI de la pantalla de Reports
    /// </summary>
    public class ReportsEventManager
    {
        private ReportsUIManager _uiManager;
        private Action _onLogoutRequested;
        private Action<IReportsOps.PanelType> _onPanelTransitionComplete;
        private ReportsOrchestrator _orchestrator;

        private UIDocument _uiDocument;
        private VisualElement _root;

        public ReportsEventManager(
            ReportsUIManager uiManager,
            Action onLogoutRequested,
            Action<IReportsOps.PanelType> onPanelTransitionComplete,
            ReportsOrchestrator orchestrator)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _onLogoutRequested = onLogoutRequested ?? throw new ArgumentNullException(nameof(onLogoutRequested));
            _onPanelTransitionComplete = onPanelTransitionComplete ?? throw new ArgumentNullException(nameof(onPanelTransitionComplete));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        public void RegisterEvents(UIDocument uiDocument)
        {
            _uiDocument = uiDocument;
            _root = uiDocument.rootVisualElement;

            // Eventos principales del menú
            _root.Q<Button>("MenuButton")?.RegisterCallback<ClickEvent>(OnMenuButtonClicked);
            _root.Q<Button>("HideMenuButton")?.RegisterCallback<ClickEvent>(OnHideMenuButtonClicked);

            // Eventos de navegación principal
            _root.Q<Button>("DashboardButton")?.RegisterCallback<ClickEvent>(OnDashboardButtonClicked);
            _root.Q<Button>("OperationsButton")?.RegisterCallback<ClickEvent>(OnOperationsButtonClicked);
            _root.Q<Button>("TrainingButton")?.RegisterCallback<ClickEvent>(OnTrainingButtonClicked);
            _root.Q<Button>("SupportButton")?.RegisterCallback<ClickEvent>(OnSupportButtonClicked);
            _root.Q<Button>("SettingsButton")?.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);
            _root.Q<Button>("LogoutButton")?.RegisterCallback<ClickEvent>(OnLogoutButtonClicked);

            // Eventos de reportes
            _root.Q<Button>("SystemReportButton")?.RegisterCallback<ClickEvent>(OnSystemReportButtonClicked);
            _root.Q<Button>("ActivityReportButton")?.RegisterCallback<ClickEvent>(OnActivityReportButtonClicked);
            _root.Q<Button>("IoTReportButton")?.RegisterCallback<ClickEvent>(OnIoTReportButtonClicked);
            _root.Q<Button>("CustomReportButton")?.RegisterCallback<ClickEvent>(OnCustomReportButtonClicked);
            _root.Q<Button>("ExportReportButton")?.RegisterCallback<ClickEvent>(OnExportReportButtonClicked);
            _root.Q<Button>("ScheduleReportButton")?.RegisterCallback<ClickEvent>(OnScheduleReportButtonClicked);
            _root.Q<Button>("ViewHistoryButton")?.RegisterCallback<ClickEvent>(OnViewHistoryButtonClicked);

            // Evento de transición del menú de navegación
            var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
            navigationMenuPanel?.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

            // Eventos de clic en el scrim
            var scrim = _root.Q<VisualElement>("Scrim");
            scrim?.RegisterCallback<ClickEvent>(OnScrimClicked);

            // Registrar eventos de teclado
            RegisterKeyboardEvents(_root);

            Debug.Log("[ReportsEventManager] All events registered successfully");
        }

        public void UnregisterEvents()
        {
            try
            {
                if (_root == null)
                {
                    Debug.LogWarning("[ReportsEventManager] Root element is null - cannot unregister events");
                    return;
                }

                // Desregistrar eventos de botones
                _root.Q<Button>("MenuButton")?.UnregisterCallback<ClickEvent>(OnMenuButtonClicked);
                _root.Q<Button>("HideMenuButton")?.UnregisterCallback<ClickEvent>(OnHideMenuButtonClicked);
                _root.Q<Button>("DashboardButton")?.UnregisterCallback<ClickEvent>(OnDashboardButtonClicked);
                _root.Q<Button>("OperationsButton")?.UnregisterCallback<ClickEvent>(OnOperationsButtonClicked);
                _root.Q<Button>("TrainingButton")?.UnregisterCallback<ClickEvent>(OnTrainingButtonClicked);
                _root.Q<Button>("SupportButton")?.UnregisterCallback<ClickEvent>(OnSupportButtonClicked);
                _root.Q<Button>("SettingsButton")?.UnregisterCallback<ClickEvent>(OnSettingsButtonClicked);
                _root.Q<Button>("LogoutButton")?.UnregisterCallback<ClickEvent>(OnLogoutButtonClicked);
                _root.Q<Button>("SystemReportButton")?.UnregisterCallback<ClickEvent>(OnSystemReportButtonClicked);
                _root.Q<Button>("ActivityReportButton")?.UnregisterCallback<ClickEvent>(OnActivityReportButtonClicked);
                _root.Q<Button>("IoTReportButton")?.UnregisterCallback<ClickEvent>(OnIoTReportButtonClicked);
                _root.Q<Button>("CustomReportButton")?.UnregisterCallback<ClickEvent>(OnCustomReportButtonClicked);
                _root.Q<Button>("ExportReportButton")?.UnregisterCallback<ClickEvent>(OnExportReportButtonClicked);
                _root.Q<Button>("ScheduleReportButton")?.UnregisterCallback<ClickEvent>(OnScheduleReportButtonClicked);
                _root.Q<Button>("ViewHistoryButton")?.UnregisterCallback<ClickEvent>(OnViewHistoryButtonClicked);

                // Desregistrar evento de transición
                var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
                navigationMenuPanel?.UnregisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

                // Desregistrar evento del scrim
                var scrim = _root.Q<VisualElement>("Scrim");
                scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);

                // Desregistrar eventos de teclado
                UnregisterKeyboardEvents(_root);

                Debug.Log("[ReportsEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReportsEventManager] Error unregistering events: {ex.Message}");
            }
        }

        public void Cleanup()
        {
            UnregisterEvents();
            _uiManager = null;
            _orchestrator = null;
            _uiDocument = null;
            _root = null;
            _onLogoutRequested = null;
            _onPanelTransitionComplete = null;
            Debug.Log("[ReportsEventManager] Event Manager cleaned up");
        }

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

        private async void HandleEscapeKey()
        {
            if (_uiManager.CurrentActivePanel != IReportsOps.PanelType.None)
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

        private async void OnMenuButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowNavigationMenu();
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_opened",
                menuItem: null,
                context: "reports_main"
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

        private async void OnDashboardButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "DashboardButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleDashboardClick();
        }

        private async void OnOperationsButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "OperationsButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleOperationsClick();
        }

        private async void OnTrainingButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "TrainingButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleTrainingClick();
        }

        private async void OnSupportButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "SupportButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleSupportClick();
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

        private async void OnLogoutButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "LogoutButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleLogoutClick();
        }

        private async void OnSystemReportButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackButtonClick("SystemReportButton", "reports_panel");
            _orchestrator.GenerateSystemReport();
        }

        private async void OnActivityReportButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackButtonClick("ActivityReportButton", "reports_panel");
            _orchestrator.GenerateActivityReport();
        }

        private async void OnIoTReportButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackButtonClick("IoTReportButton", "reports_panel");
            _orchestrator.GenerateIoTReport();
        }

        private async void OnCustomReportButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackButtonClick("CustomReportButton", "reports_panel");
            _orchestrator.CreateCustomReport();
        }

        private async void OnExportReportButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackButtonClick("ExportReportButton", "quick_actions");
            _orchestrator.ExportLastReport();
        }

        private async void OnScheduleReportButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackButtonClick("ScheduleReportButton", "quick_actions");
            _orchestrator.ScheduleReport();
        }

        private async void OnViewHistoryButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackButtonClick("ViewHistoryButton", "quick_actions");
            _orchestrator.ViewReportHistory();
        }

        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            _onPanelTransitionComplete?.Invoke(_uiManager.CurrentActivePanel);
        }

        public ReportsUIManager UIManager => _uiManager;
        public ReportsOrchestrator Orchestrator => _orchestrator;
    }
}