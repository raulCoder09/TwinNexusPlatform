using System;
using _Scripts.Controllers.UiManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.DeviceSelectionController
{
    /// <summary>
    /// Maneja todos los eventos UI del Device Selection
    /// Equivalente a WelcomeEventManager y DashboardEventManager pero para Device Selection
    /// </summary>
    public class DeviceSelectionEventManager
    {
        private DeviceSelectionUIManager _uiManager;
        private Action _onReturnToDashboard;
        private Action<IDeviceSelectionOps.PanelType> _onPanelTransitionComplete;
        private DeviceSelectionOrchestrator _orchestrator;

        // Referencias para poder desregistrar eventos
        private UIDocument _uiDocument;
        private VisualElement _root;

        #region Constructor

        public DeviceSelectionEventManager(
            DeviceSelectionUIManager uiManager, 
            Action onReturnToDashboard, 
            Action<IDeviceSelectionOps.PanelType> onPanelTransitionComplete, 
            DeviceSelectionOrchestrator orchestrator)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _onReturnToDashboard = onReturnToDashboard ?? throw new ArgumentNullException(nameof(onReturnToDashboard));
            _onPanelTransitionComplete = onPanelTransitionComplete ?? throw new ArgumentNullException(nameof(onPanelTransitionComplete));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        #endregion

        #region Event Registration

        /// <summary>
        /// Registra todos los eventos del Device Selection
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
            _root.Q<Button>("LogoutButton")?.RegisterCallback<ClickEvent>(OnLogoutButtonClicked);

            // Eventos de dispositivos - DINÁMICOS
            RegisterDeviceEvents(_root);

            // Eventos de paneles adicionales (futuros)
            _root.Q<Button>("DeviceInfoButton")?.RegisterCallback<ClickEvent>(OnDeviceInfoButtonClicked);
            _root.Q<Button>("ContextInfoButton")?.RegisterCallback<ClickEvent>(OnContextInfoButtonClicked);
            _root.Q<Button>("SettingsButton")?.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);
            _root.Q<Button>("HelpButton")?.RegisterCallback<ClickEvent>(OnHelpButtonClicked);

            // Eventos de cierre de paneles
            _root.Q<Button>("CloseDeviceInfoButton")?.RegisterCallback<ClickEvent>(OnCloseDeviceInfoPanelClicked);
            _root.Q<Button>("CloseContextInfoButton")?.RegisterCallback<ClickEvent>(OnCloseContextInfoPanelClicked);
            _root.Q<Button>("CloseSettingsButton")?.RegisterCallback<ClickEvent>(OnCloseSettingsPanelClicked);
            _root.Q<Button>("CloseHelpButton")?.RegisterCallback<ClickEvent>(OnCloseHelpPanelClicked);

            // Evento de transición del menú de navegación
            var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
            navigationMenuPanel?.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

            // Eventos de clic en el scrim para cerrar paneles
            var scrim = _root.Q<VisualElement>("Scrim");
            scrim?.RegisterCallback<ClickEvent>(OnScrimClicked);

            // Registrar eventos de teclado
            RegisterKeyboardEvents(_root);

            Debug.Log("[DeviceSelectionEventManager] All events registered successfully");
        }

        /// <summary>
        /// Registra eventos específicos de dispositivos de forma dinámica
        /// </summary>
        private void RegisterDeviceEvents(VisualElement root)
        {
            // Dispositivos principales (del código original)
            _root.Q<Button>("ARSCARAButton")?.RegisterCallback<ClickEvent>(OnDeviceButtonClicked);
            _root.Q<Button>("RobotKit1Button")?.RegisterCallback<ClickEvent>(OnDeviceButtonClicked);
            _root.Q<Button>("RobotKit2Button")?.RegisterCallback<ClickEvent>(OnDeviceButtonClicked);

            // También registrar eventos para cualquier botón con clase device-button
            var deviceButtons = root.Query<Button>(className: "device-button").ToList();
            foreach (var button in deviceButtons)
            {
                button.RegisterCallback<ClickEvent>(OnDeviceButtonClicked);
            }

            Debug.Log($"[DeviceSelectionEventManager] Registered events for {deviceButtons.Count} device buttons");
        }

        /// <summary>
        /// Desregistra todos los eventos del Device Selection
        /// </summary>
        public void UnregisterEvents()
        {
            try
            {
                Debug.Log("[DeviceSelectionEventManager] Unregistering Device Selection events...");

                if (_root == null)
                {
                    Debug.LogWarning("[DeviceSelectionEventManager] Root element is null - cannot unregister events");
                    return;
                }

                // Eventos principales del menú
                _root.Q<Button>("MenuButton")?.UnregisterCallback<ClickEvent>(OnMenuButtonClicked);
                _root.Q<Button>("HideMenuButton")?.UnregisterCallback<ClickEvent>(OnHideMenuButtonClicked);

                // Eventos de navegación principal
                _root.Q<Button>("DashboardButton")?.UnregisterCallback<ClickEvent>(OnDashboardButtonClicked);
                _root.Q<Button>("OperationsButton")?.UnregisterCallback<ClickEvent>(OnOperationsButtonClicked);
                _root.Q<Button>("TrainingButton")?.UnregisterCallback<ClickEvent>(OnTrainingButtonClicked);

                // Eventos de dispositivos
                UnregisterDeviceEvents(_root);

                // Eventos de paneles adicionales
                _root.Q<Button>("DeviceInfoButton")?.UnregisterCallback<ClickEvent>(OnDeviceInfoButtonClicked);
                _root.Q<Button>("ContextInfoButton")?.UnregisterCallback<ClickEvent>(OnContextInfoButtonClicked);
                _root.Q<Button>("SettingsButton")?.UnregisterCallback<ClickEvent>(OnSettingsButtonClicked);
                _root.Q<Button>("HelpButton")?.UnregisterCallback<ClickEvent>(OnHelpButtonClicked);

                // Eventos de cierre de paneles
                _root.Q<Button>("CloseDeviceInfoButton")?.UnregisterCallback<ClickEvent>(OnCloseDeviceInfoPanelClicked);
                _root.Q<Button>("CloseContextInfoButton")?.UnregisterCallback<ClickEvent>(OnCloseContextInfoPanelClicked);
                _root.Q<Button>("CloseSettingsButton")?.UnregisterCallback<ClickEvent>(OnCloseSettingsPanelClicked);
                _root.Q<Button>("CloseHelpButton")?.UnregisterCallback<ClickEvent>(OnCloseHelpPanelClicked);
                
                // En la sección de eventos de navegación principal
                _root.Q<Button>("ReportsButton")?.UnregisterCallback<ClickEvent>(OnReportsButtonClicked);
                _root.Q<Button>("SupportButton")?.UnregisterCallback<ClickEvent>(OnSupportButtonClicked);
                _root.Q<Button>("SettingsButton")?.UnregisterCallback<ClickEvent>(OnSettingsButtonClicked);
                _root.Q<Button>("LogoutButton")?.UnregisterCallback<ClickEvent>(OnLogoutButtonClicked);

                // Evento de transición del menú de navegación
                var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
                navigationMenuPanel?.UnregisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

                // Eventos de clic en el scrim
                var scrim = _root.Q<VisualElement>("Scrim");
                scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);

                // Desregistrar eventos de teclado
                UnregisterKeyboardEvents(_root);

                Debug.Log("[DeviceSelectionEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeviceSelectionEventManager] Error unregistering events: {ex.Message}");
            }
        }

        /// <summary>
        /// Desregistra eventos específicos de dispositivos
        /// </summary>
        private void UnregisterDeviceEvents(VisualElement root)
        {
            // Dispositivos principales
            _root.Q<Button>("ARSCARAButton")?.UnregisterCallback<ClickEvent>(OnDeviceButtonClicked);
            _root.Q<Button>("RobotKit1Button")?.UnregisterCallback<ClickEvent>(OnDeviceButtonClicked);
            _root.Q<Button>("RobotKit2Button")?.UnregisterCallback<ClickEvent>(OnDeviceButtonClicked);

            // Botones con clase device-button
            var deviceButtons = root.Query<Button>(className: "device-button").ToList();
            foreach (var button in deviceButtons)
            {
                button.UnregisterCallback<ClickEvent>(OnDeviceButtonClicked);
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

            Debug.Log("[DeviceSelectionEventManager] Event Manager cleaned up");
        }

        #endregion

        #region Keyboard Events

        /// <summary>
        /// Registra eventos de teclado para el Device Selection
        /// </summary>
        private void RegisterKeyboardEvents(VisualElement root)
        {
            // Escape para cerrar paneles/menú
            root.RegisterCallback<KeyDownEvent>(OnGlobalKeyDown);
            
            // Números 1-3 para selección rápida de dispositivos
            // M para toggle del menú
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
                    
                case KeyCode.Alpha1: // Tecla 1 - ARSCARA
                    _orchestrator.HandleDeviceSelection("ARSCARA");
                    break;
                    
                case KeyCode.Alpha2: // Tecla 2 - RobotKit1
                    _orchestrator.HandleDeviceSelection("RobotKit1");
                    break;
                    
                case KeyCode.Alpha3: // Tecla 3 - RobotKit2
                    _orchestrator.HandleDeviceSelection("RobotKit2");
                    break;
                    
                case KeyCode.D when evt.ctrlKey: // Ctrl+D para regresar a Dashboard
                    _orchestrator.HandleReturnToDashboard();
                    break;
            }
        }

        /// <summary>
        /// Maneja la tecla Escape
        /// </summary>
        private void HandleEscapeKey()
        {
            // Cerrar panel actual o menú lateral
            if (_uiManager.CurrentActivePanel != IDeviceSelectionOps.PanelType.None)
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
        /// Cambia contexto a Operations
        /// </summary>
        private void OnOperationsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Operations button clicked - switching context");
            _orchestrator.HandleContextSwitch(IDeviceSelectionOps.LaunchContext.Operations);
        }

        /// <summary>
        /// Cambia contexto a Training
        /// </summary>
        private void OnTrainingButtonClicked(ClickEvent evt)
        {
            Debug.Log("Training button clicked - switching context");
            _orchestrator.HandleContextSwitch(IDeviceSelectionOps.LaunchContext.Training);
        }
        
        /// <summary>
        /// Abre Reports
        /// </summary>
        private async void OnReportsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Reports button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "ReportsButton",
                context: "device_selection_menu"
            );
            _orchestrator.HandleReportsClick();
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
                context: "device_selection_menu"
            );
            _orchestrator.HandleSupportClick();
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
                context: "device_selection_menu"
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
                context: "device_selection_menu"
            );
            _orchestrator.HandleLogoutClick();
        }

        #endregion

        #region Device Events

        /// <summary>
        /// Maneja clic en cualquier botón de dispositivo
        /// </summary>
        private void OnDeviceButtonClicked(ClickEvent evt)
        {
            if (evt.currentTarget is Button button)
            {
                // Extraer device ID del nombre del botón
                string deviceId = ExtractDeviceIdFromButton(button);
                
                if (!string.IsNullOrEmpty(deviceId))
                {
                    Debug.Log($"Device button clicked: {deviceId}");
                    _orchestrator.HandleDeviceSelection(deviceId);
                }
                else
                {
                    Debug.LogWarning($"Could not extract device ID from button: {button.name}");
                }
            }
        }

        /// <summary>
        /// Extrae el device ID del nombre del botón
        /// </summary>
        private string ExtractDeviceIdFromButton(Button button)
        {
            // Mapeo de nombres de botones a device IDs
            return button.name switch
            {
                "ARSCARAButton" => "ARSCARA",
                "RobotKit1Button" => "RobotKit1", 
                "RobotKit2Button" => "RobotKit2",
                _ => button.name.Replace("Button", "") // Fallback: remover "Button" del final
            };
        }

        #endregion

        #region Panel Events

        /// <summary>
        /// Abre panel de información de dispositivo
        /// </summary>
        private void OnDeviceInfoButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowPanel(IDeviceSelectionOps.PanelType.DeviceInfo);
        }

        /// <summary>
        /// Abre panel de información de contexto
        /// </summary>
        private void OnContextInfoButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowPanel(IDeviceSelectionOps.PanelType.ContextInfo);
        }
        

        /// <summary>
        /// Abre panel de ayuda
        /// </summary>
        private void OnHelpButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowPanel(IDeviceSelectionOps.PanelType.Help);
        }

        #endregion

        #region Panel Close Events

        /// <summary>
        /// Cierra panel de información de dispositivo
        /// </summary>
        private void OnCloseDeviceInfoPanelClicked(ClickEvent evt)
        {
            _uiManager.HidePanel(IDeviceSelectionOps.PanelType.DeviceInfo);
        }

        /// <summary>
        /// Cierra panel de información de contexto
        /// </summary>
        private void OnCloseContextInfoPanelClicked(ClickEvent evt)
        {
            _uiManager.HidePanel(IDeviceSelectionOps.PanelType.ContextInfo);
        }

        /// <summary>
        /// Cierra panel de configuraciones
        /// </summary>
        private void OnCloseSettingsPanelClicked(ClickEvent evt)
        {
            _uiManager.HidePanel(IDeviceSelectionOps.PanelType.Settings);
        }

        /// <summary>
        /// Cierra panel de ayuda
        /// </summary>
        private void OnCloseHelpPanelClicked(ClickEvent evt)
        {
            _uiManager.HidePanel(IDeviceSelectionOps.PanelType.Help);
        }

        #endregion

        #region Transition Events

        /// <summary>
        /// Maneja el final de la transición del menú de navegación
        /// </summary>
        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            // Similar al patrón de Dashboard - notificar completion
            _onPanelTransitionComplete?.Invoke(_uiManager.CurrentActivePanel);
            
            // Si no hay paneles visibles, el UIManager ya maneja ocultar el container
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// UI Manager asociado
        /// </summary>
        public DeviceSelectionUIManager UIManager => _uiManager;

        /// <summary>
        /// Orchestrator asociado
        /// </summary>
        public DeviceSelectionOrchestrator Orchestrator => _orchestrator;

        #endregion
    }
}