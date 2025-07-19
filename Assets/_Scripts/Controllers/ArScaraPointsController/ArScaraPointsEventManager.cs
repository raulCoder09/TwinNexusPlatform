using System;
using System.Collections.Generic;
using _Scripts.Controllers.EnvironmentController;
using UnityEngine;
using UnityEngine.UIElements;
using _Scripts.Controllers.UiManagement;

namespace _Scripts.Controllers.ArScaraPointsController
{
    public class ArScaraPointsEventManager
    {
        private ArScaraPointsUIManager _uiManager;
        private Action _onReturnToDashboard;
        private Action<IArScaraPointsOps.PanelType> _onPanelTransitionComplete;
        private ArScaraPointsOrchestrator _orchestrator;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private readonly List<Button> _buttons = new List<Button>();
        private readonly List<DropdownField> _dropdowns = new List<DropdownField>();
        private readonly List<TextField> _textFields = new List<TextField>();
        private readonly List<VisualElement> _pointItems = new List<VisualElement>();

        #region Constructor

        public ArScaraPointsEventManager(
            ArScaraPointsUIManager uiManager,
            Action onReturnToDashboard,
            Action<IArScaraPointsOps.PanelType> onPanelTransitionComplete,
            ArScaraPointsOrchestrator orchestrator)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _onReturnToDashboard = onReturnToDashboard ?? throw new ArgumentNullException(nameof(onReturnToDashboard));
            _onPanelTransitionComplete = onPanelTransitionComplete ?? throw new ArgumentNullException(nameof(onPanelTransitionComplete));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        #endregion

        #region Event Registration

        public void RegisterEvents(UIDocument uiDocument)
        {
            _uiDocument = uiDocument;
            _root = uiDocument.rootVisualElement;

            // Registrar botones
            RegisterButtonEvents();
            // Registrar dropdown
            RegisterDropdownEvents();
            // Registrar textfields
            RegisterTextFieldEvents();
            // Registrar point items
            RegisterPointItemEvents();
            // Registrar eventos de teclado
            RegisterKeyboardEvents(_root);
            // Suscribirse a cambios de entorno
            EnvironmentStateManager.OnEnvironmentChanged += UpdateUIState;

            // Inicializar estado de la UI
            UpdateUIState(EnvironmentStateManager.SelectedEnvironment);

            Debug.Log("[ArScaraPointsEventManager] All events registered successfully");
        }

        private void RegisterButtonEvents()
        {
            var buttonNames = new[]
            {
                "MenuButton", "HideMenuButton", "OperationsButton", "TrainingButton",
                "ReportsButton", "SupportButton", "SettingsButton", "LogoutButton",
                "AddPointButton", "EditPointButton", "DeletePointButton", "GoToPointButton",
                "SaveAllButton"
            };

            foreach (var name in buttonNames)
            {
                var button = _root.Q<Button>(name);
                if (button != null)
                {
                    _buttons.Add(button);
                    if (name == "MenuButton")
                    {
                        button.RegisterCallback<ClickEvent>(evt =>
                        {
                            Debug.Log("MenuButton pulsado");
                            OnMenuButtonClicked(evt);
                        });
                    }
                    else if (name == "HideMenuButton")
                    {
                        button.RegisterCallback<ClickEvent>(evt =>
                        {
                            Debug.Log("HideMenuButton pulsado");
                            OnHideMenuButtonClicked(evt);
                        });
                    }
                    else if (name == "OperationsButton")
                    {
                        button.RegisterCallback<ClickEvent>(evt =>
                        {
                            Debug.Log("OperationsButton pulsado");
                            OnOperationsButtonClicked(evt);
                        });
                    }
                    else if (name == "TrainingButton")
                    {
                        button.RegisterCallback<ClickEvent>(evt =>
                        {
                            Debug.Log("TrainingButton pulsado");
                            OnTrainingButtonClicked(evt);
                        });
                    }
                    else if (name == "ReportsButton")
                    {
                        button.RegisterCallback<ClickEvent>(evt =>
                        {
                            Debug.Log("ReportsButton pulsado");
                            OnReportsButtonClicked(evt);
                        });
                    }
                    else if (name == "SupportButton")
                    {
                        button.RegisterCallback<ClickEvent>(evt =>
                        {
                            Debug.Log("SupportButton pulsado");
                            OnSupportButtonClicked(evt);
                        });
                    }
                    else if (name == "SettingsButton")
                    {
                        button.RegisterCallback<ClickEvent>(evt =>
                        {
                            Debug.Log("SettingsButton pulsado");
                            OnSettingsButtonClicked(evt);
                        });
                    }
                    else if (name == "LogoutButton")
                    {
                        button.RegisterCallback<ClickEvent>(evt =>
                        {
                            Debug.Log("LogoutButton pulsado");
                            OnLogoutButtonClicked(evt);
                        });
                    }
                    else if (name == "DashboardButton")
                    {
                        button.RegisterCallback<ClickEvent>(evt =>
                        {
                            Debug.Log("DashboardButton pulsado");
                            OnDashboardButtonClicked(evt);
                        });
                    }
                    else
                    {
                        button.RegisterCallback<ClickEvent>(evt => Debug.Log($"{name} pulsado"));
                    }
                }
            }

            var dashboardButtons = _root.Query<Button>().Where(btn => btn.text == "Dashboard").ToList();
            foreach (var dashboardButton in dashboardButtons)
            {
                _buttons.Add(dashboardButton);
                dashboardButton.RegisterCallback<ClickEvent>(evt =>
                {
                    Debug.Log("DashboardButton pulsado");
                    OnDashboardButtonClicked(evt);
                });
            }
        }

