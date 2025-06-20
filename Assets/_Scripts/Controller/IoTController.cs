using System;
using _ScriptableObjects;
using _Scripts.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class IoTController : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private IotConfigurationData iotConfigurationData;
        #endregion

        #region UI Components - Main
        private VisualElement _body;
        private Button _menuButton;
        private VisualElement _navigationMenuPanel;
        private VisualElement _scrim;
        private Button _hideMenuButton;
        private Button _dashboardButton;
        private VisualElement _subpanelsAndSmokeMaskContainer;
        #endregion

        #region UI Components - Local IoT
        private TextField _ipOrHostnameTextField;
        private TextField _portTextField;
        private TextField _clientIDTextField;
        private DropdownField _modeConnectionDropdownField;
        private TextField _usernameTextField;
        private TextField _passwordTextField;
        private Button _connectLocalButton;
        private Button _disconnectLocalButton;
        private Button _testMessageButton;
        private Label _statusLocalLabel;
        #endregion

        #region UI Components - Cloud IoT
        private TextField _endpointTextField;
        private TextField _cloudThingNameTextField;
        private TextField _cloudPortTextField;
        private TextField _caFileTextField;
        private TextField _clientCertificateFileTextField;
        private TextField _clientKeyFileTextField;
        private TextField _cloudPfxFilePathTextField;
        private DropdownField _cloudModeConnectionDropdownField;
        private Button _cloudConnectButton;
        private Button _cloudDisconnectButton;
        private Label _cloudStatusLabel;
        private Button _cloudTestMessageButton;
        #endregion

        #region UI Components - VM IoT
        private TextField _cloudVmIoTipOrHostnameTextField;
        private TextField _cloudVmIoTPortTextField;
        private TextField _cloudVmIoTClientIDTextField;
        private DropdownField _cloudVmIoTModeConnectionDropdownField;
        private TextField _cloudVmIoTUsernameTextField;
        private TextField _cloudVmIoTPasswordTextField;
        private Button _connectCloudVmIoTButton;
        private Button _disconnectCloudVmIoTButton;
        private Button _testMessageCloudVmIoTButton;
        private Label _statusCloudVmIoTLabel;
        #endregion

        #region Dependencies
        private LocalIoT _localIoT;
        private CloudIoT _cloudIoT;
        private VMIoT _vmIoT;
        private SaveSystem _saveSystem;
        private DashboardController _dashboardController;
        #endregion

        #region Constants
        private const string AUTOMATIC_CONNECTION = "Automatic connection";
        private const string ONLINE_STATUS = "Online";
        private const string OFFLINE_STATUS = "Offline";
        private const string CLIENT_NOT_CONNECTED = "Client not connected";
        private const string ERROR_CONNECTION_PREFIX = "Error connection: ";
        private const string ERROR_DISCONNECT_PREFIX = "Error disconnect: ";
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            InitializeUI();
            LoadConfiguration();
            AttemptAutoConnections();
            UpdateStatusColors();
        }
        #endregion

        #region Public Methods
        public void HideUi()
        {
            _body.style.display = DisplayStyle.None;
        }
        
        public void ShowUi()
        {
            _body.style.display = DisplayStyle.Flex;
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            GetUiComponents();
            FindDependencies();
            RegisterAllEvents();
        }

        private void InitializeUI()
        {
            HideUi();
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            _saveSystem.LoadData();
        }

        private void FindDependencies()
        {
            _localIoT = FindComponentByTag<LocalIoT>("LocalIoT");
            _cloudIoT = FindComponentByTag<CloudIoT>("CloudIoT");
            _vmIoT = FindComponentByTag<VMIoT>("VMIoT");
            _saveSystem = FindComponentByTag<SaveSystem>("GameManager");
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
        #endregion

        #region Configuration Management
        private void LoadConfiguration()
        {
            LoadLocalIoTConfiguration();
            LoadCloudIoTConfiguration();
            LoadVMIoTConfiguration();
        }

        private void LoadLocalIoTConfiguration()
        {
            _ipOrHostnameTextField.value = iotConfigurationData.brokerAddress;
            _portTextField.value = iotConfigurationData.BrokerPort.ToString();
            _clientIDTextField.value = iotConfigurationData.ClientId;
            _usernameTextField.value = iotConfigurationData.Username;
            _passwordTextField.value = iotConfigurationData.Password;
            _modeConnectionDropdownField.value = iotConfigurationData.ModeConnection;
            _dashboardController.localIoTModeLabel.text = iotConfigurationData.ModeConnection;
        }

        private void LoadCloudIoTConfiguration()
        {
            _endpointTextField.value = iotConfigurationData.endpoint;
            _cloudThingNameTextField.value = iotConfigurationData.thingName;
            _caFileTextField.value = iotConfigurationData.caFilePath;
            _clientCertificateFileTextField.value = iotConfigurationData.clientCertPath;
            _clientKeyFileTextField.value = iotConfigurationData.clientKeyPath;
            _cloudPortTextField.value = iotConfigurationData.cloudPort;
            _cloudPfxFilePathTextField.value = iotConfigurationData.pfxFilePath;
            _cloudModeConnectionDropdownField.value = iotConfigurationData.cloudModeConnection;
            _dashboardController.cloudlIoTModeLabel.text = iotConfigurationData.cloudModeConnection;
        }

        private void LoadVMIoTConfiguration()
        {
            _cloudVmIoTipOrHostnameTextField.value = iotConfigurationData.cloudVmIoTipOrHostname;
            _cloudVmIoTPortTextField.value = iotConfigurationData.cloudVmIoTPort.ToString();
            _cloudVmIoTClientIDTextField.value = iotConfigurationData.cloudVmIoTClientID;
            _cloudVmIoTModeConnectionDropdownField.value = iotConfigurationData.cloudVmIoTModeConnection;
            _cloudVmIoTUsernameTextField.value = iotConfigurationData.cloudVmIoTUsername;
            _cloudVmIoTPasswordTextField.value = iotConfigurationData.cloudVmIoTPassword;
            _dashboardController.vMIoTModeLabel.text = iotConfigurationData.cloudVmIoTModeConnection;
        }

        private void AttemptAutoConnections()
        {
            if (_modeConnectionDropdownField.value == AUTOMATIC_CONNECTION)
            {
                ConnectToLocalBroker();
            }

            if (_cloudModeConnectionDropdownField.value == AUTOMATIC_CONNECTION)
            {
                ConnectToCloudBroker();
            }
            
            if (_cloudVmIoTModeConnectionDropdownField.value == AUTOMATIC_CONNECTION)
            {
                ConnectToVMBroker();
            }
        }

        private void UpdateStatusColors()
        {
            UpdateStatusColor(_dashboardController.localIoTStatusLabel);
            UpdateStatusColor(_dashboardController.cloudIoTStatusLabel);
            UpdateStatusColor(_dashboardController.vMIoTStatusLabel);
        }

        private void UpdateStatusColor(Label statusLabel)
        {
            switch (statusLabel.text)
            {
                case ONLINE_STATUS:
                    statusLabel.style.color = Color.green;
                    break;
                case OFFLINE_STATUS:
                    statusLabel.style.color = Color.red;
                    break;
                case CLIENT_NOT_CONNECTED:
                    statusLabel.style.color = Color.yellow;
                    break;
                default:
                    if (statusLabel.text.StartsWith("Error"))
                        statusLabel.style.color = Color.red;
                    break;
            }
        }
        #endregion

        #region Connection Methods - Local IoT
        private async void ConnectToLocalBroker(ClickEvent evt = null)
        {
            try
            {
                var result = await _localIoT.ConnectToBroker();
                var status = result ? ONLINE_STATUS : OFFLINE_STATUS;
                SetConnectionStatus(_statusLocalLabel, _dashboardController.localIoTStatusLabel, status);
            }
            catch (Exception ex)
            {
                var errorMessage = ERROR_CONNECTION_PREFIX + ex.Message;
                SetConnectionStatus(_statusLocalLabel, _dashboardController.localIoTStatusLabel, errorMessage);
            }
        }

        private async void DisconnectFromLocalBroker(ClickEvent evt)
        {
            try
            {
                var result = await _localIoT.DisconnectFromBroker();
                var status = result ? OFFLINE_STATUS : CLIENT_NOT_CONNECTED;
                SetConnectionStatus(_statusLocalLabel, _dashboardController.localIoTStatusLabel, status);
            }
            catch (Exception ex)
            {
                var errorMessage = ERROR_DISCONNECT_PREFIX + ex.Message;
                SetConnectionStatus(_statusLocalLabel, _dashboardController.localIoTStatusLabel, errorMessage);
            }
        }

        private void SendLocalTestMessage(ClickEvent evt)
        {
            _ = _localIoT.SendTestMessage();
        }
        #endregion

        #region Connection Methods - Cloud IoT
        private async void ConnectToCloudBroker(ClickEvent evt = null)
        {
            var isConnected = await _cloudIoT.ConnectToAwsIoT();
            var status = isConnected ? ONLINE_STATUS : OFFLINE_STATUS;
            SetConnectionStatus(_cloudStatusLabel, _dashboardController.cloudIoTStatusLabel, status);
        }

        private async void DisconnectFromCloudBroker(ClickEvent evt)
        {
            var isDisconnected = await _cloudIoT.DisconnectFromAwsIoT();
            var status = isDisconnected ? OFFLINE_STATUS : CLIENT_NOT_CONNECTED;
            SetConnectionStatus(_cloudStatusLabel, _dashboardController.cloudIoTStatusLabel, status);
        }

        private void SendCloudTestMessage(ClickEvent evt)
        {
            _ = _cloudIoT.SendTestMessage();
        }
        #endregion

        #region Connection Methods - VM IoT
        private async void ConnectToVMBroker(ClickEvent evt = null)
        {
            try
            {
                var result = await _vmIoT.ConnectToEC2Broker();
                var status = result ? ONLINE_STATUS : OFFLINE_STATUS;
                SetConnectionStatus(_statusCloudVmIoTLabel, _dashboardController.vMIoTStatusLabel, status);
            }
            catch (Exception ex)
            {
                var errorMessage = ERROR_CONNECTION_PREFIX + ex.Message;
                SetConnectionStatus(_statusCloudVmIoTLabel, _dashboardController.vMIoTStatusLabel, errorMessage);
            }
        }

        private async void DisconnectFromVMBroker(ClickEvent evt)
        {
            try
            {
                var result = await _vmIoT.DisconnectFromEC2Broker();
                var status = result ? OFFLINE_STATUS : CLIENT_NOT_CONNECTED;
                SetConnectionStatus(_statusCloudVmIoTLabel, _dashboardController.vMIoTStatusLabel, status);
            }
            catch (Exception ex)
            {
                var errorMessage = ERROR_DISCONNECT_PREFIX + ex.Message;
                SetConnectionStatus(_statusCloudVmIoTLabel, _dashboardController.vMIoTStatusLabel, errorMessage);
            }
        }

        private void SendVMTestMessage(ClickEvent evt)
        {
            _ = _vmIoT.SendTestMessageToEC2();
        }
        #endregion

        #region Helper Methods
        private void SetConnectionStatus(Label primaryLabel, Label dashboardLabel, string status)
        {
            primaryLabel.text = status;
            dashboardLabel.text = status;
            UpdateStatusColor(dashboardLabel);
        }

        private void SaveConfigurationAndUpdateIoT<T>(T ioTInstance, Action<T> updateAction, T newValue)
        {
            updateAction(ioTInstance);
            _saveSystem.SaveData();
        }
        #endregion

        #region UI Event Registration
        private void RegisterAllEvents()
        {
            RegisterLocalIoTEvents();
            RegisterCloudIoTEvents();
            RegisterVMIoTEvents();
            RegisterButtonEvents();
            RegisterMenuEvents();
        }

        private void RegisterLocalIoTEvents()
        {
            _ipOrHostnameTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.brokerAddress = _localIoT.brokerAddress = evt.newValue;
                _saveSystem.SaveData();
            });
            
            _portTextField.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out var port))
                {
                    iotConfigurationData.BrokerPort = _localIoT.brokerPort = port;
                    _saveSystem.SaveData();
                }
            });
            
            _clientIDTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.ClientId = _localIoT.clientId = evt.newValue;
                _saveSystem.SaveData();
            });

            _modeConnectionDropdownField.RegisterValueChangedCallback(evt =>
            {
                _dashboardController.localIoTModeLabel.text = _localIoT.modeConnection = 
                    iotConfigurationData.ModeConnection = evt.newValue;
                _saveSystem.SaveData();
            });
            
            _usernameTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.Username = _localIoT.username = evt.newValue;
                _saveSystem.SaveData();
            });
            
            _passwordTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.Password = _localIoT.password = evt.newValue;
                _saveSystem.SaveData();
            });
        }

        private void RegisterCloudIoTEvents()
        {
            _endpointTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.endpoint = _cloudIoT.endpoint = evt.newValue;
                _saveSystem.SaveData();
            });
            
            _cloudThingNameTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.thingName = _cloudIoT.thingName = evt.newValue;
                _saveSystem.SaveData();
            });
            
            _caFileTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.caFilePath = _cloudIoT.caFilePath = evt.newValue;
                _saveSystem.SaveData();
            });
            
            _clientCertificateFileTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.clientCertPath = _cloudIoT.clientCertPath = evt.newValue;
                _saveSystem.SaveData();
            });
            
            _clientKeyFileTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.clientKeyPath = _cloudIoT.clientKeyPath = evt.newValue;
                _saveSystem.SaveData();
            });
            
            _cloudModeConnectionDropdownField.RegisterValueChangedCallback(evt =>
            {
                _dashboardController.cloudlIoTModeLabel.text = iotConfigurationData.cloudModeConnection = 
                    _cloudIoT.modeConnection = evt.newValue;
                _saveSystem.SaveData();
            });

            _cloudPortTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.cloudPort = _cloudIoT.port = evt.newValue;
                _saveSystem.SaveData();
            });

            _cloudPfxFilePathTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.pfxFilePath = _cloudIoT.pfxFilePath = evt.newValue;
                _saveSystem.SaveData();
            });
        }

        private void RegisterVMIoTEvents()
        {
            _cloudVmIoTipOrHostnameTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.cloudVmIoTipOrHostname = _vmIoT.ipOrHostname = evt.newValue;
                _saveSystem.SaveData();
            });
            
            _cloudVmIoTPortTextField.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out var port))
                {
                    iotConfigurationData.cloudVmIoTPort = _vmIoT.port = port;
                    _saveSystem.SaveData();
                }
            });
            
            _cloudVmIoTClientIDTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.cloudVmIoTClientID = _vmIoT.clientId = evt.newValue;
                _saveSystem.SaveData();
            });
            
            _cloudVmIoTModeConnectionDropdownField.RegisterValueChangedCallback(evt =>
            {
                _dashboardController.vMIoTModeLabel.text = iotConfigurationData.cloudVmIoTModeConnection = 
                    _vmIoT.modeConnection = evt.newValue;
                _saveSystem.SaveData();
            });
            
            _cloudVmIoTUsernameTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.cloudVmIoTUsername = _vmIoT.username = evt.newValue;
                _saveSystem.SaveData();
            });
            
            _cloudVmIoTPasswordTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.cloudVmIoTPassword = _vmIoT.password = evt.newValue;
                _saveSystem.SaveData();
            });
        }

        private void RegisterButtonEvents()
        {
            // Local IoT buttons
            _connectLocalButton.RegisterCallback<ClickEvent>(ConnectToLocalBroker);
            _disconnectLocalButton.RegisterCallback<ClickEvent>(DisconnectFromLocalBroker);
            _testMessageButton.RegisterCallback<ClickEvent>(SendLocalTestMessage);
            
            // Cloud IoT buttons
            _cloudConnectButton.RegisterCallback<ClickEvent>(ConnectToCloudBroker);
            _cloudDisconnectButton.RegisterCallback<ClickEvent>(DisconnectFromCloudBroker);
            _cloudTestMessageButton.RegisterCallback<ClickEvent>(SendCloudTestMessage);
            
            // VM IoT buttons
            _connectCloudVmIoTButton.RegisterCallback<ClickEvent>(ConnectToVMBroker);
            _disconnectCloudVmIoTButton.RegisterCallback<ClickEvent>(DisconnectFromVMBroker);
            _testMessageCloudVmIoTButton.RegisterCallback<ClickEvent>(SendVMTestMessage);
        }

        private void RegisterMenuEvents()
        {
            _menuButton.RegisterCallback<ClickEvent>(ShowMenu);
            _hideMenuButton.RegisterCallback<ClickEvent>(HideMenu);
            _dashboardButton.RegisterCallback<ClickEvent>(StartDashboard);
            _navigationMenuPanel.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);
        }
        #endregion

        #region Menu Management
        private void ShowMenu(ClickEvent evt)
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.Flex;
            _navigationMenuPanel.AddToClassList("NavigationMenuPanelInMainScreen");
            _scrim.AddToClassList("ScrimOpaque");
        }
        
        private void HideMenu(ClickEvent evt)
        {
            _navigationMenuPanel.RemoveFromClassList("NavigationMenuPanelInMainScreen");
            _scrim.RemoveFromClassList("ScrimOpaque");
        }

        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            if (!_navigationMenuPanel.ClassListContains("NavigationMenuPanelInMainScreen"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }

        private void StartDashboard(ClickEvent evt)
        {
            HideMenu(evt);
            _body.style.display = DisplayStyle.None;
            _dashboardController.ShowUi();
        }
        #endregion

        #region UI Component Retrieval
        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            
            GetMainUIComponents(root);
            GetLocalIoTComponents(root);
            GetCloudIoTComponents(root);
            GetVMIoTComponents(root);
        }

        private void GetMainUIComponents(VisualElement root)
        {
            _body = root.Q<VisualElement>("Body");
            _menuButton = root.Q<Button>("MenuButton");
            _hideMenuButton = root.Q<Button>("HideMenuButton");
            _navigationMenuPanel = root.Q<VisualElement>("NavigationMenuPanel");
            _subpanelsAndSmokeMaskContainer = root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _scrim = root.Q<VisualElement>("Scrim");
            _dashboardButton = root.Q<Button>("DashboardButton");
        }

        private void GetLocalIoTComponents(VisualElement root)
        {
            _ipOrHostnameTextField = root.Q<TextField>("IPOrHostnameTextField");
            _portTextField = root.Q<TextField>("PortTextField");
            _clientIDTextField = root.Q<TextField>("ClientIDTextField");
            _modeConnectionDropdownField = root.Q<DropdownField>("ModeConnectionDropdownField");
            _usernameTextField = root.Q<TextField>("UsernameTextField");
            _passwordTextField = root.Q<TextField>("PasswordTextField");
            _connectLocalButton = root.Q<Button>("ConnectLocalButton");
            _disconnectLocalButton = root.Q<Button>("DisconnectLocalButton");
            _testMessageButton = root.Q<Button>("TestMessageButton");
            _statusLocalLabel = root.Q<Label>("StatusLocalLabel");
        }

        private void GetCloudIoTComponents(VisualElement root)
        {
            _endpointTextField = root.Q<TextField>("CloudEndPointTextField");
            _cloudThingNameTextField = root.Q<TextField>("CloudThingNameTextField");
            _caFileTextField = root.Q<TextField>("CAFileTextField");
            _clientCertificateFileTextField = root.Q<TextField>("ClientCertificateFileTextField");
            _clientKeyFileTextField = root.Q<TextField>("ClientKeyFileTextField");
            _cloudModeConnectionDropdownField = root.Q<DropdownField>("CloudModeConnectionDropdownField");
            _cloudConnectButton = root.Q<Button>("CloudConnectButton");
            _cloudDisconnectButton = root.Q<Button>("CloudDisconnectButton");
            _cloudStatusLabel = root.Q<Label>("CloudStatusLabel");
            _cloudPortTextField = root.Q<TextField>("CloudPortTextField");
            _cloudPfxFilePathTextField = root.Q<TextField>("CloudPfxFilePathTextField");
            _cloudTestMessageButton = root.Q<Button>("CloudTestMessageButton");
        }

        private void GetVMIoTComponents(VisualElement root)
        {
            _cloudVmIoTipOrHostnameTextField = root.Q<TextField>("CloudVMIoTIPOrHostnameTextField");
            _cloudVmIoTPortTextField = root.Q<TextField>("CloudVMIoTPortTextField");
            _cloudVmIoTClientIDTextField = root.Q<TextField>("CloudVMIoTClientIDTextField");
            _cloudVmIoTModeConnectionDropdownField = root.Q<DropdownField>("CloudVMIoTModeConnectionDropdownField");
            _cloudVmIoTUsernameTextField = root.Q<TextField>("CloudVMIoTUsernameTextField");
            _cloudVmIoTPasswordTextField = root.Q<TextField>("CloudVMIoTPasswordTextField");
            _connectCloudVmIoTButton = root.Q<Button>("ConnectCloudVMIoTButton");
            _disconnectCloudVmIoTButton = root.Q<Button>("DisconnectCloudVMIoTButton");
            _testMessageCloudVmIoTButton = root.Q<Button>("TestMessageCloudVMIoTButton");
            _statusCloudVmIoTLabel = root.Q<Label>("StatusLocalCloudVMIoTLabel");
        }
        #endregion
    }
}