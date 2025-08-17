using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controller;
using _Scripts.Controllers.ServiceManagement;
using _Scripts.Controllers.SettingsController;
using _Scripts.Controllers.UiManagement;
using _Scripts.Models.LambdaManagement;
using Newtonsoft.Json;

namespace _Scripts.Controllers.LambdaSettingsController
{
    public class LambdaSettingsOrchestrator : MonoBehaviour, ILambdaSettingsOps, IUIController
    {
        #region IUIController Implementation

        public bool RequiresAuthentication => true;
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _uiConfig?.Body?.style.display == DisplayStyle.Flex;
        public string ControllerName => "LambdaSettingsController";

        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        #region ILambdaSettingsOps Implementation

        public bool IsNavigationMenuOpen => _uiManager?.NavigationMenuOpen ?? false;
        public ILambdaSettingsOps.PanelType CurrentActivePanel => _uiManager?.CurrentActivePanel ?? ILambdaSettingsOps.PanelType.None;

        public event Action OnNavigationMenuOpened;
        public event Action OnNavigationMenuClosed;
        public event Action<ILambdaSettingsOps.PanelType> OnPanelTransitionComplete;
        public event Action OnReturnToDashboardRequested;

        #endregion

        #region Private Fields

        private LambdaSettingsInfo.UIConfiguration _uiConfig = new LambdaSettingsInfo.UIConfiguration();
        private LambdaSettingsInfo.AwsConfiguration _awsConfig = new LambdaSettingsInfo.AwsConfiguration();
        private LambdaSettingsInfo.LambdaSettingsState _settingsState = new LambdaSettingsInfo.LambdaSettingsState();
        
        private LambdaSettingsUIManager _uiManager;
        private LambdaSettingsEventManager _eventManager;
        
        private UIController _mainUIController;
        
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private UIDocument _uiDocument;
        private bool _isInitialized = false;

        private DropdownField _functionNameField;
        private TextField _payloadField;
        private Button _executeFunctionButton;

        private DropdownField _functionNameContextField;
        private TextField _additionalPayloadField;
        private Button _executeWithContextButton;

        private VisualElement _functionsList;
        private Button _refreshFunctionsButton;

        private Button _listFunctionsButton;

        private Button _testConnectionButton;
        private Button _enableDisableServiceButton;
        private Label _testResultLabel;

        private LambdaManager _lambdaManager;
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
            
            Debug.Log("[LambdaSettingsOrchestrator] Started - UI hidden until navigation");
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
                    Debug.Log("LambdaSettingsOrchestrator already initialized");
                    return true;
                }

                Debug.Log("Initializing LambdaSettingsOrchestrator...");

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
                
                _uiManager = new LambdaSettingsUIManager(_uiConfig);
                _eventManager = new LambdaSettingsEventManager(_uiManager, OnReturnToDashboardHandler, OnPanelTransitionCompleteHandler, this);
                _eventManager.RegisterEvents(_uiDocument);

                _uiManager.InitializePanelSystem();
                
                FindDependencies();
                
                InitializeLambdaSettingsState();

                _lambdaManager = ServiceController.Instance?.LambdaManager;
                if (_lambdaManager == null)
                {
                    Debug.LogError("LambdaManager not found!");
                    return false;
                }

                SubscribeToLambdaManagerEvents();

                UpdateUIStates();

