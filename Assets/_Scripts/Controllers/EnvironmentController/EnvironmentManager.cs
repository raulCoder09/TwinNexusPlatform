using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.EnvironmentController
{
    /// <summary>
    /// Manager principal del sistema de ambientes - implementa IEnvironmentController
    /// Maneja la carga, descarga y transición entre diferentes ambientes
    /// </summary>
    public class EnvironmentManager : MonoBehaviour, IEnvironmentController
    {
        #region Singleton Pattern

        private static EnvironmentManager _instance;
        private static readonly object _lock = new object();

        public static EnvironmentManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindObjectOfType<EnvironmentManager>();
                            
                            if (_instance == null)
                            {
                                var go = new GameObject("EnvironmentManager");
                                _instance = go.AddComponent<EnvironmentManager>();
                                DontDestroyOnLoad(go);
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region IEnvironmentController Implementation

        public bool IsEnvironmentLoaded => _environmentState.CurrentEnvironment != EnvironmentType.None;
        public EnvironmentType CurrentEnvironment => _environmentState.CurrentEnvironment;
        public bool IsTransitioning => _environmentState.IsTransitioning;

        // Events from IEnvironmentController
        public event Action<EnvironmentType> OnEnvironmentLoaded;
        public event Action<EnvironmentType> OnEnvironmentUnloaded;
        public event Action<EnvironmentType, float> OnEnvironmentLoadProgress;
        public event Action<string> OnEnvironmentError;

        #endregion

        #region Configuration

        [Header("Environment Manager Configuration")]
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private bool _autoCleanupOnTransition = true;
        [SerializeField] private float _transitionDelay = 0.5f;

        [Header("Environment Configurations")]
        [SerializeField] private List<EnvironmentInfo.EnvironmentConfig> _environmentConfigs = new List<EnvironmentInfo.EnvironmentConfig>();

        #endregion

        #region Private Fields

        private EnvironmentInfo.EnvironmentState _environmentState = new EnvironmentInfo.EnvironmentState();
        private Dictionary<EnvironmentType, EnvironmentInfo.EnvironmentConfig> _configLookup = new Dictionary<EnvironmentType, EnvironmentInfo.EnvironmentConfig>();
        private bool _isInitialized = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton enforcement
            if (_instance != null && _instance != this)
            {
                LogDebug("Destroying duplicate EnvironmentManager instance");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }

        private void Start()
        {
            LogDebug("EnvironmentManager started");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Cleanup();
                _instance = null;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Inicializa el Environment Manager
        /// </summary>
        private void Initialize()
        {
            try
            {
                LogDebug("Initializing EnvironmentManager...");

                // Crear lookup dictionary para configuraciones
                BuildConfigurationLookup();

                // Suscribirse a eventos de estado
                SubscribeToStateEvents();

                // Marcar como inicializado
                _environmentState.IsInitialized = true;
                _isInitialized = true;

                LogDebug("EnvironmentManager initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize EnvironmentManager: {ex.Message}");
                OnEnvironmentError?.Invoke($"Initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Construye el dictionary de configuraciones para acceso rápido
        /// </summary>
        private void BuildConfigurationLookup()
        {
            _configLookup.Clear();
            
            foreach (var config in _environmentConfigs)
            {
                if (config != null && config.environmentPrefab != null)
                {
                    _configLookup[config.environmentType] = config;
                    LogDebug($"Registered environment config: {config.environmentType} - {config.environmentName}");
                }
                else
                {
                    LogWarning($"Invalid environment config found: {config?.environmentType}");
                }
            }

            LogDebug($"Built configuration lookup with {_configLookup.Count} environments");
        }

        /// <summary>
        /// Se suscribe a eventos de estado interno
        /// </summary>
        private void SubscribeToStateEvents()
        {
            _environmentState.OnEnvironmentChanged += OnInternalEnvironmentChanged;
            _environmentState.OnTransitionStateChanged += OnInternalTransitionStateChanged;
        }

        #endregion

        #region IEnvironmentController Implementation

        /// <summary>
        /// Carga un ambiente específico
        /// </summary>
        public async Task<bool> LoadEnvironment(EnvironmentType environmentType)
        {
            try
            {
                if (!_isInitialized)
                {
                    LogError("EnvironmentManager not initialized");
                    return false;
                }

                if (_environmentState.IsTransitioning)
                {
                    LogWarning("Cannot load environment - transition already in progress");
                    return false;
                }

                if (_environmentState.CurrentEnvironment == environmentType)
                {
                    LogDebug($"Environment {environmentType} already loaded");
                    return true;
                }

                LogDebug($"Starting load of environment: {environmentType}");
                
                // Marcar como en transición
                _environmentState.IsTransitioning = true;
                _environmentState.TriggerTransitionStateChanged(true);

                // Obtener configuración
                if (!_configLookup.TryGetValue(environmentType, out var config))
                {
                    LogError($"No configuration found for environment: {environmentType}");
                    return false;
                }

                // Reportar progreso inicial
                OnEnvironmentLoadProgress?.Invoke(environmentType, 0f);

                // Descargar ambiente actual si existe
                if (_environmentState.CurrentEnvironment != EnvironmentType.None)
                {
                    LogDebug("Unloading current environment...");
                    UnloadCurrentEnvironment();
                    OnEnvironmentLoadProgress?.Invoke(environmentType, 0.3f);
                }

                // Delay para transición suave
                await Task.Delay(Mathf.RoundToInt(_transitionDelay * 1000));
                OnEnvironmentLoadProgress?.Invoke(environmentType, 0.5f);

                // Instanciar nuevo ambiente
                LogDebug($"Instantiating environment prefab: {config.environmentName}");
                var environmentInstance = Instantiate(config.environmentPrefab);
                environmentInstance.name = $"{config.environmentName}_Instance";
                
                OnEnvironmentLoadProgress?.Invoke(environmentType, 0.8f);

                // Actualizar estado
                _environmentState.CurrentEnvironmentInstance = environmentInstance;
                _environmentState.CurrentEnvironment = environmentType;
                _environmentState.LastTransitionTime = DateTime.Now;

                OnEnvironmentLoadProgress?.Invoke(environmentType, 1f);

                // Finalizar transición
                _environmentState.IsTransitioning = false;
                _environmentState.TriggerTransitionStateChanged(false);
                _environmentState.TriggerEnvironmentChanged(environmentType);

                // Disparar evento público
                OnEnvironmentLoaded?.Invoke(environmentType);

                LogDebug($"Environment {environmentType} loaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error loading environment {environmentType}: {ex.Message}");
                
                // Limpiar estado en caso de error
                _environmentState.IsTransitioning = false;
                _environmentState.TriggerTransitionStateChanged(false);
                
                OnEnvironmentError?.Invoke($"Failed to load {environmentType}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Descarga el ambiente actual
        /// </summary>
        public void UnloadCurrentEnvironment()
        {
            try
            {
                if (_environmentState.CurrentEnvironment == EnvironmentType.None)
                {
                    LogDebug("No environment to unload");
                    return;
                }

                var currentEnv = _environmentState.CurrentEnvironment;
                LogDebug($"Unloading environment: {currentEnv}");

                // Destruir instancia si existe
                if (_environmentState.CurrentEnvironmentInstance != null)
                {
                    Destroy(_environmentState.CurrentEnvironmentInstance);
                    _environmentState.CurrentEnvironmentInstance = null;
                }

                // Actualizar estado
                _environmentState.CurrentEnvironment = EnvironmentType.None;

                // Disparar eventos
                OnEnvironmentUnloaded?.Invoke(currentEnv);
                _environmentState.TriggerEnvironmentChanged(EnvironmentType.None);

                LogDebug($"Environment {currentEnv} unloaded successfully");
            }
            catch (Exception ex)
            {
                LogError($"Error unloading environment: {ex.Message}");
                OnEnvironmentError?.Invoke($"Failed to unload environment: {ex.Message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Obtiene la configuración de un ambiente específico
        /// </summary>
        public EnvironmentInfo.EnvironmentConfig GetEnvironmentConfig(EnvironmentType environmentType)
        {
            _configLookup.TryGetValue(environmentType, out var config);
            return config;
        }

        /// <summary>
        /// Obtiene lista de ambientes disponibles
        /// </summary>
        public List<EnvironmentType> GetAvailableEnvironments()
        {
            return new List<EnvironmentType>(_configLookup.Keys);
        }

        /// <summary>
        /// Verifica si un ambiente está disponible
        /// </summary>
        public bool IsEnvironmentAvailable(EnvironmentType environmentType)
        {
            return _configLookup.ContainsKey(environmentType);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Maneja cambios internos de ambiente
        /// </summary>
        private void OnInternalEnvironmentChanged(EnvironmentType environmentType)
        {
            LogDebug($"Internal environment changed to: {environmentType}");
        }

        /// <summary>
        /// Maneja cambios internos de estado de transición
        /// </summary>
        private void OnInternalTransitionStateChanged(bool isTransitioning)
        {
            LogDebug($"Internal transition state changed: {isTransitioning}");
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Limpia el Environment Manager
        /// </summary>
        private void Cleanup()
        {
            try
            {
                LogDebug("Cleaning up EnvironmentManager...");

                // Descargar ambiente actual
                UnloadCurrentEnvironment();

                // Desuscribirse de eventos
                if (_environmentState != null)
                {
                    _environmentState.OnEnvironmentChanged -= OnInternalEnvironmentChanged;
                    _environmentState.OnTransitionStateChanged -= OnInternalTransitionStateChanged;
                }

                // Limpiar configuraciones
                _configLookup?.Clear();

                _isInitialized = false;
                LogDebug("EnvironmentManager cleanup completed");
            }
            catch (Exception ex)
            {
                LogError($"Error during EnvironmentManager cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[EnvironmentManager] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[EnvironmentManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[EnvironmentManager] {message}");
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Estado actual del sistema de ambientes
        /// </summary>
        public EnvironmentInfo.EnvironmentState EnvironmentState => _environmentState;

        #endregion
    }
}