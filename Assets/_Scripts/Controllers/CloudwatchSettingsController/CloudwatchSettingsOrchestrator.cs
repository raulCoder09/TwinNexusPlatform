using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controller;
using _Scripts.Controllers.ServiceManagement;
using _Scripts.Controllers.SettingsController;
using _Scripts.Controllers.UiManagement;
using _Scripts.Models.CloudWatchManagement;

namespace _Scripts.Controllers.CloudwatchSettingsController
{
    public class CloudwatchSettingsOrchestrator : MonoBehaviour, ICloudwatchSettingsOps, IUIController
    {
        #region IUIController Implementation

        public bool RequiresAuthentication => true;
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _uiConfig?.Body?.style.display == DisplayStyle.Flex;
        public string ControllerName => "CloudwatchSettingsController";

        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        #region ICloudwatchSettingsOps Implementation

        public bool IsNavigationMenuOpen => _uiManager?.NavigationMenuOpen ?? false;
        public ICloudwatchSettingsOps.PanelType CurrentActivePanel => _uiManager?.CurrentActivePanel ?? ICloudwatchSettingsOps.PanelType.None;

        public event Action OnNavigationMenuOpened;
        public event Action OnNavigationMenuClosed;
        public event Action<ICloudwatchSettingsOps.PanelType> OnPanelTransitionComplete;
        public event Action OnReturnToDashboardRequested;

        #endregion

        #region Private Fields

        private CloudwatchSettingsInfo.UIConfiguration _uiConfig = new CloudwatchSettingsInfo.UIConfiguration();
        private CloudwatchSettingsInfo.AwsConfiguration _awsConfig = new CloudwatchSettingsInfo.AwsConfiguration();
        private CloudwatchSettingsInfo.CloudwatchSettingsState _settingsState = new CloudwatchSettingsInfo.CloudwatchSettingsState();
        
        private CloudwatchSettingsUIManager _uiManager;
        private CloudwatchSettingsEventManager _eventManager;
        
        private UIController _mainUIController;
        
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private UIDocument _uiDocument;
        private bool _isInitialized = false;

        // Referencias a elementos UI del MetricsConfigPanel
        private TextField _metricNameField;
        private TextField _metricValueField;
        private DropdownField _metricUnitField;
        private Button _publishMetricButton;

        // Referencias a elementos UI del AlarmsConfigPanel
        private TextField _alarmNameField;
        private TextField _alarmMetricNameField;
        private TextField _thresholdField;
        private DropdownField _comparisonTypeField;
        private Button _createAlarmButton;

        // Referencias a elementos UI del LogsConfigPanel
        private TextField _logGroupField;
        private TextField _logStreamField;
        private TextField _logMessageField;
        private Button _sendLogButton;

        // Referencias a elementos UI del MetricsStatusPanel
        private VisualElement _metricsList;
        private Button _refreshMetricsButton;

        // Referencias a elementos UI del TestPanel
        private Button _testConnectionButton;
        private Button _testAlarmButton;
        private Button _testLogButton;
        private Button _enableDisableServiceButton;
        private Label _testResultLabel;

        private CloudWatchManager _cloudWatchManager;
        private SettingsOrchestrator _settingsOrchestrator;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            Hide();
            if (_subpanelsAndSmokeMaskContainer != null)
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
            
            Debug.Log("[CloudwatchSettingsOrchestrator] Started - UI hidden until navigation");
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
                    Debug.Log("CloudwatchSettingsOrchestrator already initialized");
                    return true;
                }

                Debug.Log("Initializing CloudwatchSettingsOrchestrator...");

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
                
                _uiManager = new CloudwatchSettingsUIManager(_uiConfig);
                _eventManager = new CloudwatchSettingsEventManager(_uiManager, OnReturnToDashboardHandler, OnPanelTransitionCompleteHandler, this);
                _eventManager.RegisterEvents(_uiDocument);

                _uiManager.InitializePanelSystem();
                
                FindDependencies();
                
                InitializeCloudwatchSettingsState();

                _cloudWatchManager = ServiceController.Instance?.CloudWatchManager;
                if (_cloudWatchManager == null)
                {
                    Debug.LogError("CloudWatchManager not found!");
                    return false;
                }

                SubscribeToCloudWatchManagerEvents();

                UpdateUIStates();

