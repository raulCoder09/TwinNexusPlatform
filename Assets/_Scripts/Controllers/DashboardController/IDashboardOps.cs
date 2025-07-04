using System;
using System.Threading.Tasks;

namespace _Scripts.Controllers.DashboardController
{
    /// <summary>
    /// Interface que define las operaciones disponibles en el Dashboard
    /// Similar a IWelcomeOps pero para navegación y acciones del dashboard
    /// </summary>
    public interface IDashboardOps
    {
        // Navegación de paneles
        void NavigateToPanel(PanelType panelType);
        void CloseCurrentPanel();
        void SwitchPanel(PanelType fromPanel, PanelType toPanel);
        
        // Operaciones específicas del Dashboard
        void StartOperations();
        void StartTraining();
        void OpenSettings();
        void Logout();
        void ShowNavigationMenu();
        void HideNavigationMenu();
        
        // Estados
        bool IsNavigationMenuOpen { get; }
        PanelType CurrentActivePanel { get; }
        
        // Eventos
        event Action OnNavigationMenuOpened;
        event Action OnNavigationMenuClosed;
        event Action<PanelType> OnPanelTransitionComplete;
        event Action OnLogoutRequested;

        /// <summary>
        /// Tipos de paneles disponibles en el Dashboard
        /// </summary>
        public enum PanelType
        {
            None,
            NavigationMenu,     // Menú lateral principal
            UserProfile,        // Panel de perfil de usuario
            Notifications,      // Panel de notificaciones
            QuickActions,       // Panel de acciones rápidas
            StatusOverlay,      // Panel de estado de IoT (Local, VM, Cloud)
            Settings,           // Panel de configuraciones rápidas
            Help                // Panel de ayuda
        }
    }
}