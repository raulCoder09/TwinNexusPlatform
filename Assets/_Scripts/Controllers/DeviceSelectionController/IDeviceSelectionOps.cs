using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _Scripts.Controllers.UiManagement;
using _Scripts.Controllers;

namespace _Scripts.Controllers.DeviceSelectionController
{
    /// <summary>
    /// Interface específica para el controlador de Device Selection
    /// Hereda de IUIController y agrega funcionalidad específica de selección de dispositivos
    /// Define el contrato para gestión de dispositivos, contexto de navegación y lanzamiento de escenas
    /// </summary>
    public interface IDeviceSelectionOps : IUIController
    {
        #region Device Selection Specific Properties

        /// <summary>
        /// Contexto de navegación actual (Training, Operations, Reports, etc.)
        /// </summary>
        NavigationContext CurrentNavigationContext { get; }

        /// <summary>
        /// Modo de dispositivo actual basado en el contexto
        /// </summary>
        DeviceMode CurrentDeviceMode { get; }

        /// <summary>
        /// Dispositivo actualmente seleccionado
        /// </summary>
        string SelectedDevice { get; }

        /// <summary>
        /// Indica si el menú de navegación está visible
        /// </summary>
        bool IsNavigationMenuVisible { get; }

        /// <summary>
        /// Usuario actualmente autenticado
        /// </summary>
        string CurrentUsername { get; }

        /// <summary>
        /// Título de la UI según el contexto actual
        /// </summary>
        string CurrentUITitle { get; }

        #endregion

        #region Device Management

        /// <summary>
        /// Obtiene todos los dispositivos disponibles en el sistema
        /// </summary>
        /// <returns>Lista de información de dispositivos</returns>
        List<DeviceSelectionInfo.DeviceInfo> GetAllDevices();

        /// <summary>
        /// Obtiene dispositivos disponibles para el modo actual
        /// </summary>
        /// <returns>Lista de dispositivos disponibles para el contexto actual</returns>
        List<DeviceSelectionInfo.DeviceInfo> GetAvailableDevicesForCurrentMode();

        /// <summary>
        /// Obtiene dispositivos disponibles para un modo específico
        /// </summary>
        /// <param name="mode">Modo de dispositivo</param>
        /// <returns>Lista de dispositivos disponibles para el modo especificado</returns>
        List<DeviceSelectionInfo.DeviceInfo> GetAvailableDevicesForMode(DeviceMode mode);

        /// <summary>
        /// Selecciona un dispositivo específico
        /// </summary>
        /// <param name="deviceId">ID del dispositivo a seleccionar</param>
        /// <returns>True si la selección fue exitosa</returns>
        bool SelectDevice(string deviceId);

        /// <summary>
        /// Verifica si un dispositivo está disponible para selección en el contexto actual
        /// </summary>
        /// <param name="deviceId">ID del dispositivo</param>
        /// <returns>True si está disponible</returns>
        bool IsDeviceAvailable(string deviceId);

        /// <summary>
        /// Verifica si un dispositivo soporta un modo específico
        /// </summary>
        /// <param name="deviceId">ID del dispositivo</param>
        /// <param name="mode">Modo a verificar</param>
        /// <returns>True si el dispositivo soporta el modo</returns>
        bool IsDeviceSupportedForMode(string deviceId, DeviceMode mode);

        /// <summary>
        /// Actualiza el estado de disponibilidad de los dispositivos
        /// </summary>
        /// <returns>Task que representa la operación asíncrona</returns>
        Task RefreshDeviceAvailabilityAsync();

        #endregion

        #region Context and Navigation Management

        /// <summary>
        /// Configura el contexto de Device Selection
        /// </summary>
        /// <param name="context">Contexto de navegación</param>
        /// <param name="contextData">Datos adicionales del contexto</param>
        void ConfigureContext(NavigationContext context, Dictionary<string, object> contextData = null);

        /// <summary>
        /// Actualiza el contexto desde NavigationContextManager
        /// </summary>
        void UpdateContextFromNavigationManager();

        /// <summary>
        /// Obtiene el título de UI apropiado para un contexto
        /// </summary>
        /// <param name="context">Contexto de navegación</param>
        /// <returns>Título de UI</returns>
        string GetUITitleForContext(NavigationContext context);

