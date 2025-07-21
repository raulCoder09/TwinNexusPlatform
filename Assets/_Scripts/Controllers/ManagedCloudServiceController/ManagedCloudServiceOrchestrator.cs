using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controller;
using _Scripts.Controllers.ServiceManagement;
using _Scripts.Controllers.SettingsController;
using _Scripts.Controllers.UiManagement;
using _Scripts.Models.MQTTManagement;

namespace _Scripts.Controllers.ManagedCloudServiceController
{
    /// <summary>
    /// Coordinador principal del ManagedCloudService - implementa IManagedCloudServiceOps e IUIController
    /// Equivalente a WelcomeOrchestrator pero para ManagedCloudService
    /// </summary>
    public class ManagedCloudServiceOrchestrator : MonoBehaviour, IManagedCloudServiceOps, IUIController
    {
        #region IUIController Implementation

        public bool RequiresAuthentication => true; // ManagedCloudService SÍ requiere autenticación
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _uiConfig?.Body?.style.display == DisplayStyle.Flex;
        public string ControllerName => "ManagedCloudServiceController";

        // Events from IUIController
        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        #region IManagedCloudServiceOps Implementation

        public bool IsNavigationMenuOpen => _uiManager?.NavigationMenuOpen ?? false;
        public IManagedCloudServiceOps.PanelType CurrentActivePanel => _uiManager?.CurrentActivePanel ?? IManagedCloudServiceOps.PanelType.None;

        // Events from IManagedCloudServiceOps
        public event Action OnNavigationMenuOpened;
        public event Action OnNavigationMenuClosed;
        public event Action<IManagedCloudServiceOps.PanelType> OnPanelTransitionComplete;
        public event Action OnLogoutRequested;
        public event Action<ManagedCloudServiceInfo.ConnectionStatus, ManagedCloudServiceInfo.ConnectionStatus, ManagedCloudServiceInfo.ConnectionStatus> OnIoTStatusChanged;

        #endregion

        #region Private Fields

        private ManagedCloudServiceInfo.UIConfiguration _uiConfig = new ManagedCloudServiceInfo.UIConfiguration();
        private ManagedCloudServiceInfo.UserData _userData = new ManagedCloudServiceInfo.UserData();
        private ManagedCloudServiceInfo.ManagedCloudServiceState _ManagedCloudServiceState = new ManagedCloudServiceInfo.ManagedCloudServiceState();
        
        private ManagedCloudServiceUIManager _uiManager;
        private ManagedCloudServiceEventManager _eventManager;
        
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
        
        // Referencias MQTT/IoT
        private MqttManager _mqttManager;
        private AwsMqttTesting _awsMqttTesting;

// Referencias a elementos del dashboard
        private Button _connectButton;
        private Button _disconnectButton;
        private Button _publishTestButton;
        private Button _subscribeButton;
        private TextField _topicField;
        private TextField _messageField;
        private DropdownField _qosDropdown;
        private ScrollView _dataStreamScroll;

// Labels de métricas
        private Label _publishedCountLabel;
        private Label _receivedCountLabel;
        private Label _topicsCountLabel;
        private Label _connectionTimeLabel;
        private Label _connectionStatusLabel;
        private Label _brokerEndpointLabel;
        private Label _clientIdLabel;
        private Label _securityStatusLabel;

// Contadores para métricas
        private int _publishedMessages = 0;
        private int _receivedMessages = 0;
        private DateTime _connectionStartTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Verificar si ya hay una instancia (no singleton como Welcome)
            Initialize();
        }
        
        private void Start()
        {
            // CRÍTICO: ManagedCloudService inicia OCULTO hasta autenticación
            HideUi();
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            
            Debug.Log("[ManagedCloudServiceOrchestrator] Started - UI hidden until authentication");
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
                    Debug.Log("ManagedCloudServiceOrchestrator already initialized");
                    return true;
                }

                Debug.Log("Initializing ManagedCloudServiceOrchestrator...");

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
                _uiManager = new ManagedCloudServiceUIManager(_uiConfig);
                _eventManager = new ManagedCloudServiceEventManager(_uiManager, OnLogoutRequestedHandler, OnPanelTransitionCompleteHandler, this);
                _eventManager.RegisterEvents(_uiDocument);

