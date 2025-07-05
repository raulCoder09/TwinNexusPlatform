using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controller;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.SupportController
{
    /// <summary>
    /// Coordinador principal del Support Center - implementa ISupportOps e IUIController
    /// Equivalente a DashboardOrchestrator pero para Support
    /// </summary>
    public class SupportOrchestrator : MonoBehaviour, ISupportOps, IUIController
    {
        #region IUIController Implementation

        public bool RequiresAuthentication => true;
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _uiConfig?.Body?.style.display == DisplayStyle.Flex;
        public string ControllerName => "SupportController";

        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        #region ISupportOps Implementation

        public bool IsNavigationMenuOpen => _uiManager?.NavigationMenuOpen ?? false;
        public ISupportOps.PanelType CurrentActivePanel => _uiManager?.CurrentActivePanel ?? ISupportOps.PanelType.None;
        public ISupportOps.SupportStatus CurrentSupportStatus => _supportState?.SystemSupportStatus ?? ISupportOps.SupportStatus.Available;

        public event Action OnNavigationMenuOpened;
        public event Action OnNavigationMenuClosed;
        public event Action<ISupportOps.PanelType> OnPanelTransitionComplete;
        public event Action<string> OnSupportActionCompleted;
        public event Action<DiagnosticResult> OnDiagnosticsCompleted;
        public event Action<SupportTicket> OnTicketSubmitted;

        #endregion

        #region Private Fields

        private SupportInfo.UIConfiguration _uiConfig = new SupportInfo.UIConfiguration();
        private SupportInfo.UserData _userData = new SupportInfo.UserData();
        private SupportInfo.SupportState _supportState = new SupportInfo.SupportState();
        private SupportInfo.SupportConfiguration _supportConfig = new SupportInfo.SupportConfiguration();
        
        private SupportUIManager _uiManager;
        private SupportEventManager _eventManager;
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
            HideUi();
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            Debug.Log("[SupportOrchestrator] Started - UI hidden until authentication");
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
                    Debug.Log("SupportOrchestrator already initialized");
                    return true;
                }

                Debug.Log("Initializing SupportOrchestrator...");

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
                
                _uiManager = new SupportUIManager(_uiConfig);
                _eventManager = new SupportEventManager(_uiManager, OnPanelTransitionCompleteHandler, this);
                
                _eventManager.RegisterEvents(_uiDocument);
                _uiManager.InitializePanelSystem();
                
                FindDependencies();
                InitializeSupportState();

                _isInitialized = true;
                Debug.Log("✅ SupportOrchestrator initialized successfully");
                
                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"SupportOrchestrator initialization error: {ex.Message}");
                OnControllerError?.Invoke(this, $"Initialization failed: {ex.Message}");
                return false;
            }
        }

        public void Show()
        {
            if (!ServiceController.Instance.IsCognitoAuthenticated)
            {
                Debug.LogError("Cannot show Support - user not authenticated");
                OnControllerError?.Invoke(this, "Authentication required");
                _mainUIController?.ShowUI("Welcome");
                return;
            }

            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.Flex;
                UpdateUserData();
                UpdateSupportSystemStatus();
                OnControllerShown?.Invoke(this);
                Debug.Log("[SupportOrchestrator] Support UI shown");
            }
        }

        public void Hide()
        {
            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.None;
                _uiManager?.CloseCurrentPanel();
                OnControllerHidden?.Invoke(this);
                Debug.Log("[SupportOrchestrator] Support UI hidden");
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
                _supportState = null;
                _supportConfig = null;
                _uiDocument = null;
                _subpanelsAndSmokeMaskContainer = null;

                _isInitialized = false;
                Debug.Log("[SupportOrchestrator] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SupportOrchestrator] Cleanup error: {ex.Message}");
            }
        }

        #endregion

        #region ISupportOps Implementation

        public void NavigateToPanel(ISupportOps.PanelType panelType)
        {
            if (!_isInitialized) Initialize();
            _uiManager?.ShowPanel(panelType);
        }

        public void CloseCurrentPanel()
        {
            _uiManager?.CloseCurrentPanel();
        }

        public void SwitchPanel(ISupportOps.PanelType fromPanel, ISupportOps.PanelType toPanel)
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

        public void ContactTechnicalSupport()
        {
            HandleTechnicalSupportClick();
        }

        public void BrowseDocumentation()
        {
            HandleDocumentationClick();
        }

        public void RunSystemDiagnostics()
        {
            HandleDiagnosticsClick();
        }

        public void RequestRemoteAssistance()
        {
            HandleRemoteAssistanceClick();
        }

        public void SubmitSupportTicket(string subject, string description, ISupportOps.SupportPriority priority)
        {
            StartCoroutine(SubmitSupportTicketCoroutine(subject, description, priority));
        }

        public void ViewSupportHistory()
        {
            Debug.Log("View Support History not implemented yet");
            OnSupportActionCompleted?.Invoke("Support history feature coming soon");
        }

        public void DownloadSystemLogs()
        {
            StartCoroutine(DownloadSystemLogsCoroutine());
        }

        #endregion

        #region Public Event Handlers

        public void HandleDashboardClick()
        {
            Debug.Log("Dashboard button clicked from Support");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Dashboard");
        }

        public void HandleTrainingClick()
        {
            var parameters = new Dictionary<string, object> {
                ["context"] = "Training",
                ["sourceController"] = "Support"
            };
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }

        public void HandleOperationsClick()
        {
            var parameters = new Dictionary<string, object> {
                ["context"] = "Operations", 
                ["sourceController"] = "Support"
            };
            _mainUIController?.ShowUI("DeviceSelection", parameters);
        }

        public void HandleSettingsClick()
        {
            Debug.Log("Settings button clicked from Support");
            _uiManager?.HideNavigationMenu();
            Hide();
            _mainUIController?.ShowUI("Settings");
        }

        public void HandleLogoutClick()
        {
            Debug.Log("🔴 Support HandleLogoutClick() called");
            _uiManager?.HideNavigationMenu();
            Hide();
            
            var uiController = UIController.Instance;
            if (uiController != null)
            {
                Debug.Log("🔴 Calling UIController.RequestLogout() from Support");
                uiController.RequestLogout();
            }
            else
            {
                Debug.Log("🔴 UIController not found - doing direct logout from Support");
                ServiceController.Instance?.CognitoManager?.SignOut();
            }
        }

        public void HandleTechnicalSupportClick()
        {
            Debug.Log("Technical Support clicked");
            
            var contactInfo = _supportConfig.ContactInfo;
            var subject = "Technical Support Request";
            var body = $"Hello Support Team,\n\nI need technical assistance with the Twin Nexus Platform.\n\nUser: {_userData.Username}\nTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\nPlease describe your issue:\n\n";
            
            var emailUrl = $"mailto:{contactInfo.TechnicalEmail}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
            
            try
            {
                Application.OpenURL(emailUrl);
                OnSupportActionCompleted?.Invoke("Technical support email opened");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to open email client: {ex.Message}");
                OnSupportActionCompleted?.Invoke("Failed to open email client");
            }
        }

        public void HandleDocumentationClick()
        {
            Debug.Log("Documentation clicked");
            
            var docUrl = _supportConfig.Documentation.BaseDocumentationUrl;
            
            try
            {
                Application.OpenURL(docUrl);
                OnSupportActionCompleted?.Invoke("Documentation opened in browser");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to open documentation: {ex.Message}");
                OnSupportActionCompleted?.Invoke("Failed to open documentation");
            }
        }

        public void HandleDiagnosticsClick()
        {
            Debug.Log("System Diagnostics clicked");
            
            if (_supportState.DiagnosticsRunning)
            {
                Debug.LogWarning("Diagnostics already running");
                return;
            }
            
            StartCoroutine(RunSystemDiagnosticsCoroutine());
        }

        public void HandleRemoteAssistanceClick()
        {
            Debug.Log("Remote Assistance clicked");
            
            if (_supportState.RemoteAssistanceActive)
            {
                EndRemoteAssistanceSession();
            }
            else
            {
                StartRemoteAssistanceSession();
            }
        }

        #endregion

        #region Private Implementation Methods

        private void GetUiComponents(VisualElement root)
        {
            _uiConfig.Body = root.Q<VisualElement>("Body");
            _uiConfig.SubpanelsContainer = _subpanelsAndSmokeMaskContainer;
            _uiConfig.Scrim = _subpanelsAndSmokeMaskContainer?.Q<VisualElement>("Scrim");
            _uiConfig.MainContentArea = root.Q<VisualElement>("MainContentArea");
            _uiConfig.StatusBar = root.Q<VisualElement>("StatusBar");

            InitializePanelConfiguration(root);
        }

        private void InitializePanelConfiguration(VisualElement root)
        {
            var panelsContainer = _subpanelsAndSmokeMaskContainer;
            
            _uiConfig.Panels[ISupportOps.PanelType.NavigationMenu] = new SupportInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer?.Q<VisualElement>("NavigationMenuPanel"),
                ShowClass = "NavigationMenuPanelinMainScreen",
                HideClass = "NavigationMenuPanelOutMainScreen",
                RequiresScrim = true,
                AnimationDuration = 0.3f
            };

            // Paneles futuros (no existen en UXML actual)
            var futurePanels = new[]
            {
                ISupportOps.PanelType.TechnicalSupport,
                ISupportOps.PanelType.Documentation,
                ISupportOps.PanelType.SystemDiagnostics,
                ISupportOps.PanelType.RemoteAssistance,
                ISupportOps.PanelType.SupportTickets,
                ISupportOps.PanelType.SupportHistory
            };

            foreach (var panelType in futurePanels)
            {
                _uiConfig.Panels[panelType] = new SupportInfo.UIConfiguration.PanelData
                {
                    Panel = null,
                    ShowClass = $"{panelType}PanelVisible",
                    HideClass = $"{panelType}PanelHidden",
                    IsModal = true,
                    RequiresScrim = true
                };
            }

            // Registrar callbacks solo para paneles que existen
            foreach (var kvp in _uiConfig.Panels)
            {
                if (kvp.Value.Panel != null)
                {
                    kvp.Value.Panel.RegisterCallback<TransitionEndEvent>(OnTransitionEndEvent);
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

        private void InitializeSupportState()
        {
            _supportState.IsInitialized = true;
            _supportState.CurrentSection = "Support";
            _supportState.CurrentActivePanel = ISupportOps.PanelType.None;
            _supportState.SystemSupportStatus = ISupportOps.SupportStatus.Available;
            _supportState.DiagnosticsRunning = false;
            _supportState.RemoteAssistanceActive = false;
            _supportState.OpenTickets = 0;
            _supportState.TotalTicketsSubmitted = 0;
            _supportState.ResolvedTickets = 0;
            _supportState.AverageResponseTime = TimeSpan.FromHours(2);
            
            InitializeDefaultConfiguration();
        }

        private void InitializeDefaultConfiguration()
        {
            _supportConfig.ContactInfo = new SupportContactInfo
            {
                EmergencyPhone = "+1-800-SUPPORT",
                GeneralEmail = "support@twinnexus.com",
                TechnicalEmail = "technical@twinnexus.com",
                BusinessHours = "Mon-Fri 8AM-6PM EST",
                TimeZone = "EST",
                WebsiteUrl = "https://twinnexus.com/support",
                RemoteAssistanceUrl = "https://remote.twinnexus.com"
            };

            _supportConfig.Documentation.BaseDocumentationUrl = "https://docs.twinnexus.com";
            _supportConfig.RemoteAssistance.EnableRemoteAssistance = true;
            _supportConfig.Diagnostics.AutoRunOnStartup = false;
            _supportConfig.Ticketing.EnableTicketSystem = true;
            _supportConfig.EnableAnalytics = true;
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
                _userData.Email = userInfo.Value.username;
                _userData.LastLoginTime = DateTime.Now;
                _userData.CurrentSupportLevel = DetermineSupportLevel(_userData.UserGroup);
            }
        }

        private string DetermineSupportLevel(string userGroup)
        {
            return userGroup?.ToLower() switch
            {
                "administradores" => "Enterprise",
                "usuarios-avanzados" => "Premium", 
                "usuarios-basicos" => "Basic",
                _ => "Basic"
            };
        }

        private void UpdateSupportSystemStatus()
        {
            var currentHour = DateTime.Now.Hour;
            
            if (currentHour >= 8 && currentHour < 18)
            {
                _supportState.SystemSupportStatus = ISupportOps.SupportStatus.Available;
            }
            else
            {
                _supportState.SystemSupportStatus = ISupportOps.SupportStatus.Offline;
            }

            _uiManager?.UpdateSupportStatus(_supportState.SystemSupportStatus);
            _uiManager?.UpdateOpenTicketsCount(_supportState.OpenTickets);
            _uiManager?.UpdateLastDiagnosticLabel(_supportState.LastDiagnosticsRun);
        }

        #endregion

        #region Support Operations (Coroutines)

        private IEnumerator RunSystemDiagnosticsCoroutine()
        {
            Debug.Log("Starting system diagnostics...");
            
            _supportState.DiagnosticsRunning = true;
            _uiManager?.ShowDiagnosticsRunning(true);
            
            var diagnosticResult = new DiagnosticResult
            {
                Timestamp = DateTime.Now,
                ReportId = Guid.NewGuid().ToString("N")[0..8].ToUpper(),
                Issues = new List<DiagnosticResult.DiagnosticIssue>()
            };

            yield return new WaitForSeconds(1f);

            diagnosticResult.SystemMetrics["NetworkConnectivity"] = Application.internetReachability != NetworkReachability.NotReachable;
            
            yield return new WaitForSeconds(1f);

            var memoryUsage = GC.GetTotalMemory(false) / (1024 * 1024);
            diagnosticResult.SystemMetrics["MemoryUsageMB"] = memoryUsage;
            
            if (memoryUsage > 500)
            {
                diagnosticResult.Issues.Add(new DiagnosticResult.DiagnosticIssue
                {
                    Component = "Memory",
                    Issue = "High memory usage detected",
                    Severity = "Medium",
                    Recommendation = "Consider restarting the application"
                });
            }

            yield return new WaitForSeconds(1f);

            var cognitoAvailable = ServiceController.Instance?.IsCognitoAuthenticated ?? false;
            diagnosticResult.SystemMetrics["CognitoService"] = cognitoAvailable;
            
            if (!cognitoAvailable)
            {
                diagnosticResult.Issues.Add(new DiagnosticResult.DiagnosticIssue
                {
                    Component = "Authentication",
                    Issue = "Cognito service not properly initialized",
                    Severity = "High",
                    Recommendation = "Check AWS credentials and network connectivity"
                });
            }

            diagnosticResult.OverallHealthy = diagnosticResult.Issues.Count == 0;
            diagnosticResult.Summary = diagnosticResult.OverallHealthy 
                ? "All systems operating normally" 
                : $"Found {diagnosticResult.Issues.Count} issues requiring attention";

            _supportState.DiagnosticsRunning = false;
            _supportState.LastDiagnosticsRun = DateTime.Now;
            
            _uiManager?.ShowDiagnosticsRunning(false);
            _uiManager?.UpdateLastDiagnosticLabel(_supportState.LastDiagnosticsRun);
            
            Debug.Log($"Diagnostics completed - Report ID: {diagnosticResult.ReportId}, Healthy: {diagnosticResult.OverallHealthy}");
            
            OnDiagnosticsCompleted?.Invoke(diagnosticResult);
            OnSupportActionCompleted?.Invoke($"System diagnostics completed - Report {diagnosticResult.ReportId}");
        }

        private IEnumerator SubmitSupportTicketCoroutine(string subject, string description, ISupportOps.SupportPriority priority)
        {
            Debug.Log($"Submitting support ticket: {subject}");
            
            var ticket = new SupportTicket
            {
                TicketId = $"TN-{DateTime.Now:yyyyMMdd}-{UnityEngine.Random.Range(1000, 9999)}",
                Subject = subject,
                Description = description,
                Priority = priority,
                CreatedAt = DateTime.Now,
                Status = "Open",
                UserId = _userData.Username,
                UserEmail = _userData.Email
            };

            yield return new WaitForSeconds(0.5f);

            try
            {
                _supportState.TotalTicketsSubmitted++;
                _supportState.OpenTickets++;
                
                _uiManager?.UpdateOpenTicketsCount(_supportState.OpenTickets);
                
                Debug.Log($"Support ticket submitted successfully - ID: {ticket.TicketId}");
                
                OnTicketSubmitted?.Invoke(ticket);
                OnSupportActionCompleted?.Invoke($"Support ticket {ticket.TicketId} submitted successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to submit support ticket: {ex.Message}");
                OnSupportActionCompleted?.Invoke("Failed to submit support ticket");
            }
        }

        private IEnumerator DownloadSystemLogsCoroutine()
        {
            Debug.Log("Starting system logs download...");
            
            yield return new WaitForSeconds(1f);

            try
            {
                var logFileName = $"system_logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var logContent = GenerateSystemLogsContent();
                
                Debug.Log($"System logs generated: {logFileName} ({logContent.Length} characters)");
                
                OnSupportActionCompleted?.Invoke($"System logs downloaded: {logFileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to download system logs: {ex.Message}");
                OnSupportActionCompleted?.Invoke("Failed to download system logs");
            }
        }

        private string GenerateSystemLogsContent()
        {
            var logs = new System.Text.StringBuilder();
            logs.AppendLine($"=== Twin Nexus Platform System Logs ===");
            logs.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logs.AppendLine($"User: {_userData.Username}");
            logs.AppendLine($"Platform: {Application.platform}");
            logs.AppendLine($"Unity Version: {Application.unityVersion}");
            logs.AppendLine();
            
            logs.AppendLine("=== Authentication Status ===");
            logs.AppendLine($"Authenticated: {ServiceController.Instance?.IsCognitoAuthenticated}");
            logs.AppendLine($"User Group: {_userData.UserGroup}");
            logs.AppendLine($"User Role: {_userData.UserRole}");
            logs.AppendLine();
            
            logs.AppendLine("=== System Status ===");
            logs.AppendLine($"Support Status: {_supportState.SystemSupportStatus}");
            logs.AppendLine($"Memory Usage: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
            logs.AppendLine($"Network: {Application.internetReachability}");
            logs.AppendLine();
            
            logs.AppendLine("=== Support Statistics ===");
            logs.AppendLine($"Open Tickets: {_supportState.OpenTickets}");
            logs.AppendLine($"Total Tickets: {_supportState.TotalTicketsSubmitted}");
            logs.AppendLine($"Last Diagnostics: {_supportState.LastDiagnosticsRun?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never"}");
            
            return logs.ToString();
        }

        #endregion

        #region Remote Assistance

        private void StartRemoteAssistanceSession()
        {
            Debug.Log("Starting remote assistance session...");
            
            if (!_supportConfig.RemoteAssistance.EnableRemoteAssistance)
            {
                Debug.LogWarning("Remote assistance is disabled");
                OnSupportActionCompleted?.Invoke("Remote assistance is not enabled");
                return;
            }

            try
            {
                var sessionId = $"RA-{DateTime.Now:yyyyMMdd}-{UnityEngine.Random.Range(1000, 9999)}";
                var remoteUrl = $"{_supportConfig.RemoteAssistance.RemoteToolUrl}?session={sessionId}&user={_userData.Username}";
                
                Application.OpenURL(remoteUrl);
                
                _supportState.RemoteAssistanceActive = true;
                _uiManager?.UpdateRemoteAssistanceStatus(true, sessionId);
                
                Debug.Log($"Remote assistance session started: {sessionId}");
                OnSupportActionCompleted?.Invoke($"Remote assistance session {sessionId} started");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start remote assistance: {ex.Message}");
                OnSupportActionCompleted?.Invoke("Failed to start remote assistance");
            }
        }

        private void EndRemoteAssistanceSession()
        {
            Debug.Log("Ending remote assistance session...");
            
            _supportState.RemoteAssistanceActive = false;
            _uiManager?.UpdateRemoteAssistanceStatus(false);
            
            OnSupportActionCompleted?.Invoke("Remote assistance session ended");
        }

        #endregion

        #region Event Handlers

        private void OnTransitionEndEvent(TransitionEndEvent evt)
        {
            if (!_uiManager.IsAnyPanelVisible())
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
                Debug.Log("All panels closed - hiding container");
            }
        }

        private void OnPanelTransitionCompleteHandler(ISupportOps.PanelType panelType)
        {
            OnPanelTransitionComplete?.Invoke(panelType);
            Debug.Log($"Panel transition complete: {panelType}");
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

        #endregion

        #region Public Properties

        public SupportUIManager UIManager => _uiManager;
        public SupportEventManager EventManager => _eventManager;
        public SupportInfo.SupportState SupportState => _supportState;
        public SupportInfo.UserData UserData => _userData;
        public SupportInfo.SupportConfiguration SupportConfiguration => _supportConfig;

        #endregion
    }
}