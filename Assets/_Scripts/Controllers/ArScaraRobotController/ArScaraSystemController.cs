using System;
using UnityEngine;

namespace _Scripts.Controllers.ArScaraRobotController
{
    /// <summary>
    /// Controlador de estados del sistema ARSCARA
    /// Maneja motores, potencia, seguridad y alarmas
    /// </summary>
    public class ArScaraSystemController : MonoBehaviour
    {
        #region Private Fields
        private ArScaraRobotManager _robotManager;
        private bool _isInitialized = false;
        
        // Estados del sistema
        private bool _motorsEnabled = false;
        private PowerLevel _currentPowerLevel = PowerLevel.Low;
        private bool _emergencyStopActive = false;
        private bool _safeguardActive = false;
        private bool _hasAlarms = false;
        private bool _isSystemActive = false;
        
        // Configuración de velocidades por nivel de potencia
        private readonly float[] _powerSpeedMultipliers = { 0.5f, 1.0f }; // Low, High
        
        // Simulación de estados de seguridad
        [Header("Safety Simulation")]
        [SerializeField] private bool _simulateEmergencyStop = false;
        [SerializeField] private bool _simulateSafeguard = false;
        #endregion

        #region Events
        public static event Action<bool> OnMotorsEnabledChanged;
        public static event Action<PowerLevel> OnPowerLevelChanged;
        public static event Action<bool> OnEmergencyStopChanged;
        public static event Action<bool> OnSafeguardChanged;
        public static event Action<bool> OnSystemActiveChanged;
        public static event Action<string> OnAlarmTriggered;
        public static event Action OnAlarmsCleared;
        #endregion

