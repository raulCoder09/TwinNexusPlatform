using System;
using System.Threading.Tasks;

namespace _Scripts.Controllers.SettingsController
{
    /// <summary>
    /// Interface que define las operaciones disponibles en Settings
    /// Maneja configuraciones del sistema y navegación
    /// </summary>
    public interface ISettingsOps
    {
        #region Configuration Management
        
        /// <summary>
        /// Abre configuración de IoT
        /// </summary>
        void OpenIoTConfiguration();
        
        /// <summary>
        /// Abre configuración de usuario
        /// </summary>
        void OpenUserConfiguration();
        
        /// <summary>
        /// Abre configuración de Cognito
        /// </summary>
        void OpenCognitoConfiguration();
        
        /// <summary>
        /// Abre configuración del sistema
        /// </summary>
        void OpenSystemConfiguration();
        
        /// <summary>
        /// Abre configuración de red
        /// </summary>
        void OpenNetworkConfiguration();
        
        #endregion
        
        #region Navigation
        
        /// <summary>
        /// Navega al panel especificado
        /// </summary>
        /// <param name="panelType">Tipo de panel</param>
        void NavigateToPanel(PanelType panelType);
        
        /// <summary>
        /// Cierra el panel actual
        /// </summary>
        void CloseCurrentPanel();
        
        /// <summary>
        /// Regresa al Dashboard
        /// </summary>
        void ReturnToDashboard();
        
        /// <summary>
        /// Muestra/oculta el menú de navegación
        /// </summary>
        void ToggleNavigationMenu();
        
        /// <summary>
        /// Ejecuta logout del usuario
        /// </summary>
        void Logout();
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Indica si el menú de navegación está abierto
        /// </summary>
        bool IsNavigationMenuOpen { get; }
        
        /// <summary>
        /// Panel actualmente activo
        /// </summary>
        PanelType CurrentActivePanel { get; }
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Se dispara cuando se abre una configuración específica
        /// </summary>
        event Action<ConfigurationType> OnConfigurationOpened;
        
        /// <summary>
        /// Se dispara cuando se abre/cierra el menú de navegación
        /// </summary>
        event Action<bool> OnNavigationMenuToggled;
        
        /// <summary>
        /// Se dispara cuando se solicita logout
        /// </summary>
        event Action OnLogoutRequested;
        
        #endregion
        
        /// <summary>
        /// Tipos de configuraciones disponibles
        /// </summary>
        public enum ConfigurationType
        {
            IoT,
            User,
            Cognito,
            System,
            Network,
            Database,
            Security,
            Backup,
            AwsServices
        }
        
        /// <summary>
        /// Tipos de paneles disponibles en Settings
        /// </summary>
        public enum PanelType
        {
            None,
            NavigationMenu,    // Menú lateral de navegación
            IoTConfig,         // Panel de configuración IoT
            UserConfig,        // Panel de configuración de usuario
            CognitoConfig,     // Panel de configuración Cognito
            SystemConfig,      // Panel de configuración del sistema
            NetworkConfig,     // Panel de configuración de red
            Help              // Panel de ayuda
        }
    }
}