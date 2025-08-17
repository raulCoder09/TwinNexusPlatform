using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.IotSettingsController
{
    /// <summary>
    /// Maneja todos los eventos UI del IotSettings
    /// Equivalente a WelcomeEventManager pero para IotSettings
    /// </summary>
    public class IotSettingsEventManager
    {
        private IotSettingsUIManager _uiManager;
        private Action _onLogoutRequested;
        private Action<IIotSettingsOps.PanelType> _onPanelTransitionComplete;
        private IotSettingsOrchestrator _orchestrator;

        // Referencias para poder desregistrar eventos
        private UIDocument _uiDocument;
        private VisualElement _root;

        #region Constructor

        public IotSettingsEventManager(
            IotSettingsUIManager uiManager, 
            Action onLogoutRequested, 
            Action<IIotSettingsOps.PanelType> onPanelTransitionComplete, 
            IotSettingsOrchestrator orchestrator)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _onLogoutRequested = onLogoutRequested ?? throw new ArgumentNullException(nameof(onLogoutRequested));
            _onPanelTransitionComplete = onPanelTransitionComplete ?? throw new ArgumentNullException(nameof(onPanelTransitionComplete));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        #endregion

        #region Event Registration

        /// <summary>
        /// Registra todos los eventos del IotSettings
        /// </summary>
        public void RegisterEvents(UIDocument uiDocument)
        {
            // Guardar referencias para desregistro posterior
            _uiDocument = uiDocument;
            _root = uiDocument.rootVisualElement;

            // Eventos principales del menú
            _root.Q<Button>("MenuButton")?.RegisterCallback<ClickEvent>(OnMenuButtonClicked);
            _root.Q<Button>("HideMenuButton")?.RegisterCallback<ClickEvent>(OnHideMenuButtonClicked);
            _root.Q<Button>("ManagedCloudServiceButton")?.RegisterCallback<ClickEvent>(OnManagedCloudServiceButtonClicked);

            // Eventos de navegación principal
            _root.Q<Button>("OperationsButton")?.RegisterCallback<ClickEvent>(OnOperationsButtonClicked);
            _root.Q<Button>("TrainingButton")?.RegisterCallback<ClickEvent>(OnTrainingButtonClicked);
            _root.Q<Button>("ReportsButton")?.RegisterCallback<ClickEvent>(OnReportsButtonClicked);
            _root.Q<Button>("SupportButton")?.RegisterCallback<ClickEvent>(OnSupportButtonClicked); 
            _root.Q<Button>("SettingsButton")?.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);
            _root.Q<Button>("LogoutButton")?.RegisterCallback<ClickEvent>(OnLogoutButtonClicked);

            // Eventos de paneles adicionales (futuros)
            _root.Q<Button>("NotificationsButton")?.RegisterCallback<ClickEvent>(OnNotificationsButtonClicked);
            _root.Q<Button>("UserProfileButton")?.RegisterCallback<ClickEvent>(OnUserProfileButtonClicked);
            _root.Q<Button>("QuickActionsButton")?.RegisterCallback<ClickEvent>(OnQuickActionsButtonClicked);
            _root.Q<Button>("StatusOverlayButton")?.RegisterCallback<ClickEvent>(OnStatusOverlayButtonClicked);

            // Eventos de cierre de paneles
            _root.Q<Button>("CloseNotificationsButton")?.RegisterCallback<ClickEvent>(OnCloseNotificationsPanelClicked);
            _root.Q<Button>("CloseUserProfileButton")?.RegisterCallback<ClickEvent>(OnCloseUserProfilePanelClicked);
            _root.Q<Button>("CloseQuickActionsButton")?.RegisterCallback<ClickEvent>(OnCloseQuickActionsPanelClicked);
            _root.Q<Button>("CloseStatusOverlayButton")?.RegisterCallback<ClickEvent>(OnCloseStatusOverlayPanelClicked);

            // Evento de transición del menú de navegación
            var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
            navigationMenuPanel?.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

            // Eventos de clic en el scrim para cerrar paneles
            var scrim = _root.Q<VisualElement>("Scrim");
            scrim?.RegisterCallback<ClickEvent>(OnScrimClicked);

            // Registrar eventos de teclado
            RegisterKeyboardEvents(_root);

            Debug.Log("[IotSettingsEventManager] All events registered successfully");
        }

        /// <summary>
        /// Desregistra todos los eventos del IotSettings
        /// </summary>
        public void UnregisterEvents()
        {
            try
            {
                Debug.Log("[IotSettingsEventManager] Unregistering IotSettings events...");

                if (_root == null)
                {
                    Debug.LogWarning("[IotSettingsEventManager] Root element is null - cannot unregister events");
                    return;
                }

                // Eventos principales del menú
                _root.Q<Button>("MenuButton")?.UnregisterCallback<ClickEvent>(OnMenuButtonClicked);
                _root.Q<Button>("HideMenuButton")?.UnregisterCallback<ClickEvent>(OnHideMenuButtonClicked);
                _root.Q<Button>("ManagedCloudServiceButton")?.UnregisterCallback<ClickEvent>(OnManagedCloudServiceButtonClicked);


                // Eventos de navegación principal
                _root.Q<Button>("OperationsButton")?.UnregisterCallback<ClickEvent>(OnOperationsButtonClicked);
                _root.Q<Button>("TrainingButton")?.UnregisterCallback<ClickEvent>(OnTrainingButtonClicked);
                _root.Q<Button>("ReportsButton")?.UnregisterCallback<ClickEvent>(OnReportsButtonClicked);
                _root.Q<Button>("SupportButton")?.UnregisterCallback<ClickEvent>(OnSupportButtonClicked);
                _root.Q<Button>("SettingsButton")?.UnregisterCallback<ClickEvent>(OnSettingsButtonClicked);
                _root.Q<Button>("LogoutButton")?.UnregisterCallback<ClickEvent>(OnLogoutButtonClicked);

                // Eventos de paneles adicionales
                _root.Q<Button>("NotificationsButton")?.UnregisterCallback<ClickEvent>(OnNotificationsButtonClicked);
                _root.Q<Button>("UserProfileButton")?.UnregisterCallback<ClickEvent>(OnUserProfileButtonClicked);
                _root.Q<Button>("QuickActionsButton")?.UnregisterCallback<ClickEvent>(OnQuickActionsButtonClicked);
                _root.Q<Button>("StatusOverlayButton")?.UnregisterCallback<ClickEvent>(OnStatusOverlayButtonClicked);

                // Eventos de cierre de paneles
                _root.Q<Button>("CloseNotificationsButton")?.UnregisterCallback<ClickEvent>(OnCloseNotificationsPanelClicked);
                _root.Q<Button>("CloseUserProfileButton")?.UnregisterCallback<ClickEvent>(OnCloseUserProfilePanelClicked);
                _root.Q<Button>("CloseQuickActionsButton")?.UnregisterCallback<ClickEvent>(OnCloseQuickActionsPanelClicked);
                _root.Q<Button>("CloseStatusOverlayButton")?.UnregisterCallback<ClickEvent>(OnCloseStatusOverlayPanelClicked);

                // Evento de transición del menú de navegación
                var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
                navigationMenuPanel?.UnregisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

                // Eventos de clic en el scrim
                var scrim = _root.Q<VisualElement>("Scrim");
                scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);

                // Desregistrar eventos de teclado
                UnregisterKeyboardEvents(_root);

                Debug.Log("[IotSettingsEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IotSettingsEventManager] Error unregistering events: {ex.Message}");
            }
        }

        /// <summary>
        /// Método Cleanup consistente con WelcomeEventManager
        /// </summary>
        public void Cleanup()
        {
            UnregisterEvents();
            
            _uiManager = null;
            _orchestrator = null;
            _uiDocument = null;
            _root = null;
            _onLogoutRequested = null;
            _onPanelTransitionComplete = null;

            Debug.Log("[IotSettingsEventManager] Event Manager cleaned up");
        }

        #endregion

        #region Keyboard Events

        /// <summary>
        /// Registra eventos de teclado para el IotSettings
        /// </summary>
        private void RegisterKeyboardEvents(VisualElement root)
        {
            // Escape para cerrar paneles/menú
            root.RegisterCallback<KeyDownEvent>(OnGlobalKeyDown);
            
            // M para toggle del menú (si se quiere)
            // Aquí puedes agregar más shortcuts de teclado
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

        /// <summary>
        /// Maneja la tecla Escape
        /// </summary>
        private async void HandleEscapeKey()
        {
            // Cerrar panel actual o menú lateral
            if (_uiManager.CurrentActivePanel != IIotSettingsOps.PanelType.None)
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
                context: "IotSettings_main"
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
            // Solo cerrar si el clic fue directamente en el scrim, no en sus hijos
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
        /// Abre Settings
        /// </summary>
        private async void OnSettingsButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "SettingsButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleSettingsClick();
        }
        /// <summary>
        /// Abre Support Center
        /// </summary>
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
        
        // <summary>
        /// Abre Reports Center
        /// </summary>
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

        #region Panel Events

        /// <summary>
        /// Abre panel de notificaciones
        /// </summary>
        private async void OnNotificationsButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowPanel(IIotSettingsOps.PanelType.Notifications);
            await UIAnalyticsManager.Instance?.TrackPanelTransition(
                fromPanel: "IotSettings_main",
                toPanel: "modal_notifications",
                transitionType: "button_click"
            );
        }

        /// <summary>
        /// Abre panel de perfil de usuario
        /// </summary>
        private void OnUserProfileButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowPanel(IIotSettingsOps.PanelType.UserProfile);
        }

        /// <summary>
        /// Abre panel de acciones rápidas
        /// </summary>
        private void OnQuickActionsButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowPanel(IIotSettingsOps.PanelType.QuickActions);
        }

        /// <summary>
        /// Abre panel de estado de IoT
        /// </summary>
        private void OnStatusOverlayButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowPanel(IIotSettingsOps.PanelType.StatusOverlay);
        }

        #endregion

        #region Panel Close Events

        /// <summary>
        /// Cierra panel de notificaciones
        /// </summary>
        private async void OnCloseNotificationsPanelClicked(ClickEvent evt)
        {
            _uiManager.HidePanel(IIotSettingsOps.PanelType.Notifications);
            await UIAnalyticsManager.Instance?.TrackPanelTransition(
                fromPanel: "modal_notifications",
                toPanel: "none", 
                transitionType: "close_button_click"
            );
        }

        /// <summary>
        /// Cierra panel de perfil de usuario
        /// </summary>
        private void OnCloseUserProfilePanelClicked(ClickEvent evt)
        {
            _uiManager.HidePanel(IIotSettingsOps.PanelType.UserProfile);
        }

        /// <summary>
        /// Cierra panel de acciones rápidas
        /// </summary>
        private void OnCloseQuickActionsPanelClicked(ClickEvent evt)
        {
            _uiManager.HidePanel(IIotSettingsOps.PanelType.QuickActions);
        }

        /// <summary>
        /// Cierra panel de estado de IoT
        /// </summary>
        private void OnCloseStatusOverlayPanelClicked(ClickEvent evt)
        {
            _uiManager.HidePanel(IIotSettingsOps.PanelType.StatusOverlay);
        }

        #endregion

        #region Transition Events

        /// <summary>
        /// Maneja el final de la transición del menú de navegación
        /// </summary>
        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            // Similar al patrón de Welcome - notificar completion
            _onPanelTransitionComplete?.Invoke(_uiManager.CurrentActivePanel);
            
            // Si no hay paneles visibles, el UIManager ya maneja ocultar el container
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// UI Manager asociado
        /// </summary>
        public IotSettingsUIManager UIManager => _uiManager;

        /// <summary>
        /// Orchestrator asociado
        /// </summary>
        public IotSettingsOrchestrator Orchestrator => _orchestrator;

        #endregion
        /// <summary>
        /// Maneja clic en botón ManagedCloudService
        /// </summary>
        private async void OnManagedCloudServiceButtonClicked(ClickEvent evt)
        {
            Debug.Log("ManagedCloudService button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackButtonClick(
                buttonName: "ManagedCloudServiceButton",
                context: "IotSettings_main",
                additionalData: new Dictionary<string, object>
                {
                    ["navigation_target"] = "ManagedCloudService",
                    ["source_ui"] = "IotSettings"
                }
            );
            _orchestrator.HandleManagedCloudServiceClick();
        }
    }
}