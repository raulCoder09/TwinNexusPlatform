using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controller;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.DeviceSelectionController
{
    /// <summary>
    /// Coordinador principal del Device Selection - implementa IDeviceSelectionOps e IUIController
    /// Maneja la selección de dispositivos para Training y Operations
    /// </summary>
    public class DeviceSelectionOrchestrator : MonoBehaviour, IDeviceSelectionOps, IUIController
    {
        #region IUIController Implementation

        public bool RequiresAuthentication => true; // Device Selection SÍ requiere autenticación
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _uiConfig?.Body?.style.display == DisplayStyle.Flex;
        public string ControllerName => "DeviceSelectionController";

        // Events from IUIController
        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        #region IDeviceSelectionOps Implementation

        public bool IsNavigationMenuOpen => _uiManager?.NavigationMenuOpen ?? false;
        public IDeviceSelectionOps.PanelType CurrentActivePanel => _uiManager?.CurrentActivePanel ?? IDeviceSelectionOps.PanelType.None;
        public IDeviceSelectionOps.LaunchContext CurrentContext => _uiManager?.CurrentContext ?? IDeviceSelectionOps.LaunchContext.None;

        // Events from IDeviceSelectionOps
        public event Action<string, IDeviceSelectionOps.LaunchContext> OnDeviceSelected;
        public event Action<string, IDeviceSelectionOps.LaunchContext> OnDeviceLaunched;
        public event Action<string, string> OnDeviceLaunchFailed;
        public event Action<bool> OnNavigationMenuToggled;
        public event Action<IDeviceSelectionOps.LaunchContext, IDeviceSelectionOps.LaunchContext> OnContextChanged;

        #endregion

        #region Private Fields

        private DeviceSelectionInfo.UIConfiguration _uiConfig = new DeviceSelectionInfo.UIConfiguration();
        private DeviceSelectionInfo.ContextData _contextData = new DeviceSelectionInfo.ContextData();
        private DeviceSelectionInfo.DeviceSelectionState _deviceSelectionState = new DeviceSelectionInfo.DeviceSelectionState();
        
        private DeviceSelectionUIManager _uiManager;
        private DeviceSelectionEventManager _eventManager;
        
        // Referencias a otros controladores
        private UIController _mainUIController;
        
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private UIDocument _uiDocument;
        private bool _isInitialized = false;

        // Configuración de dispositivos
        private DeviceSelectionInfo.DeviceConfiguration _deviceConfig;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            // Device Selection inicia OCULTO hasta que se active desde Dashboard
            HideUi();
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            
            Debug.Log("[DeviceSelectionOrchestrator] Started - UI hidden until activation");
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
                    Debug.Log("DeviceSelectionOrchestrator already initialized");
                    return true;
                }

                Debug.Log("Initializing DeviceSelectionOrchestrator...");

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
                
                // Inicializar configuración de dispositivos
                _deviceConfig = DeviceSelectionInfo.DeviceConfiguration.CreateDefault();
                
                // Inicializar managers
                _uiManager = new DeviceSelectionUIManager(_uiConfig);
                _eventManager = new DeviceSelectionEventManager(_uiManager, OnReturnToDashboardHandler, OnPanelTransitionCompleteHandler, this);
                _eventManager.RegisterEvents(_uiDocument);
                _uiManager.InitializePanelSystem();
                
                // Buscar dependencias
                FindDependencies();
                
                // Configurar estado inicial
                InitializeDeviceSelectionState();

                _isInitialized = true;
                Debug.Log("DeviceSelectionOrchestrator initialized successfully");
                
                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"DeviceSelectionOrchestrator initialization error: {ex.Message}");
                OnControllerError?.Invoke(this, $"Initialization failed: {ex.Message}");
                return false;
            }
        }

        public void Show()
        {
            // Verificar autenticación antes de mostrar
            if (!ServiceController.Instance.IsCognitoAuthenticated)
            {
                Debug.LogError("Cannot show Device Selection - user not authenticated");
                OnControllerError?.Invoke(this, "Authentication required");
                
                // Redirigir a Welcome
                _mainUIController?.ShowUI("Welcome");
                return;
            }

            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.Flex;
                
                // Actualizar UI con contexto actual
                _uiManager?.ShowUi();
                
                OnControllerShown?.Invoke(this);
                Debug.Log("[DeviceSelectionOrchestrator] Device Selection UI shown");
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
                Debug.Log("[DeviceSelectionOrchestrator] Device Selection UI hidden");
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
                _contextData = null;
                _deviceSelectionState = null;
                _deviceConfig = null;
                _uiDocument = null;
                _subpanelsAndSmokeMaskContainer = null;

                _isInitialized = false;
                Debug.Log("[DeviceSelectionOrchestrator] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeviceSelectionOrchestrator] Cleanup error: {ex.Message}");
            }
        }

        #endregion

        #region IDeviceSelectionOps Implementation

        public async Task<bool> LaunchDeviceAsync(string deviceId)
        {
            try
            {
                Debug.Log($"[DeviceSelectionOrchestrator] Launching device: {deviceId} in context: {CurrentContext}");
                
                // Validar dispositivo
                var deviceInfo = GetDeviceInfo(deviceId);
                if (deviceInfo == null)
                {
                    var errorMsg = $"Device not found: {deviceId}";
                    Debug.LogError(errorMsg);
                    OnDeviceLaunchFailed?.Invoke(deviceId, errorMsg);
                    ShowLaunchMessage(errorMsg, true);
                    return false;
                }
                
                // Verificar disponibilidad
                if (!IsDeviceAvailable(deviceId))
                {
                    var errorMsg = $"Device not available: {deviceId}";
                    Debug.LogError(errorMsg);
                    OnDeviceLaunchFailed?.Invoke(deviceId, errorMsg);
                    ShowLaunchMessage(errorMsg, true);
                    return false;
                }
                
                // Verificar compatibilidad con contexto
                if (!deviceInfo.IsCompatibleWith(CurrentContext))
                {
                    var errorMsg = $"Device {deviceId} not compatible with context {CurrentContext}";
                    Debug.LogError(errorMsg);
                    OnDeviceLaunchFailed?.Invoke(deviceId, errorMsg);
                    ShowLaunchMessage(errorMsg, true);
                    return false;
                }

                // Resaltar dispositivo seleccionado
                _uiManager?.HighlightSelectedDevice(deviceId);

                // Mostrar mensaje contextual en UI
                var contextualMessage = GetContextualLaunchMessage(deviceId, CurrentContext);
                ShowLaunchMessage(contextualMessage, false);

                // También imprimir en console para logging
                PrintContextualLaunchMessage(deviceId, CurrentContext);

                // Simular delay de lanzamiento mientras se muestra el mensaje
                await Task.Delay(3000);

                // Disparar eventos
                OnDeviceLaunched?.Invoke(deviceId, CurrentContext);

                // Ocultar mensaje y UI actual
                HideLaunchMessage();
                Hide();

                // Aquí se podría navegar a la escena específica del dispositivo
                // Por ahora solo navegamos de regreso al Dashboard
                await Task.Delay(500);
                ReturnToDashboard();

                return true;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error launching device {deviceId}: {ex.Message}";
                Debug.LogError(errorMsg);
                OnDeviceLaunchFailed?.Invoke(deviceId, errorMsg);
                ShowLaunchMessage(errorMsg, true);
                return false;
            }
        }

        public DeviceSelectionInfo.DeviceInfo GetDeviceInfo(string deviceId)
        {
            return _deviceConfig?.DeviceRegistry.GetValueOrDefault(deviceId);
        }

        public DeviceSelectionInfo.DeviceInfo[] GetAvailableDevices()
        {
            if (_deviceConfig == null) return new DeviceSelectionInfo.DeviceInfo[0];
            
            return _deviceConfig.GetDevicesForContext(CurrentContext).ToArray();
        }

        public bool IsDeviceAvailable(string deviceId)
        {
            var deviceInfo = GetDeviceInfo(deviceId);
            return deviceInfo?.IsAvailable == true && 
                   deviceInfo.Status == DeviceSelectionInfo.DeviceStatus.Online &&
                   deviceInfo.IsCompatibleWith(CurrentContext);
        }

        public void SetLaunchContext(IDeviceSelectionOps.LaunchContext context, string sourceController = null)
        {
            var previousContext = CurrentContext;
            
            _uiManager?.SetLaunchContext(context, sourceController);
            _contextData.CurrentContext = context;
            _contextData.SourceController = sourceController ?? "Unknown";
            
            Debug.Log($"[DeviceSelectionOrchestrator] Context set: {previousContext} → {context} (from {sourceController})");
            
            OnContextChanged?.Invoke(previousContext, context);
        }

        public IDeviceSelectionOps.LaunchContext GetCurrentContext()
        {
            return CurrentContext;
        }

        public string GetContextualTitle()
        {
            return _contextData.GetContextualTitle();
        }

        public void NavigateToPanel(IDeviceSelectionOps.PanelType panelType)
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
            Debug.Log("[DeviceSelectionOrchestrator] Returning to Dashboard");
            Hide();
            _mainUIController?.ShowUI("Dashboard");
        }

        public void ToggleNavigationMenu()
        {
            var wasOpen = IsNavigationMenuOpen;
            _uiManager?.ToggleNavigationMenu();
            OnNavigationMenuToggled?.Invoke(!wasOpen);
        }

        #endregion

        #region Public Event Handlers (Called by EventManager)

