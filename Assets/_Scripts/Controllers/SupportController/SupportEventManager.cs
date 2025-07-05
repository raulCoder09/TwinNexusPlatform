using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.SupportController
{
    /// <summary>
    /// Maneja todos los eventos UI del Support Center
    /// Equivalente a DashboardEventManager pero para Support
    /// </summary>
    public class SupportEventManager
    {
        private SupportUIManager _uiManager;
        private Action<ISupportOps.PanelType> _onPanelTransitionComplete;
        private SupportOrchestrator _orchestrator;

        // Referencias para poder desregistrar eventos
        private UIDocument _uiDocument;
        private VisualElement _root;

        #region Constructor

        public SupportEventManager(
            SupportUIManager uiManager, 
            Action<ISupportOps.PanelType> onPanelTransitionComplete, 
            SupportOrchestrator orchestrator)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _onPanelTransitionComplete = onPanelTransitionComplete ?? throw new ArgumentNullException(nameof(onPanelTransitionComplete));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        #endregion

        #region Event Registration

        /// <summary>
        /// Registra todos los eventos del Support Center
        /// </summary>
        public void RegisterEvents(UIDocument uiDocument)
        {
            // Guardar referencias para desregistro posterior
            _uiDocument = uiDocument;
            _root = uiDocument.rootVisualElement;

            // Eventos principales del menú
            _root.Q<Button>("MenuButton")?.RegisterCallback<ClickEvent>(OnMenuButtonClicked);
            _root.Q<Button>("HideMenuButton")?.RegisterCallback<ClickEvent>(OnHideMenuButtonClicked);

            // Eventos de navegación principal
            _root.Q<Button>("DashboardButton")?.RegisterCallback<ClickEvent>(OnDashboardButtonClicked);
            _root.Q<Button>("TrainingButton")?.RegisterCallback<ClickEvent>(OnTrainingButtonClicked);
            _root.Q<Button>("OperationsButton")?.RegisterCallback<ClickEvent>(OnOperationsButtonClicked);
            _root.Q<Button>("SettingsButton")?.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);
            _root.Q<Button>("LogoutButton")?.RegisterCallback<ClickEvent>(OnLogoutButtonClicked);

            // Eventos específicos del Support Center
            _root.Q<Button>("TechnicalSupportButton")?.RegisterCallback<ClickEvent>(OnTechnicalSupportButtonClicked);
            _root.Q<Button>("DocumentationButton")?.RegisterCallback<ClickEvent>(OnDocumentationButtonClicked);
            _root.Q<Button>("DiagnosticsButton")?.RegisterCallback<ClickEvent>(OnDiagnosticsButtonClicked);
            _root.Q<Button>("RemoteAssistanceButton")?.RegisterCallback<ClickEvent>(OnRemoteAssistanceButtonClicked);

            // Evento de transición del menú de navegación
            var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
            navigationMenuPanel?.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

            // Eventos de clic en el scrim para cerrar paneles
            var scrim = _root.Q<VisualElement>("Scrim");
            scrim?.RegisterCallback<ClickEvent>(OnScrimClicked);

            // Registrar eventos de teclado
            RegisterKeyboardEvents(_root);

            Debug.Log("[SupportEventManager] All events registered successfully");
        }

        /// <summary>
        /// Desregistra todos los eventos del Support Center
        /// </summary>
        public void UnregisterEvents()
        {
            try
            {
                Debug.Log("[SupportEventManager] Unregistering Support events...");

                if (_root == null)
                {
                    Debug.LogWarning("[SupportEventManager] Root element is null - cannot unregister events");
                    return;
                }

                // Eventos principales del menú
                _root.Q<Button>("MenuButton")?.UnregisterCallback<ClickEvent>(OnMenuButtonClicked);
                _root.Q<Button>("HideMenuButton")?.UnregisterCallback<ClickEvent>(OnHideMenuButtonClicked);

                // Eventos de navegación principal
                _root.Q<Button>("DashboardButton")?.UnregisterCallback<ClickEvent>(OnDashboardButtonClicked);
                _root.Q<Button>("TrainingButton")?.UnregisterCallback<ClickEvent>(OnTrainingButtonClicked);
                _root.Q<Button>("OperationsButton")?.UnregisterCallback<ClickEvent>(OnOperationsButtonClicked);
                _root.Q<Button>("SettingsButton")?.UnregisterCallback<ClickEvent>(OnSettingsButtonClicked);
                _root.Q<Button>("LogoutButton")?.UnregisterCallback<ClickEvent>(OnLogoutButtonClicked);

                // Eventos específicos del Support Center
                _root.Q<Button>("TechnicalSupportButton")?.UnregisterCallback<ClickEvent>(OnTechnicalSupportButtonClicked);
                _root.Q<Button>("DocumentationButton")?.UnregisterCallback<ClickEvent>(OnDocumentationButtonClicked);
                _root.Q<Button>("DiagnosticsButton")?.UnregisterCallback<ClickEvent>(OnDiagnosticsButtonClicked);
                _root.Q<Button>("RemoteAssistanceButton")?.UnregisterCallback<ClickEvent>(OnRemoteAssistanceButtonClicked);

                // Evento de transición del menú de navegación
                var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
                navigationMenuPanel?.UnregisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

                // Eventos de clic en el scrim
                var scrim = _root.Q<VisualElement>("Scrim");
                scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);

                // Desregistrar eventos de teclado
                UnregisterKeyboardEvents(_root);

                Debug.Log("[SupportEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SupportEventManager] Error unregistering events: {ex.Message}");
            }
        }

        /// <summary>
        /// Método Cleanup consistente con otros EventManagers
        /// </summary>
        public void Cleanup()
        {
            UnregisterEvents();
            
            _uiManager = null;
            _orchestrator = null;
            _uiDocument = null;
            _root = null;
            _onPanelTransitionComplete = null;

            Debug.Log("[SupportEventManager] Event Manager cleaned up");
        }

        #endregion

        #region Keyboard Events

        /// <summary>
        /// Registra eventos de teclado para el Support Center
        /// </summary>
        private void RegisterKeyboardEvents(VisualElement root)
        {
            root.RegisterCallback<KeyDownEvent>(OnGlobalKeyDown);
        }

        /// <summary>
        /// Desregistra eventos de teclado
        /// </summary>
        private void UnregisterKeyboardEvents(VisualElement root)
        {
            root?.UnregisterCallback<KeyDownEvent>(OnGlobalKeyDown);
        }

        /// <summary>
        /// Maneja eventos globales de teclado
        /// </summary>
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
                    
                case KeyCode.F1:
                    _orchestrator.HandleDocumentationClick();
                    await UIAnalyticsManager.Instance?.TrackButtonClick(
                        "DocumentationButton",
                        "keyboard_shortcut_f1"
                    );
                    break;
                    
                case KeyCode.F12:
                    _orchestrator.HandleDiagnosticsClick();
                    await UIAnalyticsManager.Instance?.TrackButtonClick(
                        "DiagnosticsButton", 
                        "keyboard_shortcut_f12"
                    );
                    break;
            }
        }

        /// <summary>
        /// Maneja la tecla Escape
        /// </summary>
        private async void HandleEscapeKey()
        {
            if (_uiManager.CurrentActivePanel != ISupportOps.PanelType.None)
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

        /// <summary>
        /// Abre el menú lateral de navegación
        /// </summary>
        private async void OnMenuButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowNavigationMenu();
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_opened",
                menuItem: null,
                context: "support_main"
            );
        }

        /// <summary>
        /// Cierra el menú lateral de navegación
        /// </summary>
        private async void OnHideMenuButtonClicked(ClickEvent evt)
        {
            _uiManager.HideNavigationMenu();
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_closed",
                menuItem: null,
                context: "hide_button_click"
            );
        }

        /// <summary>
        /// Maneja clic en el scrim para cerrar paneles
        /// </summary>
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

        /// <summary>
        /// Navega al Dashboard
        /// </summary>
        private async void OnDashboardButtonClicked(ClickEvent evt)
        {
            Debug.Log("Dashboard button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "DashboardButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleDashboardClick();
        }

        /// <summary>
        /// Inicia modo Training
        /// </summary>
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

        /// <summary>
        /// Inicia modo Operations
        /// </summary>
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

        /// <summary>
        /// Abre Settings
        /// </summary>
        private async void OnSettingsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Settings button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "SettingsButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleSettingsClick();
        }

        /// <summary>
        /// Ejecuta Logout
        /// </summary>
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

        #endregion

        #region Support-Specific Events

        /// <summary>
        /// Contacta con soporte técnico
        /// </summary>
        private async void OnTechnicalSupportButtonClicked(ClickEvent evt)
        {
            Debug.Log("Technical Support button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick(
                "TechnicalSupportButton",
                "support_main",
                new Dictionary<string, object> { ["support_type"] = "technical" }
            );
            _orchestrator.HandleTechnicalSupportClick();
        }

        /// <summary>
        /// Abre documentación
        /// </summary>
        private async void OnDocumentationButtonClicked(ClickEvent evt)
        {
            Debug.Log("Documentation button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick(
                "DocumentationButton",
                "support_main",
                new Dictionary<string, object> { ["support_type"] = "documentation" }
            );
            _orchestrator.HandleDocumentationClick();
        }

        /// <summary>
        /// Ejecuta diagnósticos del sistema
        /// </summary>
        private async void OnDiagnosticsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Diagnostics button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick(
                "DiagnosticsButton",
                "support_main",
                new Dictionary<string, object> { ["support_type"] = "diagnostics" }
            );
            _orchestrator.HandleDiagnosticsClick();
        }

        /// <summary>
        /// Solicita asistencia remota
        /// </summary>
        private async void OnRemoteAssistanceButtonClicked(ClickEvent evt)
        {
            Debug.Log("Remote Assistance button clicked");
            await UIAnalyticsManager.Instance?.TrackButtonClick(
                "RemoteAssistanceButton",
                "support_main",
                new Dictionary<string, object> { ["support_type"] = "remote_assistance" }
            );
            _orchestrator.HandleRemoteAssistanceClick();
        }

        #endregion

        #region Transition Events

        /// <summary>
        /// Maneja el final de la transición del menú de navegación
        /// </summary>
        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            _onPanelTransitionComplete?.Invoke(_uiManager.CurrentActivePanel);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// UI Manager asociado
        /// </summary>
        public SupportUIManager UIManager => _uiManager;

        /// <summary>
        /// Orchestrator asociado
        /// </summary>
        public SupportOrchestrator Orchestrator => _orchestrator;

        #endregion
    }
}