        /// <summary>
        /// Obtiene la escena de destino para un contexto
        /// </summary>
        /// <param name="context">Contexto de navegación</param>
        /// <returns>Nombre de la escena de destino</returns>
        string GetTargetSceneForContext(NavigationContext context);

        /// <summary>
        /// Verifica si el contexto actual es válido
        /// </summary>
        /// <returns>True si el contexto está configurado correctamente</returns>
        bool IsContextValid();

        #endregion

        #region Navigation Menu Management

        /// <summary>
        /// Muestra u oculta el menú de navegación
        /// </summary>
        /// <param name="show">True para mostrar, false para ocultar</param>
        void ToggleNavigationMenu(bool show);

        /// <summary>
        /// Alterna la visibilidad del menú de navegación
        /// </summary>
        void ToggleNavigationMenu();

        /// <summary>
        /// Navega a una sección específica desde el menú
        /// </summary>
        /// <param name="targetContext">Contexto de destino</param>
        /// <returns>Task que representa la operación de navegación</returns>
        Task NavigateToSectionAsync(NavigationContext targetContext);

        /// <summary>
        /// Navega de regreso al Dashboard
        /// </summary>
        /// <returns>Task que representa la operación de navegación</returns>
        Task NavigateToDashboardAsync();

        /// <summary>
        /// Cierra sesión del usuario actual
        /// </summary>
        /// <returns>Task que representa la operación de logout</returns>
        Task LogoutAsync();

        #endregion

        #region Device Launch and Scene Management

        /// <summary>
        /// Lanza el dispositivo seleccionado en la escena apropiada
        /// </summary>
        /// <returns>Task que representa el lanzamiento del dispositivo</returns>
        Task LaunchSelectedDeviceAsync();

        /// <summary>
        /// Lanza un dispositivo específico en una escena específica
        /// </summary>
        /// <param name="deviceId">ID del dispositivo</param>
        /// <param name="targetScene">Escena de destino</param>
        /// <returns>Task que representa el lanzamiento del dispositivo</returns>
        Task LaunchDeviceAsync(string deviceId, string targetScene);

        /// <summary>
        /// Prepara los datos para el lanzamiento del dispositivo
        /// </summary>
        /// <param name="deviceId">ID del dispositivo</param>
        /// <returns>Datos preparados para el lanzamiento</returns>
        Dictionary<string, object> PrepareDeviceLaunchData(string deviceId);

        /// <summary>
        /// Valida que el dispositivo puede ser lanzado en el contexto actual
        /// </summary>
        /// <param name="deviceId">ID del dispositivo</param>
        /// <returns>True si el dispositivo puede ser lanzado</returns>
        bool ValidateDeviceLaunch(string deviceId);

        #endregion

        #region User Session Management

        /// <summary>
        /// Obtiene el historial de dispositivos recientemente seleccionados
        /// </summary>
        /// <returns>Lista de IDs de dispositivos recientes</returns>
        List<string> GetRecentlySelectedDevices();

        /// <summary>
        /// Agrega un dispositivo al historial de selecciones recientes
        /// </summary>
        /// <param name="deviceId">ID del dispositivo</param>
        void AddToRecentDevices(string deviceId);

        /// <summary>
        /// Limpia el historial de dispositivos recientes
        /// </summary>
        void ClearRecentDevices();

        /// <summary>
        /// Obtiene estadísticas de la sesión actual
        /// </summary>
        /// <returns>Información de la sesión</returns>
        DeviceSelectionSessionStats GetSessionStats();

        #endregion

        #region Device Selection Events

        /// <summary>
        /// Se dispara cuando se selecciona un dispositivo
        /// </summary>
        event Action<string, DeviceMode> OnDeviceSelected;

        /// <summary>
        /// Se dispara cuando un dispositivo es lanzado exitosamente
        /// </summary>
        event Action<string, string> OnDeviceLaunched; // deviceId, sceneName

        /// <summary>
        /// Se dispara cuando cambia el contexto de navegación
        /// </summary>
        event Action<NavigationContext, NavigationContext> OnContextChanged; // from, to

        /// <summary>
        /// Se dispara cuando cambia la disponibilidad de dispositivos
        /// </summary>
        event Action<List<DeviceSelectionInfo.DeviceInfo>> OnDeviceAvailabilityChanged;

        /// <summary>
        /// Se dispara cuando se actualiza el menú de navegación
        /// </summary>
        event Action<bool> OnNavigationMenuToggled; // isVisible

