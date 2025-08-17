using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controller;
using _Scripts.Controllers.ServiceManagement;
using _Scripts.Controllers.SettingsController;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.ReportsController
{
    public class ReportsOrchestrator : MonoBehaviour, IReportsOps, IUIController
    {
        #region IUIController Implementation
        public bool RequiresAuthentication => true;
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _uiConfig?.Body?.style.display == DisplayStyle.Flex;
        public string ControllerName => "ReportsController";

        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;
        #endregion

        #region IReportsOps Implementation
        public bool IsNavigationMenuOpen => _uiManager?.NavigationMenuOpen ?? false;
        public IReportsOps.PanelType CurrentActivePanel => _uiManager?.CurrentActivePanel ?? IReportsOps.PanelType.None;

        public event Action OnNavigationMenuOpened;
        public event Action OnNavigationMenuClosed;
        public event Action<IReportsOps.PanelType> OnPanelTransitionComplete;
        public event Action OnLogoutRequested;
        #endregion

        private ReportsInfo.UIConfiguration _uiConfig = new ReportsInfo.UIConfiguration();
        private ReportsInfo.UserData _userData = new ReportsInfo.UserData();
        private ReportsInfo.ReportsState _reportsState = new ReportsInfo.ReportsState();
        private ReportsUIManager _uiManager;
        private ReportsEventManager _eventManager;
        private UIController _mainUIController;
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private UIDocument _uiDocument;
        private bool _isInitialized = false;

        private Label _reportStatusLabel;
        private Label _totalReportsLabel;
        private Label _reportsTodayLabel;
        private Label _scheduledReportsLabel;
        private Label _storageUsedLabel;
        private ProgressBar _reportProgressBar;
        private Label _reportStatusMessage;

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            HideUi();
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            Debug.Log("[ReportsOrchestrator] Started - UI hidden until authentication");
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        public bool Initialize()
        {
            try
            {
                if (_isInitialized)
                {
                    Debug.Log("ReportsOrchestrator already initialized");
                    return true;
                }

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
                _uiManager = new ReportsUIManager(_uiConfig);
                _eventManager = new ReportsEventManager(_uiManager, OnLogoutRequestedHandler, OnPanelTransitionCompleteHandler, this);
                _eventManager.RegisterEvents(_uiDocument);
                _uiManager.InitializePanelSystem();
                FindDependencies();
                InitializeReportsState();

                _isInitialized = true;
                Debug.Log("[ReportsOrchestrator] Initialized successfully");
                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ReportsOrchestrator initialization error: {ex.Message}");
                OnControllerError?.Invoke(this, $"Initialization failed: {ex.Message}");
                return false;
            }
        }

        public void Show()
        {
            if (!ServiceController.Instance.IsCognitoAuthenticated)
            {
                Debug.LogError("Cannot show Reports - user not authenticated");
                OnControllerError?.Invoke(this, "Authentication required");
                _mainUIController?.ShowUI("Welcome");
                return;
            }

            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.Flex;
                UpdateUserData();
                OnControllerShown?.Invoke(this);
                Debug.Log("[ReportsOrchestrator] Reports UI shown");
            }
        }

        public void Hide()
        {
            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.None;
                _uiManager?.CloseCurrentPanel();
                OnControllerHidden?.Invoke(this);
                Debug.Log("[ReportsOrchestrator] Reports UI hidden");
            }
        }

        public void Cleanup()
        {
            try
            {
                _eventManager?.Cleanup();
                _uiManager = null;
                _eventManager = null;
                _mainUIController = null;
                _uiConfig = null;
                _userData = null;
                _reportsState = null;
                _uiDocument = null;
                _subpanelsAndSmokeMaskContainer = null;
                _isInitialized = false;
                Debug.Log("[ReportsOrchestrator] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReportsOrchestrator] Cleanup error: {ex.Message}");
            }
        }

        public void NavigateToPanel(IReportsOps.PanelType panelType)
        {
            if (!_isInitialized) Initialize();
            _uiManager?.ShowPanel(panelType);
        }

        public void CloseCurrentPanel()
        {
            _uiManager?.CloseCurrentPanel();
        }

        public void SwitchPanel(IReportsOps.PanelType fromPanel, IReportsOps.PanelType toPanel)
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

        public void GenerateSystemReport()
        {
            Debug.Log("[ReportsOrchestrator] Generating System Report...");
            UpdateReportStatus(ReportsInfo.ReportStatus.Generating);
        }

        public void GenerateActivityReport()
        {
            Debug.Log("[ReportsOrchestrator] Generating Activity Report...");
            UpdateReportStatus(ReportsInfo.ReportStatus.Generating);
        }

        public void GenerateIoTReport()
        {
            Debug.Log("[ReportsOrchestrator] Generating IoT Report...");
            UpdateReportStatus(ReportsInfo.ReportStatus.Generating);
        }

        public void CreateCustomReport()
        {
            Debug.Log("[ReportsOrchestrator] Creating Custom Report...");
            NavigateToPanel(IReportsOps.PanelType.CustomReportEditor);
        }

        public void ExportLastReport()
        {
            Debug.Log("[ReportsOrchestrator] Exporting Last Report...");
            UpdateReportStatus(ReportsInfo.ReportStatus.Exporting);
        }

        public void ScheduleReport()
        {
            Debug.Log("[ReportsOrchestrator] Scheduling Report...");
            NavigateToPanel(IReportsOps.PanelType.ScheduleReport);
        }

        public void ViewReportHistory()
        {
            Debug.Log("[ReportsOrchestrator] Viewing Report History...");
            NavigateToPanel(IReportsOps.PanelType.ReportHistory);
        }

        public void HandleDashboardClick()
        {
            Debug.Log("[ReportsOrchestrator] Dashboard button clicked - navigating to Dashboard");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Dashboard");
        }

        public void HandleOperationsClick()
        {
            Debug.Log("[ReportsOrchestrator] Operations button clicked - navigating to DeviceSelection");
            var parameters = new Dictionary<string, object> { ["context"] = "Operations", ["sourceController"] = "Reports" };
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }

        public void HandleTrainingClick()
        {
            Debug.Log("[ReportsOrchestrator] Training button clicked - navigating to DeviceSelection");
            var parameters = new Dictionary<string, object> { ["context"] = "Training", ["sourceController"] = "Reports" };
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }
        
        public void HandleReportsClick()
        {
            Debug.Log("NombreBoton button clicked - opening NombreBoton UI");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Reports");
        }

        public void HandleSupportClick()
        {
            Debug.Log("[ReportsOrchestrator] Support button clicked - navigating to Support");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Support");
        }

        public void HandleSettingsClick()
        {
            Debug.Log("[ReportsOrchestrator] Settings button clicked - navigating to Settings");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Settings");
        }

        public void HandleLogoutClick()
        {
            Debug.Log("[ReportsOrchestrator] Logout button clicked - executing logout");
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

        private void GetUiComponents(VisualElement root)
        {
            _uiConfig.Body = root.Q<VisualElement>("Body");
            _uiConfig.SubpanelsContainer = _subpanelsAndSmokeMaskContainer;
            _uiConfig.Scrim = _subpanelsAndSmokeMaskContainer?.Q<VisualElement>("Scrim");
            _uiConfig.MainContentArea = root.Q<VisualElement>("MainContentArea");
            _uiConfig.NavigationMenuPanel = _subpanelsAndSmokeMaskContainer?.Q<VisualElement>("NavigationMenuPanel");

            _reportStatusLabel = root.Q<Label>("ReportStatusLabel");
            _totalReportsLabel = root.Q<Label>("TotalReportsLabel");
            _reportsTodayLabel = root.Q<Label>("ReportsTodayLabel");
            _scheduledReportsLabel = root.Q<Label>("ScheduledReportsLabel");
            _storageUsedLabel = root.Q<Label>("StorageUsedLabel");
            _reportProgressBar = root.Q<ProgressBar>("ReportProgressBar");
            _reportStatusMessage = root.Q<Label>("ReportStatusMessage");

            InitializePanelConfiguration(root);
        }

        private void InitializePanelConfiguration(VisualElement root)
        {
            var panelsContainer = _subpanelsAndSmokeMaskContainer;

            _uiConfig.Panels[IReportsOps.PanelType.NavigationMenu] = new ReportsInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer?.Q<VisualElement>("NavigationMenuPanel"),
                ShowClass = "NavigationMenuPanelInMainScreen",
                HideClass = "NavigationMenuPanelOutMainScreen",
                RequiresScrim = true,
                AnimationDuration = 0.3f
            };

            _uiConfig.Panels[IReportsOps.PanelType.CustomReportEditor] = new ReportsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "CustomReportEditorVisible",
                HideClass = "CustomReportEditorHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IReportsOps.PanelType.ScheduleReport] = new ReportsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "ScheduleReportVisible",
                HideClass = "ScheduleReportHidden",
                IsModal = true,
                RequiresScrim = true
            };

            _uiConfig.Panels[IReportsOps.PanelType.ReportHistory] = new ReportsInfo.UIConfiguration.PanelData
            {
                Panel = null,
                ShowClass = "ReportHistoryVisible",
                HideClass = "ReportHistoryHidden",
                IsModal = true,
                RequiresScrim = true
            };

            foreach (var kvp in _uiConfig.Panels)
            {
                var panelData = kvp.Value;
                if (panelData.Panel != null)
                {
                    panelData.Panel.RegisterCallback<TransitionEndEvent>(OnTransitionEndEvent);
                }
            }
        }

        private void FindDependencies()
        {
            _mainUIController = UIController.Instance;
            if (_mainUIController == null)
            {
                Debug.LogWarning("UIController not found - will try to find it later");
            }
        }

        private void InitializeReportsState()
        {
            _reportsState.IsInitialized = true;
            _reportsState.CurrentSection = "Reports";
            _reportsState.CurrentActivePanel = IReportsOps.PanelType.None;
            _reportsState.TotalReports = 0;
            _reportsState.ReportsToday = 0;
            _reportsState.ScheduledReports = 0;
            _reportsState.StorageUsedMB = 0;
        }

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
            }
        }

        private void OnTransitionEndEvent(TransitionEndEvent evt)
        {
            if (!_uiManager.IsAnyPanelVisible())
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }

        private void OnLogoutRequestedHandler()
        {
            OnLogoutRequested?.Invoke();
        }

        private void OnPanelTransitionCompleteHandler(IReportsOps.PanelType panelType)
        {
            OnPanelTransitionComplete?.Invoke(panelType);
        }

        internal void ShowUi()
        {
            Show();
        }

        internal void HideUi()
        {
            Hide();
        }

        public void UpdateReportStatus(ReportsInfo.ReportStatus status)
        {
            _reportsState.CurrentReportStatus = status;
            _uiManager?.UpdateReportStatusLabels(
                _reportStatusLabel, _totalReportsLabel, _reportsTodayLabel,
                _scheduledReportsLabel, _storageUsedLabel, _reportProgressBar,
                _reportStatusMessage, status, _reportsState.TotalReports,
                _reportsState.ReportsToday, _reportsState.ScheduledReports,
                _reportsState.StorageUsedMB
            );
            _reportsState.RaiseReportStatusChanged(status);
        }

        public ReportsUIManager UIManager => _uiManager;
        public ReportsEventManager EventManager => _eventManager;
        public ReportsInfo.ReportsState ReportsState => _reportsState;
        public ReportsInfo.UserData UserData => _userData;
    }
}