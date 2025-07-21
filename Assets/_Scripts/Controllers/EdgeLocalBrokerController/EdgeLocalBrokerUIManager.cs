using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.EdgeLocalBrokerController
{
    /// <summary>
    /// Maneja la lógica de UI del EdgeLocalBroker, incluyendo paneles modales y menú lateral
    /// Equivalente a WelcomeUIManager pero especializado para EdgeLocalBroker
    /// </summary>
    public class EdgeLocalBrokerUIManager
    {
        private EdgeLocalBrokerInfo.UIConfiguration _uiConfig;
        private IEdgeLocalBrokerOps.PanelType _currentActivePanel = IEdgeLocalBrokerOps.PanelType.None;
        private bool _isNavigationMenuOpen = false;
        
        // Clases USS para scrim
        private const string SCRIM_SHOW_CLASS = "ScrimOpaque";

        #region Constructor

        public EdgeLocalBrokerUIManager(EdgeLocalBrokerInfo.UIConfiguration uiConfig)
        {
            _uiConfig = uiConfig ?? throw new ArgumentNullException(nameof(uiConfig));
        }

        #endregion

        #region UI State Management

        /// <summary>
        /// Inicializa el sistema de paneles del EdgeLocalBroker
        /// </summary>
        public void InitializePanelSystem()
        {
            // Asegurar estado inicial correcto
            _uiConfig.SubpanelsContainer.style.display = DisplayStyle.None;
            HideUi(); // EdgeLocalBroker inicia oculto hasta autenticación
            ClearAllPanelStates();
            RegisterTransitionCallbacks();
            
            Debug.Log("[EdgeLocalBrokerUIManager] Panel system initialized");
        }

        /// <summary>
        /// Muestra la UI principal del EdgeLocalBroker
        /// </summary>
        public void ShowUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.Flex;
            Debug.Log("[EdgeLocalBrokerUIManager] EdgeLocalBroker UI shown");
        }

        /// <summary>
        /// Oculta la UI principal del EdgeLocalBroker
        /// </summary>
        public void HideUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.None;
            
            // Asegurar que todos los paneles estén cerrados
            CloseAllPanels();
            Debug.Log("[EdgeLocalBrokerUIManager] EdgeLocalBroker UI hidden");
        }

        #endregion

        #region Navigation Menu Management

        /// <summary>
        /// Muestra el menú de navegación lateral
        /// </summary>
        public void ShowNavigationMenu()
        {
            if (_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(IEdgeLocalBrokerOps.PanelType.NavigationMenu, out var menuPanel))
            {
                Debug.LogError("NavigationMenu panel not found in configuration");
                return;
            }

            // Mostrar contenedor y aplicar animación
            _uiConfig.SubpanelsContainer.style.display = DisplayStyle.Flex;
    
            // ✅ AGREGAR clase de visible (SIN remover la clase base)
            menuPanel.Panel.AddToClassList(menuPanel.ShowClass);
    
            // Mostrar scrim si es requerido
            if (menuPanel.RequiresScrim)
            {
                _uiConfig.Scrim.AddToClassList(SCRIM_SHOW_CLASS);
            }

            _currentActivePanel = IEdgeLocalBrokerOps.PanelType.NavigationMenu;
            _isNavigationMenuOpen = true;
    
            Debug.Log("[EdgeLocalBrokerUIManager] Navigation menu opened");
        }


        /// <summary>
        /// Oculta el menú de navegación lateral
        /// </summary>
        public void HideNavigationMenu()
        {
            if (!_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(IEdgeLocalBrokerOps.PanelType.NavigationMenu, out var menuPanel))
            {
                Debug.LogError("NavigationMenu panel not found in configuration");
                return;
            }


            menuPanel.Panel.RemoveFromClassList(menuPanel.ShowClass);
    
            // Ocultar scrim
            _uiConfig.Scrim.RemoveFromClassList(SCRIM_SHOW_CLASS);

            _currentActivePanel = IEdgeLocalBrokerOps.PanelType.None;
            _isNavigationMenuOpen = false;
    
            Debug.Log("[EdgeLocalBrokerUIManager] Navigation menu closed");
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
        public void ShowPanel(IEdgeLocalBrokerOps.PanelType panelType)
        {
            if (panelType == IEdgeLocalBrokerOps.PanelType.NavigationMenu)
            {
                ShowNavigationMenu();
                return;
            }

            if (!_uiConfig.Panels.TryGetValue(panelType, out var panelData))
            {
                Debug.LogError($"Panel type {panelType} not found in configuration");
                return;
            }

            // Si hay otro panel modal abierto, cerrarlo primero
            if (_currentActivePanel != IEdgeLocalBrokerOps.PanelType.None && 
                _currentActivePanel != IEdgeLocalBrokerOps.PanelType.NavigationMenu)
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
            Debug.Log($"[EdgeLocalBrokerUIManager] Panel {panelType} opened");
        }

        /// <summary>
        /// Oculta un panel específico
        /// </summary>
        public void HidePanel(IEdgeLocalBrokerOps.PanelType panelType)
        {
            if (panelType == IEdgeLocalBrokerOps.PanelType.NavigationMenu)
            {
                HideNavigationMenu();
                return;
            }

            if (!_uiConfig.Panels.TryGetValue(panelType, out var panelData))
            {
                Debug.LogError($"Panel type {panelType} not found in configuration");
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
                _currentActivePanel = IEdgeLocalBrokerOps.PanelType.None;
            }

            Debug.Log($"[EdgeLocalBrokerUIManager] Panel {panelType} closed");
        }

        /// <summary>
        /// Cierra el panel actualmente abierto
        /// </summary>
        public void CloseCurrentPanel()
        {
            if (_currentActivePanel != IEdgeLocalBrokerOps.PanelType.None)
            {
                HidePanel(_currentActivePanel);
            }
        }

        /// <summary>
        /// Cambia de un panel a otro
        /// </summary>
        public void SwitchPanel(IEdgeLocalBrokerOps.PanelType fromPanel, IEdgeLocalBrokerOps.PanelType toPanel)
        {
            if (fromPanel != IEdgeLocalBrokerOps.PanelType.None)
            {
                HidePanel(fromPanel);
            }
            
            if (toPanel != IEdgeLocalBrokerOps.PanelType.None)
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
                if (panelData.Panel != null && panelData.Panel.ClassListContains(panelData.ShowClass)) // 👈 VERIFICAR null
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Verifica si hay otros paneles que requieren scrim
        /// </summary>
        private bool HasOtherPanelsRequiringScrim(IEdgeLocalBrokerOps.PanelType excludePanel)
        {
            foreach (var kvp in _uiConfig.Panels)
            {
                if (kvp.Key == excludePanel) continue;
        
                var panelData = kvp.Value;
                if (panelData.Panel != null && // 👈 VERIFICAR null
                    panelData.RequiresScrim && 
                    panelData.Panel.ClassListContains(panelData.ShowClass))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Status Management - DELEGADO AL ORCHESTRATOR

        /// <summary>
        /// Actualiza el estado de conexión IoT a través de labels proporcionados por el Orchestrator
        /// </summary>
        public void UpdateIoTLabels(
            Label localStatusLabel, Label localModeLabel,
            Label vmStatusLabel, Label vmModeLabel,
            Label cloudStatusLabel, Label cloudModeLabel,
            EdgeLocalBrokerInfo.ConnectionStatus localStatus, 
            EdgeLocalBrokerInfo.ConnectionStatus vmStatus, 
            EdgeLocalBrokerInfo.ConnectionStatus cloudStatus)
        {
            // Actualizar labels usando referencias proporcionadas
            UpdateSingleStatusLabel(localStatusLabel, localModeLabel, localStatus, "Local");
            UpdateSingleStatusLabel(vmStatusLabel, vmModeLabel, vmStatus, "VM");
            UpdateSingleStatusLabel(cloudStatusLabel, cloudModeLabel, cloudStatus, "Cloud");
        }

        /// <summary>
        /// Actualiza un label de estado individual
        /// </summary>
        private void UpdateSingleStatusLabel(Label statusLabel, Label modeLabel, 
            EdgeLocalBrokerInfo.ConnectionStatus status, string prefix)
        {
            if (statusLabel == null || modeLabel == null) return;

            // Actualizar texto
            statusLabel.text = $"{prefix} IoT: {GetStatusText(status)}";
            modeLabel.text = GetModeText(status);

            // Actualizar clases USS para styling
            statusLabel.RemoveFromClassList("status-connected");
            statusLabel.RemoveFromClassList("status-disconnected");
            statusLabel.RemoveFromClassList("status-error");
            statusLabel.RemoveFromClassList("status-connecting");

            switch (status)
            {
                case EdgeLocalBrokerInfo.ConnectionStatus.Connected:
                    statusLabel.AddToClassList("status-connected");
                    break;
                case EdgeLocalBrokerInfo.ConnectionStatus.Disconnected:
                    statusLabel.AddToClassList("status-disconnected");
                    break;
                case EdgeLocalBrokerInfo.ConnectionStatus.Error:
                    statusLabel.AddToClassList("status-error");
                    break;
                case EdgeLocalBrokerInfo.ConnectionStatus.Connecting:
                    statusLabel.AddToClassList("status-connecting");
                    break;
            }
        }

        private string GetStatusText(EdgeLocalBrokerInfo.ConnectionStatus status)
        {
            return status switch
            {
                EdgeLocalBrokerInfo.ConnectionStatus.Connected => "Connected",
                EdgeLocalBrokerInfo.ConnectionStatus.Connecting => "Connecting...",
                EdgeLocalBrokerInfo.ConnectionStatus.Disconnected => "Disconnected",
                EdgeLocalBrokerInfo.ConnectionStatus.Error => "Error",
                _ => "Unknown"
            };
        }

        private string GetModeText(EdgeLocalBrokerInfo.ConnectionStatus status)
        {
            return status switch
            {
                EdgeLocalBrokerInfo.ConnectionStatus.Connected => "Active",
                EdgeLocalBrokerInfo.ConnectionStatus.Connecting => "Initializing",
                EdgeLocalBrokerInfo.ConnectionStatus.Disconnected => "Inactive",
                EdgeLocalBrokerInfo.ConnectionStatus.Error => "Failed",
                _ => "Unknown"
            };
        }

        #endregion

        #region Cleanup and Utilities

        /// <summary>
        /// Cierra todos los paneles
        /// </summary>
        private void CloseAllPanels()
        {
            // SOLO cerrar paneles que realmente existen
            foreach (var kvp in _uiConfig.Panels)
            {
                if (kvp.Value.Panel != null) // 👈 VERIFICAR que el panel existe
                {
                    HidePanel(kvp.Key);
                }
            }
    
            _currentActivePanel = IEdgeLocalBrokerOps.PanelType.None;
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
                    // ✅ SOLO remover la clase de "visible" - mantener clases base
                    panelData.Panel.RemoveFromClassList(panelData.ShowClass);
            
                    // ❌ NO remover la clase base de oculto para NavigationMenu
                    if (kvp.Key != IEdgeLocalBrokerOps.PanelType.NavigationMenu)
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
                if (panelData.Panel != null) // 👈 VERIFICAR null
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
                Debug.Log("[EdgeLocalBrokerUIManager] All panels closed - hiding container");
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Panel actualmente activo
        /// </summary>
        public IEdgeLocalBrokerOps.PanelType CurrentActivePanel => _currentActivePanel;

        /// <summary>
        /// Estado del menú de navegación
        /// </summary>
        public bool NavigationMenuOpen => _isNavigationMenuOpen;

        #endregion
    }
}