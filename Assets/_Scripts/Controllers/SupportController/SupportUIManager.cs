using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.SupportController
{
    /// <summary>
    /// Maneja la lógica de UI del Support Center, incluyendo paneles modales y menú lateral
    /// Equivalente a DashboardUIManager pero especializado para Support
    /// </summary>
    public class SupportUIManager
    {
        private SupportInfo.UIConfiguration _uiConfig;
        private ISupportOps.PanelType _currentActivePanel = ISupportOps.PanelType.None;
        private bool _isNavigationMenuOpen = false;
        
        // Clases USS para scrim
        private const string SCRIM_SHOW_CLASS = "ScrimOpaque";

        #region Constructor

        public SupportUIManager(SupportInfo.UIConfiguration uiConfig)
        {
            _uiConfig = uiConfig ?? throw new ArgumentNullException(nameof(uiConfig));
        }

        #endregion

        #region UI State Management

        /// <summary>
        /// Inicializa el sistema de paneles del Support Center
        /// </summary>
        public void InitializePanelSystem()
        {
            // Asegurar estado inicial correcto
            _uiConfig.SubpanelsContainer.style.display = DisplayStyle.None;
            HideUi(); // Support inicia oculto hasta autenticación
            ClearAllPanelStates();
            RegisterTransitionCallbacks();
            
            Debug.Log("[SupportUIManager] Panel system initialized");
        }

        /// <summary>
        /// Muestra la UI principal del Support Center
        /// </summary>
        public void ShowUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.Flex;
            Debug.Log("[SupportUIManager] Support UI shown");
        }

        /// <summary>
        /// Oculta la UI principal del Support Center
        /// </summary>
        public void HideUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.None;
            
            // Asegurar que todos los paneles estén cerrados
            CloseAllPanels();
            Debug.Log("[SupportUIManager] Support UI hidden");
        }

        #endregion

        #region Navigation Menu Management

        /// <summary>
        /// Muestra el menú de navegación lateral
        /// </summary>
        public void ShowNavigationMenu()
        {
            if (_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(ISupportOps.PanelType.NavigationMenu, out var menuPanel))
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

            _currentActivePanel = ISupportOps.PanelType.NavigationMenu;
            _isNavigationMenuOpen = true;
    
            Debug.Log("[SupportUIManager] Navigation menu opened");
        }

        /// <summary>
        /// Oculta el menú de navegación lateral
        /// </summary>
        public void HideNavigationMenu()
        {
            if (!_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(ISupportOps.PanelType.NavigationMenu, out var menuPanel))
            {
                Debug.LogError("NavigationMenu panel not found in configuration");
                return;
            }

            menuPanel.Panel.RemoveFromClassList(menuPanel.ShowClass);
    
            // Ocultar scrim
            _uiConfig.Scrim.RemoveFromClassList(SCRIM_SHOW_CLASS);

            _currentActivePanel = ISupportOps.PanelType.None;
            _isNavigationMenuOpen = false;
    
            Debug.Log("[SupportUIManager] Navigation menu closed");
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
        public void ShowPanel(ISupportOps.PanelType panelType)
        {
            if (panelType == ISupportOps.PanelType.NavigationMenu)
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
                Debug.LogWarning($"Panel {panelType} UI not implemented yet");
                return;
            }

            // Si hay otro panel modal abierto, cerrarlo primero
            if (_currentActivePanel != ISupportOps.PanelType.None && 
                _currentActivePanel != ISupportOps.PanelType.NavigationMenu)
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
            Debug.Log($"[SupportUIManager] Panel {panelType} opened");
        }

        /// <summary>
        /// Oculta un panel específico
        /// </summary>
        public void HidePanel(ISupportOps.PanelType panelType)
        {
            if (panelType == ISupportOps.PanelType.NavigationMenu)
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
                Debug.LogWarning($"Panel {panelType} UI not implemented yet");
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
                _currentActivePanel = ISupportOps.PanelType.None;
            }

            Debug.Log($"[SupportUIManager] Panel {panelType} closed");
        }

        /// <summary>
        /// Cierra el panel actualmente abierto
        /// </summary>
        public void CloseCurrentPanel()
        {
            if (_currentActivePanel != ISupportOps.PanelType.None)
            {
                HidePanel(_currentActivePanel);
            }
        }

        /// <summary>
        /// Cambia de un panel a otro
        /// </summary>
        public void SwitchPanel(ISupportOps.PanelType fromPanel, ISupportOps.PanelType toPanel)
        {
            if (fromPanel != ISupportOps.PanelType.None)
            {
                HidePanel(fromPanel);
            }
            
            if (toPanel != ISupportOps.PanelType.None)
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
        private bool HasOtherPanelsRequiringScrim(ISupportOps.PanelType excludePanel)
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

        #region Support-Specific UI Updates

        /// <summary>
        /// Actualiza los labels de estado del soporte
        /// </summary>
        public void UpdateSupportStatus(ISupportOps.SupportStatus status, string message = "")
        {
            // Buscar elementos de estado en la UI
            var statusLabel = _uiConfig.Body?.Q<Label>("SupportStatusLabel");
            var statusMessage = _uiConfig.Body?.Q<Label>("SupportStatusMessage");

            if (statusLabel != null)
            {
                statusLabel.text = GetStatusText(status);
                
                // Actualizar clases USS para styling
                statusLabel.RemoveFromClassList("status-available");
                statusLabel.RemoveFromClassList("status-busy");
                statusLabel.RemoveFromClassList("status-maintenance");
                statusLabel.RemoveFromClassList("status-offline");
                statusLabel.RemoveFromClassList("status-emergency");

                switch (status)
                {
                    case ISupportOps.SupportStatus.Available:
                        statusLabel.AddToClassList("status-available");
                        break;
                    case ISupportOps.SupportStatus.Busy:
                        statusLabel.AddToClassList("status-busy");
                        break;
                    case ISupportOps.SupportStatus.Maintenance:
                        statusLabel.AddToClassList("status-maintenance");
                        break;
                    case ISupportOps.SupportStatus.Offline:
                        statusLabel.AddToClassList("status-offline");
                        break;
                    case ISupportOps.SupportStatus.Emergency:
                        statusLabel.AddToClassList("status-emergency");
                        break;
                }
            }

            if (statusMessage != null && !string.IsNullOrEmpty(message))
            {
                statusMessage.text = message;
                statusMessage.style.display = DisplayStyle.Flex;
            }
            else if (statusMessage != null)
            {
                statusMessage.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Actualiza el contador de tickets abiertos
        /// </summary>
        public void UpdateOpenTicketsCount(int count)
        {
            var ticketsLabel = _uiConfig.Body?.Q<Label>("OpenTicketsLabel");
            if (ticketsLabel != null)
            {
                ticketsLabel.text = count.ToString();
                
                // Cambiar estilo según la cantidad
                ticketsLabel.RemoveFromClassList("tickets-none");
                ticketsLabel.RemoveFromClassList("tickets-few");
                ticketsLabel.RemoveFromClassList("tickets-many");
                
                if (count == 0)
                    ticketsLabel.AddToClassList("tickets-none");
                else if (count <= 3)
                    ticketsLabel.AddToClassList("tickets-few");
                else
                    ticketsLabel.AddToClassList("tickets-many");
            }
        }

        /// <summary>
        /// Actualiza la última fecha de diagnósticos
        /// </summary>
        public void UpdateLastDiagnosticLabel(DateTime? lastRun)
        {
            var diagnosticLabel = _uiConfig.Body?.Q<Label>("LastDiagnosticLabel");
            if (diagnosticLabel != null)
            {
                if (lastRun.HasValue)
                {
                    var timeSince = DateTime.Now - lastRun.Value;
                    if (timeSince.TotalDays >= 1)
                        diagnosticLabel.text = $"Last run: {timeSince.Days} days ago";
                    else if (timeSince.TotalHours >= 1)
                        diagnosticLabel.text = $"Last run: {(int)timeSince.TotalHours} hours ago";
                    else
                        diagnosticLabel.text = $"Last run: {(int)timeSince.TotalMinutes} minutes ago";
                }
                else
                {
                    diagnosticLabel.text = "Last run: Never";
                }
            }
        }

        /// <summary>
        /// Muestra/oculta indicador de diagnósticos en ejecución
        /// </summary>
        public void ShowDiagnosticsRunning(bool isRunning)
        {
            var diagnosticsButton = _uiConfig.Body?.Q<Button>("DiagnosticsButton");
            var runningIndicator = _uiConfig.Body?.Q<VisualElement>("DiagnosticsRunningIndicator");
            
            if (diagnosticsButton != null)
            {
                diagnosticsButton.SetEnabled(!isRunning);
                diagnosticsButton.text = isRunning ? "Running Diagnostics..." : "Run Diagnostics";
            }
            
            if (runningIndicator != null)
            {
                runningIndicator.style.display = isRunning ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// Actualiza el estado de asistencia remota
        /// </summary>
        public void UpdateRemoteAssistanceStatus(bool isActive, string sessionId = "")
        {
            var remoteButton = _uiConfig.Body?.Q<Button>("RemoteAssistanceButton");
            var statusIndicator = _uiConfig.Body?.Q<VisualElement>("RemoteAssistanceIndicator");
            var sessionLabel = _uiConfig.Body?.Q<Label>("RemoteSessionLabel");
            
            if (remoteButton != null)
            {
                remoteButton.text = isActive ? "End Remote Session" : "Request Remote Support";
                remoteButton.RemoveFromClassList("remote-inactive");
                remoteButton.RemoveFromClassList("remote-active");
                remoteButton.AddToClassList(isActive ? "remote-active" : "remote-inactive");
            }
            
            if (statusIndicator != null)
            {
                statusIndicator.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            if (sessionLabel != null)
            {
                if (isActive && !string.IsNullOrEmpty(sessionId))
                {
                    sessionLabel.text = $"Session: {sessionId}";
                    sessionLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    sessionLabel.style.display = DisplayStyle.None;
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Obtiene el texto de estado correspondiente
        /// </summary>
        private string GetStatusText(ISupportOps.SupportStatus status)
        {
            return status switch
            {
                ISupportOps.SupportStatus.Available => "All systems operational",
                ISupportOps.SupportStatus.Busy => "High volume - longer wait times",
                ISupportOps.SupportStatus.Maintenance => "Maintenance in progress",
                ISupportOps.SupportStatus.Offline => "Support temporarily unavailable",
                ISupportOps.SupportStatus.Emergency => "Emergency mode active",
                _ => "Unknown status"
            };
        }

        #endregion

        #region Cleanup and Utilities

        /// <summary>
        /// Cierra todos los paneles
        /// </summary>
        private void CloseAllPanels()
        {
            // Solo cerrar paneles que realmente existen
            foreach (var kvp in _uiConfig.Panels)
            {
                if (kvp.Value.Panel != null)
                {
                    HidePanel(kvp.Key);
                }
            }
    
            _currentActivePanel = ISupportOps.PanelType.None;
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
                    if (kvp.Key != ISupportOps.PanelType.NavigationMenu)
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
                Debug.Log("[SupportUIManager] All panels closed - hiding container");
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Panel actualmente activo
        /// </summary>
        public ISupportOps.PanelType CurrentActivePanel => _currentActivePanel;

        /// <summary>
        /// Estado del menú de navegación
        /// </summary>
        public bool NavigationMenuOpen => _isNavigationMenuOpen;

        #endregion
    }
}