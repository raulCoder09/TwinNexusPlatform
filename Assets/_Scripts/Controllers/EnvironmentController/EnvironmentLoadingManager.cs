using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.EnvironmentController
{
    /// <summary>
    /// Maneja la UI de loading para transiciones de ambientes
    /// Muestra popups y progreso durante la carga de ambientes
    /// </summary>
    public class EnvironmentLoadingManager : MonoBehaviour
    {
        #region Configuration

        [Header("Loading UI Configuration")]
        [SerializeField] private UIDocument _loadingUIDocument;
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private float _minimumLoadingTime = 1f;
        
        [Header("Loading Animation")]
        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.3f;

        #endregion

        #region Private Fields

        private EnvironmentManager _environmentManager;
        private bool _isInitialized = false;
        private bool _isLoadingUIVisible = false;
        
        // UI Elements
        private VisualElement _loadingContainer;
        private VisualElement _loadingPanel;
        private Label _loadingTitleLabel;
        private Label _loadingMessageLabel;
        private VisualElement _loadingSpinner;
        private VisualElement _loadingProgressBar;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            // Buscar dependencias
            FindDependencies();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Inicializa el Loading Manager
        /// </summary>
        private void Initialize()
        {
            try
            {
                LogDebug("Initializing EnvironmentLoadingManager...");

                // Obtener UI elements si el documento está asignado
                if (_loadingUIDocument != null)
                {
                    GetUIElements();
                }

                _isInitialized = true;
                LogDebug("EnvironmentLoadingManager initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize EnvironmentLoadingManager: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene referencias a elementos UI
        /// </summary>
        private void GetUIElements()
        {
            var root = _loadingUIDocument.rootVisualElement;
            
            _loadingContainer = root.Q<VisualElement>("LoadingContainer");
            _loadingPanel = root.Q<VisualElement>("LoadingPanel");
            _loadingTitleLabel = root.Q<Label>("LoadingTitle");
            _loadingMessageLabel = root.Q<Label>("LoadingMessage");
            _loadingSpinner = root.Q<VisualElement>("LoadingSpinner");
            _loadingProgressBar = root.Q<VisualElement>("LoadingProgressBar");

            // Inicialmente oculto
            if (_loadingContainer != null)
            {
                _loadingContainer.style.display = DisplayStyle.None;
            }

            LogDebug("Loading UI elements obtained");
        }

        /// <summary>
        /// Busca dependencias en la escena
        /// </summary>
        private void FindDependencies()
        {
            // Buscar EnvironmentManager
            _environmentManager = EnvironmentManager.Instance;
            if (_environmentManager != null)
            {
                SubscribeToEnvironmentEvents();
                LogDebug("EnvironmentManager dependency found and subscribed");
            }
            else
            {
                LogWarning("EnvironmentManager not found");
            }
        }

        /// <summary>
        /// Se suscribe a eventos del EnvironmentManager
        /// </summary>
        private void SubscribeToEnvironmentEvents()
        {
            _environmentManager.OnEnvironmentLoadProgress += OnEnvironmentLoadProgress;
            _environmentManager.OnEnvironmentLoaded += OnEnvironmentLoaded;
            _environmentManager.OnEnvironmentError += OnEnvironmentError;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Inicia la carga de un ambiente con loading UI
        /// </summary>
        public async Task<bool> LoadEnvironmentWithUI(EnvironmentType environmentType, string customMessage = null)
        {
            try
            {
                if (!_isInitialized)
                {
                    LogError("EnvironmentLoadingManager not initialized");
                    return false;
                }

                if (_environmentManager == null)
                {
                    LogError("EnvironmentManager not available");
                    return false;
                }

                // Obtener configuración del ambiente
                var config = _environmentManager.GetEnvironmentConfig(environmentType);
                var loadingMessage = customMessage ?? config?.loadingMessage ?? $"Loading {environmentType} Environment...";

                LogDebug($"Starting environment load with UI: {environmentType}");

                // Mostrar loading UI
                await ShowLoadingUI("Loading Environment", loadingMessage);

                // Tracking analytics
                await UIAnalyticsManager.Instance?.TrackButtonClick(
                    buttonName: "EnvironmentSwitch",
                    context: "environment_loading",
                    additionalData: new System.Collections.Generic.Dictionary<string, object> 
                    { 
                        ["environmentType"] = environmentType.ToString(),
                        ["loadingMessage"] = loadingMessage
                    }
                );

                // Cargar ambiente
                var startTime = DateTime.Now;
                var result = await _environmentManager.LoadEnvironment(environmentType);

                // Asegurar tiempo mínimo de loading para UX
                var elapsedTime = (DateTime.Now - startTime).TotalSeconds;
                if (elapsedTime < _minimumLoadingTime)
                {
                    var remainingTime = _minimumLoadingTime - elapsedTime;
                    await Task.Delay(Mathf.RoundToInt((float)remainingTime * 1000));
                }

                // La UI se ocultará automáticamente en OnEnvironmentLoaded
                return result;
            }
            catch (Exception ex)
            {
                LogError($"Error in LoadEnvironmentWithUI: {ex.Message}");
                await HideLoadingUI();
                return false;
            }
        }

        /// <summary>
        /// Maneja cambio de ambiente desde dropdown
        /// </summary>
        public async void HandleEnvironmentDropdownChange(string environmentName)
        {
            try
            {
                LogDebug($"RULOMORALES HandleEnvironmentDropdownChange called with: {environmentName}");
                var environmentType = ParseEnvironmentName(environmentName);
                LogDebug($"Parsed environment type: {environmentType}");
                if (environmentType == EnvironmentType.None)
                {
                    LogDebug("Environment type is None (Menu environment)");
                }
                await LoadEnvironmentWithUI(environmentType);
            }
            catch (Exception ex)
            {
                LogError($"Error handling dropdown change: {ex.Message}");
            }
        }

        #endregion

        #region Loading UI Management

        /// <summary>
        /// Muestra la UI de loading
        /// </summary>
        private async Task ShowLoadingUI(string title, string message)
        {
            try
            {
                if (_loadingContainer == null)
                {
                    LogWarning("Loading UI not configured - skipping UI display");
                    return;
                }

                if (_isLoadingUIVisible)
                {
                    LogDebug("Loading UI already visible");
                    return;
                }

                LogDebug($"Showing loading UI: {title} - {message}");

                // Actualizar textos
                if (_loadingTitleLabel != null)
                    _loadingTitleLabel.text = title;
                    
                if (_loadingMessageLabel != null)
                    _loadingMessageLabel.text = message;

                // Mostrar contenedor
                _loadingContainer.style.display = DisplayStyle.Flex;

                // Animación de fade in (opcional - depende de CSS)
                _loadingContainer.style.opacity = 0;
                _loadingContainer.style.opacity = 1;

                _isLoadingUIVisible = true;

                // Pequeño delay para que se vea la animación
                await Task.Delay(Mathf.RoundToInt(_fadeInDuration * 1000));
            }
            catch (Exception ex)
            {
                LogError($"Error showing loading UI: {ex.Message}");
            }
        }

        /// <summary>
        /// Oculta la UI de loading
        /// </summary>
        private async Task HideLoadingUI()
        {
            try
            {
                if (_loadingContainer == null || !_isLoadingUIVisible)
                {
                    return;
                }

                LogDebug("Hiding loading UI");

                // Animación de fade out (opcional - depende de CSS)
                _loadingContainer.style.opacity = 0;

                // Esperar animación
                await Task.Delay(Mathf.RoundToInt(_fadeOutDuration * 1000));

                // Ocultar contenedor
                _loadingContainer.style.display = DisplayStyle.None;

                _isLoadingUIVisible = false;
            }
            catch (Exception ex)
            {
                LogError($"Error hiding loading UI: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Maneja progreso de carga de ambiente
        /// </summary>
        private void OnEnvironmentLoadProgress(EnvironmentType environmentType, float progress)
        {
            try
            {
                LogDebug($"Environment load progress: {environmentType} - {progress:P}");
                
                // Actualizar barra de progreso si existe
                if (_loadingProgressBar != null)
                {
                    _loadingProgressBar.style.width = Length.Percent(progress * 100);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error updating load progress: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja finalización de carga de ambiente
        /// </summary>
        private async void OnEnvironmentLoaded(EnvironmentType environmentType)
        {
            try
            {
                LogDebug($"Environment loaded: {environmentType}");
                await HideLoadingUI();
            }
            catch (Exception ex)
            {
                LogError($"Error handling environment loaded: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja errores de ambiente
        /// </summary>
        private async void OnEnvironmentError(string error)
        {
            try
            {
                LogError($"Environment error: {error}");
                await HideLoadingUI();
                
                // TODO: Mostrar mensaje de error al usuario
            }
            catch (Exception ex)
            {
                LogError($"Error handling environment error: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods
        
        /// <summary>
        /// Convierte nombre de ambiente a enum
        /// </summary>
        private EnvironmentType ParseEnvironmentName(string environmentName)
        {
            // Limpiar espacios extra para mayor tolerancia
            var cleanName = environmentName?.Trim().Replace("  ", " ");
    
            return cleanName switch
            {
                "Virtual environment" => EnvironmentType.Virtual,
                "Augmented reality environment" => EnvironmentType.AugmentedReality,
                "Hybrid environment" => EnvironmentType.Hybrid,
                "Real device environment" => EnvironmentType.RealDevice,
                "Menu environment" => EnvironmentType.None, // Agregar este caso
                _ => EnvironmentType.None
            };
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Limpia el Loading Manager
        /// </summary>
        private void Cleanup()
        {
            try
            {
                LogDebug("Cleaning up EnvironmentLoadingManager...");

                // Desuscribirse de eventos
                if (_environmentManager != null)
                {
                    _environmentManager.OnEnvironmentLoadProgress -= OnEnvironmentLoadProgress;
                    _environmentManager.OnEnvironmentLoaded -= OnEnvironmentLoaded;
                    _environmentManager.OnEnvironmentError -= OnEnvironmentError;
                }

                // Ocultar UI si está visible
                if (_isLoadingUIVisible && _loadingContainer != null)
                {
                    _loadingContainer.style.display = DisplayStyle.None;
                }

                _isInitialized = false;
                LogDebug("EnvironmentLoadingManager cleanup completed");
            }
            catch (Exception ex)
            {
                LogError($"Error during cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[EnvironmentLoadingManager] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[EnvironmentLoadingManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[EnvironmentLoadingManager] {message}");
        }

        #endregion
    }
}