                // Inicializar paneles
                _uiManager.InitializePanelSystem();

                // Buscar dependencias
                FindDependencies();

                // Inicializar estado
                InitializeManagedCloudServiceState();
                InitializeMQTTSystem();

                // Suscribirse a eventos de ServiceController
                SubscribeToServiceControllerEvents();

                _isInitialized = true;
                Debug.Log("✅ ManagedCloudServiceOrchestrator initialized successfully");
                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ManagedCloudServiceOrchestrator initialization error: {ex.Message}");
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
                Debug.LogError("Cannot show ManagedCloudService - user not authenticated");
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
                Debug.Log("[ManagedCloudServiceOrchestrator] ManagedCloudService UI shown");
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
                _ManagedCloudServiceState = null;
                _uiDocument = null;
                _subpanelsAndSmokeMaskContainer = null;

                // Limpiar MQTT
                if (_mqttManager != null)
                {
                    _mqttManager.UnsubscribeFromConnected(OnMqttConnected);
                    _mqttManager.UnsubscribeFromDisconnected(OnMqttDisconnected);
                    _mqttManager.UnsubscribeFromMessageReceived(OnMqttMessageReceived);
                }


                _isInitialized = false;
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ManagedCloudServiceOrchestrator] Cleanup error: {ex.Message}");
            }
        }

        #endregion

        #region IManagedCloudServiceOps Implementation

        public void NavigateToPanel(IManagedCloudServiceOps.PanelType panelType)
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

        public void SwitchPanel(IManagedCloudServiceOps.PanelType fromPanel, IManagedCloudServiceOps.PanelType toPanel)
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
    
            // Cerrar menú y ocultar ManagedCloudService
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
                ["sourceController"] = "ManagedCloudService"
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
                ["sourceController"] = "ManagedCloudService"
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
        /// Obtiene componentes UI del ManagedCloudService
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
            
            
            // Referencias a elementos del dashboard MQTT
            Debug.Log("Getting MQTT Dashboard elements...");
            _connectButton = root.Q<Button>("ConnectButton");
            _disconnectButton = root.Q<Button>("DisconnectButton");
            _publishTestButton = root.Q<Button>("PublishTestButton");
            _subscribeButton = root.Q<Button>("SubscribeButton");
            _topicField = root.Q<TextField>("TopicField");
            _messageField = root.Q<TextField>("MessageField");
            _qosDropdown = root.Q<DropdownField>("QoSDropdown");
            _dataStreamScroll = root.Q<ScrollView>("DataStreamScroll");

