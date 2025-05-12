using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class JogAndTeachController : MonoBehaviour
    {
        [SerializeField] private GameObject[] environments;
        
        private ControlPanelController _controlPanelController;
        private GameManager _gameManager;
        
        private Button _menuButton;
        private Button _hideMenuButton;
        private Button _logoutButton;
        
        private VisualElement _body;
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private VisualElement _scrim;
        private VisualElement _navigationMenuPanel;
        
        private DropdownField _menuEnvironmentDropdownField;
        private DropdownField _menuRobotArscaraDropdownField;
        
        public DropdownField menuEnvironmentDropdownField
        {
            get => _menuEnvironmentDropdownField;
            set => _menuEnvironmentDropdownField = value;
        }
        internal DropdownField menuRobotArscaraDropdownField
        {
            get => _menuRobotArscaraDropdownField;
            set => _menuRobotArscaraDropdownField = value;
        }
        private void Awake()
        {
            GetUiComponents();
            RegisterEvents();
            FindObjects();
        }
        private void Start()
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            HideUI();
        }
        private void BorrarEsteMetodo(ClickEvent evt)
        {
            SceneManager.LoadScene("Main");
        }
        private void SelectArscaraUi(string evtNewValue)
        {
            switch (evtNewValue)
            {
                case "Control panel":
                    _controlPanelController.ShowUI();
                    HideUI();
                    break;
                case "Jog and teach":
                    break;
                case "Points":
                    break;
                default:
                    break;
            }
        }
        internal void ShowUI()
        {
            _gameManager.arscaraUiSelected = "Jog and teach";
            _menuRobotArscaraDropdownField.value=_gameManager.arscaraUiSelected;
            _body.style.display = DisplayStyle.Flex;
        }
        private void HideUI()
        {
            _body.style.display = DisplayStyle.None;
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
                    _gameManager.environmentSelected = "Virtual  environment";
                    _menuEnvironmentDropdownField.value=_gameManager.environmentSelected;
                    break;
                case "Augmented reality environment":
                    _body.style.backgroundColor = new Color(0, 0, 0, 0);
                    environments[0].SetActive(false);
                    environments[1].SetActive(true);
                    environments[2].SetActive(false);
                    environments[3].SetActive(false);
                    _gameManager.environmentSelected = "Augmented reality environment";
                    _menuEnvironmentDropdownField.value=_gameManager.environmentSelected;
                    break;
                case "Hybrid environment":
                    _body.style.backgroundColor = new Color(0, 0, 0, 0);
                    environments[0].SetActive(false);
                    environments[1].SetActive(false);
                    environments[2].SetActive(true);
                    environments[3].SetActive(false);
                    _gameManager.environmentSelected = "Hybrid environment";
                    _menuEnvironmentDropdownField.value=_gameManager.environmentSelected;
                    break;
                case "Real device environment":
                    _body.style.backgroundColor = new Color(0, 0, 0, 0);
                    environments[0].SetActive(false);
                    environments[1].SetActive(false);
                    environments[2].SetActive(false);
                    environments[3].SetActive(true);
                    _gameManager.environmentSelected = "Real device environment";
                    _menuEnvironmentDropdownField.value=_gameManager.environmentSelected;
                    break;
                default:
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
        private void GetUiComponents()
        {
            var root=GetComponent<UIDocument>().rootVisualElement;
            _body=root.Q<VisualElement>("Body");
            _menuButton=root.Q<Button>("MenuButton");
            _subpanelsAndSmokeMaskContainer=root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _scrim=root.Q<VisualElement>("Scrim");
            _navigationMenuPanel=root.Q<VisualElement>("NavigationMenuPanel");
            _hideMenuButton=root.Q<Button>("HideMenuButton");
            _logoutButton=root.Q<Button>("LogoutButton");
            _menuEnvironmentDropdownField=root.Q<DropdownField>("MenuEnvironmentDropdownField");
            _menuRobotArscaraDropdownField=root.Q<DropdownField>("MenuRobotARSCARADropdownField");
        }
        private void RegisterEvents()
        {
            _menuButton.RegisterCallback<ClickEvent>(ShowMenu);
            _hideMenuButton.RegisterCallback<ClickEvent>(HideMenu);
            _logoutButton.RegisterCallback<ClickEvent>(BorrarEsteMetodo);
            _navigationMenuPanel.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);
            
            _menuEnvironmentDropdownField.RegisterValueChangedCallback(evt => {
                EnableEnvironment(evt.newValue);
            });
            
            _menuRobotArscaraDropdownField.RegisterValueChangedCallback(evt=>
            {
                SelectArscaraUi(evt.newValue);
            });
        }
        private void FindObjects()
        {
            _controlPanelController=GameObject.FindGameObjectWithTag("ControlPanel").GetComponent<ControlPanelController>();
            _gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        }
    }
}
