using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers
{
    public class ARGeneralUiController : MonoBehaviour
    {
        private UIDocument arGeneralUiDocument;
        private VisualElement arGeneralUiRoot;
        private ARVisibilityPlanePointsController arVisibilityModel;

        private void Awake()
        {
            arGeneralUiDocument = FindUIDocument("ARGeneralUi");
            if (arGeneralUiDocument != null)
            {
                arGeneralUiRoot = arGeneralUiDocument.rootVisualElement;
            }

            arVisibilityModel = FindAnyObjectByType<ARVisibilityPlanePointsController>();
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
            if (arGeneralUiRoot == null && arGeneralUiDocument != null)
            {
                arGeneralUiRoot = arGeneralUiDocument.rootVisualElement;
            }
            ShowUi();
        }

        private void ConfigureUIElements()
        {
            if (arGeneralUiRoot != null)
            {
                var pointsButton = arGeneralUiRoot.Q<Button>("Points");
                if (pointsButton != null)
                {
                    pointsButton.clicked += () => arVisibilityModel?.TogglePointCloudVisibility();
                }

                var planesButton = arGeneralUiRoot.Q<Button>("Planes");
                if (planesButton != null)
                {
                    planesButton.clicked += () => arVisibilityModel?.TogglePlaneVisibility();
                }
                
                var dashboardButton = arGeneralUiRoot.Q<Button>("DashboardButton");
                if (dashboardButton != null)
                {
                    dashboardButton.clicked += ShowDashboard;
                }

                var exitButton = arGeneralUiRoot.Q<Button>("ExitButton");
                if (exitButton != null)
                {
                    exitButton.clicked += QuitApplication;
                }
            }
        }

        internal void ShowUi()
        {
            if (arGeneralUiRoot == null) return;
            arGeneralUiRoot.style.display = DisplayStyle.Flex;
        }

        private void HideUi()
        {
            if (arGeneralUiRoot == null) return;
            arGeneralUiRoot.style.display = DisplayStyle.None;
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
