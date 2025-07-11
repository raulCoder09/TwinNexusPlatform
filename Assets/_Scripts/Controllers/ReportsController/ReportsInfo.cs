using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.ReportsController
{
    [System.Serializable]
    public class ReportsInfo
    {
        public class UIConfiguration
        {
            public VisualElement Body { get; set; }
            public VisualElement SubpanelsContainer { get; set; }
            public VisualElement Scrim { get; set; }
            public Dictionary<IReportsOps.PanelType, PanelData> Panels { get; set; } 
                = new Dictionary<IReportsOps.PanelType, PanelData>();
            public VisualElement NavigationMenuPanel { get; set; }
            public VisualElement MainContentArea { get; set; }

            [System.Serializable]
            public class PanelData
            {
                public VisualElement Panel { get; set; }
                public string ShowClass { get; set; }
                public string HideClass { get; set; }
                public bool IsModal { get; set; } = false;
                public bool RequiresScrim { get; set; } = true;
                public float AnimationDuration { get; set; } = 0.3f;
            }
        }

        public class UserData
        {
            public string Username { get; set; }
            public string UserGroup { get; set; }
            public string UserRole { get; set; }
            public bool IsAuthenticated { get; set; }
            public DateTime LastLoginTime { get; set; }
        }

        public class ReportsState
        {
            public bool IsInitialized { get; set; }
            public bool IsNavigationMenuOpen { get; set; }
            public IReportsOps.PanelType CurrentActivePanel { get; set; } = IReportsOps.PanelType.None;
            public string CurrentSection { get; set; } = "Reports";
            public ReportStatus CurrentReportStatus { get; set; } = ReportStatus.Ready;
            public int TotalReports { get; set; }
            public int ReportsToday { get; set; }
            public int ScheduledReports { get; set; }
            public float StorageUsedMB { get; set; }

            public event Action<ReportStatus> OnReportStatusChanged;
            public event Action<IReportsOps.PanelType> OnPanelChanged;
            public event Action<bool> OnNavigationMenuToggled;

            // Método para disparar el evento OnReportStatusChanged de forma segura
            public void RaiseReportStatusChanged(ReportStatus status)
            {
                OnReportStatusChanged?.Invoke(status);
            }
        }

        public enum ReportStatus
        {
            Ready,
            Generating,
            Processing,
            Exporting,
            Completed,
            Error
        }

        public class NavigationConfig
        {
            public List<NavigationItem> MenuItems { get; set; } = new List<NavigationItem>();

            public class NavigationItem
            {
                public string Name { get; set; }
                public string DisplayName { get; set; }
                public string IconClass { get; set; }
                public bool IsEnabled { get; set; } = true;
                public bool RequiresPermission { get; set; } = false;
                public string RequiredRole { get; set; }
                public Action OnClick { get; set; }
            }
        }
    }
}