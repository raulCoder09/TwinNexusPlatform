using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers
{
    public class SupportAndHelpController: MonoBehaviour
    {
        private UIDocument supportAndHelpUIDocument;
        private VisualElement supportAndHelpRoot;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            supportAndHelpUIDocument = FindUIDocument("SupportAndHelp");
            if (supportAndHelpUIDocument != null)
            {
                supportAndHelpRoot = supportAndHelpUIDocument.rootVisualElement;
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
            if (supportAndHelpRoot == null && supportAndHelpUIDocument != null)
            {
                supportAndHelpRoot = supportAndHelpUIDocument.rootVisualElement;
            }
            HideUi();
        
        }
        private void Update()
        {
        
        }

        private void ConfigureUIElements()
        {
            if (supportAndHelpRoot != null)
            {
                var backButton = supportAndHelpRoot.Q<Button>("BackButton");
                if (backButton != null)
                {
                    backButton.clicked += ShowDashboard;
                }            
                var openDocsButton = supportAndHelpRoot.Q<Button>("OpenDocsButton");
                if (openDocsButton != null)
                {
                    openDocsButton.clicked += ShowDocumentation;
                }       
                var sendButton = supportAndHelpRoot.Q<Button>("SendButton");
                if (sendButton != null)
                {
                    sendButton.clicked += SendSupportAndHelp;
                } 
            }
        }
        
        internal void ShowUi()
        {
            if (supportAndHelpRoot == null) return;
            supportAndHelpRoot.style.display = DisplayStyle.Flex;
        }
        private void HideUi()
        {
            if (supportAndHelpRoot == null) return;
            supportAndHelpRoot.style.display = DisplayStyle.None;
        }
        private void SendSupportAndHelp()
        {
            print("SendSupportAndHelp");
        }
        private void ShowDocumentation()
        {
            print("ShowDocumentation");
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
        


        private UIDocument FindUIDocument(string nameUiDocument)
        {
            var uiDocument = GameObject.Find(nameUiDocument)?.GetComponent<UIDocument>();
            return uiDocument;
        }
    }
}
