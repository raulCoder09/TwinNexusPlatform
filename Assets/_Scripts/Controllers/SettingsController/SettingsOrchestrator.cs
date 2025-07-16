using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controller;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.SettingsController
{
    /// <summary>
    /// Coordinador principal del Settings - implementa ISettingsOps e IUIController
    /// Maneja configuraciones del sistema y navegación entre módulos
    /// </summary>
    public class SettingsOrchestrator : MonoBehaviour, ISettingsOps, IUIController
    {
        #region IUIController Implementation

        public bool RequiresAuthentication => true; // Settings SÍ requiere autenticación
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _uiConfig?.Body?.style.display == DisplayStyle.Flex;
        public string ControllerName => "SettingsController";

        // Events from IUIController
        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        #region ISettingsOps Implementation

        public bool IsNavigationMenuOpen => _uiManager?.NavigationMenuOpen ?? false;

        public ISettingsOps.PanelType CurrentActivePanel =>
            _uiManager?.CurrentActivePanel ?? ISettingsOps.PanelType.None;

        // Events from ISettingsOps
        public event Action<ISettingsOps.ConfigurationType> OnConfigurationOpened;
        public event Action<bool> OnNavigationMenuToggled;
        public event Action OnLogoutRequested;

        #endregion

        #region Private Fields

        private SettingsInfo.UIConfiguration _uiConfig = new SettingsInfo.UIConfiguration();
        private SettingsInfo.SettingsState _settingsState = new SettingsInfo.SettingsState();

        private SettingsUIManager _uiManager;
        private SettingsEventManager _eventManager;

        // Referencias a otros controladores
        private UIController _mainUIController;

        private VisualElement _subpanelsAndSmokeMaskContainer;
        private UIDocument _uiDocument;
        private bool _isInitialized = false;

        // Configuración de configuraciones
        private SettingsInfo.ConfigurationRegistry _configRegistry;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            // Settings inicia OCULTO hasta que se active desde otro módulo
            HideUi();
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;

            Debug.Log("[SettingsOrchestrator] Started - UI hidden until activation");
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
                    Debug.Log("SettingsOrchestrator already initialized");
                    return true;
                }

                Debug.Log("Initializing SettingsOrchestrator...");

                // Obtener componentes UI
                Debug.Log("Getting UIDocument component...");
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

                Debug.Log("Getting SubpanelsAndSmokeMaskContainer...");
                _subpanelsAndSmokeMaskContainer = root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
                if (_subpanelsAndSmokeMaskContainer == null)
                {
                    Debug.LogError("SubpanelsAndSmokeMaskContainer not found in UI!");
                    return false;
                }

                // Obtener referencias UI
                GetUiComponents(root);

                // Inicializar configuración de configuraciones
                _configRegistry = SettingsInfo.ConfigurationRegistry.CreateDefault();

                // Inicializar managers
                _uiManager = new SettingsUIManager(_uiConfig);
                _eventManager = new SettingsEventManager(_uiManager, OnReturnToDashboardHandler,
                    OnPanelTransitionCompleteHandler, this);
                _eventManager.RegisterEvents(_uiDocument);
                _uiManager.InitializePanelSystem();

                // Buscar dependencias
                FindDependencies();

                // Configurar estado inicial
                InitializeSettingsState();

                _isInitialized = true;
                Debug.Log("SettingsOrchestrator initialized successfully");

                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"SettingsOrchestrator initialization error: {ex.Message}");
                OnControllerError?.Invoke(this, $"Initialization failed: {ex.Message}");
                return false;
            }
        }

        public void Show()
        {
            // Verificar autenticación antes de mostrar
            if (!ServiceController.Instance.IsCognitoAuthenticated)
            {
                Debug.LogError("Cannot show Settings - user not authenticated");
                OnControllerError?.Invoke(this, "Authentication required");

                // Redirigir a Welcome
                _mainUIController?.ShowUI("Welcome");
                return;
            }

            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.Flex;

                // Actualizar UI al mostrar
                _uiManager?.ShowUi();

                OnControllerShown?.Invoke(this);
                Debug.Log("[SettingsOrchestrator] Settings UI shown");
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
                Debug.Log("[SettingsOrchestrator] Settings UI hidden");
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
                _settingsState = null;
                _configRegistry = null;
                _uiDocument = null;
                _subpanelsAndSmokeMaskContainer = null;

                _isInitialized = false;
                Debug.Log("[SettingsOrchestrator] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SettingsOrchestrator] Cleanup error: {ex.Message}");
            }
        }

        #endregion

        #region ISettingsOps Implementation

        public void OpenIoTConfiguration()
        {
            HandleConfigurationClick(ISettingsOps.ConfigurationType.IoT);
        }

        public void OpenUserConfiguration()
        {
            HandleConfigurationClick(ISettingsOps.ConfigurationType.User);
        }

        public void OpenCognitoConfiguration()
        {
            HandleConfigurationClick(ISettingsOps.ConfigurationType.Cognito);
        }

        public void OpenSystemConfiguration()
        {
            HandleConfigurationClick(ISettingsOps.ConfigurationType.System);
        }

        public void OpenNetworkConfiguration()
        {
            HandleConfigurationClick(ISettingsOps.ConfigurationType.Network);
        }

        public void NavigateToPanel(ISettingsOps.PanelType panelType)
        {
            if (!_isInitialized) Initialize();
            _uiManager?.ShowPanel(panelType);
        }

        public void CloseCurrentPanel()
        {
            _uiManager?.CloseCurrentPanel();
        }

        public void ReturnToDashboard()
        {
            Debug.Log("[SettingsOrchestrator] Returning to Dashboard");
            Hide();
            _mainUIController?.ShowUI("Dashboard");
        }

        public void ToggleNavigationMenu()
        {
            var wasOpen = IsNavigationMenuOpen;
            _uiManager?.ToggleNavigationMenu();
            OnNavigationMenuToggled?.Invoke(!wasOpen);
        }

        public void Logout()
        {
            HandleLogoutClick();
        }

        #endregion

        #region Public Event Handlers (Called by EventManager)

        /// <summary>
        /// Maneja clic en botón de configuración
        /// </summary>
        public void HandleConfigurationClick(ISettingsOps.ConfigurationType configurationType)
        {
            Debug.Log($"[SettingsOrchestrator] Configuration clicked: {configurationType}");

            // Resaltar configuración seleccionada
            var configName = configurationType.ToString();
            _uiManager?.HighlightSelectedConfiguration(configName);

            // Disparar evento
            OnConfigurationOpened?.Invoke(configurationType);

            // Mostrar mensaje específico para cada configuración
            ShowConfigurationMessage(configurationType);

            // Si es AwsServices, navegar a AWS Settings después de que el pop-up se cierre
            if (configurationType == ISettingsOps.ConfigurationType.AwsServices)
            {
                StartCoroutine(NavigateToAwsSettingsAfterPopup());
            }
        }

        private IEnumerator NavigateToAwsSettingsAfterPopup()
        {
            // Esperar el tiempo del pop-up (2 segundos, como en HideConfigurationMessageCoroutine)
            yield return new WaitForSeconds(2.0f);

            // Cerrar menú si está abierto
            _uiManager?.HideNavigationMenu();

            // Ocultar Settings
            Hide();

            // Navegar a AWS Settings
            _mainUIController?.ShowUI("AwsSettings");
            Debug.Log("[SettingsOrchestrator] Navigated to AWS Settings after popup");
        }

        /// <summary>
        /// Maneja navegación a Operations (vía DeviceSelection)
        /// </summary>
        public void HandleOperationsClick()
        {
            Debug.Log("[SettingsOrchestrator] Operations clicked - navigating via DeviceSelection");

            var parameters = new Dictionary<string, object>
            {
                ["context"] = "Operations",
                ["sourceController"] = "Settings"
            };

            Hide();
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }
        
        public void HandleReportsClick()
        {
            Debug.Log("Reports button clicked from Support");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Reports");
        }
        
        public void HandleSupportClick()
        {
            Debug.Log("Reports button clicked from Support");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Support");
        }

        /// <summary>
        /// Maneja navegación a Training (vía DeviceSelection)
        /// </summary>
        public void HandleTrainingClick()
        {
            Debug.Log("[SettingsOrchestrator] Training clicked - navigating via DeviceSelection");

            var parameters = new Dictionary<string, object>
            {
                ["context"] = "Training",
                ["sourceController"] = "Settings"
            };

            Hide();
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }

        /// <summary>
        /// Maneja regreso al Dashboard
        /// </summary>
        public void HandleReturnToDashboard()
        {
            Debug.Log("[SettingsOrchestrator] Dashboard return requested");
            ReturnToDashboard();
        }

        /// <summary>
        /// Maneja clic en botón Logout
        /// </summary>
        public void HandleLogoutClick()
        {
            Debug.Log("[SettingsOrchestrator] Logout button clicked - logging out user");

            // Cerrar menú y ocultar settings
            _uiManager?.HideNavigationMenu();
            Hide();

            // Ejecutar logout via ServiceController
            ServiceController.Instance?.CognitoManager?.SignOut();

            // Notificar evento
            OnLogoutRequested?.Invoke();

            // Navegar a Welcome
            _mainUIController?.ShowUI("Welcome");
        }

        #endregion

        #region Configuration-Specific Methods

        /// <summary>
        /// Muestra mensaje específico para cada configuración
        /// </summary>
        private void ShowConfigurationMessage(ISettingsOps.ConfigurationType configurationType)
        {
            var configInfo = _configRegistry.ConfigurationMap.GetValueOrDefault(configurationType);
            var message = GetConfigurationMessage(configurationType);

            // Crear overlay para mensaje
            var overlay = CreateConfigurationMessageOverlay(message);
            _uiConfig.Body.Add(overlay);

            // Auto-hide después de 2 segundos
            StartCoroutine(HideConfigurationMessageCoroutine(overlay, 2.0f));

            Debug.Log($"[SettingsOrchestrator] Configuration message shown: {message}");
        }

        /// <summary>
        /// Obtiene el mensaje específico para cada configuración
        /// </summary>
        private string GetConfigurationMessage(ISettingsOps.ConfigurationType configurationType)
        {
            return configurationType switch
            {
                ISettingsOps.ConfigurationType.IoT =>
                    "🔧 Ingresando a configuración de IoT\n\nConfiguración de dispositivos IoT, conexiones y protocolos de comunicación.",
                ISettingsOps.ConfigurationType.User =>
                    "👤 Ingresando a configuración de usuario\n\nGestión de perfil, preferencias y configuración personal.",
                ISettingsOps.ConfigurationType.Cognito =>
                    "🔐 Ingresando a configuración de autenticación\n\nConfiguración de AWS Cognito y parámetros de autenticación.",
                ISettingsOps.ConfigurationType.System =>
                    "⚙️ Ingresando a configuración del sistema\n\nConfiguración general del sistema y parámetros globales.",
                ISettingsOps.ConfigurationType.Network =>
                    "🌐 Ingresando a configuración de red\n\nConfiguración de conectividad, puertos y protocolos de red.",
                ISettingsOps.ConfigurationType.Database =>
                    "🗄️ Ingresando a configuración de base de datos\n\nConfiguración de conexiones y parámetros de base de datos.",
                ISettingsOps.ConfigurationType.Security =>
                    "🛡️ Ingresando a configuración de seguridad\n\nConfiguración de seguridad, certificados y control de acceso.",
                ISettingsOps.ConfigurationType.Backup =>
                    "💾 Ingresando a configuración de respaldo\n\nConfiguración de backups automáticos y recuperación.",
                ISettingsOps.ConfigurationType.AwsServices =>
                    "☁️ Ingresando a configuración de servicios AWS\n\nConfiguración de servicios AWS como SES, CloudWatch, IoT Core, S3 y Lambda.", // Agrega este caso
                _ => $"⚡ Ingresando a configuración: {configurationType}"
            };
        }

        /// <summary>
        /// Crea el overlay visual para el mensaje de configuración
        /// </summary>
        private VisualElement CreateConfigurationMessageOverlay(string message)
        {
            // Contenedor principal del overlay
            var overlay = new VisualElement();
            overlay.name = "ConfigurationMessageOverlay";
            overlay.style.position = Position.Absolute;
            overlay.style.width = Length.Percent(100);
            overlay.style.height = Length.Percent(100);
            overlay.style.backgroundColor = new Color(0, 0, 0, 0.8f);
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;

            // Panel del mensaje
            var messagePanel = new VisualElement();
            messagePanel.style.backgroundColor = Color.black;
            messagePanel.style.borderTopColor = Color.green;
            messagePanel.style.borderBottomColor = Color.green;
            messagePanel.style.borderLeftColor = Color.green;
            messagePanel.style.borderRightColor = Color.green;
            messagePanel.style.borderTopWidth = 3;
            messagePanel.style.borderBottomWidth = 3;
            messagePanel.style.borderLeftWidth = 3;
            messagePanel.style.borderRightWidth = 3;
            messagePanel.style.paddingTop = 30;
            messagePanel.style.paddingBottom = 30;
            messagePanel.style.paddingLeft = 40;
            messagePanel.style.paddingRight = 40;
            messagePanel.style.width = Length.Percent(60);
            messagePanel.style.maxWidth = 800;

            // Label del mensaje
            var messageLabel = new Label(message);
            messageLabel.style.color = Color.green;
            messageLabel.style.fontSize = 28;
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            messageLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            // Aplicar fuente VT323 si está disponible
            try
            {
                messageLabel.style.unityFontDefinition = new StyleFontDefinition(
                    Resources.Load<Font>("Fonts/VT323-Regular"));
            }
            catch
            {
                // Fallback a fuente por defecto si VT323 no está disponible
            }

            // Indicador de progreso
            var progressContainer = new VisualElement();
            progressContainer.style.marginTop = 20;
            progressContainer.style.alignItems = Align.Center;

            var progressLabel = new Label("⚡ Preparando configuración...");
            progressLabel.style.color = Color.yellow;
            progressLabel.style.fontSize = 20;
            progressLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            progressContainer.Add(progressLabel);

            // Ensamblar el panel
            messagePanel.Add(messageLabel);
            messagePanel.Add(progressContainer);
            overlay.Add(messagePanel);

            return overlay;
        }

        /// <summary>
        /// Corrutina para ocultar el mensaje de configuración
        /// </summary>
        private IEnumerator HideConfigurationMessageCoroutine(VisualElement overlay, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (overlay != null && overlay.parent != null)
            {
                overlay.parent.Remove(overlay);
                Debug.Log("[SettingsOrchestrator] Configuration message hidden");
            }
        }

        #endregion

        #region Private Implementation Methods

        /// <summary>
        /// Obtiene componentes UI del Settings
        /// </summary>
        private void GetUiComponents(VisualElement root)
        {
            Debug.Log("Getting Settings UI components...");

            // Contenedores principales
            _uiConfig.Body = root.Q<VisualElement>("Body");
            _uiConfig.SubpanelsContainer = _subpanelsAndSmokeMaskContainer;
            _uiConfig.Scrim = _subpanelsAndSmokeMaskContainer?.Q<VisualElement>("Scrim");
            _uiConfig.MainContentArea = root.Q<VisualElement>("Main");
            _uiConfig.ConfigurationGrid = root.Q<VisualElement>("ConfigurationGrid");

            // Configurar paneles
            InitializePanelConfiguration(root);

            Debug.Log("Settings UI components obtained successfully");
        }

        /// <summary>
        /// Inicializa la configuración de paneles
        /// </summary>
        private void InitializePanelConfiguration(VisualElement root)
        {
            var panelsContainer = _subpanelsAndSmokeMaskContainer;

            // Panel de menú de navegación (usando misma estructura que otros controladores)
            _uiConfig.Panels[ISettingsOps.PanelType.NavigationMenu] = new SettingsInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer?.Q<VisualElement>("NavigationMenuPanel"),
                ShowClass = "NavigationMenuPanelInMainScreen",
                HideClass = "NavigationMenuPanelOutMainScreen",
                RequiresScrim = true,
                AnimationDuration = 0.3f
            };

            // Paneles futuros (no existen en UXML actual)
            _uiConfig.Panels[ISettingsOps.PanelType.IoTConfig] = new SettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "IoTConfigPanelVisible",
                HideClass = "IoTConfigPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ISettingsOps.PanelType.UserConfig] = new SettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "UserConfigPanelVisible",
                HideClass = "UserConfigPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ISettingsOps.PanelType.CognitoConfig] = new SettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "CognitoConfigPanelVisible",
                HideClass = "CognitoConfigPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ISettingsOps.PanelType.SystemConfig] = new SettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "SystemConfigPanelVisible",
                HideClass = "SystemConfigPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ISettingsOps.PanelType.NetworkConfig] = new SettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "NetworkConfigPanelVisible",
                HideClass = "NetworkConfigPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ISettingsOps.PanelType.Help] = new SettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
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

            Debug.Log("Settings dependencies search completed");
        }

        /// <summary>
        /// Inicializa el estado del Settings
        /// </summary>
        private void InitializeSettingsState()
        {
            _settingsState.IsInitialized = true;
            _settingsState.CurrentSection = "Settings";
            _settingsState.CurrentActivePanel = ISettingsOps.PanelType.None;

            Debug.Log("Settings state initialized");
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
        /// Maneja solicitudes de regreso a Dashboard
        /// </summary>
        private void OnReturnToDashboardHandler()
        {
            ReturnToDashboard();
        }

        /// <summary>
        /// Maneja completado de transiciones de paneles
        /// </summary>
        private void OnPanelTransitionCompleteHandler(ISettingsOps.PanelType panelType)
        {
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

        #endregion

        #region Public Properties

        /// <summary>
        /// UI Manager asociado
        /// </summary>
        public SettingsUIManager UIManager => _uiManager;

        /// <summary>
        /// Event Manager asociado
        /// </summary>
        public SettingsEventManager EventManager => _eventManager;

        /// <summary>
        /// Estado actual del Settings
        /// </summary>
        public SettingsInfo.SettingsState SettingsState => _settingsState;

        /// <summary>
        /// Configuración de configuraciones
        /// </summary>
        public SettingsInfo.ConfigurationRegistry ConfigurationRegistry => _configRegistry;

        #endregion
    }
}