        private void RegisterDropdownEvents()
        {
            var dropdown = _root.Q<DropdownField>("MenuRobotARSCARADropdownField");
            if (dropdown != null)
            {
                _dropdowns.Add(dropdown);
                dropdown.RegisterCallback<ChangeEvent<string>>(evt =>
                {
                    Debug.Log($"MenuRobotARSCARADropdownField cambió a {evt.newValue}");
                    OnArScaraDropdownChanged(evt);
                });
            }
        }

        private void RegisterTextFieldEvents()
        {
            var textFieldNames = new[]
            {
                "PointNameField", "XCoordinateField", "YCoordinateField",
                "ZCoordinateField", "UCoordinateField"
            };

            foreach (var name in textFieldNames)
            {
                var textField = _root.Q<TextField>(name);
                if (textField != null)
                {
                    _textFields.Add(textField);
                    textField.RegisterCallback<ChangeEvent<string>>(evt => Debug.Log($"{name} cambió a {evt.newValue}"));
                }
            }
        }

        private void RegisterPointItemEvents()
        {
            var pointItemNames = new[] { "Point1", "Point2", "Point3", "Point4", "Point5" };
            foreach (var name in pointItemNames)
            {
                var pointItem = _root.Q<VisualElement>(name);
                if (pointItem != null)
                {
                    _pointItems.Add(pointItem);
                    pointItem.RegisterCallback<ClickEvent>(evt => Debug.Log($"{name} seleccionado"));
                }
            }
        }

        private void UpdateUIState(string environment)
        {
            bool isValidEnvironment = EnvironmentStateManager.IsValidEnvironment;

            foreach (var button in _buttons)
            {
                bool isNavigationButton = button.name is "MenuButton" or "HideMenuButton" or "DashboardButton" or
                                         "OperationsButton" or "TrainingButton" or "ReportsButton" or
                                         "SupportButton" or "SettingsButton" or "LogoutButton";
                button.style.display = isNavigationButton || isValidEnvironment ? DisplayStyle.Flex : DisplayStyle.None;
                button.SetEnabled(isNavigationButton || isValidEnvironment);
            }

            foreach (var dropdown in _dropdowns)
            {
                bool isNavigationDropdown = dropdown.name == "MenuRobotARSCARADropdownField";
                dropdown.style.display = isNavigationDropdown || isValidEnvironment ? DisplayStyle.Flex : DisplayStyle.None;
                dropdown.SetEnabled(isNavigationDropdown || isValidEnvironment);
            }

            foreach (var textField in _textFields)
            {
                textField.style.display = isValidEnvironment ? DisplayStyle.Flex : DisplayStyle.None;
                textField.SetEnabled(isValidEnvironment);
            }

            foreach (var pointItem in _pointItems)
            {
                pointItem.style.display = isValidEnvironment ? DisplayStyle.Flex : DisplayStyle.None;
                pointItem.SetEnabled(isValidEnvironment);
            }
        }

