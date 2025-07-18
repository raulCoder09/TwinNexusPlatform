using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.SESSettingsController
{
    public class SESSettingsUIManager
    {
        private SESSettingsInfo.UIConfiguration _uiConfig;
        private ISESSettingsOps.PanelType _currentActivePanel = ISESSettingsOps.PanelType.None;
        private bool _isNavigationMenuOpen = false;
        
        private const string SCRIM_SHOW_CLASS = "ScrimOpaque";

        public SESSettingsUIManager(SESSettingsInfo.UIConfiguration uiConfig)
        {
            _uiConfig = uiConfig ?? throw new ArgumentNullException(nameof(uiConfig));
        }

        public void InitializePanelSystem()
        {
            _uiConfig.SubpanelsContainer.style.display = DisplayStyle.None;
            ClearAllPanelStates();
            RegisterTransitionCallbacks();
            Debug.Log("[SESSettingsUIManager] Panel system initialized");
        }

        public void ShowUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.Flex;
            Debug.Log("[SESSettingsUIManager] AWS Settings UI shown");
        }

        public void HideUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.None;
            CloseAllPanels();
            Debug.Log("[SESSettingsUIManager] AWS Settings UI hidden");
        }

        public void ShowNavigationMenu()
        {
            if (_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(ISESSettingsOps.PanelType.NavigationMenu, out var menuPanel))
            {
                Debug.LogError("NavigationMenu panel not found in configuration");
                return;
            }

            _uiConfig.SubpanelsContainer.style.display = DisplayStyle.Flex;
            menuPanel.Panel.AddToClassList(menuPanel.ShowClass);
            if (menuPanel.RequiresScrim)
            {
                _uiConfig.Scrim.AddToClassList(SCRIM_SHOW_CLASS);
            }

            _currentActivePanel = ISESSettingsOps.PanelType.NavigationMenu;
            _isNavigationMenuOpen = true;
            Debug.Log("[SESSettingsUIManager] Navigation menu opened");
        }

        public void HideNavigationMenu()
        {
            if (!_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(ISESSettingsOps.PanelType.NavigationMenu, out var menuPanel))
            {
                Debug.LogError("NavigationMenu panel not found in configuration");
                return;
            }

            menuPanel.Panel.RemoveFromClassList(menuPanel.ShowClass);
            _uiConfig.Scrim.RemoveFromClassList(SCRIM_SHOW_CLASS);

            _currentActivePanel = ISESSettingsOps.PanelType.None;
            _isNavigationMenuOpen = false;
            Debug.Log("[SESSettingsUIManager] Navigation menu closed");
        }

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

        public void ShowPanel(ISESSettingsOps.PanelType panelType)
        {
            if (panelType == ISESSettingsOps.PanelType.NavigationMenu)
            {
                ShowNavigationMenu();
                return;
            }

            if (!_uiConfig.Panels.TryGetValue(panelType, out var panelData))
            {
                Debug.LogWarning($"Panel type {panelType} not found in configuration - will be implemented later");
                return;
            }

            if (_currentActivePanel != ISESSettingsOps.PanelType.None && 
                _currentActivePanel != ISESSettingsOps.PanelType.NavigationMenu)
            {
                HidePanel(_currentActivePanel);
            }

            if (_uiConfig.SubpanelsContainer.style.display != DisplayStyle.Flex)
            {
                _uiConfig.SubpanelsContainer.style.display = DisplayStyle.Flex;
            }

            if (panelData.Panel != null)
            {
                panelData.Panel.AddToClassList(panelData.ShowClass);
                if (panelData.RequiresScrim)
                {
                    _uiConfig.Scrim.AddToClassList(SCRIM_SHOW_CLASS);
                }
            }

            _currentActivePanel = panelType;
            Debug.Log($"[SESSettingsUIManager] Panel {panelType} opened");
        }

        public void HidePanel(ISESSettingsOps.PanelType panelType)
        {
            if (panelType == ISESSettingsOps.PanelType.NavigationMenu)
            {
                HideNavigationMenu();
                return;
            }

            if (!_uiConfig.Panels.TryGetValue(panelType, out var panelData))
            {
                Debug.LogWarning($"Panel type {panelType} not found in configuration");
                return;
            }

            if (panelData.Panel != null)
            {
                panelData.Panel.RemoveFromClassList(panelData.ShowClass);
                panelData.Panel.AddToClassList(panelData.HideClass);
                if (!HasOtherPanelsRequiringScrim(panelType))
                {
                    _uiConfig.Scrim.RemoveFromClassList(SCRIM_SHOW_CLASS);
                }
            }

            if (_currentActivePanel == panelType)
            {
                _currentActivePanel = ISESSettingsOps.PanelType.None;
            }

            Debug.Log($"[SESSettingsUIManager] Panel {panelType} closed");
        }

        public void CloseCurrentPanel()
        {
            if (_currentActivePanel != ISESSettingsOps.PanelType.None)
            {
                HidePanel(_currentActivePanel);
            }
        }

        public void SwitchPanel(ISESSettingsOps.PanelType fromPanel, ISESSettingsOps.PanelType toPanel)
        {
            if (fromPanel != ISESSettingsOps.PanelType.None)
            {
                HidePanel(fromPanel);
            }
            
            if (toPanel != ISESSettingsOps.PanelType.None)
            {
                ShowPanel(toPanel);
            }
        }

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

        private bool HasOtherPanelsRequiringScrim(ISESSettingsOps.PanelType excludePanel)
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

        public void UpdateCredentialsStatus(bool isValid, string message = null)
        {
            // TODO: Actualizar una etiqueta específica para mostrar el estado de las credenciales
            Debug.Log($"[SESSettingsUIManager] Credentials status updated: {isValid} - {message}");
        }

        public void UpdateConnectionTestResults(Dictionary<string, SESSettingsInfo.ConnectionTestResult> results)
        {
            // TODO: Actualizar UI para mostrar resultados de pruebas
            Debug.Log($"[SESSettingsUIManager] Connection test results updated for {results.Count} services");
        }

        public void UpdateServiceStates(Dictionary<string, bool> serviceStates)
        {
            // TODO: Actualizar UI para reflejar estados de servicios
            Debug.Log($"[SESSettingsUIManager] Service states updated for {serviceStates.Count} services");
        }

        private void CloseAllPanels()
        {
            foreach (var kvp in _uiConfig.Panels)
            {
                if (kvp.Value.Panel != null)
                {
                    HidePanel(kvp.Key);
                }
            }
    
            _currentActivePanel = ISESSettingsOps.PanelType.None;
            _isNavigationMenuOpen = false;
        }

        private void ClearAllPanelStates()
        {
            foreach (var kvp in _uiConfig.Panels)
            {
                var panelData = kvp.Value;
                if (panelData.Panel != null)
                {
                    panelData.Panel.RemoveFromClassList(panelData.ShowClass);
                    if (kvp.Key != ISESSettingsOps.PanelType.NavigationMenu)
                    {
                        panelData.Panel.RemoveFromClassList(panelData.HideClass);
                    }
                }
            }
    
            _uiConfig.Scrim?.RemoveFromClassList(SCRIM_SHOW_CLASS);
        }

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

        private void OnPanelTransitionComplete(TransitionEndEvent evt)
        {
            if (!IsAnyPanelVisible())
            {
                _uiConfig.SubpanelsContainer.style.display = DisplayStyle.None;
                Debug.Log("[SESSettingsUIManager] All panels closed - hiding container");
            }
        }

        public ISESSettingsOps.PanelType CurrentActivePanel => _currentActivePanel;
        public bool NavigationMenuOpen => _isNavigationMenuOpen;
    }
}