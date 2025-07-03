using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.DeviceSelectionController
{
    /// <summary>
    /// Maneja la lógica de UI del Device Selection, incluyendo paneles modales y menú lateral
    /// Equivalente a WelcomeUIManager y DashboardUIManager pero especializado para Device Selection
    /// </summary>
    public class DeviceSelectionUIManager
    {
        private DeviceSelectionInfo.UIConfiguration _uiConfig;
        private IDeviceSelectionOps.PanelType _currentActivePanel = IDeviceSelectionOps.PanelType.None;
        private bool _isNavigationMenuOpen = false;
        
        // Clases USS para scrim
        private const string SCRIM_SHOW_CLASS = "ScrimOpaque";
        
        // Configuración de dispositivos
        private DeviceSelectionInfo.DeviceConfiguration _deviceConfig;
        private DeviceSelectionInfo.ContextData _contextData;

        #region Constructor

        public DeviceSelectionUIManager(DeviceSelectionInfo.UIConfiguration uiConfig)
        {
            _uiConfig = uiConfig ?? throw new ArgumentNullException(nameof(uiConfig));
            _deviceConfig = DeviceSelectionInfo.DeviceConfiguration.CreateDefault();
            _contextData = new DeviceSelectionInfo.ContextData();
        }

        #endregion

        #region UI State Management

        /// <summary>
        /// Inicializa el sistema de paneles del Device Selection
        /// </summary>
        public void InitializePanelSystem()
        {
            // Asegurar estado inicial correcto
            _uiConfig.SubpanelsContainer.style.display = DisplayStyle.None;
            HideUi(); // Device Selection inicia oculto hasta que se active
            ClearAllPanelStates();
            RegisterTransitionCallbacks();
            
            // Inicializar dispositivos UI
            InitializeDeviceButtons();
            
            // Configurar contexto inicial
            UpdateContextualUI();
            
            Debug.Log("[DeviceSelectionUIManager] Panel system initialized");
        }

        /// <summary>
        /// Muestra la UI principal del Device Selection
        /// </summary>
        public void ShowUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.Flex;
            
            // Actualizar UI contextual al mostrar
            UpdateContextualUI();
            RefreshDeviceStates();
            
            Debug.Log("[DeviceSelectionUIManager] Device Selection UI shown");
        }

        /// <summary>
        /// Oculta la UI principal del Device Selection
        /// </summary>
        public void HideUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.None;
            
            // Asegurar que todos los paneles estén cerrados
            CloseAllPanels();
            Debug.Log("[DeviceSelectionUIManager] Device Selection UI hidden");
        }

        #endregion

        #region Context Management

        /// <summary>
        /// Configura el contexto de lanzamiento
        /// </summary>
        public void SetLaunchContext(IDeviceSelectionOps.LaunchContext context, string sourceController = null)
        {
            var previousContext = _contextData.CurrentContext;
            _contextData.PreviousContext = previousContext;
            _contextData.CurrentContext = context;
            _contextData.SourceController = sourceController ?? "Unknown";
            _contextData.ContextSetTime = DateTime.Now;
            
            // Actualizar UI con nuevo contexto
            UpdateContextualUI();
            RefreshDeviceAvailability();
            
            Debug.Log($"[DeviceSelectionUIManager] Context changed: {previousContext} → {context} (from {sourceController})");
        }

        /// <summary>
        /// Actualiza la UI basada en el contexto actual
        /// </summary>
        private void UpdateContextualUI()
        {
            if (_uiConfig.ContextTitleLabel != null)
            {
                _uiConfig.ContextTitleLabel.text = _contextData.GetContextualTitle();
                _uiConfig.ContextTitleLabel.style.color = _contextData.GetContextualColor();
            }
    
            if (_uiConfig.SelectedModeLabel != null)
            {
                var contextDescription = _contextData.GetContextualDescription();
                _uiConfig.SelectedModeLabel.text = contextDescription;
            }
            
            UpdateNavigationMenuActiveStates();
    
            Debug.Log($"[DeviceSelectionUIManager] UI updated for context: {_contextData.CurrentContext}");
        }

        /// <summary>
        /// Actualiza la disponibilidad de dispositivos según el contexto
        /// </summary>
        private void RefreshDeviceAvailability()
        {
            var compatibleDevices = _deviceConfig.GetDevicesForContext(_contextData.CurrentContext);
            
            foreach (var kvp in _uiConfig.DeviceButtons)
            {
                var deviceId = kvp.Key;
                var button = kvp.Value;
                
                if (button == null) continue;
                
                var deviceInfo = _deviceConfig.DeviceRegistry.GetValueOrDefault(deviceId);
                var isCompatible = deviceInfo?.IsCompatibleWith(_contextData.CurrentContext) ?? false;
                var isAvailable = deviceInfo?.IsAvailable ?? false;
                
                // Actualizar estado del botón
                button.SetEnabled(isCompatible && isAvailable);
                
                // Actualizar clases CSS para styling
                UpdateDeviceButtonStyling(button, deviceInfo, isCompatible, isAvailable);
            }
            
            Debug.Log($"[DeviceSelectionUIManager] Device availability updated for context: {_contextData.CurrentContext}");
        }

        /// <summary>
        /// Actualiza el styling de un botón de dispositivo
        /// </summary>
        private void UpdateDeviceButtonStyling(Button button, DeviceSelectionInfo.DeviceInfo deviceInfo, bool isCompatible, bool isAvailable)
        {
            // Limpiar clases existentes
            button.RemoveFromClassList("device-available");
            button.RemoveFromClassList("device-unavailable");
            button.RemoveFromClassList("device-incompatible");
            button.RemoveFromClassList("device-online");
            button.RemoveFromClassList("device-offline");
            button.RemoveFromClassList("device-busy");
            button.RemoveFromClassList("device-error");
            
            // Aplicar clases apropiadas
            if (!isCompatible)
            {
                button.AddToClassList("device-incompatible");
            }
            else if (!isAvailable)
            {
                button.AddToClassList("device-unavailable");
            }
            else
            {
                button.AddToClassList("device-available");
                
                // Agregar clase de estado específica
                if (deviceInfo != null)
                {
                    switch (deviceInfo.Status)
                    {
                        case DeviceSelectionInfo.DeviceStatus.Online:
                            button.AddToClassList("device-online");
                            break;
                        case DeviceSelectionInfo.DeviceStatus.Offline:
                            button.AddToClassList("device-offline");
                            break;
                        case DeviceSelectionInfo.DeviceStatus.Busy:
                            button.AddToClassList("device-busy");
                            break;
                        case DeviceSelectionInfo.DeviceStatus.Error:
                            button.AddToClassList("device-error");
                            break;
                    }
                }
            }
        }

        #endregion

        #region Navigation Menu Management
        
        /// <summary>
        /// Actualiza el estado activo de los botones del menú según el contexto
        /// </summary>
        private void UpdateNavigationMenuActiveStates()
        {
            if (_uiConfig.SubpanelsContainer == null) return;

            // Obtener botones del menú de navegación
            var trainingButton = _uiConfig.SubpanelsContainer.Q<Button>("TrainingButton");
            var operationsButton = _uiConfig.SubpanelsContainer.Q<Button>("OperationsButton");
            var dashboardButton = _uiConfig.SubpanelsContainer.Q<Button>("DashboardButton");

            // Limpiar estados activos existentes
            trainingButton?.RemoveFromClassList("navigation-menu-button-active");  // ✅ CAMBIO
            operationsButton?.RemoveFromClassList("navigation-menu-button-active"); // ✅ CAMBIO
            dashboardButton?.RemoveFromClassList("navigation-menu-button-active");  // ✅ CAMBIO

            // Aplicar clase activa según el contexto actual
            switch (_contextData.CurrentContext)
            {
                case IDeviceSelectionOps.LaunchContext.Training:
                    trainingButton?.AddToClassList("navigation-menu-button-active");  // ✅ CAMBIO
                    Debug.Log("[DeviceSelectionUIManager] Training button marked as active");
                    break;
        
                case IDeviceSelectionOps.LaunchContext.Operations:
                    operationsButton?.AddToClassList("navigation-menu-button-active"); // ✅ CAMBIO
                    Debug.Log("[DeviceSelectionUIManager] Operations button marked as active");
                    break;
        
                default:
                    // Por defecto, ningún botón activo
                    break;
            }
        }

        /// <summary>
        /// Muestra el menú de navegación lateral
        /// </summary>
        public void ShowNavigationMenu()
        {
            if (_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(IDeviceSelectionOps.PanelType.NavigationMenu, out var menuPanel))
            {
                Debug.LogError("NavigationMenu panel not found in configuration");
                return;
            }

            // Mostrar contenedor y aplicar animación
            _uiConfig.SubpanelsContainer.style.display = DisplayStyle.Flex;

            // Agregar clase de visible
            menuPanel.Panel.AddToClassList(menuPanel.ShowClass);

            // Mostrar scrim si es requerido
            if (menuPanel.RequiresScrim)
            {
                _uiConfig.Scrim.AddToClassList(SCRIM_SHOW_CLASS);
            }

            _currentActivePanel = IDeviceSelectionOps.PanelType.NavigationMenu;
            _isNavigationMenuOpen = true;
            UpdateNavigationMenuActiveStates();

            Debug.Log("[DeviceSelectionUIManager] Navigation menu opened");
        }

        /// <summary>
        /// Oculta el menú de navegación lateral
        /// </summary>
        public void HideNavigationMenu()
        {
            if (!_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(IDeviceSelectionOps.PanelType.NavigationMenu, out var menuPanel))
            {
                Debug.LogError("NavigationMenu panel not found in configuration");
                return;
            }

            menuPanel.Panel.RemoveFromClassList(menuPanel.ShowClass);
    
            // Ocultar scrim
            _uiConfig.Scrim.RemoveFromClassList(SCRIM_SHOW_CLASS);

            _currentActivePanel = IDeviceSelectionOps.PanelType.None;
            _isNavigationMenuOpen = false;
    
            Debug.Log("[DeviceSelectionUIManager] Navigation menu closed");
        }

        /// <summary>
        /// Alterna el estado del menú de navegación
        /// </summary>
        public void ToggleNavigationMenu()
        {
            if (_isNavigationMenuOpen)
            {
                HideNavigationMenu();
            }
            else
            {
                ShowNavigationMenu();
            }
        }

        #endregion

        #region Panel Management (Modal Panels)

        /// <summary>
        /// Muestra un panel modal (no el menú lateral)
        /// </summary>
        public void ShowPanel(IDeviceSelectionOps.PanelType panelType)
        {
            if (panelType == IDeviceSelectionOps.PanelType.NavigationMenu)
            {
                ShowNavigationMenu();
                return;
            }

            if (!_uiConfig.Panels.TryGetValue(panelType, out var panelData))
            {
                Debug.LogError($"Panel type {panelType} not found in configuration");
                return;
            }

            // Verificar que el panel existe
            if (panelData.Panel == null)
            {
                Debug.LogWarning($"Panel {panelType} is not implemented in current UI - skipping");
                return;
            }

            // Si hay otro panel modal abierto, cerrarlo primero
            if (_currentActivePanel != IDeviceSelectionOps.PanelType.None && 
                _currentActivePanel != IDeviceSelectionOps.PanelType.NavigationMenu)
            {
                HidePanel(_currentActivePanel);
            }

            // Mostrar contenedor si no está visible
            if (_uiConfig.SubpanelsContainer.style.display != DisplayStyle.Flex)
            {
                _uiConfig.SubpanelsContainer.style.display = DisplayStyle.Flex;
            }

            // Aplicar animación de mostrar
            panelData.Panel.AddToClassList(panelData.ShowClass);
            
            // Mostrar scrim si es requerido
            if (panelData.RequiresScrim)
            {
                _uiConfig.Scrim.AddToClassList(SCRIM_SHOW_CLASS);
            }

            _currentActivePanel = panelType;
            Debug.Log($"[DeviceSelectionUIManager] Panel {panelType} opened");
        }

        /// <summary>
        /// Oculta un panel específico
        /// </summary>
        public void HidePanel(IDeviceSelectionOps.PanelType panelType)
        {
            if (panelType == IDeviceSelectionOps.PanelType.NavigationMenu)
            {
                HideNavigationMenu();
                return;
            }

            if (!_uiConfig.Panels.TryGetValue(panelType, out var panelData))
            {
                Debug.LogError($"Panel type {panelType} not found in configuration");
                return;
            }

            // Verificar que el panel existe
            if (panelData.Panel == null)
            {
                Debug.LogWarning($"Panel {panelType} is not implemented - skipping hide");
                return;
            }

            // Aplicar animación de ocultar
            panelData.Panel.RemoveFromClassList(panelData.ShowClass);
            panelData.Panel.AddToClassList(panelData.HideClass);
            
            // Ocultar scrim si no hay otros paneles que lo requieran
            if (!HasOtherPanelsRequiringScrim(panelType))
            {
                _uiConfig.Scrim.RemoveFromClassList(SCRIM_SHOW_CLASS);
            }

            if (_currentActivePanel == panelType)
            {
                _currentActivePanel = IDeviceSelectionOps.PanelType.None;
            }

            Debug.Log($"[DeviceSelectionUIManager] Panel {panelType} closed");
        }

        /// <summary>
        /// Cierra el panel actualmente abierto
        /// </summary>
        public void CloseCurrentPanel()
        {
            if (_currentActivePanel != IDeviceSelectionOps.PanelType.None)
            {
                HidePanel(_currentActivePanel);
            }
        }

        /// <summary>
        /// Cambia de un panel a otro
        /// </summary>
        public void SwitchPanel(IDeviceSelectionOps.PanelType fromPanel, IDeviceSelectionOps.PanelType toPanel)
        {
            if (fromPanel != IDeviceSelectionOps.PanelType.None)
            {
                HidePanel(fromPanel);
            }
            
            if (toPanel != IDeviceSelectionOps.PanelType.None)
            {
                ShowPanel(toPanel);
            }
        }

        #endregion

        #region Device Management

        /// <summary>
        /// Inicializa los botones de dispositivos
        /// </summary>
        private void InitializeDeviceButtons()
        {
            // Limpiar diccionario existente
            _uiConfig.DeviceButtons.Clear();
            
            // Registrar botones de dispositivos principales
            var arscaraButton = _uiConfig.Body.Q<Button>("ARSCARAButton");
            if (arscaraButton != null) _uiConfig.DeviceButtons["ARSCARA"] = arscaraButton;
            
            var robotKit1Button = _uiConfig.Body.Q<Button>("RobotKit1Button");
            if (robotKit1Button != null) _uiConfig.DeviceButtons["RobotKit1"] = robotKit1Button;
            
            var robotKit2Button = _uiConfig.Body.Q<Button>("RobotKit2Button");
            if (robotKit2Button != null) _uiConfig.DeviceButtons["RobotKit2"] = robotKit2Button;
            
            // También buscar automáticamente botones con clase device-button
            var deviceButtons = _uiConfig.Body.Query<Button>(className: "device-button").ToList();
            foreach (var button in deviceButtons)
            {
                var deviceId = ExtractDeviceIdFromButtonName(button.name);
                if (!string.IsNullOrEmpty(deviceId) && !_uiConfig.DeviceButtons.ContainsKey(deviceId))
                {
                    _uiConfig.DeviceButtons[deviceId] = button;
                }
            }
            
            Debug.Log($"[DeviceSelectionUIManager] Initialized {_uiConfig.DeviceButtons.Count} device buttons");
        }

        /// <summary>
        /// Extrae device ID del nombre de un botón
        /// </summary>
        private string ExtractDeviceIdFromButtonName(string buttonName)
        {
            return buttonName switch
            {
                "ARSCARAButton" => "ARSCARA",
                "RobotKit1Button" => "RobotKit1",
                "RobotKit2Button" => "RobotKit2",
                _ => buttonName.Replace("Button", "") // Fallback
            };
        }

        /// <summary>
        /// Actualiza los estados de los dispositivos
        /// </summary>
        public void RefreshDeviceStates()
        {
            foreach (var kvp in _uiConfig.DeviceButtons)
            {
                var deviceId = kvp.Key;
                var button = kvp.Value;
                
                if (button == null) continue;
                
                var deviceInfo = _deviceConfig.DeviceRegistry.GetValueOrDefault(deviceId);
                if (deviceInfo == null) continue;
                
                // Actualizar texto del botón con información de estado
                UpdateDeviceButtonText(button, deviceInfo);
            }
            
            Debug.Log("[DeviceSelectionUIManager] Device states refreshed");
        }

        /// <summary>
        /// Actualiza el texto de un botón de dispositivo
        /// </summary>
        private void UpdateDeviceButtonText(Button button, DeviceSelectionInfo.DeviceInfo deviceInfo)
        {
            var statusText = deviceInfo.GetStatusText();
            var displayText = $"{deviceInfo.DisplayName}\n({statusText})";
            
            // Solo actualizar si el texto es diferente para evitar flickering
            if (button.text != displayText)
            {
                button.text = displayText;
            }
        }

        /// <summary>
        /// Resalta un dispositivo seleccionado
        /// </summary>
        public void HighlightSelectedDevice(string deviceId)
        {
            // Limpiar selección anterior
            foreach (var kvp in _uiConfig.DeviceButtons)
            {
                kvp.Value?.RemoveFromClassList("device-selected");
            }
            
            // Resaltar dispositivo actual
            if (_uiConfig.DeviceButtons.TryGetValue(deviceId, out var selectedButton))
            {
                selectedButton?.AddToClassList("device-selected");
                Debug.Log($"[DeviceSelectionUIManager] Device highlighted: {deviceId}");
            }
        }

        #endregion

        #region Panel State Queries

        /// <summary>
        /// Verifica si algún panel está visible
        /// </summary>
        public bool IsAnyPanelVisible()
        {
            foreach (var kvp in _uiConfig.Panels)
            {
                var panelData = kvp.Value;
                if (panelData.Panel != null && panelData.Panel.ClassListContains(panelData.ShowClass))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Verifica si hay otros paneles que requieren scrim
        /// </summary>
        private bool HasOtherPanelsRequiringScrim(IDeviceSelectionOps.PanelType excludePanel)
        {
            foreach (var kvp in _uiConfig.Panels)
            {
                if (kvp.Key == excludePanel) continue;
        
                var panelData = kvp.Value;
                if (panelData.Panel != null && 
                    panelData.RequiresScrim && 
                    panelData.Panel.ClassListContains(panelData.ShowClass))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Cleanup and Utilities

        /// <summary>
        /// Cierra todos los paneles
        /// </summary>
        private void CloseAllPanels()
        {
            foreach (var kvp in _uiConfig.Panels)
            {
                if (kvp.Value.Panel != null)
                {
                    HidePanel(kvp.Key);
                }
            }
    
            _currentActivePanel = IDeviceSelectionOps.PanelType.None;
            _isNavigationMenuOpen = false;
        }

        /// <summary>
        /// Limpia todos los estados de paneles
        /// </summary>
        private void ClearAllPanelStates()
        {
            foreach (var kvp in _uiConfig.Panels)
            {
                var panelData = kvp.Value;
                if (panelData.Panel != null)
                {
                    panelData.Panel.RemoveFromClassList(panelData.ShowClass);
            
                    // NO remover la clase base de oculto para NavigationMenu
                    if (kvp.Key != IDeviceSelectionOps.PanelType.NavigationMenu)
                    {
                        panelData.Panel.RemoveFromClassList(panelData.HideClass);
                    }
                }
            }
    
            _uiConfig.Scrim?.RemoveFromClassList(SCRIM_SHOW_CLASS);
        }

        /// <summary>
        /// Registra callbacks de transición
        /// </summary>
        private void RegisterTransitionCallbacks()
        {
            foreach (var panelData in _uiConfig.Panels.Values)
            {
                if (panelData.Panel != null)
                {
                    panelData.Panel.RegisterCallback<TransitionEndEvent>(OnPanelTransitionComplete);
                }
            }
        }

        /// <summary>
        /// Maneja el final de transiciones de paneles
        /// </summary>
        private void OnPanelTransitionComplete(TransitionEndEvent evt)
        {
            // Si no hay paneles visibles, ocultar el contenedor
            if (!IsAnyPanelVisible())
            {
                _uiConfig.SubpanelsContainer.style.display = DisplayStyle.None;
                Debug.Log("[DeviceSelectionUIManager] All panels closed - hiding container");
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Panel actualmente activo
        /// </summary>
        public IDeviceSelectionOps.PanelType CurrentActivePanel => _currentActivePanel;

        /// <summary>
        /// Estado del menú de navegación
        /// </summary>
        public bool NavigationMenuOpen => _isNavigationMenuOpen;

        /// <summary>
        /// Contexto actual de lanzamiento
        /// </summary>
        public IDeviceSelectionOps.LaunchContext CurrentContext => _contextData.CurrentContext;

        /// <summary>
        /// Configuración de dispositivos
        /// </summary>
        public DeviceSelectionInfo.DeviceConfiguration DeviceConfiguration => _deviceConfig;

        /// <summary>
        /// Datos del contexto actual
        /// </summary>
        public DeviceSelectionInfo.ContextData ContextData => _contextData;

        #endregion
    }
}