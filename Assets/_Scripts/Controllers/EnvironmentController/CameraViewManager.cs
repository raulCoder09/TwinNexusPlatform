using System;
using System.Collections.Generic;
using _Scripts.Controllers.ArScaraControlPanelController;
using UnityEngine;

namespace _Scripts.Controllers.EnvironmentController
{
    /// <summary>
    /// Maneja las vistas de cámara para diferentes ambientes
    /// Integra el sistema de vistas del código anterior con el nuevo framework
    /// </summary>
    public class CameraViewManager : MonoBehaviour
    {
        #region Configuration

        [Header("Camera References")] [SerializeField]
        private Camera _virtualEnvironmentCamera;

        [SerializeField] private Camera _mainCamera;

        [Header("Debug")] [SerializeField] private bool _enableDebugLogs = true;

        #endregion

        #region Private Fields

        private Dictionary<string, ViewConfiguration> _viewConfigurations = new Dictionary<string, ViewConfiguration>();
        private string _currentView = "Default";
        private EnvironmentType _currentEnvironment = EnvironmentType.None;
        private bool _isInitialized = false;

        #endregion

        #region View Configuration Class

        [System.Serializable]
        public class ViewConfiguration
        {
            public string viewName;
            public Vector3 position;
            public Vector3 rotation;
            public float fieldOfView = 60f;
            public bool isAvailableInVirtual = true;
            public bool isAvailableInAR = false;
            public bool isAvailableInHybrid = false;
            public bool isAvailableInReal = false;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeViewConfigurations();
        }