// Labels de métricas
            _publishedCountLabel = root.Q<Label>("PublishedCount");
            _receivedCountLabel = root.Q<Label>("ReceivedCount");
            _topicsCountLabel = root.Q<Label>("TopicsCount");
            _connectionTimeLabel = root.Q<Label>("ConnectionTime");
            _connectionStatusLabel = root.Q<Label>("ConnectionStatus");
            _brokerEndpointLabel = root.Q<Label>("BrokerEndpoint");
            _clientIdLabel = root.Q<Label>("ClientIdLabel");
            _securityStatusLabel = root.Q<Label>("SecurityStatus");

            Debug.Log($"MQTT Dashboard elements found - Connect: {_connectButton != null}, Publish: {_publishTestButton != null}");
            
            InitializePanelConfiguration(root);
    
            Debug.Log("✅ ManagedCloudService UI components obtained successfully");
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
    _uiConfig.Panels[IManagedCloudServiceOps.PanelType.NavigationMenu] = new ManagedCloudServiceInfo.UIConfiguration.PanelData
    {
        Panel = panelsContainer?.Q<VisualElement>("NavigationMenuPanel"),
        ShowClass = "NavigationMenuPanelinMainScreen",
        HideClass = "NavigationMenuPanelOutMainScreen", // 👈 CORREGIDO: usa la clase del USS
        RequiresScrim = true,
        AnimationDuration = 0.3f
    };

    // Paneles futuros (NO existen en UXML actual - Panel será null)
    _uiConfig.Panels[IManagedCloudServiceOps.PanelType.Notifications] = new ManagedCloudServiceInfo.UIConfiguration.PanelData
    {
        Panel = null, // 👈 SERÁ NULL porque no existe en UXML
        ShowClass = "NotificationsPanelVisible",
        HideClass = "NotificationsPanelHidden",
        IsModal = true,
        RequiresScrim = true
    };

    _uiConfig.Panels[IManagedCloudServiceOps.PanelType.UserProfile] = new ManagedCloudServiceInfo.UIConfiguration.PanelData
    {
        Panel = null, // 👈 SERÁ NULL porque no existe en UXML
        ShowClass = "UserProfilePanelVisible",
        HideClass = "UserProfilePanelHidden",
        IsModal = true,
        RequiresScrim = true
    };

    _uiConfig.Panels[IManagedCloudServiceOps.PanelType.QuickActions] = new ManagedCloudServiceInfo.UIConfiguration.PanelData
    {
        Panel = null, // 👈 SERÁ NULL porque no existe en UXML
        ShowClass = "QuickActionsPanelVisible",
        HideClass = "QuickActionsPanelHidden",
        IsModal = true,
        RequiresScrim = true
    };

    _uiConfig.Panels[IManagedCloudServiceOps.PanelType.StatusOverlay] = new ManagedCloudServiceInfo.UIConfiguration.PanelData
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
            
            Debug.Log("ManagedCloudService dependencies search completed");
        }

        /// <summary>
        /// Inicializa el estado del ManagedCloudService
        /// </summary>
        private void InitializeManagedCloudServiceState()
        {
            _ManagedCloudServiceState.IsInitialized = true;
            _ManagedCloudServiceState.CurrentSection = "ManagedCloudService";
            _ManagedCloudServiceState.CurrentActivePanel = IManagedCloudServiceOps.PanelType.None;
            
            // Inicializar estados de IoT
            _ManagedCloudServiceState.LocalIoTStatus = ManagedCloudServiceInfo.ConnectionStatus.Unknown;
            _ManagedCloudServiceState.VMIoTStatus = ManagedCloudServiceInfo.ConnectionStatus.Unknown;
            _ManagedCloudServiceState.CloudIoTStatus = ManagedCloudServiceInfo.ConnectionStatus.Unknown;
            
            Debug.Log("ManagedCloudService state initialized");
        }
        
        /// <summary>
        /// Inicializa el sistema MQTT usando ServiceController (VERSIÓN MEJORADA)
        /// </summary>
        private void InitializeMQTTSystem()
        {
            try
            {
                Debug.Log("Starting MQTT system initialization...");
        
                var serviceController = ServiceController.Instance;
                if (serviceController != null)
                {
                    _mqttManager = serviceController.MqttManager;
                    _awsMqttTesting = serviceController.AwsMqttTesting;
                }

                // NUEVO: No fallar si no están listos aún
                if (_mqttManager == null)
                {
                    Debug.LogWarning("MqttManager not ready yet - will initialize on demand");
                }
                else
                {
                    // Si está disponible, suscribirse a eventos
                    _mqttManager.SubscribeToConnected(OnMqttConnected);
                    _mqttManager.SubscribeToDisconnected(OnMqttDisconnected);
                    _mqttManager.SubscribeToMessageReceived(OnMqttMessageReceived);
                    Debug.Log("MQTT system initialized successfully");
                }

                // Configurar elementos UI independientemente
                SetupMQTTUI();
        
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing MQTT system: {ex.Message}");
            }
        }
        /// <summary>
