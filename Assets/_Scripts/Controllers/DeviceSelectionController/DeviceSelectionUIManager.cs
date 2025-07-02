using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.DeviceSelectionController
{
    /// <summary>
    /// Gestor de la interfaz de usuario para Device Selection
    /// Responsable de actualizar elementos UI, mostrar/ocultar menús, y manejar estados visuales
    /// Equivalente al DashboardUIManager y WelcomeUIManager pero para Device Selection
    /// </summary>
    public class DeviceSelectionUIManager
    {
        #region Private Fields

        private DeviceSelectionInfo.UIConfiguration _uiConfig;
        private DeviceSelectionInfo.NavigationState _navigationState;
        private DeviceSelectionInfo.DeviceSelectionState _deviceSelectionState;
        private DeviceSelectionInfo.ContextData _contextData;
        private UIDocument _uiDocument;

        // Constantes para transiciones y animaciones
        private const float ANIMATION_DURATION = 0.3f;
        private const string MENU_TRANSITION_DURATION = "1s";

        #endregion

        #region Constructor

        public DeviceSelectionUIManager(DeviceSelectionInfo.UIConfiguration uiConfig, UIDocument uiDocument)
        {
            _uiConfig = uiConfig ?? throw new ArgumentNullException(nameof(uiConfig));
            _uiDocument = uiDocument ?? throw new ArgumentNullException(nameof(uiDocument));
            _navigationState = new DeviceSelectionInfo.NavigationState();
            _deviceSelectionState = new DeviceSelectionInfo.DeviceSelectionState();
            _contextData = new DeviceSelectionInfo.ContextData();
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
                Debug.Log("[DeviceSelectionUIManager] Initializing UI Manager...");
                
                // Verificar que el DOM esté listo
                if (_uiDocument?.rootVisualElement == null)
                {
                    Debug.LogError("[DeviceSelectionUIManager] UIDocument or root element is null");
                    return false;
                }

                var root = _uiDocument.rootVisualElement;
                Debug.Log($"[DeviceSelectionUIManager] Root element found: {root != null}");

                if (!GetUIReferences())
                {
                    Debug.LogError("[DeviceSelectionUIManager] Failed to get UI references");
                    return false;
                }

                // Inicializar menú de navegación en estado cerrado
                InitializeNavigationMenu();
                
                // Inicializar estados por defecto
                InitializeDefaultStates();
                
                // Registrar callbacks de transiciones
                RegisterTransitionCallbacks();

                _deviceSelectionState.IsInitialized = true;
                _deviceSelectionState.TriggerInitialized();

                Debug.Log("[DeviceSelectionUIManager] UI Manager initialized successfully");
                LogUIElementStates();
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeviceSelectionUIManager] Initialization error: {ex.Message}");
                _deviceSelectionState.TriggerError($"Initialization failed: {ex.Message}");
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
            _uiConfig.SelectedModeUINameLabel = root.Q<Label>("SelectedModeUiName");
            _uiConfig.UsernameLabel = root.Q<Label>("Username");
            _uiConfig.LogoutLabel = root.Q<Label>("Logout");

            // Main Content Elements
            _uiConfig.MainScrollView = root.Q<ScrollView>();
            InitializeDeviceRows(root);

            // Device Button Elements
            InitializeDeviceButtons(root);

            // Navigation Menu Elements
            _uiConfig.SubpanelsContainer = root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _uiConfig.NavigationMenuPanel = root.Q<VisualElement>("NavigationMenuPanel");
            _uiConfig.NavigationMenu = root.Q<VisualElement>("NavigationMenu");
            _uiConfig.Scrim = root.Q<VisualElement>("Scrim");
            _uiConfig.HideMenuButton = root.Q<Button>("HideMenuButton");

            // Navigation Buttons
            InitializeNavigationButtons(root);

            // Verificar elementos críticos
            bool allCriticalElementsFound = _uiConfig.Body != null && 
                                          _uiConfig.MenuButton != null && 
                                          _uiConfig.NavigationMenu != null &&
                                          _uiConfig.DeviceButtons.Count > 0;

            if (!allCriticalElementsFound)
            {
                Debug.LogError("[DeviceSelectionUIManager] Critical UI elements not found");
                LogMissingElements();
            }

            return allCriticalElementsFound;
        }

        /// <summary>
        /// Inicializa las filas de dispositivos
        /// </summary>
        private void InitializeDeviceRows(VisualElement root)
        {
            _uiConfig.DeviceRows.Clear();
            
            // Buscar todas las filas de dispositivos
            var deviceRows = root.Query<VisualElement>(className: "device-row").ToList();
            
            foreach (var row in deviceRows)
            {
                _uiConfig.DeviceRows.Add(row);
            }

            Debug.Log($"[DeviceSelectionUIManager] Found {_uiConfig.DeviceRows.Count} device rows");
        }

        /// <summary>
        /// Inicializa las referencias a botones de dispositivos
        /// </summary>
        private void InitializeDeviceButtons(VisualElement root)
        {
            _uiConfig.DeviceButtons.Clear();
            _uiConfig.DeviceVisualElements.Clear();
            _uiConfig.DeviceLabels.Clear();

            // Dispositivos principales con nombres específicos
            var mainDevices = new Dictionary<string, string>
            {
                ["ARSCARAButton"] = "ARSCARA",
                ["RobotKit1Button"] = "RobotKit1",
                ["RobotKit2Button"] = "RobotKit2"
            };

            foreach (var kvp in mainDevices)
            {
                var button = root.Q<Button>(kvp.Key);
                if (button != null)
                {
                    _uiConfig.DeviceButtons[kvp.Value] = button;
                    
                    // Buscar elementos visuales dentro del botón
                    var visualElement = button.Q<VisualElement>(className: "device-visual-element");
                    if (visualElement != null)
                    {
                        _uiConfig.DeviceVisualElements[kvp.Value] = visualElement;
                    }
                    
                    // Buscar label dentro del botón
                    var label = button.Q<Label>(className: "device-label");
                    if (label != null)
                    {
                        _uiConfig.DeviceLabels[kvp.Value] = label;
                    }
                }
            }

            // Dispositivos de repuesto (Spare 1-6)
            var allButtons = root.Query<Button>(className: "device-button").ToList();
            int spareIndex = 1;
            
            foreach (var button in allButtons)
            {
                // Si no es uno de los dispositivos principales, es un spare
                if (!mainDevices.ContainsKey(button.name))
                {
                    var spareId = $"Spare{spareIndex}";
                    _uiConfig.DeviceButtons[spareId] = button;
                    
                    var visualElement = button.Q<VisualElement>(className: "device-visual-element");
                    if (visualElement != null)
                    {
                        _uiConfig.DeviceVisualElements[spareId] = visualElement;
                    }
                    
                    var label = button.Q<Label>(className: "device-label");
                    if (label != null)
                    {
                        _uiConfig.DeviceLabels[spareId] = label;
                    }
                    
                    spareIndex++;
                }
            }

            Debug.Log($"[DeviceSelectionUIManager] Initialized {_uiConfig.DeviceButtons.Count} device buttons");
        }

        /// <summary>
        /// Inicializa las referencias a botones de navegación
        /// </summary>
        private void InitializeNavigationButtons(VisualElement root)
        {
            _uiConfig.NavigationButtons[NavigationContext.Dashboard] = root.Q<Button>("DashboardButton");
            _uiConfig.NavigationButtons[NavigationContext.Training] = root.Q<Button>("TrainingButton");
            _uiConfig.NavigationButtons[NavigationContext.Operations] = root.Q<Button>("OperationsButton");
            _uiConfig.NavigationButtons[NavigationContext.Reports] = root.Q<Button>("ReportsButton");
            _uiConfig.NavigationButtons[NavigationContext.Settings] = root.Q<Button>("SettingsButton");

            // Button especial de logout
            var logoutButton = root.Q<Button>("LogoutButton");
            if (logoutButton != null)
            {
                _uiConfig.NavigationButtons[NavigationContext.None] = logoutButton; // Usar None para logout
            }
        }

        /// <summary>
        /// Inicializa el menú de navegación en estado cerrado
        /// </summary>
        private void InitializeNavigationMenu()
        {
            if (_uiConfig.NavigationMenuPanel != null)
            {
                _uiConfig.NavigationMenuPanel.RemoveFromClassList(_uiConfig.MenuVisibleClass);
            }

            if (_uiConfig.Scrim != null)
            {
                _uiConfig.Scrim.RemoveFromClassList(_uiConfig.ScrimVisibleClass);
            }

            if (_uiConfig.SubpanelsContainer != null)
            {
                _uiConfig.SubpanelsContainer.style.display = DisplayStyle.None;
            }

            _navigationState.IsMenuVisible = false;
            Debug.Log("[DeviceSelectionUIManager] Navigation menu initialized in closed state");
        }

        /// <summary>
        /// Inicializa estados por defecto de elementos UI
        /// </summary>
        private void InitializeDefaultStates()
        {
            // Establecer valores por defecto
            UpdateUsername("User");
            UpdateUITitle("Select Device");
            
            // Configurar estado inicial de dispositivos
            foreach (var kvp in _uiConfig.DeviceButtons)
            {
                SetDeviceButtonState(kvp.Key, DeviceButtonState.Available);
            }

            Debug.Log("[DeviceSelectionUIManager] Default states initialized");
        }

        /// <summary>
        /// Registra callbacks de transiciones
        /// </summary>
        private void RegisterTransitionCallbacks()
        {
            if (_uiConfig.NavigationMenuPanel != null)
            {
                _uiConfig.NavigationMenuPanel.RegisterCallback<TransitionEndEvent>(OnMenuTransitionEnd);
            }

            if (_uiConfig.Scrim != null)
            {
                _uiConfig.Scrim.RegisterCallback<TransitionEndEvent>(OnScrimTransitionEnd);
            }

            Debug.Log("[DeviceSelectionUIManager] Transition callbacks registered");
        }

        #endregion

        #region Visibility Management

        /// <summary>
        /// Muestra el Device Selection completo
        /// </summary>
        public void ShowDeviceSelection()
        {
            Debug.Log("[DeviceSelectionUIManager] ShowDeviceSelection called");
            
            if (_uiConfig.Body != null)
            {
                var currentDisplay = _uiConfig.Body.style.display;
                Debug.Log($"[DeviceSelectionUIManager] Body current display: {currentDisplay}");
                
                if (currentDisplay.value == DisplayStyle.None)
                {
                    _uiConfig.Body.style.display = DisplayStyle.Flex;
                    Debug.Log("[DeviceSelectionUIManager] Changed body display to Flex");
                }
                
                _deviceSelectionState.IsVisible = true;
                _deviceSelectionState.TriggerShown();
                Debug.Log("[DeviceSelectionUIManager] Device Selection shown");
            }
            else
            {
                Debug.LogError("[DeviceSelectionUIManager] Body element is null - cannot show device selection");
            }
        }

        /// <summary>
        /// Oculta el Device Selection completo
        /// </summary>
        public void HideDeviceSelection()
        {
            if (_uiConfig.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.None;
                _deviceSelectionState.IsVisible = false;
                _deviceSelectionState.TriggerHidden();
                Debug.Log("[DeviceSelectionUIManager] Device Selection hidden");
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
            if (_uiConfig.NavigationMenuPanel == null || _uiConfig.Scrim == null) return;

            if (show)
            {
                // Mostrar contenedor primero
                if (_uiConfig.SubpanelsContainer != null)
                {
                    _uiConfig.SubpanelsContainer.style.display = DisplayStyle.Flex;
                }

                // Agregar clases para mostrar
                _uiConfig.NavigationMenuPanel.AddToClassList(_uiConfig.MenuVisibleClass);
                _uiConfig.Scrim.AddToClassList(_uiConfig.ScrimVisibleClass);
                
                Debug.Log("[DeviceSelectionUIManager] Navigation menu shown");
            }
            else
            {
                // Remover clases para ocultar
                _uiConfig.NavigationMenuPanel.RemoveFromClassList(_uiConfig.MenuVisibleClass);
                _uiConfig.Scrim.RemoveFromClassList(_uiConfig.ScrimVisibleClass);
                
                Debug.Log("[DeviceSelectionUIManager] Navigation menu hidden");
            }

            _navigationState.IsMenuVisible = show;
        }

        #endregion

        #region Device Management UI

        /// <summary>
        /// Actualiza la visualización de dispositivos disponibles
        /// </summary>
        /// <param name="devices">Lista de dispositivos a mostrar</param>
        /// <param name="currentMode">Modo actual para determinar disponibilidad</param>
        public void UpdateDeviceDisplay(List<DeviceSelectionInfo.DeviceInfo> devices, DeviceMode currentMode)
        {
            if (devices == null) return;

            foreach (var device in devices)
            {
                UpdateDeviceButton(device, currentMode);
            }

            Debug.Log($"[DeviceSelectionUIManager] Updated device display for {devices.Count} devices in {currentMode} mode");
        }

        /// <summary>
        /// Actualiza un botón de dispositivo específico
        /// </summary>
        /// <param name="device">Información del dispositivo</param>
        /// <param name="currentMode">Modo actual</param>
        private void UpdateDeviceButton(DeviceSelectionInfo.DeviceInfo device, DeviceMode currentMode)
        {
            if (!_uiConfig.DeviceButtons.TryGetValue(device.Id, out var button)) return;

            // Actualizar label si existe
            if (_uiConfig.DeviceLabels.TryGetValue(device.Id, out var label))
            {
                label.text = device.DisplayName;
            }

            // Determinar estado del botón
            DeviceButtonState buttonState;
            if (!device.IsAvailable)
            {
                buttonState = DeviceButtonState.Unavailable;
            }
            else if (device.SupportsMode(currentMode))
            {
                buttonState = DeviceButtonState.Available;
            }
            else
            {
                buttonState = DeviceButtonState.Unsupported;
            }

            SetDeviceButtonState(device.Id, buttonState);

            // Actualizar color del elemento visual
            if (_uiConfig.DeviceVisualElements.TryGetValue(device.Id, out var visualElement))
            {
                visualElement.style.backgroundColor = device.GetDisplayColor();
            }
        }

        /// <summary>
        /// Establece el estado visual de un botón de dispositivo
        /// </summary>
        /// <param name="deviceId">ID del dispositivo</param>
        /// <param name="state">Estado del botón</param>
        public void SetDeviceButtonState(string deviceId, DeviceButtonState state)
        {
            if (!_uiConfig.DeviceButtons.TryGetValue(deviceId, out var button)) return;

            // Remover todas las clases de estado
            button.RemoveFromClassList(_uiConfig.DeviceAvailableClass);
            button.RemoveFromClassList(_uiConfig.DeviceUnavailableClass);
            button.RemoveFromClassList(_uiConfig.DeviceSelectedClass);

            // Agregar clase apropiada según el estado
            switch (state)
            {
                case DeviceButtonState.Available:
                    button.AddToClassList(_uiConfig.DeviceAvailableClass);
                    button.SetEnabled(true);
                    break;
                case DeviceButtonState.Unavailable:
                case DeviceButtonState.Unsupported:
                    button.AddToClassList(_uiConfig.DeviceUnavailableClass);
                    button.SetEnabled(false);
                    break;
                case DeviceButtonState.Selected:
                    button.AddToClassList(_uiConfig.DeviceSelectedClass);
                    button.SetEnabled(true);
                    break;
            }
        }

        /// <summary>
        /// Marca un dispositivo como seleccionado
        /// </summary>
        /// <param name="deviceId">ID del dispositivo seleccionado</param>
        public void SelectDevice(string deviceId)
        {
            // Deseleccionar todos los dispositivos
            foreach (var kvp in _uiConfig.DeviceButtons)
            {
                if (kvp.Key != deviceId)
                {
                    kvp.Value.RemoveFromClassList(_uiConfig.DeviceSelectedClass);
                }
            }

            // Seleccionar el dispositivo específico
            SetDeviceButtonState(deviceId, DeviceButtonState.Selected);
            
            Debug.Log($"[DeviceSelectionUIManager] Device selected: {deviceId}");
        }

        /// <summary>
        /// Limpia la selección de dispositivos
        /// </summary>
        public void ClearDeviceSelection()
        {
            foreach (var kvp in _uiConfig.DeviceButtons)
            {
                kvp.Value.RemoveFromClassList(_uiConfig.DeviceSelectedClass);
            }
            
            Debug.Log("[DeviceSelectionUIManager] Device selection cleared");
        }

        #endregion

        #region Context and UI Updates

        /// <summary>
        /// Actualiza la UI basada en el contexto actual
        /// </summary>
        /// <param name="context">Contexto de navegación</param>
        /// <param name="deviceMode">Modo de dispositivo</param>
        /// <param name="uiTitle">Título de la UI</param>
        public void UpdateContextUI(NavigationContext context, DeviceMode deviceMode, string uiTitle)
        {
            _contextData.CurrentContext = context;
            _contextData.RequiredMode = deviceMode;
            _contextData.UITitle = uiTitle;

            // Actualizar título en la UI
            UpdateUITitle(uiTitle);

            // Actualizar navegación activa
            UpdateActiveNavigationSection(context);

            Debug.Log($"[DeviceSelectionUIManager] Context UI updated: {context} - {deviceMode} - {uiTitle}");
        }

        /// <summary>
        /// Actualiza el título de la UI
        /// </summary>
        /// <param name="title">Nuevo título</param>
        public void UpdateUITitle(string title)
        {
            if (_uiConfig.SelectedModeUINameLabel != null)
            {
                _uiConfig.SelectedModeUINameLabel.text = title;
            }
        }

        /// <summary>
        /// Actualiza el nombre de usuario en la UI
        /// </summary>
        /// <param name="username">Nombre de usuario</param>
        public void UpdateUsername(string username)
        {
            if (_uiConfig.UsernameLabel != null)
            {
                _uiConfig.UsernameLabel.text = username;
            }
        }

        /// <summary>
        /// Actualiza la sección activa en el menú de navegación
        /// </summary>
        /// <param name="activeContext">Contexto activo</param>
        public void UpdateActiveNavigationSection(NavigationContext activeContext)
        {
            // Remover clase activa de todos los botones
            foreach (var kvp in _uiConfig.NavigationButtons)
            {
                if (kvp.Value != null && kvp.Key != NavigationContext.None) // Skip logout button
                {
                    kvp.Value.RemoveFromClassList("navigation-menu-button-active");
                }
            }

            // Agregar clase activa al botón actual
            if (_uiConfig.NavigationButtons.TryGetValue(activeContext, out var activeButton) && activeButton != null)
            {
                activeButton.AddToClassList("navigation-menu-button-active");
            }

            _navigationState.ActiveSection = activeContext;
            Debug.Log($"[DeviceSelectionUIManager] Active navigation section updated to: {activeContext}");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Maneja el fin de la transición del menú
        /// </summary>
        private void OnMenuTransitionEnd(TransitionEndEvent evt)
        {
            // Si el menú no está visible, ocultar el contenedor
            if (!_navigationState.IsMenuVisible && _uiConfig.SubpanelsContainer != null)
            {
                _uiConfig.SubpanelsContainer.style.display = DisplayStyle.None;
            }
            
            Debug.Log("[DeviceSelectionUIManager] Menu transition completed");
        }

        /// <summary>
        /// Maneja el fin de la transición del scrim
        /// </summary>
        private void OnScrimTransitionEnd(TransitionEndEvent evt)
        {
            Debug.Log("[DeviceSelectionUIManager] Scrim transition completed");
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Configuración UI actual
        /// </summary>
        public DeviceSelectionInfo.UIConfiguration UIConfig => _uiConfig;

        /// <summary>
        /// Estado de navegación actual
        /// </summary>
        public DeviceSelectionInfo.NavigationState NavigationState => _navigationState;

        /// <summary>
        /// Estado del device selection
        /// </summary>
        public DeviceSelectionInfo.DeviceSelectionState DeviceSelectionState => _deviceSelectionState;

        /// <summary>
        /// Datos del contexto actual
        /// </summary>
        public DeviceSelectionInfo.ContextData ContextData => _contextData;

        /// <summary>
        /// Indica si está visible
        /// </summary>
        public bool IsVisible => _deviceSelectionState.IsVisible;

        /// <summary>
        /// Indica si el menú de navegación está visible
        /// </summary>
        public bool IsNavigationMenuVisible => _navigationState.IsMenuVisible;

        #endregion

        #region Utility Methods

        /// <summary>
        /// Log del estado de elementos UI críticos para debugging
        /// </summary>
        private void LogUIElementStates()
        {
            Debug.Log("=== Device Selection UI Elements State ===");
            Debug.Log($"Body: {_uiConfig.Body != null}");
            Debug.Log($"Header: {_uiConfig.Header != null}");
            Debug.Log($"Main: {_uiConfig.Main != null}");
            Debug.Log($"MenuButton: {_uiConfig.MenuButton != null}");
            Debug.Log($"NavigationMenu: {_uiConfig.NavigationMenu != null}");
            Debug.Log($"Device Buttons: {_uiConfig.DeviceButtons.Count}");
            Debug.Log($"Device Rows: {_uiConfig.DeviceRows.Count}");
            Debug.Log("==========================================");
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
            if (_uiConfig.DeviceButtons.Count == 0) missingElements.Add("DeviceButtons");

            Debug.LogWarning($"[DeviceSelectionUIManager] Missing elements: {string.Join(", ", missingElements)}");
        }

        /// <summary>
        /// Fuerza una actualización completa de la UI
        /// </summary>
        public void ForceUIRefresh()
        {
            Debug.Log("[DeviceSelectionUIManager] Forcing UI refresh...");
            
            // Re-aplicar estados de contexto
            if (_contextData.IsValidContext())
            {
                UpdateContextUI(_contextData.CurrentContext, _contextData.RequiredMode, _contextData.UITitle);
            }
            
            // Re-aplicar estado del menú
            ToggleNavigationMenu(_navigationState.IsMenuVisible);
            
            Debug.Log("[DeviceSelectionUIManager] UI refresh completed");
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Limpia recursos del UI Manager
        /// </summary>
        public void Cleanup()
        {
            try
            {
                Debug.Log("[DeviceSelectionUIManager] Cleaning up UI Manager...");

                // Desregistrar callbacks
                if (_uiConfig.NavigationMenuPanel != null)
                {
                    _uiConfig.NavigationMenuPanel.UnregisterCallback<TransitionEndEvent>(OnMenuTransitionEnd);
                }

                if (_uiConfig.Scrim != null)
                {
                    _uiConfig.Scrim.UnregisterCallback<TransitionEndEvent>(OnScrimTransitionEnd);
                }

                // Limpiar referencias
                _uiConfig = null;
                _navigationState = null;
                _deviceSelectionState = null;
                _contextData = null;
                _uiDocument = null;

                Debug.Log("[DeviceSelectionUIManager] UI Manager cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeviceSelectionUIManager] Cleanup error: {ex.Message}");
            }
        }

        #endregion
    }

    #region Supporting Enums

    /// <summary>
    /// Estados visuales de los botones de dispositivos
    /// </summary>
    public enum DeviceButtonState
    {
        Available,      // Dispositivo disponible y soporta el modo actual
        Unavailable,    // Dispositivo no disponible (offline, en mantenimiento, etc.)
        Unsupported,    // Dispositivo disponible pero no soporta el modo actual
        Selected        // Dispositivo actualmente seleccionado
    }

    #endregion
}