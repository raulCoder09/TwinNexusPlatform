using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Scripts.Controller;
using _Scripts.Controllers.DashboardController;
using _Scripts.Controllers.DeviceSelectionController;
using _Scripts.Controllers.WelcomeController;
using UnityEngine;

namespace _Scripts.Controllers.UiManagement
{
    /// <summary>
    /// Coordinador supremo de todas las interfaces de usuario del sistema
    /// Gestiona el ciclo de vida, autenticación y transiciones entre UIs
    /// Actúa como intermediario entre ServiceController y los controladores de UI específicos
    /// </summary>
    public class UIController : MonoBehaviour
    {
        #region Singleton Pattern

        private static UIController _instance;
        private static readonly object _lock = new object();

        public static UIController Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindObjectOfType<UIController>();
                            
                            if (_instance == null)
                            {
                                var go = new GameObject("UIController");
                                _instance = go.AddComponent<UIController>();
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

        [Header("UI Controller Configuration")]
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private bool _autoHideUnauthenticatedUIs = true;
        [SerializeField] private float _transitionDelay = 0.5f;
        [SerializeField] private bool _enableUIPreloading = true;

        [Header("UI Scenes Configuration")]
        [SerializeField] private string _welcomeSceneTag = "Welcome";
        [SerializeField] private string _dashboardSceneTag = "Dashboard";
        [SerializeField] private string _deviceSelectionSceneTag = "DeviceSelection";

        #endregion

        #region Private Fields

        // Estado del sistema
        private bool _isInitialized = false;
        private bool _isUserAuthenticated = false;
        private bool _isTransitioning = false;

        // Controladores de UI
        private readonly Dictionary<string, IUIController> _uiControllers = new Dictionary<string, IUIController>();
        private IUIController _currentActiveController = null;
        private IUIController _previousController = null;

        // Referencias específicas
        private WelcomeOrchestrator _welcomeController;
        private DashboardOrchestrator _dashboardController;
        private DeviceSelectionOrchestrator _deviceSelectionController;

        // Lista de controladores que requieren autenticación
        private readonly HashSet<string> _authenticatedControllers = new HashSet<string>();
        private readonly HashSet<string> _publicControllers = new HashSet<string>();

        // Control de inicialización
        private readonly Dictionary<string, bool> _controllerInitializationStatus = new Dictionary<string, bool>();

        #endregion

        #region Events

        /// <summary>
        /// Eventos del UI Controller
        /// </summary>
        public event Action OnUIControllerInitialized;
        public event Action OnUserAuthenticated;
        public event Action OnUserLoggedOut;
        public event Action<IUIController, IUIController> OnUITransition; // (from, to)
        public event Action<string> OnUIError;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton enforcement
            if (_instance != null && _instance != this)
            {
                LogDebug("Destroying duplicate UIController instance");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LogDebug("UIController awakened");
        }

        private void Start()
        {
            // Inicializar UIController
            StartCoroutine(InitializeUIControllerCoroutine());
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                LogDebug("UIController destroyed - cleaning up");
                CleanupUIController();
                _instance = null;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Inicializa el UI Controller de forma asíncrona
        /// </summary>
        private IEnumerator InitializeUIControllerCoroutine()
        {
            LogDebug("Starting UIController initialization...");

            // Esperar a que ServiceController esté listo
            yield return StartCoroutine(WaitForServiceController());

            // Verificar si hubo error en ServiceController
            if (ServiceController.Instance == null)
            {
                LogError("ServiceController not available after timeout");
                OnUIError?.Invoke("ServiceController not available");
                yield break;
            }

            // Detectar y registrar controladores UI
            DiscoverUIControllers();

            // Suscribirse a eventos de ServiceController
            SubscribeToServiceControllerEvents();

            // Verificar estado de autenticación actual
            CheckCurrentAuthenticationState();

            // Inicializar controladores públicos inmediatamente
            yield return StartCoroutine(InitializePublicControllers());

            // Pre-cargar controladores autenticados si está habilitado
            if (_enableUIPreloading)
            {
                yield return StartCoroutine(PreloadAuthenticatedControllers());
            }

            // Mostrar UI inicial apropiada
            ShowInitialUI();

            _isInitialized = true;
            LogDebug("UIController initialization completed successfully");
            OnUIControllerInitialized?.Invoke();
        }

        /// <summary>
        /// Espera a que ServiceController esté disponible
        /// </summary>
        private IEnumerator WaitForServiceController()
        {
            LogDebug("Waiting for ServiceController...");
            
            int maxAttempts = 100; // 10 segundos máximo
            int attempts = 0;

            while (ServiceController.Instance == null && attempts < maxAttempts)
            {
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }

            if (ServiceController.Instance == null)
            {
                LogError("ServiceController not available after timeout");
                throw new InvalidOperationException("ServiceController not available");
            }

            LogDebug("ServiceController is available");
        }

        /// <summary>
        /// Descubre y registra todos los controladores UI disponibles
        /// </summary>
        private void DiscoverUIControllers()
        {
            LogDebug("Discovering UI controllers...");

            // Buscar WelcomeOrchestrator
            _welcomeController = FindUIController<WelcomeOrchestrator>(_welcomeSceneTag);
            if (_welcomeController != null)
            {
                RegisterUIController("Welcome", _welcomeController, requiresAuth: false);
                LogDebug("WelcomeController discovered and registered");
            }

            // Buscar DashboardOrchestrator
            _dashboardController = FindUIController<DashboardOrchestrator>(_dashboardSceneTag);
            if (_dashboardController != null)
            {
                RegisterUIController("Dashboard", _dashboardController, requiresAuth: true);
                LogDebug("DashboardController discovered and registered");
            }
            _deviceSelectionController = FindUIController<DeviceSelectionOrchestrator>(_deviceSelectionSceneTag);
            if (_deviceSelectionController != null)
            {
                RegisterUIController("DeviceSelection", _deviceSelectionController, requiresAuth: true);
                LogDebug("DeviceSelectionController discovered and registered");
            }

            // Aquí se pueden agregar más controladores en el futuro:
            // - TrainingController
            // - OperationsController  
            // - ReportsController
            // - SettingsController

            LogDebug($"Discovery completed. Found {_uiControllers.Count} UI controllers");
        }

        /// <summary>
        /// Busca un controlador UI específico por tipo y tag
        /// </summary>
        private T FindUIController<T>(string sceneTag) where T : MonoBehaviour, IUIController
        {
            // Primero buscar por instancia si existe
            var instance = FindObjectOfType<T>();
            if (instance != null)
            {
                return instance;
            }

            // Buscar por tag si no se encuentra instancia
            var gameObjectWithTag = GameObject.FindGameObjectWithTag(sceneTag);
            if (gameObjectWithTag != null)
            {
                var controller = gameObjectWithTag.GetComponent<T>();
                if (controller != null)
                {
                    return controller;
                }

                // Si no tiene el componente, intentar agregarlo
                LogDebug($"Adding {typeof(T).Name} component to GameObject with tag {sceneTag}");
                return gameObjectWithTag.AddComponent<T>();
            }

            LogWarning($"UI Controller {typeof(T).Name} not found (tag: {sceneTag})");
            return null;
        }

        /// <summary>
        /// Registra un controlador UI en el sistema
        /// </summary>
        private void RegisterUIController(string name, IUIController controller, bool requiresAuth)
        {
            if (controller == null)
            {
                LogWarning($"Attempted to register null controller: {name}");
                return;
            }

            _uiControllers[name] = controller;
            _controllerInitializationStatus[name] = false;

            if (requiresAuth)
            {
                _authenticatedControllers.Add(name);
            }
            else
            {
                _publicControllers.Add(name);
            }

            // Suscribirse a eventos del controlador
            SubscribeToControllerEvents(controller);

            LogDebug($"UI Controller registered: {name} (Auth required: {requiresAuth})");
        }

        /// <summary>
        /// Se suscribe a eventos de ServiceController
        /// </summary>
        private void SubscribeToServiceControllerEvents()
        {
            var serviceController = ServiceController.Instance;
            if (serviceController != null)
            {
                serviceController.OnCognitoServiceReady += OnCognitoServiceReady;
                serviceController.OnServiceError += OnServiceControllerError;
                LogDebug("Subscribed to ServiceController events");
            }

            // Suscribirse a eventos de WelcomeController si está disponible
            if (_welcomeController != null)
            {
                _welcomeController.OnAuthenticationSuccess += OnWelcomeAuthenticationSuccess;
                LogDebug("Subscribed to WelcomeController authentication events");
            }
            if (_deviceSelectionController != null)
            {
                _deviceSelectionController.OnDeviceLaunched += OnDeviceSelectionDeviceLaunched;
                _deviceSelectionController.OnContextChanged += OnDeviceSelectionContextChanged;
                LogDebug("Subscribed to DeviceSelectionController events");
            }
        }
        /// <summary>
        /// Maneja cuando se lanza un dispositivo desde Device Selection
        /// </summary>
        private void OnDeviceSelectionDeviceLaunched(string deviceId, string sceneName)
        {
            LogDebug($"Device launched from Device Selection: {deviceId} → {sceneName}");
    
            // Aquí se puede agregar lógica adicional si es necesario
            // Por ejemplo, tracking, analytics, cleanup de UI, etc.
        }
        
        /// <summary>
        /// Maneja cambios de contexto desde Device Selection
        /// </summary>
        private void OnDeviceSelectionContextChanged(NavigationContext fromContext, NavigationContext toContext)
        {
            LogDebug($"Device Selection context changed: {fromContext} → {toContext}");
    
            // Si el contexto cambió a Dashboard, navegar automáticamente
            if (toContext == NavigationContext.Dashboard)
            {
                ShowUI("Dashboard");
            }
        }
        #endregion

        /// <summary>
        /// Se suscribe a eventos de un controlador específico
        /// </summary>
        private void SubscribeToControllerEvents(IUIController controller)
        {
            controller.OnControllerInitialized += OnControllerInitialized;
            controller.OnControllerShown += OnControllerShown;
            controller.OnControllerHidden += OnControllerHidden;
            controller.OnControllerError += OnControllerError;
        }

        /// <summary>
        /// Verifica el estado actual de autenticación
        /// </summary>
        private void CheckCurrentAuthenticationState()
        {
            var userInfo = ServiceController.Instance?.GetUserInfo();
            _isUserAuthenticated = userInfo?.isAuthenticated ?? false;

            LogDebug($"Current authentication state: {_isUserAuthenticated}");
            
            if (_isUserAuthenticated)
            {
                LogDebug($"User already authenticated: {userInfo?.username}");
            }
        }

        /// <summary>
        /// Inicializa controladores públicos (no requieren autenticación)
        /// </summary>
        private IEnumerator InitializePublicControllers()
        {
            LogDebug("Initializing public controllers...");

            foreach (var controllerName in _publicControllers)
            {
                if (_uiControllers.TryGetValue(controllerName, out var controller))
                {
                    yield return StartCoroutine(InitializeControllerCoroutine(controllerName, controller));
                }
            }

            LogDebug("Public controllers initialization completed");
        }

        /// <summary>
        /// Pre-carga controladores autenticados para mejorar performance
        /// </summary>
        private IEnumerator PreloadAuthenticatedControllers()
        {
            if (!_isUserAuthenticated)
            {
                LogDebug("User not authenticated - skipping preload of authenticated controllers");
                yield break;
            }

            LogDebug("Preloading authenticated controllers...");

            foreach (var controllerName in _authenticatedControllers)
            {
                if (_uiControllers.TryGetValue(controllerName, out var controller))
                {
                    yield return StartCoroutine(InitializeControllerCoroutine(controllerName, controller));
                }
            }

            LogDebug("Authenticated controllers preload completed");
        }

        /// <summary>
        /// Inicializa un controlador específico
        /// </summary>
        private IEnumerator InitializeControllerCoroutine(string name, IUIController controller)
        {
            LogDebug($"Initializing controller: {name}");

            bool success = false;
            string errorMessage = null;

            // Realizar inicialización fuera del try-catch para evitar problemas con yield
            success = controller.Initialize();
            
            if (!success)
            {
                errorMessage = $"Controller {name} initialization returned false";
            }

            _controllerInitializationStatus[name] = success;

            if (success)
            {
                LogDebug($"Controller {name} initialized successfully");
            }
            else
            {
                LogError($"Controller {name} initialization failed: {errorMessage}");
            }

            // Pequeño delay para evitar bloquear el frame
            yield return null;
        }

        /// <summary>
        /// Muestra la UI inicial apropiada
        /// </summary>
        private void ShowInitialUI()
        {
            if (_isUserAuthenticated)
            {
                // Usuario ya autenticado - mostrar Dashboard
                ShowUI("Dashboard");
            }
            else
            {
                // Usuario no autenticado - mostrar Welcome
                ShowUI("Welcome");
            }
        }
        

        #region UI Management

        /// <summary>
        /// Muestra una UI específica
        /// </summary>
        public bool ShowUI(string uiName, Dictionary<string, object> parameters = null)
        {
            try
            {
                if (_isTransitioning)
                {
                    LogWarning($"Cannot show UI {uiName} - transition in progress");
                    return false;
                }

                if (!_uiControllers.TryGetValue(uiName, out var controller))
                {
                    LogError($"UI Controller not found: {uiName}");
                    return false;
                }

                // Verificar autenticación si es necesario
                if (controller.RequiresAuthentication && !_isUserAuthenticated)
                {
                    LogWarning($"Cannot show UI {uiName} - user not authenticated");
                    
                    // Redirigir a Welcome automáticamente
                    if (uiName != "Welcome")
                    {
                        return ShowUI("Welcome");
                    }
                    return false;
                }
                if (uiName == "DeviceSelection" && parameters != null)
                {
                    HandleDeviceSelectionParameters(parameters);
                }

                // Verificar si el controlador está inicializado
                if (!_controllerInitializationStatus.GetValueOrDefault(uiName, false))
                {
                    LogDebug($"Controller {uiName} not initialized - initializing now");
                    StartCoroutine(InitializeAndShowUICoroutine(uiName, controller));
                    return true;
                }

                // Ejecutar transición
                StartCoroutine(TransitionToUICoroutine(uiName, controller));
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error showing UI {uiName}: {ex.Message}");
                OnUIError?.Invoke($"Failed to show UI {uiName}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Maneja parámetros específicos para Device Selection
        /// </summary>
        private void HandleDeviceSelectionParameters(Dictionary<string, object> parameters)
        {
            try
            {
                LogDebug("Handling Device Selection parameters");
        
                // Los parámetros se pueden pasar al NavigationContextManager
                var navManager = NavigationContextManager.Instance;
                if (navManager != null)
                {
                    // Si no hay contexto específico en parámetros, usar el actual del NavigationContextManager
                    if (!parameters.ContainsKey("context"))
                    {
                        parameters["context"] = navManager.CurrentContext;
                    }
            
                    // Agregar datos adicionales al contexto
                    foreach (var kvp in parameters)
                    {
                        navManager.AddContextData(kvp.Key, kvp.Value);
                    }
                }
        
                LogDebug($"Device Selection parameters processed: {parameters.Count} items");
            }
            catch (Exception ex)
            {
                LogError($"Error handling Device Selection parameters: {ex.Message}");
            }
        }
        #endregion

        /// <summary>
        /// Inicializa y muestra una UI en secuencia
        /// </summary>
        private IEnumerator InitializeAndShowUICoroutine(string uiName, IUIController controller, Dictionary<string, object> parameters = null)
        {
            // Inicializar primero
            yield return StartCoroutine(InitializeControllerCoroutine(uiName, controller));

            // Verificar si la inicialización fue exitosa
            if (_controllerInitializationStatus.GetValueOrDefault(uiName, false))
            {
                // Mostrar la UI
                yield return StartCoroutine(TransitionToUICoroutine(uiName, controller));
            }
            else
            {
                LogError($"Cannot show UI {uiName} - initialization failed");
            }
        }

        /// <summary>
        /// Ejecuta transición entre UIs
        /// </summary>
        private IEnumerator TransitionToUICoroutine(string uiName, IUIController targetController, Dictionary<string, object> parameters = null)
        {
            _isTransitioning = true;
            _previousController = _currentActiveController;

            LogDebug($"Starting UI transition to: {uiName}");

            // Ocultar UI actual si existe
            if (_currentActiveController != null)
            {
                LogDebug($"Hiding current UI: {_currentActiveController.ControllerName}");
                
                // Manejar posibles errores en Hide() sin try-catch
                var currentControllerName = _currentActiveController.ControllerName;
                _currentActiveController.Hide();
                
                // Delay para permitir animaciones de salida
                yield return new WaitForSeconds(_transitionDelay);
            }
            
            if (uiName == "DeviceSelection" && _deviceSelectionController != null)
            {
                ConfigureDeviceSelectionController(parameters);
            }

            // Mostrar nueva UI
            LogDebug($"Showing new UI: {uiName}");
            
            // Manejar posibles errores en Show() sin try-catch
            targetController.Show();
            _currentActiveController = targetController;

            // Disparar evento de transición
            OnUITransition?.Invoke(_previousController, _currentActiveController);

            LogDebug($"UI transition completed: {uiName}");
            _isTransitioning = false;
        }
        
        /// <summary>
        /// Configura el Device Selection Controller antes de mostrarlo
        /// </summary>
        private void ConfigureDeviceSelectionController(Dictionary<string, object> parameters)
        {
            try
            {
                LogDebug("Configuring Device Selection Controller");
        
                // El DeviceSelectionOrchestrator se configurará automáticamente 
                // desde el NavigationContextManager en su método Show()
                // No necesitamos configuración adicional aquí
        
                LogDebug("Device Selection Controller configured");
            }
            catch (Exception ex)
            {
                LogError($"Error configuring Device Selection Controller: {ex.Message}");
            }
        }
        /// <summary>
        /// Oculta una UI específica
        /// </summary>
        public bool HideUI(string uiName)
        {
            try
            {
                if (!_uiControllers.TryGetValue(uiName, out var controller))
                {
                    LogError($"UI Controller not found: {uiName}");
                    return false;
                }

                controller.Hide();
                
                if (_currentActiveController == controller)
                {
                    _currentActiveController = null;
                }

                LogDebug($"UI hidden: {uiName}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error hiding UI {uiName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Oculta todas las UIs
        /// </summary>
        public void HideAllUIs()
        {
            LogDebug("Hiding all UIs");

            foreach (var kvp in _uiControllers)
            {
                try
                {
                    kvp.Value.Hide();
                }
                catch (Exception ex)
                {
                    LogError($"Error hiding UI {kvp.Key}: {ex.Message}");
                }
            }

            _currentActiveController = null;
        }
        

        #region Authentication Management

        /// <summary>
        /// Maneja autenticación exitosa del usuario
        /// </summary>
        private void HandleUserAuthentication()
        {
            if (_isUserAuthenticated)
            {
                LogDebug("User already authenticated - skipping authentication handling");
                return;
            }

            LogDebug("Handling user authentication...");
            _isUserAuthenticated = true;

            // Inicializar controladores autenticados si no se hizo antes
            if (!_enableUIPreloading)
            {
                StartCoroutine(InitializeAuthenticatedControllers());
            }

            // Transición automática a Dashboard
            ShowUI("Dashboard");

            // Disparar evento
            OnUserAuthenticated?.Invoke();

            LogDebug("User authentication handled successfully");
        }

        /// <summary>
        /// Maneja logout del usuario
        /// </summary>
        private void HandleUserLogout()
        {
            LogDebug("Handling user logout...");
            _isUserAuthenticated = false;

            // Ocultar UIs autenticadas si está configurado
            if (_autoHideUnauthenticatedUIs)
            {
                HideAuthenticatedUIs();
            }

            // Mostrar Welcome
            ShowUI("Welcome");

            // Disparar evento
            OnUserLoggedOut?.Invoke();

            LogDebug("User logout handled successfully");
        }

        /// <summary>
        /// Inicializa controladores autenticados después de login
        /// </summary>
        private IEnumerator InitializeAuthenticatedControllers()
        {
            LogDebug("Initializing authenticated controllers after login...");

            foreach (var controllerName in _authenticatedControllers)
            {
                if (_uiControllers.TryGetValue(controllerName, out var controller))
                {
                    if (!_controllerInitializationStatus.GetValueOrDefault(controllerName, false))
                    {
                        yield return StartCoroutine(InitializeControllerCoroutine(controllerName, controller));
                    }
                }
            }

            LogDebug("Authenticated controllers initialization completed");
        }

        /// <summary>
        /// Oculta todas las UIs que requieren autenticación
        /// </summary>
        private void HideAuthenticatedUIs()
        {
            LogDebug("Hiding authenticated UIs");

            foreach (var controllerName in _authenticatedControllers)
            {
                HideUI(controllerName);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Maneja cuando Cognito está listo en ServiceController
        /// </summary>
        private void OnCognitoServiceReady()
        {
            LogDebug("Cognito service ready - checking authentication state");
            CheckCurrentAuthenticationState();
        }

        /// <summary>
        /// Maneja errores de ServiceController
        /// </summary>
        private void OnServiceControllerError(string serviceName, string error)
        {
            LogError($"ServiceController error in {serviceName}: {error}");
            OnUIError?.Invoke($"Service error: {error}");
        }

        /// <summary>
        /// Maneja autenticación exitosa desde Welcome
        /// </summary>
        private void OnWelcomeAuthenticationSuccess()
        {
            LogDebug("Welcome authentication success received");
            HandleUserAuthentication();
        }

        /// <summary>
        /// Maneja inicialización exitosa de controlador
        /// </summary>
        private void OnControllerInitialized(IUIController controller)
        {
            LogDebug($"Controller initialized: {controller.ControllerName}");
        }

        /// <summary>
        /// Maneja cuando se muestra un controlador
        /// </summary>
        private void OnControllerShown(IUIController controller)
        {
            LogDebug($"Controller shown: {controller.ControllerName}");
        }

        /// <summary>
        /// Maneja cuando se oculta un controlador
        /// </summary>
        private void OnControllerHidden(IUIController controller)
        {
            LogDebug($"Controller hidden: {controller.ControllerName}");
        }

        /// <summary>
        /// Maneja errores de controladores
        /// </summary>
        private void OnControllerError(IUIController controller, string error)
        {
            LogError($"Controller error in {controller.ControllerName}: {error}");
            OnUIError?.Invoke($"UI error in {controller.ControllerName}: {error}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Obtiene información sobre el estado del UIController
        /// </summary>
        public UIControllerStatus GetStatus()
        {
            return new UIControllerStatus
            {
                IsInitialized = _isInitialized,
                IsUserAuthenticated = _isUserAuthenticated,
                IsTransitioning = _isTransitioning,
                CurrentActiveController = _currentActiveController?.ControllerName,
                TotalControllers = _uiControllers.Count,
                InitializedControllers = _controllerInitializationStatus.Values.Count(v => v),
                AuthenticatedControllers = _authenticatedControllers.ToList(),
                PublicControllers = _publicControllers.ToList()
            };
        }

        /// <summary>
        /// Obtiene lista de controladores disponibles
        /// </summary>
        public List<string> GetAvailableControllers()
        {
            return _uiControllers.Keys.ToList();
        }

        /// <summary>
        /// Verifica si un controlador está inicializado
        /// </summary>
        public bool IsControllerInitialized(string controllerName)
        {
            return _controllerInitializationStatus.GetValueOrDefault(controllerName, false);
        }

        /// <summary>
        /// Obtiene el controlador actualmente activo
        /// </summary>
        public IUIController GetActiveController()
        {
            return _currentActiveController;
        }

        /// <summary>
        /// Fuerza la re-inicialización de un controlador
        /// </summary>
        public bool ReinitializeController(string controllerName)
        {
            if (!_uiControllers.TryGetValue(controllerName, out var controller))
            {
                LogError($"Controller not found for reinitialization: {controllerName}");
                return false;
            }

            LogDebug($"Reinitializing controller: {controllerName}");
            
            // Ocultar si está activo
            if (_currentActiveController == controller)
            {
                controller.Hide();
                _currentActiveController = null;
            }

            // Limpiar estado
            controller.Cleanup();
            _controllerInitializationStatus[controllerName] = false;

            // Re-inicializar
            StartCoroutine(InitializeControllerCoroutine(controllerName, controller));
            return true;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Limpia el UIController
        /// </summary>
        private void CleanupUIController()
        {
            try
            {
                LogDebug("Cleaning up UIController...");

                // Desuscribirse de eventos de ServiceController
                var serviceController = ServiceController.Instance;
                if (serviceController != null)
                {
                    serviceController.OnCognitoServiceReady -= OnCognitoServiceReady;
                    serviceController.OnServiceError -= OnServiceControllerError;
                }

                // Desuscribirse de eventos de WelcomeController
                if (_welcomeController != null)
                {
                    _welcomeController.OnAuthenticationSuccess -= OnWelcomeAuthenticationSuccess;
                }
                
                if (_deviceSelectionController != null)
                {
                    _deviceSelectionController.OnDeviceLaunched -= OnDeviceSelectionDeviceLaunched;
                    _deviceSelectionController.OnContextChanged -= OnDeviceSelectionContextChanged;
                }


                // Limpiar controladores
                foreach (var kvp in _uiControllers)
                {
                    try
                    {
                        UnsubscribeFromControllerEvents(kvp.Value);
                        kvp.Value.Cleanup();
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error cleaning up controller {kvp.Key}: {ex.Message}");
                    }
                }

                // Limpiar colecciones
                _uiControllers.Clear();
                _authenticatedControllers.Clear();
                _publicControllers.Clear();
                _controllerInitializationStatus.Clear();

                // Reset estado
                _isInitialized = false;
                _isUserAuthenticated = false;
                _isTransitioning = false;
                _currentActiveController = null;
                _previousController = null;

                LogDebug("UIController cleanup completed");
            }
            catch (Exception ex)
            {
                LogError($"Error during UIController cleanup: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Método helper para navegar a Device Selection con contexto específico
        /// </summary>
        public bool ShowDeviceSelection(NavigationContext context, Dictionary<string, object> additionalData = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["context"] = context
            };
    
            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    parameters[kvp.Key] = kvp.Value;
                }
            }
    
            return ShowUI("DeviceSelection", parameters);
        }

        /// <summary>
        /// Verifica si Device Selection está disponible
        /// </summary>
        public bool IsDeviceSelectionAvailable()
        {
            return _deviceSelectionController != null && 
                   _controllerInitializationStatus.GetValueOrDefault("DeviceSelection", false);
        }
        #endregion

        /// <summary>
        /// Desuscribirse de eventos de un controlador
        /// </summary>
        private void UnsubscribeFromControllerEvents(IUIController controller)
        {
            controller.OnControllerInitialized -= OnControllerInitialized;
            controller.OnControllerShown -= OnControllerShown;
            controller.OnControllerHidden -= OnControllerHidden;
            controller.OnControllerError -= OnControllerError;
        }
        

        #region Logging

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[UIController] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[UIController] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[UIController] {message}");
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Estado del UIController
    /// </summary>
    [Serializable]
    public class UIControllerStatus
    {
        public bool IsInitialized { get; set; }
        public bool IsUserAuthenticated { get; set; }
        public bool IsTransitioning { get; set; }
        public string CurrentActiveController { get; set; }
        public int TotalControllers { get; set; }
        public int InitializedControllers { get; set; }
        public List<string> AuthenticatedControllers { get; set; } = new List<string>();
        public List<string> PublicControllers { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"UIController - Initialized: {IsInitialized}, Auth: {IsUserAuthenticated}, " +
                   $"Active: {CurrentActiveController}, Controllers: {InitializedControllers}/{TotalControllers}";
        }
    }

    #endregion
}