        private void Start()
        {
            SubscribeToEnvironmentEvents();
            FindCameraReferences();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEnvironmentEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Inicializa las configuraciones de vistas basadas en el sistema anterior
        /// </summary>
        private void InitializeViewConfigurations()
        {
            _viewConfigurations.Clear();

            // Configuraciones basadas en tu sistema anterior
            _viewConfigurations["Default"] = new ViewConfiguration
            {
                viewName = "Default",
                position = new Vector3(0f, 0.325f, 0f),
                rotation = new Vector3(66f, 0f, 0f),
                fieldOfView = 60f,
                isAvailableInVirtual = true,
                isAvailableInAR = true,
                isAvailableInHybrid = true,
                isAvailableInReal = true
            };

            _viewConfigurations["Top"] = new ViewConfiguration
            {
                viewName = "Top",
                position = new Vector3(0f, 0.35f, 0.13f),
                rotation = new Vector3(90f, 0f, 0f),
                fieldOfView = 60f,
                isAvailableInVirtual = true,
                isAvailableInAR = false,
                isAvailableInHybrid = false,
                isAvailableInReal = false
            };

            _viewConfigurations["Front"] = new ViewConfiguration
            {
                viewName = "Front",
                position = new Vector3(0f, 0.03f, 0.5f),
                rotation = new Vector3(-10f, 180f, 0f),
                fieldOfView = 60f,
                isAvailableInVirtual = true
            };

            _viewConfigurations["Back"] = new ViewConfiguration
            {
                viewName = "Back",
                position = new Vector3(0f, 0.06f, -0.25f),
                rotation = new Vector3(-10f, 0f, 0f),
                fieldOfView = 60f,
                isAvailableInVirtual = true
            };

            _viewConfigurations["Left"] = new ViewConfiguration
            {
                viewName = "Left",
                position = new Vector3(-0.35f, 0.03f, 0.12f),
                rotation = new Vector3(-10f, 90f, 0f),
                fieldOfView = 60f,
                isAvailableInVirtual = true
            };

            _viewConfigurations["Right"] = new ViewConfiguration
            {
                viewName = "Right",
                position = new Vector3(0.35f, 0.03f, 0.12f),
                rotation = new Vector3(-10f, 270f, 0f),
                fieldOfView = 60f,
                isAvailableInVirtual = true
            };

            _isInitialized = true;
            LogDebug($"Camera view configurations initialized: {_viewConfigurations.Count} views");
        }

        private void FindCameraReferences()
        {
            if (_virtualEnvironmentCamera == null)
            {
                var cameraGO = GameObject.FindGameObjectWithTag("VirtualEnvironmentCamera");
                if (cameraGO != null)
                {
                    _virtualEnvironmentCamera = cameraGO.GetComponent<Camera>();
                    LogDebug("Virtual environment camera found by tag");
                }
                // AGREGAR ELSE PARA DEBUG:
                else
                {
                    LogDebug("VirtualEnvironmentCamera not found - will search again later");
                }
            }
        }

        /// <summary>
        /// Se suscribe a eventos del Environment Manager
        /// </summary>
        private void SubscribeToEnvironmentEvents()
        {
            var environmentManager = EnvironmentManager.Instance;
            if (environmentManager != null)
            {
                environmentManager.OnEnvironmentLoaded += OnEnvironmentChanged;
                Debug.LogError("CameraViewManager subscribed to environment events"); // Temporal
            }
            else
            {
                Debug.LogError("CameraViewManager: EnvironmentManager not found!"); // Temporal
            }
        }

        /// <summary>
        /// Se desuscribe de eventos
        /// </summary>
        private void UnsubscribeFromEnvironmentEvents()
        {
            var environmentManager = EnvironmentManager.Instance;
            if (environmentManager != null)
            {
                environmentManager.OnEnvironmentLoaded -= OnEnvironmentChanged;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Cambia a una vista específica
        /// </summary>
        public bool ChangeView(string viewName)
        {
            if (!_isInitialized)
            {
                LogError("CameraViewManager not initialized");
                return false;
            }

            if (!_viewConfigurations.TryGetValue(viewName, out var viewConfig))
            {
                LogWarning($"View configuration '{viewName}' not found");
                return false;
            }

            // Verificar si la vista está disponible en el ambiente actual
            if (!IsViewAvailableInCurrentEnvironment(viewConfig))
            {
                LogWarning($"View '{viewName}' not available in current environment: {_currentEnvironment}");
                return false;
            }

            ApplyViewConfiguration(viewConfig);
            _currentView = viewName;

            LogDebug($"Changed to view: {viewName}");
            return true;
        }

        /// <summary>
        /// Obtiene lista de vistas disponibles para el ambiente actual
        /// </summary>
        public List<string> GetAvailableViews()
        {
            var availableViews = new List<string>();
    
            Debug.LogError($"GetAvailableViews called - configs: {_viewConfigurations.Count}, currentEnv: {_currentEnvironment}");

            foreach (var kvp in _viewConfigurations)
            {
                if (IsViewAvailableInCurrentEnvironment(kvp.Value))
                {
                    availableViews.Add(kvp.Key);
                }
            }
    
            Debug.LogError($"Available views found: {availableViews.Count}");
            return availableViews;
        }

        /// <summary>
        /// Obtiene la vista actual
        /// </summary>
        public string GetCurrentView()
        {
            return _currentView;
        }

        /// <summary>
        /// Verifica si una vista está disponible
        /// </summary>
        public bool IsViewAvailable(string viewName)
        {
            if (!_viewConfigurations.TryGetValue(viewName, out var viewConfig))
                return false;

            return IsViewAvailableInCurrentEnvironment(viewConfig);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Verifica si una vista está disponible en el ambiente actual
        /// </summary>
        private bool IsViewAvailableInCurrentEnvironment(ViewConfiguration viewConfig)
        {
            return _currentEnvironment switch
            {
                EnvironmentType.Virtual => viewConfig.isAvailableInVirtual,
                EnvironmentType.AugmentedReality => viewConfig.isAvailableInAR,
                EnvironmentType.Hybrid => viewConfig.isAvailableInHybrid,
                EnvironmentType.RealDevice => viewConfig.isAvailableInReal,
                _ => false
            };
        }

        /// <summary>
        /// Aplica la configuración de vista a la cámara apropiada
        /// </summary>
        private void ApplyViewConfiguration(ViewConfiguration viewConfig)
        {
            Camera targetCamera = GetTargetCamera();

            if (targetCamera == null)
            {
                LogError("No target camera available");
                return;
            }

            // Aplicar transformación
            targetCamera.transform.position = viewConfig.position;
            targetCamera.transform.eulerAngles = viewConfig.rotation;
            targetCamera.fieldOfView = viewConfig.fieldOfView;

            LogDebug(
                $"Applied view '{viewConfig.viewName}' to camera: pos={viewConfig.position}, rot={viewConfig.rotation}");
        }

        /// <summary>
        /// Obtiene la cámara objetivo basada en el ambiente actual
        /// </summary>
        private Camera GetTargetCamera()
        {
            return _currentEnvironment switch
            {
                EnvironmentType.Virtual => _virtualEnvironmentCamera,
                EnvironmentType.AugmentedReality => _mainCamera,
                EnvironmentType.Hybrid => _mainCamera,
                EnvironmentType.RealDevice => _mainCamera,
                _ => _mainCamera
            };
        }

        /// <summary>
        /// Maneja cambios de ambiente
        /// </summary>
        private void OnEnvironmentChanged(EnvironmentType environmentType)
        {
            Debug.LogError($"CameraViewManager received environment change: {environmentType}"); // Temporal
    
            _currentEnvironment = environmentType;
            LogDebug($"Environment changed to: {environmentType}");

            // Buscar cámara nuevamente cuando cambie a Virtual
            if (environmentType == EnvironmentType.Virtual)
            {
                FindCameraReferences();
            }
            
            if (!IsViewAvailable(_currentView))
            {
                ChangeView("Default");
                LogDebug("Changed to default view due to environment compatibility");
            }
            
            var orchestrator = FindObjectOfType<ArScaraControlPanelOrchestrator>();
            if (orchestrator != null && orchestrator.IsActive)
            {
                orchestrator.UpdateViewsDropdownForEnvironment(environmentType);
            }
        }
        
        
        
        
        

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[CameraViewManager] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[CameraViewManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[CameraViewManager] {message}");
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Ambiente actual
        /// </summary>
        public EnvironmentType CurrentEnvironment => _currentEnvironment;

        /// <summary>
        /// Cámara virtual de ambiente
        /// </summary>
        public Camera VirtualEnvironmentCamera
        {
            get => _virtualEnvironmentCamera;
            set => _virtualEnvironmentCamera = value;
        }

        #endregion
    }
}