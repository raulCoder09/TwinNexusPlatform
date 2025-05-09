using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class JogAndTeachController : MonoBehaviour
    {
        private VisualElement _body;
        private Button _menuButton;
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private VisualElement _scrim;
        private VisualElement _navigationMenuPanel;
        private Button _hideMenuButton;
        private DropdownField _menuEnvironmentDropdownField;
        [SerializeField] private GameObject[] environments;
        private Button _logoutButton;
        
        private void Awake()
        {
            var root=GetComponent<UIDocument>().rootVisualElement;
            _body=root.Q<VisualElement>("Body");
            _menuButton=root.Q<Button>("MenuButton");
            _menuButton.RegisterCallback<ClickEvent>(ShowMenu);
            _subpanelsAndSmokeMaskContainer=root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _scrim=root.Q<VisualElement>("Scrim");
            _navigationMenuPanel=root.Q<VisualElement>("NavigationMenuPanel");
            _hideMenuButton=root.Q<Button>("HideMenuButton");
            _hideMenuButton.RegisterCallback<ClickEvent>(HideMenu);
            _navigationMenuPanel.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);
            _menuEnvironmentDropdownField=root.Q<DropdownField>("MenuEnvironmentDropdownField");
            _menuEnvironmentDropdownField.RegisterValueChangedCallback(evt => {
                EnableEnvironment(evt.newValue);
            });
            _logoutButton=root.Q<Button>("LogoutButton");
            _logoutButton.RegisterCallback<ClickEvent>(BorrarEsteMetodo);
        }

        private void BorrarEsteMetodo(ClickEvent evt)
        {
            SceneManager.LoadScene("Main");
        }

        private void EnableEnvironment(string evtNewValue)
        {
            switch (evtNewValue)
            {
                case "Virtual  environment":
                    _body.style.backgroundColor = new Color(0, 0, 0, 0);
                    environments[0].SetActive(true);
                    environments[1].SetActive(false);
                    environments[2].SetActive(false);
                    environments[3].SetActive(false);
                    break;
                case "Augmented reality environment":
                    _body.style.backgroundColor = new Color(0, 0, 0, 0);
                    environments[0].SetActive(false);
                    environments[1].SetActive(true);
                    environments[2].SetActive(false);
                    environments[3].SetActive(false);
                    break;
                case "Hybrid environment":
                    _body.style.backgroundColor = new Color(0, 0, 0, 0);
                    environments[0].SetActive(false);
                    environments[1].SetActive(false);
                    environments[2].SetActive(true);
                    environments[3].SetActive(false);
                    break;
                case "Real device environment":
                    _body.style.backgroundColor = new Color(0, 0, 0, 0);
                    environments[0].SetActive(false);
                    environments[1].SetActive(false);
                    environments[2].SetActive(false);
                    environments[3].SetActive(true);
                    break;
                default:
                    print("opcion no disponible");
                    break;
            }
        }


        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            if (!_navigationMenuPanel.ClassListContains("NavigationMenuPanelInMainScreen"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }

        private void ShowMenu(ClickEvent evt)
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.Flex;
            _scrim.AddToClassList("Opaque");
            _navigationMenuPanel.AddToClassList("NavigationMenuPanelInMainScreen");
        }

        private void HideMenu(ClickEvent evt)
        {
            _scrim.RemoveFromClassList("Opaque");
            _navigationMenuPanel.RemoveFromClassList("NavigationMenuPanelInMainScreen");
        }

        private void Start()
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
        }
    }
}
