using System;
using _ScriptableObjects;
using _Scripts.Models;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;


namespace _Scripts.Controller
{
    public class IoTController : MonoBehaviour
    {
        private VisualElement _body;
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
        
        private LocalIoT _localIoT;
        private CloudIoT _cloudIoT;
        private VMIoT _vmIoT;
        private SaveSystem _saveSystem;
        private DashboardController _dashboardController;
        private Button _menuButton;
        private VisualElement _navigationMenuPanel;
        private VisualElement _scrim;
        private Button _hideMenuButton;
        private Button _dashboardButton;
        
        
        
        [SerializeField] private IotConfigurationData iotConfigurationData;
        
        private VisualElement _subpanelsAndSmokeMaskContainer;

        private TextField _cloudVmIoTipOrHostnameTextField;
        private TextField _cloudVmIoTPortTextField;
        private TextField _cloudVmIoTClientIDTextField;
        private DropdownField _cloudVmIoTModeConnectionDropdownField;
        private TextField _cloudVmIoTUsernameTextField;
        private TextField _cloudVmIoTPasswordTextField;
        private Button _connectCloudVmIoTButton;
        private Button _disconnectCloudVmIoTButton;
        private Button _testMessageCloudVmIoTButton;
        private Label _statusLocalCloudVmIoTLabel;
        

        private void Awake()
        {
            GetUiComponents();
            RegisterEvents();
            FindObjects();
        }
        
        private void Start()
        {
            HideUi();
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            _saveSystem.LoadData();
            _ipOrHostnameTextField.value= iotConfigurationData.brokerAddress;
            _portTextField.value = iotConfigurationData.BrokerPort.ToString();
            _clientIDTextField.value=iotConfigurationData.ClientId;
            _usernameTextField.value=iotConfigurationData.Username;
            _passwordTextField.value=iotConfigurationData.Password;
            _dashboardController.localIoTModeLabel.text=_modeConnectionDropdownField.value = iotConfigurationData.ModeConnection;

            _cloudPfxFilePathTextField.value = iotConfigurationData.pfxFilePath;
            _endpointTextField.value=iotConfigurationData.endpoint;
            _cloudThingNameTextField.value = iotConfigurationData.thingName;
            _caFileTextField.value = iotConfigurationData.caFilePath;
            _clientCertificateFileTextField.value = iotConfigurationData.clientCertPath;
            _clientKeyFileTextField.value = iotConfigurationData.clientKeyPath;
            _cloudPortTextField.value = iotConfigurationData.cloudPort;
            _cloudPfxFilePathTextField.value = iotConfigurationData.pfxFilePath;
            _dashboardController.cloudlIoTModeLabel.text= _cloudModeConnectionDropdownField.value=iotConfigurationData.cloudModeConnection;
            
            _cloudVmIoTipOrHostnameTextField.value = iotConfigurationData.cloudVmIoTipOrHostname;
            _cloudVmIoTPortTextField.value = iotConfigurationData.cloudVmIoTPort.ToString();
            _cloudVmIoTClientIDTextField.value = iotConfigurationData.cloudVmIoTClientID;
            _cloudVmIoTModeConnectionDropdownField.value = iotConfigurationData.cloudVmIoTModeConnection;
            _cloudVmIoTUsernameTextField.value=iotConfigurationData.cloudVmIoTUsername;
            _cloudVmIoTPasswordTextField.value=iotConfigurationData.cloudVmIoTPassword;
                
            if (_modeConnectionDropdownField.value=="Automatic connection")
            {
                ConnectToBroker();
            }

            if (_cloudModeConnectionDropdownField.value == "Automatic connection")
            {
                ConnectToAwsIotCore();
            }
            
            if (_cloudVmIoTModeConnectionDropdownField.value == "Automatic connection")
            {
                ConnectToVM();
            }
            
            if (_dashboardController.localIoTStatusLabel.text=="Online")
            {
                _dashboardController.localIoTStatusLabel.style.color=Color.green;
            }
            else
            {
                _dashboardController.localIoTStatusLabel.style.color=Color.red;
            }
            
            if (_dashboardController.cloudIoTStatusLabel.text=="Online")
            {
                _dashboardController.cloudIoTStatusLabel.style.color=Color.green;
            }
            else
            {
                _dashboardController.cloudIoTStatusLabel.style.color=Color.red;
            }
            
        }
        
        internal void HideUi()
        {
            _body.style.display = DisplayStyle.None;
        }
        internal void ShowUi()
        {
            _body.style.display = DisplayStyle.Flex;
        }
        