                _isInitialized = true;
                Debug.Log("CloudwatchSettingsOrchestrator initialized successfully");
                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"CloudwatchSettingsOrchestrator initialization error: {ex.Message}");
                OnControllerError?.Invoke(this, $"Initialization failed: {ex.Message}");
                return false;
            }
        }

        private void SubscribeToCloudWatchManagerEvents()
        {
            if (_cloudWatchManager != null)
            {
                _cloudWatchManager.Metrics.OnMetricPublished += OnMetricPublished;
                _cloudWatchManager.Metrics.OnMetricsListed += OnMetricsListed;
                _cloudWatchManager.Alarms.OnAlarmCreated += OnAlarmCreated;
                _cloudWatchManager.Logs.OnLogSent += OnLogSent;
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

            // MetricsConfigPanel
            _metricNameField = root.Q<TextField>("MetricNameField");
            _metricValueField = root.Q<TextField>("MetricValueField");
            _metricUnitField = root.Q<DropdownField>("MetricUnitField");
            _publishMetricButton = root.Q<Button>("PublishMetricButton");

            // AlarmsConfigPanel
            _alarmNameField = root.Q<TextField>("AlarmNameField");
            _alarmMetricNameField = root.Q<TextField>("MetricNameField", "AlarmsConfigContent");
            _thresholdField = root.Q<TextField>("ThresholdField");
            _comparisonTypeField = root.Q<DropdownField>("ComparisonTypeField");
            _createAlarmButton = root.Q<Button>("CreateAlarmButton");

            // LogsConfigPanel
            _logGroupField = root.Q<TextField>("LogGroupField");
            _logStreamField = root.Q<TextField>("LogStreamField");
            _logMessageField = root.Q<TextField>("LogMessageField");
            _sendLogButton = root.Q<Button>("SendLogButton");

            // MetricsStatusPanel
            _metricsList = root.Q<VisualElement>("MetricsList");
            _refreshMetricsButton = root.Q<Button>("RefreshMetricsButton");

            // TestPanel
            _testConnectionButton = root.Q<Button>("TestConnectionButton");
            _testAlarmButton = root.Q<Button>("TestAlarmButton");
            _testLogButton = root.Q<Button>("TestLogButton");
            _enableDisableServiceButton = root.Q<Button>("EnableDisableServiceButton");
            _testResultLabel = root.Q<Label>("TestResult");

            InitializePanelConfiguration(root);
            Debug.Log("Cloudwatch Settings UI components obtained successfully");
        }

        private void InitializePanelConfiguration(VisualElement root)
        {
            var panelsContainer = _subpanelsAndSmokeMaskContainer;
            
            _uiConfig.Panels[ICloudwatchSettingsOps.PanelType.NavigationMenu] = new CloudwatchSettingsInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer?.Q<VisualElement>("NavigationMenuPanel"),
                ShowClass = "NavigationMenuPanelInMainScreen",
                HideClass = "NavigationMenuPanelOutMainScreen",
                RequiresScrim = true,
                AnimationDuration = 0.3f
            };

            _uiConfig.Panels[ICloudwatchSettingsOps.PanelType.AwsCredentials] = new CloudwatchSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "AwsCredentialsPanelVisible",
                HideClass = "AwsCredentialsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ICloudwatchSettingsOps.PanelType.ServiceConfig] = new CloudwatchSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "ServiceConfigPanelVisible",
                HideClass = "ServiceConfigPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ICloudwatchSettingsOps.PanelType.TestResults] = new CloudwatchSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "TestResultsPanelVisible",
                HideClass = "TestResultsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ICloudwatchSettingsOps.PanelType.SecuritySettings] = new CloudwatchSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "SecuritySettingsPanelVisible",
                HideClass = "SecuritySettingsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ICloudwatchSettingsOps.PanelType.RegionSettings] = new CloudwatchSettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "RegionSettingsPanelVisible",
                HideClass = "RegionSettingsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[ICloudwatchSettingsOps.PanelType.Help] = new CloudwatchSettingsInfo.UIConfiguration.PanelData
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
            if (_cloudWatchManager != null)
            {
                _logGroupField.value = _cloudWatchManager.Logs.DefaultLogGroup;
                _logStreamField.value = _cloudWatchManager.Logs.CurrentLogStreamName;
                UpdateMetricsList();
            }
        }

        private async void UpdateMetricsList()
        {
            if (_cloudWatchManager != null)
            {
                var metrics = await _cloudWatchManager.ListMetrics();
                _metricsList.Clear();
                foreach (var metric in metrics)
                {
                    var label = new Label($"{metric.Name} (Namespace: {metric.Namespace})");
                    label.AddToClassList("recent-activity-item");
                    _metricsList.Add(label);
                }
            }
        }

        public void Show()
        {
            if (!ServiceController.Instance.IsCognitoAuthenticated)
            {
                Debug.LogError("Cannot show Cloudwatch Settings - user not authenticated");
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
                Debug.Log("[CloudwatchSettingsOrchestrator] Cloudwatch Settings UI shown");
            }
        }

        public void Hide()
        {
            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.None;
                _uiManager?.CloseCurrentPanel();
                OnControllerHidden?.Invoke(this);
                Debug.Log("[CloudwatchSettingsOrchestrator] Cloudwatch Settings UI hidden");
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
                if (_cloudWatchManager != null)
                {
                    _cloudWatchManager.Metrics.OnMetricPublished -= OnMetricPublished;
                    _cloudWatchManager.Metrics.OnMetricsListed -= OnMetricsListed;
                    _cloudWatchManager.Alarms.OnAlarmCreated -= OnAlarmCreated;
                    _cloudWatchManager.Logs.OnLogSent -= OnLogSent;
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

                _metricNameField = null;
                _metricValueField = null;
                _metricUnitField = null;
                _publishMetricButton = null;
                _alarmNameField = null;
                _alarmMetricNameField = null;
                _thresholdField = null;
                _comparisonTypeField = null;
                _createAlarmButton = null;
                _logGroupField = null;
                _logStreamField = null;
                _logMessageField = null;
                _sendLogButton = null;
                _metricsList = null;
                _refreshMetricsButton = null;
                _testConnectionButton = null;
                _testAlarmButton = null;
                _testLogButton = null;
                _enableDisableServiceButton = null;
                _testResultLabel = null;
                _cloudWatchManager = null;

                _isInitialized = false;
                Debug.Log("[CloudwatchSettingsOrchestrator] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudwatchSettingsOrchestrator] Cleanup error: {ex.Message}");
            }
        }

        #region ICloudwatchSettingsOps Implementation

        public void NavigateToPanel(ICloudwatchSettingsOps.PanelType panelType)
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

        public void SwitchPanel(ICloudwatchSettingsOps.PanelType fromPanel, ICloudwatchSettingsOps.PanelType toPanel)
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
            if (_cloudWatchManager != null)
            {
                _metricNameField.value = "Enter metric name";
                _metricValueField.value = "0.0";
                _metricUnitField.value = "Count";
                _alarmNameField.value = "Enter alarm name";
                _alarmMetricNameField.value = "Enter metric name";
                _thresholdField.value = "30.0";
                _comparisonTypeField.value = "LessThanThreshold";
                _logMessageField.value = "Enter log message";
                _settingsState.HasUnsavedChanges = false;
                _uiManager.UpdateCredentialsStatus(true, "Configuration reset");
                Debug.Log("Cloudwatch Configuration reset to default");
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

        public async Task PublishMetric()
        {
            if (_cloudWatchManager != null)
            {
                var metricName = _metricNameField.value;
                if (string.IsNullOrWhiteSpace(metricName) || metricName == "Enter metric name")
                {
                    Debug.LogError("Metric name cannot be empty");
                    return;
                }
                if (double.TryParse(_metricValueField.value, out double value))
                {
                    var success = await _cloudWatchManager.PublishMetric(metricName, value);
                    if (success)
                    {
                        Debug.Log($"Metric {metricName} published successfully");
                        UpdateMetricsList();
                    }
                    else
                    {
                        Debug.LogError($"Failed to publish metric {metricName}");
                    }
                }
                else
                {
                    Debug.LogError("Invalid metric value");
                }
            }
        }

        public async Task CreateAlarm()
        {
            if (_cloudWatchManager != null)
            {
                var alarmName = _alarmNameField.value;
                var metricName = _alarmMetricNameField.value;
                if (string.IsNullOrWhiteSpace(alarmName) || alarmName == "Enter alarm name" || 
                    string.IsNullOrWhiteSpace(metricName) || metricName == "Enter metric name")
                {
                    Debug.LogError("Alarm name and metric name cannot be empty");
                    return;
                }
                if (double.TryParse(_thresholdField.value, out double threshold))
                {
                    var comparisonType = _comparisonTypeField.value;
                    var success = await _cloudWatchManager.CreateAlarm(alarmName, metricName, threshold, comparisonType);
                    if (success)
                    {
                        Debug.Log($"Alarm {alarmName} created successfully");
                    }
                    else
                    {
                        Debug.LogError($"Failed to create alarm {alarmName}");
                    }
                }
                else
                {
                    Debug.LogError("Invalid threshold value");
                }
            }
        }

        public async Task SendLog()
        {
            if (_cloudWatchManager != null)
            {
                var message = _logMessageField.value;
                if (string.IsNullOrWhiteSpace(message) || message == "Enter log message")
                {
                    Debug.LogError("Log message cannot be empty");
                    return;
                }
                var success = await _cloudWatchManager.SendLog(message);
                if (success)
                {
                    Debug.Log("Log sent successfully");
                }
                else
                {
                    Debug.LogError("Failed to send log");
                }
            }
        }

        public async Task RefreshMetrics()
        {
            UpdateMetricsList();
        }

        public async Task TestAlarm()
        {
            if (_cloudWatchManager != null)
            {
                var success = await _cloudWatchManager.Alarms.CreateTestAlarmAsync("TestMetric");
                if (success)
                {
                    Debug.Log("Test alarm created successfully");
                    _testResultLabel.text = "Result: Test alarm created";
                    _testResultLabel.RemoveFromClassList("status-error");
                    _testResultLabel.AddToClassList("status-success");
                }
                else
                {
                    Debug.LogError("Failed to create test alarm");
                    _testResultLabel.text = "Result: Test alarm failed";
                    _testResultLabel.RemoveFromClassList("status-success");
                    _testResultLabel.AddToClassList("status-error");
                }
            }
        }

        public async Task TestLog()
        {
            if (_cloudWatchManager != null)
            {
                var success = await _cloudWatchManager.Logs.SendTestLogAsync();
                if (success)
                {
                    Debug.Log("Test log sent successfully");
                    _testResultLabel.text = "Result: Test log sent";
                    _testResultLabel.RemoveFromClassList("status-error");
                    _testResultLabel.AddToClassList("status-success");
                }
                else
                {
                    Debug.LogError("Failed to send test log");
                    _testResultLabel.text = "Result: Test log failed";
                    _testResultLabel.RemoveFromClassList("status-success");
                    _testResultLabel.AddToClassList("status-error");
                }
            }
        }

        public async void ToggleService()
        {
            Debug.Log("Toggling CloudWatch service state");
            _awsConfig.ServiceStates["CloudWatch"] = !_awsConfig.ServiceStates.GetValueOrDefault("CloudWatch", false);
            _uiManager.UpdateServiceStates(_awsConfig.ServiceStates);
            if (_awsConfig.ServiceStates["CloudWatch"] && _cloudWatchManager != null)
            {
                await _cloudWatchManager.InitializeAsync(ServiceController.Instance.CognitoManager.CurrentAWSCredentials, ServiceController.Instance.CognitoManager.GetRegionEndpoint());
            }
        }

        #endregion

        #region Public Event Handlers

        public void HandleDashboardClick()
        {
            Debug.Log("Dashboard button clicked - returning to Dashboard");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Dashboard");
        }

        public void HandleReportsClick()
        {
            Debug.Log("Reports button clicked - opening Reports Center");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Reports");
        }

        public void HandleOperationsClick()
        {
            var parameters = new Dictionary<string, object> {
                ["context"] = "Operations", 
                ["sourceController"] = "CloudwatchSettings"
            };
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }

        public void HandleTrainingClick()
        {
            var parameters = new Dictionary<string, object> {
                ["context"] = "Training",
                ["sourceController"] = "CloudwatchSettings"
            };
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }

        public void HandleSupportClick()
        {
            Debug.Log("Support button clicked - opening support center");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Support");
        }

        public void HandleLogoutClick()
        {
            Debug.Log("Cloudwatch Settings HandleLogoutClick() called");
            _uiManager?.HideNavigationMenu();
            Hide();
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
        #endregion

        #region Private Implementation Methods

        private void FindDependencies()
        {
            _mainUIController = UIController.Instance;
            if (_mainUIController == null)
            {
                Debug.LogWarning("UIController not found - will try to find it later");
            }
            _settingsOrchestrator = FindObjectOfType<SettingsOrchestrator>();
        }

        private void InitializeCloudwatchSettingsState()
        {
            _settingsState.IsInitialized = true;
            _settingsState.CurrentSection = "CloudwatchSettings";
            _settingsState.CurrentActivePanel = ICloudwatchSettingsOps.PanelType.None;
            _awsConfig.Region = "us-east-1";
            _awsConfig.ServiceStates = new Dictionary<string, bool>();
            _awsConfig.ServiceConfigs = new Dictionary<string, Dictionary<string, object>>();
            _awsConfig.TestResults = new Dictionary<string, CloudwatchSettingsInfo.ConnectionTestResult>();
            Debug.Log("Cloudwatch Settings state initialized");
        }

        private void LoadAwsConfiguration()
        {
            Debug.Log("Loading Cloudwatch configuration...");
            var serviceController = ServiceController.Instance;
            if (serviceController != null)
            {
                _settingsState.HasValidCredentials = serviceController.IsCognitoAuthenticated;
                Debug.Log($"Cloudwatch configuration loaded - Valid credentials: {_settingsState.HasValidCredentials}");
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

        private void OnPanelTransitionCompleteHandler(ICloudwatchSettingsOps.PanelType panelType)
        {
            OnPanelTransitionComplete?.Invoke(panelType);
            Debug.Log($"Panel transition complete: {panelType}");
        }

        private void OnMetricPublished(bool success, string message, string metricName)
        {
            Debug.Log($"Metric publication: {message}");
            UpdateUIStates();
        }

        private void OnMetricsListed(bool success, string message, List<MetricInfo> metrics)
        {
            Debug.Log($"Metrics listed: {message}");
            UpdateMetricsList();
        }

        private void OnAlarmCreated(bool success, string message, string alarmName)
        {
            Debug.Log($"Alarm creation: {message}");
        }

        private void OnLogSent(bool success, string message, string logGroup)
        {
            Debug.Log($"Log sent: {message}");
        }

        private async Task SaveConfigurationAsync()
        {
            if (_cloudWatchManager != null)
            {
                var config = new Dictionary<string, object>
                {
                    ["MetricName"] = _metricNameField.value,
                    ["MetricValue"] = _metricValueField.value,
                    ["MetricUnit"] = _metricUnitField.value,
                    ["AlarmName"] = _alarmNameField.value,
                    ["AlarmMetricName"] = _alarmMetricNameField.value,
                    ["Threshold"] = _thresholdField.value,
                    ["ComparisonType"] = _comparisonTypeField.value
                };
                _awsConfig.ServiceConfigs["CloudWatch"] = config;
                _settingsState.HasUnsavedChanges = false;
                _uiManager.UpdateCredentialsStatus(true, "Configuration saved");
                Debug.Log("Cloudwatch configuration saved");
            }
        }

        private async Task TestConnectionAsync()
        {
            if (_cloudWatchManager != null)
            {
                _settingsState.IsTestingConnection = true;
                _testResultLabel.text = "Result: Testing...";
                var success = await _cloudWatchManager.Metrics.PublishTestMetricAsync();
                _testResultLabel.text = $"Result: {(success ? "Success" : "Failed")}";
                _testResultLabel.RemoveFromClassList("status-success");
                _testResultLabel.RemoveFromClassList("status-error");
                _testResultLabel.AddToClassList(success ? "status-success" : "status-error");
                _settingsState.IsTestingConnection = false;

                var testResult = new CloudwatchSettingsInfo.ConnectionTestResult
                {
                    ServiceName = "CloudWatch",
                    IsSuccessful = success,
                    Message = success ? "Connection test successful" : "Connection test failed",
                    TestTime = DateTime.Now,
                    ResponseTime = TimeSpan.FromMilliseconds(150)
                };
                _awsConfig.TestResults["CloudWatch"] = testResult;
                _uiManager.UpdateConnectionTestResults(_awsConfig.TestResults);
            }
        }

        #endregion

        #region Public Properties

        public CloudwatchSettingsUIManager UIManager => _uiManager;
        public CloudwatchSettingsEventManager EventManager => _eventManager;
        public CloudwatchSettingsInfo.CloudwatchSettingsState SettingsState => _settingsState;
        public CloudwatchSettingsInfo.AwsConfiguration AwsConfig => _awsConfig;

        #endregion
    }
}