using System;
using System.Collections.Generic;

namespace _Scripts.Controllers.ArScaraRobotController
{
    /// <summary>
    /// Interface para el controlador de sistema del robot ARSCARA
    /// Define el contrato para manejo de estados, seguridad y operaciones del sistema
    /// </summary>
    public interface ISystemController
    {
        #region Properties
        /// <summary>
        /// Indica si el controlador está inicializado
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Estado actual de los motores (habilitados/deshabilitados)
        /// </summary>
        bool MotorsEnabled { get; }
        
        /// <summary>
        /// Nivel de potencia actual del sistema
        /// </summary>
        PowerLevel CurrentPowerLevel { get; }
        
        /// <summary>
        /// Estado del Emergency Stop
        /// </summary>
        bool EmergencyStopActive { get; }
        
        /// <summary>
        /// Estado del Safeguard (puerta de seguridad)
        /// </summary>
        bool SafeguardActive { get; }
        
        /// <summary>
        /// Indica si hay alarmas activas en el sistema
        /// </summary>
        bool HasAlarms { get; }
        
        /// <summary>
        /// Indica si el sistema está activo y puede ejecutar movimientos
        /// </summary>
        bool IsSystemActive { get; }
        
        /// <summary>
        /// Multiplicador de velocidad actual basado en el nivel de potencia
        /// </summary>
        float CurrentSpeedMultiplier { get; }
        
        /// <summary>
        /// Lista de alarmas activas
        /// </summary>
        IReadOnlyList<SystemAlarm> ActiveAlarms { get; }
        #endregion

        #region Initialization
        /// <summary>
        /// Inicializa el controlador de sistema
        /// </summary>
        /// <param name="robotManager">Referencia al robot manager</param>
        /// <returns>True si la inicialización fue exitosa</returns>
        bool Initialize(IRobotManager robotManager);
        
        /// <summary>
        /// Limpia recursos del controlador
        /// </summary>
        void Cleanup();
        #endregion

        #region Motor Control
        /// <summary>
        /// Habilita o deshabilita los motores del robot
        /// </summary>
        /// <param name="enabled">True para habilitar motores</param>
        void SetMotorsEnabled(bool enabled);
        
        /// <summary>
        /// Establece el nivel de potencia del robot
        /// </summary>
        /// <param name="level">Nivel de potencia a establecer</param>
        void SetPowerLevel(PowerLevel level);
        
        /// <summary>
        /// Obtiene los niveles de potencia disponibles
        /// </summary>
        /// <returns>Array con niveles de potencia soportados</returns>
        PowerLevel[] GetAvailablePowerLevels();
        
        /// <summary>
        /// Obtiene el multiplicador de velocidad para un nivel de potencia específico
        /// </summary>
        /// <param name="level">Nivel de potencia</param>
        /// <returns>Multiplicador de velocidad</returns>
        float GetSpeedMultiplierForPowerLevel(PowerLevel level);
        #endregion

        #region Safety Control
        /// <summary>
        /// Establece el estado del Emergency Stop
        /// </summary>
        /// <param name="active">True para activar Emergency Stop</param>
        void SetEmergencyStop(bool active);
        
        /// <summary>
        /// Establece el estado del Safeguard
        /// </summary>
        /// <param name="active">True para activar Safeguard</param>
        void SetSafeguard(bool active);
        
        /// <summary>
        /// Verifica si es seguro ejecutar movimientos
        /// </summary>
        /// <returns>True si es seguro mover el robot</returns>
        bool IsSafeToMove();
        
        /// <summary>
        /// Ejecuta paro de emergencia inmediato
        /// </summary>
        void ExecuteEmergencyStop();
        
        /// <summary>
        /// Verifica el estado de todos los sistemas de seguridad
        /// </summary>
        /// <returns>Resultado de la verificación de seguridad</returns>
        SafetyCheckResult PerformSafetyCheck();
        #endregion

        #region Alarm Management
        /// <summary>
        /// Dispara una nueva alarma en el sistema
        /// </summary>
        /// <param name="alarmType">Tipo de alarma</param>
        /// <param name="message">Mensaje descriptivo</param>
        /// <param name="severity">Severidad de la alarma</param>
        void TriggerAlarm(AlarmType alarmType, string message, AlarmSeverity severity = AlarmSeverity.Warning);
        
        /// <summary>
        /// Limpia una alarma específica
        /// </summary>
        /// <param name="alarmId">ID de la alarma a limpiar</param>
        /// <returns>True si la alarma fue limpiada exitosamente</returns>
        bool ClearAlarm(int alarmId);
        
        /// <summary>
        /// Limpia todas las alarmas del sistema
        /// </summary>
        void ClearAllAlarms();
        
        /// <summary>
        /// Obtiene alarmas por tipo
        /// </summary>
        /// <param name="alarmType">Tipo de alarma a buscar</param>
        /// <returns>Lista de alarmas del tipo especificado</returns>
        IReadOnlyList<SystemAlarm> GetAlarmsByType(AlarmType alarmType);
        
        /// <summary>
        /// Obtiene alarmas por severidad
        /// </summary>
        /// <param name="severity">Severidad a buscar</param>
        /// <returns>Lista de alarmas con la severidad especificada</returns>
        IReadOnlyList<SystemAlarm> GetAlarmsBySeverity(AlarmSeverity severity);
        #endregion

        #region System Operations
        /// <summary>
        /// Ejecuta secuencia de Home del robot
        /// </summary>
        /// <returns>True si la operación se inició correctamente</returns>
        bool ExecuteHome();
        
        /// <summary>
        /// Ejecuta reset completo del sistema
        /// </summary>
        void ExecuteSystemReset();
        
        /// <summary>
        /// Ejecuta shutdown seguro del sistema
        /// </summary>
        void ExecuteShutdown();
        
