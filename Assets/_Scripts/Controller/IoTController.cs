using System;
using _Scripts.Models;
using UnityEngine;
using UnityEngine.UIElements;


namespace _Scripts.Controller
{
    public class IoTController : MonoBehaviour
    {
        private VisualElement _body;
        private TextField _ipOrHostnameTextField;
        private TextField _portTextField;
        private TextField _clientIDTextField;
        private DropdownField _connectionTypeDropdownField;
        private DropdownField _modeConnectionDropdownField;
        private DropdownField _protocolCommunicationDropdownField;
        private TextField _usernameTextField;
        private TextField _passwordTextField;
        private Button _testLocalButton;
        private Button _connectLocalButton;
        private Button _disconnectLocalButton;
        private Label _statusLocalLabel;
        private MQTTProtocol _mqttProtocol;
        
        
        private void Awake()
        {
            GetUiComponents();
            RegisterEvents();
            FindObjects();
        }
        
        private void Start()
        {
            HideUi();
            _connectionTypeDropdownField.value = "Connection type";
            _modeConnectionDropdownField.value = "Mode connection";
            _protocolCommunicationDropdownField.value = "Protocol communication";
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
            _mqttProtocol = GameObject.FindGameObjectWithTag("MQTTProtocol").GetComponent<MQTTProtocol>();
        }
        
        private void RegisterEvents()
        {
            _ipOrHostnameTextField.RegisterValueChangedCallback(evt =>
            {
                _mqttProtocol.brokerAddress = evt.newValue;
            });
            
            _portTextField.RegisterValueChangedCallback(evt =>
            {
                int.TryParse(evt.newValue, out var port);
                _mqttProtocol.brokerPort =port;
            });
            
            _clientIDTextField.RegisterValueChangedCallback(evt =>
            {
                _mqttProtocol.clientId = evt.newValue;
            });
            _connectionTypeDropdownField.RegisterValueChangedCallback(evt =>
            {
                
            });
            _modeConnectionDropdownField.RegisterValueChangedCallback(evt =>
            {
                
            });
            
            _protocolCommunicationDropdownField.RegisterValueChangedCallback(evt =>
            {
            });
            _usernameTextField.RegisterValueChangedCallback(evt =>
            {
                _mqttProtocol.username = evt.newValue;
            });
            _passwordTextField.RegisterValueChangedCallback(evt =>
            {
                _mqttProtocol.password = evt.newValue;
            });
            
            _connectLocalButton.RegisterCallback<ClickEvent>(ConnectToBroker);
            _disconnectLocalButton.RegisterCallback<ClickEvent>(DisconnectFromBroker);
                
        }

        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _ipOrHostnameTextField= root.Q<TextField>("IPOrHostnameTextField");
            _portTextField= root.Q<TextField>("PortTextField");
            _clientIDTextField= root.Q<TextField>("ClientIDTextField");
            _connectionTypeDropdownField=root.Q<DropdownField>("ConnectionTypeDropdownField");
            _modeConnectionDropdownField=root.Q<DropdownField>("ModeConnectionDropdownField");
            _protocolCommunicationDropdownField=root.Q<DropdownField>("ProtocolCommunicationDropdownField");
            _usernameTextField=root.Q<TextField>("UsernameTextField");
            _passwordTextField=root.Q<TextField>("PasswordTextField");
            _testLocalButton=root.Q<Button>("TestLocalButton");
            _connectLocalButton=root.Q<Button>("ConnectLocalButton");
            _disconnectLocalButton=root.Q<Button>("DisconnectLocalButton");
            _statusLocalLabel=root.Q<Label>("StatusLocalLabel");
        }
        
 
        
        private async void ConnectToBroker(ClickEvent evt)
        {
            try
            {
                var result = await _mqttProtocol.ConnectToBroker();
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
                var result = await _mqttProtocol.DisconnectFromBroker();
                _statusLocalLabel.text = result ? "Status: Offline" : "The client is not connected";
            }
            catch (Exception ex)
            {
                _statusLocalLabel.text = "Error disconnect: " + ex.Message;
            }
        }
    }
}
