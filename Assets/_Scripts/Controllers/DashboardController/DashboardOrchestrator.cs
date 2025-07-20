using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controller;
using _Scripts.Controllers.SettingsController;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.DashboardController
{
    /// <summary>
    /// Coordinador principal del Dashboard - implementa IDashboardOps e IUIController
    /// Equivalente a WelcomeOrchestrator pero para Dashboard
    /// </summary>
    public class DashboardOrchestrator : MonoBehaviour, IDashboardOps, IUIController
    {
        #region IUIController Implementation

        public bool RequiresAuthentication => true; // Dashboard SÍ requiere autenticación
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _uiConfig?.Body?.style.display == DisplayStyle.Flex;
        public string ControllerName => "DashboardController";

        // Events from IUIController
        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        #region IDashboardOps Implementation

        public bool IsNavigationMenuOpen => _uiManager?.NavigationMenuOpen ?? false;
        public IDashboardOps.PanelType CurrentActivePanel => _uiManager?.CurrentActivePanel ?? IDashboardOps.PanelType.None;

        // Events from IDashboardOps
        public event Action OnNavigationMenuOpened;
        public event Action OnNavigationMenuClosed;
        public event Action<IDashboardOps.PanelType> OnPanelTransitionComplete;
        public event Action OnLogoutRequested;
        public event Action<DashboardInfo.ConnectionStatus, DashboardInfo.ConnectionStatus, DashboardInfo.ConnectionStatus> OnIoTStatusChanged;

        #endregion

        #region Private Fields

        private DashboardInfo.UIConfiguration _uiConfig = new DashboardInfo.UIConfiguration();
        private DashboardInfo.UserData _userData = new DashboardInfo.UserData();
        private DashboardInfo.DashboardState _dashboardState = new DashboardInfo.DashboardState();
        
        private DashboardUIManager _uiManager;
        private DashboardEventManager _eventManager;
        
        // Referencias a otros controladores
        private UIController _mainUIController;
        private SettingsOrchestrator _settingsOrchestrator;
        private GameManager _gameManager;
        
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private UIDocument _uiDocument;
        private bool _isInitialized = false;
        
        private Label _localIoTStatusLabel;
        private Label _localIoTModeLabel;
        private Label _vMIoTStatusLabel;
        private Label _vMIoTModeLabel;
        private Label _cloudIoTStatusLabel;
        private Label _cloudIoTModeLabel;
        
        private Label _cognitoStatusLabel;
        private Label _cloudwatchStatusLabel;
        private Label _sesStatusLabel;
        private Label _iotCoreStatusLabel;
        private Label _s3StatusLabel;
        private Label _lambdaStatusLabel;
        private Label _ec2StatusLabel;
        private Label _auroraStatusLabel;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Verificar si ya hay una instancia (no singleton como Welcome)
            Initialize();
        }
        
        private void Start()
        {
            // CRÍTICO: Dashboard inicia OCULTO hasta autenticación
            HideUi();
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            
            Debug.Log("[DashboardOrchestrator] Started - UI hidden until authentication");
        }
        
        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region IUIController Lifecycle Methods

        public bool Initialize()
        {
            try
            {
                if (_isInitialized)
                {
                    Debug.Log("DashboardOrchestrator already initialized");
                    return true;
                }

                Debug.Log("Initializing DashboardOrchestrator...");

                // Obtener UIDocument y root
                _uiDocument = GetComponent<UIDocument>();
                if (_uiDocument == null)
                {
                    Debug.LogError("UIDocument component not found!");
                    return false;
                }

                var root = _uiDocument.rootVisualElement;
                if (root == null)
                {
                    Debug.LogError("Root visual element is null!");
                    return false;
                }

                _subpanelsAndSmokeMaskContainer = root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
                if (_subpanelsAndSmokeMaskContainer == null)
                {
                    Debug.LogError("SubpanelsAndSmokeMaskContainer not found in UI!");
                    return false;
                }

                // Obtener referencias UI, incluyendo las nuevas etiquetas
                GetUiComponents(root);
                
                // Inicializar managers
                _uiManager = new DashboardUIManager(_uiConfig);
                _eventManager = new DashboardEventManager(_uiManager, OnLogoutRequestedHandler, OnPanelTransitionCompleteHandler, this);
                _eventManager.RegisterEvents(_uiDocument);

                // Inicializar paneles
                _uiManager.InitializePanelSystem();

                // Buscar dependencias
                FindDependencies();

                // Inicializar estado
                InitializeDashboardState();

                // Suscribirse a eventos de ServiceController
                SubscribeToServiceControllerEvents();

                _isInitialized = true;
                Debug.Log("✅ DashboardOrchestrator initialized successfully");
                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"DashboardOrchestrator initialization error: {ex.Message}");
                OnControllerError?.Invoke(this, $"Initialization failed: {ex.Message}");
                return false;
            }
        }
        
        
        
        
        private void SubscribeToServiceControllerEvents()
        {
            var serviceController = ServiceController.Instance;
            if (serviceController != null)
            {
                serviceController.OnCognitoServiceReady += OnCognitoServiceReady;
                serviceController.OnServiceError += OnServiceControllerError;
                serviceController.OnAWSServicesInitialized += OnAWSServicesInitialized;
                serviceController.OnServiceActivated += OnServiceActivated;
                Debug.Log("Subscribed to ServiceController events");
            }
        }
        

        public void Show()
        {
            // SEGURIDAD: Verificar autenticación antes de mostrar
            if (!ServiceController.Instance.IsCognitoAuthenticated)
            {
                Debug.LogError("Cannot show Dashboard - user not authenticated");
                OnControllerError?.Invoke(this, "Authentication required");
                
                // Redirigir a Welcome
                _mainUIController?.ShowUI("Welcome");
                return;
            }

            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.Flex;
                UpdateUserData();
                UpdateServiceStatuses(); 
                OnControllerShown?.Invoke(this);
                Debug.Log("[DashboardOrchestrator] Dashboard UI shown");
            }
        }

        public void Hide()
        {
            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.None;
                _uiManager?.CloseCurrentPanel();
                OnControllerHidden?.Invoke(this);
            }
        }

        public void Cleanup()
        {
            try
            {
                // Limpiar managers
                _eventManager?.Cleanup();
                _uiManager = null;
                _eventManager = null;

                // Limpiar referencias
                _mainUIController = null;
                _settingsOrchestrator = null;
                _gameManager = null;
                _uiConfig = null;
                _userData = null;
                _dashboardState = null;
                _uiDocument = null;
                _subpanelsAndSmokeMaskContainer = null;

                _isInitialized = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DashboardOrchestrator] Cleanup error: {ex.Message}");
            }
        }

        #endregion

        #region IDashboardOps Implementation

        public void NavigateToPanel(IDashboardOps.PanelType panelType)
        {
            if (!_isInitialized) Initialize();
            if (_uiManager == null)
            {
                Debug.LogError("UI manager not initialized");
                return;
            }

            _uiManager.ShowPanel(panelType);
        }

        public void CloseCurrentPanel()
        {
            _uiManager?.CloseCurrentPanel();
        }

        public void SwitchPanel(IDashboardOps.PanelType fromPanel, IDashboardOps.PanelType toPanel)
        {
            _uiManager?.SwitchPanel(fromPanel, toPanel);
        }

        public void ShowNavigationMenu()
        {
            _uiManager?.ShowNavigationMenu();
            OnNavigationMenuOpened?.Invoke();
        }

        public void HideNavigationMenu()
        {
            _uiManager?.HideNavigationMenu();
            OnNavigationMenuClosed?.Invoke();
        }

        public void StartOperations()
        {
            HandleOperationsClick();
        }

        public void StartTraining()
        {
            HandleTrainingClick();
        }

        public void OpenSettings()
        {
            HandleSettingsClick();
        }

        public void Logout()
        {
            HandleLogoutClick();
        }

        #endregion

        #region Public Event Handlers (Called by EventManager)
        /// <summary>
        /// Maneja clic en botón Reports
        /// </summary>
        public void HandleReportsClick()
        {
            Debug.Log("Reports button clicked - opening Reports Center");
    
            // Cerrar menú y ocultar dashboard
            _uiManager?.HideNavigationMenu();
            Hide();
    
            // Mostrar reports controller
            _mainUIController?.ShowUI("Reports");
        }

        /// <summary>
        /// Maneja clic en botón Operations
        /// </summary>
        public void HandleOperationsClick()
        {
            var parameters = new Dictionary<string, object> {
                ["context"] = "Operations", 
                ["sourceController"] = "Dashboard"
            };
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }

        /// <summary>
        /// Maneja clic en botón Training
        /// </summary>
        public void HandleTrainingClick()
        {
            var parameters = new Dictionary<string, object> {
                ["context"] = "Training",
                ["sourceController"] = "Dashboard"
            };
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }

        /// <summary>
        /// Maneja clic en botón Settings
        /// </summary>
        public void HandleSettingsClick()
        {
            _uiManager?.HideNavigationMenu();
            Hide();
            
            // Mostrar settings controller
            if (_settingsOrchestrator != null)
            {
                _settingsOrchestrator.ShowUi();
            }
            else
            {
                _mainUIController?.ShowUI("Settings");
            }
        }

        /// <summary>
        /// Maneja clic en botón Support
        /// </summary>
        public void HandleSupportClick()
        {
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Support");
        }
        /// <summary>
        /// Maneja clic en botón Logout
        /// </summary>
        public void HandleLogoutClick()
        {
            _uiManager?.HideNavigationMenu();
            Hide();
            var uiController = UIController.Instance;
            if (uiController != null)
            {
                uiController.RequestLogout();
            }
            else
            {
                ServiceController.Instance?.CognitoManager?.SignOut();
            }
            OnLogoutRequested?.Invoke();
        }

        #endregion

        #region Private Implementation Methods

        /// <summary>
        /// Obtiene componentes UI del Dashboard
        /// </summary>
        private void GetUiComponents(VisualElement root)
        {
            Debug.Log("Getting UI components...");
    
            // Contenedores principales
            Debug.Log("Getting Body...");
            _uiConfig.Body = root.Q<VisualElement>("Body");
            Debug.Log($"Body found: {_uiConfig.Body != null}");
    
            Debug.Log("Setting SubpanelsContainer...");
            _uiConfig.SubpanelsContainer = _subpanelsAndSmokeMaskContainer;
            Debug.Log($"SubpanelsContainer set: {_uiConfig.SubpanelsContainer != null}");
    
            Debug.Log("Getting Scrim...");
            _uiConfig.Scrim = _subpanelsAndSmokeMaskContainer?.Q<VisualElement>("Scrim");
            Debug.Log($"Scrim found: {_uiConfig.Scrim != null}");
    
            Debug.Log("Getting MainContentArea...");
            _uiConfig.MainContentArea = root.Q<VisualElement>("MainContentArea");
            Debug.Log($"MainContentArea found: {_uiConfig.MainContentArea != null}");
    
            Debug.Log("Getting StatusBar...");
            _uiConfig.StatusBar = root.Q<VisualElement>("StatusBar");
            Debug.Log($"StatusBar found: {_uiConfig.StatusBar != null}");

            // Referencias a Labels de IoT (del código original)
            Debug.Log("Getting IoT Status Labels...");
            _localIoTStatusLabel = root.Q<Label>("LocalIoTStatusLabel");
            _localIoTModeLabel = root.Q<Label>("LocalIoTModeLabel");
            _vMIoTStatusLabel = root.Q<Label>("VMIoTStatusLabel");
            _vMIoTModeLabel = root.Q<Label>("VMIoTModeLabel");
            _cloudIoTStatusLabel = root.Q<Label>("CloudIoTStatusLabel");
            _cloudIoTModeLabel = root.Q<Label>("CloudlIoTModeLabel");
    
            Debug.Log($"IoT Labels found - Local: {_localIoTStatusLabel != null}, VM: {_vMIoTStatusLabel != null}, Cloud: {_cloudIoTStatusLabel != null}");

            
            // Configurar paneles
            Debug.Log("Initializing panel configuration...");
            _cognitoStatusLabel = root.Q<Label>("CognitoStatusLabel");
            _cloudwatchStatusLabel = root.Q<Label>("CloudwatchStatusLabel");
            _sesStatusLabel = root.Q<Label>("SESStatusLabel");
            _iotCoreStatusLabel = root.Q<Label>("IotCoreStatusLabel");
            _s3StatusLabel = root.Q<Label>("S3StatusLabel");
            _lambdaStatusLabel = root.Q<Label>("LambdaStatusLabel");
            _ec2StatusLabel = root.Q<Label>("EC2StatusLabel");
            _auroraStatusLabel = root.Q<Label>("AuroraStatusLabel");
            InitializePanelConfiguration(root);
    
            Debug.Log("✅ Dashboard UI components obtained successfully");
        }
        
        private void OnCognitoServiceReady()
        {
            Debug.Log("Cognito service ready - updating service statuses");
            UpdateServiceStatuses();
        }

        private void OnAWSServicesInitialized()
        {
            Debug.Log("AWS services initialized - updating service statuses");
            UpdateServiceStatuses();
        }

        private void OnServiceActivated(string serviceName)
        {
            Debug.Log($"Service {serviceName} activated - updating status");
            UpdateServiceStatuses();
        }

        private void OnServiceControllerError(string serviceName, string error)
        {
            Debug.LogError($"ServiceController error in {serviceName}: {error}");
            UpdateServiceStatuses(); // Actualizar para reflejar posibles errores
        }
        
        private void UpdateServiceStatuses()
        {
            var serviceController = ServiceController.Instance;
            if (serviceController == null) return;

            // Actualizar cada etiqueta según el estado del servicio
            UpdateServiceStatusLabel(_cognitoStatusLabel, "Cognito", serviceController.IsServiceAvailable("Cognito"));
            UpdateServiceStatusLabel(_cloudwatchStatusLabel, "Cloudwatch", serviceController.IsServiceAvailable("CloudWatch"));
            UpdateServiceStatusLabel(_sesStatusLabel, "SES", serviceController.IsServiceAvailable("SES"));
            UpdateServiceStatusLabel(_iotCoreStatusLabel, "IoT Core", serviceController.IsServiceAvailable("IoT"));
            UpdateServiceStatusLabel(_s3StatusLabel, "S3", serviceController.IsServiceAvailable("S3"));
            UpdateServiceStatusLabel(_lambdaStatusLabel, "Lambda", serviceController.IsServiceAvailable("Lambda"));
            UpdateServiceStatusLabel(_ec2StatusLabel, "EC2", false); // EC2 no está implementado
            UpdateServiceStatusLabel(_auroraStatusLabel, "Aurora", false); // Aurora no está implementado
        }

        private void UpdateServiceStatusLabel(Label label, string serviceName, bool isAvailable)
        {
            if (label == null) return;

            label.text = $"{serviceName} status: {(isAvailable ? "Active" : "Inactive")}";

            // Actualizar clases USS para estilos
            label.RemoveFromClassList("status-active");
            label.RemoveFromClassList("status-inactive");

            if (isAvailable)
            {
                label.AddToClassList("status-active");
            }
            else
            {
                label.AddToClassList("status-inactive");
            }
        }

        /// <summary>
        /// Inicializa la configuración de paneles
        /// </summary>
        /// <summary>
