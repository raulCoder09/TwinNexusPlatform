using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.SettingsController
{
    /// <summary>
    /// Maneja la lógica de UI del Settings, incluyendo paneles modales y menú lateral
    /// Equivalente a WelcomeUIManager, DashboardUIManager y DeviceSelectionUIManager
    /// </summary>
    public class SettingsUIManager
    {
        private SettingsInfo.UIConfiguration _uiConfig;
        private ISettingsOps.PanelType _currentActivePanel = ISettingsOps.PanelType.None;
        private bool _isNavigationMenuOpen = false;

        // Clases USS para scrim
        private const string SCRIM_SHOW_CLASS = "ScrimOpaque";

        // Configuración de configuraciones
        private SettingsInfo.ConfigurationRegistry _configRegistry;

        #region Constructor

        public SettingsUIManager(SettingsInfo.UIConfiguration uiConfig)
        {
            _uiConfig = uiConfig ?? throw new ArgumentNullException(nameof(uiConfig));
            _configRegistry = SettingsInfo.ConfigurationRegistry.CreateDefault();
        }

        #endregion

        #region UI State Management

        /// <summary>
        /// Inicializa el sistema de paneles del Settings
        /// </summary>
        public void InitializePanelSystem()
        {
            // Asegurar estado inicial correcto
            _uiConfig.SubpanelsContainer.style.display = DisplayStyle.None;
            HideUi(); // Settings inicia oculto hasta que se active
            ClearAllPanelStates();
            RegisterTransitionCallbacks();

            // Inicializar botones de configuración UI
            InitializeConfigurationButtons();

            // Configurar menú de navegación con estado activo
            UpdateNavigationMenuActiveStates();

            Debug.Log("[SettingsUIManager] Panel system initialized");
        }

        /// <summary>
        /// Muestra la UI principal del Settings
        /// </summary>
        public void ShowUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.Flex;

            // Actualizar UI al mostrar
            UpdateNavigationMenuActiveStates();
            RefreshConfigurationStates();

            Debug.Log("[SettingsUIManager] Settings UI shown");
        }

        /// <summary>
        /// Oculta la UI principal del Settings
        /// </summary>
        public void HideUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.None;

            // Asegurar que todos los paneles estén cerrados
            CloseAllPanels();
            Debug.Log("[SettingsUIManager] Settings UI hidden");
        }

        #endregion

        #region Navigation Menu Management

        /// <summary>
        /// Actualiza el estado activo de los botones del menú (Settings activo)
        /// </summary>
        private void UpdateNavigationMenuActiveStates()
        {
            if (_uiConfig.SubpanelsContainer == null) return;

            // Obtener botones del menú de navegación
            var dashboardButton = _uiConfig.SubpanelsContainer.Q<Button>("DashboardButton");
            var trainingButton = _uiConfig.SubpanelsContainer.Q<Button>("TrainingButton");
            var operationsButton = _uiConfig.SubpanelsContainer.Q<Button>("OperationsButton");
            var settingsButton = _uiConfig.SubpanelsContainer.Q<Button>("SettingsButton");

            // Limpiar estados activos existentes
            dashboardButton?.RemoveFromClassList("navigation-menu-button-active");
            trainingButton?.RemoveFromClassList("navigation-menu-button-active");
            operationsButton?.RemoveFromClassList("navigation-menu-button-active");
            settingsButton?.RemoveFromClassList("navigation-menu-button-active");

            // Settings siempre activo cuando estamos en Settings
            settingsButton?.AddToClassList("navigation-menu-button-active");
            Debug.Log("[SettingsUIManager] Settings button marked as active");
        }

        /// <summary>
        /// Muestra el menú de navegación lateral
        /// </summary>
        public void ShowNavigationMenu()
        {
            if (_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(ISettingsOps.PanelType.NavigationMenu, out var menuPanel))
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

            _currentActivePanel = ISettingsOps.PanelType.NavigationMenu;
            _isNavigationMenuOpen = true;
            UpdateNavigationMenuActiveStates();

            Debug.Log("[SettingsUIManager] Navigation menu opened");
        }

        /// <summary>
        /// Oculta el menú de navegación lateral
        /// </summary>
        public void HideNavigationMenu()
        {
            if (!_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(ISettingsOps.PanelType.NavigationMenu, out var menuPanel))
            {
                Debug.LogError("NavigationMenu panel not found in configuration");
                return;
            }

            menuPanel.Panel.RemoveFromClassList(menuPanel.ShowClass);

            // Ocultar scrim
            _uiConfig.Scrim.RemoveFromClassList(SCRIM_SHOW_CLASS);

            _currentActivePanel = ISettingsOps.PanelType.None;
            _isNavigationMenuOpen = false;

            Debug.Log("[SettingsUIManager] Navigation menu closed");
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
        public void ShowPanel(ISettingsOps.PanelType panelType)
        {
            if (panelType == ISettingsOps.PanelType.NavigationMenu)
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
            if (_currentActivePanel != ISettingsOps.PanelType.None &&
                _currentActivePanel != ISettingsOps.PanelType.NavigationMenu)
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
            Debug.Log($"[SettingsUIManager] Panel {panelType} opened");
        }

        /// <summary>
        /// Oculta un panel específico
        /// </summary>
        public void HidePanel(ISettingsOps.PanelType panelType)
        {
            if (panelType == ISettingsOps.PanelType.NavigationMenu)
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
                _currentActivePanel = ISettingsOps.PanelType.None;
            }

            Debug.Log($"[SettingsUIManager] Panel {panelType} closed");
        }

        /// <summary>
        /// Cierra el panel actualmente abierto
        /// </summary>
        public void CloseCurrentPanel()
        {
            if (_currentActivePanel != ISettingsOps.PanelType.None)
            {
                HidePanel(_currentActivePanel);
            }
        }

        /// <summary>
        /// Cambia de un panel a otro
        /// </summary>
        public void SwitchPanel(ISettingsOps.PanelType fromPanel, ISettingsOps.PanelType toPanel)
        {
            if (fromPanel != ISettingsOps.PanelType.None)
            {
                HidePanel(fromPanel);
            }

            if (toPanel != ISettingsOps.PanelType.None)
            {
                ShowPanel(toPanel);
            }
        }

        #endregion

        #region Configuration Management

        /// <summary>
        /// Inicializa los botones de configuración
        /// </summary>
        private void InitializeConfigurationButtons()
        {
            // Limpiar diccionario existente
            _uiConfig.ConfigurationButtons.Clear();

            // Registrar botones de configuración principales
            var iotButton = _uiConfig.Body.Q<Button>("IoTButton");
            if (iotButton != null) _uiConfig.ConfigurationButtons["IoT"] = iotButton;

            var userConfigButton = _uiConfig.Body.Q<Button>("UserConfigButton");
            if (userConfigButton != null) _uiConfig.ConfigurationButtons["User"] = userConfigButton;

            var cognitoConfigButton = _uiConfig.Body.Q<Button>("CognitoConfigButton");
            if (cognitoConfigButton != null) _uiConfig.ConfigurationButtons["Cognito"] = cognitoConfigButton;

            var systemConfigButton = _uiConfig.Body.Q<Button>("SystemConfigButton");
            if (systemConfigButton != null) _uiConfig.ConfigurationButtons["System"] = systemConfigButton;

            var networkConfigButton = _uiConfig.Body.Q<Button>("NetworkConfigButton");
            if (networkConfigButton != null) _uiConfig.ConfigurationButtons["Network"] = networkConfigButton;

            // También buscar automáticamente botones con clase config-button
            var configButtons = _uiConfig.Body.Query<Button>(className: "config-button").ToList();
            foreach (var button in configButtons)
            {
                var configId = ExtractConfigIdFromButtonName(button.name);
                if (!string.IsNullOrEmpty(configId) && !_uiConfig.ConfigurationButtons.ContainsKey(configId))
                {
                    _uiConfig.ConfigurationButtons[configId] = button;
                }
            }

            Debug.Log($"[SettingsUIManager] Initialized {_uiConfig.ConfigurationButtons.Count} configuration buttons");
        }

        /// <summary>
        /// Extrae config ID del nombre de un botón
        /// </summary>
        private string ExtractConfigIdFromButtonName(string buttonName)
        {
            return buttonName switch
            {
                "IoTButton" => "IoT",
                "UserConfigButton" => "User",
                "CognitoConfigButton" => "Cognito",
                "SystemConfigButton" => "System",
                "NetworkConfigButton" => "Network",
                _ => buttonName.Replace("Button", "").Replace("Config", "") // Fallback
            };
        }

        /// <summary>
        /// Actualiza los estados de las configuraciones
        /// </summary>
        public void RefreshConfigurationStates()
        {
            foreach (var kvp in _uiConfig.ConfigurationButtons)
            {
                var configId = kvp.Key;
                var button = kvp.Value;

                if (button == null) continue;

                // Aquí se puede agregar lógica para actualizar estados de botones
                // basado en disponibilidad, permisos, etc.
                UpdateConfigurationButtonStyling(button, configId);
            }

            Debug.Log("[SettingsUIManager] Configuration states refreshed");
        }

        /// <summary>
        /// Actualiza el styling de un botón de configuración
        /// </summary>
        private void UpdateConfigurationButtonStyling(Button button, string configId)
        {
            // Limpiar clases existentes
            button.RemoveFromClassList("config-available");
            button.RemoveFromClassList("config-unavailable");
            button.RemoveFromClassList("config-restricted");

            // Por ahora todas las configuraciones están disponibles
            // En el futuro se puede agregar lógica de permisos aquí
            button.AddToClassList("config-available");
        }

        /// <summary>
        /// Resalta una configuración seleccionada
        /// </summary>
        public void HighlightSelectedConfiguration(string configId)
        {
            // Limpiar selección anterior
            foreach (var kvp in _uiConfig.ConfigurationButtons)
            {
                kvp.Value?.RemoveFromClassList("config-selected");
            }

            // Resaltar configuración actual
            if (_uiConfig.ConfigurationButtons.TryGetValue(configId, out var selectedButton))
            {
                selectedButton?.AddToClassList("config-selected");
                Debug.Log($"[SettingsUIManager] Configuration highlighted: {configId}");
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
        private bool HasOtherPanelsRequiringScrim(ISettingsOps.PanelType excludePanel)
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

            _currentActivePanel = ISettingsOps.PanelType.None;
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
                    if (kvp.Key != ISettingsOps.PanelType.NavigationMenu)
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
                Debug.Log("[SettingsUIManager] All panels closed - hiding container");
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Panel actualmente activo
        /// </summary>
        public ISettingsOps.PanelType CurrentActivePanel => _currentActivePanel;

        /// <summary>
        /// Estado del menú de navegación
        /// </summary>
        public bool NavigationMenuOpen => _isNavigationMenuOpen;

        /// <summary>
        /// Configuración de configuraciones
        /// </summary>
        public SettingsInfo.ConfigurationRegistry ConfigurationRegistry => _configRegistry;

        #endregion
    }
}