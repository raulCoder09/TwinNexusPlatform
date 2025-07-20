using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controller;
using _Scripts.Controllers.SettingsController;
using _Scripts.Controllers.UiManagement;
using _Scripts.Models.IoTCoreManagement;

namespace _Scripts.Controllers.IotCoreSettingsController
{
    public class IotCoreSettingsOrchestrator : MonoBehaviour, IIotCoreSettingsOps, IUIController
    {
        #region IUIController Implementation

        public bool RequiresAuthentication => true;
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _uiConfig?.Body?.style.display == DisplayStyle.Flex;
        public string ControllerName => "IotCoreSettingsController";

        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        #region IIotCoreSettingsOps Implementation

        public bool IsNavigationMenuOpen => _uiManager?.NavigationMenuOpen ?? false;
        public IIotCoreSettingsOps.PanelType CurrentActivePanel => _uiManager?.CurrentActivePanel ?? IIotCoreSettingsOps.PanelType.None;

        public event Action OnNavigationMenuOpened;
        public event Action OnNavigationMenuClosed;
        public event Action<IIotCoreSettingsOps.PanelType> OnPanelTransitionComplete;
        public event Action OnReturnToDashboardRequested;

        #endregion

        #region Private Fields

        private IotCoreSettingsInfo.UIConfiguration _uiConfig = new IotCoreSettingsInfo.UIConfiguration();
        private IotCoreSettingsInfo.AwsConfiguration _awsConfig = new IotCoreSettingsInfo.AwsConfiguration();
        private IotCoreSettingsInfo.IotCoreSettingsState _settingsState = new IotCoreSettingsInfo.IotCoreSettingsState();
        
        private IotCoreSettingsUIManager _uiManager;
        private IotCoreSettingsEventManager _eventManager;
        
        private UIController _mainUIController;
        
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private UIDocument _uiDocument;
        private bool _isInitialized = false;

        // Referencias a elementos UI del ThingsConfigPanel
        private TextField _thingNameField;
        private TextField _thingTypeField;
        private Button _createThingButton;

        // Referencias a elementos UI del PolicyConfigPanel
        private TextField _policyNameField;
        private TextField _policyDocumentField;
        private Button _createPolicyButton;

        // Referencias a elementos UI del CertificatesConfigPanel
        private Button _createCertificateButton;
        private TextField _certificateIdField;
        private TextField _thingNameForCertificateField;
        private Button _attachCertificateButton;
        private TextField _policyNameForCertificateField;
        private Button _attachPolicyButton;

        // Referencias a elementos UI del ThingsStatusPanel
        private VisualElement _thingsList;
        private Button _refreshThingsButton;

        // Referencias a elementos UI del TestPanel
        private Button _testConnectivityButton;
        private Button _enableDisableServiceButton;
        private Label _testResultLabel;

        private IoTCoreManager _iotCoreManager;
        private SettingsOrchestrator _settingsOrchestrator;
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            HideUi();
            if (_subpanelsAndSmokeMaskContainer != null)
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
            
            Debug.Log("[IotCoreSettingsOrchestrator] Started - UI hidden until navigation");
        }
        
        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion
        