/// Inicializa la configuración de paneles
/// </summary>
private void InitializePanelConfiguration(VisualElement root)
{
    var panelsContainer = _subpanelsAndSmokeMaskContainer;
    
    // Panel de menú de navegación (SÍ existe en tu UXML)
    _uiConfig.Panels[IDashboardOps.PanelType.NavigationMenu] = new DashboardInfo.UIConfiguration.PanelData
    {
        Panel = panelsContainer?.Q<VisualElement>("NavigationMenuPanel"),
        ShowClass = "NavigationMenuPanelinMainScreen",
        HideClass = "NavigationMenuPanelOutMainScreen", // 👈 CORREGIDO: usa la clase del USS
        RequiresScrim = true,
        AnimationDuration = 0.3f
    };

    // Paneles futuros (NO existen en UXML actual - Panel será null)
    _uiConfig.Panels[IDashboardOps.PanelType.Notifications] = new DashboardInfo.UIConfiguration.PanelData
    {
        Panel = null, // 👈 SERÁ NULL porque no existe en UXML
        ShowClass = "NotificationsPanelVisible",
        HideClass = "NotificationsPanelHidden",
        IsModal = true,
        RequiresScrim = true
    };

    _uiConfig.Panels[IDashboardOps.PanelType.UserProfile] = new DashboardInfo.UIConfiguration.PanelData
    {
        Panel = null, // 👈 SERÁ NULL porque no existe en UXML
        ShowClass = "UserProfilePanelVisible",
        HideClass = "UserProfilePanelHidden",
        IsModal = true,
        RequiresScrim = true
    };

    _uiConfig.Panels[IDashboardOps.PanelType.QuickActions] = new DashboardInfo.UIConfiguration.PanelData
    {
        Panel = null, // 👈 SERÁ NULL porque no existe en UXML
        ShowClass = "QuickActionsPanelVisible",
        HideClass = "QuickActionsPanelHidden",
        IsModal = true,
        RequiresScrim = true
    };

    _uiConfig.Panels[IDashboardOps.PanelType.StatusOverlay] = new DashboardInfo.UIConfiguration.PanelData
    {
        Panel = null, // 👈 SERÁ NULL porque no existe en UXML
        ShowClass = "StatusOverlayPanelVisible",
        HideClass = "StatusOverlayPanelHidden",
        IsModal = true,
        RequiresScrim = false
    };

    // Registrar callbacks SOLO para paneles que existen
    foreach (var kvp in _uiConfig.Panels)
    {
        var panelData = kvp.Value;
        if (panelData.Panel != null) // 👈 VERIFICAR null
        {
            Debug.Log($"Registering callback for panel: {kvp.Key}");
            panelData.Panel.RegisterCallback<TransitionEndEvent>(OnTransitionEndEvent);
        }
        else
        {
            Debug.Log($"Panel {kvp.Key} not found in UI - skipping callback registration");
        }
    }
}

        /// <summary>
        /// Busca dependencias en la escena
        /// </summary>
        private void FindDependencies()
        {
            // Buscar UIController principal
            _mainUIController = UIController.Instance;
            if (_mainUIController == null)
            {
                Debug.LogWarning("UIController not found - will try to find it later");
            }

            // Buscar otros controladores (del código original)
            _settingsOrchestrator = FindObjectOfType<SettingsOrchestrator>();
            _gameManager = FindObjectOfType<GameManager>();
            
            Debug.Log("Dashboard dependencies search completed");
        }

        /// <summary>
        /// Inicializa el estado del Dashboard
        /// </summary>
        private void InitializeDashboardState()
        {
            _dashboardState.IsInitialized = true;
            _dashboardState.CurrentSection = "Dashboard";
            _dashboardState.CurrentActivePanel = IDashboardOps.PanelType.None;
            
            // Inicializar estados de IoT
            _dashboardState.LocalIoTStatus = DashboardInfo.ConnectionStatus.Unknown;
            _dashboardState.VMIoTStatus = DashboardInfo.ConnectionStatus.Unknown;
            _dashboardState.CloudIoTStatus = DashboardInfo.ConnectionStatus.Unknown;
            
            Debug.Log("Dashboard state initialized");
        }

        /// <summary>
        /// Actualiza datos del usuario autenticado
        /// </summary>
        private void UpdateUserData()
        {
            var userInfo = ServiceController.Instance?.GetUserInfo();
            if (userInfo.HasValue)
            {
                _userData.Username = userInfo.Value.username;
                _userData.UserGroup = userInfo.Value.userGroup;
                _userData.IsAuthenticated = userInfo.Value.isAuthenticated;
                _userData.UserRole = ServiceController.Instance?.GetUserRole() ?? "usuarios-basicos";
                _userData.LastLoginTime = DateTime.Now;
                
                Debug.Log($"User data updated - User: {_userData.Username}, Role: {_userData.UserRole}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Maneja el final de transiciones de paneles
        /// </summary>
        private void OnTransitionEndEvent(TransitionEndEvent evt)
        {
            if (!_uiManager.IsAnyPanelVisible())
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
                Debug.Log("All panels closed - hiding container");
            }
        }

        /// <summary>
        /// Maneja solicitudes de logout
        /// </summary>
        private void OnLogoutRequestedHandler()
        {
            OnLogoutRequested?.Invoke();
        }

        /// <summary>
        /// Maneja completado de transiciones de paneles
        /// </summary>
        private void OnPanelTransitionCompleteHandler(IDashboardOps.PanelType panelType)
        {
            OnPanelTransitionComplete?.Invoke(panelType);
            Debug.Log($"Panel transition complete: {panelType}");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Muestra la UI principal (equivalente al método original)
        /// </summary>
        internal void ShowUi()
        {
            Show();
        }

        /// <summary>
        /// Oculta la UI principal (equivalente al método original)
        /// </summary>
        internal void HideUi()
        {
            Hide();
        }

        /// <summary>
        /// Actualiza estados de IoT
        /// </summary>
        public void UpdateIoTStatus(
            DashboardInfo.ConnectionStatus localStatus,
            DashboardInfo.ConnectionStatus vmStatus,
            DashboardInfo.ConnectionStatus cloudStatus)
        {
            _dashboardState.LocalIoTStatus = localStatus;
            _dashboardState.VMIoTStatus = vmStatus;
            _dashboardState.CloudIoTStatus = cloudStatus;
            
            // Actualizar UI a través del UIManager
            _uiManager?.UpdateIoTLabels(
                _localIoTStatusLabel, _localIoTModeLabel,
                _vMIoTStatusLabel, _vMIoTModeLabel,
                _cloudIoTStatusLabel, _cloudIoTModeLabel,
                localStatus, vmStatus, cloudStatus
            );
            
            // ✅ CORRECTO - Notificar cambio de estado usando nuestro evento
            OnIoTStatusChanged?.Invoke(localStatus, vmStatus, cloudStatus);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// UI Manager asociado
        /// </summary>
        public DashboardUIManager UIManager => _uiManager;

        /// <summary>
        /// Event Manager asociado
        /// </summary>
        public DashboardEventManager EventManager => _eventManager;

        /// <summary>
        /// Estado actual del Dashboard
        /// </summary>
        public DashboardInfo.DashboardState DashboardState => _dashboardState;

        /// <summary>
        /// Datos del usuario actual
        /// </summary>
        public DashboardInfo.UserData UserData => _userData;

        #endregion
    }
}