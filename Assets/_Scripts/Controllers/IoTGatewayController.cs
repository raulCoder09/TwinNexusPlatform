using _Scripts.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers
{
    public class IoTGatewayController: MonoBehaviour
    {
        private UIDocument ioTGatewayUIDocument;
        private VisualElement ioTGatewayRoot;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ioTGatewayUIDocument = FindUIDocument("IoTGateway");
            if (ioTGatewayUIDocument != null)
            {
                ioTGatewayRoot = ioTGatewayUIDocument.rootVisualElement;
            }
        }

        private void OnEnable()
        {
            ConfigureUIElements();
        }

        private void OnDisable()
        {
            HideUi();
        }

        private void Start()
        {
            HideUi();
            IoTGatewayModel.Instance.OnConnected += () => UpdateStatusLabel("Connected");
            IoTGatewayModel.Instance.OnDisconnected += () => UpdateStatusLabel("Disconnected");

            if (ioTGatewayRoot == null && ioTGatewayUIDocument != null)
            {
                ioTGatewayRoot = ioTGatewayUIDocument.rootVisualElement;
            }
        }
        private void ConfigureUIElements()
        {
            if (ioTGatewayRoot != null)
            {
                var saveButton = ioTGatewayRoot.Q<Button>("SaveButton");
                if (saveButton != null)
                {
                    saveButton.clicked += SavePreferences;
                }    
                var testButton = ioTGatewayRoot.Q<Button>("TestButton");
                if (testButton != null)
                {
                    testButton.clicked += TestConnection;
                }               
                var disconnectButton = ioTGatewayRoot.Q<Button>("DisconnectButton");
                if (disconnectButton != null)
                {
                    disconnectButton.clicked += DisconnectServer;
                }   
                var backButton = ioTGatewayRoot.Q<Button>("BackButton");
                if (backButton != null)
                {
                    backButton.clicked += ShowSettings;
                }       
                var dashboardButton = ioTGatewayRoot.Q<Button>("DashboardButton");
                if (dashboardButton != null)
                {
                    dashboardButton.clicked += ShowDashboard;
                }       
                var exitButton = ioTGatewayRoot.Q<Button>("ExitButton");
                if (exitButton != null)
                {
                    exitButton.clicked += QuitApplication;
                }
            }
        }


        
        private void UpdateStatusLabel(string status)
        {
            var statusLabel = ioTGatewayRoot?.Q<Label>("StatusLabel");
            if (statusLabel != null)
                statusLabel.text = $"Status: {status}";
        }
        internal void ShowUi()
        {
            if (ioTGatewayRoot == null) return;
            ioTGatewayRoot.style.display = DisplayStyle.Flex;
        }
        private void HideUi()
        {
            if (ioTGatewayRoot == null) return;
            ioTGatewayRoot.style.display = DisplayStyle.None;
        }
        private void DisconnectServer()
        {
            IoTGatewayModel.Instance.Disconnect();
            UpdateStatusLabel("Disconnected");
        }
        private void TestConnection()
        {
            IoTGatewayModel.Instance.Connect();
            UpdateStatusLabel("Connected");
        }
        private void SavePreferences()
        {
                var brokerAddressField = ioTGatewayRoot.Q<TextField>("BrokerAddressField");
                var brokerPortField = ioTGatewayRoot.Q<TextField>("BrokerPortField");
                var clientIdField    = ioTGatewayRoot.Q<TextField>("ClientIdField");
                var usernameField    = ioTGatewayRoot.Q<TextField>("UsernameField");
                var passwordField    = ioTGatewayRoot.Q<TextField>("PasswordField");
                
                var brokerAddress = brokerAddressField?.value ?? "localhost";
                int.TryParse(brokerPortField?.value, out int brokerPort); // Manejar error si no es entero
                var clientId = clientIdField?.value ?? "UnityClient";
                var username = usernameField?.value;
                var password = passwordField?.value;
                
                IoTGatewayModel.Initialize(brokerAddress, brokerPort, clientId, username, password);

                Debug.Log($"Prefs guardados: {brokerAddress}:{brokerPort}, ClientId={clientId}");
            
        }
        private void ShowSettings()
        {
            HideUi();
            var settingsController = FindAnyObjectByType<SettingsController>();
            if (settingsController != null)
            {
                settingsController.ShowUi(); 
            }
        }
        private void ShowDashboard()
        {
            HideUi();
            var dashboardController = FindAnyObjectByType<DashboardController>();
            if (dashboardController != null)
            {
                dashboardController.ShowUi(); 
            }
        }
        private void QuitApplication()
        {
            Application.Quit();
        }

        private UIDocument FindUIDocument(string nameUiDocument)
        {
            var uiDocument = GameObject.Find(nameUiDocument)?.GetComponent<UIDocument>();
            return uiDocument;
        }
    }
}
