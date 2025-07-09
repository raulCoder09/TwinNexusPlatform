using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.ReportsController
{
    public class ReportsUIManager
    {
        private ReportsInfo.UIConfiguration _uiConfig;
        private IReportsOps.PanelType _currentActivePanel = IReportsOps.PanelType.None;
        private bool _isNavigationMenuOpen = false;
        private const string SCRIM_SHOW_CLASS = "ScrimOpaque";

        public ReportsUIManager(ReportsInfo.UIConfiguration uiConfig)
        {
            _uiConfig = uiConfig ?? throw new ArgumentNullException(nameof(uiConfig));
        }

        public void InitializePanelSystem()
        {
            _uiConfig.SubpanelsContainer.style.display = DisplayStyle.None;
            HideUi();
            ClearAllPanelStates();
            RegisterTransitionCallbacks();
            Debug.Log("[ReportsUIManager] Panel system initialized");
        }

        public void ShowUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.Flex;
            Debug.Log("[ReportsUIManager] Reports UI shown");
        }

        public void HideUi()
        {
            _uiConfig.Body.style.display = DisplayStyle.None;
            CloseAllPanels();
            Debug.Log("[ReportsUIManager] Reports UI hidden");
        }

        public void ShowNavigationMenu()
        {
            if (_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(IReportsOps.PanelType.NavigationMenu, out var menuPanel))
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

            _currentActivePanel = IReportsOps.PanelType.NavigationMenu;
            _isNavigationMenuOpen = true;
            Debug.Log("[ReportsUIManager] Navigation menu opened");
        }

        public void HideNavigationMenu()
        {
            if (!_isNavigationMenuOpen) return;

            if (!_uiConfig.Panels.TryGetValue(IReportsOps.PanelType.NavigationMenu, out var menuPanel))
            {
                Debug.LogError("NavigationMenu panel not found in configuration");
                return;
            }

            menuPanel.Panel.RemoveFromClassList(menuPanel.ShowClass);
            _uiConfig.Scrim.RemoveFromClassList(SCRIM_SHOW_CLASS);

            _currentActivePanel = IReportsOps.PanelType.None;
            _isNavigationMenuOpen = false;
            Debug.Log("[ReportsUIManager] Navigation menu closed");
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

        public void ShowPanel(IReportsOps.PanelType panelType)
        {
            if (panelType == IReportsOps.PanelType.NavigationMenu)
            {
                ShowNavigationMenu();
                return;
            }

            if (!_uiConfig.Panels.TryGetValue(panelType, out var panelData))
            {
                Debug.LogError($"Panel type {panelType} not found in configuration");
                return;
            }

            if (_currentActivePanel != IReportsOps.PanelType.None && 
                _currentActivePanel != IReportsOps.PanelType.NavigationMenu)
            {
                HidePanel(_currentActivePanel);
            }

            if (_uiConfig.SubpanelsContainer.style.display != DisplayStyle.Flex)
            {
                _uiConfig.SubpanelsContainer.style.display = DisplayStyle.Flex;
            }

            panelData.Panel.AddToClassList(panelData.ShowClass);
            if (panelData.RequiresScrim)
            {
                _uiConfig.Scrim.AddToClassList(SCRIM_SHOW_CLASS);
            }

            _currentActivePanel = panelType;
            Debug.Log($"[ReportsUIManager] Panel {panelType} opened");
        }

        public void HidePanel(IReportsOps.PanelType panelType)
        {
            if (panelType == IReportsOps.PanelType.NavigationMenu)
            {
                HideNavigationMenu();
                return;
            }

            if (!_uiConfig.Panels.TryGetValue(panelType, out var panelData))
            {
                Debug.LogError($"Panel type {panelType} not found in configuration");
                return;
            }

            panelData.Panel.RemoveFromClassList(panelData.ShowClass);
            if (!HasOtherPanelsRequiringScrim(panelType))
            {
                _uiConfig.Scrim.RemoveFromClassList(SCRIM_SHOW_CLASS);
            }

            if (_currentActivePanel == panelType)
            {
                _currentActivePanel = IReportsOps.PanelType.None;
            }

            Debug.Log($"[ReportsUIManager] Panel {panelType} closed");
        }

        public void CloseCurrentPanel()
        {
            if (_currentActivePanel != IReportsOps.PanelType.None)
            {
                HidePanel(_currentActivePanel);
            }
        }

        public void SwitchPanel(IReportsOps.PanelType fromPanel, IReportsOps.PanelType toPanel)
        {
            if (fromPanel != IReportsOps.PanelType.None)
            {
                HidePanel(fromPanel);
            }
            
            if (toPanel != IReportsOps.PanelType.None)
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

        private bool HasOtherPanelsRequiringScrim(IReportsOps.PanelType excludePanel)
        {
            foreach (var kvp in _uiConfig.Panels)
            {
                if (kvp.Key == excludePanel) continue;
                var panelData = kvp.Value;
                if (panelData.Panel != null && panelData.RequiresScrim && 
                    panelData.Panel.ClassListContains(panelData.ShowClass))
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateReportStatusLabels(
            Label reportStatusLabel, Label totalReportsLabel, Label reportsTodayLabel,
            Label scheduledReportsLabel, Label storageUsedLabel, ProgressBar reportProgressBar,
            Label reportStatusMessage, ReportsInfo.ReportStatus status, int totalReports,
            int reportsToday, int scheduledReports, float storageUsedMB)
        {
            if (reportStatusLabel != null)
            {
                reportStatusLabel.text = GetStatusText(status);
                reportStatusLabel.RemoveFromClassList("status-ready");
                reportStatusLabel.RemoveFromClassList("status-generating");
                reportStatusLabel.RemoveFromClassList("status-processing");
                reportStatusLabel.RemoveFromClassList("status-exporting");
                reportStatusLabel.RemoveFromClassList("status-completed");
                reportStatusLabel.RemoveFromClassList("status-error");
                reportStatusLabel.AddToClassList($"status-{status.ToString().ToLower()}");
            }

            if (totalReportsLabel != null) totalReportsLabel.text = totalReports.ToString();
            if (reportsTodayLabel != null) reportsTodayLabel.text = reportsToday.ToString();
            if (scheduledReportsLabel != null) scheduledReportsLabel.text = scheduledReports.ToString();
            if (storageUsedLabel != null) storageUsedLabel.text = $"{storageUsedMB:F2} MB";

            if (reportProgressBar != null)
            {
                reportProgressBar.style.display = status == ReportsInfo.ReportStatus.Generating || 
                                                 status == ReportsInfo.ReportStatus.Processing || 
                                                 status == ReportsInfo.ReportStatus.Exporting 
                                                 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (reportStatusMessage != null)
            {
                reportStatusMessage.style.display = status == ReportsInfo.ReportStatus.Error ? DisplayStyle.Flex : DisplayStyle.None;
                reportStatusMessage.text = status == ReportsInfo.ReportStatus.Error ? "Error generating report" : "";
            }
        }

        private string GetStatusText(ReportsInfo.ReportStatus status)
        {
            return status switch
            {
                ReportsInfo.ReportStatus.Ready => "Ready",
                ReportsInfo.ReportStatus.Generating => "Generating...",
                ReportsInfo.ReportStatus.Processing => "Processing...",
                ReportsInfo.ReportStatus.Exporting => "Exporting...",
                ReportsInfo.ReportStatus.Completed => "Completed",
                ReportsInfo.ReportStatus.Error => "Error",
                _ => "Unknown"
            };
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
            _currentActivePanel = IReportsOps.PanelType.None;
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
                    if (kvp.Key != IReportsOps.PanelType.NavigationMenu)
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
                Debug.Log("[ReportsUIManager] All panels closed - hiding container");
            }
        }

        public IReportsOps.PanelType CurrentActivePanel => _currentActivePanel;
        public bool NavigationMenuOpen => _isNavigationMenuOpen;
    }
}