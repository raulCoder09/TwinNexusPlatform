using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers
{
    public class WelcomeController : MonoBehaviour
    {
        private UIDocument welcomeUIDocument;
        private VisualElement welcomeRoot;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            welcomeUIDocument = FindUIDocument("Welcome");
            if (welcomeUIDocument != null)
            {
                welcomeRoot = welcomeUIDocument.rootVisualElement;
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
            if (welcomeRoot == null && welcomeUIDocument != null)
            {
                welcomeRoot = welcomeUIDocument.rootVisualElement;
            }
            ShowUi();
        }
        
        private void ConfigureUIElements()
        {
            if (welcomeRoot != null)
            {
                var exitButton = welcomeRoot.Q<Button>("ExitButton");
                if (exitButton != null)
                {
                    exitButton.clicked += QuitApplication;
                }
                
                var launchButton = welcomeRoot.Q<Button>("LaunchButton");
                if (launchButton != null)
                {
                    launchButton.clicked += LaunchApplication;
                }
            }
        }

        internal void ShowUi()
        {
            if (welcomeRoot == null) return;
            welcomeRoot.style.display = DisplayStyle.Flex;
        }
        private void HideUi()
        {
            if (welcomeRoot == null) return;
            welcomeRoot.style.display = DisplayStyle.None;
        }
        
        private void LaunchApplication()
        {
            HideUi();
            var loginController = FindAnyObjectByType<LoginController>();
            if (loginController != null)
            {
                loginController.ShowUi(); 
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
