using _Scripts.Controller;

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
        private VisualElement _subpanelsAndSmokeMaskContainer; // Nueva variable
        private bool _isInitialized = false;

        // Eventos
        public event Action OnAuthenticationSuccess;

        public async Task<bool> AuthenticateUserAsync(string username, string password)
        {
            if (!_isInitialized) Initialize();
            if (_authHandler == null)
            {
                Debug.LogError("Authentication handler not initialized");
                return false;
            }

            _userData.Username = username;
            var success = await _authHandler.AuthenticateUserAsync(username, password);
            return success;
        }

        public async Task<bool> RegisterUserAsync(string username, string password, string email, string phoneNumber = null)
        {
            if (!_isInitialized) Initialize();
            if (_authHandler == null)
            {
                Debug.LogError("Authentication handler not initialized");
                return false;
            }

            _userData.Username = username;
            _userData.Email = email;
            _userData.PhoneNumber = phoneNumber;
            var success = await _authHandler.RegisterUserAsync(username, password, email, phoneNumber);
            return success;
        }

        public async Task<bool> VerifyEmailAsync(string confirmationCode)
        {
            if (!_isInitialized) Initialize();
            if (_authHandler == null)
            {
                Debug.LogError("Authentication handler not initialized");
                return false;
            }

            var success = await _authHandler.VerifyEmailAsync(confirmationCode);
            if (success) _userData.IsAuthenticated = true;
            return success;
        }

        public async Task<bool> RecoverPasswordAsync(string username)
        {
            if (!_isInitialized) Initialize();
            if (_authHandler == null)
            {
                Debug.LogError("Authentication handler not initialized");
                return false;
            }

            _userData.Username = username;
            var success = await _authHandler.RecoverPasswordAsync(username);
            return success;
        }

        public async Task<bool> ResendVerificationCodeAsync()
        {
            if (!_isInitialized) Initialize();
            if (_authHandler == null)
            {
                Debug.LogError("Authentication handler not initialized");
                return false;
            }

            var success = await _authHandler.ResendVerificationCodeAsync();
            return success;
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
        }

        private void Initialize()
        {
            try
            {
                if (_isInitialized)
                {
                    Debug.Log("WelcomeOrchestrator already initialized");
                    return;
                }

                Debug.Log("Initializing WelcomeOrchestrator...");
                _authHandler = new WelcomeAuthHandler();
                _authHandler.OnAuthenticationSuccess += OnAuthenticationSuccessHandler;
                var root = GetComponent<UIDocument>().rootVisualElement;
                _subpanelsAndSmokeMaskContainer = root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
                _uiManager = new WelcomeUIManager(_uiConfig);
                _eventManager = new WelcomeEventManager(_uiManager, OnExitApplication, OnPanelTransitionComplete);
                _eventManager.RegisterEvents(GetComponent<UIDocument>());
                GetUiComponents(root); // Asegurar que se llame después de asignar _subpanelsAndSmokeMaskContainer
                FindDependencies();
                StartCoroutine(GlitchEffectRoutine());
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None; // Inicialización invisible

                _isInitialized = true;
                Debug.Log("WelcomeOrchestrator initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Initialization error: {ex.Message}");
            }
        }

        private void GetUiComponents(VisualElement root)
        {
            _uiConfig.Body = root.Q<VisualElement>("Body");
            _uiConfig.SubpanelsContainer = _subpanelsAndSmokeMaskContainer; // Asignar el contenedor ya encontrado
            _uiConfig.Scrim = _subpanelsAndSmokeMaskContainer.Q<VisualElement>("Scrim");

            var panelsContainer = _subpanelsAndSmokeMaskContainer;
            _uiConfig.Panels[IWelcomeOps.PanelType.Login] = new WelcomeInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer.Q<VisualElement>("LoginPanel"),
                ShowClass = "LoginPanelMoveB",
                HideClass = "LoginPanelMoveA"
            };
            Debug.Log($"LoginPanel found: {_uiConfig.Panels[IWelcomeOps.PanelType.Login].Panel != null}");

            _uiConfig.Panels[IWelcomeOps.PanelType.Register] = new WelcomeInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer.Q<VisualElement>("RegisterPanel"),
                ShowClass = "RegisterPanelInMainScreen",
                HideClass = "RegisterPanelOutMainScreen"
            };
            Debug.Log($"RegisterPanel found: {_uiConfig.Panels[IWelcomeOps.PanelType.Register].Panel != null}");

            _uiConfig.Panels[IWelcomeOps.PanelType.RecoverPassword] = new WelcomeInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer.Q<VisualElement>("RecoverPasswordPanel"),
                ShowClass = "RecoverPasswordPanelInMainScreen",
                HideClass = "RecoverPasswordPanelOutMainScreen"
            };
            Debug.Log($"RecoverPasswordPanel found: {_uiConfig.Panels[IWelcomeOps.PanelType.RecoverPassword].Panel != null}");

            _uiConfig.Panels[IWelcomeOps.PanelType.EmailVerification] = new WelcomeInfo.UIConfiguration.PanelData
            {
                Panel = panelsContainer.Q<VisualElement>("EmailVerificationPanel"),
                ShowClass = "EmailVerificationPanelInMainScreen",
                HideClass = "EmailVerificationPanelOutMainScreen"
            };
            Debug.Log($"EmailVerificationPanel found: {_uiConfig.Panels[IWelcomeOps.PanelType.EmailVerification].Panel != null}");
        }

        private void OnAuthenticationSuccessHandler()
        {
            _userData.IsAuthenticated = true;
            OnAuthenticationSuccess?.Invoke();
            Debug.Log("Authentication successful - services ready");
            if (_dashboardController != null) _dashboardController.ShowUi();
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

        private void OnPanelTransitionComplete(IWelcomeOps.PanelType panelType)
        {
            if (!_uiManager.IsAnyPanelVisible())
            {
                _uiManager.HideUi();
            }
        }

        private IEnumerator GlitchEffectRoutine()
        {
            yield return new WaitForSeconds(0.5f); // Breve retraso para inicialización UI
            var validPanels = new[] { IWelcomeOps.PanelType.Login, IWelcomeOps.PanelType.Register, IWelcomeOps.PanelType.RecoverPassword, IWelcomeOps.PanelType.EmailVerification };
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
                yield return new WaitForSeconds(3f); // Intervalo entre ciclos
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