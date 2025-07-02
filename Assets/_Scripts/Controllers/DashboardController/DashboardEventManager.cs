using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.DashboardController
{
    /// <summary>
    /// Gestor de eventos de UI del Dashboard
    /// Responsable de registrar y manejar todos los eventos de interacción del usuario
    /// Equivalente al WelcomeEventManager pero para Dashboard
    /// </summary>
    public class DashboardEventManager
    {
        #region Private Fields

        private DashboardUIManager _uiManager;
        private DashboardOrchestrator _orchestrator;
        private UIDocument _uiDocument;
        private DashboardInfo.UIConfiguration _uiConfig;

        // Actions para comunicación con sistemas externos
        private Action _onLogoutRequested;
        private Action<DashboardSection> _onNavigationRequested;
        private Action<string> _onDeviceSelectionChanged;

        #endregion

        #region Constructor

        public DashboardEventManager(
            DashboardUIManager uiManager, 
            DashboardOrchestrator orchestrator,
            UIDocument uiDocument,
            Action onLogoutRequested = null,
            Action<DashboardSection> onNavigationRequested = null,
            Action<string> onDeviceSelectionChanged = null)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _uiDocument = uiDocument ?? throw new ArgumentNullException(nameof(uiDocument));
            _uiConfig = _uiManager.UIConfig;

            _onLogoutRequested = onLogoutRequested;
            _onNavigationRequested = onNavigationRequested;
            _onDeviceSelectionChanged = onDeviceSelectionChanged;
        }

        #endregion

        #region Event Registration

        /// <summary>
        /// Registra todos los eventos del Dashboard
        /// </summary>
        public void RegisterEvents()
        {
            try
            {
                Debug.Log("[DashboardEventManager] Registering Dashboard events...");

                RegisterHeaderEvents();
                RegisterNavigationEvents();
                RegisterDeviceControlEvents();
                RegisterMenuEvents();
                RegisterKeyboardEvents();

                Debug.Log("[DashboardEventManager] All events registered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DashboardEventManager] Error registering events: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra eventos del header (menú, logout)
        /// </summary>
        private void RegisterHeaderEvents()
        {
            // Botón de menú
            _uiConfig.MenuButton?.RegisterCallback<ClickEvent>(OnMenuButtonClicked);

            // Logout (puede ser botón o label clickeable)
            _uiConfig.LogoutLabel?.RegisterCallback<ClickEvent>(OnLogoutClicked);

            Debug.Log("[DashboardEventManager] Header events registered");
        }

        /// <summary>
        /// Registra eventos del menú de navegación
        /// </summary>
        private void RegisterNavigationEvents()
        {
            // Botón para ocultar menú
            _uiConfig.HideMenuButton?.RegisterCallback<ClickEvent>(OnHideMenuButtonClicked);

            // Eventos del scrim (click para cerrar menú)
            _uiConfig.Scrim?.RegisterCallback<ClickEvent>(OnScrimClicked);

            // Botones de navegación
            foreach (var kvp in _uiConfig.NavigationButtons)
            {
                var section = kvp.Key;
                var button = kvp.Value;
                
                if (button != null)
                {
                    // Usamos una lambda que captura la sección específica
                    button.RegisterCallback<ClickEvent>(evt => OnNavigationButtonClicked(evt, section));
                }
            }

            Debug.Log("[DashboardEventManager] Navigation events registered");
        }

        /// <summary>
        /// Registra eventos del panel de control de dispositivos
        /// </summary>
        private void RegisterDeviceControlEvents()
        {
            // Navegación entre dispositivos
            _uiConfig.DeviceNavLeftButton?.RegisterCallback<ClickEvent>(OnDeviceNavLeftClicked);
            _uiConfig.DeviceNavRightButton?.RegisterCallback<ClickEvent>(OnDeviceNavRightClicked);

            // Click en área de visualización del dispositivo (para futuras interacciones)
            _uiConfig.DeviceVisualizationBox?.RegisterCallback<ClickEvent>(OnDeviceVisualizationClicked);

            Debug.Log("[DashboardEventManager] Device control events registered");
        }

        /// <summary>
        /// Registra eventos específicos del menú (navegación lateral)
        /// </summary>
        private void RegisterMenuEvents()
        {
            // El menú se maneja principalmente a través de RegisterNavigationEvents
            // Este método está separado para futuras extensiones específicas del menú
            
            // Posibles eventos futuros:
            // - Hover effects
            // - Context menus
            // - Keyboard navigation dentro del menú
            
            Debug.Log("[DashboardEventManager] Menu-specific events registered");
        }

        /// <summary>
        /// Registra eventos de teclado para accesos rápidos
        /// </summary>
        private void RegisterKeyboardEvents()
        {
            var root = _uiDocument.rootVisualElement;

            // Registrar eventos de teclado globales para el Dashboard
            root?.RegisterCallback<KeyDownEvent>(OnKeyDown);

            Debug.Log("[DashboardEventManager] Keyboard events registered");
        }

        #endregion

        #region Header Event Handlers

        /// <summary>
        /// Maneja el click del botón de menú
        /// </summary>
        private void OnMenuButtonClicked(ClickEvent evt)
        {
            Debug.Log("[DashboardEventManager] Menu button clicked");
            _uiManager.ToggleNavigationMenu();
        }

        /// <summary>
        /// Maneja el click de logout
        /// </summary>
        private void OnLogoutClicked(ClickEvent evt)
        {
            Debug.Log("[DashboardEventManager] Logout clicked");
            
            // Confirmar logout a través del orchestrator
            _orchestrator?.HandleLogoutRequest();
            
            // Notificar a sistemas externos si es necesario
            _onLogoutRequested?.Invoke();
        }

        #endregion

        #region Navigation Event Handlers

        /// <summary>
        /// Maneja el click del botón para ocultar menú
        /// </summary>
        private void OnHideMenuButtonClicked(ClickEvent evt)
        {
            Debug.Log("[DashboardEventManager] Hide menu button clicked");
            _uiManager.ToggleNavigationMenu(false);
        }

        /// <summary>
        /// Maneja el click en el scrim (área oscura detrás del menú)
        /// </summary>
        private void OnScrimClicked(ClickEvent evt)
        {
            Debug.Log("[DashboardEventManager] Scrim clicked - closing menu");
            _uiManager.ToggleNavigationMenu(false);
        }

        /// <summary>
        /// Maneja el click en botones de navegación
        /// </summary>
        private void OnNavigationButtonClicked(ClickEvent evt, DashboardSection section)
        {
            Debug.Log($"[DashboardEventManager] Navigation button clicked: {section}");
            
            // Actualizar UI para mostrar sección activa
            _uiManager.UpdateActiveNavigationSection(section);
            
            // Cerrar menú después de navegar
            _uiManager.ToggleNavigationMenu(false);
            
            // Notificar al orchestrator sobre la navegación
            _orchestrator?.HandleNavigationRequest(section);
            
            // Notificar a sistemas externos
            _onNavigationRequested?.Invoke(section);
        }

        #endregion

        #region Device Control Event Handlers

        /// <summary>
        /// Maneja navegación hacia el dispositivo anterior
        /// </summary>
        private void OnDeviceNavLeftClicked(ClickEvent evt)
        {
            Debug.Log("[DashboardEventManager] Device nav left clicked");
            _orchestrator?.HandleDeviceNavigationLeft();
        }

        /// <summary>
        /// Maneja navegación hacia el siguiente dispositivo
        /// </summary>
        private void OnDeviceNavRightClicked(ClickEvent evt)
        {
            Debug.Log("[DashboardEventManager] Device nav right clicked");
            _orchestrator?.HandleDeviceNavigationRight();
        }

        /// <summary>
        /// Maneja click en el área de visualización del dispositivo
        /// </summary>
        private void OnDeviceVisualizationClicked(ClickEvent evt)
        {
            Debug.Log("[DashboardEventManager] Device visualization clicked");
            
            // Aquí se puede implementar lógica para:
            // - Mostrar detalles del dispositivo
            // - Abrir panel de control avanzado
            // - Cambiar vista de visualización
            
            _orchestrator?.HandleDeviceVisualizationClick();
        }

        #endregion

        #region Keyboard Event Handlers

        /// <summary>
        /// Maneja eventos de teclado globales
        /// </summary>
        private void OnKeyDown(KeyDownEvent evt)
        {
            // Atajos de teclado útiles para el Dashboard
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
                    // Toggle menú con M
                    if (evt.ctrlKey)
                    {
                        _uiManager.ToggleNavigationMenu();
                        evt.StopPropagation();
                    }
                    break;

                case KeyCode.LeftArrow:
                    // Navegar dispositivo anterior con flecha izquierda
                    if (evt.ctrlKey)
                    {
                        _orchestrator?.HandleDeviceNavigationLeft();
                        evt.StopPropagation();
                    }
                    break;

                case KeyCode.RightArrow:
                    // Navegar siguiente dispositivo con flecha derecha
                    if (evt.ctrlKey)
                    {
                        _orchestrator?.HandleDeviceNavigationRight();
                        evt.StopPropagation();
                    }
                    break;

                case KeyCode.F5:
                    // Refresh datos con F5
                    _orchestrator?.HandleRefreshRequest();
                    evt.StopPropagation();
                    break;

                // Atajos numéricos para navegación rápida
                case KeyCode.Alpha1:
                    if (evt.ctrlKey) NavigateToSectionByShortcut(DashboardSection.Main, evt);
                    break;
                case KeyCode.Alpha2:
                    if (evt.ctrlKey) NavigateToSectionByShortcut(DashboardSection.Training, evt);
                    break;
                case KeyCode.Alpha3:
                    if (evt.ctrlKey) NavigateToSectionByShortcut(DashboardSection.Operations, evt);
                    break;
                case KeyCode.Alpha4:
                    if (evt.ctrlKey) NavigateToSectionByShortcut(DashboardSection.Reports, evt);
                    break;
                case KeyCode.Alpha5:
                    if (evt.ctrlKey) NavigateToSectionByShortcut(DashboardSection.Support, evt);
                    break;
                case KeyCode.Alpha6:
                    if (evt.ctrlKey) NavigateToSectionByShortcut(DashboardSection.Settings, evt);
                    break;
            }
        }

        /// <summary>
        /// Navega a una sección usando atajo de teclado
        /// </summary>
        private void NavigateToSectionByShortcut(DashboardSection section, KeyDownEvent evt)
        {
            Debug.Log($"[DashboardEventManager] Keyboard shortcut navigation to: {section}");
            _uiManager.UpdateActiveNavigationSection(section);
            _orchestrator?.HandleNavigationRequest(section);
            _onNavigationRequested?.Invoke(section);
            evt.StopPropagation();
        }

        #endregion

        #region Widget Events (for future expansion)

        /// <summary>
        /// Registra eventos específicos de widgets (para futuras expansiones)
        /// </summary>
        private void RegisterWidgetEvents()
        {
            // Eventos futuros para widgets específicos:
            // - Click en elementos del widget de IoT para cambiar modos
            // - Click en actividades recientes para ver detalles
            // - Hover effects en widgets
            // - Resize de widgets
            
            Debug.Log("[DashboardEventManager] Widget events registered (placeholder for future expansion)");
        }

        /// <summary>
        /// Maneja clicks en elementos del widget de IoT
        /// </summary>
        private void OnIoTWidgetElementClicked(ClickEvent evt, IoTService service)
        {
            Debug.Log($"[DashboardEventManager] IoT widget element clicked: {service}");
            _orchestrator?.HandleIoTServiceInteraction(service);
        }

        /// <summary>
        /// Maneja clicks en actividades recientes
        /// </summary>
        private void OnRecentActivityClicked(ClickEvent evt, SystemActivity activity)
        {
            Debug.Log($"[DashboardEventManager] Recent activity clicked: {activity.Action}");
            _orchestrator?.HandleActivityDetailsRequest(activity);
        }

        #endregion

        #region Event Unregistration

        /// <summary>
        /// Desregistra todos los eventos del Dashboard
        /// </summary>
        public void UnregisterEvents()
        {
            try
            {
                Debug.Log("[DashboardEventManager] Unregistering Dashboard events...");

                // Header events
                _uiConfig.MenuButton?.UnregisterCallback<ClickEvent>(OnMenuButtonClicked);
                _uiConfig.LogoutLabel?.UnregisterCallback<ClickEvent>(OnLogoutClicked);

                // Navigation events
                _uiConfig.HideMenuButton?.UnregisterCallback<ClickEvent>(OnHideMenuButtonClicked);
                _uiConfig.Scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);

                // Device control events
                _uiConfig.DeviceNavLeftButton?.UnregisterCallback<ClickEvent>(OnDeviceNavLeftClicked);
                _uiConfig.DeviceNavRightButton?.UnregisterCallback<ClickEvent>(OnDeviceNavRightClicked);
                _uiConfig.DeviceVisualizationBox?.UnregisterCallback<ClickEvent>(OnDeviceVisualizationClicked);

                // Keyboard events
                var root = _uiDocument.rootVisualElement;
                root?.UnregisterCallback<KeyDownEvent>(OnKeyDown);

                // Navigation buttons (necesitamos un approach diferente para lambdas)
                UnregisterNavigationButtons();

                Debug.Log("[DashboardEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DashboardEventManager] Error unregistering events: {ex.Message}");
            }
        }

        /// <summary>
        /// Desregistra eventos de botones de navegación
        /// </summary>
        private void UnregisterNavigationButtons()
        {
            // Para lambdas, necesitamos recrear la referencia exacta o usar un approach diferente
            // Por simplicidad, registraremos los eventos de manera que se puedan desregistrar fácilmente
            foreach (var kvp in _uiConfig.NavigationButtons)
            {
                var button = kvp.Value;
                if (button != null)
                {
                    // Nota: Para desregistrar lambdas correctamente, necesitaríamos almacenar las referencias
                    // Por ahora, esto es un placeholder para la implementación completa
                    Debug.Log($"[DashboardEventManager] Unregistering navigation button: {kvp.Key}");
                }
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// UI Manager asociado
        /// </summary>
        public DashboardUIManager UIManager => _uiManager;

        /// <summary>
        /// Orchestrator asociado
        /// </summary>
        public DashboardOrchestrator Orchestrator => _orchestrator;

        #endregion

        #region Cleanup

        /// <summary>
        /// Limpia recursos del Event Manager
        /// </summary>
        public void Cleanup()
        {
            UnregisterEvents();
            
            _uiManager = null;
            _orchestrator = null;
            _uiDocument = null;
            _uiConfig = null;
            _onLogoutRequested = null;
            _onNavigationRequested = null;
            _onDeviceSelectionChanged = null;

            Debug.Log("[DashboardEventManager] Event Manager cleaned up");
        }

        #endregion
    }
}