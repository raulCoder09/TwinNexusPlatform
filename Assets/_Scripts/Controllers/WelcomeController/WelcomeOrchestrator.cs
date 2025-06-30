using System.Collections.Generic;
using _Scripts.Controller;
using _Scripts.Models.CognitoManagement;
using Amazon;

namespace _Scripts.Controllers.WelcomeController
{
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class WelcomeOrchestrator : MonoBehaviour, IWelcomeOps
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
                // Desuscribirse de eventos de WelcomeAuthHandler
                if (_authHandler != null)
                {
                    _authHandler.OnAuthenticationSuccess -= OnAuthenticationSuccessHandler;
                    _authHandler.OnAuthenticationFailure -= OnAuthenticationFailureHandler;
                    _authHandler.OnRegistrationSuccess -= OnRegistrationSuccessHandler;
                    _authHandler.OnRegistrationFailure -= OnRegistrationFailureHandler;
                    _authHandler.OnEmailVerificationSuccess -= OnEmailVerificationSuccessHandler;
                    _authHandler.OnEmailVerificationFailure -= OnEmailVerificationFailureHandler;
                    _authHandler.OnPasswordRecoverySuccess -= OnPasswordRecoverySuccessHandler;
                    _authHandler.OnPasswordRecoveryFailure -= OnPasswordRecoveryFailureHandler;
                    _authHandler.OnResendVerificationSuccess -= OnResendVerificationSuccessHandler;
                    _authHandler.OnResendVerificationFailure -= OnResendVerificationFailureHandler;
                }

