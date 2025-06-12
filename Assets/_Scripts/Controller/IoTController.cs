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
        private TextField _usernameTextField;
        private TextField _passwordTextField;
        private Button _testLocalButton;
        private Button _connectLocalButton;
        private Button _disconnectLocalButton;
        private Label _statusLocalLabel;
        
        
        
        
        private void Awake()
        {
            GetUiComponents();
            RegisterEvents();
            FindObjects();
        }
        
        private void Start()
        {
            HideUi();
            _usernameTextField.style.display = DisplayStyle.None;
            _passwordTextField.style.display = DisplayStyle.None;
            _connectionTypeDropdownField.value = "Connection type";
            _modeConnectionDropdownField.value = "Mode connection";
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
        }

        private void RegisterEvents()
        {
            

        }

        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _connectionTypeDropdownField=root.Q<DropdownField>("ConnectionTypeDropdownField");
            _modeConnectionDropdownField=root.Q<DropdownField>("ModeConnectionDropdownField");
            _usernameTextField=root.Q<TextField>("UsernameTextField");
            _passwordTextField=root.Q<TextField>("PasswordTextField");
            _testLocalButton=root.Q<Button>("TestLocalButton");
            _connectLocalButton=root.Q<Button>("ConnectLocalButton");
            _disconnectLocalButton=root.Q<Button>("DisconnectLocalButton");
            _statusLocalLabel=root.Q<Label>("StatusLocalLabel");
        }
    }
}