        private void FindObjects()
        {
            _localIoT = GameObject.FindGameObjectWithTag("LocalIoT").GetComponent<LocalIoT>();
            _cloudIoT = GameObject.FindGameObjectWithTag("CloudIoT").GetComponent<CloudIoT>();
            _vmIoT= GameObject.FindGameObjectWithTag("VMIoT").GetComponent<VMIoT>();
            _saveSystem=GameObject.FindGameObjectWithTag("GameManager").GetComponent<SaveSystem>();
            _dashboardController=GameObject.FindGameObjectWithTag("Dashboard").GetComponent<DashboardController>();
        }
        
        private void RegisterEvents()
        {
            _ipOrHostnameTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.brokerAddress=_localIoT.brokerAddress = evt.newValue;
                _saveSystem.SaveData();
            });
            
            _portTextField.RegisterValueChangedCallback(evt =>
            {
                int.TryParse(evt.newValue, out var port);
                iotConfigurationData.BrokerPort=_localIoT.brokerPort =port;
                _saveSystem.SaveData();
            });
            
            _clientIDTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.ClientId=_localIoT.clientId = evt.newValue;
                _saveSystem.SaveData();
            });

            _modeConnectionDropdownField.RegisterValueChangedCallback(evt =>
            {
                _localIoT.modeConnection = iotConfigurationData.ModeConnection= evt.newValue;
                _saveSystem.SaveData();
            });
            
            _usernameTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.Username=_localIoT.username = evt.newValue;
                _saveSystem.SaveData();
            });
            _passwordTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.Password=_localIoT.password = evt.newValue;
                _saveSystem.SaveData();
                
            });
            
            _endpointTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.endpoint=_cloudIoT.endpoint = evt.newValue;
                _saveSystem.SaveData();
            });
            _cloudThingNameTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.thingName=_cloudIoT.thingName = evt.newValue;
                _saveSystem.SaveData();
            });
            _caFileTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.caFilePath=_cloudIoT.caFilePath = evt.newValue;
                _saveSystem.SaveData();
            });
            _clientCertificateFileTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.clientCertPath=_cloudIoT.clientCertPath = evt.newValue;
                _saveSystem.SaveData();
            });
            _clientKeyFileTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.clientKeyPath=_cloudIoT.clientKeyPath = evt.newValue;
                _saveSystem.SaveData();
            });
            _cloudModeConnectionDropdownField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.cloudModeConnection=_cloudIoT.modeConnection = evt.newValue;
                _saveSystem.SaveData();
            });

            _cloudPortTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.cloudPort = _cloudIoT.port = evt.newValue;
                _saveSystem.SaveData();
            });

            _cloudPfxFilePathTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.pfxFilePath = _cloudIoT.pfxFilePath= evt.newValue;;
                _saveSystem.SaveData();
            });



            
            _cloudVmIoTipOrHostnameTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.cloudVmIoTipOrHostname = _vmIoT.ipOrHostname= evt.newValue;;
                _saveSystem.SaveData();
            });
            _cloudVmIoTPortTextField.RegisterValueChangedCallback(evt =>
            {
                int.TryParse(evt.newValue, out var port);
                iotConfigurationData.cloudVmIoTPort = _vmIoT.port= port;
                _saveSystem.SaveData();
            });
            _cloudVmIoTClientIDTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.cloudVmIoTClientID = _vmIoT.clientId= evt.newValue;;
                _saveSystem.SaveData();
            });
            _cloudVmIoTModeConnectionDropdownField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.cloudVmIoTModeConnection = _vmIoT.modeConnection= evt.newValue;;
                _saveSystem.SaveData();
            });
            _cloudVmIoTUsernameTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.cloudVmIoTUsername = _vmIoT.username= evt.newValue;;
                _saveSystem.SaveData();
            });
            _cloudVmIoTPasswordTextField.RegisterValueChangedCallback(evt =>
            {
                iotConfigurationData.cloudVmIoTPassword = _vmIoT.password= evt.newValue;;
                _saveSystem.SaveData();
            });
            
            
            _connectLocalButton.RegisterCallback<ClickEvent>(ConnectToBroker);
            _disconnectLocalButton.RegisterCallback<ClickEvent>(DisconnectFromBroker);
            _cloudConnectButton.RegisterCallback<ClickEvent>(ConnectToAwsIotCore);
            _cloudDisconnectButton.RegisterCallback<ClickEvent>(DisconnectFromAwsIotCore);
            _cloudTestMessageButton.RegisterCallback<ClickEvent>(TestMessageAwsIotCore);
            _testMessageButton.RegisterCallback<ClickEvent>(TestMessageLocal);
            _connectCloudVmIoTButton.RegisterCallback<ClickEvent>(ConnectToVM);;
            _disconnectCloudVmIoTButton.RegisterCallback<ClickEvent>(DisconnectFromVM);;
            _testMessageCloudVmIoTButton.RegisterCallback<ClickEvent>(TestMessageVM);;
            _menuButton.RegisterCallback<ClickEvent>(ShowMenu);
            _hideMenuButton.RegisterCallback<ClickEvent>(HideMenu);
            _navigationMenuPanel.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);
            _dashboardButton.RegisterCallback<ClickEvent>(StartDashboard);
            
        }
        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            if (!_navigationMenuPanel.ClassListContains("NavigationMenuPanelInMainScreen"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }


        
        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _ipOrHostnameTextField= root.Q<TextField>("IPOrHostnameTextField");
            _portTextField= root.Q<TextField>("PortTextField");
            _clientIDTextField= root.Q<TextField>("ClientIDTextField");
            _modeConnectionDropdownField=root.Q<DropdownField>("ModeConnectionDropdownField");
            _usernameTextField=root.Q<TextField>("UsernameTextField");
            _passwordTextField=root.Q<TextField>("PasswordTextField");
            _connectLocalButton=root.Q<Button>("ConnectLocalButton");
            _disconnectLocalButton=root.Q<Button>("DisconnectLocalButton");
            _testMessageButton=root.Q<Button>("TestMessageButton");
            _statusLocalLabel=root.Q<Label>("StatusLocalLabel");
            
            
            _endpointTextField=root.Q<TextField>("CloudEndPointTextField");
            _cloudThingNameTextField=root.Q<TextField>("CloudThingNameTextField");
            _caFileTextField=root.Q<TextField>("CAFileTextField");
            _clientCertificateFileTextField=root.Q<TextField>("ClientCertificateFileTextField");
            _clientKeyFileTextField=root.Q<TextField>("ClientKeyFileTextField");
            _cloudModeConnectionDropdownField=root.Q<DropdownField>("CloudModeConnectionDropdownField");
            
            _cloudConnectButton=root.Q<Button>("CloudConnectButton");
            _cloudDisconnectButton=root.Q<Button>("CloudDisconnectButton");
            _cloudStatusLabel=root.Q<Label>("CloudStatusLabel");
            _cloudPortTextField=root.Q<TextField>("CloudPortTextField");
            _cloudPfxFilePathTextField=root.Q<TextField>("CloudPfxFilePathTextField");
            _cloudTestMessageButton=root.Q<Button>("CloudTestMessageButton");
            
            _menuButton=root.Q<Button>("MenuButton");
            _hideMenuButton=root.Q<Button>("HideMenuButton");
            _navigationMenuPanel=root.Q<VisualElement>("NavigationMenuPanel");
            _subpanelsAndSmokeMaskContainer=root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _scrim=root.Q<VisualElement>("Scrim");
            _dashboardButton= root.Q<Button>("DashboardButton");
            
            _cloudVmIoTipOrHostnameTextField= root.Q<TextField>("CloudVMIoTIPOrHostnameTextField");
            _cloudVmIoTPortTextField= root.Q<TextField>("CloudVMIoTPortTextField");
            _cloudVmIoTClientIDTextField= root.Q<TextField>("CloudVMIoTClientIDTextField");
            _cloudVmIoTModeConnectionDropdownField= root.Q<DropdownField>("CloudVMIoTModeConnectionDropdownField");
            _cloudVmIoTUsernameTextField= root.Q<TextField>("CloudVMIoTUsernameTextField");
            _cloudVmIoTPasswordTextField= root.Q<TextField>("CloudVMIoTPasswordTextField");
            _connectCloudVmIoTButton= root.Q<Button>("ConnectCloudVMIoTButton");
            _disconnectCloudVmIoTButton= root.Q<Button>("DisconnectCloudVMIoTButton");
            _testMessageCloudVmIoTButton= root.Q<Button>("TestMessageCloudVMIoTButton");
            _statusLocalCloudVmIoTLabel= root.Q<Label>("StatusLocalCloudVMIoTLabel");
            
        }
        
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
        
        private async void ConnectToBroker(ClickEvent evt)
        {
            try
            {
                var result = await _localIoT.ConnectToBroker();
                _dashboardController.localIoTStatusLabel.text = _statusLocalLabel.text = result ? "Online" : "Offline";
                if (_dashboardController.localIoTStatusLabel.text=="Online")
                {
                    _dashboardController.localIoTStatusLabel.style.color=Color.green;
                }
                else
                {
                    _dashboardController.localIoTStatusLabel.style.color=Color.red;
                }
            }
            catch (Exception ex)
            {
                _dashboardController.localIoTStatusLabel.text =_statusLocalLabel.text = "Error connection: " + ex.Message;
                _dashboardController.localIoTStatusLabel.style.color=Color.red;
            }
        }
        
        private async void ConnectToBroker()
        {
            try
            {
                var result = await _localIoT.ConnectToBroker();
                _dashboardController.localIoTStatusLabel.text=_statusLocalLabel.text = result ? "Online" : "Offline";
                if (_dashboardController.localIoTStatusLabel.text=="Online")
                {
                    _dashboardController.localIoTStatusLabel.style.color=Color.green;
                }
                else
                {
                    _dashboardController.localIoTStatusLabel.style.color=Color.red;
                }
            }
            catch (Exception ex)
            {
                _dashboardController.localIoTStatusLabel.text= _statusLocalLabel.text = "Error connection: " + ex.Message;
                _dashboardController.localIoTStatusLabel.style.color=Color.red;
            }
        }
        

        private async void DisconnectFromBroker(ClickEvent evt)
        {
            try
            {
                var result = await _localIoT.DisconnectFromBroker();
                _dashboardController.localIoTStatusLabel.text=_statusLocalLabel.text = result ? "Offline" : "Client not connected";
                if (_dashboardController.localIoTStatusLabel.text == "Offline")
                {
                    _dashboardController.localIoTStatusLabel.style.color=Color.red;
                }else if (_dashboardController.localIoTStatusLabel.text == "Client not connected")
                {
                    _dashboardController.localIoTStatusLabel.style.color=Color.yellow;
                }
            }
            catch (Exception ex)
            {
                _dashboardController.localIoTStatusLabel.text=_statusLocalLabel.text = "Error disconnect: " + ex.Message;
                _dashboardController.localIoTStatusLabel.style.color=Color.red;
            }
        }
        private void TestMessageLocal(ClickEvent evt)
        {
            _ = _localIoT.SendTestMessage();
        }
        private async void ConnectToAwsIotCore(ClickEvent evt)
        {
            var isConnected = await _cloudIoT.ConnectToAwsIoT();

            _dashboardController.cloudIoTStatusLabel.text=_cloudStatusLabel.text = isConnected ? "Online" : "Offline";
            if (_dashboardController.cloudIoTStatusLabel.text=="Online")
            {
                _dashboardController.cloudIoTStatusLabel.style.color=Color.green;
            }
            else
            {
                _dashboardController.cloudIoTStatusLabel.style.color=Color.red;
            }
        }
        
        private async void ConnectToAwsIotCore()
        {
            var isConnected = await _cloudIoT.ConnectToAwsIoT();

            _dashboardController.cloudIoTStatusLabel.text=_cloudStatusLabel.text = isConnected ? "Online" : "Offline";
            if (_dashboardController.cloudIoTStatusLabel.text=="Online")
            {
                _dashboardController.cloudIoTStatusLabel.style.color=Color.green;
            }
            else
            {
                _dashboardController.cloudIoTStatusLabel.style.color=Color.red;
            }
        }

        private async void DisconnectFromAwsIotCore(ClickEvent evt)
        {
            var isDisconnected = await _cloudIoT.DisconnectFromAwsIoT();
            _dashboardController.cloudIoTStatusLabel.text=_cloudStatusLabel.text = isDisconnected ? "Offline" : "Client not connected";
            if (_dashboardController.cloudIoTStatusLabel.text == "Offline")
            {
                _dashboardController.cloudIoTStatusLabel.style.color=Color.red;
            }else if (_dashboardController.cloudIoTStatusLabel.text == "Client not connected")
            {
                _dashboardController.cloudIoTStatusLabel.style.color=Color.yellow;
            }
        }
        
        private void TestMessageAwsIotCore(ClickEvent evt)
        {
            _ = _cloudIoT.SendTestMessage();
        }
        
        private void ConnectToVM(ClickEvent evt)
        {
            print("si recibi el evento");
            _ = _vmIoT.ConnectToEC2Broker();
        }
        private void ConnectToVM()
        {
            _ = _vmIoT.ConnectToEC2Broker();
        }
        private void DisconnectFromVM(ClickEvent evt)
        {
            _ = _vmIoT.DisconnectFromEC2Broker();
        }

        private void TestMessageVM(ClickEvent evt)
        {
            _ = _vmIoT.SendTestMessageToEC2();
        }
        private void StartDashboard(ClickEvent evt)
        {
            HideMenu(evt);
            _body.style.display = DisplayStyle.None;
            _dashboardController.ShowUi();
        }
        
        
    }
}