                _authHandler = null;
                _uiManager = null;
                _eventManager = null;
                _instance = null;
            }
        }

        #endregion

        private WelcomeInfo.UIConfiguration _uiConfig = new WelcomeInfo.UIConfiguration();
        private WelcomeInfo.UserData _userData = new WelcomeInfo.UserData();
        private WelcomeAuthHandler _authHandler;
        private WelcomeUIManager _uiManager;
        private WelcomeEventManager _eventManager;
        private DashboardController _dashboardController;
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private UIDocument _uiDocument;
        private bool _isInitialized = false;
        
        [Header("Cognito Settings Configuration")]
        [SerializeField] private SettingsCognitoParametersData _cognitoSettings;
        
        private const string DEFAULT_COGNITO_SETTINGS_PATH = "CognitoSettings/DefaultCognitoSettings";

        // Eventos públicos del Orchestrator
        public event Action OnAuthenticationSuccess;

        #region Manejo de eventos de WelcomeAuthHandler

        private void OnAuthenticationSuccessHandler()
        {
            _userData.IsAuthenticated = true;
            _userData.Username = _authHandler.GetCurrentUsername();

            ShowMessage("Authentication successful!", false);
            OnAuthenticationSuccess?.Invoke();

            Debug.Log("Authentication successful - services ready");
            if (_dashboardController != null)
            {
                _dashboardController.ShowUi();
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

        #region IWelcomeOps Implementation - Delegando a WelcomeAuthHandler

        public async Task<bool> AuthenticateUserAsync(string username, string password)
        {
            if (!_isInitialized) Initialize();
            if (_authHandler == null)
            {
                Debug.LogError("Authentication handler not initialized");
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
                // Delegar al AuthHandler que maneja todo internamente
                var success = await _authHandler.AuthenticateUserAsync(username, password);

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

        public async Task<bool> RegisterUserAsync(string username, string password, string email,
            string phoneNumber = null)
        {
            if (!_isInitialized) Initialize();
            if (_authHandler == null)
            {
                Debug.LogError("Authentication handler not initialized");
                ShowMessage("Authentication system not ready", true);
                return false;
            }

            // Validaciones
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(email))
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

                // Delegar al AuthHandler que maneja todo internamente
                var success = await _authHandler.RegisterUserAsync(username, password, email, phoneNumber);

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
            if (_authHandler == null)
            {
                Debug.LogError("Authentication handler not initialized");
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

                // Delegar al AuthHandler que maneja todo internamente
                var success = await _authHandler.VerifyEmailAsync(confirmationCode);

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
            if (_authHandler == null)
            {
                Debug.LogError("Authentication handler not initialized");
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
                // Delegar al AuthHandler que maneja todo internamente
                var success = await _authHandler.RecoverPasswordAsync(username);

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
            if (_authHandler == null)
            {
                Debug.LogError("Authentication handler not initialized");
                ShowMessage("Authentication system not ready", true);
                return false;
            }

            try
            {
                SetResendCodeButtonEnabled(false);
                ShowMessage("Resending verification code...", false);

                // Delegar al AuthHandler que maneja todo internamente
                var success = await _authHandler.ResendVerificationCodeAsync();

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
                    // Apply configuration to AuthHandler
                    ApplyCognitoConfiguration();
                    
                    ShowMessage("Configuration saved successfully!", false);
                    print($"Cognito configuration saved to: {CognitoSettingsManager.GetConfigurationPath()}");
                    
                    // Close settings panel after successful save
                    await System.Threading.Tasks.Task.Delay(1500); // Show success message briefly
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
                print("Cognito settings ScriptableObject is null");
            }
        }
        catch (System.Exception ex)
        {
            print($"Error saving configuration: {ex.Message}");
            ShowMessage("Failed to save configuration", true);
        }
        finally
        {
            SetSaveButtonEnabled(true);
        }
    }
}

private RegionEndpoint ConvertToRegionEndpoint(string displayName)
{
    if (string.IsNullOrEmpty(displayName)) return null;

    // Extract region code from display name (e.g., "us-east-1 (N. Virginia)" -> "us-east-1")
    var regionCode = displayName.Split(' ')[0];
    
    return regionCode switch
    {
        "us-east-1" => RegionEndpoint.USEast1,
        "us-east-2" => RegionEndpoint.USEast2,
        "us-west-1" => RegionEndpoint.USWest1,
        "us-west-2" => RegionEndpoint.USWest2,
        "eu-west-1" => RegionEndpoint.EUWest1,
        "eu-central-1" => RegionEndpoint.EUCentral1,
        "ap-southeast-1" => RegionEndpoint.APSoutheast1,
        "ap-northeast-1" => RegionEndpoint.APNortheast1,
        _ => RegionEndpoint.USEast1 // Default fallback
    };
}

private string ConvertDisplayToRegionCode(string displayName)
{
    if (string.IsNullOrEmpty(displayName)) return "us-east-1";

    // Extract region code from display name (e.g., "us-east-1 (N. Virginia)" -> "us-east-1")
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
            // Load from ScriptableObject (which has been loaded from JSON in Initialize)
            userPoolIdField.value = _cognitoSettings.UserPoolId;
            clientIdField.value = _cognitoSettings.ClientId;
            identityPoolIdField.value = _cognitoSettings.IdentityPoolId;

            // Set dropdown value
            var regionDisplay = ConvertRegionCodeToDisplay(_cognitoSettings.AwsRegionCode);
            if (regionDropdown.choices.Contains(regionDisplay))
            {
                regionDropdown.value = regionDisplay;
            }

            print("Configuration loaded into Settings panel from ScriptableObject");
        }
        else
        {
            print("Cognito settings ScriptableObject is null");
            
            // Clear all fields as fallback
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
        _ => "us-east-1 (N. Virginia)" // Default
    };
}

#endregion

        #endregion

        #region Public Helper Methods

        /// <summary>
        /// Obtiene información del usuario autenticado
        /// </summary>
        public (string username, string userGroup, bool isAuthenticated) GetUserInfo()
        {
            return _authHandler?.GetUserInfo() ?? (string.Empty, string.Empty, false);
        }

        /// <summary>
        /// Cierra sesión del usuario actual
        /// </summary>
        public void Logout()
        {
            _authHandler?.Logout();
            _userData = new WelcomeInfo.UserData(); // Reset user data
            // Aquí podrías agregar lógica adicional como limpiar UI, navegar al panel de login, etc.
        }

        /// <summary>
        /// Verifica si el usuario pertenece a un grupo específico
        /// </summary>
        public bool IsUserInGroup(string groupName)
        {
            return _authHandler?.IsUserInSpecificGroup(groupName) ?? false;
        }

        #endregion

        protected void Initialize()
{
    try
    {
        if (_isInitialized)
        {
            Debug.Log("WelcomeOrchestrator already initialized");
            return;
        }

        Debug.Log("Initializing WelcomeOrchestrator...");

        // Initialize Cognito Settings
        InitializeCognitoSettings();

        // Crear el AuthHandler y suscribirse a sus eventos
        _authHandler = gameObject.AddComponent<WelcomeAuthHandler>();
        
        // Apply loaded configuration to AuthHandler
        ApplyCognitoConfiguration();
        
        _authHandler.OnAuthenticationSuccess += OnAuthenticationSuccessHandler;
        _authHandler.OnAuthenticationFailure += OnAuthenticationFailureHandler;
        _authHandler.OnRegistrationSuccess += OnRegistrationSuccessHandler;
        _authHandler.OnRegistrationFailure += OnRegistrationFailureHandler;
        _authHandler.OnEmailVerificationSuccess += OnEmailVerificationSuccessHandler;
        _authHandler.OnEmailVerificationFailure += OnEmailVerificationFailureHandler;
        _authHandler.OnPasswordRecoverySuccess += OnPasswordRecoverySuccessHandler;
        _authHandler.OnPasswordRecoveryFailure += OnPasswordRecoveryFailureHandler;
        _authHandler.OnResendVerificationSuccess += OnResendVerificationSuccessHandler;
        _authHandler.OnResendVerificationFailure += OnResendVerificationFailureHandler;

        // Obtener componentes UI
        _uiDocument = GetComponent<UIDocument>();
        var root = _uiDocument.rootVisualElement;
        _subpanelsAndSmokeMaskContainer = root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");

        // Inicializar managers
        _uiManager = new WelcomeUIManager(_uiConfig);
        _eventManager = new WelcomeEventManager(_uiManager, OnExitApplication, OnPanelTransitionCompleteHandler,
            this);
        _eventManager.RegisterEvents(_uiDocument);

        GetUiComponents(root);
        FindDependencies();
        StartCoroutine(GlitchEffectRoutine());
        _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;

        _isInitialized = true;
        Debug.Log("WelcomeOrchestrator initialized successfully");
    }
    catch (Exception ex)
    {
        Debug.LogError($"Initialization error: {ex.Message}");
    }
}
        private void InitializeCognitoSettings()
        {
            try
            {
                // Try to load from Inspector first
                if (_cognitoSettings == null)
                {
                    // Try to load default ScriptableObject from Resources
                    _cognitoSettings = Resources.Load<SettingsCognitoParametersData>(DEFAULT_COGNITO_SETTINGS_PATH);
            
                    if (_cognitoSettings == null)
                    {
                        // Create a runtime instance if none found
                        _cognitoSettings = ScriptableObject.CreateInstance<SettingsCognitoParametersData>();
                        print("Created runtime Cognito settings instance");
                    }
                    else
                    {
                        print("Loaded default Cognito settings from Resources");
                    }
                }
        
                // Try to load saved configuration from JSON
                bool configLoaded = CognitoSettingsManager.LoadConfiguration(_cognitoSettings);
        
                if (configLoaded)
                {
                    print("Cognito configuration loaded from JSON file");
                }
                else
                {
                    print("Using default Cognito configuration");
                }
        
                // Validate the configuration
                _cognitoSettings.ValidateConfiguration();
                print($"Cognito configuration valid: {_cognitoSettings.IsConfigurationValid}");
            }
            catch (Exception ex)
            {
                print($"Error initializing Cognito settings: {ex.Message}");
        
                // Fallback: create empty runtime instance
                _cognitoSettings = ScriptableObject.CreateInstance<SettingsCognitoParametersData>();
            }
        }

private void ApplyCognitoConfiguration()
{
    if (_authHandler != null && _cognitoSettings != null)
    {
        try
        {
            var regionEndpoint = _cognitoSettings.GetRegionEndpoint();
            _authHandler.UpdateCognitoConfiguration(
                _cognitoSettings.UserPoolId,
                _cognitoSettings.ClientId,
                _cognitoSettings.IdentityPoolId,
                regionEndpoint
            );
            
            print("Cognito configuration applied to AuthHandler");
        }
        catch (Exception ex)
        {
            print($"Error applying Cognito configuration: {ex.Message}");
        }
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
            Debug.Log(
                $"RecoverPasswordPanel found: {_uiConfig.Panels[IWelcomeOps.PanelType.RecoverPassword].Panel != null}");

            _uiConfig.Panels[IWelcomeOps.PanelType.RecoverPassword].Panel
                .RegisterCallback<TransitionEndEvent>(OnTransitionEndEvent);

            _uiConfig.Panels[IWelcomeOps.PanelType.EmailVerification] = new WelcomeInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer.Q<VisualElement>("EmailVerificationPanel"),
                ShowClass = "EmailVerificationPanelInMainScreen",
                HideClass = "EmailVerificationPanelOutMainScreen"
            };
            Debug.Log(
                $"EmailVerificationPanel found: {_uiConfig.Panels[IWelcomeOps.PanelType.EmailVerification].Panel != null}");

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
                regionDropdown.value = "us-east-1 (N. Virginia)"; // Default selection
        
                print("AWS Region dropdown initialized with available regions");
            }
            else
            {
                print("AWS Region dropdown not found in UI");
            }
        }

        private void FindDependencies()
        {
            _dashboardController = FindComponentByTag<DashboardController>("Dashboard");
        }

        private T FindComponentByTag<T>(string tag) where T : Component
        {
            var gameObject = GameObject.FindGameObjectWithTag(tag);
            if (gameObject == null)
            {
                Debug.LogError($"GameObject with tag '{tag}' not found");
                return null;
            }

            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"Component '{typeof(T).Name}' not found on GameObject with tag '{tag}'");
            }

            return component;
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
    }
}