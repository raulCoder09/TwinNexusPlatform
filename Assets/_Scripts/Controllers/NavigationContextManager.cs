using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Controllers
{
    /// <summary>
    /// Gestor centralizado del contexto de navegación entre UIs
    /// Mantiene información sobre el origen y destino de las transiciones
    /// Permite a las UIs comportarse diferente según su contexto de activación
    /// </summary>
    public class NavigationContextManager : MonoBehaviour
    {
        #region Singleton Pattern

        private static NavigationContextManager _instance;
        private static readonly object _lock = new object();

        public static NavigationContextManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindObjectOfType<NavigationContextManager>();
                            
                            if (_instance == null)
                            {
                                var go = new GameObject("NavigationContextManager");
                                _instance = go.AddComponent<NavigationContextManager>();
                                DontDestroyOnLoad(go);
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Configuration

        [Header("Navigation Context Configuration")]
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private int _maxNavigationHistory = 50;
        [SerializeField] private bool _persistContextBetweenScenes = true;

        #endregion

        #region Private Fields

        // Estado actual del contexto
        private NavigationContext _currentContext = NavigationContext.None;
        private NavigationContext _previousContext = NavigationContext.None;
        private Dictionary<string, object> _contextData = new Dictionary<string, object>();

        // Historial de navegación
        private readonly List<NavigationRecord> _navigationHistory = new List<NavigationRecord>();

        // Control de inicialización
        private bool _isInitialized = false;

        #endregion

        #region Public Properties

        /// <summary>
        /// Contexto de navegación actual
        /// </summary>
        public NavigationContext CurrentContext => _currentContext;

        /// <summary>
        /// Contexto de navegación anterior
        /// </summary>
        public NavigationContext PreviousContext => _previousContext;

        /// <summary>
        /// Datos asociados al contexto actual
        /// </summary>
        public IReadOnlyDictionary<string, object> ContextData => _contextData;

        /// <summary>
        /// Indica si el manager está inicializado
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Historial de navegación (solo lectura)
        /// </summary>
        public IReadOnlyList<NavigationRecord> NavigationHistory => _navigationHistory.AsReadOnly();

        #endregion

        #region Events

        /// <summary>
        /// Se dispara cuando cambia el contexto de navegación
        /// </summary>
        public event Action<NavigationContext, NavigationContext> OnContextChanged;

        /// <summary>
        /// Se dispara cuando se agregan datos al contexto
        /// </summary>
        public event Action<string, object> OnContextDataAdded;

        /// <summary>
        /// Se dispara cuando se limpia el contexto
        /// </summary>
        public event Action OnContextCleared;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton enforcement
            if (_instance != null && _instance != this)
            {
                LogDebug("Destroying duplicate NavigationContextManager instance");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                LogDebug("NavigationContextManager destroyed");
                Cleanup();
                _instance = null;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Inicializa el Navigation Context Manager
        /// </summary>
        private void Initialize()
        {
            try
            {
                LogDebug("Initializing NavigationContextManager...");

                // Inicializar estado por defecto
                _currentContext = NavigationContext.None;
                _previousContext = NavigationContext.None;
                _contextData.Clear();
                _navigationHistory.Clear();

                _isInitialized = true;
                LogDebug("NavigationContextManager initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"Error initializing NavigationContextManager: {ex.Message}");
            }
        }

        #endregion

        #region Context Management

        /// <summary>
        /// Establece el contexto de navegación
        /// </summary>
        /// <param name="context">Nuevo contexto</param>
        /// <param name="data">Datos asociados al contexto (opcional)</param>
        public void SetContext(NavigationContext context, Dictionary<string, object> data = null)
        {
            try
            {
                var oldContext = _currentContext;
                _previousContext = _currentContext;
                _currentContext = context;

                // Actualizar datos del contexto
                _contextData.Clear();
                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        _contextData[kvp.Key] = kvp.Value;
                    }
                }

                // Agregar al historial
                AddToNavigationHistory(oldContext, context, data);

                // Disparar evento
                OnContextChanged?.Invoke(_previousContext, _currentContext);

                LogDebug($"Context changed: {_previousContext} → {_currentContext}");
            }
            catch (Exception ex)
            {
                LogError($"Error setting context: {ex.Message}");
            }
        }

        /// <summary>
        /// Agrega datos al contexto actual sin cambiar el contexto
        /// </summary>
        /// <param name="key">Clave del dato</param>
        /// <param name="value">Valor del dato</param>
        public void AddContextData(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                LogWarning("Attempted to add context data with null or empty key");
                return;
            }

            _contextData[key] = value;
            OnContextDataAdded?.Invoke(key, value);
            
            LogDebug($"Context data added: {key} = {value}");
        }

        /// <summary>
        /// Obtiene un dato del contexto actual
        /// </summary>
        /// <typeparam name="T">Tipo del dato</typeparam>
        /// <param name="key">Clave del dato</param>
        /// <param name="defaultValue">Valor por defecto si no existe</param>
        /// <returns>El valor del dato o el valor por defecto</returns>
        public T GetContextData<T>(string key, T defaultValue = default(T))
        {
            try
            {
                if (string.IsNullOrEmpty(key) || !_contextData.ContainsKey(key))
                {
                    return defaultValue;
                }

                var value = _contextData[key];
                if (value is T typedValue)
                {
                    return typedValue;
                }

                // Intentar conversión
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                LogWarning($"Error getting context data for key '{key}': {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Verifica si existe un dato en el contexto
        /// </summary>
        /// <param name="key">Clave a verificar</param>
        /// <returns>True si existe el dato</returns>
        public bool HasContextData(string key)
        {
            return !string.IsNullOrEmpty(key) && _contextData.ContainsKey(key);
        }

        /// <summary>
        /// Limpia el contexto actual
        /// </summary>
        public void ClearContext()
        {
            _previousContext = _currentContext;
            _currentContext = NavigationContext.None;
            _contextData.Clear();

            OnContextCleared?.Invoke();
            LogDebug("Context cleared");
        }

        #endregion

        #region Device Mode Management

        /// <summary>
        /// Obtiene el modo de dispositivo para un contexto específico
        /// </summary>
        /// <param name="context">Contexto de navegación</param>
        /// <returns>Modo de dispositivo correspondiente</returns>
        public DeviceMode GetDeviceModeForContext(NavigationContext context)
        {
            return context switch
            {
                NavigationContext.Training => DeviceMode.Learning,
                NavigationContext.Operations => DeviceMode.Operating,
                NavigationContext.Reports => DeviceMode.Monitoring,
                NavigationContext.Settings => DeviceMode.Configuration,
                NavigationContext.Dashboard => DeviceMode.Operating, // Default
                _ => DeviceMode.Operating
            };
        }

        /// <summary>
        /// Obtiene el título de UI para un contexto específico
        /// </summary>
        /// <param name="context">Contexto de navegación</param>
        /// <returns>Título de UI correspondiente</returns>
        public string GetUITitleForContext(NavigationContext context)
        {
            return context switch
            {
                NavigationContext.Training => "Devices available for learning",
                NavigationContext.Operations => "Devices available for operate",
                NavigationContext.Reports => "Devices available for monitoring",
                NavigationContext.Settings => "Devices available for configuration",
                NavigationContext.Dashboard => "Available devices",
                _ => "Select device"
            };
        }

        /// <summary>
        /// Obtiene la escena de destino para un contexto específico
        /// </summary>
        /// <param name="context">Contexto de navegación</param>
        /// <returns>Nombre de la escena de destino</returns>
        public string GetTargetSceneForContext(NavigationContext context)
        {
            return context switch
            {
                NavigationContext.Training => "Training",
                NavigationContext.Operations => "Operations",
                NavigationContext.Reports => "Reports",
                NavigationContext.Settings => "Settings",
                _ => "Dashboard"
            };
        }

        #endregion

        #region Navigation History

        /// <summary>
        /// Agrega una entrada al historial de navegación
        /// </summary>
        private void AddToNavigationHistory(NavigationContext from, NavigationContext to, Dictionary<string, object> data)
        {
            var record = new NavigationRecord
            {
                FromContext = from,
                ToContext = to,
                Timestamp = DateTime.Now,
                ContextData = data != null ? new Dictionary<string, object>(data) : new Dictionary<string, object>()
            };

            _navigationHistory.Insert(0, record); // Insertar al principio (más reciente)

            // Mantener límite del historial
            if (_navigationHistory.Count > _maxNavigationHistory)
            {
                _navigationHistory.RemoveAt(_navigationHistory.Count - 1);
            }
        }

        /// <summary>
        /// Obtiene el último registro de navegación
        /// </summary>
        /// <returns>Último registro o null si no hay historial</returns>
        public NavigationRecord GetLastNavigationRecord()
        {
            return _navigationHistory.Count > 0 ? _navigationHistory[0] : null;
        }

        /// <summary>
        /// Obtiene el historial de navegación filtrado por contexto
        /// </summary>
        /// <param name="context">Contexto a filtrar</param>
        /// <returns>Lista de registros filtrados</returns>
        public List<NavigationRecord> GetNavigationHistoryForContext(NavigationContext context)
        {
            return _navigationHistory.FindAll(record => record.ToContext == context || record.FromContext == context);
        }

        /// <summary>
        /// Limpia el historial de navegación
        /// </summary>
        public void ClearNavigationHistory()
        {
            _navigationHistory.Clear();
            LogDebug("Navigation history cleared");
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Verifica si el contexto actual permite una acción específica
        /// </summary>
        /// <param name="action">Acción a verificar</param>
        /// <returns>True si la acción está permitida</returns>
        public bool IsActionAllowedInCurrentContext(string action)
        {
            // Esta lógica se puede extender según las reglas de negocio
            return _currentContext switch
            {
                NavigationContext.Training => action.Contains("Learning") || action.Contains("Training"),
                NavigationContext.Operations => action.Contains("Operating") || action.Contains("Operation"),
                NavigationContext.Reports => action.Contains("Monitoring") || action.Contains("Report"),
                NavigationContext.Settings => action.Contains("Configuration") || action.Contains("Settings"),
                _ => true // Por defecto permitir todas las acciones
            };
        }

        /// <summary>
        /// Obtiene información completa del estado actual
        /// </summary>
        /// <returns>Estado actual del contexto</returns>
        public NavigationContextStatus GetStatus()
        {
            return new NavigationContextStatus
            {
                CurrentContext = _currentContext,
                PreviousContext = _previousContext,
                ContextDataCount = _contextData.Count,
                NavigationHistoryCount = _navigationHistory.Count,
                IsInitialized = _isInitialized,
                LastNavigationTime = GetLastNavigationRecord()?.Timestamp
            };
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Limpia recursos del Navigation Context Manager
        /// </summary>
        private void Cleanup()
        {
            try
            {
                LogDebug("Cleaning up NavigationContextManager...");

                _contextData.Clear();
                _navigationHistory.Clear();
                
                _currentContext = NavigationContext.None;
                _previousContext = NavigationContext.None;
                _isInitialized = false;

                LogDebug("NavigationContextManager cleanup completed");
            }
            catch (Exception ex)
            {
                LogError($"Error during NavigationContextManager cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[NavigationContextManager] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[NavigationContextManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[NavigationContextManager] {message}");
        }

        #endregion
    }

    #region Supporting Enums and Classes

    /// <summary>
    /// Contextos de navegación disponibles en el sistema
    /// </summary>
    public enum NavigationContext
    {
        None,
        Dashboard,
        Training,
        Operations,
        Reports,
        Settings,
        DeviceSelection
    }

    /// <summary>
    /// Modos de funcionamiento de dispositivos según el contexto
    /// </summary>
    public enum DeviceMode
    {
        Learning,        // Desde Training - funcionalidades educativas
        Operating,       // Desde Operations - funcionalidades operativas
        Monitoring,      // Desde Reports - funcionalidades de monitoreo
        Configuration    // Desde Settings - funcionalidades de configuración
    }

    /// <summary>
    /// Registro de una navegación específica
    /// </summary>
    [Serializable]
    public class NavigationRecord
    {
        public NavigationContext FromContext { get; set; }
        public NavigationContext ToContext { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> ContextData { get; set; } = new Dictionary<string, object>();

        public override string ToString()
        {
            return $"{FromContext} → {ToContext} at {Timestamp:HH:mm:ss}";
        }
    }

    /// <summary>
    /// Estado actual del Navigation Context Manager
    /// </summary>
    [Serializable]
    public class NavigationContextStatus
    {
        public NavigationContext CurrentContext { get; set; }
        public NavigationContext PreviousContext { get; set; }
        public int ContextDataCount { get; set; }
        public int NavigationHistoryCount { get; set; }
        public bool IsInitialized { get; set; }
        public DateTime? LastNavigationTime { get; set; }

        public override string ToString()
        {
            return $"Context: {CurrentContext}, Previous: {PreviousContext}, " +
                   $"Data: {ContextDataCount}, History: {NavigationHistoryCount}";
        }
    }

    #endregion
}