using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace _Scripts.Controllers
{
    public class DashboardController: MonoBehaviour
    {
        private UIDocument dashboardUIDocument;
        private VisualElement dashboardRoot;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            dashboardUIDocument = FindUIDocument("Dashboard");
            if (dashboardUIDocument != null)
            {
                dashboardRoot = dashboardUIDocument.rootVisualElement;
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
            if (dashboardRoot == null && dashboardUIDocument != null)
            {
                dashboardRoot = dashboardUIDocument.rootVisualElement;
            }
            HideUi();
        }

        private void ConfigureUIElements()
        {
            if (dashboardRoot != null)
            {
                var virtualSpaceButton = dashboardRoot.Q<Button>("VirtualSpaceButton");
                if (virtualSpaceButton != null)
                {
                    virtualSpaceButton.clicked += StartVirtualSpace;
                }       
                var augmentedRealityButton = dashboardRoot.Q<Button>("AugmentedRealityButton");
                if (augmentedRealityButton != null)
                {
                    augmentedRealityButton.clicked += StartAugmentedReality;
                }    
                var settingsButton = dashboardRoot.Q<Button>("SettingsButton");
                if (settingsButton != null)
                {
                    settingsButton.clicked += ShowSettings;
                }               
                var systemLogsButton = dashboardRoot.Q<Button>("SystemLogsButton");
                if (systemLogsButton != null)
                {
                    systemLogsButton.clicked += StartSystemLogs;
                }        
                var supportAndHelpButton = dashboardRoot.Q<Button>("SupportAndHelpButton");
                if (supportAndHelpButton != null)
                {
                    supportAndHelpButton.clicked += ShowSupportAndHelp;
                }       
                var exitButton = dashboardRoot.Q<Button>("ExitButton");
                if (exitButton != null)
                {
                    exitButton.clicked += QuitApplication;
                }          
                var logoutButton = dashboardRoot.Q<Button>("LogoutButton");
                if (logoutButton != null)
                {
                    logoutButton.clicked += Logout;
                }
            }
        }

        internal void ShowUi()
        {
            if (dashboardRoot == null) return;
            dashboardRoot.style.display = DisplayStyle.Flex;
        }
        private void HideUi()
        {
            if (dashboardRoot == null) return;
            dashboardRoot.style.display = DisplayStyle.None;
        }
        
        
        private void StartVirtualSpace()
        {
            SceneManager.LoadScene($"VirtualSpace");
        }
        
        
        private void StartAugmentedReality()
        {
            HideUi();
            SceneManager.LoadScene($"AugmentedReality");
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
        
        private void StartSystemLogs()
        {
            HideUi();
            var systemLogsController = FindAnyObjectByType<SystemLogsController>();
            if (systemLogsController != null)
            {
                systemLogsController.ShowUi(); 
            }
        }
        
        
        private void ShowSupportAndHelp()
        {
            HideUi();
            var supportAndHelpController = FindAnyObjectByType<SupportAndHelpController>();
            if (supportAndHelpController != null)
            {
                supportAndHelpController.ShowUi(); 
            }
        }
        
        
        private void QuitApplication()
        {
            Application.Quit();
        }
        
        private void Logout()
        {
            print("Logout");
        }


        private UIDocument FindUIDocument(string nameUiDocument)
        {
            var uiDocument = GameObject.Find(nameUiDocument)?.GetComponent<UIDocument>();
            return uiDocument;
        }
    }
}
