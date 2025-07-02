using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.DashboardController
{
    /// <summary>
    /// Interface específica para el controlador del Dashboard
    /// Hereda de IUIController y agrega funcionalidad específica del Dashboard
    /// </summary>
    public interface IDashboardOps : IUIController
    {
        #region Dashboard Specific Properties

        /// <summary>
        /// Usuario actualmente logueado en el dashboard
        /// </summary>
        string CurrentUsername { get; }

        /// <summary>
        /// Indica si el menu de navegación está actualmente visible
        /// </summary>
        bool IsNavigationMenuVisible { get; }

        /// <summary>
        /// Dispositivo actualmente seleccionado para control
        /// </summary>
        string SelectedDevice { get; }

        #endregion

        #region Device Management

        /// <summary>
        /// Actualiza el estado de todos los dispositivos del sistema
        /// </summary>
        /// <returns>True si la actualización fue exitosa</returns>
        Task<bool> RefreshDeviceStatusAsync();

        /// <summary>
        /// Selecciona un dispositivo específico para control
        /// </summary>
        /// <param name="deviceName">Nombre del dispositivo a seleccionar</param>
        void SelectDevice(string deviceName);

        /// <summary>
        /// Obtiene la lista de dispositivos disponibles
        /// </summary>
        /// <returns>Lista de nombres de dispositivos</returns>
        List<string> GetAvailableDevices();

        /// <summary>
        /// Obtiene el estado actual de un dispositivo específico
        /// </summary>
        /// <param name="deviceName">Nombre del dispositivo</param>
        /// <returns>Estado del dispositivo (Online, Offline, Busy, etc.)</returns>
        DeviceStatus GetDeviceStatus(string deviceName);

        #endregion

        #region IoT Infrastructure

        /// <summary>
        /// Actualiza el estado de la infraestructura IoT
        /// </summary>
        Task RefreshIoTInfrastructureAsync();

        /// <summary>
        /// Obtiene el estado de conexión de los servicios IoT
        /// </summary>
        /// <returns>Dictionary con el estado de cada servicio IoT</returns>
        Dictionary<IoTService, ConnectionStatus> GetIoTServicesStatus();

        /// <summary>
        /// Cambia el modo de operación de un servicio IoT
        /// </summary>
        /// <param name="service">Servicio IoT a modificar</param>
        /// <param name="mode">Nuevo modo de operación</param>
        Task<bool> SetIoTServiceModeAsync(IoTService service, OperationMode mode);

        #endregion

        #region System Activity

        /// <summary>
        /// Obtiene la actividad reciente del sistema
        /// </summary>
        /// <param name="maxItems">Número máximo de elementos a retornar</param>
        /// <returns>Lista de actividades recientes</returns>
        List<SystemActivity> GetRecentActivity(int maxItems = 10);

        /// <summary>
        /// Agrega una nueva actividad al historial
        /// </summary>
        /// <param name="activity">Actividad a registrar</param>
        void LogActivity(SystemActivity activity);

        /// <summary>
        /// Limpia el historial de actividades
        /// </summary>
        void ClearActivityHistory();

        #endregion

        #region Navigation

        /// <summary>
        /// Muestra u oculta el menu de navegación
        /// </summary>
        /// <param name="show">True para mostrar, false para ocultar</param>
        void ToggleNavigationMenu(bool show);

        /// <summary>
        /// Navega a una sección específica del dashboard
        /// </summary>
        /// <param name="section">Sección a la que navegar</param>
        void NavigateToSection(DashboardSection section);

        /// <summary>
        /// Cierra sesión del usuario actual
        /// </summary>
        Task LogoutAsync();

        #endregion

        #region Dashboard Specific Events

        /// <summary>
        /// Se dispara cuando cambia el estado de un dispositivo
        /// </summary>
        event Action<string, DeviceStatus> OnDeviceStatusChanged;

        /// <summary>
        /// Se dispara cuando se selecciona un dispositivo diferente
        /// </summary>
        event Action<string> OnDeviceSelected;

        /// <summary>
        /// Se dispara cuando cambia el estado de la infraestructura IoT
        /// </summary>
        event Action<IoTService, ConnectionStatus> OnIoTServiceStatusChanged;

        /// <summary>
        /// Se dispara cuando se registra una nueva actividad
        /// </summary>
        event Action<SystemActivity> OnNewActivityLogged;

        /// <summary>
        /// Se dispara cuando el usuario navega a una sección diferente
        /// </summary>
        event Action<DashboardSection> OnSectionNavigated;

        /// <summary>
        /// Se dispara cuando el usuario cierra sesión
        /// </summary>
        event Action OnUserLoggedOut;

        #endregion
    }

    #region Supporting Enums and Classes

    /// <summary>
    /// Estados posibles de un dispositivo
    /// </summary>
    public enum DeviceStatus
    {
        Unknown,
        Online,
        Offline,
        Busy,
        Alarmed,
        Maintenance
    }

    /// <summary>
    /// Servicios IoT disponibles en el sistema
    /// </summary>
    public enum IoTService
    {
        LocalIoT,
        VMIoT,
        AWSIoTCore
    }

    /// <summary>
    /// Estados de conexión de servicios
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    /// <summary>
    /// Modos de operación para servicios IoT
    /// </summary>
    public enum OperationMode
    {
        Manual,
        Automatic,
        Maintenance
    }

    /// <summary>
    /// Secciones navegables del dashboard
    /// </summary>
    public enum DashboardSection
    {
        Main,
        Training,
        Operations,
        Reports,
        Support,
        Settings
    }

    /// <summary>
    /// Representa una actividad del sistema
    /// </summary>
    [Serializable]
    public class SystemActivity
    {
        public DateTime Timestamp { get; set; }
        public string DeviceName { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public ActivityType Type { get; set; }

        public SystemActivity(string deviceName, string action, string description, ActivityType type = ActivityType.Info)
        {
            Timestamp = DateTime.Now;
            DeviceName = deviceName;
            Action = action;
            Description = description;
            Type = type;
        }

        public override string ToString()
        {
            return $"{DeviceName} {Action} at {Timestamp:HH:mm}";
        }
    }

    /// <summary>
    /// Tipos de actividad del sistema
    /// </summary>
    public enum ActivityType
    {
        Info,
        Warning,
        Error,
        Success
    }

    #endregion
}