using System;

namespace _Scripts.Controllers.ArScaraJogAndTeachController
{
    /// <summary>
    /// Interface que define las operaciones disponibles en AWS Settings
    /// Similar a IDashboardOps pero enfocado en configuraciones AWS
    /// </summary>
    public interface IArScaraJogAndTeachOps
    {
        // Navegación de paneles (futuros)
        void NavigateToPanel(PanelType panelType);
        void CloseCurrentPanel();
        void SwitchPanel(PanelType fromPanel, PanelType toPanel);
        
        // Operaciones específicas de AWS Settings
        void OpenAwsConfiguration();
        void SaveConfiguration();
        void ResetConfiguration();
        void TestConnection();
        void ShowNavigationMenu();
        void HideNavigationMenu();
        void ReturnToDashboard();
        
        // Estados
        bool IsNavigationMenuOpen { get; }
        PanelType CurrentActivePanel { get; }
        
        // Eventos
        event Action OnNavigationMenuOpened;
        event Action OnNavigationMenuClosed;
        event Action<PanelType> OnPanelTransitionComplete;
        event Action OnReturnToDashboardRequested;

        /// <summary>
        /// Tipos de paneles disponibles en AWS Settings (futuros)
        /// </summary>
        public enum PanelType
        {
            None,
            NavigationMenu,     // Menú lateral principal
            AwsCredentials,     // Panel de credenciales AWS
            ServiceConfig,      // Panel de configuración de servicios
            TestResults,        // Panel de resultados de pruebas
            SecuritySettings,   // Panel de configuraciones de seguridad
            RegionSettings,     // Panel de configuración de regiones
            Help                // Panel de ayuda
        }
    }
}