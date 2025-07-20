using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controller;
using _Scripts.Controllers.SettingsController;
using _Scripts.Controllers.UiManagement;
using _Scripts.Models.S3Management;

namespace _Scripts.Controllers.S3SettingsController
{
    public class S3SettingsOrchestrator : MonoBehaviour, IS3SettingsOps, IUIController
    {
        #region IUIController Implementation

        public bool RequiresAuthentication => true;
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _uiConfig?.Body?.style.display == DisplayStyle.Flex;
        public string ControllerName => "S3SettingsController";

        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        #region IS3SettingsOps Implementation

        public bool IsNavigationMenuOpen => _uiManager?.NavigationMenuOpen ?? false;
        public IS3SettingsOps.PanelType CurrentActivePanel => _uiManager?.CurrentActivePanel ?? IS3SettingsOps.PanelType.None;

        public event Action OnNavigationMenuOpened;
        public event Action OnNavigationMenuClosed;
        public event Action<IS3SettingsOps.PanelType> OnPanelTransitionComplete;
        public event Action OnReturnToDashboardRequested;

        #endregion

        #region Private Fields

        private S3SettingsInfo.UIConfiguration _uiConfig = new S3SettingsInfo.UIConfiguration();
        private S3SettingsInfo.AwsConfiguration _awsConfig = new S3SettingsInfo.AwsConfiguration();
        private S3SettingsInfo.S3SettingsState _settingsState = new S3SettingsInfo.S3SettingsState();
        
        private S3SettingsUIManager _uiManager;
        private S3SettingsEventManager _eventManager;
        
        private UIController _mainUIController;
        
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private UIDocument _uiDocument;
        private bool _isInitialized = false;

        // Referencias a elementos UI del UploadFilePanel
        private TextField _fileNameField;
        private DropdownField _contentTypeField;
        private Button _uploadFileButton;

        // Referencias a elementos UI del DownloadFilePanel
        private TextField _fileKeyField;
        private TextField _localPathField;
        private Button _downloadFileButton;

        // Referencias a elementos UI del FileManagementPanel
        private TextField _listPrefixField;
        private Button _listFilesButton;
        private TextField _deleteFileKeyField;
        private Button _deleteFileButton;

        // Referencias a elementos UI del FileStatusPanel
        private VisualElement _filesList;
        private Button _refreshFilesButton;

        // Referencias a elementos UI del TestPanel
        private Button _testConnectionButton;
        private Button _enableDisableServiceButton;
        private Label _testResultLabel;

        private S3Manager _s3Manager;
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
            
            Debug.Log("[S3SettingsOrchestrator] Started - UI hidden until navigation");
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
                    Debug.Log("S3SettingsOrchestrator already initialized");
                    return true;
                }

                Debug.Log("Initializing S3SettingsOrchestrator...");

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
                
                _uiManager = new S3SettingsUIManager(_uiConfig);
                _eventManager = new S3SettingsEventManager(_uiManager, OnReturnToDashboardHandler, OnPanelTransitionCompleteHandler, this);
                _eventManager.RegisterEvents(_uiDocument);

                _uiManager.InitializePanelSystem();
                
                FindDependencies();
                
                InitializeS3SettingsState();

                _s3Manager = ServiceController.Instance?.S3Manager;
                if (_s3Manager == null)
                {
                    Debug.LogError("S3Manager not found!");
                    return false;
                }

                SubscribeToS3ManagerEvents();

                UpdateUIStates();