        #endregion

        #region Event Unregistration

        public void UnregisterEvents()
        {
            try
            {
                Debug.Log("[ArScaraPointsEventManager] Unregistering AWS Settings events...");

                if (_root == null)
                {
                    Debug.LogWarning("[ArScaraPointsEventManager] Root element is null - cannot unregister events");
                    return;
                }

                foreach (var button in _buttons)
                {
                    button.UnregisterCallback<ClickEvent>(evt => Debug.Log($"{button.name} pulsado"));
                    if (button.name == "MenuButton") button.UnregisterCallback<ClickEvent>(OnMenuButtonClicked);
                    else if (button.name == "HideMenuButton") button.UnregisterCallback<ClickEvent>(OnHideMenuButtonClicked);
                    else if (button.name == "OperationsButton") button.UnregisterCallback<ClickEvent>(OnOperationsButtonClicked);
                    else if (button.name == "TrainingButton") button.UnregisterCallback<ClickEvent>(OnTrainingButtonClicked);
                    else if (button.name == "ReportsButton") button.UnregisterCallback<ClickEvent>(OnReportsButtonClicked);
                    else if (button.name == "SupportButton") button.UnregisterCallback<ClickEvent>(OnSupportButtonClicked);
                    else if (button.name == "SettingsButton") button.UnregisterCallback<ClickEvent>(OnSettingsButtonClicked);
                    else if (button.name == "LogoutButton") button.UnregisterCallback<ClickEvent>(OnLogoutButtonClicked);
                    else if (button.text == "Dashboard") button.UnregisterCallback<ClickEvent>(OnDashboardButtonClicked);
                }

                foreach (var dropdown in _dropdowns)
                {
                    dropdown.UnregisterCallback<ChangeEvent<string>>(OnArScaraDropdownChanged);
                }

                foreach (var textField in _textFields)
                {
                    textField.UnregisterCallback<ChangeEvent<string>>(evt => Debug.Log($"{textField.name} cambió a {evt.newValue}"));
                }

                foreach (var pointItem in _pointItems)
                {
                    pointItem.UnregisterCallback<ClickEvent>(evt => Debug.Log($"{pointItem.name} seleccionado"));
                }

                var navigationMenuPanel = _root.Q<VisualElement>("NavigationMenuPanel");
                navigationMenuPanel?.UnregisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);

                var scrim = _root.Q<VisualElement>("Scrim");
                scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);

                UnregisterKeyboardEvents(_root);
                EnvironmentStateManager.OnEnvironmentChanged -= UpdateUIState;