        /// <summary>
        /// Habilita todos los joints del robot
        /// </summary>
        void EnableAllJoints();
        
        /// <summary>
        /// Deshabilita todos los joints del robot (Lock All)
        /// </summary>
        void LockAllJoints();
        
        /// <summary>
        /// Verifica si una operación específica es permitida
        /// </summary>
        /// <param name="operation">Tipo de operación</param>
        /// <returns>True si la operación es permitida</returns>
        bool IsOperationAllowed(SystemOperation operation);
        #endregion

        #region State Monitoring
        /// <summary>
        /// Actualiza el estado del sistema (llamado desde Update loop)
        /// </summary>
        void UpdateSystemState();
        
        /// <summary>
        /// Obtiene el estado completo del sistema
        /// </summary>
        /// <returns>Estado actual del sistema</returns>
        SystemState GetSystemState();
        
        /// <summary>
        /// Obtiene información de diagnóstico del sistema
        /// </summary>
        /// <returns>Información de diagnóstico</returns>
        SystemDiagnostics GetSystemDiagnostics();
        
        /// <summary>
        /// Verifica la salud general del sistema
        /// </summary>
        /// <returns>Estado de salud del sistema</returns>
        SystemHealth CheckSystemHealth();
        #endregion

        #region Configuration
        /// <summary>
        /// Establece configuración de seguridad
        /// </summary>
        /// <param name="config">Configuración de seguridad</param>
        void SetSafetyConfiguration(SafetyConfiguration config);
        
        /// <summary>
        /// Obtiene configuración de seguridad actual
        /// </summary>
        /// <returns>Configuración de seguridad</returns>
        SafetyConfiguration GetSafetyConfiguration();
        #endregion

        #region Events
        /// <summary>
        /// Se dispara cuando cambia el estado de los motores
        /// </summary>
        event Action<bool> OnMotorsEnabledChanged;
        
        /// <summary>
        /// Se dispara cuando cambia el nivel de potencia
        /// </summary>
        event Action<PowerLevel> OnPowerLevelChanged;
        
        /// <summary>
        /// Se dispara cuando cambia el estado del Emergency Stop
        /// </summary>
        event Action<bool> OnEmergencyStopChanged;
        
        /// <summary>
        /// Se dispara cuando cambia el estado del Safeguard
        /// </summary>
        event Action<bool> OnSafeguardChanged;
        
        /// <summary>
        /// Se dispara cuando cambia si el sistema está activo
        /// </summary>
        event Action<bool> OnSystemActiveChanged;
        
        /// <summary>
        /// Se dispara cuando se activa una nueva alarma
        /// </summary>
        event Action<SystemAlarm> OnAlarmTriggered;
        
        /// <summary>
        /// Se dispara cuando se limpia una alarma
        /// </summary>
        event Action<int> OnAlarmCleared;
        
        /// <summary>
        /// Se dispara cuando se limpian todas las alarmas
        /// </summary>
        event Action OnAllAlarmsCleared;
        
        /// <summary>
        /// Se dispara cuando se completa una operación del sistema
        /// </summary>
        event Action<SystemOperation, bool> OnSystemOperationCompleted;
        
        /// <summary>
        /// Se dispara cuando cambia el estado general del sistema
        /// </summary>
        event Action<SystemState> OnSystemStateChanged;
        #endregion
    }

    #region Supporting Data Types
    
    /// <summary>
    /// Tipos de alarmas del sistema
    /// </summary>
    public enum AlarmType
    {
        System,
        Safety,
        Motion,
        Communication,
        Hardware,
        Software,
        User
    }

    /// <summary>
    /// Severidad de las alarmas
    /// </summary>
    public enum AlarmSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Operaciones del sistema
    /// </summary>
    public enum SystemOperation
    {
        Home,
        Reset,
        Shutdown,
        MotorEnable,
        MotorDisable,
        JointMove,
        PowerChange,
        SafetyReset
    }

    /// <summary>
    /// Estado de salud del sistema
    /// </summary>
    public enum SystemHealth
    {
        Healthy,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Información de una alarma del sistema
    /// </summary>
    [System.Serializable]
    public struct SystemAlarm
    {
        public int AlarmId;
        public AlarmType Type;
        public AlarmSeverity Severity;
        public string Message;
        public DateTime Timestamp;
        public bool IsActive;
    }

    /// <summary>
    /// Estado completo del sistema
    /// </summary>
    [System.Serializable]
    public struct SystemState
    {
        public bool MotorsEnabled;
        public PowerLevel PowerLevel;
        public bool EmergencyStopActive;
        public bool SafeguardActive;
        public bool HasAlarms;
        public bool IsSystemActive;
        public SystemHealth Health;
        public DateTime LastUpdate;
    }

    /// <summary>
    /// Resultado de verificación de seguridad
    /// </summary>
    [System.Serializable]
    public struct SafetyCheckResult
    {
        public bool IsSafe;
        public string[] Issues;
        public AlarmSeverity HighestSeverity;
    }

    /// <summary>
    /// Información de diagnóstico del sistema
    /// </summary>
    [System.Serializable]
    public struct SystemDiagnostics
    {
        public float SystemUptime;
        public int TotalOperations;
        public int TotalAlarms;
        public SystemHealth CurrentHealth;
        public string[] ActiveSystems;
        public string[] FailedSystems;
    }

    /// <summary>
    /// Configuración de seguridad
    /// </summary>
    [System.Serializable]
    public struct SafetyConfiguration
    {
        public bool EnableEmergencyStop;
        public bool EnableSafeguard;
        public float SafetyTimeout;
        public bool RequireConfirmationForCriticalOps;
        public AlarmSeverity MinAlarmLevelForStop;
    }

    #endregion
}