/// <summary>
/// Maneja selección de dispositivo
/// </summary>
public async void HandleDeviceSelection(string deviceId)
{
    Debug.Log($"[DeviceSelectionOrchestrator] Device selected: {deviceId} in context: {CurrentContext}");
    
    // Disparar evento de selección
    OnDeviceSelected?.Invoke(deviceId, CurrentContext);
    
    if (deviceId == "ARSCARA")
    {
        if (CurrentContext == IDeviceSelectionOps.LaunchContext.Operations)
        {
            Debug.Log("[DeviceSelectionOrchestrator] Navigating to ArScaraControlPanel UI (Operations context)");
            
            // Disparar evento de dispositivo lanzado
            OnDeviceLaunched?.Invoke(deviceId, CurrentContext);
            
            // Cerrar menú y ocultar DeviceSelection
            _uiManager?.HideNavigationMenu();
            Hide();
            
            // Mostrar la UI de ArScaraControlPanel
            _mainUIController?.ShowUI("ArScaraControlPanel");
        }
        else if (CurrentContext == IDeviceSelectionOps.LaunchContext.Training)
        {
            Debug.Log("[DeviceSelectionOrchestrator] Navigating to MedaraConcreteArScara UI (Training context)");
            
            // Disparar evento de dispositivo lanzado
            OnDeviceLaunched?.Invoke(deviceId, CurrentContext);
            
            // Cerrar menú y ocultar DeviceSelection
            _uiManager?.HideNavigationMenu();
            Hide();
            
            // Mostrar la UI de MedaraConcreteArScara
            _mainUIController?.ShowUI("MedaraConcreteArScara");
        }
        else
        {
            // Contexto no permitido
            var errorMsg = $"Device {deviceId} cannot be launched in context {CurrentContext}. Please use Operations or Training context.";
            Debug.LogError(errorMsg);
            OnDeviceLaunchFailed?.Invoke(deviceId, errorMsg);
            ShowLaunchMessage(errorMsg, true);
        }
    }
    else
    {
        // Mantener la lógica original para otros dispositivos
        await LaunchDeviceAsync(deviceId);
    }
}

        /// <summary>
        /// Maneja cambio de contexto
        /// </summary>
        public void HandleContextSwitch(IDeviceSelectionOps.LaunchContext newContext)
        {
            Debug.Log($"[DeviceSelectionOrchestrator] Context switch requested: {CurrentContext} → {newContext}");
            
            SetLaunchContext(newContext, "DeviceSelection");
            
            // Cerrar menú de navegación después del cambio
            if (IsNavigationMenuOpen)
            {
                _uiManager?.HideNavigationMenu();
            }
        }

        /// <summary>
        /// Maneja regreso al Dashboard
        /// </summary>
        public void HandleReturnToDashboard()
        {
            Debug.Log("[DeviceSelectionOrchestrator] Dashboard return requested");
            ReturnToDashboard();
        }
        
        /// <summary>
        /// Maneja clic en botón Reports
        /// </summary>
        public void HandleReportsClick()
        {
            Debug.Log("Reports button clicked - opening Reports Center");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Reports");
        }

        /// <summary>
        /// Maneja clic en botón Support
        /// </summary>
        public void HandleSupportClick()
        {
            Debug.Log("Support button clicked - opening support center");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Support");
        }

        /// <summary>
        /// Maneja clic en botón Settings
        /// </summary>
        public void HandleSettingsClick()
        {
            Debug.Log("Settings button clicked - opening settings");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Settings");
        }

        /// <summary>
        /// Maneja clic en botón Logout
        /// </summary>
        public void HandleLogoutClick()
        {
            Debug.Log("DeviceSelection HandleLogoutClick() called");
            _uiManager?.HideNavigationMenu();
            Hide();
    
            var uiController = UIController.Instance;
            if (uiController != null)
            {
                Debug.Log("Calling UIController.RequestLogout() from DeviceSelection");
                uiController.RequestLogout();
            }
            else
            {
                Debug.Log("UIController not found - doing direct logout");
                ServiceController.Instance?.CognitoManager?.SignOut();
            }
        }

        #endregion

        #region Context-Specific Methods

        /// <summary>
        /// Configura el Device Selection para modo Training
        /// </summary>
        public void ConfigureForTraining(string sourceController = "Dashboard")
        {
            SetLaunchContext(IDeviceSelectionOps.LaunchContext.Training, sourceController);
            Debug.Log("[DeviceSelectionOrchestrator] Configured for Training mode");
        }

        /// <summary>
        /// Configura el Device Selection para modo Operations
        /// </summary>
        public void ConfigureForOperations(string sourceController = "Dashboard")
        {
            SetLaunchContext(IDeviceSelectionOps.LaunchContext.Operations, sourceController);
            Debug.Log("[DeviceSelectionOrchestrator] Configured for Operations mode");
        }

        /// <summary>
        /// Imprime mensaje contextual al lanzar dispositivo (para logging)
        /// </summary>
        private void PrintContextualLaunchMessage(string deviceId, IDeviceSelectionOps.LaunchContext context)
        {
            var deviceInfo = GetDeviceInfo(deviceId);
            var deviceName = deviceInfo?.DisplayName ?? deviceId;
            
            switch (context)
            {
                case IDeviceSelectionOps.LaunchContext.Training:
                    Debug.Log($"🎓 Abriendo escena para entrenamiento con {deviceName}");
                    Debug.Log($"📚 Iniciando sesión de aprendizaje seguro con {deviceName}");
                    break;
                    
                case IDeviceSelectionOps.LaunchContext.Operations:
                    Debug.Log($"🏭 Abriendo escena para operaciones industriales con {deviceName}");
                    Debug.Log($"⚙️ Iniciando operaciones industriales con {deviceName} - Verificar protocolos de seguridad");
                    break;
                    
                default:
                    Debug.Log($"🤖 Lanzando dispositivo: {deviceName}");
                    break;
            }
        }

        /// <summary>
        /// Obtiene el mensaje contextual para lanzamiento de dispositivo
        /// </summary>
        private string GetContextualLaunchMessage(string deviceId, IDeviceSelectionOps.LaunchContext context)
        {
            var deviceInfo = GetDeviceInfo(deviceId);
            var deviceName = deviceInfo?.DisplayName ?? deviceId;
            
            return context switch
            {
                IDeviceSelectionOps.LaunchContext.Training => 
                    $"🎓 Abriendo escena para entrenamiento con {deviceName}\n\n📚 Iniciando sesión de aprendizaje seguro.\nPuedes practicar y aprender sin riesgos.",
                    
                IDeviceSelectionOps.LaunchContext.Operations => 
                    $"🏭 Abriendo escena para operaciones industriales con {deviceName}\n\n⚙️ Iniciando operaciones industriales.\n⚠️ Verificar protocolos de seguridad.",
                    
                _ => $"🤖 Lanzando dispositivo: {deviceName}"
            };
        }

        /// <summary>
        /// Muestra un mensaje de lanzamiento en la UI
        /// </summary>
        private void ShowLaunchMessage(string message, bool isError = false)
        {
            // Crear overlay para mensaje
            var overlay = CreateLaunchMessageOverlay(message, isError);
            _uiConfig.Body.Add(overlay);
            
            Debug.Log($"[DeviceSelectionOrchestrator] Launch message shown: {message}");
        }

        /// <summary>
        /// Oculta el mensaje de lanzamiento
        /// </summary>
        private void HideLaunchMessage()
        {
            var overlay = _uiConfig.Body.Q<VisualElement>("LaunchMessageOverlay");
            if (overlay != null)
            {
                _uiConfig.Body.Remove(overlay);
                Debug.Log("[DeviceSelectionOrchestrator] Launch message hidden");
            }
        }

        /// <summary>
        /// Crea el overlay visual para el mensaje de lanzamiento
        /// </summary>
        private VisualElement CreateLaunchMessageOverlay(string message, bool isError = false)
        {
            // Contenedor principal del overlay
            var overlay = new VisualElement();
            overlay.name = "LaunchMessageOverlay";
            overlay.style.position = Position.Absolute;
            overlay.style.width = Length.Percent(100);
            overlay.style.height = Length.Percent(100);
            overlay.style.backgroundColor = new Color(0, 0, 0, 0.8f);
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;

            // Panel del mensaje
            var messagePanel = new VisualElement();
            messagePanel.style.backgroundColor = Color.black;
            messagePanel.style.borderTopColor = isError ? Color.red : Color.green;
            messagePanel.style.borderBottomColor = isError ? Color.red : Color.green;
            messagePanel.style.borderLeftColor = isError ? Color.red : Color.green;
            messagePanel.style.borderRightColor = isError ? Color.red : Color.green;
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
            messageLabel.style.color = isError ? Color.red : Color.green;
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

            var progressLabel = new Label(isError ? "❌ Error" : "⏳ Preparando...");
            progressLabel.style.color = isError ? Color.red : Color.yellow;
            progressLabel.style.fontSize = 20;
            progressLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            progressContainer.Add(progressLabel);

            // Ensamblar el panel
            messagePanel.Add(messageLabel);
            if (!isError)
            {
                messagePanel.Add(progressContainer);
            }

            overlay.Add(messagePanel);

            return overlay;
        }

        #endregion

        #region Private Implementation Methods

        /// <summary>
        /// Obtiene componentes UI del Device Selection
        /// </summary>
        private void GetUiComponents(VisualElement root)
        {
            Debug.Log("Getting Device Selection UI components...");
    
            // Contenedores principales
            _uiConfig.Body = root.Q<VisualElement>("Body");
            _uiConfig.SubpanelsContainer = _subpanelsAndSmokeMaskContainer;
            _uiConfig.Scrim = _subpanelsAndSmokeMaskContainer?.Q<VisualElement>("Scrim");
            _uiConfig.MainContentArea = root.Q<VisualElement>("Main");
            _uiConfig.DeviceGridContainer = root.Q<VisualElement>("DeviceGrid");
            
            // Labels contextuales
            _uiConfig.SelectedModeLabel = root.Q<Label>("SelectedModeUiName");
            _uiConfig.ContextTitleLabel = root.Q<Label>("ContextTitle");

            // Configurar paneles
            InitializePanelConfiguration(root);
    
            Debug.Log("Device Selection UI components obtained successfully");
        }

        /// <summary>
        /// Inicializa la configuración de paneles
        /// </summary>
        private void InitializePanelConfiguration(VisualElement root)
        {
            var panelsContainer = _subpanelsAndSmokeMaskContainer;
            
            // Panel de menú de navegación (usando misma estructura que Dashboard)
            _uiConfig.Panels[IDeviceSelectionOps.PanelType.NavigationMenu] = new DeviceSelectionInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer?.Q<VisualElement>("NavigationMenuPanel"),
                ShowClass = "NavigationMenuPanelinMainScreen",
                HideClass = "NavigationMenuPanelOutMainScreen",
                RequiresScrim = true,
                AnimationDuration = 0.3f
            };

            // Paneles futuros (no existen en UXML actual)
            _uiConfig.Panels[IDeviceSelectionOps.PanelType.DeviceInfo] = new DeviceSelectionInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "DeviceInfoPanelVisible",
                HideClass = "DeviceInfoPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IDeviceSelectionOps.PanelType.ContextInfo] = new DeviceSelectionInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "ContextInfoPanelVisible",
                HideClass = "ContextInfoPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IDeviceSelectionOps.PanelType.Settings] = new DeviceSelectionInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "SettingsPanelVisible",
                HideClass = "SettingsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IDeviceSelectionOps.PanelType.Help] = new DeviceSelectionInfo.UIConfiguration.PanelData
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
            
            Debug.Log("Device Selection dependencies search completed");
        }

        /// <summary>
        /// Inicializa el estado del Device Selection
        /// </summary>
        private void InitializeDeviceSelectionState()
        {
            _deviceSelectionState.IsInitialized = true;
            _deviceSelectionState.CurrentSection = "DeviceSelection";
            _deviceSelectionState.CurrentActivePanel = IDeviceSelectionOps.PanelType.None;
            
            Debug.Log("Device Selection state initialized");
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
        private void OnPanelTransitionCompleteHandler(IDeviceSelectionOps.PanelType panelType)
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
        public DeviceSelectionUIManager UIManager => _uiManager;

        /// <summary>
        /// Event Manager asociado
        /// </summary>
        public DeviceSelectionEventManager EventManager => _eventManager;

        /// <summary>
        /// Estado actual del Device Selection
        /// </summary>
        public DeviceSelectionInfo.DeviceSelectionState DeviceSelectionState => _deviceSelectionState;

        /// <summary>
        /// Configuración de dispositivos
        /// </summary>
        public DeviceSelectionInfo.DeviceConfiguration DeviceConfiguration => _deviceConfig;

        /// <summary>
        /// Datos del contexto actual
        /// </summary>
        public DeviceSelectionInfo.ContextData ContextData => _contextData;

        #endregion
    }
}