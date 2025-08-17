using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controller;
using _Scripts.Controllers.ServiceManagement;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.ArScaraPointsController
{
    /// <summary>
    /// Coordinador principal de AWS Settings - implementa IArScaraPointsOps e IUIController
    /// Equivalente a DashboardOrchestrator pero para configuraciones AWS
    /// </summary>
    public class ArScaraPointsOrchestrator : MonoBehaviour, IArScaraPointsOps, IUIController
    {
        #region IUIController Implementation

        public bool RequiresAuthentication => true; // AWS Settings SÍ requiere autenticación
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _uiConfig?.Body?.style.display == DisplayStyle.Flex;
        public string ControllerName => "ArScaraPointsController";

        // Events from IUIController
        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        #region IArScaraPointsOps Implementation

        public bool IsNavigationMenuOpen => _uiManager?.NavigationMenuOpen ?? false;
        public IArScaraPointsOps.PanelType CurrentActivePanel => _uiManager?.CurrentActivePanel ?? IArScaraPointsOps.PanelType.None;

        // Events from IArScaraPointsOps
        public event Action OnNavigationMenuOpened;
        public event Action OnNavigationMenuClosed;
        public event Action<IArScaraPointsOps.PanelType> OnPanelTransitionComplete;
        public event Action OnReturnToDashboardRequested;

        #endregion

        #region Private Fields

        private ArScaraPointsInfo.UIConfiguration _uiConfig = new ArScaraPointsInfo.UIConfiguration();
        private ArScaraPointsInfo.AwsConfiguration _awsConfig = new ArScaraPointsInfo.AwsConfiguration();
        private ArScaraPointsInfo.ArScaraPointsState _settingsState = new ArScaraPointsInfo.ArScaraPointsState();
        
        private ArScaraPointsUIManager _uiManager;
        private ArScaraPointsEventManager _eventManager;
        
        // Referencias a otros controladores
        private UIController _mainUIController;
        
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private UIDocument _uiDocument;
        private bool _isInitialized = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            // AWS Settings inicia OCULTO hasta que se navegue desde otra UI
             HideUi();
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            
            
            Debug.Log("[ArScaraPointsOrchestrator] Started - UI hidden until navigation");
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
                    Debug.Log("ArScaraPointsOrchestrator already initialized");
                    return true;
                }

                Debug.Log("Initializing ArScaraPointsOrchestrator...");

                // Obtener componentes UI
                Debug.Log("Step 1: Getting UIDocument component...");
                _uiDocument = GetComponent<UIDocument>();
                if (_uiDocument == null)
                {
                    Debug.LogError("UIDocument component not found!");
                    return false;
                }
                Debug.Log("UIDocument found successfully");

                Debug.Log("Step 2: Getting root visual element...");
                var root = _uiDocument.rootVisualElement;
                if (root == null)
                {
                    Debug.LogError("Root visual element is null!");
                    return false;
                }
                Debug.Log("Root visual element found successfully");

                Debug.Log("Step 3: Getting SubpanelsAndSmokeMaskContainer...");
                _subpanelsAndSmokeMaskContainer = root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
                if (_subpanelsAndSmokeMaskContainer == null)
                {
                    Debug.LogError("SubpanelsAndSmokeMaskContainer not found in UI!");
                    return false;
                }
                Debug.Log("SubpanelsAndSmokeMaskContainer found successfully");

                // Obtener referencias UI
                Debug.Log("Step 4: Getting UI components...");
                GetUiComponents(root);
                Debug.Log("UI components obtained");
                
                // Inicializar managers
                Debug.Log("Step 5: Creating ArScaraPointsUIManager...");
                _uiManager = new ArScaraPointsUIManager(_uiConfig);
                Debug.Log("ArScaraPointsUIManager created successfully");

                Debug.Log("Step 6: Creating ArScaraPointsEventManager...");
                _eventManager = new ArScaraPointsEventManager(_uiManager, OnReturnToDashboardHandler, OnPanelTransitionCompleteHandler, this);
                Debug.Log("ArScaraPointsEventManager created successfully");

                Debug.Log("Step 7: Registering events...");
                _eventManager.RegisterEvents(_uiDocument);
                Debug.Log("Events registered successfully");

                Debug.Log("Step 8: Initializing panel system...");
                _uiManager.InitializePanelSystem();
                Debug.Log("Panel system initialized successfully");
                
                // Buscar dependencias
                Debug.Log("Step 9: Finding dependencies...");
                FindDependencies();
                Debug.Log("Dependencies found");
                
                // Configurar estado inicial
                Debug.Log("Step 10: Initializing AWS settings state...");
                InitializeArScaraPointsState();
                Debug.Log("AWS settings state initialized");

                _isInitialized = true;
                Debug.Log("ArScaraPointsOrchestrator initialized successfully");
                
                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ArScaraPointsOrchestrator initialization error: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
                OnControllerError?.Invoke(this, $"Initialization failed: {ex.Message}");
                return false;
            }
        }

        public void Show()
        {
            // SEGURIDAD: Verificar autenticación antes de mostrar
            if (!ServiceController.Instance.IsCognitoAuthenticated)
            {
                Debug.LogError("Cannot show AWS Settings - user not authenticated");
                OnControllerError?.Invoke(this, "Authentication required");
                
                // Redirigir a Welcome
                _mainUIController?.ShowUI("Welcome");
                return;
            }

            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.Flex;
                
                // Actualizar configuraciones AWS si es necesario
                LoadAwsConfiguration();
                
                OnControllerShown?.Invoke(this);
                Debug.Log("[ArScaraPointsOrchestrator] AWS Settings UI shown");
            }
            var arScaraDropdown = _uiDocument?.rootVisualElement?.Q<DropdownField>("MenuRobotARSCARADropdownField");
            if (arScaraDropdown != null)
            {
                arScaraDropdown.SetValueWithoutNotify("Points");
            }
        }

        public void Hide()
        {
            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.None;
                
                // Cerrar cualquier panel abierto
                _uiManager?.CloseCurrentPanel();
                
                OnControllerHidden?.Invoke(this);
                Debug.Log("[ArScaraPointsOrchestrator] AWS Settings UI hidden");
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
                _uiConfig = null;
                _awsConfig = null;
                _settingsState = null;
                _uiDocument = null;
                _subpanelsAndSmokeMaskContainer = null;

                _isInitialized = false;
                Debug.Log("[ArScaraPointsOrchestrator] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArScaraPointsOrchestrator] Cleanup error: {ex.Message}");
            }
        }

        #endregion

        #region IArScaraPointsOps Implementation

        public void NavigateToPanel(IArScaraPointsOps.PanelType panelType)
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

        public void SwitchPanel(IArScaraPointsOps.PanelType fromPanel, IArScaraPointsOps.PanelType toPanel)
        {
            _uiManager?.SwitchPanel(fromPanel, toPanel);
        }

        public void OpenAwsConfiguration()
        {
            // TODO: Implementar cuando se agregue contenido específico
            Debug.Log("Opening AWS Configuration panel");
        }

        public void SaveConfiguration()
        {
            // TODO: Implementar cuando se agregue contenido específico
            Debug.Log("Saving AWS Configuration");
        }

        public void ResetConfiguration()
        {
            // TODO: Implementar cuando se agregue contenido específico
            Debug.Log("Resetting AWS Configuration");
        }

        public void TestConnection()
        {
            // TODO: Implementar cuando se agregue contenido específico
            Debug.Log("Testing AWS Connection");
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

        public void ReturnToDashboard()
        {
            HandleDashboardClick();
        }

        #endregion

        #region Public Event Handlers (Called by EventManager)

        /// <summary>
        /// Maneja navegación a ArScaraControlPanel
        /// </summary>
        public void HandleArScaraControlPanelNavigation()
        {
            Debug.Log("ArScaraControlPanel navigation requested");
    
            // Cerrar menú si está abierto
            _uiManager?.HideNavigationMenu();
            Hide();
    
            // Mostrar ArScaraControlPanel
            _mainUIController?.ShowUI("ArScaraControlPanel");
        }

        /// <summary>
        /// Maneja navegación a ArScaraJogAndTeach
        /// </summary>
        public void HandleArScaraJogAndTeachNavigation()
        {
            Debug.Log("ArScaraJogAndTeach navigation requested");
    
            // Cerrar menú si está abierto
            _uiManager?.HideNavigationMenu();
            Hide();
    
            // Mostrar ArScaraJogAndTeach
            _mainUIController?.ShowUI("ArScaraJogAndTeach");
        }
        /// <summary>
        /// Maneja clic en botón Dashboard
        /// </summary>
        public void HandleDashboardClick()
        {
            Debug.Log("Dashboard button clicked - returning to Dashboard");
    
            // Cerrar menú y ocultar AWS Settings
            _uiManager?.HideNavigationMenu();
            Hide();
    
            // Mostrar Dashboard
            _mainUIController?.ShowUI("Dashboard");
        }

        /// <summary>
        /// Maneja clic en botón Reports
        /// </summary>
        public void HandleReportsClick()
        {
            Debug.Log("Reports button clicked - opening Reports Center");
    
            // Cerrar menú y ocultar AWS Settings
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
                ["sourceController"] = "ArScaraPoints"
            };
            
            // Cerrar menú y ocultar AWS Settings
            _uiManager?.HideNavigationMenu();
            Hide();
            
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }

        /// <summary>
        /// Maneja clic en botón Training
        /// </summary>
        public void HandleTrainingClick()
        {
            var parameters = new Dictionary<string, object> {
                ["context"] = "Training",
                ["sourceController"] = "ArScaraPoints"
            };
            
            // Cerrar menú y ocultar AWS Settings
            _uiManager?.HideNavigationMenu();
            Hide();
            
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }

        /// <summary>
        /// Maneja clic en botón Support
        /// </summary>
        public void HandleSupportClick()
        {
            Debug.Log("Support button clicked - opening support center");
    
            // Cerrar menú y ocultar AWS Settings
            _uiManager?.HideNavigationMenu();
            Hide();
    
            // Mostrar support controller
            _mainUIController?.ShowUI("Support");
        }

        /// <summary>
        /// Maneja clic en botón Logout
        /// </summary>
        public void HandleLogoutClick()
        {
            Debug.Log("AWS Settings HandleLogoutClick() called");
    
            // Cerrar menú y ocultar AWS Settings
            _uiManager?.HideNavigationMenu();
            Hide();
    
            // Llamar a UIController para manejar logout
            var uiController = UIController.Instance;
            if (uiController != null)
            {
                Debug.Log("Calling UIController.RequestLogout()");
                uiController.RequestLogout();
            }
            else
            {
                Debug.Log("UIController not found - doing direct logout");
                ServiceController.Instance?.CognitoManager?.SignOut();
            }
        }

        #endregion

        #region Private Implementation Methods

        /// <summary>
        /// Obtiene componentes UI de AWS Settings
        /// </summary>
        private void GetUiComponents(VisualElement root)
        {
            Debug.Log("Getting AWS Settings UI components...");
    
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
            _uiConfig.MainContentArea = root.Q<VisualElement>("Main");
            Debug.Log($"MainContentArea found: {_uiConfig.MainContentArea != null}");
    
            Debug.Log("Getting HeaderArea...");
            _uiConfig.HeaderArea = root.Q<VisualElement>("Header");
            Debug.Log($"HeaderArea found: {_uiConfig.HeaderArea != null}");
    
            Debug.Log("Getting FooterArea...");
            _uiConfig.FooterArea = root.Q<VisualElement>("Footer");
            Debug.Log($"FooterArea found: {_uiConfig.FooterArea != null}");

            // Configurar paneles
            Debug.Log("Initializing panel configuration...");
            InitializePanelConfiguration(root);
    
            Debug.Log("AWS Settings UI components obtained successfully");
        }

        /// <summary>
        /// Inicializa la configuración de paneles
        /// </summary>
        private void InitializePanelConfiguration(VisualElement root)
        {
            var panelsContainer = _subpanelsAndSmokeMaskContainer;
            
            // Panel de menú de navegación (SÍ existe en UXML)
            _uiConfig.Panels[IArScaraPointsOps.PanelType.NavigationMenu] = new ArScaraPointsInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer?.Q<VisualElement>("NavigationMenuPanel"),
                ShowClass = "NavigationMenuPanelInMainScreen",
                HideClass = "NavigationMenuPanelOutMainScreen",
                RequiresScrim = true,
                AnimationDuration = 0.3f
            };

            // Paneles futuros específicos de AWS (NO existen en UXML actual)
            _uiConfig.Panels[IArScaraPointsOps.PanelType.AwsCredentials] = new ArScaraPointsInfo.UIConfiguration.PanelData
            {
                Panel = null, // Será null porque no existe en UXML
                ShowClass = "AwsCredentialsPanelVisible",
                HideClass = "AwsCredentialsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IArScaraPointsOps.PanelType.ServiceConfig] = new ArScaraPointsInfo.UIConfiguration.PanelData
            {
                Panel = null, // Será null porque no existe en UXML
                ShowClass = "ServiceConfigPanelVisible",
                HideClass = "ServiceConfigPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IArScaraPointsOps.PanelType.TestResults] = new ArScaraPointsInfo.UIConfiguration.PanelData
            {
                Panel = null, // Será null porque no existe en UXML
                ShowClass = "TestResultsPanelVisible",
                HideClass = "TestResultsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IArScaraPointsOps.PanelType.SecuritySettings] = new ArScaraPointsInfo.UIConfiguration.PanelData
            {
                Panel = null, // Será null porque no existe en UXML
                ShowClass = "SecuritySettingsPanelVisible",
                HideClass = "SecuritySettingsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IArScaraPointsOps.PanelType.RegionSettings] = new ArScaraPointsInfo.UIConfiguration.PanelData
            {
                Panel = null, // Será null porque no existe en UXML
                ShowClass = "RegionSettingsPanelVisible",
                HideClass = "RegionSettingsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IArScaraPointsOps.PanelType.Help] = new ArScaraPointsInfo.UIConfiguration.PanelData
            {
                Panel = null, // Será null porque no existe en UXML
                ShowClass = "HelpPanelVisible",
                HideClass = "HelpPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            // Registrar callbacks SOLO para paneles que existen
            foreach (var kvp in _uiConfig.Panels)
            {
                var panelData = kvp.Value;
                if (panelData.Panel != null)
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

            Debug.Log("AWS Settings dependencies search completed");
        }

        /// <summary>
        /// Inicializa el estado de AWS Settings
        /// </summary>
        private void InitializeArScaraPointsState()
        {
            _settingsState.IsInitialized = true;
            _settingsState.CurrentSection = "ArScaraPoints";
            _settingsState.CurrentActivePanel = IArScaraPointsOps.PanelType.None;
            
            // Inicializar configuración AWS básica
            _awsConfig.Region = "us-east-1";
            _awsConfig.ServiceStates = new Dictionary<string, bool>();
            _awsConfig.ServiceConfigs = new Dictionary<string, Dictionary<string, object>>();
            _awsConfig.TestResults = new Dictionary<string, ArScaraPointsInfo.ConnectionTestResult>();
            
            Debug.Log("AWS Settings state initialized");
        }

        /// <summary>
        /// Carga la configuración AWS actual
        /// </summary>
        private void LoadAwsConfiguration()
        {
            // TODO: Cargar configuración desde ServiceController o almacenamiento
            Debug.Log("Loading AWS configuration...");
            
            // Por ahora, obtener configuración básica del ServiceController si está disponible
            var serviceController = ServiceController.Instance;
            if (serviceController != null)
            {
                _settingsState.HasValidCredentials = serviceController.IsCognitoAuthenticated;
                Debug.Log($"AWS configuration loaded - Valid credentials: {_settingsState.HasValidCredentials}");
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
        /// Maneja solicitudes de regreso al Dashboard
        /// </summary>
        private void OnReturnToDashboardHandler()
        {
            OnReturnToDashboardRequested?.Invoke();
        }

        /// <summary>
        /// Maneja completado de transiciones de paneles
        /// </summary>
        private void OnPanelTransitionCompleteHandler(IArScaraPointsOps.PanelType panelType)
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
        /// Actualiza el estado de credenciales AWS
        /// </summary>
        public void UpdateCredentialsStatus(bool isValid, string message = null)
        {
            _settingsState.HasValidCredentials = isValid;
            
            // Actualizar UI a través del UIManager
            _uiManager?.UpdateCredentialsStatus(isValid, message);
            
            // Notificar cambio de estado usando el método público
            _settingsState.TriggerCredentialsValidityChanged(isValid);
            
            Debug.Log($"AWS credentials status updated: {isValid} - {message}");
        }

        /// <summary>
        /// Actualiza resultados de pruebas de conexión
        /// </summary>
        public void UpdateConnectionTestResults(Dictionary<string, ArScaraPointsInfo.ConnectionTestResult> results)
        {
            _awsConfig.TestResults = results;
            
            // Actualizar UI a través del UIManager
            _uiManager?.UpdateConnectionTestResults(results);
            
            // Notificar resultados individualmente usando el método público
            foreach (var kvp in results)
            {
                _settingsState.TriggerConnectionTestCompleted(kvp.Key, kvp.Value);
            }
            
            Debug.Log($"Connection test results updated for {results.Count} services");
        }

        /// <summary>
        /// Actualiza configuración de servicios AWS
        /// </summary>
        public void UpdateServiceConfiguration(string serviceName, Dictionary<string, object> config)
        {
            _awsConfig.ServiceConfigs[serviceName] = config;
            _awsConfig.ServiceStates[serviceName] = true;
            
            // Marcar como cambios no guardados
            _settingsState.HasUnsavedChanges = true;
            
            Debug.Log($"Service configuration updated for {serviceName}");
        }

        /// <summary>
        /// Prueba la conexión a un servicio AWS específico
        /// </summary>
        public void TestServiceConnection(string serviceName)
        {
            // TODO: Implementar prueba real de conexión
            _settingsState.IsTestingConnection = true;
            
            Debug.Log($"Testing connection to AWS service: {serviceName}");
            
            // Simular resultado de prueba (reemplazar con lógica real)
            var testResult = new ArScaraPointsInfo.ConnectionTestResult
            {
                ServiceName = serviceName,
                IsSuccessful = true, // Esto debería venir de una prueba real
                Message = "Connection test completed successfully",
                TestTime = DateTime.Now,
                ResponseTime = TimeSpan.FromMilliseconds(150)
            };
            
            _awsConfig.TestResults[serviceName] = testResult;
            _settingsState.TriggerConnectionTestCompleted(serviceName, testResult);
            _settingsState.IsTestingConnection = false;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// UI Manager asociado
        /// </summary>
        public ArScaraPointsUIManager UIManager => _uiManager;

        /// <summary>
        /// Event Manager asociado
        /// </summary>
        public ArScaraPointsEventManager EventManager => _eventManager;

        /// <summary>
        /// Estado actual de AWS Settings
        /// </summary>
        public ArScaraPointsInfo.ArScaraPointsState SettingsState => _settingsState;

        /// <summary>
        /// Configuración AWS actual
        /// </summary>
        public ArScaraPointsInfo.AwsConfiguration AwsConfig => _awsConfig;

        #endregion
    }
}