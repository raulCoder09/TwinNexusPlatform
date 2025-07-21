using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _Scripts.Models.CloudWatchManagement;
using _Scripts.Models.CognitoManagement;
using _Scripts.Models.IoTCoreManagement;
using _Scripts.Models.LambdaManagement;
using _Scripts.Models.MQTTManagement;
using _Scripts.Models.S3Management;
using _Scripts.Models.SESManagement;
using Amazon;
using UnityEngine;

namespace _Scripts.Controllers.ServiceManagement
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
        
        [Header("AWS Configuration")]
        [SerializeField] private string _awsAccountId = "156041417101";

        #endregion

        #region Private Fields

        // Estados de servicios
        private bool _isInitialized = false;
        private bool _cognitoReady = false;
        private bool _awsServicesInitialized = false;
        
        // Referencias a servicios - ServiceController ahora los gestiona
        private CognitoManager _cognitoManager;
        private SESManager _sesManager;
        private CloudWatchManager _cloudWatchManager;
        private IoTCoreManager _iotCoreManager;
        private S3Manager _s3Manager;
        private LambdaManager _lambdaManager;
        
        #region MQTT Management
        private MqttManager _mqttManager;
        private AwsMqttTesting _awsMqttTesting;
        #endregion
        
        // Control de creación de servicios
        private bool _cognitoManagerCreatedByUs = false;
        
        // Lista de servicios AWS que se activarán post-autenticación
        private readonly List<string> _pendingAWSServices = new List<string>
        {
            "SESManager",
            "CloudWatchManager",
            "IoTCoreManager",
            "S3Manager",
            "LambdaManager",
            "EC2Manager",
            "MqttManager"
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
            
            // Buscar CloudWatchManager existente
            ObserveCloudWatchManager();
            
            // Buscar IoTCoreManager existente
            ObserveIoTCoreManager();
            
            // Buscar S3Manager existente
            ObserveS3Manager();
            
            // Buscar LambdaManager existente
            ObserveLambdaManager();
            
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
                else
                {
                    // NUEVO: Forzar obtención de credenciales si no las tiene
                    LogDebug("CognitoManager authenticated but no AWS credentials - requesting them");
                    StartCoroutine(RequestAWSCredentialsCoroutine());
                }
            }
        }

        /// <summary>
        /// Corrutina para solicitar credenciales AWS
        /// </summary>
        private System.Collections.IEnumerator RequestAWSCredentialsCoroutine()
        {
            yield return new WaitForSeconds(0.5f); // Pequeño delay
            
            if (_cognitoManager != null && _cognitoManager.IsUserAuthenticated)
            {
                LogDebug("Requesting AWS credentials...");
                var task = _cognitoManager.GetAWSCredentialsAsync();
                
                // Esperar a que termine la tarea
                while (!task.IsCompleted)
                {
                    yield return null;
                }
                
                if (task.Result)
                {
                    LogDebug("AWS credentials obtained successfully via coroutine");
                }
                else
                {
                    LogError("Failed to obtain AWS credentials via coroutine");
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

        /// <summary>
        /// Observa el CloudWatchManager existente
        /// </summary>
        private void ObserveCloudWatchManager()
        {
            // Buscar instancia existente
            _cloudWatchManager = CloudWatchManager.Instance;
            
            if (_cloudWatchManager != null)
            {
                LogDebug("Found existing CloudWatchManager - observing");
                
                // Suscribirse a eventos existentes (si los definimos)
                // _cloudWatchManager.OnInitializationCompleted += OnCloudWatchInitializationCompleted; // Añadir si definimos evento
            }
            else
            {
                LogDebug("CloudWatchManager not found - will activate later");
            }
        }

        /// <summary>
        /// Observa el IoTCoreManager existente
        /// </summary>
        private void ObserveIoTCoreManager()
        {
            // Buscar instancia existente
            _iotCoreManager = IoTCoreManager.Instance;
            
            if (_iotCoreManager != null)
            {
                LogDebug("Found existing IoTCoreManager - observing");
                
                // Suscribirse a evento de inicialización
                _iotCoreManager.OnInitializationCompleted += OnIoTCoreInitializationCompleted;
            }
            else
            {
                LogDebug("IoTCoreManager not found - will activate later");
            }
        }

        /// <summary>
        /// Observa el S3Manager existente
        /// </summary>
        private void ObserveS3Manager()
        {
            // Buscar instancia existente
            _s3Manager = S3Manager.Instance;
            
            if (_s3Manager != null)
            {
                LogDebug("Found existing S3Manager - observing");
                
                // Suscribirse a evento de inicialización
                _s3Manager.OnInitializationCompleted += OnS3InitializationCompleted;
            }
            else
            {
                LogDebug("S3Manager not found - will activate later");
            }
        }

        /// <summary>
        /// Observa el LambdaManager existente
        /// </summary>
        private void ObserveLambdaManager()
        {
            // Buscar instancia existente
            _lambdaManager = LambdaManager.Instance;
            
            if (_lambdaManager != null)
            {
                LogDebug("Found existing LambdaManager - observing");
                
                // Suscribirse a evento de ejecución (por ahora usamos OnExecutionComplete como proxy)
                _lambdaManager.OnExecutionComplete += OnLambdaExecutionComplete;
            }
            else
            {
                LogDebug("LambdaManager not found - will activate later");
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
                
                // NUEVO: Solicitar credenciales AWS inmediatamente después de autenticación
                StartCoroutine(RequestAWSCredentialsAfterAuthCoroutine());
            }
            else
            {
                LogDebug($"Cognito authentication failed: {message}");
            }
        }

        /// <summary>
        /// Corrutina para solicitar credenciales AWS después de autenticación
        /// </summary>
        private System.Collections.IEnumerator RequestAWSCredentialsAfterAuthCoroutine()
        {
            // Dar un pequeño tiempo para que se complete la autenticación
            yield return new WaitForSeconds(1.0f);
            
            if (_cognitoManager != null && _cognitoManager.IsUserAuthenticated)
            {
                LogDebug("Requesting AWS credentials after authentication...");
                
                // Verificar si ya tiene credenciales
                if (_cognitoManager.CurrentAWSCredentials != null)
                {
                    LogDebug("AWS credentials already available");
                    HandleAWSCredentialsReady();
                }
                else
                {
                    LogDebug("No AWS credentials found - requesting them");
                    var task = _cognitoManager.GetAWSCredentialsAsync();
                    
                    // Esperar a que termine la tarea
                    while (!task.IsCompleted)
                    {
                        yield return null;
                    }
                    
                    LogDebug($"AWS credentials request completed - Success: {task.Result}");
                }
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
                // También solicitar credenciales AWS
                StartCoroutine(RequestAWSCredentialsAfterAuthCoroutine());
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
            LogDebug($"AWS credentials obtained event - Success: {success}, Message: {message}");
            
            if (success)
            {
                LogDebug("AWS credentials obtained - ready to initialize AWS services");
                HandleAWSCredentialsReady();
            }
            else
            {
                LogError($"Failed to obtain AWS credentials: {message}");
                OnServiceError?.Invoke("AWSCredentials", message);
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

        /// <summary>
        /// Maneja la inicialización de CloudWatch (a definir si se añade evento)
        /// </summary>
        private void OnCloudWatchInitializationCompleted(bool success, string message)
        {
            if (success)
            {
                LogDebug("CloudWatch initialization completed successfully");
                OnServiceActivated?.Invoke("CloudWatchManager");
            }
            else
            {
                LogDebug($"CloudWatch initialization failed: {message}");
                OnServiceError?.Invoke("CloudWatchManager", message);
            }
        }

        /// <summary>
        /// Maneja la inicialización de IoTCore
        /// </summary>
        private void OnIoTCoreInitializationCompleted(bool success, string message)
        {
            if (success)
            {
                LogDebug("IoTCore initialization completed successfully");
                OnServiceActivated?.Invoke("IoTCoreManager");
            }
            else
            {
                LogDebug($"IoTCore initialization failed: {message}");
                OnServiceError?.Invoke("IoTCoreManager", message);
            }
        }

        /// <summary>
        /// Maneja la inicialización de S3
        /// </summary>
        private void OnS3InitializationCompleted(bool success, string message)
        {
            if (success)
            {
                LogDebug("S3 initialization completed successfully");
                OnServiceActivated?.Invoke("S3Manager");
            }
            else
            {
                LogDebug($"S3 initialization failed: {message}");
                OnServiceError?.Invoke("S3Manager", message);
            }
        }

        /// <summary>
        /// Maneja la ejecución de Lambda
        /// </summary>
        private void OnLambdaExecutionComplete(bool success, string message, object data)
        {
            if (success)
            {
                LogDebug("Lambda execution completed successfully");
                OnServiceActivated?.Invoke("LambdaManager"); // Usamos esto como proxy para inicialización
            }
            else
            {
                LogDebug($"Lambda execution failed: {message}");
                OnServiceError?.Invoke("LambdaManager", message);
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
                LogDebug("AWS credentials ready - initializing AWS services");
                // Por ahora solo notificamos, en el siguiente paso activaremos servicios
                PrepareAWSServicesActivation();
            }
        }

        /// <summary>
        /// Prepara la activación de servicios AWS (activar tras autenticación)
        /// </summary>
        private void PrepareAWSServicesActivation()
        {
            LogDebug("AWS credentials ready - initializing AWS services...");
            
            // CAMBIO: Ahora SÍ inicializamos servicios AWS
            InitializeAWSServices();

            InitializeServices();
            
            _awsServicesInitialized = true;
            OnAWSServicesInitialized?.Invoke();
            
            LogDebug("AWS services initialization complete");
        }

        /// <summary>
        /// Inicializa servicios AWS con las credenciales obtenidas
        /// </summary>
        private async void InitializeAWSServices()
        {
            try
            {
                LogDebug("Starting AWS services initialization...");
                
                // Verificar que tenemos credenciales AWS
                if (_cognitoManager == null || _cognitoManager.CurrentAWSCredentials == null)
                {
                    LogError("Cannot initialize AWS services - no AWS credentials available");
                    return;
                }

                LogDebug($"AWS Credentials available - AccessKey: {_cognitoManager.CurrentAWSCredentials.GetCredentials().AccessKey.Substring(0, 8)}...");

                // Inicializar SESManager si no existe
                await InitializeSESManager();
                
                // Inicializar CloudWatchManager
                await InitializeCloudWatchManager();
                
                // Inicializar IoTCoreManager
                await InitializeIoTCoreManager();
                
                // Inicializar S3Manager
                await InitializeS3Manager();
                
                // Inicializar LambdaManager
                await InitializeLambdaManager();
                
                // Aquí puedes agregar otros servicios AWS en el futuro
                // await InitializeS3Manager();
                // await InitializeCloudWatchManager();
                
                LogDebug("AWS services initialization completed");
            }
            catch (Exception ex)
            {
                LogError($"Error initializing AWS services: {ex.Message}");
                OnServiceError?.Invoke("AWSServices", ex.Message);
            }
        }

        private async void InitializeServices()
        {
            await InitializeMqttManager();
        }

        /// <summary>
        /// Inicializa SESManager con las credenciales de Cognito
        /// </summary>
        private async Task InitializeSESManager()
        {
            try
            {
                LogDebug("Initializing SESManager...");
                
                // Buscar SESManager existente o crear uno nuevo
                _sesManager = SESManager.Instance;
                
                if (_sesManager != null)
                {
                    LogDebug("Found existing SESManager - will reinitialize with credentials");
                    
                    // Suscribirse a eventos
                    _sesManager.OnInitializationCompleted += OnSESInitializationCompleted;
                    
                    // Forzar inicialización con credenciales correctas
                    var initSuccess = await _sesManager.InitializeAsync();
                    
                    if (initSuccess)
                    {
                        LogDebug("SESManager initialized successfully");
                        OnServiceActivated?.Invoke("SESManager");
                    }
                    else
                    {
                        LogError("SESManager initialization failed");
                        OnServiceError?.Invoke("SESManager", "Initialization failed");
                    }
                }
                else
                {
                    LogDebug("SESManager not found - it will initialize itself when needed");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error initializing SESManager: {ex.Message}");
                OnServiceError?.Invoke("SESManager", ex.Message);
            }
        }

        /// <summary>
        /// Inicializa CloudWatchManager con las credenciales de Cognito
        /// </summary>
        private async Task InitializeCloudWatchManager()
        {
            try
            {
                LogDebug("Initializing CloudWatchManager...");
                
                // Buscar CloudWatchManager existente o crear uno nuevo
                _cloudWatchManager = CloudWatchManager.Instance;
                
                if (_cloudWatchManager != null)
                {
                    LogDebug("Found existing CloudWatchManager - will initialize with credentials");
                    
                    // Forzar inicialización con credenciales correctas
                    var initSuccess = await _cloudWatchManager.InitializeAsync(_cognitoManager.CurrentAWSCredentials, _cognitoManager.GetRegionEndpoint());
                    
                    if (initSuccess)
                    {
                        LogDebug("CloudWatchManager initialized successfully");
                        OnServiceActivated?.Invoke("CloudWatchManager");
                    }
                    else
                    {
                        LogError("CloudWatchManager initialization failed");
                        OnServiceError?.Invoke("CloudWatchManager", "Initialization failed");
                    }
                }
                else
                {
                    LogDebug("CloudWatchManager not found - it will initialize itself when needed");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error initializing CloudWatchManager: {ex.Message}");
                OnServiceError?.Invoke("CloudWatchManager", ex.Message);
            }
        }

        /// <summary>
        /// Inicializa IoTCoreManager con las credenciales de Cognito
        /// </summary>
        private async Task InitializeIoTCoreManager()
        {
            try
            {
                LogDebug("Initializing IoTCoreManager...");
                
                // Buscar IoTCoreManager existente o crear uno nuevo
                _iotCoreManager = IoTCoreManager.Instance;
                
                if (_iotCoreManager != null)
                {
                    LogDebug("Found existing IoTCoreManager - will initialize with credentials");
                    
                    // Suscribirse a evento de inicialización
                    _iotCoreManager.OnInitializationCompleted += OnIoTCoreInitializationCompleted;
                    
                    // Forzar inicialización con credenciales correctas y accountId
                    string accountId = "156041417101"; // Hardcodeado temporalmente; idealmente obtenerlo de CognitoManager
                    var initSuccess = await _iotCoreManager.InitializeAsync(_cognitoManager.CurrentAWSCredentials, _cognitoManager.GetRegionEndpoint(), accountId);
                    
                    if (initSuccess)
                    {
                        LogDebug("IoTCoreManager initialized successfully");
                        OnServiceActivated?.Invoke("IoTCoreManager");
                    }
                    else
                    {
                        LogError("IoTCoreManager initialization failed");
                        OnServiceError?.Invoke("IoTCoreManager", "Initialization failed");
                    }
                }
                else
                {
                    LogDebug("IoTCoreManager not found - it will initialize itself when needed");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error initializing IoTCoreManager: {ex.Message}");
                OnServiceError?.Invoke("IoTCoreManager", ex.Message);
            }
        }

        /// <summary>
        /// Inicializa S3Manager con las credenciales de Cognito
        /// </summary>
        private async Task InitializeS3Manager()
        {
            try
            {
                LogDebug("Initializing S3Manager...");
                
                // Buscar S3Manager existente o crear uno nuevo
                _s3Manager = S3Manager.Instance;
                
                if (_s3Manager != null)
                {
                    LogDebug("Found existing S3Manager - will initialize with credentials");
                    
                    // Suscribirse a evento de inicialización
                    _s3Manager.OnInitializationCompleted += OnS3InitializationCompleted;
                    
                    // Forzar inicialización con credenciales correctas
                    var initSuccess = await _s3Manager.InitializeAsync(_cognitoManager.CurrentAWSCredentials, _cognitoManager.GetRegionEndpoint());
                    
                    if (initSuccess)
                    {
                        LogDebug("S3Manager initialized successfully");
                        OnServiceActivated?.Invoke("S3Manager");
                    }
                    else
                    {
                        LogError("S3Manager initialization failed");
                        OnServiceError?.Invoke("S3Manager", "Initialization failed");
                    }
                }
                else
                {
                    LogDebug("S3Manager not found - it will initialize itself when needed");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error initializing S3Manager: {ex.Message}");
                OnServiceError?.Invoke("S3Manager", ex.Message);
            }
        }

        /// <summary>
        /// Inicializa LambdaManager con las credenciales de Cognito
        /// </summary>
        private async Task InitializeLambdaManager()
        {
            try
            {
                LogDebug("Initializing LambdaManager...");
                
                // Buscar LambdaManager existente o crear uno nuevo
                _lambdaManager = LambdaManager.Instance;
                
                if (_lambdaManager != null)
                {
                    LogDebug("Found existing LambdaManager - will initialize with credentials");
                    
                    // Suscribirse a evento de ejecución como proxy para inicialización
                    _lambdaManager.OnExecutionComplete += OnLambdaExecutionComplete;
                    
                    // Forzar inicialización con credenciales correctas
                    var initSuccess = await _lambdaManager.InitializeAsync(_cognitoManager.CurrentAWSCredentials, _cognitoManager.GetRegionEndpoint());
                    
                    if (initSuccess)
                    {
                        LogDebug("LambdaManager initialized successfully");
                        OnServiceActivated?.Invoke("LambdaManager");
                    }
                    else
                    {
                        LogError("LambdaManager initialization failed");
                        OnServiceError?.Invoke("LambdaManager", "Initialization failed");
                    }
                }
                else
                {
                    LogDebug("LambdaManager not found - it will initialize itself when needed");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error initializing LambdaManager: {ex.Message}");
                OnServiceError?.Invoke("LambdaManager", ex.Message);
            }
        }

        #endregion

        #region Public API - Cognito Configuration

        /// <summary>
        /// Actualiza la configuración de Cognito y reinicializa CognitoManager
        /// </summary>
        public bool UpdateCognitoConfiguration(string userPoolId, string clientId, string identityPoolId, RegionEndpoint region)
        {
            try
            {
                LogDebug($"Updating Cognito configuration - UserPool: {userPoolId}, Client: {clientId}, Region: {region.SystemName}");
                
                // Desuscribirse de eventos del CognitoManager actual
                if (_cognitoManager != null)
                {
                    UnsubscribeFromCognitoEvents();
                }
                
                // Destruir CognitoManager actual si lo creamos nosotros
                if (_cognitoManagerCreatedByUs && _cognitoManager != null)
                {
                    LogDebug("Destroying old CognitoManager to create new one with updated configuration");
                    DestroyImmediate(_cognitoManager.gameObject);
                    _cognitoManager = null;
                }
                
                // Crear nuevo CognitoManager con configuración actualizada
                CreateCognitoManagerWithConfiguration(userPoolId, clientId, identityPoolId, region);
                
                // Suscribirse a eventos del nuevo CognitoManager
                SubscribeToCognitoEvents();
                
                // Reset estados
                _cognitoReady = false;
                _awsServicesInitialized = false;
                
                LogDebug("Cognito configuration updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error updating Cognito configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Crea CognitoManager con configuración específica
        /// </summary>
        private void CreateCognitoManagerWithConfiguration(string userPoolId, string clientId, string identityPoolId, RegionEndpoint region)
        {
            var cognitoGO = new GameObject("CognitoManager");
            cognitoGO.transform.SetParent(this.transform);
            cognitoGO.transform.localPosition = Vector3.zero;
            
            _cognitoManager = cognitoGO.AddComponent<CognitoManager>();
            
            // Configurar las propiedades del CognitoManager usando reflection
            var cognitoType = _cognitoManager.GetType();
            
            // Acceder a los campos protegidos usando reflection
            var userPoolIdField = cognitoType.GetField("_userPoolId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var clientIdField = cognitoType.GetField("_clientId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var identityPoolIdField = cognitoType.GetField("_identityPoolId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var regionField = cognitoType.GetField("_regionEndpoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Establecer los valores
            userPoolIdField?.SetValue(_cognitoManager, userPoolId);
            clientIdField?.SetValue(_cognitoManager, clientId);
            identityPoolIdField?.SetValue(_cognitoManager, identityPoolId);
            regionField?.SetValue(_cognitoManager, region);
            
            _cognitoManagerCreatedByUs = true;
            
            LogDebug($"Created new CognitoManager with configuration - UserPool: {userPoolId}, Client: {clientId}");
        }

        /// <summary>
        /// Desuscribirse de eventos de CognitoManager
        /// </summary>
        private void UnsubscribeFromCognitoEvents()
        {
            if (_cognitoManager == null) return;
            
            LogDebug("Unsubscribing from CognitoManager events");
            
            _cognitoManager.OnAuthenticationComplete -= OnCognitoAuthenticationComplete;
            _cognitoManager.OnAWSCredentialsObtained -= OnCognitoAWSCredentialsObtained;
            _cognitoManager.OnRegistrationComplete -= OnCognitoRegistrationComplete;
            _cognitoManager.OnEmailVerificationComplete -= OnCognitoEmailVerificationComplete;
            _cognitoManager.OnPasswordRecoveryComplete -= OnCognitoPasswordRecoveryComplete;
            _cognitoManager.OnResendVerificationComplete -= OnCognitoResendVerificationComplete;
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

        #region Public API - AWS Services Access

        /// <summary>
        /// Obtiene la instancia de SESManager gestionada por ServiceController
        /// </summary>
        public SESManager SESManager => _sesManager;

        /// <summary>
        /// Obtiene la instancia de CloudWatchManager gestionada por ServiceController
        /// </summary>
        public CloudWatchManager CloudWatchManager => _cloudWatchManager;

        /// <summary>
        /// Obtiene la instancia de IoTCoreManager gestionada por ServiceController
        /// </summary>
        public IoTCoreManager IoTCoreManager => _iotCoreManager;

        /// <summary>
        /// Obtiene la instancia de S3Manager gestionada por ServiceController
        /// </summary>
        public S3Manager S3Manager => _s3Manager;

        /// <summary>
        /// Obtiene la instancia de LambdaManager gestionada por ServiceController
        /// </summary>
        public LambdaManager LambdaManager => _lambdaManager;

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
                    
                case "cloudwatch":
                case "cloudwatchmanager":
                    return _awsServicesInitialized && _cloudWatchManager != null;
                    
                case "iot":
                case "iotcoremanager":
                    return _awsServicesInitialized && _iotCoreManager != null;
                    
                case "s3":
                case "s3manager":
                    return _awsServicesInitialized && _s3Manager != null;
                    
                case "lambda":
                case "lambdamanager":
                    return _awsServicesInitialized && _lambdaManager != null;
                case "mqtt":
                case "mqttmanager":
                    return _awsServicesInitialized && _mqttManager != null;
                    
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
            if (_awsServicesInitialized && _cloudWatchManager != null) services.Add("CloudWatchManager");
            if (_awsServicesInitialized && _iotCoreManager != null) services.Add("IoTCoreManager");
            if (_awsServicesInitialized && _s3Manager != null) services.Add("S3Manager");
            if (_awsServicesInitialized && _lambdaManager != null) services.Add("LambdaManager");
            if (_awsServicesInitialized && _mqttManager != null) services.Add("MqttManager");
            
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
            
            // Desuscribirse de eventos de CloudWatchManager (si se añaden)
            // if (_cloudWatchManager != null)
            // {
            //     _cloudWatchManager.OnInitializationCompleted -= OnCloudWatchInitializationCompleted;
            // }
            
            // Desuscribirse de eventos de IoTCoreManager
            if (_iotCoreManager != null)
            {
                _iotCoreManager.OnInitializationCompleted -= OnIoTCoreInitializationCompleted;
            }
            
            // Desuscribirse de eventos de S3Manager
            if (_s3Manager != null)
            {
                _s3Manager.OnInitializationCompleted -= OnS3InitializationCompleted;
            }
            
            // Desuscribirse de eventos de LambdaManager
            if (_lambdaManager != null)
            {
                _lambdaManager.OnExecutionComplete -= OnLambdaExecutionComplete;
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
        
        /// <summary>
        /// Inicializa MqttManager con las credenciales de Cognito
        /// </summary>
        private async Task InitializeMqttManager()
        {
            try
            {
                LogDebug("Initializing MqttManager...");
        
                // Buscar o crear MqttManager
                _mqttManager = MqttManager.Instance;
                if (_mqttManager == null)
                {
                    LogDebug("Creating new MqttManager...");
                    var mqttGO = new GameObject("MqttManager");
                    mqttGO.transform.SetParent(this.transform);
                    _mqttManager = mqttGO.AddComponent<MqttManager>();
                }

                // Buscar o crear AwsMqttTesting
                _awsMqttTesting = FindObjectOfType<AwsMqttTesting>();
                if (_awsMqttTesting == null)
                {
                    LogDebug("Creating AwsMqttTesting component...");
                    var testingGO = new GameObject("AwsMqttTesting");
                    testingGO.transform.SetParent(this.transform);
                    _awsMqttTesting = testingGO.AddComponent<AwsMqttTesting>();
                }

                // Forzar inicialización del MqttManager
                _mqttManager.Initialize();
        
                LogDebug("MqttManager initialized successfully");
                OnServiceActivated?.Invoke("MqttManager");
            }
            catch (Exception ex)
            {
                LogError($"Error initializing MqttManager: {ex.Message}");
                OnServiceError?.Invoke("MqttManager", ex.Message);
            }
        }

        /// <summary>
        /// Obtiene la instancia de MqttManager gestionada por ServiceController
        /// </summary>
        public MqttManager MqttManager => _mqttManager;

        /// <summary>
        /// Obtiene la instancia de AwsMqttTesting gestionada por ServiceController
        /// </summary>
        public AwsMqttTesting AwsMqttTesting => _awsMqttTesting;
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