                _isInitialized = true;
                Debug.Log("S3SettingsOrchestrator initialized successfully");
                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"S3SettingsOrchestrator initialization error: {ex.Message}");
                OnControllerError?.Invoke(this, $"Initialization failed: {ex.Message}");
                return false;
            }
        }

        private void SubscribeToS3ManagerEvents()
        {
            if (_s3Manager != null)
            {
                _s3Manager.OnUploadComplete += OnUploadCompleted;
                _s3Manager.OnDownloadComplete += OnDownloadCompleted;
                _s3Manager.OnFileListComplete += OnFileListCompleted;
                _s3Manager.OnFileDeleteComplete += OnFileDeleteCompleted;
                _s3Manager.OnInitializationCompleted += OnS3InitializationCompleted;
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

            // UploadFilePanel
            _fileNameField = root.Q<TextField>("FileNameField");
            _contentTypeField = root.Q<DropdownField>("ContentTypeField");
            _uploadFileButton = root.Q<Button>("UploadFileButton");

            // DownloadFilePanel
            _fileKeyField = root.Q<TextField>("FileKeyField");
            _localPathField = root.Q<TextField>("LocalPathField");
            _downloadFileButton = root.Q<Button>("DownloadFileButton");

            // FileManagementPanel
            _listPrefixField = root.Q<TextField>("ListPrefixField");
            _listFilesButton = root.Q<Button>("ListFilesButton");
            _deleteFileKeyField = root.Q<TextField>("DeleteFileKeyField");
            _deleteFileButton = root.Q<Button>("DeleteFileButton");

            // FileStatusPanel
            _filesList = root.Q<VisualElement>("FilesList");
            _refreshFilesButton = root.Q<Button>("RefreshFilesButton");

            // TestPanel
            _testConnectionButton = root.Q<Button>("TestConnectionButton");
            _enableDisableServiceButton = root.Q<Button>("EnableDisableServiceButton");
            _testResultLabel = root.Q<Label>("TestResult");

            InitializePanelConfiguration(root);
            Debug.Log("S3 Settings UI components obtained successfully");
        }

        private void InitializePanelConfiguration(VisualElement root)
        {
            var panelsContainer = _subpanelsAndSmokeMaskContainer;
            
            _uiConfig.Panels[IS3SettingsOps.PanelType.NavigationMenu] = new S3SettingsInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer?.Q<VisualElement>("NavigationMenuPanel"),
                ShowClass = "NavigationMenuPanelInMainScreen",
                HideClass = "NavigationMenuPanelOutMainScreen",
                RequiresScrim = true,
                AnimationDuration = 0.3f
            };

            _uiConfig.Panels[IS3SettingsOps.PanelType.AwsCredentials] = new S3SettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "AwsCredentialsPanelVisible",
                HideClass = "AwsCredentialsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IS3SettingsOps.PanelType.ServiceConfig] = new S3SettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "ServiceConfigPanelVisible",
                HideClass = "ServiceConfigPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IS3SettingsOps.PanelType.TestResults] = new S3SettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "TestResultsPanelVisible",
                HideClass = "TestResultsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IS3SettingsOps.PanelType.SecuritySettings] = new S3SettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "SecuritySettingsPanelVisible",
                HideClass = "SecuritySettingsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IS3SettingsOps.PanelType.RegionSettings] = new S3SettingsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "RegionSettingsPanelVisible",
                HideClass = "RegionSettingsPanelHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IS3SettingsOps.PanelType.Help] = new S3SettingsInfo.UIConfiguration.PanelData
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
            if (_s3Manager != null)
            {
                UpdateFilesList();
            }
        }

        private async void UpdateFilesList()
        {
            if (_s3Manager != null)
            {
                var files = await _s3Manager.ListFilesAsync();
                _filesList.Clear();
                foreach (var file in files)
                {
                    var label = new Label($"{file.fileName} ({file.fileType}, {file.formattedSize})");
                    label.AddToClassList("recent-activity-item");
                    _filesList.Add(label);
                }
            }
        }

        public void Show()
        {
            if (!ServiceController.Instance.IsCognitoAuthenticated)
            {
                Debug.LogError("Cannot show S3 Settings - user not authenticated");
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
                Debug.Log("[S3SettingsOrchestrator] S3 Settings UI shown");
            }
        }

        public void Hide()
        {
            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.None;
                _uiManager?.CloseCurrentPanel();
                OnControllerHidden?.Invoke(this);
                Debug.Log("[S3SettingsOrchestrator] S3 Settings UI hidden");
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
                if (_s3Manager != null)
                {
                    _s3Manager.OnUploadComplete -= OnUploadCompleted;
                    _s3Manager.OnDownloadComplete -= OnDownloadCompleted;
                    _s3Manager.OnFileListComplete -= OnFileListCompleted;
                    _s3Manager.OnFileDeleteComplete -= OnFileDeleteCompleted;
                    _s3Manager.OnInitializationCompleted -= OnS3InitializationCompleted;
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
                _fileNameField = null;
                _contentTypeField = null;
                _uploadFileButton = null;
                _fileKeyField = null;
                _localPathField = null;
                _downloadFileButton = null;
                _listPrefixField = null;
                _listFilesButton = null;
                _deleteFileKeyField = null;
                _deleteFileButton = null;
                _filesList = null;
                _refreshFilesButton = null;
                _testConnectionButton = null;
                _enableDisableServiceButton = null;
                _testResultLabel = null;
                _s3Manager = null;

                _isInitialized = false;
                Debug.Log("[S3SettingsOrchestrator] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[S3SettingsOrchestrator] Cleanup error: {ex.Message}");
            }
        }

        #region IS3SettingsOps Implementation

        public void NavigateToPanel(IS3SettingsOps.PanelType panelType)
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

        public void SwitchPanel(IS3SettingsOps.PanelType fromPanel, IS3SettingsOps.PanelType toPanel)
        {
            _uiManager?.SwitchPanel(fromPanel, toPanel);
        }

        public void OpenAwsConfiguration()
        {
            Debug.Log("Opening AWS Configuration panel");
            // Futuro: Mostrar un panel de configuración de credenciales o región
        }

        public async void SaveConfiguration()
        {
            await SaveConfigurationAsync();
        }

        public void ResetConfiguration()
        {
            if (_s3Manager != null)
            {
                _fileNameField.value = "Enter file name";
                _contentTypeField.value = "application/octet-stream";
                _fileKeyField.value = "Enter file key";
                _localPathField.value = "Enter local path";
                _listPrefixField.value = "Enter prefix";
                _deleteFileKeyField.value = "Enter file key";
                _settingsState.HasUnsavedChanges = false;
                UpdateCredentialsStatus(true, "Configuration reset");
                Debug.Log("S3 Configuration reset to default");
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

        public async Task UploadFile()
        {
            if (_s3Manager != null)
            {
                var fileName = _fileNameField.value;
                var contentType = _contentTypeField.value;
                if (string.IsNullOrWhiteSpace(fileName) || fileName == "Enter file name")
                {
                    Debug.LogError("File name cannot be empty");
                    ShowErrorMessage("File name cannot be empty");
                    return;
                }
                // Simulación: Subir un archivo de texto de prueba
                string testContent = "Test file uploaded from Unity";
                var success = await _s3Manager.UploadTextFileAsync(fileName, testContent);
                if (success)
                {
                    UpdateFilesList();
                }
            }
        }

        public async Task DownloadFile()
        {
            if (_s3Manager != null)
            {
                var fileKey = _fileKeyField.value;
                var localPath = _localPathField.value;
                if (string.IsNullOrWhiteSpace(fileKey) || fileKey == "Enter file key" ||
                    string.IsNullOrWhiteSpace(localPath) || localPath == "Enter local path")
                {
                    Debug.LogError("File key and local path cannot be empty");
                    ShowErrorMessage("File key and local path cannot be empty");
                    return;
                }
                var success = await _s3Manager.DownloadFileToPathAsync(fileKey, localPath);
                if (success)
                {
                    Debug.Log($"File downloaded to: {localPath}");
                }
            }
        }

        public async Task ListFiles()
        {
            if (_s3Manager != null)
            {
                var prefix = _listPrefixField.value;
                if (prefix == "Enter prefix")
                {
                    prefix = null;
                }
                var files = await _s3Manager.ListFilesAsync(prefix);
                UpdateFilesList();
            }
        }

        public async Task DeleteFile()
        {
            if (_s3Manager != null)
            {
                var fileKey = _deleteFileKeyField.value;
                if (string.IsNullOrWhiteSpace(fileKey) || fileKey == "Enter file key")
                {
                    Debug.LogError("File key cannot be empty");
                    ShowErrorMessage("File key cannot be empty");
                    return;
                }
                var success = await _s3Manager.DeleteFileAsync(fileKey);
                if (success)
                {
                    UpdateFilesList();
                }
            }
        }

        public async Task TestConnectionAsync()
        {
            if (_s3Manager != null)
            {
                _settingsState.IsTestingConnection = true;
                _testResultLabel.text = "Result: Testing...";
                var files = await _s3Manager.ListFilesAsync(); // Prueba de conexión listando archivos
                _testResultLabel.text = $"Result: {(files != null && files.Count >= 0 ? "Success" : "Failed")}";
                _testResultLabel.RemoveFromClassList("status-success");
                _testResultLabel.RemoveFromClassList("status-error");
                _testResultLabel.AddToClassList(files != null && files.Count >= 0 ? "status-success" : "status-error");
                _settingsState.IsTestingConnection = false;

                var testResult = new S3SettingsInfo.ConnectionTestResult
                {
                    ServiceName = "S3",
                    IsSuccessful = files != null && files.Count >= 0,
                    Message = files != null && files.Count >= 0 ? "Connection test successful" : "Connection test failed",
                    TestTime = DateTime.Now,
                    ResponseTime = TimeSpan.FromMilliseconds(150)
                };
                _awsConfig.TestResults["S3"] = testResult;
                _uiManager.UpdateConnectionTestResults(_awsConfig.TestResults);
            }
        }

        public async Task ToggleService()
        {
            Debug.Log("Toggling S3 service state");
            _awsConfig.ServiceStates["S3"] = !_awsConfig.ServiceStates.GetValueOrDefault("S3", false);
            _uiManager.UpdateServiceStates(_awsConfig.ServiceStates);
            if (_awsConfig.ServiceStates["S3"] && _s3Manager != null)
            {
                await _s3Manager.InitializeAsync(ServiceController.Instance.CognitoManager.CurrentAWSCredentials, 
                                                ServiceController.Instance.CognitoManager.GetRegionEndpoint());
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
                ["sourceController"] = "S3Settings"
            };
            _uiManager?.HideNavigationMenu();
            Hide();
            await Task.Run(() => _mainUIController?.ShowUI("DeviceSelection", parameters));
        }

        public async Task HandleTrainingClick()
        {
            var parameters = new Dictionary<string, object> {
                ["context"] = "Training",
                ["sourceController"] = "S3Settings"
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
            Debug.Log("S3 Settings HandleLogoutClick() called");
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

        #endregion

        #region Private Implementation Methods

        private void FindDependencies()
        {
            _mainUIController = UIController.Instance;
            if (_mainUIController == null)
            {
                Debug.LogWarning("UIController not found - will try to find it later");
            }
            Debug.Log("S3 Settings dependencies search completed");
        }

        private void InitializeS3SettingsState()
        {
            _settingsState.IsInitialized = true;
            _settingsState.CurrentSection = "S3Settings";
            _settingsState.CurrentActivePanel = IS3SettingsOps.PanelType.None;
            _awsConfig.Region = "us-east-1";
            _awsConfig.ServiceStates = new Dictionary<string, bool>();
            _awsConfig.ServiceConfigs = new Dictionary<string, Dictionary<string, object>>();
            _awsConfig.TestResults = new Dictionary<string, S3SettingsInfo.ConnectionTestResult>();
            Debug.Log("S3 Settings state initialized");
        }

        private void LoadAwsConfiguration()
        {
            Debug.Log("Loading S3 configuration...");
            var serviceController = ServiceController.Instance;
            if (serviceController != null)
            {
                _settingsState.HasValidCredentials = serviceController.IsCognitoAuthenticated;
                Debug.Log($"S3 configuration loaded - Valid credentials: {_settingsState.HasValidCredentials}");
            }
        }

        private void UpdateCredentialsStatus(bool isValid, string message = null)
        {
            _settingsState.HasValidCredentials = isValid;
            _uiManager?.UpdateCredentialsStatus(isValid, message);
            _settingsState.TriggerCredentialsValidityChanged(isValid);
            Debug.Log($"S3 credentials status updated: {isValid} - {message}");
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

        private void OnPanelTransitionCompleteHandler(IS3SettingsOps.PanelType panelType)
        {
            OnPanelTransitionComplete?.Invoke(panelType);
            Debug.Log($"Panel transition complete: {panelType}");
        }

        private void OnS3InitializationCompleted(bool success, string message)
        {
            Debug.Log($"S3 initialization: {message}");
            UpdateCredentialsStatus(success, message);
        }

        private void OnUploadCompleted(bool success, string message, string fileKey)
        {
            Debug.Log($"Upload completed: {message}");
            ShowErrorMessage(success ? $"Uploaded {fileKey}" : message);
        }

        private void OnDownloadCompleted(bool success, string message, byte[] fileData)
        {
            Debug.Log($"Download completed: {message}");
            ShowErrorMessage(success ? "Download successful" : message);
        }

        private void OnFileListCompleted(bool success, string message, List<S3FileInfo> fileList)
        {
            Debug.Log($"File list completed: {message}");
            ShowErrorMessage(success ? $"Listed {fileList.Count} files" : message);
        }

        private void OnFileDeleteCompleted(bool success, string message, string fileKey)
        {
            Debug.Log($"File delete completed: {message}");
            ShowErrorMessage(success ? $"Deleted {fileKey}" : message);
        }

        private void ShowErrorMessage(string message)
        {
            var existingLabel = _filesList.Q<Label>("ErrorMessage");
            if (existingLabel != null)
            {
                _filesList.Remove(existingLabel);
            }
            var errorLabel = new Label(message);
            errorLabel.name = "ErrorMessage";
            errorLabel.AddToClassList("error-message");
            _filesList.Add(errorLabel);
        }

        private async Task SaveConfigurationAsync()
        {
            if (_s3Manager != null)
            {
                var config = new Dictionary<string, object>
                {
                    ["FileName"] = _fileNameField.value,
                    ["ContentType"] = _contentTypeField.value,
                    ["FileKey"] = _fileKeyField.value,
                    ["LocalPath"] = _localPathField.value,
                    ["ListPrefix"] = _listPrefixField.value,
                    ["DeleteFileKey"] = _deleteFileKeyField.value
                };
                _awsConfig.ServiceConfigs["S3"] = config;
                _settingsState.HasUnsavedChanges = false;
                UpdateCredentialsStatus(true, "Configuration saved");
                Debug.Log("S3 configuration saved");
            }
        }

        #endregion

        #region Public Properties

        public S3SettingsUIManager UIManager => _uiManager;
        public S3SettingsEventManager EventManager => _eventManager;
        public S3SettingsInfo.S3SettingsState SettingsState => _settingsState;
        public S3SettingsInfo.AwsConfiguration AwsConfig => _awsConfig;

        #endregion
    }
}