        public bool Initialize()
        {
            try
            {
                if (_isInitialized)
                {
                    Debug.Log("IotCoreSettingsOrchestrator already initialized");
                    return true;
                }

                Debug.Log("Initializing IotCoreSettingsOrchestrator...");

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

                GetUiComponents(root);
                
                _uiManager = new IotCoreSettingsUIManager(_uiConfig);
                _eventManager = new IotCoreSettingsEventManager(_uiManager, OnReturnToDashboardHandler, OnPanelTransitionCompleteHandler, this);
                _eventManager.RegisterEvents(_uiDocument);

                _uiManager.InitializePanelSystem();
                
                FindDependencies();
                
                InitializeIotCoreSettingsState();

                _iotCoreManager = ServiceController.Instance?.IoTCoreManager;
                if (_iotCoreManager == null)
                {
                    Debug.LogError("IoTCoreManager not found!");
                    return false;
                }

                SubscribeToIoTCoreManagerEvents();

                UpdateUIStates();

                _isInitialized = true;
                Debug.Log("IotCoreSettingsOrchestrator initialized successfully");
                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"IotCoreSettingsOrchestrator initialization error: {ex.Message}");
                OnControllerError?.Invoke(this, $"Initialization failed: {ex.Message}");
                return false;
            }
        }

        private void SubscribeToIoTCoreManagerEvents()
        {
            if (_iotCoreManager != null)
            {
                _iotCoreManager.SubscribeToThingsListed(OnThingsListed);
                _iotCoreManager.SubscribeToThingCreated(OnThingCreated);
                _iotCoreManager.SubscribeToPolicyCreated(OnPolicyCreated);
                _iotCoreManager.SubscribeToCertificateCreated(OnCertificateCreated);
                _iotCoreManager.SubscribeToCertificateAttached(OnCertificateAttached);
                _iotCoreManager.SubscribeToPolicyAttached(OnPolicyAttached);
            }
        }

        private void GetUiComponents(VisualElement root)
        {
            _uiConfig.Body = root.Q<VisualElement>("Body");
            _uiConfig.SubpanelsContainer = _subpanelsAndSmokeMaskContainer;
            _uiConfig.Scrim = _subpanelsAndSmokeMaskContainer?.Q<VisualElement>("Scrim");
            _uiConfig.MainContentArea = root.Q<VisualElement>("Main");
            _uiConfig.HeaderArea = root.Q<VisualElement>("Header");
            _uiConfig.FooterArea = root.Q<VisualElement>("Footer");

            // ThingsConfigPanel
            _thingNameField = root.Q<TextField>("ThingNameField");
            _thingTypeField = root.Q<TextField>("ThingTypeField");
            _createThingButton = root.Q<Button>("CreateThingButton");

            // PolicyConfigPanel
            _policyNameField = root.Q<TextField>("PolicyNameField");
            _policyDocumentField = root.Q<TextField>("PolicyDocumentField");
            _createPolicyButton = root.Q<Button>("CreatePolicyButton");

            // CertificatesConfigPanel
            _createCertificateButton = root.Q<Button>("CreateCertificateButton");
            _certificateIdField = root.Q<TextField>("CertificateIdField");
            _thingNameForCertificateField = root.Q<TextField>("ThingNameForCertificateField");
            _attachCertificateButton = root.Q<Button>("AttachCertificateButton");
            _policyNameForCertificateField = root.Q<TextField>("PolicyNameForCertificateField");
            _attachPolicyButton = root.Q<Button>("AttachPolicyButton");

            // ThingsStatusPanel
            _thingsList = root.Q<VisualElement>("ThingsList");
            _refreshThingsButton = root.Q<Button>("RefreshThingsButton");

            // TestPanel
            _testConnectivityButton = root.Q<Button>("TestConnectivityButton");
            _enableDisableServiceButton = root.Q<Button>("EnableDisableServiceButton");
            _testResultLabel = root.Q<Label>("TestResult");

            InitializePanelConfiguration(root);
            Debug.Log("IoT Core Settings UI components obtained successfully");
        }

        private void InitializePanelConfiguration(VisualElement root)
        {
            var panelsContainer = _subpanelsAndSmokeMaskContainer;
            
            _uiConfig.Panels[IIotCoreSettingsOps.PanelType.NavigationMenu] = new IotCoreSettingsInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer?.Q<VisualElement>("NavigationMenuPanel"),
                ShowClass = "NavigationMenuPanelInMainScreen",
                HideClass = "NavigationMenuPanelOutMainScreen",
                RequiresScrim = true,
                AnimationDuration = 0.3f
            };

            _uiConfig.Panels[IIotCoreSettingsOps.PanelType.AwsCredentials] = new IotCoreSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "AwsCredentialsPanelVisible",
                HideClass = "AwsCredentialsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IIotCoreSettingsOps.PanelType.ServiceConfig] = new IotCoreSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "ServiceConfigPanelVisible",
                HideClass = "ServiceConfigPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IIotCoreSettingsOps.PanelType.TestResults] = new IotCoreSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "TestResultsPanelVisible",
                HideClass = "TestResultsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IIotCoreSettingsOps.PanelType.SecuritySettings] = new IotCoreSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "SecuritySettingsPanelVisible",
                HideClass = "SecuritySettingsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IIotCoreSettingsOps.PanelType.RegionSettings] = new IotCoreSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "RegionSettingsPanelVisible",
                HideClass = "RegionSettingsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IIotCoreSettingsOps.PanelType.Help] = new IotCoreSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "HelpPanelVisible",
                HideClass = "HelpPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

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
            if (_iotCoreManager != null)
            {
                UpdateThingsList();
            }
        }

        private async void UpdateThingsList()
        {
            if (_iotCoreManager != null)
            {
                var things = await _iotCoreManager.ListThingsAsync();
                _thingsList.Clear();
                foreach (var thing in things)
                {
                    var label = new Label($"{thing.ThingName} (Type: {thing.ThingTypeName ?? "None"})");
                    label.AddToClassList("recent-activity-item");
                    _thingsList.Add(label);
                }
            }
        }

        public void Show()
        {
            if (!ServiceController.Instance.IsCognitoAuthenticated)
            {
                Debug.LogError("Cannot show IoT Core Settings - user not authenticated");
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
                Debug.Log("[IotCoreSettingsOrchestrator] IoT Core Settings UI shown");
            }
        }
        
        /// <summary>
        /// Maneja clic en botón Settings
        /// </summary>
        public void HandleSettingsClick()
        {
            Debug.Log("Settings button clicked - opening settings");
            
            // Cerrar menú y ocultar dashboard
            _uiManager?.HideNavigationMenu();
            Hide();
            
            // Mostrar settings controller
            if (_settingsOrchestrator != null)
            {
                _settingsOrchestrator.ShowUi();
            }
            else
            {
                // Fallback: usar UIController
                _mainUIController?.ShowUI("Settings");
            }
        }

        public void Hide()
        {
            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.None;
                _uiManager?.CloseCurrentPanel();
                OnControllerHidden?.Invoke(this);
                Debug.Log("[IotCoreSettingsOrchestrator] IoT Core Settings UI hidden");
            }
        }

        internal void HideUi()
        {
            Hide();
        }

        public void Cleanup()
        {
            try
            {
                if (_iotCoreManager != null)
                {
                    _iotCoreManager.UnsubscribeFromThingsListed(OnThingsListed);
                    _iotCoreManager.UnsubscribeFromThingCreated(OnThingCreated);
                    _iotCoreManager.UnsubscribeFromPolicyCreated(OnPolicyCreated);
                    _iotCoreManager.UnsubscribeFromCertificateCreated(OnCertificateCreated);
                    _iotCoreManager.UnsubscribeFromCertificateAttached(OnCertificateAttached);
                    _iotCoreManager.UnsubscribeFromPolicyAttached(OnPolicyAttached);
                }

                _eventManager?.Cleanup();
                _uiManager = null;
                _eventManager = null;

                _mainUIController = null;
                _uiConfig = null;
                _awsConfig = null;
                _settingsState = null;
                _uiDocument = null;
                _subpanelsAndSmokeMaskContainer = null;
                _settingsOrchestrator = null;
                _thingNameField = null;
                _thingTypeField = null;
                _createThingButton = null;
                _policyNameField = null;
                _policyDocumentField = null;
                _createPolicyButton = null;
                _createCertificateButton = null;
                _certificateIdField = null;
                _thingNameForCertificateField = null;
                _attachCertificateButton = null;
                _policyNameForCertificateField = null;
                _attachPolicyButton = null;
                _thingsList = null;
                _refreshThingsButton = null;
                _testConnectivityButton = null;
                _enableDisableServiceButton = null;
                _testResultLabel = null;
                _iotCoreManager = null;

                _isInitialized = false;
                Debug.Log("[IotCoreSettingsOrchestrator] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IotCoreSettingsOrchestrator] Cleanup error: {ex.Message}");
            }
        }

        #region IIotCoreSettingsOps Implementation

        public void NavigateToPanel(IIotCoreSettingsOps.PanelType panelType)
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

        public void SwitchPanel(IIotCoreSettingsOps.PanelType fromPanel, IIotCoreSettingsOps.PanelType toPanel)
        {
            _uiManager?.SwitchPanel(fromPanel, toPanel);
        }

        public void OpenAwsConfiguration()
        {
            Debug.Log("Opening AWS Configuration panel");
        }

        public async void SaveConfiguration()
        {
            await SaveConfigurationAsync();
        }

        public void ResetConfiguration()
        {
            if (_iotCoreManager != null)
            {
                _thingNameField.value = "Unity-Test-Device";
                _thingTypeField.value = "Enter thing type (optional)";
                _policyNameField.value = "Unity-IoT-Policy";
                _policyDocumentField.value = "Enter JSON policy document";
                _certificateIdField.value = "Enter certificate ID";
                _thingNameForCertificateField.value = "Enter thing name";
                _policyNameForCertificateField.value = "Enter policy name";
                _settingsState.HasUnsavedChanges = false;
                _uiManager.UpdateCredentialsStatus(true, "Configuration reset");
                Debug.Log("IoT Core Configuration reset to default");
            }
        }

        public async Task TestConnection()
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

        public async Task CreateThing()
        {
            if (_iotCoreManager != null)
            {
                var thingName = _thingNameField.value;
                var thingType = _thingTypeField.value;
                if (string.IsNullOrWhiteSpace(thingName) || thingName == "Unity-Test-Device")
                {
                    Debug.LogError("Thing name cannot be empty");
                    return;
                }
                if (thingType == "Enter thing type (optional)")
                {
                    thingType = null;
                }
                var success = await _iotCoreManager.CreateThingAsync(thingName, thingType);
                if (success)
                {
                    Debug.Log($"Thing {thingName} created successfully");
                    UpdateThingsList();
                }
                else
                {
                    Debug.LogError($"Failed to create Thing {thingName}");
                }
            }
        }

        public async Task CreatePolicy()
        {
            if (_iotCoreManager != null)
            {
                var policyName = _policyNameField.value;
                var policyDocument = _policyDocumentField.value;
                if (string.IsNullOrWhiteSpace(policyName) || policyName == "Unity-IoT-Policy")
                {
                    Debug.LogError("Policy name cannot be empty");
                    return;
                }
                if (policyDocument == "Enter JSON policy document")
                {
                    policyDocument = null;
                }
                var success = await _iotCoreManager.CreatePolicyAsync(policyName, policyDocument);
                if (success)
                {
                    Debug.Log($"Policy {policyName} created successfully");
                }
                else
                {
                    Debug.LogError($"Failed to create policy {policyName}");
                }
            }
        }

        public async Task CreateCertificate()
        {
            if (_iotCoreManager != null)
            {
                var certificateData = await _iotCoreManager.CreateThingCertificateAsync();
                if (certificateData != null)
                {
                    Debug.Log($"Certificate {certificateData.CertificateId.Substring(0, Math.Min(8, certificateData.CertificateId.Length))}... created successfully");
                    _certificateIdField.value = certificateData.CertificateId;
                }
                else
                {
                    Debug.LogError("Failed to create certificate");
                }
            }
        }

        public async Task AttachCertificate()
        {
            if (_iotCoreManager != null)
            {
                var certificateId = _certificateIdField.value;
                var thingName = _thingNameForCertificateField.value;
                if (string.IsNullOrWhiteSpace(certificateId) || certificateId == "Enter certificate ID" ||
                    string.IsNullOrWhiteSpace(thingName) || thingName == "Enter thing name")
                {
                    Debug.LogError("Certificate ID and Thing name cannot be empty");
                    return;
                }
                var success = await _iotCoreManager.AttachCertificateToThingAsync(certificateId, thingName);
                if (success)
                {
                    Debug.Log($"Certificate {certificateId.Substring(0, Math.Min(8, certificateId.Length))}... attached to {thingName}");
                }
                else
                {
                    Debug.LogError($"Failed to attach certificate {certificateId}");
                }
            }
        }

        public async Task AttachPolicy()
        {
            if (_iotCoreManager != null)
            {
                var policyName = _policyNameForCertificateField.value;
                var certificateId = _certificateIdField.value;
                if (string.IsNullOrWhiteSpace(policyName) || policyName == "Enter policy name" ||
                    string.IsNullOrWhiteSpace(certificateId) || certificateId == "Enter certificate ID")
                {
                    Debug.LogError("Policy name and certificate ID cannot be empty");
                    return;
                }
                var success = await _iotCoreManager.AttachPolicyAsync(policyName, certificateId);
                if (success)
                {
                    Debug.Log($"Policy {policyName} attached to certificate {certificateId.Substring(0, Math.Min(8, certificateId.Length))}...");
                }
                else
                {
                    Debug.LogError($"Failed to attach policy {policyName}");
                }
            }
        }

        public async Task RefreshThings()
        {
            UpdateThingsList();
        }

        public async Task TestConnectionAsync()
        {
            if (_iotCoreManager != null)
            {
                _settingsState.IsTestingConnection = true;
                _testResultLabel.text = "Result: Testing...";
                var success = await _iotCoreManager.TestIoTConnectivityAsync();
                _testResultLabel.text = $"Result: {(success ? "Success" : "Failed")}";
                _testResultLabel.RemoveFromClassList("status-success");
                _testResultLabel.RemoveFromClassList("status-error");
                _testResultLabel.AddToClassList(success ? "status-success" : "status-error");
                _settingsState.IsTestingConnection = false;

                var testResult = new IotCoreSettingsInfo.ConnectionTestResult
                {
                    ServiceName = "IoTCore",
                    IsSuccessful = success,
                    Message = success ? "Connection test successful" : "Connection test failed",
                    TestTime = DateTime.Now,
                    ResponseTime = TimeSpan.FromMilliseconds(150)
                };
                _awsConfig.TestResults["IoTCore"] = testResult;
                _uiManager.UpdateConnectionTestResults(_awsConfig.TestResults);
            }
        }

        public async Task ToggleService()
        {
            Debug.Log("Toggling IoT Core service state");
            _awsConfig.ServiceStates["IoTCore"] = !_awsConfig.ServiceStates.GetValueOrDefault("IoTCore", false);
            _uiManager.UpdateServiceStates(_awsConfig.ServiceStates);
            if (_awsConfig.ServiceStates["IoTCore"] && _iotCoreManager != null)
            {
                await _iotCoreManager.InitializeAsync(ServiceController.Instance.CognitoManager.CurrentAWSCredentials, ServiceController.Instance.CognitoManager.GetRegionEndpoint(), "156041417101");
            }
        }

        #endregion

        #region Public Event Handlers

        public async Task HandleDashboardClick()
        {
            Debug.Log("Dashboard button clicked - returning to Dashboard");
            _uiManager?.HideNavigationMenu();
            Hide();
            await Task.Run(() => _mainUIController?.ShowUI("Dashboard"));
        }

        public async Task HandleReportsClick()
        {
            Debug.Log("Reports button clicked - opening Reports Center");
            _uiManager?.HideNavigationMenu();
            Hide();
            await Task.Run(() => _mainUIController?.ShowUI("Reports"));
        }

        public async Task HandleOperationsClick()
        {
            var parameters = new Dictionary<string, object> {
                ["context"] = "Operations", 
                ["sourceController"] = "IotCoreSettings"
            };
            _uiManager?.HideNavigationMenu();
            Hide();
            await Task.Run(() => _mainUIController?.ShowUI("DeviceSelection", parameters));
        }

        public async Task HandleTrainingClick()
        {
            var parameters = new Dictionary<string, object> {
                ["context"] = "Training",
                ["sourceController"] = "IotCoreSettings"
            };
            _uiManager?.HideNavigationMenu();
            Hide();
            await Task.Run(() => _mainUIController?.ShowUI("DeviceSelection", parameters));
        }

        public async Task HandleSupportClick()
        {
            Debug.Log("Support button clicked - opening support center");
            _uiManager?.HideNavigationMenu();
            Hide();
            await Task.Run(() => _mainUIController?.ShowUI("Support"));
        }

        public async Task HandleLogoutClick()
        {
            Debug.Log("IoT Core Settings HandleLogoutClick() called");
            _uiManager?.HideNavigationMenu();
            Hide();
            var uiController = UIController.Instance;
            if (uiController != null)
            {
                Debug.Log("Calling UIController.RequestLogout()");
                await Task.Run(() => uiController.RequestLogout());
            }
            else
            {
                Debug.Log("UIController not found - doing direct logout");
                ServiceController.Instance?.CognitoManager?.SignOut();
            }
        }

        #endregion

        #region Private Implementation Methods

        private void FindDependencies()
        {
            _mainUIController = UIController.Instance;
            if (_mainUIController == null)
            {
                Debug.LogWarning("UIController not found - will try to find it later");
            }
            Debug.Log("IoT Core Settings dependencies search completed");
            _settingsOrchestrator = FindObjectOfType<SettingsOrchestrator>();
        }

        private void InitializeIotCoreSettingsState()
        {
            _settingsState.IsInitialized = true;
            _settingsState.CurrentSection = "IotCoreSettings";
            _settingsState.CurrentActivePanel = IIotCoreSettingsOps.PanelType.None;
            _awsConfig.Region = "us-east-1";
            _awsConfig.ServiceStates = new Dictionary<string, bool>();
            _awsConfig.ServiceConfigs = new Dictionary<string, Dictionary<string, object>>();
            _awsConfig.TestResults = new Dictionary<string, IotCoreSettingsInfo.ConnectionTestResult>();
            Debug.Log("IoT Core Settings state initialized");
        }

        private void LoadAwsConfiguration()
        {
            Debug.Log("Loading IoT Core configuration...");
            var serviceController = ServiceController.Instance;
            if (serviceController != null)
            {
                _settingsState.HasValidCredentials = serviceController.IsCognitoAuthenticated;
                Debug.Log($"IoT Core configuration loaded - Valid credentials: {_settingsState.HasValidCredentials}");
            }
        }

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

        private void OnPanelTransitionCompleteHandler(IIotCoreSettingsOps.PanelType panelType)
        {
            OnPanelTransitionComplete?.Invoke(panelType);
            Debug.Log($"Panel transition complete: {panelType}");
        }

        private void OnThingsListed(bool success, string message, List<IoTInfo.ThingInfo> things)
        {
            Debug.Log($"Things listed: {message}");
            UpdateThingsList();
        }

        private void OnThingCreated(bool success, string message, string thingName)
        {
            Debug.Log($"Thing creation: {message}");
        }

        private void OnPolicyCreated(bool success, string message, string policyName)
        {
            Debug.Log($"Policy creation: {message}");
        }

        private void OnCertificateCreated(bool success, string message, IoTInfo.CertificateData certificateData)
        {
            Debug.Log($"Certificate creation: {message}");
        }

        private void OnCertificateAttached(bool success, string message, string thingName, string certificateId)
        {
            Debug.Log($"Certificate attachment: {message}");
        }

        private void OnPolicyAttached(bool success, string message, string policyName, string certificateId)
        {
            Debug.Log($"Policy attachment: {message}");
        }

        private async Task SaveConfigurationAsync()
        {
            if (_iotCoreManager != null)
            {
                var config = new Dictionary<string, object>
                {
                    ["ThingName"] = _thingNameField.value,
                    ["ThingType"] = _thingTypeField.value,
                    ["PolicyName"] = _policyNameField.value,
                    ["PolicyDocument"] = _policyDocumentField.value,
                    ["CertificateId"] = _certificateIdField.value,
                    ["ThingNameForCertificate"] = _thingNameForCertificateField.value,
                    ["PolicyNameForCertificate"] = _policyNameForCertificateField.value
                };
                _awsConfig.ServiceConfigs["IoTCore"] = config;
                _settingsState.HasUnsavedChanges = false;
                _uiManager.UpdateCredentialsStatus(true, "Configuration saved");
                Debug.Log("IoT Core configuration saved");
            }
        }

        #endregion

        #region Public Properties

        public IotCoreSettingsUIManager UIManager => _uiManager;
        public IotCoreSettingsEventManager EventManager => _eventManager;
        public IotCoreSettingsInfo.IotCoreSettingsState SettingsState => _settingsState;
        public IotCoreSettingsInfo.AwsConfiguration AwsConfig => _awsConfig;

        #endregion
    }
}