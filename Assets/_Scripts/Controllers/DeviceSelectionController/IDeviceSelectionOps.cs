using System;
using System.Threading.Tasks;

namespace _Scripts.Controllers.DeviceSelectionController
{
    /// <summary>
    /// Interface que define las operaciones disponibles en Device Selection
    /// Maneja la selección de dispositivos para Training u Operations
    /// </summary>
    public interface IDeviceSelectionOps
    {
        #region Device Management
        
        /// <summary>
        /// Lanza un dispositivo específico en el contexto apropiado
        /// </summary>
        /// <param name="deviceId">ID del dispositivo (ARSCARA, RobotKit1, RobotKit2)</param>
        /// <returns>True si el lanzamiento fue exitoso</returns>
        Task<bool> LaunchDeviceAsync(string deviceId);
        
        /// <summary>
        /// Obtiene información detallada de un dispositivo
        /// </summary>
        /// <param name="deviceId">ID del dispositivo</param>
        /// <returns>Información del dispositivo o null si no existe</returns>
        DeviceSelectionInfo.DeviceInfo GetDeviceInfo(string deviceId);
        
        /// <summary>
        /// Obtiene lista de todos los dispositivos disponibles
        /// </summary>
        /// <returns>Lista de dispositivos disponibles</returns>
        DeviceSelectionInfo.DeviceInfo[] GetAvailableDevices();
        
        /// <summary>
        /// Verifica si un dispositivo está disponible para el contexto actual
        /// </summary>
        /// <param name="deviceId">ID del dispositivo</param>
        /// <returns>True si está disponible</returns>
        bool IsDeviceAvailable(string deviceId);
        
        #endregion
        
        #region Context Management
        
        /// <summary>
        /// Configura el contexto de lanzamiento (Training/Operations)
        /// </summary>
        /// <param name="context">Contexto de lanzamiento</param>
        /// <param name="sourceController">Controlador origen</param>
        void SetLaunchContext(LaunchContext context, string sourceController = null);
        
        /// <summary>
        /// Obtiene el contexto actual de lanzamiento
        /// </summary>
        /// <returns>Contexto actual</returns>
        LaunchContext GetCurrentContext();
        
        /// <summary>
        /// Obtiene el título apropiado para el contexto actual
        /// </summary>
        /// <returns>Título contextual</returns>
        string GetContextualTitle();
        
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
        
        /// <summary>
        /// Contexto actual de lanzamiento
        /// </summary>
        LaunchContext CurrentContext { get; }
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Se dispara cuando se selecciona un dispositivo
        /// </summary>
        event Action<string, LaunchContext> OnDeviceSelected;
        
        /// <summary>
        /// Se dispara cuando se lanza un dispositivo exitosamente
        /// </summary>
        event Action<string, LaunchContext> OnDeviceLaunched;
        
        /// <summary>
        /// Se dispara cuando falla el lanzamiento de un dispositivo
        /// </summary>
        event Action<string, string> OnDeviceLaunchFailed;
        
        /// <summary>
        /// Se dispara cuando se abre/cierra el menú de navegación
        /// </summary>
        event Action<bool> OnNavigationMenuToggled;
        
        /// <summary>
        /// Se dispara cuando se cambia el contexto de lanzamiento
        /// </summary>
        event Action<LaunchContext, LaunchContext> OnContextChanged; // (from, to)
        
        #endregion
        
        /// <summary>
        /// Contexto de lanzamiento para dispositivos
        /// </summary>
        public enum LaunchContext
        {
            None,
            Training,    // Dispositivos para entrenamiento/aprendizaje
            Operations   // Dispositivos para operaciones industriales
        }
        
        /// <summary>
        /// Tipos de paneles disponibles en Device Selection
        /// </summary>
        public enum PanelType
        {
            None,
            NavigationMenu,    // Menú lateral de navegación
            DeviceInfo,        // Panel de información detallada del dispositivo
            ContextInfo,       // Panel de información del contexto actual
            Settings,          // Panel de configuraciones rápidas
            Help              // Panel de ayuda
        }
    }
}