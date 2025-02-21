using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers
{
    public class SystemLogsController: MonoBehaviour
    {
        private UIDocument systemLogsUIDocument;
        private VisualElement systemLogsRoot;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            systemLogsUIDocument = FindUIDocument("SystemLogs");
            if (systemLogsUIDocument != null)
            {
                systemLogsRoot = systemLogsUIDocument.rootVisualElement;
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
            if (systemLogsRoot == null && systemLogsUIDocument != null)
            {
                systemLogsRoot = systemLogsUIDocument.rootVisualElement;
            }
            HideUi();
        
        }
        private void Update()
        {
        
        }

        private void ConfigureUIElements()
        {
            if (systemLogsRoot != null)
            {
                var filterButton = systemLogsRoot.Q<Button>("FilterButton");
                if (filterButton != null)
                {
                    filterButton.clicked += FilerLogs;
                }      
                var exportButton = systemLogsRoot.Q<Button>("ExportButton");
                if (exportButton != null)
                {
                    exportButton.clicked += ExportLogs;
                }           
                var dashboardButton = systemLogsRoot.Q<Button>("DashboardButton");
                if (dashboardButton != null)
                {
                    dashboardButton.clicked += ShowDashboard;
                }         
                var exitButton = systemLogsRoot.Q<Button>("ExitButton");
                if (exitButton != null)
                {
                    exitButton.clicked += QuitApplication;
                } 
            }
        }


        internal void ShowUi()
        {
            if (systemLogsRoot == null) return;
            systemLogsRoot.style.display = DisplayStyle.Flex;
        }
        private void HideUi()
        {
            if (systemLogsRoot == null) return;
            systemLogsRoot.style.display = DisplayStyle.None;
        }
        private void FilerLogs()
        {
           print("FilerLogs");
        }
        private void ExportLogs()
        {
            print("ExportLogs");
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
