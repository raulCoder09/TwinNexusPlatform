using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers
{
    public class SettingsController: MonoBehaviour
    {
        private UIDocument settingsUIDocument;
        private VisualElement settingsRoot;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            settingsUIDocument = FindUIDocument("Settings");
            if (settingsUIDocument != null)
            {
                settingsRoot = settingsUIDocument.rootVisualElement;
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
            if (settingsRoot == null && settingsUIDocument != null)
            {
                settingsRoot = settingsUIDocument.rootVisualElement;
            }
            HideUi();
        
        }

        private void ConfigureUIElements()
        {
            if (settingsRoot != null)
            {
                var logoutButton = settingsRoot.Q<Button>("LogoutButton");
                if (logoutButton != null)
                {
                    logoutButton.clicked += Logout;
                }       
                var ioTGatewayButton = settingsRoot.Q<Button>("IoTGatewayButton");
                if (ioTGatewayButton != null)
                {
                    ioTGatewayButton.clicked += StartIoTGateway;
                } 
                var dashboardButton = settingsRoot.Q<Button>("DashboardButton");
                if (dashboardButton != null)
                {
                    dashboardButton.clicked += ShowDashboard;
                } 
                var exitButton = settingsRoot.Q<Button>("ExitButton");
                if (exitButton != null)
                {
                    exitButton.clicked += QuitApplication;
                } 
                
            }
        }

        internal void ShowUi()
        {
            if (settingsRoot == null) return;
            settingsRoot.style.display = DisplayStyle.Flex;
        }
        private void HideUi()
        {
            if (settingsRoot == null) return;
            settingsRoot.style.display = DisplayStyle.None;
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
        
        private void StartIoTGateway()
        {
            HideUi();
            var ioTGatewayController = FindAnyObjectByType<IoTGatewayController>();
            if (ioTGatewayController != null)
            {
                ioTGatewayController.ShowUi(); 
            }
        }
        private void Logout()
        {
            print("Logout");
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
