using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using _Scripts.Controller;
using _Scripts.Controllers.ServiceManagement;

namespace _Scripts.Controllers.UiManagement
{
    /// <summary>
    /// Manager centralizado para el tracking de eventos UI via CloudWatch
    /// Captura interacciones del usuario para analytics y monitoreo
    /// </summary>
    public class UIAnalyticsManager : MonoBehaviour
    {
        #region Singleton Pattern

        private static UIAnalyticsManager _instance;
        private static readonly object _lock = new object();

        public static UIAnalyticsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindObjectOfType<UIAnalyticsManager>();
                            
                            if (_instance == null)
                            {
                                var go = new GameObject("UIAnalyticsManager");
                                _instance = go.AddComponent<UIAnalyticsManager>();
                                DontDestroyOnLoad(go);
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Configuration

        [Header("UI Analytics Configuration")]
        [SerializeField] private bool _enableTracking = true;
        [SerializeField] private bool _enableDebugLogs = true;
        [SerializeField] private bool _trackAnonymousUsers = false;
        
        [Header("Event Categories")]
        [SerializeField] private bool _trackMenuEvents = true;
        [SerializeField] private bool _trackButtonClicks = true;
        [SerializeField] private bool _trackPanelTransitions = true;
        [SerializeField] private bool _trackFormInteractions = true;

        #endregion

        #region Private Fields

        private bool _isInitialized = false;
        private string _currentSessionId;
        private DateTime _sessionStartTime;
        
        // Cache para evitar logs duplicados
        private readonly Dictionary<string, DateTime> _lastEventTimes = new Dictionary<string, DateTime>();
        private readonly TimeSpan _duplicateEventThreshold = TimeSpan.FromMilliseconds(500);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton enforcement
            if (_instance != null && _instance != this)
            {
                LogDebug("Destroying duplicate UIAnalyticsManager instance");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                LogDebug("UIAnalyticsManager destroyed");
                _instance = null;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Inicializa el sistema de analytics UI
        /// </summary>
        private void Initialize()
        {
            try
            {
                if (_isInitialized) return;

                // Generar session ID único
                _currentSessionId = GenerateSessionId();
                _sessionStartTime = DateTime.UtcNow;

                LogDebug($"UIAnalyticsManager initialized - Session: {_currentSessionId}");
                
                // Enviar evento de inicio de sesión
                if (_enableTracking)
                {
                    _ = TrackSessionEvent("session_started");
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize UIAnalyticsManager: {ex.Message}");
            }
        }

        /// <summary>
        /// Genera un ID único para la sesión
        /// </summary>
        private string GenerateSessionId()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var random = UnityEngine.Random.Range(1000, 9999);
            return $"ui-session-{timestamp}-{random}";
        }

        #endregion

        #region Public Tracking Methods

        /// <summary>
        /// Trackea eventos del menú lateral de navegación
        /// </summary>
        public async Task TrackMenuEvent(string action, string menuItem = null, string context = null)
        {
            if (!ShouldTrackEvent("menu")) return;

            try
            {
                var eventData = new Dictionary<string, object>
                {
                    ["action"] = action,
                    ["menuItem"] = menuItem ?? "unknown",
                    ["context"] = context ?? GetCurrentUIContext(),
                    ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                await SendUIEvent("NavigationMenu", eventData);
                LogDebug($"Menu event tracked: {action} - {menuItem}");
            }
            catch (Exception ex)
            {
                LogError($"Error tracking menu event: {ex.Message}");
            }
        }

        /// <summary>
        /// Trackea clicks en botones generales
        /// </summary>
        public async Task TrackButtonClick(string buttonName, string context = null, Dictionary<string, object> additionalData = null)
        {
            if (!ShouldTrackEvent("button")) return;

            try
            {
                var eventData = new Dictionary<string, object>
                {
                    ["buttonName"] = buttonName,
                    ["context"] = context ?? GetCurrentUIContext(),
                    ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                // Agregar datos adicionales si se proporcionan
                if (additionalData != null)
                {
                    foreach (var kvp in additionalData)
                    {
                        eventData[$"extra_{kvp.Key}"] = kvp.Value;
                    }
                }

                await SendUIEvent("ButtonClick", eventData);
                LogDebug($"Button click tracked: {buttonName}");
            }
            catch (Exception ex)
            {
                LogError($"Error tracking button click: {ex.Message}");
            }
        }

        /// <summary>
        /// Trackea transiciones entre paneles/UIs
        /// </summary>
        public async Task TrackPanelTransition(string fromPanel, string toPanel, string transitionType = "navigation")
        {
            if (!ShouldTrackEvent("panel")) return;

            try
            {
                var eventData = new Dictionary<string, object>
                {
                    ["fromPanel"] = fromPanel ?? "none",
                    ["toPanel"] = toPanel ?? "none", 
                    ["transitionType"] = transitionType,
                    ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                await SendUIEvent("PanelTransition", eventData);
                LogDebug($"Panel transition tracked: {fromPanel} → {toPanel}");
            }
            catch (Exception ex)
            {
                LogError($"Error tracking panel transition: {ex.Message}");
            }
        }

        /// <summary>
        /// Trackea interacciones con formularios
        /// </summary>
        public async Task TrackFormInteraction(string formName, string fieldName, string action, object value = null)
        {
            if (!ShouldTrackEvent("form")) return;

            try
            {
                var eventData = new Dictionary<string, object>
                {
                    ["formName"] = formName,
                    ["fieldName"] = fieldName,
                    ["action"] = action, // "focus", "blur", "change", "submit", etc.
                    ["hasValue"] = value != null,
                    // NO almacenar el valor real por privacidad
                    ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                await SendUIEvent("FormInteraction", eventData);
                LogDebug($"Form interaction tracked: {formName}.{fieldName} - {action}");
            }
            catch (Exception ex)
            {
                LogError($"Error tracking form interaction: {ex.Message}");
            }
        }

        /// <summary>
        /// Trackea eventos de sesión (inicio, fin, etc.)
        /// </summary>
        public async Task TrackSessionEvent(string eventType, Dictionary<string, object> additionalData = null)
        {
            try
            {
                var eventData = new Dictionary<string, object>
                {
                    ["eventType"] = eventType,
                    ["sessionDuration"] = GetSessionDuration(),
                    ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                if (additionalData != null)
                {
                    foreach (var kvp in additionalData)
                    {
                        eventData[kvp.Key] = kvp.Value;
                    }
                }

                await SendUIEvent("SessionEvent", eventData);
                LogDebug($"Session event tracked: {eventType}");
            }
            catch (Exception ex)
            {
                LogError($"Error tracking session event: {ex.Message}");
            }
        }

        #endregion

        #region Core Analytics Logic

        /// <summary>
        /// Envía el evento UI a CloudWatch
        /// </summary>
        private async Task SendUIEvent(string eventType, Dictionary<string, object> eventData)
        {
            try
            {
                // Verificar que CloudWatch esté disponible
                var cloudWatchManager = ServiceController.Instance?.CloudWatchManager;
                if (cloudWatchManager == null)
                {
                    LogWarning("CloudWatch not available - UI event not sent");
                    return;
                }

                // Obtener información del usuario
                var userInfo = GetUserInfo();
                
                // Preparar datos completos del evento
                var completeEventData = PrepareEventData(eventType, eventData, userInfo);
                
                // Enviar a CloudWatch como log de monitoreo de app
                var success = await cloudWatchManager.SendAppMonitoringLog(
                    $"UIEvent_{eventType}",
                    System.Text.Json.JsonSerializer.Serialize(completeEventData),
                    userInfo.userId,
                    "INFO"
                );

                if (!success)
                {
                    LogWarning($"Failed to send UI event to CloudWatch: {eventType}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error sending UI event to CloudWatch: {ex.Message}");
            }
        }

        /// <summary>
        /// Prepara los datos completos del evento
        /// </summary>
        private Dictionary<string, object> PrepareEventData(string eventType, Dictionary<string, object> eventData, (string userId, string userGroup, bool isAuthenticated) userInfo)
        {
            var completeData = new Dictionary<string, object>
            {
                // Información del evento
                ["eventType"] = eventType,
                ["sessionId"] = _currentSessionId,
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                
                // Información del usuario
                ["userId"] = userInfo.userId,
                ["userGroup"] = userInfo.userGroup,
                ["isAuthenticated"] = userInfo.isAuthenticated,
                
                // Información del contexto UI
                ["currentController"] = GetCurrentUIContext(),
                ["sessionDuration"] = GetSessionDuration(),
                
                // Información del dispositivo (básica)
                ["screenResolution"] = $"{Screen.width}x{Screen.height}",
                ["platform"] = Application.platform.ToString(),
                ["unityVersion"] = Application.unityVersion
            };

            // Agregar datos específicos del evento
            foreach (var kvp in eventData)
            {
                completeData[kvp.Key] = kvp.Value;
            }

            return completeData;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Verifica si debe trackear un evento específico
        /// </summary>
        private bool ShouldTrackEvent(string eventCategory)
        {
            if (!_enableTracking || !_isInitialized) return false;

            // Verificar configuración por categoría
            return eventCategory switch
            {
                "menu" => _trackMenuEvents,
                "button" => _trackButtonClicks,
                "panel" => _trackPanelTransitions,
                "form" => _trackFormInteractions,
                _ => true
            };
        }

        /// <summary>
        /// Verifica duplicados de eventos
        /// </summary>
        private bool IsDuplicateEvent(string eventKey)
        {
            if (_lastEventTimes.TryGetValue(eventKey, out var lastTime))
            {
                var timeSinceLastEvent = DateTime.UtcNow - lastTime;
                if (timeSinceLastEvent < _duplicateEventThreshold)
                {
                    return true;
                }
            }

            _lastEventTimes[eventKey] = DateTime.UtcNow;
            return false;
        }

        /// <summary>
        /// Obtiene información del usuario actual
        /// </summary>
        private (string userId, string userGroup, bool isAuthenticated) GetUserInfo()
        {
            var userInfo = ServiceController.Instance?.GetUserInfo();
            if (userInfo.HasValue && userInfo.Value.isAuthenticated)
            {
                return (userInfo.Value.username, userInfo.Value.userGroup, true);
            }

            return (_trackAnonymousUsers ? "anonymous" : "unknown", "none", false);
        }

        /// <summary>
        /// Obtiene el contexto UI actual
        /// </summary>
        private string GetCurrentUIContext()
        {
            // Intentar obtener el controlador activo desde UIController
            var uiController = UIController.Instance;
            var activeController = uiController?.GetActiveController();
            
            return activeController?.ControllerName ?? "unknown";
        }

        /// <summary>
        /// Obtiene la duración de la sesión actual en segundos
        /// </summary>
        private double GetSessionDuration()
        {
            return (DateTime.UtcNow - _sessionStartTime).TotalSeconds;
        }

        #endregion

        #region Public Configuration Methods

        /// <summary>
        /// Habilita/deshabilita el tracking
        /// </summary>
        public void SetTrackingEnabled(bool enabled)
        {
            _enableTracking = enabled;
            LogDebug($"UI tracking {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Configura qué categorías de eventos trackear
        /// </summary>
        public void ConfigureEventTracking(bool menuEvents = true, bool buttonClicks = true, bool panelTransitions = true, bool formInteractions = true)
        {
            _trackMenuEvents = menuEvents;
            _trackButtonClicks = buttonClicks;
            _trackPanelTransitions = panelTransitions;
            _trackFormInteractions = formInteractions;
            
            LogDebug($"Event tracking configured - Menu: {menuEvents}, Buttons: {buttonClicks}, Panels: {panelTransitions}, Forms: {formInteractions}");
        }

        /// <summary>
        /// Obtiene estadísticas de la sesión actual
        /// </summary>
        public Dictionary<string, object> GetSessionStats()
        {
            return new Dictionary<string, object>
            {
                ["sessionId"] = _currentSessionId,
                ["sessionDuration"] = GetSessionDuration(),
                ["sessionStartTime"] = _sessionStartTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ["trackingEnabled"] = _enableTracking,
                ["isInitialized"] = _isInitialized,
                ["currentContext"] = GetCurrentUIContext()
            };
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[UIAnalyticsManager] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[UIAnalyticsManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[UIAnalyticsManager] {message}");
        }

        #endregion
    }
}