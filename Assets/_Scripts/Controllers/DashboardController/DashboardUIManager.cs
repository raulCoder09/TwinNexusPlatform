using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.DashboardController
{
    /// <summary>
    /// Gestor de la interfaz de usuario del Dashboard
    /// Responsable de actualizar elementos UI, mostrar/ocultar paneles, y manejar transiciones visuales
    /// Equivalente al WelcomeUIManager pero para Dashboard
    /// </summary>
    public class DashboardUIManager
    {
        #region Private Fields

        private DashboardInfo.UIConfiguration _uiConfig;
        private DashboardInfo.NavigationState _navigationState;
        private DashboardInfo.DashboardState _dashboardState;
        private UIDocument _uiDocument;

        // Constantes para transiciones
        private const float ANIMATION_DURATION = 0.3f;
        private const string MENU_TRANSITION_DURATION = "1s";

        #endregion

        #region Constructor

        public DashboardUIManager(DashboardInfo.UIConfiguration uiConfig, UIDocument uiDocument)
        {
            _uiConfig = uiConfig ?? throw new ArgumentNullException(nameof(uiConfig));
            _uiDocument = uiDocument ?? throw new ArgumentNullException(nameof(uiDocument));
            _navigationState = new DashboardInfo.NavigationState();
            _dashboardState = new DashboardInfo.DashboardState();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Inicializa el UI Manager y obtiene referencias a elementos UI
        /// </summary>
        public bool Initialize()
        {
            try
            {
                Debug.Log("[DashboardUIManager] Initializing UI Manager...");
                
                // ✅ AGREGADO: Verificar que el DOM esté listo
                if (_uiDocument?.rootVisualElement == null)
                {
                    Debug.LogError("[DashboardUIManager] UIDocument or root element is null");
                    return false;
                }

                // ✅ AGREGADO: Delay para asegurar que USS esté cargado
                var root = _uiDocument.rootVisualElement;
                Debug.Log($"[DashboardUIManager] Root element found: {root != null}");
                Debug.Log($"[DashboardUIManager] Root element children count: {root?.childCount ?? 0}");

                if (!GetUIReferences())
                {
                    Debug.LogError("[DashboardUIManager] Failed to get UI references");
                    return false;
                }

                // ✅ REHABILITADO: Ahora que está corregido, podemos inicializar el menú
                InitializeNavigationMenu();
                
                // ✅ OPCIONAL: Usar versión segura de estados por defecto
                InitializeDefaultStatesSafe();
                
                RegisterTransitionCallbacks();

                _dashboardState.IsInitialized = true;
                _dashboardState.TriggerInitialized();

                Debug.Log("[DashboardUIManager] UI Manager initialized successfully");
                
                // ✅ AGREGADO: Log del estado de elementos críticos
                LogUIElementStates();
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DashboardUIManager] Initialization error: {ex.Message}");
                _dashboardState.TriggerError($"Initialization failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene referencias a todos los elementos UI necesarios
        /// </summary>
        private bool GetUIReferences()
        {
            var root = _uiDocument.rootVisualElement;

            // Main UI Elements
            _uiConfig.Body = root.Q<VisualElement>("Body");
            _uiConfig.Header = root.Q<VisualElement>("Header");
            _uiConfig.Main = root.Q<VisualElement>("Main");
            _uiConfig.Footer = root.Q<VisualElement>("Footer");

            // Header Elements
            _uiConfig.MenuButton = root.Q<Button>("MenuButton");
            _uiConfig.UsernameLabel = root.Q<Label>("Username");
            _uiConfig.LogoutLabel = root.Q<Label>("Logout");

            // Widget Panels
            _uiConfig.WelcomePanel = root.Q<VisualElement>("WidgetPanel0");
            _uiConfig.IoTStatusPanel = root.Q<VisualElement>("WidgetPanel1");
            _uiConfig.SystemStatusPanel = root.Q<VisualElement>("WidgetPanel2");
            _uiConfig.RecentActivityPanel = root.Q<VisualElement>("WidgetPanel3");
            _uiConfig.DeviceControlPanel = root.Q<VisualElement>("device-control-panel");

            // Navigation Menu
            _uiConfig.NavigationMenuContainer = root.Q<VisualElement>("NavigationMenuPanel");
            _uiConfig.NavigationMenu = root.Q<VisualElement>("NavigationMenu");
            _uiConfig.Scrim = root.Q<VisualElement>("Scrim");
            _uiConfig.HideMenuButton = root.Q<Button>("HideMenuButton");

            // Navigation Buttons
            InitializeNavigationButtons(root);

            // Device Control Elements
            InitializeDeviceControlElements(root);

            // IoT Status Elements
            InitializeIoTStatusElements(root);

            // Verificar elementos críticos
            bool allCriticalElementsFound = _uiConfig.Body != null && 
                                          _uiConfig.MenuButton != null && 
                                          _uiConfig.NavigationMenu != null;

            if (!allCriticalElementsFound)
            {
                Debug.LogError("[DashboardUIManager] Critical UI elements not found");
                LogMissingElements();
            }

            return allCriticalElementsFound;
        }

        /// <summary>
        /// Inicializa las referencias a botones de navegación
        /// </summary>
        private void InitializeNavigationButtons(VisualElement root)
        {
            _uiConfig.NavigationButtons[DashboardSection.Main] = root.Q<Button>("DashboardButton");
            _uiConfig.NavigationButtons[DashboardSection.Training] = root.Q<Button>("TrainingButton");
            _uiConfig.NavigationButtons[DashboardSection.Operations] = root.Q<Button>("OperationsButton");
            _uiConfig.NavigationButtons[DashboardSection.Reports] = root.Q<Button>("ReportsButton");
            _uiConfig.NavigationButtons[DashboardSection.Support] = root.Q<Button>("SupportButton");
            _uiConfig.NavigationButtons[DashboardSection.Settings] = root.Q<Button>("SettingsButton");
        }

        /// <summary>
        /// Inicializa elementos del panel de control de dispositivos
        /// </summary>
        private void InitializeDeviceControlElements(VisualElement root)
        {
            _uiConfig.DeviceNavLeftButton = root.Q<Button>("device-nav-left");
            _uiConfig.DeviceNavRightButton = root.Q<Button>("device-nav-right");
            _uiConfig.DeviceTitleLabel = root.Q<Label>("device-title");
            _uiConfig.DeviceStatusOnlineLabel = root.Q<Label>("device-status-online");
            _uiConfig.DeviceStatusBusyLabel = root.Q<Label>("device-status-busy");
            _uiConfig.DeviceStatusAlarmedLabel = root.Q<Label>("device-status-alarmed");
            _uiConfig.DeviceVisualizationBox = root.Q<VisualElement>("device-visualization-box");
            _uiConfig.DeviceModeLabel = root.Q<Label>("device-mode-label");
            _uiConfig.CoordinateXLabel = root.Q<Label>("coordinate-x");
            _uiConfig.CoordinateYLabel = root.Q<Label>("coordinate-y");
            _uiConfig.CoordinateZLabel = root.Q<Label>("coordinate-z");
        }

        /// <summary>
        /// Inicializa elementos del panel de estado IoT
        /// </summary>
        private void InitializeIoTStatusElements(VisualElement root)
        {
            _uiConfig.LocalIoTStatusLabel = root.Q<Label>("LocalIoTStatusLabel");
            _uiConfig.LocalIoTModeLabel = root.Q<Label>("LocalIoTModeLabel");
            _uiConfig.VMIoTStatusLabel = root.Q<Label>("VMIoTStatusLabel");
            _uiConfig.VMIoTModeLabel = root.Q<Label>("VMIoTModeLabel");
            _uiConfig.CloudIoTStatusLabel = root.Q<Label>("CloudIoTStatusLabel");
            _uiConfig.CloudIoTModeLabel = root.Q<Label>("CloudlIoTModeLabel");
        }

        /// <summary>
        /// Registra callbacks de transiciones
        /// </summary>
        private void RegisterTransitionCallbacks()
        {
            if (_uiConfig.NavigationMenuContainer != null)
            {
                _uiConfig.NavigationMenuContainer.RegisterCallback<TransitionEndEvent>(OnMenuTransitionEnd);
            }

            if (_uiConfig.Scrim != null)
            {
                _uiConfig.Scrim.RegisterCallback<TransitionEndEvent>(OnScrimTransitionEnd);
            }
        }

        /// <summary>
        /// Inicializa el menú de navegación en estado cerrado
        /// </summary>
        private void InitializeNavigationMenu()
        {
            // ✅ CORREGIDO: No interferir con las clases USS base, 
            // solo asegurar que no tenga la clase de mostrar
            if (_uiConfig.NavigationMenuContainer != null)
            {
                _uiConfig.NavigationMenuContainer.RemoveFromClassList("NavigationMenuPanelinMainScreen");
                // No agregar NavigationMenuPanelOutMainScreen - que se mantenga como está en USS
            }

            if (_uiConfig.Scrim != null)
            {
                _uiConfig.Scrim.RemoveFromClassList(_uiConfig.ScrimVisibleClass);
                _uiConfig.Scrim.AddToClassList(_uiConfig.ScrimHiddenClass);
            }

            _navigationState.IsMenuVisible = false;
            Debug.Log("[DashboardUIManager] Navigation menu initialized in closed state");
        }

        /// <summary>
        /// Inicializa estados por defecto de elementos UI
        /// </summary>
        private void InitializeDefaultStates()
        {
            // Ocultar dashboard inicialmente
            if (_uiConfig.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.None;
            }

            // Establecer valores por defecto
            UpdateUsername("User");
            UpdateWelcomeMessage("Welcome back, User");
        }

        /// <summary>
        /// Log del estado de elementos UI críticos para debugging
        /// </summary>
        private void LogUIElementStates()
        {
            Debug.Log("=== Dashboard UI Elements State ===");
            Debug.Log($"Body: {_uiConfig.Body != null}");
            Debug.Log($"Header: {_uiConfig.Header != null}");
            Debug.Log($"Main: {_uiConfig.Main != null}");
            Debug.Log($"Footer: {_uiConfig.Footer != null}");
            Debug.Log($"MenuButton: {_uiConfig.MenuButton != null}");
            Debug.Log($"NavigationMenu: {_uiConfig.NavigationMenu != null}");
            Debug.Log($"DeviceControlPanel: {_uiConfig.DeviceControlPanel != null}");
            
            if (_uiConfig.Body != null)
            {
                Debug.Log($"Body computed style display: {_uiConfig.Body.resolvedStyle.display}");
                Debug.Log($"Body style display: {_uiConfig.Body.style.display}");
            }
            
            Debug.Log("================================");
        }

        /// <summary>
        /// Log de elementos UI faltantes para debugging
        /// </summary>
        private void LogMissingElements()
        {
            var missingElements = new List<string>();
            
            if (_uiConfig.Body == null) missingElements.Add("Body");
            if (_uiConfig.MenuButton == null) missingElements.Add("MenuButton");
            if (_uiConfig.NavigationMenu == null) missingElements.Add("NavigationMenu");
            if (_uiConfig.UsernameLabel == null) missingElements.Add("Username");

            Debug.LogWarning($"[DashboardUIManager] Missing elements: {string.Join(", ", missingElements)}");
        }

        /// <summary>
        /// Inicializa estados por defecto de elementos UI - VERSIÓN SEGURA
        /// </summary>
        private void InitializeDefaultStatesSafe()
        {
            // No ocultar dashboard inicialmente - puede interferir con USS
            // if (_uiConfig.Body != null)
            // {
            //     _uiConfig.Body.style.display = DisplayStyle.None;
            // }

            // Establecer valores por defecto solo si los elementos existen
            if (_uiConfig.UsernameLabel != null)
            {
                UpdateUsername("User");
            }
            
            // Solo actualizar mensaje de bienvenida si el panel existe
            if (_uiConfig.WelcomePanel != null)
            {
                UpdateWelcomeMessage("Welcome back, User");
            }
            
            Debug.Log("[DashboardUIManager] Safe default states initialized");
        }

        #endregion

        #region Visibility Management

        /// <summary>
        /// Muestra el Dashboard completo
        /// </summary>
        public void ShowDashboard()
        {
            Debug.Log("[DashboardUIManager] ShowDashboard called");
            
            if (_uiConfig.Body != null)
            {
                // ✅ CAMBIO CRÍTICO: No modificar display style
                // _uiConfig.Body.style.display = DisplayStyle.Flex;
                
                // En su lugar, verificar que esté visible y loggear
                var currentDisplay = _uiConfig.Body.style.display;
                Debug.Log($"[DashboardUIManager] Body current display: {currentDisplay}");
                
                // Solo modificar si realmente está oculto
                if (currentDisplay.value == DisplayStyle.None)
                {
                    _uiConfig.Body.style.display = DisplayStyle.Flex;
                    Debug.Log("[DashboardUIManager] Changed body display to Flex");
                }
                
                _dashboardState.IsVisible = true;
                _dashboardState.TriggerShown();
                Debug.Log("[DashboardUIManager] Dashboard shown");
            }
            else
            {
                Debug.LogError("[DashboardUIManager] Body element is null - cannot show dashboard");
            }
        }

        /// <summary>
        /// Oculta el Dashboard completo
        /// </summary>
        public void HideDashboard()
        {
            if (_uiConfig.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.None;
                _dashboardState.IsVisible = false;
                _dashboardState.TriggerHidden();
                Debug.Log("[DashboardUIManager] Dashboard hidden");
            }
        }

        /// <summary>
        /// Alterna la visibilidad del menú de navegación
        /// </summary>
        public void ToggleNavigationMenu()
        {
            ToggleNavigationMenu(!_navigationState.IsMenuVisible);
        }

        /// <summary>
        /// Muestra u oculta el menú de navegación
        /// </summary>
        public void ToggleNavigationMenu(bool show)
        {
            if (_uiConfig.NavigationMenuContainer == null || _uiConfig.Scrim == null) return;

            if (show)
            {
                // ✅ CORREGIDO: Solo agregar la clase para mostrar, sin remover la base
                _uiConfig.NavigationMenuContainer.AddToClassList("NavigationMenuPanelinMainScreen");
                _uiConfig.Scrim.RemoveFromClassList(_uiConfig.ScrimHiddenClass);
                _uiConfig.Scrim.AddToClassList(_uiConfig.ScrimVisibleClass);
                Debug.Log("[DashboardUIManager] Navigation menu shown");
            }
            else
            {
                // ✅ CORREGIDO: Solo remover la clase de mostrar
                _uiConfig.NavigationMenuContainer.RemoveFromClassList("NavigationMenuPanelinMainScreen");
                _uiConfig.Scrim.RemoveFromClassList(_uiConfig.ScrimVisibleClass);
                _uiConfig.Scrim.AddToClassList(_uiConfig.ScrimHiddenClass);
                Debug.Log("[DashboardUIManager] Navigation menu hidden");
            }

            _navigationState.IsMenuVisible = show;
        }

        #endregion

        #region Device Management UI

        /// <summary>
        /// Actualiza la información del dispositivo seleccionado
        /// </summary>
        public void UpdateSelectedDevice(DashboardInfo.DeviceInfo deviceInfo)
        {
            if (deviceInfo == null) return;

            // Actualizar título del dispositivo
            if (_uiConfig.DeviceTitleLabel != null)
            {
                _uiConfig.DeviceTitleLabel.text = deviceInfo.Name;
            }

            // Actualizar estados de dispositivo
            UpdateDeviceStatusLabels(deviceInfo);

            // Actualizar coordenadas
            UpdateDeviceCoordinates(deviceInfo.Coordinates);

            // Actualizar modo de operación
            if (_uiConfig.DeviceModeLabel != null)
            {
                string mode = deviceInfo.Mode == OperationMode.Automatic ? "Automatic mode" : "Manual mode";
                _uiConfig.DeviceModeLabel.text = mode;
            }

            Debug.Log($"[DashboardUIManager] Updated device UI for: {deviceInfo.Name}");
        }

        /// <summary>
        /// Actualiza las etiquetas de estado del dispositivo
        /// </summary>
        private void UpdateDeviceStatusLabels(DashboardInfo.DeviceInfo deviceInfo)
        {
            // Resetear todos los estados
            ResetDeviceStatusLabels();

            // Configurar estado actual
            if (deviceInfo.IsAlarmed && _uiConfig.DeviceStatusAlarmedLabel != null)
            {
                _uiConfig.DeviceStatusAlarmedLabel.style.color = Color.red;
                _uiConfig.DeviceStatusAlarmedLabel.text = "Alarmed";
            }
            else if (deviceInfo.IsBusy && _uiConfig.DeviceStatusBusyLabel != null)
            {
                _uiConfig.DeviceStatusBusyLabel.style.color = Color.yellow;
                _uiConfig.DeviceStatusBusyLabel.text = "Busy";
            }
            else if (deviceInfo.Status == DeviceStatus.Online && _uiConfig.DeviceStatusOnlineLabel != null)
            {
                _uiConfig.DeviceStatusOnlineLabel.style.color = Color.green;
                _uiConfig.DeviceStatusOnlineLabel.text = "Online";
            }
        }

        /// <summary>
        /// Resetea las etiquetas de estado del dispositivo
        /// </summary>
        private void ResetDeviceStatusLabels()
        {
            if (_uiConfig.DeviceStatusOnlineLabel != null)
            {
                _uiConfig.DeviceStatusOnlineLabel.style.color = Color.gray;
            }
            if (_uiConfig.DeviceStatusBusyLabel != null)
            {
                _uiConfig.DeviceStatusBusyLabel.style.color = Color.gray;
            }
            if (_uiConfig.DeviceStatusAlarmedLabel != null)
            {
                _uiConfig.DeviceStatusAlarmedLabel.style.color = Color.gray;
            }
        }

        /// <summary>
        /// Actualiza las coordenadas del dispositivo
        /// </summary>
        private void UpdateDeviceCoordinates(Vector3 coordinates)
        {
            if (_uiConfig.CoordinateXLabel != null)
            {
                _uiConfig.CoordinateXLabel.text = $"X:{coordinates.x:000.000} mm";
            }
            if (_uiConfig.CoordinateYLabel != null)
            {
                _uiConfig.CoordinateYLabel.text = $"Y:{coordinates.y:000.000} mm";
            }
            if (_uiConfig.CoordinateZLabel != null)
            {
                _uiConfig.CoordinateZLabel.text = $"Z:{coordinates.z:000.000} mm";
            }
        }

        /// <summary>
        /// Actualiza la lista de dispositivos en el sistema de estado
        /// </summary>
        public void UpdateSystemDevicesList(Dictionary<string, DashboardInfo.DeviceInfo> devices)
        {
            // Esta implementación dependerá de cómo esté estructurado el panel de estado del sistema
            // Por ahora dejamos un placeholder para la implementación específica
            Debug.Log($"[DashboardUIManager] System devices list updated with {devices.Count} devices");
        }

        #endregion

        #region IoT Infrastructure UI

        /// <summary>
        /// Actualiza el estado de los servicios IoT
        /// </summary>
        public void UpdateIoTServicesStatus(Dictionary<IoTService, DashboardInfo.IoTServiceInfo> services)
        {
            foreach (var kvp in services)
            {
                UpdateIoTServiceStatus(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Actualiza el estado de un servicio IoT específico
        /// </summary>
        private void UpdateIoTServiceStatus(IoTService service, DashboardInfo.IoTServiceInfo serviceInfo)
        {
            Label statusLabel = null;
            Label modeLabel = null;

            // Obtener las etiquetas correspondientes al servicio
            switch (service)
            {
                case IoTService.LocalIoT:
                    statusLabel = _uiConfig.LocalIoTStatusLabel;
                    modeLabel = _uiConfig.LocalIoTModeLabel;
                    break;
                case IoTService.VMIoT:
                    statusLabel = _uiConfig.VMIoTStatusLabel;
                    modeLabel = _uiConfig.VMIoTModeLabel;
                    break;
                case IoTService.AWSIoTCore:
                    statusLabel = _uiConfig.CloudIoTStatusLabel;
                    modeLabel = _uiConfig.CloudIoTModeLabel;
                    break;
            }

            // Actualizar estado
            if (statusLabel != null)
            {
                statusLabel.text = serviceInfo.GetStatusDisplayText();
                statusLabel.style.color = serviceInfo.GetStatusColor();
            }

            // Actualizar modo
            if (modeLabel != null)
            {
                modeLabel.text = serviceInfo.Mode.ToString();
            }
        }

        #endregion

        #region System Activity UI

        /// <summary>
        /// Actualiza la lista de actividades recientes
        /// </summary>
        public void UpdateRecentActivities(List<SystemActivity> activities)
        {
            if (_uiConfig.RecentActivityPanel == null) return;

            // Esta implementación necesitará ser más específica basada en la estructura actual del panel
            // Por ahora mantenemos la funcionalidad básica
            Debug.Log($"[DashboardUIManager] Updated recent activities with {activities.Count} items");
            
            // TODO: Implementar actualización específica de la lista de actividades en el panel
        }

        #endregion

        #region User Session UI

        /// <summary>
        /// Actualiza el nombre de usuario en la UI
        /// </summary>
        public void UpdateUsername(string username)
        {
            if (_uiConfig.UsernameLabel != null)
            {
                _uiConfig.UsernameLabel.text = username;
            }
        }

        /// <summary>
        /// Actualiza el mensaje de bienvenida
        /// </summary>
        public void UpdateWelcomeMessage(string message)
        {
            // Buscar el label de bienvenida en el panel de bienvenida
            var welcomeLabel = _uiConfig.WelcomePanel?.Q<Label>();
            if (welcomeLabel != null)
            {
                welcomeLabel.text = message;
            }
        }

        #endregion

        #region Navigation UI

        /// <summary>
        /// Actualiza la sección activa en el menú de navegación
        /// </summary>
        public void UpdateActiveNavigationSection(DashboardSection section)
        {
            // Remover clase activa de todos los botones
            foreach (var kvp in _uiConfig.NavigationButtons)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.RemoveFromClassList("nav-button-active");
                    kvp.Value.AddToClassList("nav-button");
                }
            }

            // Agregar clase activa al botón actual
            if (_uiConfig.NavigationButtons.TryGetValue(section, out var activeButton) && activeButton != null)
            {
                activeButton.AddToClassList("nav-button-active");
                activeButton.RemoveFromClassList("nav-button");
            }

            _navigationState.NavigateTo(section);
            Debug.Log($"[DashboardUIManager] Active navigation section updated to: {section}");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Maneja el fin de la transición del menú
        /// </summary>
        private void OnMenuTransitionEnd(TransitionEndEvent evt)
        {
            Debug.Log("[DashboardUIManager] Menu transition completed");
        }

        /// <summary>
        /// Maneja el fin de la transición del scrim
        /// </summary>
        private void OnScrimTransitionEnd(TransitionEndEvent evt)
        {
            Debug.Log("[DashboardUIManager] Scrim transition completed");
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Configuración UI actual
        /// </summary>
        public DashboardInfo.UIConfiguration UIConfig => _uiConfig;

        /// <summary>
        /// Estado de navegación actual
        /// </summary>
        public DashboardInfo.NavigationState NavigationState => _navigationState;

        /// <summary>
        /// Estado del dashboard
        /// </summary>
        public DashboardInfo.DashboardState DashboardState => _dashboardState;

        /// <summary>
        /// Indica si el dashboard está visible
        /// </summary>
        public bool IsVisible => _dashboardState.IsVisible;

        /// <summary>
        /// Indica si el menú de navegación está visible
        /// </summary>
        public bool IsNavigationMenuVisible => _navigationState.IsMenuVisible;

        #endregion

        #region Cleanup

        /// <summary>
        /// Limpia recursos del UI Manager
        /// </summary>
        public void Cleanup()
        {
            // Desregistrar callbacks
            if (_uiConfig.NavigationMenuContainer != null)
            {
                _uiConfig.NavigationMenuContainer.UnregisterCallback<TransitionEndEvent>(OnMenuTransitionEnd);
            }

            if (_uiConfig.Scrim != null)
            {
                _uiConfig.Scrim.UnregisterCallback<TransitionEndEvent>(OnScrimTransitionEnd);
            }

            // Limpiar referencias
            _uiConfig = null;
            _navigationState = null;
            _dashboardState = null;
            _uiDocument = null;

            Debug.Log("[DashboardUIManager] UI Manager cleaned up");
        }

        #endregion
    }
}