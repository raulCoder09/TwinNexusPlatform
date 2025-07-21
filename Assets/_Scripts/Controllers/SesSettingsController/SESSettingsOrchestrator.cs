using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controller;
using _Scripts.Controllers.ServiceManagement;
using _Scripts.Controllers.SettingsController;
using _Scripts.Controllers.UiManagement;
using _Scripts.Models.SESManagement;

namespace _Scripts.Controllers.SESSettingsController
{
    /// <summary>
    /// Coordinador principal de AWS Settings - implementa ISESSettingsOps e IUIController
    /// Equivalente a DashboardOrchestrator pero para configuraciones AWS
    /// </summary>
    public class SESSettingsOrchestrator : MonoBehaviour, ISESSettingsOps, IUIController
    {
        #region IUIController Implementation

        public bool RequiresAuthentication => true; // AWS Settings SÍ requiere autenticación
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _uiConfig?.Body?.style.display == DisplayStyle.Flex;
        public string ControllerName => "SESSettingsController";

        // Events from IUIController
        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        #region ISESSettingsOps Implementation

        public bool IsNavigationMenuOpen => _uiManager?.NavigationMenuOpen ?? false;
        public ISESSettingsOps.PanelType CurrentActivePanel => _uiManager?.CurrentActivePanel ?? ISESSettingsOps.PanelType.None;

        // Events from ISESSettingsOps
        public event Action OnNavigationMenuOpened;
        public event Action OnNavigationMenuClosed;
        public event Action<ISESSettingsOps.PanelType> OnPanelTransitionComplete;
        public event Action OnReturnToDashboardRequested;

        #endregion

        #region Private Fields

        private SESSettingsInfo.UIConfiguration _uiConfig = new SESSettingsInfo.UIConfiguration();
        private SESSettingsInfo.AwsConfiguration _awsConfig = new SESSettingsInfo.AwsConfiguration();
        private SESSettingsInfo.SESSettingsState _settingsState = new SESSettingsInfo.SESSettingsState();
        
        private SESSettingsUIManager _uiManager;
        private SESSettingsEventManager _eventManager;
        
        // Referencias a otros controladores
        private UIController _mainUIController;
        
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private UIDocument _uiDocument;
        private bool _isInitialized = false;

        // Referencias a elementos UI del SenderConfigPanel
        private TextField _senderEmailField;
        private TextField _senderNameField;
        private Button _saveSenderConfigButton;

        // Referencias a elementos UI del TemplateManagementPanel
        private VisualElement _templateList;
        private Button _createTemplateButton;

        // Referencias a elementos UI del QuotaPanel
        private Label _quotaMax24HourLabel;
        private Label _quotaSent24HourLabel;
        private Label _quotaRateLabel;
        private Label _quotaUsageLabel;

        // Referencias a elementos UI del EmailVerificationPanel
        private TextField _emailToVerifyField;
        private Button _verifyEmailButton;
        private VisualElement _verifiedEmailsList;

        // Referencias a elementos UI del TestConnectionPanel
        private Button _testConnectionButton;
        private Button _enableDisableServiceButton;
        private Label _testResultLabel;

        private SESManager _sesManager;
        private SettingsOrchestrator _settingsOrchestrator;

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
            if (_subpanelsAndSmokeMaskContainer != null)
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
            
            Debug.Log("[SESSettingsOrchestrator] Started - UI hidden until navigation");
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
                    Debug.Log("SESSettingsOrchestrator already initialized");
                    return true;
                }

                Debug.Log("Initializing SESSettingsOrchestrator...");

                // Obtener UIDocument
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

                // Obtener referencias UI
                GetUiComponents(root);
                
                // Inicializar managers
                _uiManager = new SESSettingsUIManager(_uiConfig);
                _eventManager = new SESSettingsEventManager(_uiManager, OnReturnToDashboardHandler, OnPanelTransitionCompleteHandler, this);
                _eventManager.RegisterEvents(_uiDocument);

                _uiManager.InitializePanelSystem();
                
                // Buscar dependencias
                FindDependencies();
                
                // Configurar estado inicial
                InitializeSESSettingsState();

                // Inicializar SESManager
                _sesManager = ServiceController.Instance?.SESManager;
                if (_sesManager == null)
                {
                    Debug.LogError("SESManager not found!");
                    return false;
                }

                // Suscribirse a eventos de SESManager
                SubscribeToSESManagerEvents();

                // Actualizar estados iniciales
                UpdateUIStates();

