using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.DashboardController
{
    /// <summary>
    /// Maneja todos los eventos UI del Dashboard
    /// Equivalente a WelcomeEventManager pero para Dashboard
    /// </summary>
    public class DashboardEventManager
    {
        private DashboardUIManager _uiManager;
        private Action _onLogoutRequested;
        private Action<IDashboardOps.PanelType> _onPanelTransitionComplete;
        private DashboardOrchestrator _orchestrator;

        // Referencias para poder desregistrar eventos
        private UIDocument _uiDocument;
        private VisualElement _root;

        #region Constructor

        public DashboardEventManager(
            DashboardUIManager uiManager, 
            Action onLogoutRequested, 
            Action<IDashboardOps.PanelType> onPanelTransitionComplete, 
            DashboardOrchestrator orchestrator)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _onLogoutRequested = onLogoutRequested ?? throw new ArgumentNullException(nameof(onLogoutRequested));
            _onPanelTransitionComplete = onPanelTransitionComplete ?? throw new ArgumentNullException(nameof(onPanelTransitionComplete));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        #endregion

        #region Event Registration

        /// <summary>
        /// Registra todos los eventos del Dashboard
        /// </summary>
        public void RegisterEvents(UIDocument uiDocument)
        {
            // Guardar referencias para desregistro posterior
            _uiDocument = uiDocument;
            _root = uiDocument.rootVisualElement;

            // Eventos principales del menú
            _root.Q<Button>("MenuButton")?.RegisterCallback<ClickEvent>(OnMenuButtonClicked);
            _root.Q<Button>("HideMenuButton")?.RegisterCallback<ClickEvent>(OnHideMenuButtonClicked);

            // Eventos de navegación principal
            _root.Q<Button>("OperationsButton")?.RegisterCallback<ClickEvent>(OnOperationsButtonClicked);
            _root.Q<Button>("TrainingButton")?.RegisterCallback<ClickEvent>(OnTrainingButtonClicked);
            _root.Q<Button>("SettingsButton")?.RegisterCallback<ClickEvent>(OnSettingsButtonClicked);
            _root.Q<Button>("LogoutButton")?.RegisterCallback<ClickEvent>(OnLogoutButtonClicked);

            // Eventos de paneles adicionales (futuros)
            _root.Q<Button>("NotificationsButton")?.RegisterCallback<ClickEvent>(OnNotificationsButtonClicked);
            _root.Q<Button>("UserProfileButton")?.RegisterCallback<ClickEvent>(OnUserProfileButtonClicked);
            _root.Q<Button>("QuickActionsButton")?.RegisterCallback<ClickEvent>(OnQuickActionsButtonClicked);
            _root.Q<Button>("StatusOverlayButton")?.RegisterCallback<ClickEvent>(OnStatusOverlayButtonClicked);

            // Eventos de cierre de paneles
            _root.Q<Button>("CloseNotificationsButton")?.RegisterCallback<ClickEvent>(OnCloseNotificationsPanelClicked);
            _root.Q<Button>("CloseUserProfileButton")?.RegisterCallback<ClickEvent>(OnCloseUserProfilePanelClicked);
            _root.Q<Button>("CloseQuickActionsButton")?.RegisterCallback<ClickEvent>(OnCloseQuickActionsPanelClicked);
            _root.Q<Button>("CloseStatusOverlayButton")?.RegisterCallback<ClickEvent>(OnCloseStatusOverlayPanelClicked);

            // Evento de transición del menú de navegación
            var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
            navigationMenuPanel?.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

            // Eventos de clic en el scrim para cerrar paneles
            var scrim = _root.Q<VisualElement>("Scrim");
            scrim?.RegisterCallback<ClickEvent>(OnScrimClicked);

            // Registrar eventos de teclado
            RegisterKeyboardEvents(_root);

            Debug.Log("[DashboardEventManager] All events registered successfully");
        }

        /// <summary>
        /// Desregistra todos los eventos del Dashboard
        /// </summary>
        public void UnregisterEvents()
        {
            try
            {
                Debug.Log("[DashboardEventManager] Unregistering Dashboard events...");

                if (_root == null)
                {
                    Debug.LogWarning("[DashboardEventManager] Root element is null - cannot unregister events");
                    return;
                }

                // Eventos principales del menú
                _root.Q<Button>("MenuButton")?.UnregisterCallback<ClickEvent>(OnMenuButtonClicked);
                _root.Q<Button>("HideMenuButton")?.UnregisterCallback<ClickEvent>(OnHideMenuButtonClicked);

                // Eventos de navegación principal
                _root.Q<Button>("OperationsButton")?.UnregisterCallback<ClickEvent>(OnOperationsButtonClicked);
                _root.Q<Button>("TrainingButton")?.UnregisterCallback<ClickEvent>(OnTrainingButtonClicked);
                _root.Q<Button>("SettingsButton")?.UnregisterCallback<ClickEvent>(OnSettingsButtonClicked);
                _root.Q<Button>("LogoutButton")?.UnregisterCallback<ClickEvent>(OnLogoutButtonClicked);

                // Eventos de paneles adicionales
                _root.Q<Button>("NotificationsButton")?.UnregisterCallback<ClickEvent>(OnNotificationsButtonClicked);
                _root.Q<Button>("UserProfileButton")?.UnregisterCallback<ClickEvent>(OnUserProfileButtonClicked);
                _root.Q<Button>("QuickActionsButton")?.UnregisterCallback<ClickEvent>(OnQuickActionsButtonClicked);
                _root.Q<Button>("StatusOverlayButton")?.UnregisterCallback<ClickEvent>(OnStatusOverlayButtonClicked);

                // Eventos de cierre de paneles
                _root.Q<Button>("CloseNotificationsButton")?.UnregisterCallback<ClickEvent>(OnCloseNotificationsPanelClicked);
                _root.Q<Button>("CloseUserProfileButton")?.UnregisterCallback<ClickEvent>(OnCloseUserProfilePanelClicked);
                _root.Q<Button>("CloseQuickActionsButton")?.UnregisterCallback<ClickEvent>(OnCloseQuickActionsPanelClicked);
                _root.Q<Button>("CloseStatusOverlayButton")?.UnregisterCallback<ClickEvent>(OnCloseStatusOverlayPanelClicked);

                // Evento de transición del menú de navegación
                var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
                navigationMenuPanel?.UnregisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

                // Eventos de clic en el scrim
                var scrim = _root.Q<VisualElement>("Scrim");
                scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);

                // Desregistrar eventos de teclado
                UnregisterKeyboardEvents(_root);

                Debug.Log("[DashboardEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DashboardEventManager] Error unregistering events: {ex.Message}");
            }
        }

        /// <summary>
        /// Método Cleanup consistente con WelcomeEventManager
        /// </summary>
        public void Cleanup()
        {
            UnregisterEvents();
            
            _uiManager = null;
            _orchestrator = null;
            _uiDocument = null;
            _root = null;
            _onLogoutRequested = null;
            _onPanelTransitionComplete = null;

            Debug.Log("[DashboardEventManager] Event Manager cleaned up");
        }

        #endregion

        #region Keyboard Events

        /// <summary>
        /// Registra eventos de teclado para el Dashboard
        /// </summary>
        private void RegisterKeyboardEvents(VisualElement root)
        {
            // Escape para cerrar paneles/menú
            root.RegisterCallback<KeyDownEvent>(OnGlobalKeyDown);
            
            // M para toggle del menú (si se quiere)
            // Aquí puedes agregar más shortcuts de teclado
        }

        /// <summary>
        /// Desregistra eventos de teclado
        /// </summary>
        private void UnregisterKeyboardEvents(VisualElement root)
        {
            root?.UnregisterCallback<KeyDownEvent>(OnGlobalKeyDown);
        }

        /// <summary>
        /// Maneja eventos globales de teclado
        /// </summary>
        private void OnGlobalKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    HandleEscapeKey();
                    break;
                    
                case KeyCode.M when evt.ctrlKey: // Ctrl+M para toggle menú
                    _uiManager.ToggleNavigationMenu();
                    break;
            }
        }

        /// <summary>
        /// Maneja la tecla Escape
        /// </summary>
        private void HandleEscapeKey()
        {
            // Cerrar panel actual o menú lateral
            if (_uiManager.CurrentActivePanel != IDashboardOps.PanelType.None)
            {
                if (_uiManager.NavigationMenuOpen)
                {
                    _uiManager.HideNavigationMenu();
                }
                else
                {
                    _uiManager.CloseCurrentPanel();
                }
            }
        }

        #endregion

        #region Main Navigation Events

        /// <summary>
        /// Abre el menú lateral de navegación
        /// </summary>
        private void OnMenuButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowNavigationMenu();
        }

        /// <summary>
        /// Cierra el menú lateral de navegación
        /// </summary>
        private void OnHideMenuButtonClicked(ClickEvent evt)
        {
            _uiManager.HideNavigationMenu();
        }

        /// <summary>
        /// Maneja clic en el scrim para cerrar paneles
        /// </summary>
        private void OnScrimClicked(ClickEvent evt)
        {
            // Solo cerrar si el clic fue directamente en el scrim, no en sus hijos
            if (evt.target == evt.currentTarget)
            {
                _uiManager.CloseCurrentPanel();
            }
        }

        #endregion

        #region Navigation Action Events

        /// <summary>
        /// Inicia modo Operations
        /// </summary>
        private void OnOperationsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Operations button clicked - executing navigation");
            _orchestrator.HandleOperationsClick();
        }

        /// <summary>
        /// Inicia modo Training
        /// </summary>
        private void OnTrainingButtonClicked(ClickEvent evt)
        {
            Debug.Log("Training button clicked - executing navigation");
            _orchestrator.HandleTrainingClick();
        }

        /// <summary>
        /// Abre Settings
        /// </summary>
        private void OnSettingsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Settings button clicked - executing navigation");
            _orchestrator.HandleSettingsClick();
        }

        /// <summary>
        /// Ejecuta Logout
        /// </summary>
        private void OnLogoutButtonClicked(ClickEvent evt)
        {
            Debug.Log("Logout button clicked - executing logout");
            _orchestrator.HandleLogoutClick();
        }

        #endregion

        #region Panel Events

        /// <summary>
        /// Abre panel de notificaciones
        /// </summary>
        private void OnNotificationsButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowPanel(IDashboardOps.PanelType.Notifications);
        }

        /// <summary>
        /// Abre panel de perfil de usuario
        /// </summary>
        private void OnUserProfileButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowPanel(IDashboardOps.PanelType.UserProfile);
        }

        /// <summary>
        /// Abre panel de acciones rápidas
        /// </summary>
        private void OnQuickActionsButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowPanel(IDashboardOps.PanelType.QuickActions);
        }

        /// <summary>
        /// Abre panel de estado de IoT
        /// </summary>
        private void OnStatusOverlayButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowPanel(IDashboardOps.PanelType.StatusOverlay);
        }

        #endregion

        #region Panel Close Events

        /// <summary>
        /// Cierra panel de notificaciones
        /// </summary>
        private void OnCloseNotificationsPanelClicked(ClickEvent evt)
        {
            _uiManager.HidePanel(IDashboardOps.PanelType.Notifications);
        }

        /// <summary>
        /// Cierra panel de perfil de usuario
        /// </summary>
        private void OnCloseUserProfilePanelClicked(ClickEvent evt)
        {
            _uiManager.HidePanel(IDashboardOps.PanelType.UserProfile);
        }

        /// <summary>
        /// Cierra panel de acciones rápidas
        /// </summary>
        private void OnCloseQuickActionsPanelClicked(ClickEvent evt)
        {
            _uiManager.HidePanel(IDashboardOps.PanelType.QuickActions);
        }

        /// <summary>
        /// Cierra panel de estado de IoT
        /// </summary>
        private void OnCloseStatusOverlayPanelClicked(ClickEvent evt)
        {
            _uiManager.HidePanel(IDashboardOps.PanelType.StatusOverlay);
        }

        #endregion

        #region Transition Events

        /// <summary>
        /// Maneja el final de la transición del menú de navegación
        /// </summary>
        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            // Similar al patrón de Welcome - notificar completion
            _onPanelTransitionComplete?.Invoke(_uiManager.CurrentActivePanel);
            
            // Si no hay paneles visibles, el UIManager ya maneja ocultar el container
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
    }
}