                _isInitialized = true;
                Debug.Log("LambdaSettingsOrchestrator initialized successfully");
                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"LambdaSettingsOrchestrator initialization error: {ex.Message}");
                OnControllerError?.Invoke(this, $"Initialization failed: {ex.Message}");
                return false;
            }
        }

        private void SubscribeToLambdaManagerEvents()
        {
            if (_lambdaManager != null)
            {
                _lambdaManager.OnExecutionComplete += OnExecutionCompleted;
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

            _functionNameField = root.Q<DropdownField>("FunctionNameField");
            _payloadField = root.Q<TextField>("PayloadField");
            _executeFunctionButton = root.Q<Button>("ExecuteFunctionButton");

            _functionNameContextField = root.Q<DropdownField>("FunctionNameContextField");
            _additionalPayloadField = root.Q<TextField>("AdditionalPayloadField");
            _executeWithContextButton = root.Q<Button>("ExecuteWithContextButton");

            _functionsList = root.Q<VisualElement>("FunctionsList");
            _refreshFunctionsButton = root.Q<Button>("RefreshFunctionsButton");

            _listFunctionsButton = root.Q<Button>("ListFunctionsButton");

            _testConnectionButton = root.Q<Button>("TestConnectionButton");
            _enableDisableServiceButton = root.Q<Button>("EnableDisableServiceButton");
            _testResultLabel = root.Q<Label>("TestResult");

            InitializePanelConfiguration(root);
            Debug.Log("Lambda Settings UI components obtained successfully");
        }

        private void InitializePanelConfiguration(VisualElement root)
        {
            var panelsContainer = _subpanelsAndSmokeMaskContainer;
            
            _uiConfig.Panels[ILambdaSettingsOps.PanelType.NavigationMenu] = new LambdaSettingsInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer?.Q<VisualElement>("NavigationMenuPanel"),
                ShowClass = "NavigationMenuPanelInMainScreen",
                HideClass = "NavigationMenuPanelOutMainScreen",
                RequiresScrim = true,
                AnimationDuration = 0.3f
            };

            _uiConfig.Panels[ILambdaSettingsOps.PanelType.AwsCredentials] = new LambdaSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "AwsCredentialsPanelVisible",
                HideClass = "AwsCredentialsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ILambdaSettingsOps.PanelType.ServiceConfig] = new LambdaSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "ServiceConfigPanelVisible",
                HideClass = "ServiceConfigPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ILambdaSettingsOps.PanelType.TestResults] = new LambdaSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "TestResultsPanelVisible",
                HideClass = "TestResultsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ILambdaSettingsOps.PanelType.SecuritySettings] = new LambdaSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "SecuritySettingsPanelVisible",
                HideClass = "SecuritySettingsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ILambdaSettingsOps.PanelType.RegionSettings] = new LambdaSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "RegionSettingsPanelVisible",
                HideClass = "RegionSettingsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ILambdaSettingsOps.PanelType.Help] = new LambdaSettingsInfo.UIConfiguration.PanelData
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

        private async void UpdateUIStates()
        {
            if (_lambdaManager != null)
            {
                await UpdateFunctionsList();
            }
        }

        private async Task UpdateFunctionsList()
        {
            if (_lambdaManager != null)
            {
                var functions = await _lambdaManager.GetAvailableFunctionsAsync();
                _functionsList.Clear();
                foreach (var function in functions)
                {
                    var label = new Label(function);
                    label.AddToClassList("recent-activity-item");
                    _functionsList.Add(label);
                }
                UpdateFunctionDropdowns(functions);
            }
        }

        private void UpdateFunctionDropdowns(List<string> functions)
        {
            _functionNameField.choices = functions;
            _functionNameContextField.choices = functions;
            if (functions.Count > 0)
            {
                _functionNameField.value = functions[0];
                _functionNameContextField.value = functions[0];
            }
        }

        public void Show()
        {
            if (!ServiceController.Instance.IsCognitoAuthenticated)
            {
                Debug.LogError("Cannot show Lambda Settings - user not authenticated");
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
                Debug.Log("[LambdaSettingsOrchestrator] Lambda Settings UI shown");
            }
        }

        public void Hide()
        {
            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.None;
                _uiManager?.CloseCurrentPanel();
                OnControllerHidden?.Invoke(this);
                Debug.Log("[LambdaSettingsOrchestrator] Lambda Settings UI hidden");
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
                if (_lambdaManager != null)
                {
                    _lambdaManager.OnExecutionComplete -= OnExecutionCompleted;
                }

                _eventManager?.Cleanup();
                _uiManager = null;
                _eventManager = null;

                _settingsOrchestrator = null;
                _mainUIController = null;
                _uiConfig = null;
                _awsConfig = null;
                _settingsState = null;
                _uiDocument = null;
                _subpanelsAndSmokeMaskContainer = null;

                _functionNameField = null;
                _payloadField = null;
                _executeFunctionButton = null;
                _functionNameContextField = null;
                _additionalPayloadField = null;
                _executeWithContextButton = null;
                _functionsList = null;
                _refreshFunctionsButton = null;
                _listFunctionsButton = null;
                _testConnectionButton = null;
                _enableDisableServiceButton = null;
                _testResultLabel = null;
                _lambdaManager = null;

                _isInitialized = false;
                Debug.Log("[LambdaSettingsOrchestrator] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LambdaSettingsOrchestrator] Cleanup error: {ex.Message}");
            }
        }

        #region ILambdaSettingsOps Implementation

        public void NavigateToPanel(ILambdaSettingsOps.PanelType panelType)
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

        public void SwitchPanel(ILambdaSettingsOps.PanelType fromPanel, ILambdaSettingsOps.PanelType toPanel)
        {
            _uiManager?.SwitchPanel(fromPanel, toPanel);
        }

        public void OpenAwsConfiguration()
        {
            Debug.Log("Opening AWS Configuration panel");
            // Future: Show a panel for configuring credentials or region
        }

        public async void SaveConfiguration()
        {
            await SaveConfigurationAsync();
        }

        public void ResetConfiguration()
        {
            if (_lambdaManager != null)
            {
                _functionNameField.value = "test";
                _payloadField.value = "{}";
                _functionNameContextField.value = "test";
                _additionalPayloadField.value = "{}";
                _settingsState.HasUnsavedChanges = false;
                UpdateCredentialsStatus(true, "Configuration reset");
                Debug.Log("Lambda Configuration reset to default");
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

        public async Task ExecuteFunction()
        {
            if (_lambdaManager != null)
            {
                var functionName = _functionNameField.value;
                var payloadText = _payloadField.value;
                if (string.IsNullOrWhiteSpace(functionName))
                {
                    Debug.LogError("Function name cannot be empty");
                    ShowErrorMessage("Function name cannot be empty");
                    return;
                }
                try
                {
                    var payload = JsonConvert.DeserializeObject(payloadText);
                    var success = await _lambdaManager.ExecuteFunctionAsync(functionName, payload);
                    if (success)
                    {
                        await UpdateFunctionsList();
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogError($"Invalid JSON payload: {ex.Message}");
                    ShowErrorMessage($"Invalid JSON payload: {ex.Message}");
                }
            }
        }

        public async Task ExecuteFunctionWithContext()
        {
            if (_lambdaManager != null)
            {
                var functionName = _functionNameContextField.value;
                var additionalPayloadText = _additionalPayloadField.value;
                if (string.IsNullOrWhiteSpace(functionName))
                {
                    Debug.LogError("Function name cannot be empty");
                    ShowErrorMessage("Function name cannot be empty");
                    return;
                }
                try
                {
                    var additionalPayload = JsonConvert.DeserializeObject(additionalPayloadText);
                    var success = await _lambdaManager.ExecuteFunctionWithContextAsync(functionName, additionalPayload);
                    if (success)
                    {
                        await UpdateFunctionsList();
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogError($"Invalid JSON payload: {ex.Message}");
                    ShowErrorMessage($"Invalid JSON payload: {ex.Message}");
                }
            }
        }

        public async Task ListFunctions()
        {
            if (_lambdaManager != null)
            {
                await UpdateFunctionsList();
            }
        }

        public async Task TestConnectionAsync()
        {
            if (_lambdaManager != null)
            {
                _settingsState.IsTestingConnection = true;
                _testResultLabel.text = "Result: Testing...";
                var success = await _lambdaManager.TestConnectivityAsync();
                _testResultLabel.text = $"Result: {(success ? "Success" : "Failed")}";
                _testResultLabel.RemoveFromClassList("status-success");
                _testResultLabel.RemoveFromClassList("status-error");
                _testResultLabel.AddToClassList(success ? "status-success" : "status-error");
                _settingsState.IsTestingConnection = false;

                var testResult = new LambdaSettingsInfo.ConnectionTestResult
                {
                    ServiceName = "Lambda",
                    IsSuccessful = success,
                    Message = success ? "Connection test successful" : "Connection test failed",
                    TestTime = DateTime.Now,
                    ResponseTime = TimeSpan.FromMilliseconds(150)
                };
                _awsConfig.TestResults["Lambda"] = testResult;
                _uiManager.UpdateConnectionTestResults(_awsConfig.TestResults);
            }
        }

        public async Task ToggleService()
        {
            Debug.Log("Toggling Lambda service state");
            _awsConfig.ServiceStates["Lambda"] = !_awsConfig.ServiceStates.GetValueOrDefault("Lambda", false);
            _uiManager.UpdateServiceStates(_awsConfig.ServiceStates);
            if (_awsConfig.ServiceStates["Lambda"] && _lambdaManager != null)
            {
                var success = await _lambdaManager.InitializeAsync(ServiceController.Instance.CognitoManager.CurrentAWSCredentials, 
                                                                ServiceController.Instance.CognitoManager.GetRegionEndpoint());
                UpdateCredentialsStatus(success, success ? "Lambda service initialized" : "Failed to initialize Lambda service");
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
                ["sourceController"] = "LambdaSettings"
            };
            _uiManager?.HideNavigationMenu();
            Hide();
            await Task.Run(() => _mainUIController?.ShowUI("DeviceSelection", parameters));
        }

        public async Task HandleTrainingClick()
        {
            var parameters = new Dictionary<string, object> {
                ["context"] = "Training",
                ["sourceController"] = "LambdaSettings"
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
            Debug.Log("Lambda Settings HandleLogoutClick() called");
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
                // Fallback: usar UIController
                _mainUIController?.ShowUI("Settings");
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
            Debug.Log("Lambda Settings dependencies search completed");
            _settingsOrchestrator = FindObjectOfType<SettingsOrchestrator>();
        }

        private void InitializeLambdaSettingsState()
        {
            _settingsState.IsInitialized = true;
            _settingsState.CurrentSection = "LambdaSettings";
            _settingsState.CurrentActivePanel = ILambdaSettingsOps.PanelType.None;
            _awsConfig.Region = "us-east-1";
            _awsConfig.ServiceStates = new Dictionary<string, bool>();
            _awsConfig.ServiceConfigs = new Dictionary<string, Dictionary<string, object>>();
            _awsConfig.TestResults = new Dictionary<string, LambdaSettingsInfo.ConnectionTestResult>();
            Debug.Log("Lambda Settings state initialized");
        }

        private void LoadAwsConfiguration()
        {
            Debug.Log("Loading Lambda configuration...");
            var serviceController = ServiceController.Instance;
            if (serviceController != null)
            {
                _settingsState.HasValidCredentials = serviceController.IsCognitoAuthenticated;
                Debug.Log($"Lambda configuration loaded - Valid credentials: {_settingsState.HasValidCredentials}");
            }
        }

        private void UpdateCredentialsStatus(bool isValid, string message = null)
        {
            _settingsState.HasValidCredentials = isValid;
            _uiManager?.UpdateCredentialsStatus(isValid, message);
            _settingsState.TriggerCredentialsValidityChanged(isValid);
            Debug.Log($"Lambda credentials status updated: {isValid} - {message}");
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

        private void OnPanelTransitionCompleteHandler(ILambdaSettingsOps.PanelType panelType)
        {
            OnPanelTransitionComplete?.Invoke(panelType);
            Debug.Log($"Panel transition complete: {panelType}");
        }

        private void OnExecutionCompleted(bool success, string message, object data)
        {
            Debug.Log($"Lambda execution completed: {message}");
            ShowErrorMessage(success ? $"Execution successful: {data?.ToString()}" : message);
        }

        private void ShowErrorMessage(string message)
        {
            var existingLabel = _functionsList.Q<Label>("ErrorMessage");
            if (existingLabel != null)
            {
                _functionsList.Remove(existingLabel);
            }
            var errorLabel = new Label(message);
            errorLabel.name = "ErrorMessage";
            errorLabel.AddToClassList("error-message");
            _functionsList.Add(errorLabel);
        }

        private async Task SaveConfigurationAsync()
        {
            if (_lambdaManager != null)
            {
                var config = new Dictionary<string, object>
                {
                    ["FunctionName"] = _functionNameField.value,
                    ["Payload"] = _payloadField.value,
                    ["FunctionNameContext"] = _functionNameContextField.value,
                    ["AdditionalPayload"] = _additionalPayloadField.value
                };
                _awsConfig.ServiceConfigs["Lambda"] = config;
                _settingsState.HasUnsavedChanges = false;
                UpdateCredentialsStatus(true, "Configuration saved");
                Debug.Log("Lambda configuration saved");
            }
        }

        #endregion

        #region Public Properties

        public LambdaSettingsUIManager UIManager => _uiManager;
        public LambdaSettingsEventManager EventManager => _eventManager;
        public LambdaSettingsInfo.LambdaSettingsState SettingsState => _settingsState;
        public LambdaSettingsInfo.AwsConfiguration AwsConfig => _awsConfig;

        #endregion
    }
}