                _isInitialized = true;
                Debug.Log("SESSettingsOrchestrator initialized successfully");
                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"SESSettingsOrchestrator initialization error: {ex.Message}");
                OnControllerError?.Invoke(this, $"Initialization failed: {ex.Message}");
                return false;
            }
        }

        private void SubscribeToSESManagerEvents()
        {
            if (_sesManager != null)
            {
                _sesManager.OnInitializationCompleted += OnSESInitializationCompleted;
                _sesManager.OnQuotaUpdated += OnQuotaUpdated;
                _sesManager.OnEmailVerificationRequested += OnEmailVerificationRequested;
                _sesManager.OnEmailOperationCompleted += OnEmailOperationCompleted;
            }
        }

        private void GetUiComponents(VisualElement root)
        {
            // Contenedores principales
            _uiConfig.Body = root.Q<VisualElement>("Body");
            _uiConfig.SubpanelsContainer = _subpanelsAndSmokeMaskContainer;
            _uiConfig.Scrim = _subpanelsAndSmokeMaskContainer?.Q<VisualElement>("Scrim");
            _uiConfig.MainContentArea = root.Q<VisualElement>("Main");
            _uiConfig.HeaderArea = root.Q<VisualElement>("Header");
            _uiConfig.FooterArea = root.Q<VisualElement>("Footer");

            // SenderConfigPanel
            _senderEmailField = root.Q<TextField>("SenderEmailField");
            _senderNameField = root.Q<TextField>("SenderNameField");
            _saveSenderConfigButton = root.Q<Button>("SaveSenderConfigButton");

            // TemplateManagementPanel
            _templateList = root.Q<VisualElement>("TemplateList");
            _createTemplateButton = root.Q<Button>("CreateTemplateButton");

            // QuotaPanel
            _quotaMax24HourLabel = root.Q<Label>("QuotaMax24Hour");
            _quotaSent24HourLabel = root.Q<Label>("QuotaSent24Hour");
            _quotaRateLabel = root.Q<Label>("QuotaRate");
            _quotaUsageLabel = root.Q<Label>("QuotaUsage");

            // EmailVerificationPanel
            _emailToVerifyField = root.Q<TextField>("EmailToVerifyField");
            _verifyEmailButton = root.Q<Button>("VerifyEmailButton");
            _verifiedEmailsList = root.Q<VisualElement>("VerifiedEmailsList");

            // TestConnectionPanel
            _testConnectionButton = root.Q<Button>("TestConnectionButton");
            _enableDisableServiceButton = root.Q<Button>("EnableDisableServiceButton");
            _testResultLabel = root.Q<Label>("TestResult");

            InitializePanelConfiguration(root);
            Debug.Log("AWS Settings UI components obtained successfully");
        }

        private void InitializePanelConfiguration(VisualElement root)
        {
            var panelsContainer = _subpanelsAndSmokeMaskContainer;
            
            // Panel de menú de navegación (SÍ existe en UXML)
            _uiConfig.Panels[ISESSettingsOps.PanelType.NavigationMenu] = new SESSettingsInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer?.Q<VisualElement>("NavigationMenuPanel"),
                ShowClass = "NavigationMenuPanelInMainScreen",
                HideClass = "NavigationMenuPanelOutMainScreen",
                RequiresScrim = true,
                AnimationDuration = 0.3f
            };

            // Paneles futuros específicos de AWS (NO existen en UXML actual)
            _uiConfig.Panels[ISESSettingsOps.PanelType.AwsCredentials] = new SESSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null, // Será null porque no existe en UXML
                ShowClass = "AwsCredentialsPanelVisible",
                HideClass = "AwsCredentialsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ISESSettingsOps.PanelType.ServiceConfig] = new SESSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null, // Será null porque no existe en UXML
                ShowClass = "ServiceConfigPanelVisible",
                HideClass = "ServiceConfigPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ISESSettingsOps.PanelType.TestResults] = new SESSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null, // Será null porque no existe en UXML
                ShowClass = "TestResultsPanelVisible",
                HideClass = "TestResultsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ISESSettingsOps.PanelType.SecuritySettings] = new SESSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null, // Será null porque no existe en UXML
                ShowClass = "SecuritySettingsPanelVisible",
                HideClass = "SecuritySettingsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ISESSettingsOps.PanelType.RegionSettings] = new SESSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null, // Será null porque no existe en UXML
                ShowClass = "RegionSettingsPanelVisible",
                HideClass = "RegionSettingsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ISESSettingsOps.PanelType.Help] = new SESSettingsInfo.UIConfiguration.PanelData
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

        private void UpdateUIStates()
        {
            // Actualizar SenderConfigPanel
            if (_sesManager != null)
            {
                _senderEmailField.value = _sesManager._senderEmail;
                _senderNameField.value = _sesManager._senderName;
            }

            // Actualizar QuotaPanel
            UpdateQuotaStatus();

            // Actualizar VerifiedEmailsList
            UpdateVerifiedEmailsList();
        }

        private async void UpdateQuotaStatus()
        {
            if (_sesManager != null)
            {
                var quota = await _sesManager.GetSendQuotaAsync();
                _quotaMax24HourLabel.text = $"Max 24h Send: {quota.Max24HourSend}";
                _quotaSent24HourLabel.text = $"Sent Last 24h: {quota.SentLast24Hours}";
                _quotaRateLabel.text = $"Max Send Rate: {quota.MaxSendRate}/sec";
                _quotaUsageLabel.text = $"Usage: {quota.UsagePercentage:F1}%";
                ApplyQuotaStatusStyles(quota);
            }
        }
        
        private void ApplyQuotaStatusStyles(SESQuotaInfo quota)
        {
            _quotaUsageLabel.RemoveFromClassList("status-warning");
            _quotaUsageLabel.RemoveFromClassList("status-error");
            if (quota.IsNearLimit)
            {
                _quotaUsageLabel.AddToClassList(quota.UsagePercentage >= 90 ? "status-error" : "status-warning");
            }
        }

        private async void UpdateVerifiedEmailsList()
        {
            if (_sesManager != null)
            {
                var verifiedEmails = await _sesManager.GetVerifiedEmailsAsync();
                _verifiedEmailsList.Clear();
                foreach (var email in verifiedEmails)
                {
                    var label = new Label($"{email} (Verified)");
                    label.AddToClassList("recent-activity-item");
                    _verifiedEmailsList.Add(label);
                }
            }
        }

        public void Show()
        {
            if (!ServiceController.Instance.IsCognitoAuthenticated)
            {
                Debug.LogError("Cannot show AWS Settings - user not authenticated");
                OnControllerError?.Invoke(this, "Authentication required");
                _mainUIController?.ShowUI("Welcome");
                return;
            }

            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.Flex;
                LoadAwsConfiguration();
                UpdateUIStates();
                OnControllerShown?.Invoke(this);
                Debug.Log("[SESSettingsOrchestrator] AWS Settings UI shown");
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
                Debug.Log("[SESSettingsOrchestrator] AWS Settings UI hidden");
            }
        }

        public void Cleanup()
        {
            try
            {
                // Desuscribirse de eventos de SESManager
                if (_sesManager != null)
                {
                    _sesManager.OnInitializationCompleted -= OnSESInitializationCompleted;
                    _sesManager.OnQuotaUpdated -= OnQuotaUpdated;
                    _sesManager.OnEmailVerificationRequested -= OnEmailVerificationRequested;
                    _sesManager.OnEmailOperationCompleted -= OnEmailOperationCompleted;
                }

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
                _settingsOrchestrator = null;
                // Limpiar referencias UI
                _senderEmailField = null;
                _senderNameField = null;
                _saveSenderConfigButton = null;
                _templateList = null;
                _createTemplateButton = null;
                _quotaMax24HourLabel = null;
                _quotaSent24HourLabel = null;
                _quotaRateLabel = null;
                _quotaUsageLabel = null;
                _emailToVerifyField = null;
                _verifyEmailButton = null;
                _verifiedEmailsList = null;
                _testConnectionButton = null;
                _enableDisableServiceButton = null;
                _testResultLabel = null;
                _sesManager = null;

                _isInitialized = false;
                Debug.Log("[SESSettingsOrchestrator] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SESSettingsOrchestrator] Cleanup error: {ex.Message}");
            }
        }

        #endregion

        #region ISESSettingsOps Implementation

        public void NavigateToPanel(ISESSettingsOps.PanelType panelType)
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

        public void SwitchPanel(ISESSettingsOps.PanelType fromPanel, ISESSettingsOps.PanelType toPanel)
        {
            _uiManager?.SwitchPanel(fromPanel, toPanel);
        }

        public void OpenAwsConfiguration()
        {
            Debug.Log("Opening AWS Configuration panel");
            // TODO: Implementar cuando se agregue contenido específico para el panel AwsCredentials
        }

        public async void SaveConfiguration()
        {
            await SaveConfigurationAsync();
        }

        public void ResetConfiguration()
        {
            if (_sesManager != null)
            {
                _senderEmailField.value = _sesManager._senderEmail;
                _senderNameField.value = _sesManager._senderName;
                _settingsState.HasUnsavedChanges = false;
                _uiManager.UpdateCredentialsStatus(true, "Configuration reset");
                Debug.Log("AWS Configuration reset to default");
            }
        }

        public async void TestConnection()
        {
            await TestConnectionAsync();
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

        public async Task RequestEmailVerification()
        {
            if (_sesManager != null && SESInfo.Validation.IsValidEmail(_emailToVerifyField.value))
            {
                var email = _emailToVerifyField.value;
                var success = await _sesManager.RequestEmailVerificationAsync(email);
                if (success)
                {
                    Debug.Log($"Verification requested for {email}");
                    UpdateVerifiedEmailsList();
                }
                else
                {
                    Debug.LogError($"Failed to request verification for {email}");
                }
            }
            else
            {
                Debug.LogError("Invalid email to verify");
            }
        }

        public void CreateNewTemplate()
        {
            Debug.Log("Creating new email template (to be implemented)");
            // TODO: Implementar lógica para abrir un modal o formulario para crear plantilla
        }

        public async void ToggleService()
        {
            Debug.Log("Toggling SES service state");
            _awsConfig.ServiceStates["SES"] = !_awsConfig.ServiceStates.GetValueOrDefault("SES", false);
            _uiManager.UpdateServiceStates(_awsConfig.ServiceStates);
            if (_awsConfig.ServiceStates["SES"] && _sesManager != null)
            {
                await _sesManager.InitializeAsync();
            }
        }

        #endregion

        #region Public Event Handlers (Called by EventManager)

        public void HandleDashboardClick()
        {
            Debug.Log("Dashboard button clicked - returning to Dashboard");
    
            // Cerrar menú y ocultar AWS Settings
            _uiManager?.HideNavigationMenu();
            Hide();
    
            // Mostrar Dashboard
            _mainUIController?.ShowUI("Dashboard");
        }

        public void HandleReportsClick()
        {
            Debug.Log("Reports button clicked - opening Reports Center");
    
            // Cerrar menú y ocultar AWS Settings
            _uiManager?.HideNavigationMenu();
            Hide();
    
            // Mostrar reports controller
            _mainUIController?.ShowUI("Reports");
        }

        public void HandleOperationsClick()
        {
            var parameters = new Dictionary<string, object> {
                ["context"] = "Operations", 
                ["sourceController"] = "SESSettings"
            };
            
            // Cerrar menú y ocultar AWS Settings
            _uiManager?.HideNavigationMenu();
            Hide();
            
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }

        public void HandleTrainingClick()
        {
            var parameters = new Dictionary<string, object> {
                ["context"] = "Training",
                ["sourceController"] = "SESSettings"
            };
            
            // Cerrar menú y ocultar AWS Settings
            _uiManager?.HideNavigationMenu();
            Hide();
            
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }

        public void HandleSupportClick()
        {
            Debug.Log("Support button clicked - opening support center");
    
            // Cerrar menú y ocultar AWS Settings
            _uiManager?.HideNavigationMenu();
            Hide();
    
            // Mostrar support controller
            _mainUIController?.ShowUI("Support");
        }

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

        #endregion
        

        private void FindDependencies()
        {
            // Buscar UIController principal
            _mainUIController = UIController.Instance;
            if (_mainUIController == null)
            {
                Debug.LogWarning("UIController not found - will try to find it later");
            }
            _settingsOrchestrator = FindObjectOfType<SettingsOrchestrator>();
        }

        private void InitializeSESSettingsState()
        {
            _settingsState.IsInitialized = true;
            _settingsState.CurrentSection = "SESSettings";
            _settingsState.CurrentActivePanel = ISESSettingsOps.PanelType.None;
            
            // Inicializar configuración AWS básica
            _awsConfig.Region = "us-east-1";
            _awsConfig.ServiceStates = new Dictionary<string, bool>();
            _awsConfig.ServiceConfigs = new Dictionary<string, Dictionary<string, object>>();
            _awsConfig.TestResults = new Dictionary<string, SESSettingsInfo.ConnectionTestResult>();
            
            Debug.Log("AWS Settings state initialized");
        }

        private void LoadAwsConfiguration()
        {
            Debug.Log("Loading AWS configuration...");
            var serviceController = ServiceController.Instance;
            if (serviceController != null)
            {
                _settingsState.HasValidCredentials = serviceController.IsCognitoAuthenticated;
                Debug.Log($"AWS configuration loaded - Valid credentials: {_settingsState.HasValidCredentials}");
            }
        }

        #region Event Handlers

        private void OnTransitionEndEvent(TransitionEndEvent evt)
        {
            if (!_uiManager.IsAnyPanelVisible())
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
                Debug.Log("All panels closed - hiding container");
            }
        }

        private void OnReturnToDashboardHandler()
        {
            OnReturnToDashboardRequested?.Invoke();
        }

        private void OnPanelTransitionCompleteHandler(ISESSettingsOps.PanelType panelType)
        {
            OnPanelTransitionComplete?.Invoke(panelType);
            Debug.Log($"Panel transition complete: {panelType}");
        }

        private void OnSESInitializationCompleted(bool success, string message)
        {
            Debug.Log($"SES initialization: {message}");
            UpdateUIStates();
        }

        private void OnQuotaUpdated(SESQuotaInfo quota)
        {
            UpdateQuotaStatus();
        }

        private void OnEmailVerificationRequested(string email, string result)
        {
            Debug.Log($"Email verification requested for {email}: {result}");
            UpdateVerifiedEmailsList();
        }

        private void OnEmailOperationCompleted(SESOperationResult result)
        {
            Debug.Log($"Email operation completed: {result.Message}");
        }

        #endregion

        #region Helper Methods

        internal void ShowUi()
        {
            Show();
        }

        internal void HideUi()
        {
            Hide();
        }

        public void UpdateCredentialsStatus(bool isValid, string message = null)
        {
            _settingsState.HasValidCredentials = isValid;
            _uiManager?.UpdateCredentialsStatus(isValid, message);
            _settingsState.TriggerCredentialsValidityChanged(isValid);
            Debug.Log($"AWS credentials status updated: {isValid} - {message}");
        }

        public void UpdateConnectionTestResults(Dictionary<string, SESSettingsInfo.ConnectionTestResult> results)
        {
            _awsConfig.TestResults = results;
            _uiManager?.UpdateConnectionTestResults(results);
            foreach (var kvp in results)
            {
                _settingsState.TriggerConnectionTestCompleted(kvp.Key, kvp.Value);
            }
            Debug.Log($"Connection test results updated for {results.Count} services");
        }

        public void UpdateServiceConfiguration(string serviceName, Dictionary<string, object> config)
        {
            _awsConfig.ServiceConfigs[serviceName] = config;
            _awsConfig.ServiceStates[serviceName] = true;
            _settingsState.HasUnsavedChanges = true;
            Debug.Log($"Service configuration updated for {serviceName}");
        }

        private async Task SaveConfigurationAsync()
        {
            if (_sesManager != null)
            {
                var newSenderEmail = _senderEmailField.value;
                var newSenderName = _senderNameField.value;

                if (SESInfo.Validation.IsValidEmail(newSenderEmail) && !string.IsNullOrWhiteSpace(newSenderName))
                {
                    var success = await _sesManager.ReinitializeAsync(newSenderEmail, newSenderName);
                    if (success)
                    {
                        Debug.Log("Sender configuration saved successfully");
                        _settingsState.HasUnsavedChanges = false;
                        _uiManager.UpdateCredentialsStatus(true, "Configuration saved");
                    }
                    else
                    {
                        Debug.LogError("Failed to save sender configuration");
                        _uiManager.UpdateCredentialsStatus(false, "Failed to save configuration");
                    }
                }
                else
                {
                    Debug.LogError("Invalid sender email or name");
                    _uiManager.UpdateCredentialsStatus(false, "Invalid email or name");
                }
            }
        }

        private async Task TestConnectionAsync()
        {
            if (_sesManager != null)
            {
                _settingsState.IsTestingConnection = true;
                _testResultLabel.text = "Result: Testing...";
                var success = await _sesManager.SendTestEmailAsync();
                _testResultLabel.text = $"Result: {(success ? "Success" : "Failed")}";
                _testResultLabel.RemoveFromClassList("status-success");
                _testResultLabel.RemoveFromClassList("status-error");
                _testResultLabel.AddToClassList(success ? "status-success" : "status-error");
                _settingsState.IsTestingConnection = false;

                var testResult = new SESSettingsInfo.ConnectionTestResult
                {
                    ServiceName = "SES",
                    IsSuccessful = success,
                    Message = success ? "Connection test successful" : "Connection test failed",
                    TestTime = DateTime.Now,
                    ResponseTime = TimeSpan.FromMilliseconds(150) // Simulado, ajustar según respuesta real
                };
                _awsConfig.TestResults["SES"] = testResult;
                _uiManager.UpdateConnectionTestResults(_awsConfig.TestResults);
            }
        }

        #endregion
    }
}