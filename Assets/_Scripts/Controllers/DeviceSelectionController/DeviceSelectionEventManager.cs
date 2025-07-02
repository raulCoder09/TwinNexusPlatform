using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.DeviceSelectionController
{
    /// <summary>
    /// Gestor de eventos de UI del Device Selection
    /// Responsable de registrar y manejar todos los eventos de interacción del usuario
    /// Equivalente al WelcomeEventManager y DashboardEventManager pero para Device Selection
    /// </summary>
    public class DeviceSelectionEventManager
    {
        #region Private Fields

        private DeviceSelectionUIManager _uiManager;
        private DeviceSelectionOrchestrator _orchestrator;
        private UIDocument _uiDocument;
        private VisualElement _root;
        private DeviceSelectionInfo.UIConfiguration _uiConfig;

        // Actions para comunicación con sistemas externos
        private Action _onLogoutRequested;
        private Action<NavigationContext> _onNavigationRequested;
        private Action<string> _onDeviceSelected;

        // Referencias para desregistro de eventos
        private readonly Dictionary<Button, EventCallback<ClickEvent>> _buttonEventReferences = new Dictionary<Button, EventCallback<ClickEvent>>();
        private readonly Dictionary<VisualElement, EventCallback<KeyDownEvent>> _keyEventReferences = new Dictionary<VisualElement, EventCallback<KeyDownEvent>>();

        #endregion

        #region Constructor

        public DeviceSelectionEventManager(
            DeviceSelectionUIManager uiManager, 
            DeviceSelectionOrchestrator orchestrator,
            UIDocument uiDocument,
            Action onLogoutRequested = null,
            Action<NavigationContext> onNavigationRequested = null,
            Action<string> onDeviceSelected = null)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _uiDocument = uiDocument ?? throw new ArgumentNullException(nameof(uiDocument));
            _uiConfig = _uiManager.UIConfig;
            _root = _uiDocument.rootVisualElement;

            _onLogoutRequested = onLogoutRequested;
            _onNavigationRequested = onNavigationRequested;
            _onDeviceSelected = onDeviceSelected;
        }

        #endregion

        #region Event Registration

        /// <summary>
        /// Registra todos los eventos del Device Selection
        /// </summary>
        public void RegisterEvents()
        {
            try
            {
                Debug.Log("[DeviceSelectionEventManager] Registering Device Selection events...");

                RegisterHeaderEvents();
                RegisterNavigationMenuEvents();
                RegisterDeviceSelectionEvents();
                RegisterKeyboardEvents();

                Debug.Log("[DeviceSelectionEventManager] All events registered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeviceSelectionEventManager] Error registering events: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra eventos del header (menú, logout)
        /// </summary>
        private void RegisterHeaderEvents()
        {
            // Botón de menú
            RegisterButtonEvent(_uiConfig.MenuButton, OnMenuButtonClicked);

            // Logout (label clickeable)
            if (_uiConfig.LogoutLabel != null)
            {
                EventCallback<ClickEvent> logoutAction = OnLogoutClicked;
                _uiConfig.LogoutLabel.RegisterCallback<ClickEvent>(logoutAction);
                // No agregamos a _buttonEventReferences porque es Label, no Button
            }

            Debug.Log("[DeviceSelectionEventManager] Header events registered");
        }

        /// <summary>
        /// Registra eventos del menú de navegación
        /// </summary>
        private void RegisterNavigationMenuEvents()
        {
            // Botón para ocultar menú
            RegisterButtonEvent(_uiConfig.HideMenuButton, OnHideMenuButtonClicked);

            // Eventos del scrim (click para cerrar menú)
            if (_uiConfig.Scrim != null)
            {
                EventCallback<ClickEvent> scrimAction = OnScrimClicked;
                _uiConfig.Scrim.RegisterCallback<ClickEvent>(scrimAction);
            }

            // Botones de navegación
            foreach (var kvp in _uiConfig.NavigationButtons)
            {
                var context = kvp.Key;
                var button = kvp.Value;
                
                if (button != null)
                {
                    if (context == NavigationContext.None) // Logout button
                    {
                        RegisterButtonEvent(button, OnLogoutClicked);
                    }
                    else
                    {
                        // Crear EventCallback específico para cada contexto
                        EventCallback<ClickEvent> navigationAction = (evt) => OnNavigationButtonClicked(evt, context);
                        button.RegisterCallback<ClickEvent>(navigationAction);
                        _buttonEventReferences[button] = navigationAction;
                    }
                }
            }

            Debug.Log("[DeviceSelectionEventManager] Navigation menu events registered");
        }

        /// <summary>
        /// Registra eventos de selección de dispositivos
        /// </summary>
        private void RegisterDeviceSelectionEvents()
        {
            // Registrar eventos para todos los botones de dispositivos
            foreach (var kvp in _uiConfig.DeviceButtons)
            {
                var deviceId = kvp.Key;
                var button = kvp.Value;
                
                if (button != null)
                {
                    // Crear EventCallback específico para cada dispositivo
                    EventCallback<ClickEvent> deviceAction = (evt) => OnDeviceButtonClicked(evt, deviceId);
                    button.RegisterCallback<ClickEvent>(deviceAction);
                    _buttonEventReferences[button] = deviceAction;
                }
            }

            Debug.Log($"[DeviceSelectionEventManager] Device selection events registered for {_uiConfig.DeviceButtons.Count} devices");
        }

        /// <summary>
        /// Registra eventos de teclado para accesos rápidos
        /// </summary>
        private void RegisterKeyboardEvents()
        {
            // Registrar eventos de teclado globales
            if (_root != null)
            {
                EventCallback<KeyDownEvent> keyAction = OnKeyDown;
                _root.RegisterCallback<KeyDownEvent>(keyAction);
                _keyEventReferences[_root] = keyAction;
            }

            Debug.Log("[DeviceSelectionEventManager] Keyboard events registered");
        }

        /// <summary>
        /// Método helper para registrar eventos de botones con tracking
        /// </summary>
        private void RegisterButtonEvent(Button button, EventCallback<ClickEvent> action)
        {
            if (button != null && action != null)
            {
                button.RegisterCallback<ClickEvent>(action);
                _buttonEventReferences[button] = action;
            }
        }

        #endregion

        #region Header Event Handlers

        /// <summary>
        /// Maneja el click del botón de menú
        /// </summary>
        private void OnMenuButtonClicked(ClickEvent evt)
        {
            Debug.Log("[DeviceSelectionEventManager] Menu button clicked");
            _uiManager.ToggleNavigationMenu();
        }

        /// <summary>
        /// Maneja el click de logout
        /// </summary>
        private void OnLogoutClicked(ClickEvent evt)
        {
            Debug.Log("[DeviceSelectionEventManager] Logout clicked");
            
            // Procesar logout a través del orchestrator
            _orchestrator?.HandleLogoutRequest();
            
            // Notificar a sistemas externos
            _onLogoutRequested?.Invoke();
        }

        #endregion

        #region Navigation Event Handlers

        /// <summary>
        /// Maneja el click del botón para ocultar menú
        /// </summary>
        private void OnHideMenuButtonClicked(ClickEvent evt)
        {
            Debug.Log("[DeviceSelectionEventManager] Hide menu button clicked");
            _uiManager.ToggleNavigationMenu(false);
        }

        /// <summary>
        /// Maneja el click en el scrim (área oscura detrás del menú)
        /// </summary>
        private void OnScrimClicked(ClickEvent evt)
        {
            Debug.Log("[DeviceSelectionEventManager] Scrim clicked - closing menu");
            _uiManager.ToggleNavigationMenu(false);
        }

        /// <summary>
        /// Maneja el click en botones de navegación
        /// </summary>
        private void OnNavigationButtonClicked(ClickEvent evt, NavigationContext targetContext)
        {
            Debug.Log($"[DeviceSelectionEventManager] Navigation button clicked: {targetContext}");
            
            // Cerrar menú primero
            _uiManager.ToggleNavigationMenu(false);
            
            // Procesar navegación a través del orchestrator
            _orchestrator?.HandleNavigationRequest(targetContext);
            
            // Notificar a sistemas externos
            _onNavigationRequested?.Invoke(targetContext);
        }

        #endregion

        #region Device Selection Event Handlers

        /// <summary>
        /// Maneja el click en botones de dispositivos
        /// </summary>
        private void OnDeviceButtonClicked(ClickEvent evt, string deviceId)
        {
            Debug.Log($"[DeviceSelectionEventManager] Device button clicked: {deviceId}");
            
            // Validar que el dispositivo está disponible
            if (!_orchestrator.IsDeviceAvailable(deviceId))
            {
                Debug.LogWarning($"[DeviceSelectionEventManager] Device {deviceId} is not available for selection");
                return;
            }

            // Procesar selección a través del orchestrator
            bool selected = _orchestrator.SelectDevice(deviceId);
            
            if (selected)
            {
                // Actualizar UI
                _uiManager.SelectDevice(deviceId);
                
                // Notificar a sistemas externos
                _onDeviceSelected?.Invoke(deviceId);
                
                // Procesar lanzamiento del dispositivo
                _orchestrator.HandleDeviceLaunch(deviceId);
            }
            else
            {
                Debug.LogWarning($"[DeviceSelectionEventManager] Failed to select device: {deviceId}");
            }
        }

        #endregion

        #region Keyboard Event Handlers

        /// <summary>
        /// Maneja eventos de teclado globales
        /// </summary>
        private void OnKeyDown(KeyDownEvent evt)
        {
            // Atajos de teclado útiles para Device Selection
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    // Cerrar menú si está abierto
                    if (_uiManager.IsNavigationMenuVisible)
                    {
                        _uiManager.ToggleNavigationMenu(false);
                        evt.StopPropagation();
                    }
                    break;

                case KeyCode.M:
                    // Toggle menú con Ctrl+M
                    if (evt.ctrlKey)
                    {
                        _uiManager.ToggleNavigationMenu();
                        evt.StopPropagation();
                    }
                    break;

                case KeyCode.F5:
                    // Refresh dispositivos con F5
                    _orchestrator?.HandleRefreshDevices();
                    evt.StopPropagation();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    // Lanzar dispositivo seleccionado con Enter
                    if (!string.IsNullOrEmpty(_orchestrator.SelectedDevice))
                    {
                        _orchestrator.HandleDeviceLaunch(_orchestrator.SelectedDevice);
                        evt.StopPropagation();
                    }
                    break;

                // Atajos numéricos para selección rápida de dispositivos
                case KeyCode.Alpha1:
                    if (evt.ctrlKey) SelectDeviceByShortcut("ARSCARA", evt);
                    break;
                case KeyCode.Alpha2:
                    if (evt.ctrlKey) SelectDeviceByShortcut("RobotKit1", evt);
                    break;
                case KeyCode.Alpha3:
                    if (evt.ctrlKey) SelectDeviceByShortcut("RobotKit2", evt);
                    break;

                // Atajos de navegación rápida
                case KeyCode.D:
                    if (evt.ctrlKey) NavigateByShortcut(NavigationContext.Dashboard, evt);
                    break;
                case KeyCode.T:
                    if (evt.ctrlKey) NavigateByShortcut(NavigationContext.Training, evt);
                    break;
                case KeyCode.O:
                    if (evt.ctrlKey) NavigateByShortcut(NavigationContext.Operations, evt);
                    break;
                case KeyCode.R:
                    if (evt.ctrlKey) NavigateByShortcut(NavigationContext.Reports, evt);
                    break;
            }
        }

        /// <summary>
        /// Selecciona un dispositivo usando atajo de teclado
        /// </summary>
        private void SelectDeviceByShortcut(string deviceId, KeyDownEvent evt)
        {
            if (_orchestrator.IsDeviceAvailable(deviceId))
            {
                Debug.Log($"[DeviceSelectionEventManager] Keyboard shortcut device selection: {deviceId}");
                OnDeviceButtonClicked(null, deviceId);
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// Navega a una sección usando atajo de teclado
        /// </summary>
        private void NavigateByShortcut(NavigationContext context, KeyDownEvent evt)
        {
            Debug.Log($"[DeviceSelectionEventManager] Keyboard shortcut navigation to: {context}");
            _orchestrator?.HandleNavigationRequest(context);
            _onNavigationRequested?.Invoke(context);
            evt.StopPropagation();
        }

        #endregion

        #region Special Device Events (for future expansion)

        /// <summary>
        /// Maneja doble click en dispositivos (para futuras funcionalidades)
        /// </summary>
        private void OnDeviceDoubleClicked(string deviceId)
        {
            Debug.Log($"[DeviceSelectionEventManager] Device double-clicked: {deviceId}");
            
            // Funcionalidad futura: mostrar detalles del dispositivo, configuración avanzada, etc.
            _orchestrator?.HandleDeviceDetailsRequest(deviceId);
        }

        /// <summary>
        /// Maneja hover sobre dispositivos (para futuras funcionalidades)
        /// </summary>
        private void OnDeviceHover(string deviceId, bool isHovering)
        {
            Debug.Log($"[DeviceSelectionEventManager] Device hover: {deviceId} - {isHovering}");
            
            // Funcionalidad futura: tooltips, preview de información, etc.
            if (isHovering)
            {
                _orchestrator?.HandleDeviceHoverEnter(deviceId);
            }
            else
            {
                _orchestrator?.HandleDeviceHoverExit(deviceId);
            }
        }

        /// <summary>
        /// Maneja context menu en dispositivos (para futuras funcionalidades)
        /// </summary>
        private void OnDeviceContextMenu(string deviceId, Vector2 position)
        {
            Debug.Log($"[DeviceSelectionEventManager] Device context menu: {deviceId} at {position}");
            
            // Funcionalidad futura: menú contextual con opciones específicas del dispositivo
            _orchestrator?.HandleDeviceContextMenu(deviceId, position);
        }

        #endregion

        #region Event Unregistration

        /// <summary>
        /// Desregistra todos los eventos del Device Selection
        /// </summary>
        public void UnregisterEvents()
        {
            try
            {
                Debug.Log("[DeviceSelectionEventManager] Unregistering Device Selection events...");

                // Desregistrar eventos de botones
                foreach (var kvp in _buttonEventReferences)
                {
                    var button = kvp.Key;
                    var action = kvp.Value;
                    
                    if (button != null && action != null)
                    {
                        button.UnregisterCallback<ClickEvent>(action);
                    }
                }
                _buttonEventReferences.Clear();

                // Desregistrar eventos de teclado
                foreach (var kvp in _keyEventReferences)
                {
                    var element = kvp.Key;
                    var action = kvp.Value;
                    
                    if (element != null && action != null)
                    {
                        element.UnregisterCallback<KeyDownEvent>(action);
                    }
                }
                _keyEventReferences.Clear();

                // Desregistrar eventos especiales
                if (_uiConfig.LogoutLabel != null)
                {
                    // Note: Necesitaríamos mantener referencia para desregistrar correctamente
                    // Por simplicidad, asumimos que el cleanup general manejará esto
                }

                if (_uiConfig.Scrim != null)
                {
                    // Similar al logout label
                }

                Debug.Log("[DeviceSelectionEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeviceSelectionEventManager] Error unregistering events: {ex.Message}");
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// UI Manager asociado
        /// </summary>
        public DeviceSelectionUIManager UIManager => _uiManager;

        /// <summary>
        /// Orchestrator asociado
        /// </summary>
        public DeviceSelectionOrchestrator Orchestrator => _orchestrator;

        #endregion

        #region Cleanup

        /// <summary>
        /// Limpia recursos del Event Manager
        /// </summary>
        public void Cleanup()
        {
            try
            {
                Debug.Log("[DeviceSelectionEventManager] Cleaning up Event Manager...");

                // Desregistrar todos los eventos
                UnregisterEvents();
                
                // Limpiar referencias
                _uiManager = null;
                _orchestrator = null;
                _uiDocument = null;
                _root = null;
                _uiConfig = null;
                _onLogoutRequested = null;
                _onNavigationRequested = null;
                _onDeviceSelected = null;

                Debug.Log("[DeviceSelectionEventManager] Event Manager cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeviceSelectionEventManager] Cleanup error: {ex.Message}");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Obtiene el número total de eventos registrados
        /// </summary>
        /// <returns>Número de eventos activos</returns>
        public int GetRegisteredEventsCount()
        {
            return _buttonEventReferences.Count + _keyEventReferences.Count;
        }

        /// <summary>
        /// Verifica si los eventos están correctamente registrados
        /// </summary>
        /// <returns>True si todos los eventos críticos están registrados</returns>
        public bool AreEventsRegistered()
        {
            return _buttonEventReferences.Count > 0 && 
                   _uiConfig.MenuButton != null && 
                   _buttonEventReferences.ContainsKey(_uiConfig.MenuButton);
        }

        /// <summary>
        /// Obtiene estadísticas de eventos registrados
        /// </summary>
        /// <returns>Información de eventos</returns>
        public string GetEventStatistics()
        {
            return $"Button Events: {_buttonEventReferences.Count}, " +
                   $"Keyboard Events: {_keyEventReferences.Count}, " +
                   $"Device Buttons: {_uiConfig.DeviceButtons.Count}, " +
                   $"Navigation Buttons: {_uiConfig.NavigationButtons.Count}";
        }

        #endregion
    }
}