/// Asegura que el sistema MQTT esté inicializado antes de usar
/// </summary>
private async Task<bool> EnsureMQTTSystemInitialized()
{
    try
    {
        // Si ya están inicializados, return true
        if (_mqttManager != null && _awsMqttTesting != null)
        {
            Debug.Log("MQTT components already initialized");
            return true;
        }

        Debug.Log("MQTT components not ready - attempting to initialize...");

        // Verificar que ServiceController esté listo
        var serviceController = ServiceController.Instance;
        if (serviceController == null)
        {
            Debug.LogError("ServiceController instance not available");
            return false;
        }

        // Verificar autenticación
        if (!serviceController.IsCognitoAuthenticated)
        {
            Debug.LogError("Cannot initialize MQTT - user not authenticated");
            return false;
        }

        // Esperar a que MQTT esté disponible en ServiceController
        int attempts = 0;
        const int maxAttempts = 50; // 5 segundos máximo
        
        while (!serviceController.IsServiceAvailable("mqtt") && attempts < maxAttempts)
        {
            await Task.Delay(100);
            attempts++;
            
            if (attempts % 10 == 0) // Log cada segundo
            {
                Debug.Log($"Waiting for MQTT service... Attempt {attempts}/{maxAttempts}");
            }
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogError("Timeout waiting for MQTT service to become available");
            return false;
        }

        // Obtener referencias del ServiceController
        _mqttManager = serviceController.MqttManager;
        _awsMqttTesting = serviceController.AwsMqttTesting;

        // Verificar que se obtuvieron correctamente
        if (_mqttManager == null)
        {
            Debug.LogError("Failed to get MqttManager from ServiceController");
            return false;
        }

        if (_awsMqttTesting == null)
        {
            Debug.LogWarning("AwsMqttTesting not available, but MqttManager is ready");
        }

        // Suscribirse a eventos MQTT si no estaba ya suscrito
        _mqttManager.SubscribeToConnected(OnMqttConnected);
        _mqttManager.SubscribeToDisconnected(OnMqttDisconnected);
        _mqttManager.SubscribeToMessageReceived(OnMqttMessageReceived);

        Debug.Log(" MQTT system initialized successfully from ServiceController");
        return true;
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error ensuring MQTT system initialization: {ex.Message}");
        return false;
    }
}

        /// <summary>
        /// Configura los elementos UI del dashboard MQTT
        /// </summary>
        private void SetupMQTTUI()
        {
            // Configurar valores iniciales
            if (_topicField != null)
                _topicField.value = "tnp/TNPSGA52/test";
    
            if (_messageField != null)
                _messageField.value = "{\"message\": \"Hello from Unity!\", \"timestamp\": \"\"}";

            // Estado inicial
            UpdateConnectionUI(false);
            UpdateMetricsUI();
        }
        
        /// <summary>
