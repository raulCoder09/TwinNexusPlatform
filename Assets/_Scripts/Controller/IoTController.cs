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
        private SaveSystem _saveSystem;
        
        
        
        
        [SerializeField] private IotConfigurationData iotConfigurationData;
        
        private void Awake()
        {
            GetUiComponents();
            RegisterEvents();
            FindObjects();
        }
        
        private void Start()
        {
            HideUi();
            _saveSystem.LoadData();
            _ipOrHostnameTextField.value= iotConfigurationData.brokerAddress;
            _portTextField.value = iotConfigurationData.BrokerPort.ToString();
            _clientIDTextField.value=iotConfigurationData.ClientId;
            _usernameTextField.value=iotConfigurationData.Username;
            _passwordTextField.value=iotConfigurationData.Password;
            _modeConnectionDropdownField.value = iotConfigurationData.ModeConnection;

            _cloudPfxFilePathTextField.value = iotConfigurationData.pfxFilePath;
            _endpointTextField.value=iotConfigurationData.endpoint;
            _cloudThingNameTextField.value = iotConfigurationData.thingName;
            _caFileTextField.value = iotConfigurationData.caFilePath;
            _clientCertificateFileTextField.value = iotConfigurationData.clientCertPath;
            _clientKeyFileTextField.value = iotConfigurationData.clientKeyPath;
            _cloudPortTextField.value = iotConfigurationData.cloudPort;
            _cloudPfxFilePathTextField.value = iotConfigurationData.pfxFilePath;
            _cloudModeConnectionDropdownField.value=iotConfigurationData.cloudModeConnection;
                
            if (_modeConnectionDropdownField.value=="Automatic connection")
            {
                ConnectToBroker();
            }

            if (_cloudModeConnectionDropdownField.value == "Automatic connection")
            {
                print("conectando con AWS");
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
            _saveSystem=GameObject.FindGameObjectWithTag("GameManager").GetComponent<SaveSystem>();
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
            
            
            
            _connectLocalButton.RegisterCallback<ClickEvent>(ConnectToBroker);
            _disconnectLocalButton.RegisterCallback<ClickEvent>(DisconnectFromBroker);
            _cloudConnectButton.RegisterCallback<ClickEvent>(ConnectToAwsIotCore);
            _cloudDisconnectButton.RegisterCallback<ClickEvent>(DisconnectFromAwsIotCore);
            _cloudTestMessageButton.RegisterCallback<ClickEvent>(TestMessageAwsIotCore);
            _testMessageButton.RegisterCallback<ClickEvent>(TestMessageLocal);
            
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
            
            
        }
        
        private async void ConnectToBroker(ClickEvent evt)
        {
            try
            {
                var result = await _localIoT.ConnectToBroker();
                _statusLocalLabel.text = result ? "Status: Online" : "Status: Offline";
            }
            catch (Exception ex)
            {
                _statusLocalLabel.text = "Error connection: " + ex.Message;
            }
        }
        
        private async void ConnectToBroker()
        {
            try
            {
                var result = await _localIoT.ConnectToBroker();
                _statusLocalLabel.text = result ? "Status: Online" : "Status: Offline";
            }
            catch (Exception ex)
            {
                _statusLocalLabel.text = "Error connection: " + ex.Message;
            }
        }
        

        private async void DisconnectFromBroker(ClickEvent evt)
        {
            try
            {
                var result = await _localIoT.DisconnectFromBroker();
                _statusLocalLabel.text = result ? "Status: Offline" : "Client not connected";
            }
            catch (Exception ex)
            {
                _statusLocalLabel.text = "Error disconnect: " + ex.Message;
            }
        }
        private void TestMessageLocal(ClickEvent evt)
        {
            _ = _localIoT.SendTestMessage();
        }
        private async void ConnectToAwsIotCore(ClickEvent evt)
        {
            var isConnected = await _cloudIoT.ConnectToAwsIoT();

            _cloudStatusLabel.text = isConnected ? "Status: Online" : "Status: Offline";
        }

        private async void DisconnectFromAwsIotCore(ClickEvent evt)
        {
            var isDisconnected = await _cloudIoT.DisconnectFromAwsIoT();
            _cloudStatusLabel.text = isDisconnected ? "Status: Offline" : "Client not connected";
        }
        
        private void TestMessageAwsIotCore(ClickEvent evt)
        {
            _ = _cloudIoT.SendTestMessage();
        }
    }
}
