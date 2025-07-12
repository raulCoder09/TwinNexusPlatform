using System;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.AwsSettingsController
{
    /// <summary>
    /// Maneja todos los eventos UI de AWS Settings
    /// Equivalente a DashboardEventManager pero para AWS Settings
    /// </summary>
    public class AwsSettingsEventManager
    {
        private AwsSettingsUIManager _uiManager;
        private Action _onReturnToDashboard;
        private Action<IAwsSettingsOps.PanelType> _onPanelTransitionComplete;
        private AwsSettingsOrchestrator _orchestrator;

        // Referencias para poder desregistrar eventos
        private UIDocument _uiDocument;
        private VisualElement _root;

        #region Constructor

        public AwsSettingsEventManager(
            AwsSettingsUIManager uiManager, 
            Action onReturnToDashboard, 
            Action<IAwsSettingsOps.PanelType> onPanelTransitionComplete, 
            AwsSettingsOrchestrator orchestrator)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _onReturnToDashboard = onReturnToDashboard ?? throw new ArgumentNullException(nameof(onReturnToDashboard));
            _onPanelTransitionComplete = onPanelTransitionComplete ?? throw new ArgumentNullException(nameof(onPanelTransitionComplete));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        #endregion

        #region Event Registration

        /// <summary>
        /// Registra todos los eventos de AWS Settings
        /// </summary>
        public void RegisterEvents(UIDocument uiDocument)
        {
            // Guardar referencias para desregistro posterior
            _uiDocument = uiDocument;
            _root = uiDocument.rootVisualElement;

            // Eventos principales del menú
            _root.Q<Button>("MenuButton")?.RegisterCallback<ClickEvent>(OnMenuButtonClicked);
            _root.Q<Button>("HideMenuButton")?.RegisterCallback<ClickEvent>(OnHideMenuButtonClicked);

            // Eventos de navegación principal (desde el menú lateral)
            _root.Q<Button>("OperationsButton")?.RegisterCallback<ClickEvent>(OnOperationsButtonClicked);
            _root.Q<Button>("TrainingButton")?.RegisterCallback<ClickEvent>(OnTrainingButtonClicked);
            _root.Q<Button>("ReportsButton")?.RegisterCallback<ClickEvent>(OnReportsButtonClicked);
            _root.Q<Button>("SupportButton")?.RegisterCallback<ClickEvent>(OnSupportButtonClicked); 
            _root.Q<Button>("SettingsButton")?.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);
            _root.Q<Button>("LogoutButton")?.RegisterCallback<ClickEvent>(OnLogoutButtonClicked);

            // Buscar el botón Dashboard para regresar
            var dashboardButtons = _root.Query<Button>().Where(btn => btn.text == "Dashboard").ToList();
            foreach (var dashboardButton in dashboardButtons)
            {
                dashboardButton.RegisterCallback<ClickEvent>(OnDashboardButtonClicked);
            }

            // Eventos de paneles adicionales (futuros)
            // TODO: Agregar cuando se implementen paneles específicos de AWS
            // _root.Q<Button>("CredentialsButton")?.RegisterCallback<ClickEvent>(OnCredentialsButtonClicked);
            // _root.Q<Button>("ServicesButton")?.RegisterCallback<ClickEvent>(OnServicesButtonClicked);

            // Evento de transición del menú de navegación
            var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
            navigationMenuPanel?.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

            // Eventos de clic en el scrim para cerrar paneles
            var scrim = _root.Q<VisualElement>("Scrim");
            scrim?.RegisterCallback<ClickEvent>(OnScrimClicked);

            // Registrar eventos de teclado
            RegisterKeyboardEvents(_root);

            Debug.Log("[AwsSettingsEventManager] All events registered successfully");
        }

        /// <summary>
        /// Desregistra todos los eventos de AWS Settings
        /// </summary>
        public void UnregisterEvents()
        {
            try
            {
                Debug.Log("[AwsSettingsEventManager] Unregistering AWS Settings events...");

                if (_root == null)
                {
                    Debug.LogWarning("[AwsSettingsEventManager] Root element is null - cannot unregister events");
                    return;
                }

                // Eventos principales del menú
                _root.Q<Button>("MenuButton")?.UnregisterCallback<ClickEvent>(OnMenuButtonClicked);
                _root.Q<Button>("HideMenuButton")?.UnregisterCallback<ClickEvent>(OnHideMenuButtonClicked);

                // Eventos de navegación principal
                _root.Q<Button>("OperationsButton")?.UnregisterCallback<ClickEvent>(OnOperationsButtonClicked);
                _root.Q<Button>("TrainingButton")?.UnregisterCallback<ClickEvent>(OnTrainingButtonClicked);
                _root.Q<Button>("ReportsButton")?.UnregisterCallback<ClickEvent>(OnReportsButtonClicked);
                _root.Q<Button>("SupportButton")?.UnregisterCallback<ClickEvent>(OnSupportButtonClicked);
                _root.Q<Button>("SettingsButton")?.UnregisterCallback<ClickEvent>(OnSettingsButtonClicked);
                _root.Q<Button>("LogoutButton")?.UnregisterCallback<ClickEvent>(OnLogoutButtonClicked);

                // Desregistrar botones Dashboard
                var dashboardButtons = _root.Query<Button>().Where(btn => btn.text == "Dashboard").ToList();
                foreach (var dashboardButton in dashboardButtons)
                {
                    dashboardButton.UnregisterCallback<ClickEvent>(OnDashboardButtonClicked);
                }

                // Evento de transición del menú de navegación
                var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
                navigationMenuPanel?.UnregisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

                // Eventos de clic en el scrim
                var scrim = _root.Q<VisualElement>("Scrim");
                scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);

                // Desregistrar eventos de teclado
                UnregisterKeyboardEvents(_root);

                Debug.Log("[AwsSettingsEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AwsSettingsEventManager] Error unregistering events: {ex.Message}");
            }
        }

        /// <summary>
        /// Método Cleanup consistente con el patrón del sistema
        /// </summary>
        public void Cleanup()
        {
            UnregisterEvents();
            
            _uiManager = null;
            _orchestrator = null;
            _uiDocument = null;
            _root = null;
            _onReturnToDashboard = null;
            _onPanelTransitionComplete = null;

            Debug.Log("[AwsSettingsEventManager] Event Manager cleaned up");
        }

        #endregion

        #region Keyboard Events

        /// <summary>
        /// Registra eventos de teclado para AWS Settings
        /// </summary>
        private void RegisterKeyboardEvents(VisualElement root)
        {
            // Escape para cerrar paneles/menú
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
            if (_uiManager.CurrentActivePanel != IAwsSettingsOps.PanelType.None)
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
                context: "aws_settings_main"
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
        /// Regresa al Dashboard
        /// </summary>
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
        /// Abre Settings (esto sería recursivo, así que lo mantenemos en AWS Settings)
        /// </summary>
        private async void OnSettingsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Settings button clicked - already in AWS Settings");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "SettingsButton",
                context: "navigation_menu"
            );
            
            // Simplemente cerrar el menú ya que estamos en Settings
            _uiManager.HideNavigationMenu();
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
        
        /// <summary>
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

        #region AWS Settings Specific Events (Futuros)

        /// <summary>
        /// Maneja eventos específicos de AWS Settings cuando se implementen
        /// </summary>
        
        // TODO: Implementar cuando se agregue contenido específico
        // private async void OnCredentialsButtonClicked(ClickEvent evt) { ... }
        // private async void OnTestConnectionButtonClicked(ClickEvent evt) { ... }
        // private async void OnSaveConfigurationButtonClicked(ClickEvent evt) { ... }

        #endregion

        #region Transition Events

        /// <summary>
        /// Maneja el final de la transición del menú de navegación
        /// </summary>
        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            // Similar al patrón del Dashboard - notificar completion
            _onPanelTransitionComplete?.Invoke(_uiManager.CurrentActivePanel);
            
            // Si no hay paneles visibles, el UIManager ya maneja ocultar el container
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// UI Manager asociado
        /// </summary>
        public AwsSettingsUIManager UIManager => _uiManager;

        /// <summary>
        /// Orchestrator asociado
        /// </summary>
        public AwsSettingsOrchestrator Orchestrator => _orchestrator;

        #endregion
    }
}