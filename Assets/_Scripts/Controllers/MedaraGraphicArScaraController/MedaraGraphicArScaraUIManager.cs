using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.MedaraGraphicArScaraController
{
    /// <summary>
    /// Maneja la lógica de UI de AWS Settings, incluyendo el menú lateral
    /// Equivalente a DashboardUIManager pero especializado para AWS Settings
    /// </summary>
    public class MedaraGraphicArScaraUIManager
    {
        private MedaraGraphicArScaraInfo.UIConfiguration _uiConfig;
        private IMedaraGraphicArScaraOps.PanelType _currentActivePanel = IMedaraGraphicArScaraOps.PanelType.None;
        private bool _isNavigationMenuOpen = false;
        
        // Clases USS para scrim
        private const string SCRIM_SHOW_CLASS = "ScrimOpaque";

        #region Constructor

        public MedaraGraphicArScaraUIManager(MedaraGraphicArScaraInfo.UIConfiguration uiConfig)
        {
            _uiConfig = uiConfig ?? throw new ArgumentNullException(nameof(uiConfig));
        }

        #endregion

        #region UI State Management

        /// <summary>
        /// Inicializa el sistema de paneles de AWS Settings
        /// </summary>
        public void InitializePanelSystem()
        {
            // Asegurar estado inicial correcto
            _uiConfig.SubpanelsContainer.style.display = DisplayStyle.None;
            ClearAllPanelStates();
            RegisterTransitionCallbacks();
            
            Debug.Log("[MedaraGraphicArScaraUIManager] Panel system initialized");
        }

        /// <summary>
        /// Muestra la UI principal de AWS Settings
        /// </summary>
        public void ShowUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.Flex;
            Debug.Log("[MedaraGraphicArScaraUIManager] AWS Settings UI shown");
        }

        /// <summary>
        /// Oculta la UI principal de AWS Settings
        /// </summary>
        public void HideUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.None;
            
            // Asegurar que todos los paneles estén cerrados
            CloseAllPanels();
            Debug.Log("[MedaraGraphicArScaraUIManager] AWS Settings UI hidden");
        }

        #endregion

        #region Navigation Menu Management

        /// <summary>
        /// Muestra el menú de navegación lateral
        /// </summary>
        public void ShowNavigationMenu()
        {
            if (_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(IMedaraGraphicArScaraOps.PanelType.NavigationMenu, out var menuPanel))
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

            _currentActivePanel = IMedaraGraphicArScaraOps.PanelType.NavigationMenu;
            _isNavigationMenuOpen = true;
    
            Debug.Log("[MedaraGraphicArScaraUIManager] Navigation menu opened");
        }

        /// <summary>
        /// Oculta el menú de navegación lateral
        /// </summary>
        public void HideNavigationMenu()
        {
            if (!_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(IMedaraGraphicArScaraOps.PanelType.NavigationMenu, out var menuPanel))
            {
                Debug.LogError("NavigationMenu panel not found in configuration");
                return;
            }

            menuPanel.Panel.RemoveFromClassList(menuPanel.ShowClass);
    
            // Ocultar scrim
            _uiConfig.Scrim.RemoveFromClassList(SCRIM_SHOW_CLASS);

            _currentActivePanel = IMedaraGraphicArScaraOps.PanelType.None;
            _isNavigationMenuOpen = false;
    
            Debug.Log("[MedaraGraphicArScaraUIManager] Navigation menu closed");
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

        #region Panel Management (Modal Panels - Futuros)

        /// <summary>
        /// Muestra un panel modal (no el menú lateral)
        /// </summary>
        public void ShowPanel(IMedaraGraphicArScaraOps.PanelType panelType)
        {
            if (panelType == IMedaraGraphicArScaraOps.PanelType.NavigationMenu)
            {
                ShowNavigationMenu();
                return;
            }

            if (!_uiConfig.Panels.TryGetValue(panelType, out var panelData))
            {
                Debug.LogWarning($"Panel type {panelType} not found in configuration - will be implemented later");
                return;
            }

            // Si hay otro panel modal abierto, cerrarlo primero
            if (_currentActivePanel != IMedaraGraphicArScaraOps.PanelType.None && 
                _currentActivePanel != IMedaraGraphicArScaraOps.PanelType.NavigationMenu)
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
            Debug.Log($"[MedaraGraphicArScaraUIManager] Panel {panelType} opened");
        }

        /// <summary>
        /// Oculta un panel específico
        /// </summary>
        public void HidePanel(IMedaraGraphicArScaraOps.PanelType panelType)
        {
            if (panelType == IMedaraGraphicArScaraOps.PanelType.NavigationMenu)
            {
                HideNavigationMenu();
                return;
            }

            if (!_uiConfig.Panels.TryGetValue(panelType, out var panelData))
            {
                Debug.LogWarning($"Panel type {panelType} not found in configuration");
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
                _currentActivePanel = IMedaraGraphicArScaraOps.PanelType.None;
            }

            Debug.Log($"[MedaraGraphicArScaraUIManager] Panel {panelType} closed");
        }

        /// <summary>
        /// Cierra el panel actualmente abierto
        /// </summary>
        public void CloseCurrentPanel()
        {
            if (_currentActivePanel != IMedaraGraphicArScaraOps.PanelType.None)
            {
                HidePanel(_currentActivePanel);
            }
        }

        /// <summary>
        /// Cambia de un panel a otro
        /// </summary>
        public void SwitchPanel(IMedaraGraphicArScaraOps.PanelType fromPanel, IMedaraGraphicArScaraOps.PanelType toPanel)
        {
            if (fromPanel != IMedaraGraphicArScaraOps.PanelType.None)
            {
                HidePanel(fromPanel);
            }
            
            if (toPanel != IMedaraGraphicArScaraOps.PanelType.None)
            {
                ShowPanel(toPanel);
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
        private bool HasOtherPanelsRequiringScrim(IMedaraGraphicArScaraOps.PanelType excludePanel)
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

        #region AWS Settings Specific UI Updates (Futuros)

        /// <summary>
        /// Actualiza el estado visual de credenciales AWS
        /// </summary>
        public void UpdateCredentialsStatus(bool isValid, string message = null)
        {
            // TODO: Implementar cuando se agregue contenido al Main
            Debug.Log($"[MedaraGraphicArScaraUIManager] Credentials status updated: {isValid} - {message}");
        }

        /// <summary>
        /// Actualiza resultados de pruebas de conexión
        /// </summary>
        public void UpdateConnectionTestResults(Dictionary<string, MedaraGraphicArScaraInfo.ConnectionTestResult> results)
        {
            // TODO: Implementar cuando se agregue contenido al Main
            Debug.Log($"[MedaraGraphicArScaraUIManager] Connection test results updated for {results.Count} services");
        }

        /// <summary>
        /// Actualiza el estado de servicios AWS
        /// </summary>
        public void UpdateServiceStates(Dictionary<string, bool> serviceStates)
        {
            // TODO: Implementar cuando se agregue contenido al Main
            Debug.Log($"[MedaraGraphicArScaraUIManager] Service states updated for {serviceStates.Count} services");
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
    
            _currentActivePanel = IMedaraGraphicArScaraOps.PanelType.None;
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
                    // Solo remover la clase de "visible" - mantener clases base
                    panelData.Panel.RemoveFromClassList(panelData.ShowClass);
            
                    // No remover la clase base de oculto para NavigationMenu
                    if (kvp.Key != IMedaraGraphicArScaraOps.PanelType.NavigationMenu)
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
                Debug.Log("[MedaraGraphicArScaraUIManager] All panels closed - hiding container");
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Panel actualmente activo
        /// </summary>
        public IMedaraGraphicArScaraOps.PanelType CurrentActivePanel => _currentActivePanel;

        /// <summary>
        /// Estado del menú de navegación
        /// </summary>
        public bool NavigationMenuOpen => _isNavigationMenuOpen;

        #endregion
    }
}