using System;
using System.Collections.Generic;
using UnityEngine;
using _Scripts.Models.CognitoManagement;
using _Scripts.Models.SESManagement;

namespace _Scripts.Controller
{
    /// <summary>
    /// Coordinador central de todos los servicios AWS
    /// Responsabilidad: Inicializar servicios en el orden correcto y manejar dependencias
    /// </summary>
    public class ServiceController : MonoBehaviour
    {
        #region Singleton Pattern
        
        private static ServiceController _instance;
        private static readonly object _lock = new object();

        public static ServiceController Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // Buscar existente primero
                            _instance = FindObjectOfType<ServiceController>();
                            
                            if (_instance == null)
                            {
                                var go = new GameObject("ServiceController");
                                _instance = go.AddComponent<ServiceController>();
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

        [Header("Service Controller Configuration")]
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private bool _autoInitializeServices = true;
        [SerializeField] private float _serviceInitializationDelay = 1.0f;

        #endregion

        #region Private Fields

        // Estados de servicios
        private bool _isInitialized = false;
        private bool _cognitoReady = false;
        private bool _awsServicesInitialized = false;
        
        // Referencias a servicios - ServiceController ahora los gestiona
        private CognitoManager _cognitoManager;
        private SESManager _sesManager;
        
        // Control de creación de servicios
        private bool _cognitoManagerCreatedByUs = false;
        
        // Lista de servicios AWS que se activarán post-autenticación
        private readonly List<string> _pendingAWSServices = new List<string>
        {
            "SESManager",
            "CloudWatchManager", 
            "S3Manager",
            "IoTCoreManager",
            "EC2Manager",
            "LambdaManager"
        };

        #endregion

        #region Events

        /// <summary>
        /// Eventos para notificar cambios de estado
        /// </summary>
        public event Action OnServiceControllerInitialized;
        public event Action OnCognitoServiceReady;
        public event Action OnAWSServicesInitialized;
        public event Action<string> OnServiceActivated;
        public event Action<string, string> OnServiceError;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton enforcement
            if (_instance != null && _instance != this)
            {
                LogDebug("Destroying duplicate ServiceController instance");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LogDebug("ServiceController awakened - starting initialization");
            
            // Inicializar inmediatamente
            InitializeServiceController();
        }

        private void Start()
        {
            if (_autoInitializeServices)
            {
                // Por ahora solo observamos, no interferimos con el sistema existente
                StartObservingExistingServices();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                LogDebug("ServiceController destroyed - cleaning up");
                CleanupServices();
                _instance = null;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Inicializa el ServiceController y toma control de CognitoManager
        /// </summary>
        private void InitializeServiceController()
        {
            try
            {
                if (_isInitialized)
                {
                    LogDebug("ServiceController already initialized");
                    return;
                }

                LogDebug("Initializing ServiceController...");
                
                // NUEVO: Crear o tomar control de CognitoManager
                InitializeCognitoManager();
                
                // Preparar estructura de otros servicios
                PrepareServiceStructure();
                
                _isInitialized = true;
                LogDebug("ServiceController initialized successfully");
                
                OnServiceControllerInitialized?.Invoke();
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize ServiceController: {ex.Message}");
                OnServiceError?.Invoke("ServiceController", ex.Message);
            }
        }

        /// <summary>
        /// Prepara la estructura de servicios sin activarlos todavía
        /// </summary>
        private void PrepareServiceStructure()
        {
            LogDebug("Preparing service structure...");
            
            // Por ahora solo registramos qué servicios están disponibles
            foreach (var serviceName in _pendingAWSServices)
            {
                LogDebug($"Service registered: {serviceName}");
            }
            
            LogDebug("Service structure prepared");
        }

        /// <summary>
        /// Observa los servicios existentes sin modificarlos
        /// </summary>
        private void StartObservingExistingServices()
        {
            LogDebug("Starting to observe existing services...");
            
            // Buscar CognitoManager existente (puede estar en WelcomeOrchestrator)
            ObserveCognitoManager();
            
            // Buscar SESManager existente
            ObserveSESManager();
            
            LogDebug("Service observation started");
        }

        #endregion

        #region Cognito Management

        /// <summary>
        /// Inicializa o toma control de CognitoManager
        /// </summary>
        private void InitializeCognitoManager()
        {
            LogDebug("Initializing CognitoManager...");
            
            // Buscar CognitoManager existente primero
            _cognitoManager = FindObjectOfType<CognitoManager>();
            
            if (_cognitoManager != null)
            {
                LogDebug("Found existing CognitoManager - taking control");
                _cognitoManagerCreatedByUs = false;
                
                // Asegurarse de que esté en nuestro GameObject como hijo
                EnsureCognitoManagerAsChild();
            }
            else
            {
                LogDebug("No existing CognitoManager found - creating new one");
                CreateCognitoManager();
                _cognitoManagerCreatedByUs = true;
            }
            
            // Suscribirse a eventos de CognitoManager
            SubscribeToCognitoEvents();
            
            // Verificar estado actual
            CheckCognitoCurrentState();
            
            LogDebug("CognitoManager initialization completed");
        }

        /// <summary>
        /// Asegura que CognitoManager esté como hijo de ServiceController
        /// </summary>
        private void EnsureCognitoManagerAsChild()
        {
            if (_cognitoManager.transform.parent != this.transform)
            {
                LogDebug("Moving existing CognitoManager to ServiceController hierarchy");
                _cognitoManager.transform.SetParent(this.transform);
                _cognitoManager.transform.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// Crea un nuevo CognitoManager como hijo
        /// </summary>
        private void CreateCognitoManager()
        {
            var cognitoGO = new GameObject("CognitoManager");
            cognitoGO.transform.SetParent(this.transform);
            cognitoGO.transform.localPosition = Vector3.zero;
            
            _cognitoManager = cognitoGO.AddComponent<CognitoManager>();
            LogDebug("Created new CognitoManager as child");
        }

        /// <summary>
        /// Se suscribe a eventos de CognitoManager
        /// </summary>
        private void SubscribeToCognitoEvents()
        {
            if (_cognitoManager == null) return;
            
            LogDebug("Subscribing to CognitoManager events");
            
            _cognitoManager.OnAuthenticationComplete += OnCognitoAuthenticationComplete;
            _cognitoManager.OnAWSCredentialsObtained += OnCognitoAWSCredentialsObtained;
            _cognitoManager.OnRegistrationComplete += OnCognitoRegistrationComplete;
            _cognitoManager.OnEmailVerificationComplete += OnCognitoEmailVerificationComplete;
            _cognitoManager.OnPasswordRecoveryComplete += OnCognitoPasswordRecoveryComplete;
            _cognitoManager.OnResendVerificationComplete += OnCognitoResendVerificationComplete;
        }

        /// <summary>
        /// Verifica el estado actual de CognitoManager
        /// </summary>
        private void CheckCognitoCurrentState()
        {
            if (_cognitoManager != null && _cognitoManager.IsUserAuthenticated)
            {
                LogDebug("CognitoManager is already authenticated");
                HandleCognitoReady();
                
                // Si ya tiene credenciales AWS, activar servicios
                if (_cognitoManager.CurrentAWSCredentials != null)
                {
                    LogDebug("CognitoManager already has AWS credentials");
                    HandleAWSCredentialsReady();
                }
            }
        }

        #endregion

        /// <summary>
        /// Observa el CognitoManager existente
        /// </summary>
        private void ObserveCognitoManager()
        {
            // Buscar instancia existente
            _cognitoManager = CognitoManager.Instance;
            
            if (_cognitoManager != null)
            {
                LogDebug("Found existing CognitoManager - observing");
                
                // Suscribirse a eventos existentes sin interferir
                _cognitoManager.OnAuthenticationComplete += OnCognitoAuthenticationComplete;
                _cognitoManager.OnAWSCredentialsObtained += OnCognitoAWSCredentialsObtained;
                
                // Verificar si ya está autenticado
                if (_cognitoManager.IsUserAuthenticated)
                {
                    LogDebug("CognitoManager already authenticated");
                    HandleCognitoReady();
                }
            }
            else
            {
                LogDebug("CognitoManager not found - will wait for it");
            }
        }

        /// <summary>
        /// Observa el SESManager existente
        /// </summary>
        private void ObserveSESManager()
        {
            // Buscar instancia existente
            _sesManager = SESManager.Instance;
            
            if (_sesManager != null)
            {
                LogDebug("Found existing SESManager - observing");
                
                // Suscribirse a eventos existentes
                _sesManager.OnInitializationCompleted += OnSESInitializationCompleted;
            }
            else
            {
                LogDebug("SESManager not found - will activate later");
            }
        }
        

        #region Event Handlers

        /// <summary>
        /// Maneja el éxito de autenticación de Cognito
        /// </summary>
        private void OnCognitoAuthenticationComplete(bool success, string message)
        {
            LogDebug($"Cognito authentication complete - Success: {success}, Message: {message}");
            
            if (success)
            {
                LogDebug("Cognito authentication successful - preparing AWS services");
                HandleCognitoReady();
            }
            else
            {
                LogDebug($"Cognito authentication failed: {message}");
            }
        }

        /// <summary>
        /// Maneja el éxito de registro de Cognito
        /// </summary>
        private void OnCognitoRegistrationComplete(bool success, string message)
        {
            LogDebug($"Cognito registration complete - Success: {success}, Message: {message}");
        }

        /// <summary>
        /// Maneja la verificación de email de Cognito
        /// </summary>
        private void OnCognitoEmailVerificationComplete(bool success, string message)
        {
            LogDebug($"Cognito email verification complete - Success: {success}, Message: {message}");
            
            if (success)
            {
                // Después de verificar email exitosamente, el usuario queda autenticado
                HandleCognitoReady();
            }
        }

        /// <summary>
        /// Maneja la recuperación de contraseña de Cognito
        /// </summary>
        private void OnCognitoPasswordRecoveryComplete(bool success, string message)
        {
            LogDebug($"Cognito password recovery complete - Success: {success}, Message: {message}");
        }

        /// <summary>
        /// Maneja el reenvío de código de verificación
        /// </summary>
        private void OnCognitoResendVerificationComplete(bool success, string message)
        {
            LogDebug($"Cognito resend verification complete - Success: {success}, Message: {message}");
        }

        /// <summary>
        /// Maneja la obtención de credenciales AWS
        /// </summary>
        private void OnCognitoAWSCredentialsObtained(bool success, string message)
        {
            if (success)
            {
                LogDebug("AWS credentials obtained - ready to initialize AWS services");
                HandleAWSCredentialsReady();
            }
            else
            {
                LogDebug($"Failed to obtain AWS credentials: {message}");
            }
        }

        /// <summary>
        /// Maneja la inicialización de SES
        /// </summary>
        private void OnSESInitializationCompleted(bool success, string message)
        {
            if (success)
            {
                LogDebug("SES initialization completed successfully");
                OnServiceActivated?.Invoke("SESManager");
            }
            else
            {
                LogDebug($"SES initialization failed: {message}");
                OnServiceError?.Invoke("SESManager", message);
            }
        }

        #endregion

        #region Service State Handlers

        /// <summary>
        /// Maneja cuando Cognito está listo
        /// </summary>
        private void HandleCognitoReady()
        {
            if (!_cognitoReady)
            {
                _cognitoReady = true;
                LogDebug("Cognito service is ready");
                OnCognitoServiceReady?.Invoke();
            }
        }

        /// <summary>
        /// Maneja cuando las credenciales AWS están listas
        /// </summary>
        private void HandleAWSCredentialsReady()
        {
            if (_cognitoReady && !_awsServicesInitialized)
            {
                LogDebug("AWS credentials ready - can initialize AWS services");
                // Por ahora solo notificamos, en el siguiente paso activaremos servicios
                PrepareAWSServicesActivation();
            }
        }

        /// <summary>
        /// Prepara la activación de servicios AWS (sin activar todavía)
        /// </summary>
        private void PrepareAWSServicesActivation()
        {
            LogDebug("Preparing AWS services for activation...");
            
            _awsServicesInitialized = true;
            OnAWSServicesInitialized?.Invoke();
            
            LogDebug("AWS services preparation complete");
        }

        #endregion

        #region Public API - Cognito Access

        /// <summary>
        /// Obtiene la instancia de CognitoManager gestionada por ServiceController
        /// </summary>
        public CognitoManager CognitoManager => _cognitoManager;

        /// <summary>
        /// Verifica si Cognito está listo y autenticado
        /// </summary>
        public bool IsCognitoAuthenticated => _cognitoReady && _cognitoManager != null && _cognitoManager.IsUserAuthenticated;

        /// <summary>
        /// Obtiene información del usuario autenticado
        /// </summary>
        public (string username, string userGroup, bool isAuthenticated) GetUserInfo()
        {
            if (_cognitoManager != null)
            {
                return (_cognitoManager.CurrentUsername, _cognitoManager.CurrentUserGroup, _cognitoManager.IsUserAuthenticated);
            }
            return (string.Empty, string.Empty, false);
        }

        /// <summary>
        /// Verifica si el usuario pertenece a un grupo específico
        /// </summary>
        public bool IsUserInGroup(string groupName)
        {
            return _cognitoManager?.IsUserInGroup(groupName) ?? false;
        }

        /// <summary>
        /// Obtiene el rol del usuario actual
        /// </summary>
        public string GetUserRole()
        {
            return _cognitoManager?.GetUserRole() ?? "usuarios-basicos";
        }

        #endregion

        #region Public API (for future use)

        /// <summary>
        /// Verifica si un servicio está disponible
        /// </summary>
        public bool IsServiceAvailable(string serviceName)
        {
            switch (serviceName.ToLower())
            {
                case "cognito":
                case "cognitomanager":
                    return _cognitoReady;
                    
                case "ses":
                case "sesmanager":
                    return _awsServicesInitialized && _sesManager != null;
                    
                default:
                    return false;
            }
        }

        /// <summary>
        /// Obtiene información del estado de servicios
        /// </summary>
        public ServiceStatus GetServiceStatus()
        {
            return new ServiceStatus
            {
                IsInitialized = _isInitialized,
                CognitoReady = _cognitoReady,
                AWSServicesInitialized = _awsServicesInitialized,
                AvailableServices = GetAvailableServices()
            };
        }

        /// <summary>
        /// Obtiene lista de servicios disponibles
        /// </summary>
        public List<string> GetAvailableServices()
        {
            var services = new List<string>();
            
            if (_cognitoReady) services.Add("CognitoManager");
            if (_awsServicesInitialized && _sesManager != null) services.Add("SESManager");
            
            return services;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Limpia recursos al destruir
        /// </summary>
        private void CleanupServices()
        {
            // Desuscribirse de eventos de CognitoManager
            if (_cognitoManager != null)
            {
                _cognitoManager.OnAuthenticationComplete -= OnCognitoAuthenticationComplete;
                _cognitoManager.OnAWSCredentialsObtained -= OnCognitoAWSCredentialsObtained;
                _cognitoManager.OnRegistrationComplete -= OnCognitoRegistrationComplete;
                _cognitoManager.OnEmailVerificationComplete -= OnCognitoEmailVerificationComplete;
                _cognitoManager.OnPasswordRecoveryComplete -= OnCognitoPasswordRecoveryComplete;
                _cognitoManager.OnResendVerificationComplete -= OnCognitoResendVerificationComplete;
                
                // Si lo creamos nosotros, destruirlo; si no, solo desvincularnos
                if (_cognitoManagerCreatedByUs && _cognitoManager.gameObject != null)
                {
                    LogDebug("Destroying CognitoManager created by ServiceController");
                    DestroyImmediate(_cognitoManager.gameObject);
                }
            }
            
            // Desuscribirse de eventos de SESManager
            if (_sesManager != null)
            {
                _sesManager.OnInitializationCompleted -= OnSESInitializationCompleted;
            }
            
            LogDebug("ServiceController cleanup completed");
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[ServiceController] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[ServiceController] {message}");
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Estado de los servicios
    /// </summary>
    [System.Serializable]
    public class ServiceStatus
    {
        public bool IsInitialized { get; set; }
        public bool CognitoReady { get; set; }
        public bool AWSServicesInitialized { get; set; }
        public List<string> AvailableServices { get; set; } = new List<string>();
        
        public override string ToString()
        {
            return $"ServiceController Status - Initialized: {IsInitialized}, Cognito: {CognitoReady}, AWS: {AWSServicesInitialized}, Services: [{string.Join(", ", AvailableServices)}]";
        }
    }

    #endregion
}