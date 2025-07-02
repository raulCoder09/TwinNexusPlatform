using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _Scripts.Controller;
using _Scripts.Controllers.UiManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace _Scripts.Controllers.DeviceSelectionController
{
    /// <summary>
    /// Orchestrador principal de Device Selection
    /// Implementa IDeviceSelectionOps y coordina toda la funcionalidad de selección de dispositivos
    /// Equivalente al WelcomeOrchestrator y DashboardOrchestrator pero para Device Selection
    /// </summary>
    public class DeviceSelectionOrchestrator : MonoBehaviour, IDeviceSelectionOps
    {
        #region Singleton Pattern

        private static DeviceSelectionOrchestrator _instance;

        public static DeviceSelectionOrchestrator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DeviceSelectionOrchestrator>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DeviceSelectionOrchestrator");
                        _instance = go.AddComponent<DeviceSelectionOrchestrator>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Configuration

        [Header("Device Selection Configuration")]
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private float _deviceRefreshInterval = 30.0f;
        [SerializeField] private bool _autoLaunchOnSelection = true;
        [SerializeField] private float _launchDelay = 1.0f;

        #endregion

        #region Private Fields

        // Managers y componentes internos
        private DeviceSelectionUIManager _uiManager;
        private DeviceSelectionEventManager _eventManager;
        private UIDocument _uiDocument;
        
        // Data containers
        private DeviceSelectionInfo.UIConfiguration _uiConfig;
        private DeviceSelectionInfo.DeviceData _deviceData;
        private DeviceSelectionInfo.ContextData _contextData;
        private DeviceSelectionInfo.NavigationState _navigationState;
        private DeviceSelectionInfo.UserSessionData _sessionData;
        private DeviceSelectionInfo.DeviceSelectionState _deviceSelectionState;

        // Estado interno
        private bool _isInitialized = false;
        private bool _isTransitioning = false;
        private Coroutine _deviceRefreshCoroutine;

        // Referencias externas
        private GameManager _gameManager;

        #endregion

        #region IUIController Implementation

        public bool RequiresAuthentication => true;
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _deviceSelectionState?.IsVisible ?? false;
        public string ControllerName => "DeviceSelectionController";

        // Events from IUIController
        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        #region IDeviceSelectionOps Properties

        public NavigationContext CurrentNavigationContext => _contextData?.CurrentContext ?? NavigationContext.None;
        public DeviceMode CurrentDeviceMode => _contextData?.RequiredMode ?? DeviceMode.Operating;
        public string SelectedDevice => _deviceData?.SelectedDevice ?? "";
        public bool IsNavigationMenuVisible => _uiManager?.IsNavigationMenuVisible ?? false;
        public string CurrentUsername => _sessionData?.Username ?? "Unknown";
        public string CurrentUITitle => _contextData?.UITitle ?? "Select Device";

        #endregion

        #region IDeviceSelectionOps Events

        public event Action<string, DeviceMode> OnDeviceSelected;
        public event Action<string, string> OnDeviceLaunched;
        public event Action<NavigationContext, NavigationContext> OnContextChanged;
        public event Action<List<DeviceSelectionInfo.DeviceInfo>> OnDeviceAvailabilityChanged;
        public event Action<bool> OnNavigationMenuToggled;
        public event Action<string, string> OnDeviceSelectionError;
        public event Func<string, string, bool> OnDeviceLaunchValidation;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // La inicialización se hace externamente por UIController
            if (!_isInitialized)
            {
                LogDebug("Device Selection not initialized, waiting for external initialization");
            }
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
        /// Inicializa el Device Selection Orchestrator
        /// </summary>
        public bool Initialize()
        {
            try
            {
                LogDebug("Initializing Device Selection Orchestrator...");

                if (_isInitialized)
                {
                    LogDebug("Device Selection already initialized");
                    return true;
                }

                // Obtener UIDocument
                _uiDocument = GetComponent<UIDocument>();
                if (_uiDocument == null)
                {
                    LogError("UIDocument component not found");
                    return false;
                }

                // Inicializar datos
                InitializeDataContainers();

                // Buscar GameManager
                FindGameManager();

                // Inicializar UI Manager
                if (!InitializeUIManager())
                {
                    LogError("Failed to initialize UI Manager");
                    return false;
                }

                // Inicializar Event Manager
                InitializeEventManager();

                // Configurar usuario desde ServiceController
                SetupUserSession();

                // Configurar contexto desde NavigationContextManager
                UpdateContextFromNavigationManager();

                // Inicializar datos por defecto
                InitializeDefaultDevices();

                // Configurar auto-refresh si está habilitado
                if (_deviceRefreshInterval > 0)
                {
                    StartDeviceRefresh();
                }

                _isInitialized = true;
                LogDebug("Device Selection Orchestrator initialized successfully");
                
                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Initialization error: {ex.Message}");
                OnControllerError?.Invoke(this, $"Initialization failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Inicializa los contenedores de datos
        /// </summary>
        private void InitializeDataContainers()
        {
            _uiConfig = new DeviceSelectionInfo.UIConfiguration();
            _deviceData = new DeviceSelectionInfo.DeviceData();
            _contextData = new DeviceSelectionInfo.ContextData();
            _navigationState = new DeviceSelectionInfo.NavigationState();
            _sessionData = new DeviceSelectionInfo.UserSessionData();
            _deviceSelectionState = new DeviceSelectionInfo.DeviceSelectionState();

            // Configurar eventos internos
            _deviceSelectionState.OnDeviceSelectionInitialized += () => LogDebug("Device Selection state initialized");
            _deviceSelectionState.OnDeviceSelectionShown += () => OnControllerShown?.Invoke(this);
            _deviceSelectionState.OnDeviceSelectionHidden += () => OnControllerHidden?.Invoke(this);
            _deviceSelectionState.OnError += (error) => OnControllerError?.Invoke(this, error);

            LogDebug("Data containers initialized");
        }

        /// <summary>
        /// Busca y configura referencia al GameManager
        /// </summary>
        private void FindGameManager()
        {
            try
            {
                var gameManagerGO = GameObject.FindGameObjectWithTag("GameManager");
                if (gameManagerGO != null)
                {
                    _gameManager = gameManagerGO.GetComponent<GameManager>();
                    if (_gameManager != null)
                    {
                        LogDebug("GameManager found and referenced");
                    }
                    else
                    {
                        LogWarning("GameObject with GameManager tag found but no GameManager component");
                    }
                }
                else
                {
                    LogWarning("No GameObject with GameManager tag found");
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Error finding GameManager: {ex.Message}");
            }
        }

        /// <summary>
        /// Inicializa el UI Manager
        /// </summary>
        private bool InitializeUIManager()
        {
            _uiManager = new DeviceSelectionUIManager(_uiConfig, _uiDocument);
            return _uiManager.Initialize();
        }

        /// <summary>
        /// Inicializa el Event Manager
        /// </summary>
        private void InitializeEventManager()
        {
            _eventManager = new DeviceSelectionEventManager(
                _uiManager,
                this,
                _uiDocument,
                onLogoutRequested: () => LogDebug("Logout requested"),
                onNavigationRequested: (context) => OnContextChanged?.Invoke(CurrentNavigationContext, context),
                onDeviceSelected: (deviceId) => OnDeviceSelected?.Invoke(deviceId, CurrentDeviceMode)
            );

            _eventManager.RegisterEvents();
            LogDebug("Event Manager initialized");
        }

        /// <summary>
        /// Configura la sesión del usuario usando ServiceController
        /// </summary>
        private void SetupUserSession()
        {
            var userInfo = ServiceController.Instance?.GetUserInfo();
            if (userInfo.HasValue && userInfo.Value.isAuthenticated)
            {
                _sessionData.Username = userInfo.Value.username;
                _sessionData.IsAuthenticated = true;
                _sessionData.SessionStart = DateTime.Now;
                _sessionData.UpdateActivity();

                // Actualizar UI
                _uiManager.UpdateUsername(_sessionData.Username);

                LogDebug($"User session configured: {_sessionData.Username}");
            }
            else
            {
                LogWarning("No authenticated user found in ServiceController");
            }
        }

        /// <summary>
        /// Inicializa dispositivos por defecto
        /// </summary>
        private void InitializeDefaultDevices()
        {
            // Los dispositivos ya están inicializados en DeviceData constructor
            // Actualizar UI con dispositivos disponibles
            UpdateDeviceDisplayForCurrentMode();
            
            LogDebug($"Default devices initialized: {_deviceData.AvailableDevices.Count} devices");
        }

        #endregion

        #region Lifecycle Methods (IUIController)

        public void Show()
        {
            if (!_isInitialized)
            {
                LogError("Cannot show Device Selection - not initialized");
                return;
            }

            // Actualizar contexto antes de mostrar
            UpdateContextFromNavigationManager();
            
            // Configurar UI según contexto
            ConfigureUIForCurrentContext();

            // Mostrar UI
            _uiManager.ShowDeviceSelection();
            _sessionData.UpdateActivity();
            
            LogDebug("Device Selection shown");
        }

        public void Hide()
        {
            _uiManager.HideDeviceSelection();
            LogDebug("Device Selection hidden");
        }

        public void Cleanup()
        {
            try
            {
                LogDebug("Cleaning up Device Selection Orchestrator...");

                // Detener auto-refresh
                StopDeviceRefresh();

                // Limpiar managers
                _eventManager?.Cleanup();
                _uiManager?.Cleanup();

                // Limpiar datos
                _uiConfig = null;
                _deviceData = null;
                _contextData = null;
                _navigationState = null;
                _sessionData = null;
                _deviceSelectionState = null;

                // Limpiar referencias
                _gameManager = null;

                _isInitialized = false;
                LogDebug("Device Selection Orchestrator cleaned up");
            }
            catch (Exception ex)
            {
                LogError($"Cleanup error: {ex.Message}");
            }
        }

        #endregion

        #region Device Management (IDeviceSelectionOps)

        public List<DeviceSelectionInfo.DeviceInfo> GetAllDevices()
        {
            return _deviceData?.AvailableDevices.Values.ToList() ?? new List<DeviceSelectionInfo.DeviceInfo>();
        }

        public List<DeviceSelectionInfo.DeviceInfo> GetAvailableDevicesForCurrentMode()
        {
            return GetAvailableDevicesForMode(CurrentDeviceMode);
        }

        public List<DeviceSelectionInfo.DeviceInfo> GetAvailableDevicesForMode(DeviceMode mode)
        {
            return _deviceData?.GetAvailableDevicesForMode(mode) ?? new List<DeviceSelectionInfo.DeviceInfo>();
        }

        public bool SelectDevice(string deviceId)
        {
            if (!IsDeviceAvailable(deviceId))
            {
                LogWarning($"Cannot select device {deviceId} - not available");
                return false;
            }

            var previousDevice = _deviceData.SelectedDevice;
            _deviceData.SelectedDevice = deviceId;
            _deviceData.CurrentMode = CurrentDeviceMode;

            // Actualizar UI
            _uiManager.SelectDevice(deviceId);

            // Agregar a historial
            _sessionData.AddRecentDevice(deviceId);

            // Disparar evento
            OnDeviceSelected?.Invoke(deviceId, CurrentDeviceMode);

            LogDebug($"Device selected: {deviceId} (previous: {previousDevice})");
            return true;
        }

        public bool IsDeviceAvailable(string deviceId)
        {
            return _deviceData?.IsDeviceAvailableForMode(deviceId, CurrentDeviceMode) ?? false;
        }

        public bool IsDeviceSupportedForMode(string deviceId, DeviceMode mode)
        {
            if (!_deviceData.AvailableDevices.TryGetValue(deviceId, out var device))
                return false;
                
            return device.SupportsMode(mode);
        }

        public async Task RefreshDeviceAvailabilityAsync()
        {
            try
            {
                LogDebug("Refreshing device availability...");

                // Simular consulta a servicios reales
                await Task.Delay(500);

                // Actualizar disponibilidad (en un sistema real, consultar servicios IoT, etc.)
                UpdateDeviceAvailability();

                // Actualizar UI
                UpdateDeviceDisplayForCurrentMode();

                // Disparar evento
                OnDeviceAvailabilityChanged?.Invoke(GetAllDevices());

                LogDebug("Device availability refreshed");
            }
            catch (Exception ex)
            {
                LogError($"Error refreshing device availability: {ex.Message}");
                OnDeviceSelectionError?.Invoke("RefreshAvailability", ex.Message);
            }
        }

        /// <summary>
        /// Actualiza la disponibilidad de dispositivos (simulado)
        /// </summary>
        private void UpdateDeviceAvailability()
        {
            foreach (var device in _deviceData.AvailableDevices.Values)
            {
                // En un sistema real, consultar estado real de dispositivos
                // Por ahora, simular cambios ocasionales
                if (UnityEngine.Random.value < 0.1f) // 10% chance de cambio
                {
                    device.IsAvailable = !device.IsAvailable;
                    device.LastUpdate = DateTime.Now;
                    LogDebug($"Device {device.Id} availability changed to: {device.IsAvailable}");
                }
            }
        }

        #endregion

        #region Context and Navigation Management (IDeviceSelectionOps)

        public void ConfigureContext(NavigationContext context, Dictionary<string, object> contextData = null)
        {
            var previousContext = _contextData.CurrentContext;
            
            _contextData.CurrentContext = context;
            _contextData.RequiredMode = NavigationContextManager.Instance?.GetDeviceModeForContext(context) ?? DeviceMode.Operating;
            _contextData.UITitle = NavigationContextManager.Instance?.GetUITitleForContext(context) ?? "Select Device";
            _contextData.TargetScene = NavigationContextManager.Instance?.GetTargetSceneForContext(context) ?? "";

            if (contextData != null)
            {
                _contextData.AdditionalData.Clear();
                foreach (var kvp in contextData)
                {
                    _contextData.AdditionalData[kvp.Key] = kvp.Value;
                }
            }

            // Actualizar UI
            ConfigureUIForCurrentContext();

            // Disparar evento
            OnContextChanged?.Invoke(previousContext, context);

            LogDebug($"Context configured: {previousContext} → {context}");
        }

        public void UpdateContextFromNavigationManager()
        {
            var navManager = NavigationContextManager.Instance;
            if (navManager != null)
            {
                _contextData.ConfigureFromNavigationContext();
                LogDebug($"Context updated from NavigationManager: {_contextData.CurrentContext}");
            }
        }

        public string GetUITitleForContext(NavigationContext context)
        {
            return NavigationContextManager.Instance?.GetUITitleForContext(context) ?? "Select Device";
        }

        public string GetTargetSceneForContext(NavigationContext context)
        {
            return NavigationContextManager.Instance?.GetTargetSceneForContext(context) ?? "";
        }

        public bool IsContextValid()
        {
            return _contextData?.IsValidContext() ?? false;
        }

        /// <summary>
        /// Configura la UI según el contexto actual
        /// </summary>
        private void ConfigureUIForCurrentContext()
        {
            if (_uiManager == null || _contextData == null) return;

            // Actualizar UI con información del contexto
            _uiManager.UpdateContextUI(_contextData.CurrentContext, _contextData.RequiredMode, _contextData.UITitle);
            
            // Actualizar dispositivos disponibles según el modo actual
            UpdateDeviceDisplayForCurrentMode();
        }

        /// <summary>
        /// Actualiza la visualización de dispositivos según el modo actual
        /// </summary>
        private void UpdateDeviceDisplayForCurrentMode()
        {
            var availableDevices = GetAllDevices();
            _uiManager.UpdateDeviceDisplay(availableDevices, CurrentDeviceMode);
        }

        #endregion

        #region Navigation Menu Management (IDeviceSelectionOps)

        public void ToggleNavigationMenu(bool show)
        {
            _uiManager.ToggleNavigationMenu(show);
            _navigationState.IsMenuVisible = show;
            OnNavigationMenuToggled?.Invoke(show);
        }

        public void ToggleNavigationMenu()
        {
            ToggleNavigationMenu(!IsNavigationMenuVisible);
        }

        public async Task NavigateToSectionAsync(NavigationContext targetContext)
        {
            try
            {
                if (_isTransitioning)
                {
                    LogWarning("Navigation already in progress");
                    return;
                }

                _isTransitioning = true;
                LogDebug($"Navigating to section: {targetContext}");

                // Actualizar contexto en NavigationContextManager
                NavigationContextManager.Instance?.SetContext(targetContext);

                // Usar UIController para navegar
                var uiController = UIController.Instance;
                if (uiController != null)
                {
                    switch (targetContext)
                    {
                        case NavigationContext.Dashboard:
                            uiController.ShowUI("Dashboard");
                            break;
                        case NavigationContext.Training:
                        case NavigationContext.Operations:
                            // Estos van a DeviceSelection con contexto específico
                            ConfigureContext(targetContext);
                            break;
                        default:
                            LogWarning($"Navigation to {targetContext} not implemented yet");
                            break;
                    }
                }
                else
                {
                    LogError("UIController not found - cannot navigate");
                }

                await Task.Delay(100); // Pequeño delay para permitir transición
            }
            catch (Exception ex)
            {
                LogError($"Navigation error: {ex.Message}");
                OnDeviceSelectionError?.Invoke("Navigation", ex.Message);
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        public async Task NavigateToDashboardAsync()
        {
            await NavigateToSectionAsync(NavigationContext.Dashboard);
        }

        public async Task LogoutAsync()
        {
            try
            {
                LogDebug("Processing logout request...");

                // Limpiar datos de sesión local
                _sessionData.IsAuthenticated = false;
                _sessionData.UpdateActivity();

                // Detener procesos en curso
                StopDeviceRefresh();

                // Usar ServiceController para logout
                var cognitoManager = ServiceController.Instance?.CognitoManager;
                if (cognitoManager != null)
                {
                    cognitoManager.SignOut();
                    LogDebug("User signed out via ServiceController");
                }

                // Usar UIController para volver a Welcome
                var uiController = UIController.Instance;
                if (uiController != null)
                {
                    uiController.ShowUI("Welcome");
                }

                LogDebug("Logout completed");
            }
            catch (Exception ex)
            {
                LogError($"Error during logout: {ex.Message}");
                OnDeviceSelectionError?.Invoke("Logout", ex.Message);
            }
        }

        #endregion

        #region Device Launch and Scene Management (IDeviceSelectionOps)

        public async Task LaunchSelectedDeviceAsync()
        {
            if (string.IsNullOrEmpty(SelectedDevice))
            {
                LogWarning("No device selected for launch");
                return;
            }

            await LaunchDeviceAsync(SelectedDevice, _contextData.TargetScene);
        }

        public async Task LaunchDeviceAsync(string deviceId, string targetScene)
        {
            try
            {
                if (!ValidateDeviceLaunch(deviceId))
                {
                    LogWarning($"Device launch validation failed for: {deviceId}");
                    return;
                }

                LogDebug($"Launching device: {deviceId} → {targetScene}");

                // Preparar datos para la escena de destino
                var launchData = PrepareDeviceLaunchData(deviceId);

                // Configurar GameManager si está disponible
                if (_gameManager != null)
                {
                    _gameManager.deviceSelected = deviceId;
                    _gameManager.modeSelected = CurrentNavigationContext.ToString() + "Button";
                    _gameManager.selectedModeUiName = CurrentUITitle;
                }

                // Delay antes del lanzamiento si está configurado
                if (_launchDelay > 0)
                {
                    await Task.Delay((int)(_launchDelay * 1000));
                }

                // Validación final con event
                bool canLaunch = OnDeviceLaunchValidation?.Invoke(deviceId, targetScene) ?? true;
                if (!canLaunch)
                {
                    LogWarning("Device launch cancelled by validation event");
                    return;
                }

                // Lanzar escena
                if (!string.IsNullOrEmpty(targetScene))
                {
                    SceneManager.LoadScene(targetScene);
                    
                    // Disparar evento de lanzamiento exitoso
                    OnDeviceLaunched?.Invoke(deviceId, targetScene);
                    
                    LogDebug($"Device launched successfully: {deviceId} → {targetScene}");
                }
                else
                {
                    LogError("Target scene not specified for device launch");
                    OnDeviceSelectionError?.Invoke("DeviceLaunch", "Target scene not specified");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error launching device {deviceId}: {ex.Message}");
                OnDeviceSelectionError?.Invoke("DeviceLaunch", ex.Message);
            }
        }

        public Dictionary<string, object> PrepareDeviceLaunchData(string deviceId)
        {
            var launchData = new Dictionary<string, object>
            {
                ["deviceId"] = deviceId,
                ["deviceMode"] = CurrentDeviceMode,
                ["navigationContext"] = CurrentNavigationContext,
                ["username"] = CurrentUsername,
                ["sessionStart"] = _sessionData.SessionStart,
                ["launchTime"] = DateTime.Now
            };

            // Agregar datos específicos del dispositivo
            if (_deviceData.AvailableDevices.TryGetValue(deviceId, out var deviceInfo))
            {
                launchData["deviceType"] = deviceInfo.Type;
                launchData["deviceDisplayName"] = deviceInfo.DisplayName;
                launchData["supportedModes"] = deviceInfo.SupportedModes;
            }

            // Agregar datos adicionales del contexto
            foreach (var kvp in _contextData.AdditionalData)
            {
                launchData[$"context_{kvp.Key}"] = kvp.Value;
            }

            return launchData;
        }

        public bool ValidateDeviceLaunch(string deviceId)
        {
            // Validaciones básicas
            if (string.IsNullOrEmpty(deviceId))
            {
                LogWarning("Device ID is null or empty");
                return false;
            }

            if (!IsDeviceAvailable(deviceId))
            {
                LogWarning($"Device {deviceId} is not available");
                return false;
            }

            if (!IsContextValid())
            {
                LogWarning("Current context is not valid");
                return false;
            }

            if (string.IsNullOrEmpty(_contextData.TargetScene))
            {
                LogWarning("Target scene is not specified");
                return false;
            }

            return true;
        }

        #endregion

        #region User Session Management (IDeviceSelectionOps)

        public List<string> GetRecentlySelectedDevices()
        {
            return _sessionData?.RecentlySelectedDevices ?? new List<string>();
        }

        public void AddToRecentDevices(string deviceId)
        {
            _sessionData?.AddRecentDevice(deviceId);
        }

        public void ClearRecentDevices()
        {
            _sessionData?.RecentlySelectedDevices.Clear();
        }

        public DeviceSelectionSessionStats GetSessionStats()
        {
            if (_sessionData == null) return new DeviceSelectionSessionStats();

            var stats = new DeviceSelectionSessionStats
            {
                SessionStart = _sessionData.SessionStart,
                SessionDuration = _sessionData.GetSessionDuration(),
                DevicesViewed = _deviceData.AvailableDevices.Count,
                DevicesSelected = _sessionData.RecentlySelectedDevices.Count,
                NavigationMenuToggles = 0, // TODO: Implementar contador
                ContextHistory = new List<string> { CurrentNavigationContext.ToString() },
                MostSelectedDevice = _sessionData.RecentlySelectedDevices.FirstOrDefault(),
                CurrentContext = CurrentNavigationContext
            };

            return stats;
        }

        #endregion

        #region Utility Methods (IDeviceSelectionOps)

        public DeviceSelectionInfo.DeviceInfo GetDeviceInfo(string deviceId)
        {
            return _deviceData?.AvailableDevices.TryGetValue(deviceId, out var device) == true ? device : null;
        }

        public DeviceSelectionStatus GetCurrentStatus()
        {
            return new DeviceSelectionStatus
            {
                IsInitialized = _isInitialized,
                IsVisible = IsActive,
                CurrentContext = CurrentNavigationContext,
                CurrentMode = CurrentDeviceMode,
                SelectedDevice = SelectedDevice,
                AvailableDevicesCount = GetAvailableDevicesForCurrentMode().Count,
                TotalDevicesCount = GetAllDevices().Count,
                IsMenuVisible = IsNavigationMenuVisible,
                IsContextValid = IsContextValid(),
                LastUpdate = DateTime.Now,
                LastError = _deviceSelectionState?.LastError ?? ""
            };
        }

        public ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult();

            // Validar inicialización
            if (!_isInitialized)
                result.AddError("Device Selection not initialized");

            // Validar managers
            if (_uiManager == null)
                result.AddError("UI Manager is null");
            
            if (_eventManager == null)
                result.AddError("Event Manager is null");

            // Validar datos
            if (_deviceData?.AvailableDevices.Count == 0)
                result.AddWarning("No devices available");

            // Validar contexto
            if (!IsContextValid())
                result.AddWarning("Current context is not valid");

            // Validar UI
            if (_uiConfig?.Body == null)
                result.AddError("UI Body element not found");

            result.GenerateSummary();
            return result;
        }

        public void ResetToInitialState()
        {
            LogDebug("Resetting to initial state...");

            // Limpiar selección
            _deviceData.SelectedDevice = "";
            _uiManager.ClearDeviceSelection();

            // Cerrar menú
            ToggleNavigationMenu(false);

            // Resetear contexto
            _contextData.CurrentContext = NavigationContext.None;

            // Actualizar UI
            _uiManager.ForceUIRefresh();

            LogDebug("Reset to initial state completed");
        }

        public void ForceUIRefresh()
        {
            _uiManager?.ForceUIRefresh();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Maneja solicitud de navegación
        /// </summary>
        public async void HandleNavigationRequest(NavigationContext targetContext)
        {
            await NavigateToSectionAsync(targetContext);
        }

        /// <summary>
        /// Maneja solicitud de logout
        /// </summary>
        public async void HandleLogoutRequest()
        {
            await LogoutAsync();
        }

        /// <summary>
        /// Maneja lanzamiento de dispositivo
        /// </summary>
        public async void HandleDeviceLaunch(string deviceId)
        {
            if (_autoLaunchOnSelection)
            {
                await LaunchDeviceAsync(deviceId, _contextData.TargetScene);
            }
        }

        /// <summary>
        /// Maneja refresh de dispositivos
        /// </summary>
        public async void HandleRefreshDevices()
        {
            await RefreshDeviceAvailabilityAsync();
        }

        // Placeholders para futuras funcionalidades
        public void HandleDeviceDetailsRequest(string deviceId) 
        {
            LogDebug($"Device details requested for: {deviceId}");
            // TODO: Implementar panel de detalles de dispositivo
        }

        public void HandleDeviceHoverEnter(string deviceId) 
        {
            LogDebug($"Device hover enter: {deviceId}");
            // TODO: Implementar tooltip o preview de dispositivo
        }

        public void HandleDeviceHoverExit(string deviceId) 
        {
            LogDebug($"Device hover exit: {deviceId}");
            // TODO: Ocultar tooltip o preview
        }

        public void HandleDeviceContextMenu(string deviceId, Vector2 position) 
        {
            LogDebug($"Device context menu: {deviceId} at {position}");
            // TODO: Implementar menú contextual
        }

        #endregion

        #region Auto Refresh

        /// <summary>
        /// Inicia el refresh automático de dispositivos
        /// </summary>
        private void StartDeviceRefresh()
        {
            if (_deviceRefreshCoroutine != null)
            {
                StopCoroutine(_deviceRefreshCoroutine);
            }

            _deviceRefreshCoroutine = StartCoroutine(DeviceRefreshRoutine());
            LogDebug($"Auto-refresh started with interval: {_deviceRefreshInterval}s");
        }

        /// <summary>
        /// Detiene el refresh automático
        /// </summary>
        private void StopDeviceRefresh()
        {
            if (_deviceRefreshCoroutine != null)
            {
                StopCoroutine(_deviceRefreshCoroutine);
                _deviceRefreshCoroutine = null;
                LogDebug("Auto-refresh stopped");
            }
        }

        /// <summary>
        /// Rutina de refresh automático de dispositivos
        /// </summary>
        private IEnumerator DeviceRefreshRoutine()
        {
            while (_isInitialized && _deviceRefreshInterval > 0)
            {
                yield return new WaitForSeconds(_deviceRefreshInterval);
                
                if (IsActive && !_isTransitioning)
                {
                    RefreshDeviceAvailabilityAsync();
                }
            }
        }

        #endregion

        #region Android Specific

        /// <summary>
        /// Maneja el botón "Back" de Android
        /// </summary>
        private void Update()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    HandleAndroidBackButton();
                }
            #endif
        }

        /// <summary>
        /// Procesa el botón "Back" de Android
        /// </summary>
        private void HandleAndroidBackButton()
        {
            LogDebug("Android back button pressed");

            // Si el menú está abierto, cerrarlo
            if (IsNavigationMenuVisible)
            {
                ToggleNavigationMenu(false);
                return;
            }

            // Si hay un dispositivo seleccionado, deseleccionarlo
            if (!string.IsNullOrEmpty(SelectedDevice))
            {
                _deviceData.SelectedDevice = "";
                _uiManager.ClearDeviceSelection();
                return;
            }

            // Como último recurso, navegar de regreso al Dashboard
            HandleNavigationRequest(NavigationContext.Dashboard);
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[DeviceSelectionOrchestrator] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[DeviceSelectionOrchestrator] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[DeviceSelectionOrchestrator] {message}");
        }

        #endregion
    }
}