        #region Initialization
        /// <summary>
        /// Inicializa el controlador del sistema
        /// </summary>
        public void Initialize(ArScaraRobotManager robotManager)
        {
            _robotManager = robotManager ?? throw new ArgumentNullException(nameof(robotManager));
            
            try
            {
                // Configurar estado inicial seguro
                _motorsEnabled = false;
                _currentPowerLevel = PowerLevel.Low;
                _emergencyStopActive = false;
                _safeguardActive = false;
                _hasAlarms = false;
                
                // Actualizar estado del sistema
                UpdateSystemActiveState();
                
                _isInitialized = true;
                Debug.Log("[ArScaraSystemController] System controller initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArScaraSystemController] Initialization failed: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region Update Loop
        private void Update()
        {
            if (!_isInitialized) return;
            
            // Monitorear estados de seguridad simulados
            MonitorSafetyStates();
            
            // Actualizar estado general del sistema
            UpdateSystemActiveState();
        }

        /// <summary>
        /// Monitorea estados de seguridad (simulados)
        /// </summary>
        private void MonitorSafetyStates()
        {
            // Emergency Stop simulation
            if (_simulateEmergencyStop != _emergencyStopActive)
            {
                SetEmergencyStop(_simulateEmergencyStop);
            }
            
            // Safeguard simulation  
            if (_simulateSafeguard != _safeguardActive)
            {
                SetSafeguard(_simulateSafeguard);
            }
        }

        /// <summary>
        /// Actualiza si el sistema está activo y puede moverse
        /// </summary>
        private void UpdateSystemActiveState()
        {
            bool wasActive = _isSystemActive;
            
            // Sistema activo solo si: motores ON, no emergency stop, safeguard OK, sin alarmas
            _isSystemActive = _motorsEnabled && 
                             !_emergencyStopActive && 
                             !_safeguardActive && 
                             !_hasAlarms;
            
            // Notificar cambio
            if (wasActive != _isSystemActive)
            {
                OnSystemActiveChanged?.Invoke(_isSystemActive);
                
                if (!_isSystemActive)
                {
                    Debug.Log("[ArScaraSystemController] System deactivated - robot movement stopped");
                    // Parar movimiento del robot
                    _robotManager?.JointController?.StopAllJoints();
                }
                else
                {
                    Debug.Log("[ArScaraSystemController] System activated - robot ready for movement");
                }
            }
        }
        #endregion

        #region Motor Control
        /// <summary>
        /// Habilita/deshabilita los motores del robot
        /// </summary>
        public void SetMotorsEnabled(bool enabled)
        {
            if (!_isInitialized) return;
            
            bool wasEnabled = _motorsEnabled;
            _motorsEnabled = enabled;
            
            if (wasEnabled != enabled)
            {
                Debug.Log($"[ArScaraSystemController] Motors {(enabled ? "enabled" : "disabled")}");
                OnMotorsEnabledChanged?.Invoke(enabled);
                
                if (!enabled)
                {
                    // Al deshabilitar motores, parar todo movimiento
                    _robotManager?.JointController?.StopAllJoints();
                }
                
                UpdateSystemActiveState();
            }
        }

        /// <summary>
        /// Establece el nivel de potencia del robot
        /// </summary>
        public void SetPowerLevel(PowerLevel level)
        {
            if (!_isInitialized) return;
            
            PowerLevel previousLevel = _currentPowerLevel;
            _currentPowerLevel = level;
            
            if (previousLevel != level)
            {
                Debug.Log($"[ArScaraSystemController] Power level changed to {level}");
                OnPowerLevelChanged?.Invoke(level);
                
                // Actualizar velocidad del joint controller
                float speedMultiplier = _powerSpeedMultipliers[(int)level];
                _robotManager?.JointController?.SetMovementSpeed(speedMultiplier);
            }
        }
        #endregion

        #region Safety Control
        /// <summary>
        /// Establece el estado del Emergency Stop
        /// </summary>
        public void SetEmergencyStop(bool active)
        {
            if (!_isInitialized) return;
            
            bool wasActive = _emergencyStopActive;
            _emergencyStopActive = active;
            
            if (wasActive != active)
            {
                Debug.Log($"[ArScaraSystemController] Emergency Stop {(active ? "ACTIVATED" : "deactivated")}");
                OnEmergencyStopChanged?.Invoke(active);
                
                if (active)
                {
                    // Emergency stop para todo inmediatamente
                    _robotManager?.JointController?.StopAllJoints();
                    TriggerAlarm("Emergency Stop Activated");
                }
                
                UpdateSystemActiveState();
            }
        }

        /// <summary>
        /// Establece el estado del Safeguard
        /// </summary>
        public void SetSafeguard(bool active)
        {
            if (!_isInitialized) return;
            
            bool wasActive = _safeguardActive;
            _safeguardActive = active;
            
            if (wasActive != active)
            {
                Debug.Log($"[ArScaraSystemController] Safeguard {(active ? "TRIGGERED" : "cleared")}");
                OnSafeguardChanged?.Invoke(active);
                
                if (active)
                {
                    // Safeguard para movimiento suavemente
                    _robotManager?.JointController?.StopAllJoints();
                    TriggerAlarm("Safeguard Triggered");
                }
                
                UpdateSystemActiveState();
            }
        }
        #endregion

        #region Alarm Management
        /// <summary>
        /// Dispara una alarma del sistema
        /// </summary>
        public void TriggerAlarm(string message)
        {
            if (!_isInitialized) return;
            
            _hasAlarms = true;
            Debug.LogWarning($"[ArScaraSystemController] ALARM: {message}");
            OnAlarmTriggered?.Invoke(message);
            
            // Parar movimiento cuando hay alarma
            _robotManager?.JointController?.StopAllJoints();
            UpdateSystemActiveState();
        }

        /// <summary>
        /// Limpia todas las alarmas del sistema
        /// </summary>
        public void ResetAlarms()
        {
            if (!_isInitialized) return;
            
            bool hadAlarms = _hasAlarms;
            _hasAlarms = false;
            
            if (hadAlarms)
            {
                Debug.Log("[ArScaraSystemController] All alarms cleared");
                OnAlarmsCleared?.Invoke();
                UpdateSystemActiveState();
            }
        }
        #endregion

        #region Home and Reset Operations
        /// <summary>
        /// Ejecuta secuencia de Home del robot
        /// </summary>
        public void ExecuteHome()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[ArScaraSystemController] Cannot execute Home - system not initialized");
                return;
            }
            
            if (!_isSystemActive)
            {
                Debug.LogWarning("[ArScaraSystemController] Cannot execute Home - system not active");
                TriggerAlarm("Home attempted while system inactive");
                return;
            }
            
            Debug.Log("[ArScaraSystemController] Executing Home sequence...");
            
            // Delegar al robot manager para mover a home
            _robotManager?.SetToHomePosition();
        }

        /// <summary>
        /// Ejecuta reset completo del sistema
        /// </summary>
        public void ExecuteSystemReset()
        {
            if (!_isInitialized) return;
            
            Debug.Log("[ArScaraSystemController] Executing system reset...");
            
            // Limpiar alarmas
            ResetAlarms();
            
            // Limpiar estados de seguridad (solo los reseteables)
            // Emergency Stop y Safeguard normalmente requieren intervención física
            // pero en simulación los podemos limpiar
            _emergencyStopActive = false;
            _safeguardActive = false;
            
            // Notificar cambios
            OnEmergencyStopChanged?.Invoke(_emergencyStopActive);
            OnSafeguardChanged?.Invoke(_safeguardActive);
            
            UpdateSystemActiveState();
            
            Debug.Log("[ArScaraSystemController] System reset completed");
        }
        #endregion

        #region Joint Enable/Disable Operations
        /// <summary>
        /// Habilita todos los joints
        /// </summary>
        public void EnableAllJoints()
        {
            if (!_isInitialized) return;
            
            Debug.Log("[ArScaraSystemController] Enabling all joints...");
            
            for (int i = 0; i < 4; i++)
            {
                _robotManager?.JointController?.SetJointEnabled(i, true);
            }
        }

        /// <summary>
        /// Deshabilita todos los joints (Lock All)
        /// </summary>
        public void LockAllJoints()
        {
            if (!_isInitialized) return;
            
            Debug.Log("[ArScaraSystemController] Locking all joints...");
            
            // Primero parar todo movimiento
            _robotManager?.JointController?.StopAllJoints();
            
            // Luego deshabilitar todos los joints
            for (int i = 0; i < 4; i++)
            {
                _robotManager?.JointController?.SetJointEnabled(i, false);
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Estado de los motores
        /// </summary>
        public bool MotorsEnabled => _motorsEnabled;

        /// <summary>
        /// Nivel de potencia actual
        /// </summary>
        public PowerLevel CurrentPowerLevel => _currentPowerLevel;

        /// <summary>
        /// Estado del Emergency Stop
        /// </summary>
        public bool EmergencyStopActive => _emergencyStopActive;

        /// <summary>
        /// Estado del Safeguard
        /// </summary>
        public bool SafeguardActive => _safeguardActive;

        /// <summary>
        /// Si hay alarmas activas
        /// </summary>
        public bool HasAlarms => _hasAlarms;

        /// <summary>
        /// Si el sistema está activo y puede moverse
        /// </summary>
        public bool IsSystemActive => _isSystemActive;

        /// <summary>
        /// Si el controlador está inicializado
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Obtiene el multiplicador de velocidad para el nivel de potencia actual
        /// </summary>
        public float CurrentSpeedMultiplier => _powerSpeedMultipliers[(int)_currentPowerLevel];
        #endregion

        #region Debug and Testing
        [ContextMenu("Debug System State")]
        public void DebugSystemState()
        {
            if (!_isInitialized)
            {
                Debug.Log("System controller not initialized");
                return;
            }

            Debug.Log("=== System Controller Debug ===");
            Debug.Log($"Motors Enabled: {_motorsEnabled}");
            Debug.Log($"Power Level: {_currentPowerLevel}");
            Debug.Log($"Emergency Stop: {_emergencyStopActive}");
            Debug.Log($"Safeguard: {_safeguardActive}");
            Debug.Log($"Has Alarms: {_hasAlarms}");
            Debug.Log($"System Active: {_isSystemActive}");
            Debug.Log($"Speed Multiplier: {CurrentSpeedMultiplier:F2}x");
        }

        [ContextMenu("Test Emergency Stop")]
        public void TestEmergencyStop()
        {
            SetEmergencyStop(!_emergencyStopActive);
        }

        [ContextMenu("Test Safeguard")]
        public void TestSafeguard()
        {
            SetSafeguard(!_safeguardActive);
        }

        [ContextMenu("Test Alarm")]
        public void TestAlarm()
        {
            TriggerAlarm("Test alarm triggered from debug menu");
        }

        /// <summary>
        /// Simula condiciones de emergencia para testing
        /// </summary>
        public void SimulateEmergencyCondition(bool active)
        {
            _simulateEmergencyStop = active;
        }

        /// <summary>
        /// Simula condiciones de safeguard para testing
        /// </summary>
        public void SimulateSafeguardCondition(bool active)
        {
            _simulateSafeguard = active;
        }
        #endregion
    }
}