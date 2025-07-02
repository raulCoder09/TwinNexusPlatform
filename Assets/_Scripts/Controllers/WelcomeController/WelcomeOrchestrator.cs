using System.Collections.Generic;
using _Scripts.Controller;
using _Scripts.Models.CognitoManagement;
using _Scripts.Controllers.UiManagement; 
using Amazon;

namespace _Scripts.Controllers.WelcomeController
{
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class WelcomeOrchestrator : MonoBehaviour, IWelcomeOps, IUIController // ✅ AGREGADO: Implementar IUIController
    {
        #region Singleton Pattern

        private static WelcomeOrchestrator _instance;

        public static WelcomeOrchestrator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<WelcomeOrchestrator>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("WelcomeOrchestrator");
                        _instance = go.AddComponent<WelcomeOrchestrator>();
                        DontDestroyOnLoad(go);
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                // Cleanup usando método de IUIController
                Cleanup();
                _instance = null;
            }
        }

        #endregion

        #region IUIController Implementation

        public bool RequiresAuthentication => false; // ✅ Welcome no requiere autenticación
        public bool IsInitialized => _isInitialized;
        public bool IsActive => _uiConfig?.Body?.style.display == DisplayStyle.Flex;
        public string ControllerName => "WelcomeController";

        // Events from IUIController
        public event Action<IUIController> OnControllerInitialized;
        public event Action<IUIController> OnControllerShown;
        public event Action<IUIController> OnControllerHidden;
        public event Action<IUIController, string> OnControllerError;

        #endregion

        private WelcomeInfo.UIConfiguration _uiConfig = new WelcomeInfo.UIConfiguration();
        private WelcomeInfo.UserData _userData = new WelcomeInfo.UserData();
        
        private WelcomeUIManager _uiManager;
        private WelcomeEventManager _eventManager;
        
        // ✅ CAMBIADO: Ya no DashboardController, ahora UIController
        private UIController _uiController;
        
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private UIDocument _uiDocument;
        private bool _isInitialized = false;
        
        [Header("Cognito Settings Configuration")]
        [SerializeField] private SettingsCognitoParametersData _cognitoSettings;
        
        private const string DEFAULT_COGNITO_SETTINGS_PATH = "CognitoSettings/DefaultCognitoSettings";

        // Eventos públicos del Orchestrator
        public event Action OnAuthenticationSuccess;

        #region Manejo de eventos de CognitoManager via ServiceController

        private void OnAuthenticationSuccessHandler()
        {
            _userData.IsAuthenticated = true;
            _userData.Username = ServiceController.Instance.GetUserInfo().username;

            ShowMessage("Authentication successful!", false);
            OnAuthenticationSuccess?.Invoke();

            Debug.Log("Authentication successful - services ready");
            
            // ✅ CAMBIADO: Usar UIController para navegar a Dashboard
            if (_uiController != null)
            {
                _uiController.ShowUI("Dashboard");
            }
            else
            {
                // Fallback: buscar UIController si no se tiene referencia
                var uiController = UIController.Instance;
                if (uiController != null)
                {
                    uiController.ShowUI("Dashboard");
                }
                else
                {
                    Debug.LogWarning("UIController not found - cannot navigate to Dashboard");
                }
            }

            // Cerrar cualquier panel abierto
            _uiManager?.CloseCurrentPanel();
        }

        private void OnAuthenticationFailureHandler(string message)
        {
            ShowMessage("Invalid username or password", true);
            SetLoginButtonEnabled(true);
        }

        private void OnRegistrationSuccessHandler(string message)
        {
            ShowMessage("Account created! Please check your email for verification.", false);
            NavigateToPanel(IWelcomeOps.PanelType.EmailVerification);
            SetEmailVerificationInfo(_userData.Email);
            SetRegisterButtonEnabled(true);
        }

        private void OnRegistrationFailureHandler(string message)
        {
            ShowMessage("Registration failed. Username or email may already exist.", true);
            SetRegisterButtonEnabled(true);
        }

        private void OnEmailVerificationSuccessHandler(string message)
        {
            ShowMessage("Email verified successfully!", false);
            _userData.IsAuthenticated = true;
            _uiManager.CloseCurrentPanel();
            OnAuthenticationSuccessHandler(); // Reutilizar la lógica de éxito de autenticación
            SetVerifyEmailButtonEnabled(true);
        }

        private void OnEmailVerificationFailureHandler(string message)
        {
            ShowMessage("Invalid verification code. Please try again.", true);
            SetVerifyEmailButtonEnabled(true);
        }

        private void OnPasswordRecoverySuccessHandler(string message)
        {
            ShowMessage("Recovery email sent! Please check your inbox.", false);
            SetRecoverButtonEnabled(true);
        }

        private void OnPasswordRecoveryFailureHandler(string message)
        {
            ShowMessage("Recovery failed. Please check the username and try again.", true);
            SetRecoverButtonEnabled(true);
        }

        private void OnResendVerificationSuccessHandler(string message)
        {
            ShowMessage("Verification code resent! Please check your email.", false);
            SetResendCodeButtonEnabled(true);
        }

        private void OnResendVerificationFailureHandler(string message)
        {
            ShowMessage("Failed to resend code. Please try again.", true);
            SetResendCodeButtonEnabled(true);
        }

        #endregion

        #region CognitoManager Access via ServiceController

        /// <summary>
        /// Obtiene CognitoManager desde ServiceController
        /// </summary>
        private CognitoManager GetCognitoManager()
        {
            return ServiceController.Instance?.CognitoManager;
        }

        /// <summary>
        /// Suscribirse a eventos de CognitoManager via ServiceController
        /// </summary>
        private void SubscribeToCognitoEvents()
        {
            var cognitoManager = GetCognitoManager();
            if (cognitoManager != null)
            {
                Debug.Log("[WelcomeOrchestrator] Subscribing to CognitoManager events via ServiceController");
                
                cognitoManager.OnAuthenticationComplete += OnCognitoAuthenticationComplete;
                cognitoManager.OnRegistrationComplete += OnCognitoRegistrationComplete;
                cognitoManager.OnEmailVerificationComplete += OnCognitoEmailVerificationComplete;
                cognitoManager.OnPasswordRecoveryComplete += OnCognitoPasswordRecoveryComplete;
                cognitoManager.OnResendVerificationComplete += OnCognitoResendVerificationComplete;
            }
            else
            {
                Debug.LogWarning("[WelcomeOrchestrator] CognitoManager not available from ServiceController");
            }
        }

        /// <summary>
        /// Desuscribirse de eventos de CognitoManager
        /// </summary>
        private void UnsubscribeFromCognitoEvents()
        {
            var cognitoManager = GetCognitoManager();
            if (cognitoManager != null)
            {
                Debug.Log("[WelcomeOrchestrator] Unsubscribing from CognitoManager events");
                
                cognitoManager.OnAuthenticationComplete -= OnCognitoAuthenticationComplete;
                cognitoManager.OnRegistrationComplete -= OnCognitoRegistrationComplete;
                cognitoManager.OnEmailVerificationComplete -= OnCognitoEmailVerificationComplete;
                cognitoManager.OnPasswordRecoveryComplete -= OnCognitoPasswordRecoveryComplete;
                cognitoManager.OnResendVerificationComplete -= OnCognitoResendVerificationComplete;
            }
        }

        /// <summary>
        /// Maneja eventos de autenticación desde CognitoManager
        /// </summary>
        private void OnCognitoAuthenticationComplete(bool success, string message)
        {
            if (success)
            {
                OnAuthenticationSuccessHandler();
            }
            else
            {
                OnAuthenticationFailureHandler(message);
            }
        }

        private void OnCognitoRegistrationComplete(bool success, string message)
        {
            if (success)
            {
                OnRegistrationSuccessHandler(message);
            }
            else
            {
                OnRegistrationFailureHandler(message);
            }
        }

        private void OnCognitoEmailVerificationComplete(bool success, string message)
        {
            if (success)
            {
                OnEmailVerificationSuccessHandler(message);
            }
            else
            {
                OnEmailVerificationFailureHandler(message);
            }
        }

        private void OnCognitoPasswordRecoveryComplete(bool success, string message)
        {
            if (success)
            {
                OnPasswordRecoverySuccessHandler(message);
            }
            else
            {
                OnPasswordRecoveryFailureHandler(message);
            }
        }

        private void OnCognitoResendVerificationComplete(bool success, string message)
        {
            if (success)
            {
                OnResendVerificationSuccessHandler(message);
            }
            else
            {
                OnResendVerificationFailureHandler(message);
            }
        }

        #endregion

        #region IUIController Lifecycle Methods

        public bool Initialize()
        {
            try
            {
                if (_isInitialized)
                {
                    Debug.Log("WelcomeOrchestrator already initialized");
                    return true;
                }

                Debug.Log("Initializing WelcomeOrchestrator...");

                // Initialize Cognito Settings
                InitializeCognitoSettings();

                // Suscribirse a ServiceController
                SubscribeToCognitoEvents();
                
                // Apply loaded configuration to CognitoManager via ServiceController
                ApplyCognitoConfiguration();

                // Obtener componentes UI
                _uiDocument = GetComponent<UIDocument>();
                var root = _uiDocument.rootVisualElement;
                _subpanelsAndSmokeMaskContainer = root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");

                // Inicializar managers
                _uiManager = new WelcomeUIManager(_uiConfig);
                _eventManager = new WelcomeEventManager(_uiManager, OnExitApplication, OnPanelTransitionCompleteHandler, this);
                _eventManager.RegisterEvents(_uiDocument);

                GetUiComponents(root);
                FindDependencies();
                StartCoroutine(GlitchEffectRoutine());
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;

                _isInitialized = true;
                Debug.Log("WelcomeOrchestrator initialized successfully");
                
                OnControllerInitialized?.Invoke(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Initialization error: {ex.Message}");
                OnControllerError?.Invoke(this, $"Initialization failed: {ex.Message}");
                return false;
            }
        }

        public void Show()
        {
            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.Flex;
                OnControllerShown?.Invoke(this);
                Debug.Log("[WelcomeOrchestrator] Welcome UI shown");
            }
        }

        public void Hide()
        {
            if (_uiConfig?.Body != null)
            {
                _uiConfig.Body.style.display = DisplayStyle.None;
                OnControllerHidden?.Invoke(this);
                Debug.Log("[WelcomeOrchestrator] Welcome UI hidden");
            }
        }

        public void Cleanup()
        {
            try
            {
                // Desuscribirse de eventos de ServiceController/CognitoManager
                UnsubscribeFromCognitoEvents();

                // Limpiar managers
                _eventManager?.Cleanup(); // ✅ CORREGIDO: Ahora Cleanup() existe
                _uiManager = null;
                _eventManager = null;

                // Limpiar referencias
                _uiController = null;
                _uiConfig = null;
                _userData = null;
                _uiDocument = null;
                _subpanelsAndSmokeMaskContainer = null;

                _isInitialized = false;
                Debug.Log("[WelcomeOrchestrator] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WelcomeOrchestrator] Cleanup error: {ex.Message}");
            }
        }

        #endregion

        #region IWelcomeOps Implementation - Delegando a ServiceController.CognitoManager

        public async Task<bool> AuthenticateUserAsync(string username, string password)
        {
            if (!_isInitialized) Initialize();
            
            var cognitoManager = GetCognitoManager();
            if (cognitoManager == null)
            {
                Debug.LogError("CognitoManager not available from ServiceController");
                ShowMessage("Authentication system not ready", true);
                return false;
            }

            // Validaciones
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowMessage("Please fill in all fields", true);
                return false;
            }

            try
            {
                SetLoginButtonEnabled(false);
                ShowMessage("Authenticating...", false);

                _userData.Username = username;
                var success = await cognitoManager.SignInAsync(username, password);

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Authentication error: {ex.Message}");
                ShowMessage("Authentication failed. Please try again.", true);
                SetLoginButtonEnabled(true);
                return false;
            }
        }

        public async Task<bool> RegisterUserAsync(string username, string password, string email, string phoneNumber = null)
        {
            if (!_isInitialized) Initialize();
            
            var cognitoManager = GetCognitoManager();
            if (cognitoManager == null)
            {
                Debug.LogError("CognitoManager not available from ServiceController");
                ShowMessage("Authentication system not ready", true);
                return false;
            }

            // Validaciones
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))
            {
                ShowMessage("Please fill in all required fields", true);
                return false;
            }

            if (!ValidationHelper.IsValidEmail(email))
            {
                ShowMessage("Please enter a valid email address", true);
                return false;
            }

            if (!ValidationHelper.IsValidPassword(password))
            {
                ShowMessage("Password must be at least 8 characters with uppercase, lowercase, and number", true);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(phoneNumber) && !ValidationHelper.IsValidPhoneNumber(phoneNumber))
            {
                ShowMessage("Please enter a valid phone number (e.g., +1234567890)", true);
                return false;
            }

            try
            {
                SetRegisterButtonEnabled(false);
                ShowMessage("Creating account...", false);

                _userData.Username = username;
                _userData.Email = email;
                _userData.PhoneNumber = phoneNumber;

                var success = await cognitoManager.SignUpAsync(username, password, email, phoneNumber);
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Registration error: {ex.Message}");
                ShowMessage("Registration failed. Please try again.", true);
                SetRegisterButtonEnabled(true);
                return false;
            }
        }

        public async Task<bool> VerifyEmailAsync(string confirmationCode)
        {
            if (!_isInitialized) Initialize();
            
            var cognitoManager = GetCognitoManager();
            if (cognitoManager == null)
            {
                Debug.LogError("CognitoManager not available from ServiceController");
                ShowMessage("Authentication system not ready", true);
                return false;
            }

            if (string.IsNullOrWhiteSpace(confirmationCode))
            {
                ShowMessage("Please enter the verification code", true);
                return false;
            }

            if (!ValidationHelper.IsValidVerificationCode(confirmationCode))
            {
                ShowMessage("Please enter a valid 6-digit verification code", true);
                return false;
            }

            try
            {
                SetVerifyEmailButtonEnabled(false);
                ShowMessage("Verifying email...", false);

                var success = await cognitoManager.ConfirmSignUpAsync(cognitoManager.PendingUsername, confirmationCode);
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Email verification error: {ex.Message}");
                ShowMessage("Verification failed. Please try again.", true);
                SetVerifyEmailButtonEnabled(true);
                return false;
            }
        }

        public async Task<bool> RecoverPasswordAsync(string username)
        {
            if (!_isInitialized) Initialize();
            
            var cognitoManager = GetCognitoManager();
            if (cognitoManager == null)
            {
                Debug.LogError("CognitoManager not available from ServiceController");
                ShowMessage("Authentication system not ready", true);
                return false;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowMessage("Please enter your username or email", true);
                return false;
            }

            try
            {
                SetRecoverButtonEnabled(false);
                ShowMessage("Sending recovery email...", false);

                _userData.Username = username;
                var success = await cognitoManager.ForgotPasswordAsync(username);
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Password recovery error: {ex.Message}");
                ShowMessage("Recovery failed. Please try again.", true);
                SetRecoverButtonEnabled(true);
                return false;
            }
        }

        public async Task<bool> ResendVerificationCodeAsync()
        {
            if (!_isInitialized) Initialize();
            
            var cognitoManager = GetCognitoManager();
            if (cognitoManager == null)
            {
                Debug.LogError("CognitoManager not available from ServiceController");
                ShowMessage("Authentication system not ready", true);
                return false;
            }

            try
            {
                SetResendCodeButtonEnabled(false);
                ShowMessage("Resending verification code...", false);

                var success = await cognitoManager.ResendConfirmationCodeAsync();
                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Resend verification error: {ex.Message}");
                ShowMessage("Failed to resend code. Please try again.", true);
                SetResendCodeButtonEnabled(true);
                return false;
            }
        }

        public void NavigateToPanel(IWelcomeOps.PanelType panelType)
        {
            if (!_isInitialized) Initialize();
            if (_uiManager == null)
            {
                Debug.LogError("UI manager not initialized");
                return;
            }

            _uiManager.NavigateToPanel(panelType);
    
            // Load saved configuration when opening Settings panel
            if (panelType == IWelcomeOps.PanelType.SettingsCognito)
            {
                LoadSavedConfiguration();
            }
        }

        #endregion

        #region Public Methods for Event Handling

        public async void HandleLoginButtonClick()
        {
            var root = _uiDocument.rootVisualElement;
            var usernameField = root.Q<TextField>("UsernameLoginField");
            var passwordField = root.Q<TextField>("PasswordLoginField");

            if (usernameField != null && passwordField != null)
            {
                await AuthenticateUserAsync(usernameField.value, passwordField.value);
            }
        }

        public async void HandleRegisterButtonClick()
        {
            var root = _uiDocument.rootVisualElement;
            var usernameField = root.Q<TextField>("UsernameRegisterField");
            var emailField = root.Q<TextField>("EmailRegisterField");
            var phoneField = root.Q<TextField>("PhoneRegisterField");
            var passwordField = root.Q<TextField>("PasswordRegisterField");
            var repeatPasswordField = root.Q<TextField>("RepeatPasswordRegisterField");

            if (usernameField != null && emailField != null && passwordField != null && repeatPasswordField != null)
            {
                // Validar que las contraseñas coincidan
                if (passwordField.value != repeatPasswordField.value)
                {
                    ShowMessage("Passwords do not match", true);
                    return;
                }

                await RegisterUserAsync(usernameField.value, passwordField.value, emailField.value, phoneField.value);
            }
        }

        public async void HandleVerifyEmailButtonClick()
        {
            var root = _uiDocument.rootVisualElement;
            var codeField = root.Q<TextField>("VerificationCodeField");

            if (codeField != null)
            {
                await VerifyEmailAsync(codeField.value);
            }
        }

        public async void HandleRecoverPasswordButtonClick()
        {
            var root = _uiDocument.rootVisualElement;
            var emailField = root.Q<TextField>("EmailRecoverField");

            if (emailField != null)
            {
                await RecoverPasswordAsync(emailField.value);
            }
        }

        public async void HandleResendCodeButtonClick()
        {
            await ResendVerificationCodeAsync();
        }
        
        #region Settings Panel Methods

        public async void HandleSaveSettingsButtonClick()
        {
            var root = _uiDocument.rootVisualElement;
            var userPoolIdField = root.Q<TextField>("UserPoolIdField");
            var clientIdField = root.Q<TextField>("ClientIdField");
            var identityPoolIdField = root.Q<TextField>("IdentityPoolIdField");
            var regionDropdown = root.Q<DropdownField>("AwsRegionDropdownField");

            if (userPoolIdField != null && clientIdField != null && identityPoolIdField != null && regionDropdown != null)
            {
                string userPoolId = userPoolIdField.value?.Trim();
                string clientId = clientIdField.value?.Trim();
                string identityPoolId = identityPoolIdField.value?.Trim();
                string selectedRegion = regionDropdown.value;

                // Validate inputs
                if (string.IsNullOrEmpty(userPoolId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(identityPoolId))
                {
                    ShowMessage("Please fill in all required fields", true);
                    return;
                }

                // Convert region display name to region code
                var regionCode = ConvertDisplayToRegionCode(selectedRegion);
                if (string.IsNullOrEmpty(regionCode))
                {
                    ShowMessage("Invalid AWS region selected", true);
                    return;
                }

                try
                {
                    ShowMessage("Saving configuration...", false);
                    SetSaveButtonEnabled(false);

                    // Update ScriptableObject
                    if (_cognitoSettings != null)
                    {
                        _cognitoSettings.SetConfiguration(userPoolId, clientId, identityPoolId, regionCode);
                        
                        // Save to JSON file
                        bool saveSuccess = CognitoSettingsManager.SaveConfiguration(_cognitoSettings);
                        
                        if (saveSuccess)
                        {
                            ApplyCognitoConfiguration();
                            
                            ShowMessage("Configuration saved successfully!", false);
                            Debug.Log($"Cognito configuration saved to: {CognitoSettingsManager.GetConfigurationPath()}");
                            
                            // Close settings panel after successful save
                            await System.Threading.Tasks.Task.Delay(1500);
                            _uiManager.CloseCurrentPanel();
                        }
                        else
                        {
                            ShowMessage("Failed to save configuration to file", true);
                        }
                    }
                    else
                    {
                        ShowMessage("Configuration system not initialized", true);
                        Debug.Log("Cognito settings ScriptableObject is null");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.Log($"Error saving configuration: {ex.Message}");
                    ShowMessage("Failed to save configuration", true);
                }
                finally
                {
                    SetSaveButtonEnabled(true);
                }
            }
        }

        private string ConvertDisplayToRegionCode(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "us-east-1";
            return displayName.Split(' ')[0];
        }

        private void LoadSavedConfiguration()
        {
            var root = _uiDocument.rootVisualElement;
            var userPoolIdField = root.Q<TextField>("UserPoolIdField");
            var clientIdField = root.Q<TextField>("ClientIdField");
            var identityPoolIdField = root.Q<TextField>("IdentityPoolIdField");
            var regionDropdown = root.Q<DropdownField>("AwsRegionDropdownField");

            if (userPoolIdField != null && clientIdField != null && identityPoolIdField != null && regionDropdown != null)
            {
                if (_cognitoSettings != null)
                {
                    userPoolIdField.value = _cognitoSettings.UserPoolId;
                    clientIdField.value = _cognitoSettings.ClientId;
                    identityPoolIdField.value = _cognitoSettings.IdentityPoolId;

                    var regionDisplay = ConvertRegionCodeToDisplay(_cognitoSettings.AwsRegionCode);
                    if (regionDropdown.choices.Contains(regionDisplay))
                    {
                        regionDropdown.value = regionDisplay;
                    }

                    Debug.Log("Configuration loaded into Settings panel from ScriptableObject");
                }
                else
                {
                    Debug.Log("Cognito settings ScriptableObject is null");
                    
                    userPoolIdField.value = "";
                    clientIdField.value = "";
                    identityPoolIdField.value = "";
                    regionDropdown.value = "us-east-1 (N. Virginia)";
                }
            }
        }

        private string ConvertRegionCodeToDisplay(string regionCode)
        {
            return regionCode switch
            {
                "us-east-1" => "us-east-1 (N. Virginia)",
                "us-east-2" => "us-east-2 (Ohio)",
                "us-west-1" => "us-west-1 (N. California)",
                "us-west-2" => "us-west-2 (Oregon)",
                "eu-west-1" => "eu-west-1 (Ireland)",
                "eu-central-1" => "eu-central-1 (Frankfurt)",
                "ap-southeast-1" => "ap-southeast-1 (Singapore)",
                "ap-northeast-1" => "ap-northeast-1 (Tokyo)",
                _ => "us-east-1 (N. Virginia)"
            };
        }

        #endregion

        #endregion

        #region Public Helper Methods

        /// <summary>
        /// Obtiene información del usuario autenticado via ServiceController
        /// </summary>
        public (string username, string userGroup, bool isAuthenticated) GetUserInfo()
        {
            return ServiceController.Instance?.GetUserInfo() ?? (string.Empty, string.Empty, false);
        }

        /// <summary>
        /// Cierra sesión del usuario actual via ServiceController
        /// </summary>
        public void Logout()
        {
            var cognitoManager = GetCognitoManager();
            cognitoManager?.SignOut();
            _userData = new WelcomeInfo.UserData();
        }

        /// <summary>
        /// Verifica si el usuario pertenece a un grupo específico via ServiceController
        /// </summary>
        public bool IsUserInGroup(string groupName)
        {
            return ServiceController.Instance?.IsUserInGroup(groupName) ?? false;
        }

        #endregion

        #region Private Implementation Methods

        private void InitializeCognitoSettings()
        {
            try
            {
                if (_cognitoSettings == null)
                {
                    _cognitoSettings = Resources.Load<SettingsCognitoParametersData>(DEFAULT_COGNITO_SETTINGS_PATH);
            
                    if (_cognitoSettings == null)
                    {
                        _cognitoSettings = ScriptableObject.CreateInstance<SettingsCognitoParametersData>();
                        Debug.Log("Created runtime Cognito settings instance");
                    }
                    else
                    {
                        Debug.Log("Loaded default Cognito settings from Resources");
                    }
                }
        
                bool configLoaded = CognitoSettingsManager.LoadConfiguration(_cognitoSettings);
        
                if (configLoaded)
                {
                    Debug.Log("Cognito configuration loaded from JSON file");
                }
                else
                {
                    Debug.Log("Using default Cognito configuration");
                }
        
                _cognitoSettings.ValidateConfiguration();
                Debug.Log($"Cognito configuration valid: {_cognitoSettings.IsConfigurationValid}");
            }
            catch (Exception ex)
            {
                Debug.Log($"Error initializing Cognito settings: {ex.Message}");
                _cognitoSettings = ScriptableObject.CreateInstance<SettingsCognitoParametersData>();
            }
        }

        private void ApplyCognitoConfiguration()
        {
            if (_cognitoSettings != null)
            {
                try
                {
                    var regionEndpoint = _cognitoSettings.GetRegionEndpoint();
                    
                    // Usar ServiceController para actualizar configuración de CognitoManager
                    bool success = ServiceController.Instance?.UpdateCognitoConfiguration(
                        _cognitoSettings.UserPoolId,
                        _cognitoSettings.ClientId,
                        _cognitoSettings.IdentityPoolId,
                        regionEndpoint
                    ) ?? false;
                    
                    if (success)
                    {
                        Debug.Log("Cognito configuration applied successfully via ServiceController");
                        
                        // Re-suscribirse a eventos del nuevo CognitoManager
                        UnsubscribeFromCognitoEvents();
                        SubscribeToCognitoEvents();
                    }
                    else
                    {
                        Debug.Log("Failed to apply Cognito configuration");
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"Error applying Cognito configuration: {ex.Message}");
                }
            }
            else
            {
                Debug.Log("Cognito settings is null - cannot apply configuration");
            }
        }

        private void GetUiComponents(VisualElement root)
        {
            _uiConfig.Body = root.Q<VisualElement>("Body");
            _uiConfig.SubpanelsContainer = _subpanelsAndSmokeMaskContainer;
            _uiConfig.Scrim = _subpanelsAndSmokeMaskContainer.Q<VisualElement>("Scrim");
            InitializeAwsRegionDropdown(root);

            var panelsContainer = _subpanelsAndSmokeMaskContainer;
            _uiConfig.Panels[IWelcomeOps.PanelType.Login] = new WelcomeInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer.Q<VisualElement>("LoginPanel"),
                ShowClass = "LoginPanelMoveB",
                HideClass = "LoginPanelMoveA"
            };
            Debug.Log($"LoginPanel found: {_uiConfig.Panels[IWelcomeOps.PanelType.Login].Panel != null}");

            _uiConfig.Panels[IWelcomeOps.PanelType.Login].Panel
                .RegisterCallback<TransitionEndEvent>(OnTransitionEndEvent);

            _uiConfig.Panels[IWelcomeOps.PanelType.Register] = new WelcomeInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer.Q<VisualElement>("RegisterPanel"),
                ShowClass = "RegisterPanelInMainScreen",
                HideClass = "RegisterPanelOutMainScreen"
            };
            Debug.Log($"RegisterPanel found: {_uiConfig.Panels[IWelcomeOps.PanelType.Register].Panel != null}");

            _uiConfig.Panels[IWelcomeOps.PanelType.Register].Panel
                .RegisterCallback<TransitionEndEvent>(OnTransitionEndEvent);

            _uiConfig.Panels[IWelcomeOps.PanelType.RecoverPassword] = new WelcomeInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer.Q<VisualElement>("RecoverPasswordPanel"),
                ShowClass = "RecoverPasswordPanelInMainScreen",
                HideClass = "RecoverPasswordPanelOutMainScreen"
            };
            Debug.Log($"RecoverPasswordPanel found: {_uiConfig.Panels[IWelcomeOps.PanelType.RecoverPassword].Panel != null}");

            _uiConfig.Panels[IWelcomeOps.PanelType.RecoverPassword].Panel
                .RegisterCallback<TransitionEndEvent>(OnTransitionEndEvent);

            _uiConfig.Panels[IWelcomeOps.PanelType.EmailVerification] = new WelcomeInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer.Q<VisualElement>("EmailVerificationPanel"),
                ShowClass = "EmailVerificationPanelInMainScreen",
                HideClass = "EmailVerificationPanelOutMainScreen"
            };
            Debug.Log($"EmailVerificationPanel found: {_uiConfig.Panels[IWelcomeOps.PanelType.EmailVerification].Panel != null}");

            _uiConfig.Panels[IWelcomeOps.PanelType.EmailVerification].Panel
                .RegisterCallback<TransitionEndEvent>(OnTransitionEndEvent);
            
            _uiConfig.Panels[IWelcomeOps.PanelType.SettingsCognito] = new WelcomeInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer.Q<VisualElement>("SettingsCognitoParametersPanel"),
                ShowClass = "SettingsCognitoParametersPanelInMainScreen",
                HideClass = "SettingsCognitoParametersPanelOutMainScreen"
            };
            Debug.Log($"SettingsCognitoParametersPanel found: {_uiConfig.Panels[IWelcomeOps.PanelType.SettingsCognito].Panel != null}");

            _uiConfig.Panels[IWelcomeOps.PanelType.SettingsCognito].Panel
                .RegisterCallback<TransitionEndEvent>(OnTransitionEndEvent);
        }
        
        private void InitializeAwsRegionDropdown(VisualElement root)
        {
            var regionDropdown = root.Q<DropdownField>("AwsRegionDropdownField");
            if (regionDropdown != null)
            {
                var availableRegions = new List<string>
                {
                    "us-east-1 (N. Virginia)",
                    "us-east-2 (Ohio)",
                    "us-west-1 (N. California)",
                    "us-west-2 (Oregon)",
                    "eu-west-1 (Ireland)",
                    "eu-central-1 (Frankfurt)",
                    "ap-southeast-1 (Singapore)",
                    "ap-northeast-1 (Tokyo)"
                };
        
                regionDropdown.choices = availableRegions;
                regionDropdown.value = "us-east-1 (N. Virginia)";
        
                Debug.Log("AWS Region dropdown initialized with available regions");
            }
            else
            {
                Debug.Log("AWS Region dropdown not found in UI");
            }
        }

        private void FindDependencies()
        {
            // ✅ CAMBIADO: Buscar UIController en lugar de DashboardController
            _uiController = UIController.Instance;
            if (_uiController == null)
            {
                Debug.LogWarning("UIController not found - will try to find it later");
            }
            else
            {
                Debug.Log("UIController found and referenced");
            }
        }

        private void OnExitApplication()
        {
            Debug.Log("Application exit requested");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void OnTransitionEndEvent(TransitionEndEvent evt)
        {
            if (!IsAnyPanelVisible())
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
                Debug.Log("All panels closed - hiding container");
            }
        }

        private bool IsAnyPanelVisible()
        {
            foreach (var kvp in _uiConfig.Panels)
            {
                var panelData = kvp.Value;
                if (panelData.Panel.ClassListContains(panelData.ShowClass))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnPanelTransitionCompleteHandler(IWelcomeOps.PanelType panelType)
        {
            Debug.Log($"Panel transition complete: {panelType}");
        }

        private void SetEmailVerificationInfo(string email)
        {
            var emailLabel = _uiConfig.Panels[IWelcomeOps.PanelType.EmailVerification].Panel
                .Q<Label>("EmailVerificationEmail");
            if (emailLabel != null)
            {
                emailLabel.text = email;
            }
        }

        #region UI Helper Methods

        private void ShowMessage(string message, bool isError = false)
        {
            var messageLabel = _uiConfig.Body.Q<Label>("MessageLabel");
            if (messageLabel != null)
            {
                messageLabel.text = message;
                messageLabel.style.color = isError ? Color.red : Color.green;
                messageLabel.style.display = DisplayStyle.Flex;
            }

            Debug.Log($"{(isError ? "Error" : "Info")}: {message}");
        }

        private void SetLoginButtonEnabled(bool enabled)
        {
            var button = _uiConfig.Body.Q<Button>("LoginButton");
            if (button != null)
            {
                button.SetEnabled(enabled);
                button.text = enabled ? "Login" : "Logging in...";
            }
        }

        private void SetRegisterButtonEnabled(bool enabled)
        {
            var button = _uiConfig.Body.Q<Button>("RegisterAndLoginButton");
            if (button != null)
            {
                button.SetEnabled(enabled);
                button.text = enabled ? "Register and Login" : "Creating account...";
            }
        }

        private void SetRecoverButtonEnabled(bool enabled)
        {
            var button = _uiConfig.Body.Q<Button>("RecoverPasswordButton");
            if (button != null)
            {
                button.SetEnabled(enabled);
                button.text = enabled ? "Recover" : "Sending email...";
            }
        }

        private void SetVerifyEmailButtonEnabled(bool enabled)
        {
            var button = _uiConfig.Body.Q<Button>("VerifyEmailButton");
            if (button != null)
            {
                button.SetEnabled(enabled);
                button.text = enabled ? "Verify Email" : "Verifying...";
            }
        }

        private void SetResendCodeButtonEnabled(bool enabled)
        {
            var button = _uiConfig.Body.Q<Button>("ResendVerificationCodeButton");
            if (button != null)
            {
                button.SetEnabled(enabled);
                button.text = enabled ? "Resend Code" : "Resending...";
            }
        }
        
        private void SetSaveButtonEnabled(bool enabled)
        {
            var button = _uiConfig.Body.Q<Button>("SaveButton");
            if (button != null)
            {
                button.SetEnabled(enabled);
                button.text = enabled ? "Save" : "Saving...";
            }
        }

        #endregion

        private IEnumerator GlitchEffectRoutine()
        {
            yield return new WaitForSeconds(0.5f);
            var validPanels = new[]
            {
                IWelcomeOps.PanelType.Login, IWelcomeOps.PanelType.Register, IWelcomeOps.PanelType.RecoverPassword,
                IWelcomeOps.PanelType.EmailVerification, IWelcomeOps.PanelType.SettingsCognito
            };
            foreach (var panelType in validPanels)
            {
                if (_uiConfig.Panels.ContainsKey(panelType))
                {
                    var title = _uiConfig.Panels[panelType].Panel.Q<Label>($"{panelType}Title");
                    if (title != null)
                    {
                        yield return StartCoroutine(ApplyGlitchEffect(title));
                    }
                    else
                    {
                        Debug.LogWarning($"Title not found for panel {panelType}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Panel {panelType} not found in _uiConfig.Panels");
                }
            }

            while (true)
            {
                foreach (var panelType in validPanels)
                {
                    if (_uiConfig.Panels.ContainsKey(panelType))
                    {
                        var title = _uiConfig.Panels[panelType].Panel.Q<Label>($"{panelType}Title");
                        if (title != null)
                        {
                            yield return StartCoroutine(ApplyGlitchEffect(title));
                        }
                    }
                }

                yield return new WaitForSeconds(3f);
            }
        }

        private IEnumerator ApplyGlitchEffect(Label title)
        {
            title.AddToClassList("glitch");
            yield return new WaitForSeconds(0.2f);
            title.RemoveFromClassList("glitch");
        }

        #endregion
    }
}