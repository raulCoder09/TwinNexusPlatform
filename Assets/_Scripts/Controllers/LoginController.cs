using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers
{
    public class LoginController : MonoBehaviour
    {
        private UIDocument loginUIDocument;
        private VisualElement loginRoot;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            loginUIDocument = FindUIDocument("Login");
            if (loginUIDocument != null)
            {
                loginRoot = loginUIDocument.rootVisualElement;
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
            if (loginRoot == null && loginUIDocument != null)
            {
                loginRoot = loginUIDocument.rootVisualElement;
            }
            HideUi();
        
        }

        private void ConfigureUIElements()
        {
            if (loginRoot != null)
            {
                var loginButton = loginRoot.Q<Button>("LoginButton");
                if (loginButton != null)
                {
                    loginButton.clicked += StartSession;
                }
                
                var guestButton = loginRoot.Q<Button>("GuestButton");
                if (guestButton != null)
                {
                    guestButton.clicked += ShowDashboard;
                }
                var cancelButton = loginRoot.Q<Button>("CancelButton");
                if (cancelButton != null)
                {
                    cancelButton.clicked += ShowWelcome;
                }
            }
        }


        internal void ShowUi()
        {
            if (loginRoot == null) return;
            loginRoot.style.display = DisplayStyle.Flex;
        }
        private void HideUi()
        {
            if (loginRoot == null) return;
            loginRoot.style.display = DisplayStyle.None;
        }

        private void StartSession()
        {
            print("StartSession");
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
        private void ShowWelcome()
        {
            HideUi();
            var welcomeController = FindAnyObjectByType<WelcomeController>();
            if (welcomeController != null)
            {
                welcomeController.ShowUi(); 
            }
            
        }

        private UIDocument FindUIDocument(string nameUiDocument)
        {
            var uiDocument = GameObject.Find(nameUiDocument)?.GetComponent<UIDocument>();
            return uiDocument;
        }
    }
}