/// Maneja clic en Connect
/// </summary>
internal async void OnConnectButtonClicked(ClickEvent evt)
{
    try
    {
        Debug.Log("Connect button clicked");
        
        // NUEVO: Asegurar que MQTT esté inicializado
        bool mqttReady = await EnsureMQTTSystemInitialized();
        if (!mqttReady)
        {
            Debug.LogError("Failed to initialize MQTT system");
            return;
        }

        // Verificar de nuevo que tenemos las referencias
        if (_mqttManager == null)
        {
            Debug.LogError("MQTT components still not available after initialization attempt");
            return;
        }

        Debug.Log("MQTT components ready - attempting connection...");

        // Usar la configuración de AwsMqttTesting para conectar
        var connected = await _mqttManager.ConnectAsync(
            "aqloxhiemdroo-ats.iot.us-east-1.amazonaws.com",
            8883,
            "TNPSGA52",
            "",
            "",
            true,
            "resources/certificates",
            "aws-iot-core.pfx",
            "5859"
        );

        if (connected)
        {
            _connectionStartTime = DateTime.Now;
            Debug.Log("✅ MQTT connection successful");
        }
        else
        {
            Debug.LogError("❌ MQTT connection failed");
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error connecting MQTT: {ex.Message}");
    }
}

        internal async void OnDisconnectButtonClicked(ClickEvent evt)
        {
            try
            {
                Debug.Log("Disconnect button clicked");
        
                // Asegurar que MQTT esté inicializado
                bool mqttReady = await EnsureMQTTSystemInitialized();
                if (!mqttReady)
                {
                    Debug.LogWarning("MQTT not initialized for disconnect");
                    return;
                }
        
                if (_mqttManager != null)
                {
                    await _mqttManager.DisconnectAsync();
                    Debug.Log("Disconnect command sent");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error disconnecting MQTT: {ex.Message}");
            }
        }

/// <summary>
/// Maneja clic en Publish Test
/// </summary>
internal async void OnPublishTestButtonClicked(ClickEvent evt)
{
    try
    {
        Debug.Log("Publish Test button clicked");
        
        bool mqttReady = await EnsureMQTTSystemInitialized();
        if (!mqttReady)
        {
            Debug.LogWarning("MQTT not initialized for publish");
            return;
        }

        if (_mqttManager == null || !_mqttManager.IsConnected())
        {
            Debug.LogWarning("MQTT not connected");
            return;
        }

        string topic = _topicField?.value ?? "tnp/TNPSGA52/test";
        string message = _messageField?.value ?? "{\"message\": \"Hello from Unity!\"}";
        
        // Agregar timestamp si no existe
        if (message.Contains("\"timestamp\": \"\""))
        {
            message = message.Replace("\"timestamp\": \"\"", 
                $"\"timestamp\": \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"");
        }

        var qosLevel = _qosDropdown?.index switch
        {
            0 => MqttInfo.QoSLevel.AtMostOnce,
            2 => MqttInfo.QoSLevel.ExactlyOnce,
            _ => MqttInfo.QoSLevel.AtLeastOnce
        };

        bool published = await _mqttManager.PublishAsync(topic, message, qosLevel);
        
        if (published)
        {
            _publishedMessages++;
            UpdateMetricsUI();
            Debug.Log($"Message published to {topic}");
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error publishing message: {ex.Message}");
    }
}

/// <summary>
/// Maneja clic en Subscribe
/// </summary>
internal async void OnSubscribeButtonClicked(ClickEvent evt)
{
    try
    {
        Debug.Log("Subscribe button clicked");
        
        // Asegurar que MQTT esté inicializado
        bool mqttReady = await EnsureMQTTSystemInitialized();
        if (!mqttReady)
        {
            Debug.LogWarning("MQTT not initialized for subscribe");
            return;
        }
        
        if (_mqttManager == null || !_mqttManager.IsConnected())
        {
            Debug.LogWarning("MQTT not connected");
            return;
        }

        string topic = _topicField?.value ?? "tnp/TNPSGA52/test";
        
        var qosLevel = _qosDropdown?.index switch
        {
            0 => MqttInfo.QoSLevel.AtMostOnce,
            2 => MqttInfo.QoSLevel.ExactlyOnce,
            _ => MqttInfo.QoSLevel.AtLeastOnce
        };

        bool subscribed = await _mqttManager.SubscribeAsync(topic, qosLevel, null);
        
        if (subscribed)
        {
            UpdateMetricsUI();
            Debug.Log($"✅ Subscribed to {topic}");
        }
        else
        {
            Debug.LogError("❌ Failed to subscribe");
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error subscribing: {ex.Message}");
    }
}

/// <summary>
/// Maneja evento de conexión MQTT
/// </summary>
private void OnMqttConnected()
{
    Debug.Log("MQTT Connected event received");
    UpdateConnectionUI(true);
    UpdateIoTStatus(
        ManagedCloudServiceInfo.ConnectionStatus.Disconnected,
        ManagedCloudServiceInfo.ConnectionStatus.Disconnected,
        ManagedCloudServiceInfo.ConnectionStatus.Connected
    );
}

/// <summary>
/// Maneja evento de desconexión MQTT
/// </summary>
private void OnMqttDisconnected(string reason)
{
    Debug.Log($"MQTT Disconnected: {reason}");
    UpdateConnectionUI(false);
    UpdateIoTStatus(
        ManagedCloudServiceInfo.ConnectionStatus.Disconnected,
        ManagedCloudServiceInfo.ConnectionStatus.Disconnected,
        ManagedCloudServiceInfo.ConnectionStatus.Disconnected
    );
}

/// <summary>
/// Maneja mensajes MQTT recibidos
/// </summary>
private void OnMqttMessageReceived(string topic, string message)
{
    Debug.Log($"MQTT Message received on {topic}: {message}");
    _receivedMessages++;
    UpdateMetricsUI();
    AddMessageToDataStream(topic, message);
}
/// <summary>
/// Actualiza UI de conexión
/// </summary>
private void UpdateConnectionUI(bool isConnected)
{
    if (_connectionStatusLabel != null)
    {
        _connectionStatusLabel.text = isConnected 
            ? "🟢 Connected via TLS 1.2" 
            : "🔴 Disconnected";
    }

    if (_securityStatusLabel != null)
    {
        _securityStatusLabel.text = isConnected 
            ? "🔒 SSL/TLS Secured" 
            : "🔓 Not Connected";
    }

    // Habilitar/deshabilitar botones
    if (_connectButton != null)
        _connectButton.SetEnabled(!isConnected);
    
    if (_disconnectButton != null)
        _disconnectButton.SetEnabled(isConnected);
    
    if (_publishTestButton != null)
        _publishTestButton.SetEnabled(isConnected);
    
    if (_subscribeButton != null)
        _subscribeButton.SetEnabled(isConnected);
}

/// <summary>
/// Actualiza métricas UI
/// </summary>
private void UpdateMetricsUI()
{
    if (_publishedCountLabel != null)
        _publishedCountLabel.text = _publishedMessages.ToString();
    
    if (_receivedCountLabel != null)
        _receivedCountLabel.text = _receivedMessages.ToString();
    
    if (_topicsCountLabel != null && _mqttManager != null)
    {
        var status = _mqttManager.GetConnectionStatus();
        _topicsCountLabel.text = status.subscribedTopics?.Count.ToString() ?? "0";
    }
    
    if (_connectionTimeLabel != null && _mqttManager?.IsConnected() == true)
    {
        var elapsed = DateTime.Now - _connectionStartTime;
        _connectionTimeLabel.text = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
    }
}

/// <summary>
/// Agrega mensaje al stream de datos
/// </summary>
private void AddMessageToDataStream(string topic, string message)
{
    if (_dataStreamScroll == null) return;

    var dataItem = new VisualElement();
    dataItem.AddToClassList("data-item");

    var topicLabel = new Label(topic);
    topicLabel.AddToClassList("data-topic");
    
    var payloadLabel = new Label(message);
    payloadLabel.AddToClassList("data-payload");
    
    var timestampLabel = new Label("Now");
    timestampLabel.AddToClassList("data-timestamp");

    dataItem.Add(topicLabel);
    dataItem.Add(payloadLabel);
    dataItem.Add(timestampLabel);

    // Insertar al principio
    _dataStreamScroll.Insert(0, dataItem);

    // Limitar a 10 mensajes
    while (_dataStreamScroll.childCount > 10)
    {
        _dataStreamScroll.RemoveAt(_dataStreamScroll.childCount - 1);
    }
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
        private void OnPanelTransitionCompleteHandler(IManagedCloudServiceOps.PanelType panelType)
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
            ManagedCloudServiceInfo.ConnectionStatus localStatus,
            ManagedCloudServiceInfo.ConnectionStatus vmStatus,
            ManagedCloudServiceInfo.ConnectionStatus cloudStatus)
        {
            _ManagedCloudServiceState.LocalIoTStatus = localStatus;
            _ManagedCloudServiceState.VMIoTStatus = vmStatus;
            _ManagedCloudServiceState.CloudIoTStatus = cloudStatus;
            
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
        public ManagedCloudServiceUIManager UIManager => _uiManager;

        /// <summary>
        /// Event Manager asociado
        /// </summary>
        public ManagedCloudServiceEventManager EventManager => _eventManager;

        /// <summary>
        /// Estado actual del ManagedCloudService
        /// </summary>
        public ManagedCloudServiceInfo.ManagedCloudServiceState ManagedCloudServiceState => _ManagedCloudServiceState;

        /// <summary>
        /// Datos del usuario actual
        /// </summary>
        public ManagedCloudServiceInfo.UserData UserData => _userData;

        #endregion
    }
}