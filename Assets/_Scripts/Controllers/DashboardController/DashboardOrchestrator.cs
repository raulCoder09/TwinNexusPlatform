using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _Scripts.Controller;
using _Scripts.Controllers.UiManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.DashboardController
{
    /// <summary>
    /// Orchestrador principal del Dashboard
    /// Implementa IDashboardOps y coordina toda la funcionalidad del Dashboard
    /// Equivalente al WelcomeOrchestrator pero para Dashboard
    /// </summary>
    public class DashboardOrchestrator : MonoBehaviour, IDashboardOps
    {
        #region Singleton Pattern

        private static DashboardOrchestrator _instance;

        public static DashboardOrchestrator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DashboardOrchestrator>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DashboardOrchestrator");
                        _instance = go.AddComponent<DashboardOrchestrator>();
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

        #region Private Fields

        // Managers y componentes internos
        private DashboardUIManager _uiManager;
        private DashboardEventManager _eventManager;
        private UIDocument _uiDocument;
        
        // Data containers
        private DashboardInfo.UIConfiguration _uiConfig;
        private DashboardInfo.DeviceData _deviceData;
        private DashboardInfo.IoTInfrastructureData _iotData;
        private DashboardInfo.SystemActivityData _activityData;
        private DashboardInfo.UserSessionData _sessionData;
        private DashboardInfo.NavigationState _navigationState;
        private DashboardInfo.DashboardState _dashboardState;

        // Estado interno
        private bool _isInitialized = false;
        private bool _isRefreshing = false;
        private Coroutine _autoRefreshCoroutine;

        // Configuración
        [Header("Dashboard Configuration")]
        [SerializeField] private float _autoRefreshInterval = 5.0f;
        [SerializeField] private bool _enableAutoRefresh = true;
        [SerializeField] private bool _enableDebugLogs = true;

        #endregion

        #region IUIController Implementation

        public bool RequiresAuthentication => true;
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _dashboardState?.IsVisible ?? false;
        public string ControllerName => "DashboardController";

        // Events from IUIController
        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        #region IDashboardOps Implementation

        #region Properties

        public string CurrentUsername => _sessionData?.Username ?? "Unknown";
        public bool IsNavigationMenuVisible => _uiManager?.IsNavigationMenuVisible ?? false;
        public string SelectedDevice => _deviceData?.CurrentSelectedDevice ?? "None";

        #endregion

        #region Events

        public event Action<string, DeviceStatus> OnDeviceStatusChanged;
        public event Action<string> OnDeviceSelected;
        public event Action<IoTService, ConnectionStatus> OnIoTServiceStatusChanged;
        public event Action<SystemActivity> OnNewActivityLogged;
        public event Action<DashboardSection> OnSectionNavigated;
        public event Action OnUserLoggedOut;

        #endregion

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // La inicialización se hace externamente por UIController
            // Start solo verifica que todo esté listo
            if (!_isInitialized)
            {
                LogDebug("Dashboard not initialized, waiting for external initialization");
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
        /// Inicializa el Dashboard Orchestrator
        /// </summary>
        public bool Initialize()
        {
            try
            {
                LogDebug("Initializing Dashboard Orchestrator...");

                if (_isInitialized)
                {
                    LogDebug("Dashboard already initialized");
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

                // Inicializar datos por defecto
                InitializeDefaultData();

                // Configurar auto-refresh si está habilitado
                if (_enableAutoRefresh)
                {
                    StartAutoRefresh();
                }

                _isInitialized = true;
                LogDebug("Dashboard Orchestrator initialized successfully");
                
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
            _uiConfig = new DashboardInfo.UIConfiguration();
            _deviceData = new DashboardInfo.DeviceData();
            _iotData = new DashboardInfo.IoTInfrastructureData();
            _activityData = new DashboardInfo.SystemActivityData();
            _sessionData = new DashboardInfo.UserSessionData();
            _navigationState = new DashboardInfo.NavigationState();
            _dashboardState = new DashboardInfo.DashboardState();

            // Configurar eventos internos
            _dashboardState.OnDashboardInitialized += () => LogDebug("Dashboard state initialized");
            _dashboardState.OnDashboardShown += () => OnControllerShown?.Invoke(this);
            _dashboardState.OnDashboardHidden += () => OnControllerHidden?.Invoke(this);
            _dashboardState.OnError += (error) => OnControllerError?.Invoke(this, error);

            LogDebug("Data containers initialized");
        }

        /// <summary>
        /// Inicializa el UI Manager
        /// </summary>
        private bool InitializeUIManager()
        {
            _uiManager = new DashboardUIManager(_uiConfig, _uiDocument);
            return _uiManager.Initialize();
        }

        /// <summary>
        /// Inicializa el Event Manager
        /// </summary>
        private void InitializeEventManager()
        {
            _eventManager = new DashboardEventManager(
                _uiManager,
                this,
                _uiDocument,
                onLogoutRequested: () => OnUserLoggedOut?.Invoke(),
                onNavigationRequested: (section) => OnSectionNavigated?.Invoke(section),
                onDeviceSelectionChanged: (device) => OnDeviceSelected?.Invoke(device)
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
                _sessionData.UserGroup = userInfo.Value.userGroup;
                _sessionData.IsAuthenticated = true;
                _sessionData.LoginTime = DateTime.Now;
                _sessionData.UpdateActivity();

                LogDebug($"User session configured: {_sessionData.Username}");
            }
            else
            {
                LogWarning("No authenticated user found in ServiceController");
            }
        }

        /// <summary>
        /// Inicializa datos por defecto del sistema
        /// </summary>
        private void InitializeDefaultData()
        {
            // Inicializar dispositivos por defecto
            InitializeDefaultDevices();

            // Inicializar actividad por defecto
            InitializeDefaultActivity();

            // Actualizar UI con datos iniciales
            UpdateUIWithCurrentData();

            LogDebug("Default data initialized");
        }

        /// <summary>
        /// Inicializa dispositivos por defecto
        /// </summary>
        private void InitializeDefaultDevices()
        {
            var devices = new[]
            {
                new DashboardInfo.DeviceInfo("ARSCARA") { Status = DeviceStatus.Online },
                new DashboardInfo.DeviceInfo("Robotics kit 1") { Status = DeviceStatus.Online },
                new DashboardInfo.DeviceInfo("Robotics kit 2") { Status = DeviceStatus.Offline },
                new DashboardInfo.DeviceInfo("Device 3") { Status = DeviceStatus.Offline }
            };

            foreach (var device in devices)
            {
                _deviceData.Devices[device.Name] = device;
            }

            _deviceData.CurrentSelectedDevice = "ARSCARA";
        }

        /// <summary>
        /// Inicializa actividad por defecto del sistema
        /// </summary>
        private void InitializeDefaultActivity()
        {
            var defaultActivities = new[]
            {
                new SystemActivity("ARSCARA", "simulation started", "at 10:05 am", ActivityType.Info),
                new SystemActivity("ARSCARA", "monitoring stopped", "at 9:45 am", ActivityType.Warning),
                new SystemActivity("ARSCARA", "report generated", "at 9:30 am", ActivityType.Success)
            };

            foreach (var activity in defaultActivities)
            {
                _activityData.AddActivity(activity);
            }
        }

        #endregion

        #region Lifecycle Methods (IUIController)

        public void Show()
        {
            if (!_isInitialized)
            {
                LogError("Cannot show Dashboard - not initialized");
                return;
            }

            _uiManager.ShowDashboard();
            _sessionData.UpdateActivity();
            
            LogDebug("Dashboard shown");
        }

        public void Hide()
        {
            _uiManager.HideDashboard();
            LogDebug("Dashboard hidden");
        }

        public void Cleanup()
        {
            try
            {
                LogDebug("Cleaning up Dashboard Orchestrator...");

                // Detener auto-refresh
                StopAutoRefresh();

                // Limpiar managers
                _eventManager?.Cleanup();
                _uiManager?.Cleanup();

                // Limpiar datos
                _uiConfig = null;
                _deviceData = null;
                _iotData = null;
                _activityData = null;
                _sessionData = null;
                _navigationState = null;
                _dashboardState = null;

                _isInitialized = false;
                LogDebug("Dashboard Orchestrator cleaned up");
            }
            catch (Exception ex)
            {
                LogError($"Cleanup error: {ex.Message}");
            }
        }

        #endregion

        #region Device Management (IDashboardOps)

        public async Task<bool> RefreshDeviceStatusAsync()
        {
            if (_isRefreshing)
            {
                LogDebug("Device refresh already in progress");
                return false;
            }

            try
            {
                _isRefreshing = true;
                LogDebug("Refreshing device status...");

                // Simular consulta a servicios reales (IoT, ServiceController, etc.)
                await Task.Delay(500); // Simular latencia de red

                // Actualizar estados de dispositivos
                UpdateDeviceStatuses();

                // Actualizar UI
                UpdateDeviceUI();

                // Registrar actividad
                LogActivity(new SystemActivity("System", "device status refresh", "completed", ActivityType.Info));

                LogDebug("Device status refreshed successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error refreshing device status: {ex.Message}");
                return false;
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        public void SelectDevice(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName) || !_deviceData.Devices.ContainsKey(deviceName))
            {
                LogWarning($"Invalid device name: {deviceName}");
                return;
            }

            var previousDevice = _deviceData.CurrentSelectedDevice;
            _deviceData.CurrentSelectedDevice = deviceName;

            // Actualizar UI
            var deviceInfo = _deviceData.Devices[deviceName];
            _uiManager.UpdateSelectedDevice(deviceInfo);

            // Registrar actividad
            LogActivity(new SystemActivity(deviceName, "device selected", $"switched from {previousDevice}", ActivityType.Info));

            // Disparar evento
            OnDeviceSelected?.Invoke(deviceName);

            LogDebug($"Device selected: {deviceName}");
        }

        public List<string> GetAvailableDevices()
        {
            return _deviceData.DeviceOrder.Where(name => _deviceData.Devices.ContainsKey(name)).ToList();
        }

        public DeviceStatus GetDeviceStatus(string deviceName)
        {
            return _deviceData.Devices.TryGetValue(deviceName, out var device) ? device.Status : DeviceStatus.Unknown;
        }

        /// <summary>
        /// Actualiza los estados de dispositivos (simulado)
        /// </summary>
        private void UpdateDeviceStatuses()
        {
            foreach (var kvp in _deviceData.Devices)
            {
                var device = kvp.Value;
                var previousStatus = device.Status;

                // Simular cambios de estado aleatorios para demo
                if (UnityEngine.Random.value < 0.1f) // 10% chance de cambio
                {
                    device.Status = (DeviceStatus)UnityEngine.Random.Range(1, 4); // Online, Offline, Busy
                    device.LastUpdate = DateTime.Now;

                    if (device.Status != previousStatus)
                    {
                        OnDeviceStatusChanged?.Invoke(device.Name, device.Status);
                        LogActivity(new SystemActivity(device.Name, "status changed", $"from {previousStatus} to {device.Status}", ActivityType.Info));
                    }
                }
            }
        }

        /// <summary>
        /// Actualiza la UI con información actual de dispositivos
        /// </summary>
        private void UpdateDeviceUI()
        {
            // Actualizar dispositivo seleccionado
            if (_deviceData.Devices.TryGetValue(_deviceData.CurrentSelectedDevice, out var selectedDevice))
            {
                _uiManager.UpdateSelectedDevice(selectedDevice);
            }

            // Actualizar lista de estado del sistema
            _uiManager.UpdateSystemDevicesList(_deviceData.Devices);
        }

        #endregion

        #region IoT Infrastructure (IDashboardOps)

        public async Task RefreshIoTInfrastructureAsync()
        {
            try
            {
                LogDebug("Refreshing IoT infrastructure...");

                // Simular consulta a servicios IoT reales
                await Task.Delay(300);

                // Actualizar estados IoT (simulado)
                UpdateIoTServiceStatuses();

                // Actualizar UI
                _uiManager.UpdateIoTServicesStatus(_iotData.Services);

                LogDebug("IoT infrastructure refreshed");
            }
            catch (Exception ex)
            {
                LogError($"Error refreshing IoT infrastructure: {ex.Message}");
            }
        }

        public Dictionary<IoTService, ConnectionStatus> GetIoTServicesStatus()
        {
            return _iotData.Services.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Status);
        }

        public async Task<bool> SetIoTServiceModeAsync(IoTService service, OperationMode mode)
        {
            try
            {
                LogDebug($"Setting {service} mode to {mode}");

                if (!_iotData.Services.TryGetValue(service, out var serviceInfo))
                {
                    LogWarning($"IoT service not found: {service}");
                    return false;
                }

                // Simular cambio de modo
                await Task.Delay(200);

                var previousMode = serviceInfo.Mode;
                serviceInfo.Mode = mode;
                serviceInfo.LastUpdate = DateTime.Now;

                // Actualizar UI
                _uiManager.UpdateIoTServicesStatus(_iotData.Services);

                // Registrar actividad
                LogActivity(new SystemActivity(service.ToString(), "mode changed", $"from {previousMode} to {mode}", ActivityType.Info));

                LogDebug($"IoT service mode updated: {service} = {mode}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error setting IoT service mode: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Actualiza estados de servicios IoT (simulado)
        /// </summary>
        private void UpdateIoTServiceStatuses()
        {
            foreach (var kvp in _iotData.Services)
            {
                var service = kvp.Key;
                var serviceInfo = kvp.Value;
                var previousStatus = serviceInfo.Status;

                // Simular cambios de estado
                if (UnityEngine.Random.value < 0.05f) // 5% chance de cambio
                {
                    var statuses = new[] { ConnectionStatus.Connected, ConnectionStatus.Disconnected, ConnectionStatus.Error };
                    serviceInfo.Status = statuses[UnityEngine.Random.Range(0, statuses.Length)];
                    serviceInfo.LastUpdate = DateTime.Now;

                    if (serviceInfo.Status != previousStatus)
                    {
                        OnIoTServiceStatusChanged?.Invoke(service, serviceInfo.Status);
                        LogActivity(new SystemActivity(service.ToString(), "connection status", $"changed to {serviceInfo.Status}", ActivityType.Info));
                    }
                }
            }
        }

        #endregion

        #region System Activity (IDashboardOps)

        public List<SystemActivity> GetRecentActivity(int maxItems = 10)
        {
            return _activityData.GetRecentActivities(maxItems);
        }

        public void LogActivity(SystemActivity activity)
        {
            if (activity == null) return;

            _activityData.AddActivity(activity);
            _uiManager.UpdateRecentActivities(_activityData.GetRecentActivities(5));
            OnNewActivityLogged?.Invoke(activity);

            LogDebug($"Activity logged: {activity}");
        }

        public void ClearActivityHistory()
        {
            _activityData.ClearHistory();
            _uiManager.UpdateRecentActivities(_activityData.GetRecentActivities(5));
            LogDebug("Activity history cleared");
        }

        #endregion

        #region Navigation (IDashboardOps)

        public void ToggleNavigationMenu(bool show)
        {
            _uiManager.ToggleNavigationMenu(show);
        }

        public void NavigateToSection(DashboardSection section)
        {
            var previousSection = _navigationState.CurrentSection;
            _navigationState.NavigateTo(section);
            
            _uiManager.UpdateActiveNavigationSection(section);
            
            LogActivity(new SystemActivity("System", "navigation", $"navigated to {section}", ActivityType.Info));
            OnSectionNavigated?.Invoke(section);
            
            LogDebug($"Navigated to section: {section} (from {previousSection})");
        }

        public async Task LogoutAsync()
        {
            try
            {
                LogDebug("Processing logout request...");

                // Registrar actividad de logout
                LogActivity(new SystemActivity("System", "user logout", $"user {CurrentUsername} logged out", ActivityType.Info));

                // Limpiar datos de sesión local
                _sessionData.IsAuthenticated = false;
                _sessionData.UpdateActivity();

                // Cerrar cualquier proceso en curso
                StopAutoRefresh();

                // Usar ServiceController para logout
                var cognitoManager = ServiceController.Instance?.CognitoManager;
                if (cognitoManager != null)
                {
                    cognitoManager.SignOut();
                    LogDebug("User signed out via ServiceController");
                }

                // Disparar evento
                OnUserLoggedOut?.Invoke();

                LogDebug("Logout completed");
            }
            catch (Exception ex)
            {
                LogError($"Error during logout: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Maneja navegación hacia el dispositivo anterior
        /// </summary>
        public void HandleDeviceNavigationLeft()
        {
            var devices = GetAvailableDevices();
            var currentIndex = devices.IndexOf(_deviceData.CurrentSelectedDevice);
            
            if (currentIndex > 0)
            {
                SelectDevice(devices[currentIndex - 1]);
            }
            else if (devices.Count > 0)
            {
                SelectDevice(devices[devices.Count - 1]); // Wrap around
            }
        }

        /// <summary>
        /// Maneja navegación hacia el siguiente dispositivo
        /// </summary>
        public void HandleDeviceNavigationRight()
        {
            var devices = GetAvailableDevices();
            var currentIndex = devices.IndexOf(_deviceData.CurrentSelectedDevice);
            
            if (currentIndex < devices.Count - 1)
            {
                SelectDevice(devices[currentIndex + 1]);
            }
            else if (devices.Count > 0)
            {
                SelectDevice(devices[0]); // Wrap around
            }
        }

        /// <summary>
        /// Maneja click en visualización de dispositivo
        /// </summary>
        public void HandleDeviceVisualizationClick()
        {
            LogDebug($"Device visualization clicked for: {SelectedDevice}");
            // Aquí se puede implementar lógica específica como:
            // - Mostrar panel de control avanzado
            // - Cambiar modo de visualización
            // - Abrir detalles del dispositivo
        }

        /// <summary>
        /// Maneja solicitud de navegación
        /// </summary>
        public void HandleNavigationRequest(DashboardSection section)
        {
            NavigateToSection(section);
        }

        /// <summary>
        /// Maneja solicitud de logout
        /// </summary>
        public async void HandleLogoutRequest()
        {
            await LogoutAsync();
        }

        /// <summary>
        /// Maneja solicitud de refresh
        /// </summary>
        public async void HandleRefreshRequest()
        {
            LogDebug("Manual refresh requested");
            await RefreshDeviceStatusAsync();
            await RefreshIoTInfrastructureAsync();
        }

        /// <summary>
        /// Maneja interacción con servicio IoT
        /// </summary>
        public void HandleIoTServiceInteraction(IoTService service)
        {
            LogDebug($"IoT service interaction: {service}");
            // Implementar lógica específica de interacción
        }

        /// <summary>
        /// Maneja solicitud de detalles de actividad
        /// </summary>
        public void HandleActivityDetailsRequest(SystemActivity activity)
        {
            LogDebug($"Activity details requested: {activity.Action}");
            // Implementar mostrar detalles de actividad
        }

        #endregion

        #region Auto Refresh

        /// <summary>
        /// Inicia el refresh automático
        /// </summary>
        private void StartAutoRefresh()
        {
            if (_autoRefreshCoroutine != null)
            {
                StopCoroutine(_autoRefreshCoroutine);
            }

            _autoRefreshCoroutine = StartCoroutine(AutoRefreshRoutine());
            LogDebug($"Auto-refresh started with interval: {_autoRefreshInterval}s");
        }

        /// <summary>
        /// Detiene el refresh automático
        /// </summary>
        private void StopAutoRefresh()
        {
            if (_autoRefreshCoroutine != null)
            {
                StopCoroutine(_autoRefreshCoroutine);
                _autoRefreshCoroutine = null;
                LogDebug("Auto-refresh stopped");
            }
        }

        /// <summary>
        /// Rutina de refresh automático
        /// </summary>
        private IEnumerator AutoRefreshRoutine()
        {
            while (_enableAutoRefresh && _isInitialized)
            {
                yield return new WaitForSeconds(_autoRefreshInterval);
                
                if (_dashboardState.IsVisible && !_isRefreshing)
                {
                    RefreshDeviceStatusAsync();
                    RefreshIoTInfrastructureAsync();
                }
            }
        }

        #endregion

        #region UI Data Update

        /// <summary>
        /// Actualiza toda la UI con datos actuales
        /// </summary>
        private void UpdateUIWithCurrentData()
        {
            // Actualizar información de usuario
            _uiManager.UpdateUsername(_sessionData.Username);
            _uiManager.UpdateWelcomeMessage(_sessionData.GetWelcomeMessage());

            // Actualizar dispositivo seleccionado
            if (_deviceData.Devices.TryGetValue(_deviceData.CurrentSelectedDevice, out var selectedDevice))
            {
                _uiManager.UpdateSelectedDevice(selectedDevice);
            }

            // Actualizar servicios IoT
            _uiManager.UpdateIoTServicesStatus(_iotData.Services);

            // Actualizar actividades recientes
            _uiManager.UpdateRecentActivities(_activityData.GetRecentActivities(5));

            LogDebug("UI updated with current data");
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[DashboardOrchestrator] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[DashboardOrchestrator] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[DashboardOrchestrator] {message}");
        }

        #endregion
    }
}