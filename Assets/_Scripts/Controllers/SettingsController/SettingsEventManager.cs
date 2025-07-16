using System;
using _Scripts.Controllers.UiManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.SettingsController
{
    /// <summary>
    /// Maneja todos los eventos UI del Settings
    /// Equivalente a WelcomeEventManager, DashboardEventManager y DeviceSelectionEventManager
    /// </summary>
    public class SettingsEventManager
    {
        private SettingsUIManager _uiManager;
        private Action _onReturnToDashboard;
        private Action<ISettingsOps.PanelType> _onPanelTransitionComplete;
        private SettingsOrchestrator _orchestrator;

        // Referencias para poder desregistrar eventos
        private UIDocument _uiDocument;
        private VisualElement _root;

        #region Constructor

        public SettingsEventManager(
            SettingsUIManager uiManager, 
            Action onReturnToDashboard, 
            Action<ISettingsOps.PanelType> onPanelTransitionComplete, 
            SettingsOrchestrator orchestrator)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _onReturnToDashboard = onReturnToDashboard ?? throw new ArgumentNullException(nameof(onReturnToDashboard));
            _onPanelTransitionComplete = onPanelTransitionComplete ?? throw new ArgumentNullException(nameof(onPanelTransitionComplete));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        #endregion

        #region Event Registration

        /// <summary>
        /// Registra todos los eventos del Settings
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
            _root.Q<Button>("OperationsButton")?.RegisterCallback<ClickEvent>(OnOperationsButtonClicked);
            _root.Q<Button>("TrainingButton")?.RegisterCallback<ClickEvent>(OnTrainingButtonClicked);
            _root.Q<Button>("ReportsButton")?.RegisterCallback<ClickEvent>(OnReportsButtonClicked);
            _root.Q<Button>("SupportButton")?.RegisterCallback<ClickEvent>(OnSupportButtonClicked);
            _root.Q<Button>("SettingsButton")?.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);
            _root.Q<Button>("LogoutButton")?.RegisterCallback<ClickEvent>(OnLogoutButtonClicked);

            // Eventos de configuraciones - DINÁMICOS
            RegisterConfigurationEvents(_root);

            // Eventos de paneles adicionales (futuros)
            _root.Q<Button>("HelpButton")?.RegisterCallback<ClickEvent>(OnHelpButtonClicked);

            // Eventos de cierre de paneles
            _root.Q<Button>("CloseHelpButton")?.RegisterCallback<ClickEvent>(OnCloseHelpPanelClicked);

            // Evento de transición del menú de navegación
            var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
            navigationMenuPanel?.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

            // Eventos de clic en el scrim para cerrar paneles
            var scrim = _root.Q<VisualElement>("Scrim");
            scrim?.RegisterCallback<ClickEvent>(OnScrimClicked);

            // Registrar eventos de teclado
            RegisterKeyboardEvents(_root);

            Debug.Log("[SettingsEventManager] All events registered successfully");
        }

        /// <summary>
        /// Registra eventos específicos de configuraciones de forma dinámica
        /// </summary>
        private void RegisterConfigurationEvents(VisualElement root)
        {
            // Configuraciones principales (del UXML actual)
            _root.Q<Button>("IoTButton")?.RegisterCallback<ClickEvent>(OnConfigurationButtonClicked);

            // Configuraciones futuras que se pueden agregar
            _root.Q<Button>("UserConfigButton")?.RegisterCallback<ClickEvent>(OnConfigurationButtonClicked);
            _root.Q<Button>("CognitoConfigButton")?.RegisterCallback<ClickEvent>(OnConfigurationButtonClicked);
            _root.Q<Button>("SystemConfigButton")?.RegisterCallback<ClickEvent>(OnConfigurationButtonClicked);
            _root.Q<Button>("NetworkConfigButton")?.RegisterCallback<ClickEvent>(OnConfigurationButtonClicked);

            // También registrar eventos para cualquier botón con clase config-button
            var configButtons = root.Query<Button>(className: "config-button").ToList();
            foreach (var button in configButtons)
            {
                button.RegisterCallback<ClickEvent>(OnConfigurationButtonClicked);
            }

            Debug.Log($"[SettingsEventManager] Registered events for configuration buttons");
        }

        /// <summary>
        /// Desregistra todos los eventos del Settings
        /// </summary>
        public void UnregisterEvents()
        {
            try
            {
                Debug.Log("[SettingsEventManager] Unregistering Settings events...");

                if (_root == null)
                {
                    Debug.LogWarning("[SettingsEventManager] Root element is null - cannot unregister events");
                    return;
                }

                // Eventos principales del menú
                _root.Q<Button>("MenuButton")?.UnregisterCallback<ClickEvent>(OnMenuButtonClicked);
                _root.Q<Button>("HideMenuButton")?.UnregisterCallback<ClickEvent>(OnHideMenuButtonClicked);

                // Eventos de navegación principal
                _root.Q<Button>("DashboardButton")?.UnregisterCallback<ClickEvent>(OnDashboardButtonClicked);
                _root.Q<Button>("OperationsButton")?.UnregisterCallback<ClickEvent>(OnOperationsButtonClicked);
                _root.Q<Button>("TrainingButton")?.UnregisterCallback<ClickEvent>(OnTrainingButtonClicked);
                _root.Q<Button>("ReportsButton")?.UnregisterCallback<ClickEvent>(OnReportsButtonClicked);
                _root.Q<Button>("SupportButton")?.UnregisterCallback<ClickEvent>(OnSupportButtonClicked);
                _root.Q<Button>("SettingsButton")?.UnregisterCallback<ClickEvent>(OnSettingsButtonClicked);
                _root.Q<Button>("LogoutButton")?.UnregisterCallback<ClickEvent>(OnLogoutButtonClicked);

                // Eventos de configuraciones
                UnregisterConfigurationEvents(_root);

                // Eventos de paneles adicionales
                _root.Q<Button>("HelpButton")?.UnregisterCallback<ClickEvent>(OnHelpButtonClicked);

                // Eventos de cierre de paneles
                _root.Q<Button>("CloseHelpButton")?.UnregisterCallback<ClickEvent>(OnCloseHelpPanelClicked);

                // Evento de transición del menú de navegación
                var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
                navigationMenuPanel?.UnregisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

                // Eventos de clic en el scrim
                var scrim = _root.Q<VisualElement>("Scrim");
                scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);

                // Desregistrar eventos de teclado
                UnregisterKeyboardEvents(_root);

                Debug.Log("[SettingsEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SettingsEventManager] Error unregistering events: {ex.Message}");
            }
        }

        /// <summary>
        /// Desregistra eventos específicos de configuraciones
        /// </summary>
        private void UnregisterConfigurationEvents(VisualElement root)
        {
            // Configuraciones principales
            _root.Q<Button>("IoTButton")?.UnregisterCallback<ClickEvent>(OnConfigurationButtonClicked);
            _root.Q<Button>("UserConfigButton")?.UnregisterCallback<ClickEvent>(OnConfigurationButtonClicked);
            _root.Q<Button>("CognitoConfigButton")?.UnregisterCallback<ClickEvent>(OnConfigurationButtonClicked);
            _root.Q<Button>("SystemConfigButton")?.UnregisterCallback<ClickEvent>(OnConfigurationButtonClicked);
            _root.Q<Button>("NetworkConfigButton")?.UnregisterCallback<ClickEvent>(OnConfigurationButtonClicked);

            // Botones con clase config-button
            var configButtons = root.Query<Button>(className: "config-button").ToList();
            foreach (var button in configButtons)
            {
                button.UnregisterCallback<ClickEvent>(OnConfigurationButtonClicked);
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
            _onReturnToDashboard = null;
            _onPanelTransitionComplete = null;

            Debug.Log("[SettingsEventManager] Event Manager cleaned up");
        }

        #endregion

        #region Keyboard Events

        /// <summary>
        /// Registra eventos de teclado para el Settings
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
        private void OnGlobalKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    HandleEscapeKey();
                    break;
                    
                case KeyCode.M when evt.ctrlKey: // Ctrl+M para toggle menú
                    _uiManager.ToggleNavigationMenu();
                    break;
                    
                case KeyCode.D when evt.ctrlKey: // Ctrl+D para regresar a Dashboard
                    _orchestrator.HandleReturnToDashboard();
                    break;

                case KeyCode.I when evt.ctrlKey: // Ctrl+I para IoT config
                    _orchestrator.HandleConfigurationClick(ISettingsOps.ConfigurationType.IoT);
                    break;
            }
        }

        /// <summary>
        /// Maneja la tecla Escape
        /// </summary>
        private void HandleEscapeKey()
        {
            // Cerrar panel actual o menú lateral
            if (_uiManager.CurrentActivePanel != ISettingsOps.PanelType.None)
            {
                _uiManager.CloseCurrentPanel();
            }
            else if (_uiManager.NavigationMenuOpen)
            {
                _uiManager.HideNavigationMenu();
            }
            else
            {
                // Si no hay paneles abiertos, regresar a Dashboard
                _orchestrator.HandleReturnToDashboard();
            }
        }

        #endregion

        #region Main Navigation Events

        /// <summary>
        /// Abre el menú lateral de navegación
        /// </summary>
        private void OnMenuButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowNavigationMenu();
        }

        /// <summary>
        /// Cierra el menú lateral de navegación
        /// </summary>
        private void OnHideMenuButtonClicked(ClickEvent evt)
        {
            _uiManager.HideNavigationMenu();
        }

        /// <summary>
        /// Maneja clic en el scrim para cerrar paneles
        /// </summary>
        private void OnScrimClicked(ClickEvent evt)
        {
            // Solo cerrar si el clic fue directamente en el scrim, no en sus hijos
            if (evt.target == evt.currentTarget)
            {
                _uiManager.CloseCurrentPanel();
            }
        }

        #endregion

        #region Navigation Action Events

        /// <summary>
        /// Regresa al Dashboard
        /// </summary>
        private void OnDashboardButtonClicked(ClickEvent evt)
        {
            Debug.Log("Dashboard button clicked - returning to dashboard");
            _orchestrator.HandleReturnToDashboard();
        }

        /// <summary>
        /// Navega a Operations (vía DeviceSelection)
        /// </summary>
        private void OnOperationsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Operations button clicked - navigating to operations");
            _orchestrator.HandleOperationsClick();
        }

        /// <summary>
        /// Navega a Training (vía DeviceSelection)
        /// </summary>
        private void OnTrainingButtonClicked(ClickEvent evt)
        {
            Debug.Log("Training button clicked - navigating to training");
            _orchestrator.HandleTrainingClick();
        }
        
        private async void OnReportsButtonClicked(ClickEvent evt)
        {
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "ReportsButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleReportsClick();
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

        /// <summary>
        /// Settings button - mantiene en Settings activo
        /// </summary>
        private void OnSettingsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Settings button clicked - staying in settings");
            // Cerrar menú si está abierto
            _uiManager.HideNavigationMenu();
        }

        /// <summary>
        /// Ejecuta Logout
        /// </summary>
        private void OnLogoutButtonClicked(ClickEvent evt)
        {
            Debug.Log("Logout button clicked - executing logout");
            _orchestrator.HandleLogoutClick();
        }

        #endregion

        #region Configuration Events

        /// <summary>
        /// Maneja clic en cualquier botón de configuración
        /// </summary>
        private void OnConfigurationButtonClicked(ClickEvent evt)
        {
            if (evt.currentTarget is Button button)
            {
                // Extraer configuration type del nombre del botón
                var configType = ExtractConfigurationTypeFromButton(button);
                
                if (configType != null)
                {
                    Debug.Log($"Configuration button clicked: {configType}");
                    _orchestrator.HandleConfigurationClick(configType.Value);
                }
                else
                {
                    Debug.LogWarning($"Could not extract configuration type from button: {button.name}");
                }
            }
        }

        /// <summary>
        /// Extrae el configuration type del nombre del botón
        /// </summary>
        private ISettingsOps.ConfigurationType? ExtractConfigurationTypeFromButton(Button button)
        {
            // Mapeo de nombres de botones a configuration types
            return button.name switch
            {
                "IoTButton" => ISettingsOps.ConfigurationType.IoT,
                "UserConfigButton" => ISettingsOps.ConfigurationType.User,
                "CognitoConfigButton" => ISettingsOps.ConfigurationType.Cognito,
                "SystemConfigButton" => ISettingsOps.ConfigurationType.System,
                "NetworkConfigButton" => ISettingsOps.ConfigurationType.Network,
                "AwsServicesConfigButton" => ISettingsOps.ConfigurationType.AwsServices,
                _ => null // No reconocido
            };
        }

        #endregion

        #region Panel Events

        /// <summary>
        /// Abre panel de ayuda
        /// </summary>
        private void OnHelpButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowPanel(ISettingsOps.PanelType.Help);
        }

        #endregion

        #region Panel Close Events

        /// <summary>
        /// Cierra panel de ayuda
        /// </summary>
        private void OnCloseHelpPanelClicked(ClickEvent evt)
        {
            _uiManager.HidePanel(ISettingsOps.PanelType.Help);
        }

        #endregion

        #region Transition Events

        /// <summary>
        /// Maneja el final de la transición del menú de navegación
        /// </summary>
        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            // Similar al patrón de otros controladores - notificar completion
            _onPanelTransitionComplete?.Invoke(_uiManager.CurrentActivePanel);
            
            // Si no hay paneles visibles, el UIManager ya maneja ocultar el container
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// UI Manager asociado
        /// </summary>
        public SettingsUIManager UIManager => _uiManager;

        /// <summary>
        /// Orchestrator asociado
        /// </summary>
        public SettingsOrchestrator Orchestrator => _orchestrator;

        #endregion
    }
}