                Debug.Log("[ArScaraPointsEventManager] All events unregistered successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArScaraPointsEventManager] Error unregistering events: {ex.Message}");
            }
        }

        public void Cleanup()
        {
            UnregisterEvents();
            _uiManager = null;
            _orchestrator = null;
            _uiDocument = null;
            _root = null;
            _onReturnToDashboard = null;
            _onPanelTransitionComplete = null;
            Debug.Log("[ArScaraPointsEventManager] Event Manager cleaned up");
        }

        #endregion

        #region Keyboard Events

        private void RegisterKeyboardEvents(VisualElement root)
        {
            root.RegisterCallback<KeyDownEvent>(OnGlobalKeyDown);
        }

        private void UnregisterKeyboardEvents(VisualElement root)
        {
            root?.UnregisterCallback<KeyDownEvent>(OnGlobalKeyDown);
        }

        private async void OnGlobalKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    HandleEscapeKey();
                    break;
                case KeyCode.M when evt.ctrlKey:
                    _uiManager.ToggleNavigationMenu();
                    var action = _uiManager.NavigationMenuOpen ? "menu_opened" : "menu_closed";
                    await UIAnalyticsManager.Instance?.TrackMenuEvent(
                        action: action,
                        menuItem: null,
                        context: "keyboard_shortcut_ctrl_m"
                    );
                    break;
            }
        }

        private async void HandleEscapeKey()
        {
            if (_uiManager.CurrentActivePanel != IArScaraPointsOps.PanelType.None)
            {
                if (_uiManager.NavigationMenuOpen)
                {
                    _uiManager.HideNavigationMenu();
                    await UIAnalyticsManager.Instance?.TrackMenuEvent(
                        action: "menu_closed",
                        menuItem: null,
                        context: "escape_key"
                    );
                }
                else
                {
                    _uiManager.CloseCurrentPanel();
                    await UIAnalyticsManager.Instance?.TrackPanelTransition(
                        fromPanel: _uiManager.CurrentActivePanel.ToString(),
                        toPanel: "none",
                        transitionType: "escape_key"
                    );
                }
            }
        }

        #endregion

        #region Main Navigation Events

        private async void OnMenuButtonClicked(ClickEvent evt)
        {
            _uiManager.ShowNavigationMenu();
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_opened",
                menuItem: null,
                context: "aws_settings_main"
            );
        }

        private async void OnHideMenuButtonClicked(ClickEvent evt)
        {
            _uiManager.HideNavigationMenu();
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_closed",
                menuItem: null,
                context: "hide_button_click"
            );
        }

        private async void OnScrimClicked(ClickEvent evt)
        {
            if (evt.target == evt.currentTarget)
            {
                _uiManager.CloseCurrentPanel();
                await UIAnalyticsManager.Instance?.TrackMenuEvent(
                    action: "menu_closed",
                    menuItem: null,
                    context: "scrim_click"
                );
            }
        }

        #endregion

        #region Navigation Action Events

        private async void OnDashboardButtonClicked(ClickEvent evt)
        {
            Debug.Log("Dashboard button clicked - returning to Dashboard");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "DashboardButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleDashboardClick();
        }

        private async void OnOperationsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Operations button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "OperationsButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleOperationsClick();
        }

        private async void OnTrainingButtonClicked(ClickEvent evt)
        {
            Debug.Log("Training button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "TrainingButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleTrainingClick();
        }

        private async void OnSettingsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Settings button clicked - already in AWS Settings");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "SettingsButton",
                context: "navigation_menu"
            );
            _uiManager.HideNavigationMenu();
        }

        private async void OnSupportButtonClicked(ClickEvent evt)
        {
            Debug.Log("Support button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "SupportButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleSupportClick();
        }

        private async void OnLogoutButtonClicked(ClickEvent evt)
        {
            Debug.Log("Logout button clicked - executing logout");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "LogoutButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleLogoutClick();
        }

        private async void OnReportsButtonClicked(ClickEvent evt)
        {
            Debug.Log("Reports button clicked - executing navigation");
            await UIAnalyticsManager.Instance?.TrackMenuEvent(
                action: "menu_item_clicked",
                menuItem: "ReportsButton",
                context: "navigation_menu"
            );
            _orchestrator.HandleReportsClick();
        }

        #endregion

        #region Transition Events

        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            _onPanelTransitionComplete?.Invoke(_uiManager.CurrentActivePanel);
        }

        #endregion

        #region Public Properties

        public ArScaraPointsUIManager UIManager => _uiManager;
        public ArScaraPointsOrchestrator Orchestrator => _orchestrator;

        #endregion

        private async void OnArScaraDropdownChanged(ChangeEvent<string> evt)
        {
            var selectedValue = evt.newValue;
            await UIAnalyticsManager.Instance?.TrackButtonClick(
                buttonName: "ArScaraDropdown",
                context: "dropdown_navigation",
                additionalData: new Dictionary<string, object> { ["selection"] = selectedValue }
            );

            switch (selectedValue)
            {
                case "Control panel":
                    Debug.Log("Navigating to ArScaraControlPanel");
                    _orchestrator.HandleArScaraControlPanelNavigation();
                    break;
                case "Jog and teach":
                    Debug.Log("Navigating to ArScaraJogAndTeach");
                    _orchestrator.HandleArScaraJogAndTeachNavigation();
                    break;
                case "Points":
                    break;
            }
        }
    }
}