        /// <summary>
        /// Se dispara cuando ocurre un error en la selección de dispositivos
        /// </summary>
        event Action<string, string> OnDeviceSelectionError; // operation, error

        /// <summary>
        /// Se dispara antes de lanzar un dispositivo (permite validación)
        /// </summary>
        event Func<string, string, bool> OnDeviceLaunchValidation; // deviceId, sceneName, return canLaunch

        #endregion

        #region Utility Methods

        /// <summary>
        /// Obtiene información detallada de un dispositivo específico
        /// </summary>
        /// <param name="deviceId">ID del dispositivo</param>
        /// <returns>Información del dispositivo o null si no existe</returns>
        DeviceSelectionInfo.DeviceInfo GetDeviceInfo(string deviceId);

        /// <summary>
        /// Obtiene el estado actual completo del sistema Device Selection
        /// </summary>
        /// <returns>Estado actual del sistema</returns>
        DeviceSelectionStatus GetCurrentStatus();

        /// <summary>
        /// Valida la configuración actual del sistema
        /// </summary>
        /// <returns>Resultado de la validación</returns>
        ValidationResult ValidateConfiguration();

        /// <summary>
        /// Reinicia el sistema Device Selection al estado inicial
        /// </summary>
        void ResetToInitialState();

        /// <summary>
        /// Fuerza una actualización completa de la UI
        /// </summary>
        void ForceUIRefresh();

        #endregion
    }

    #region Supporting Classes and Enums

    /// <summary>
    /// Estadísticas de la sesión de Device Selection
    /// </summary>
    [Serializable]
    public class DeviceSelectionSessionStats
    {
        public DateTime SessionStart { get; set; }
        public TimeSpan SessionDuration { get; set; }
        public int DevicesViewed { get; set; }
        public int DevicesSelected { get; set; }
        public int NavigationMenuToggles { get; set; }
        public List<string> ContextHistory { get; set; } = new List<string>();
        public string MostSelectedDevice { get; set; }
        public NavigationContext CurrentContext { get; set; }
        
        public override string ToString()
        {
            return $"Session: {SessionDuration.TotalMinutes:F1}min, " +
                   $"Devices: {DevicesSelected}/{DevicesViewed}, " +
                   $"Context: {CurrentContext}";
        }
    }

    /// <summary>
    /// Estado actual del sistema Device Selection
    /// </summary>
    [Serializable]
    public class DeviceSelectionStatus
    {
        public bool IsInitialized { get; set; }
        public bool IsVisible { get; set; }
        public NavigationContext CurrentContext { get; set; }
        public DeviceMode CurrentMode { get; set; }
        public string SelectedDevice { get; set; }
        public int AvailableDevicesCount { get; set; }
        public int TotalDevicesCount { get; set; }
        public bool IsMenuVisible { get; set; }
        public bool IsContextValid { get; set; }
        public DateTime LastUpdate { get; set; }
        public string LastError { get; set; }
        
        public override string ToString()
        {
            return $"DeviceSelection - Init: {IsInitialized}, " +
                   $"Context: {CurrentContext}, Mode: {CurrentMode}, " +
                   $"Selected: {SelectedDevice}, Available: {AvailableDevicesCount}/{TotalDevicesCount}";
        }
    }

    /// <summary>
    /// Resultado de validación de configuración
    /// </summary>
    [Serializable]
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public string Summary { get; set; }
        
        public ValidationResult(bool isValid = true)
        {
            IsValid = isValid;
        }
        
        /// <summary>
        /// Agrega una advertencia
        /// </summary>
        /// <param name="warning">Mensaje de advertencia</param>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
        
        /// <summary>
        /// Agrega un error (marca como inválido)
        /// </summary>
        /// <param name="error">Mensaje de error</param>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }
        
        /// <summary>
        /// Genera un resumen del resultado
        /// </summary>
        public void GenerateSummary()
        {
            if (IsValid && Errors.Count == 0 && Warnings.Count == 0)
            {
                Summary = "Configuration is valid with no issues.";
            }
            else
            {
                Summary = $"Validation result: {(IsValid ? "VALID" : "INVALID")} - " +
                         $"{Errors.Count} errors, {Warnings.Count} warnings";
            }
        }
        
        public override string ToString()
        {
            return Summary ?? $"Valid: {IsValid}, Errors: {Errors.Count}, Warnings: {Warnings.Count}";
        }
    }

    #endregion
}