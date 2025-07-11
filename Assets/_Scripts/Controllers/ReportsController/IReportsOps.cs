using System;

namespace _Scripts.Controllers.ReportsController
{
    public interface IReportsOps
    {
        void NavigateToPanel(PanelType panelType);
        void CloseCurrentPanel();
        void SwitchPanel(PanelType fromPanel, PanelType toPanel);
        void ShowNavigationMenu();
        void HideNavigationMenu();
        void GenerateSystemReport();
        void GenerateActivityReport();
        void GenerateIoTReport();
        void CreateCustomReport();
        void ExportLastReport();
        void ScheduleReport();
        void ViewReportHistory();
        void HandleDashboardClick(); // Agregado
        void HandleOperationsClick(); // Agregado
        void HandleTrainingClick(); // Agregado
        void HandleSupportClick(); // Agregado
        void HandleSettingsClick(); // Agregado
        void HandleLogoutClick(); // Agregado

        bool IsNavigationMenuOpen { get; }
        PanelType CurrentActivePanel { get; }

        event Action OnNavigationMenuOpened;
        event Action OnNavigationMenuClosed;
        event Action<PanelType> OnPanelTransitionComplete;
        event Action OnLogoutRequested;

        public enum PanelType
        {
            None,
            NavigationMenu,
            CustomReportEditor,
            ScheduleReport,
            ReportHistory
        }
    }
}