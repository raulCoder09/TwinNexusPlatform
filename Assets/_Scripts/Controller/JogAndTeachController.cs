using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class JogAndTeachController : MonoBehaviour
    {
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

        private VirtualEnvironmentController _virtualEnvironment;
        private AugmentedRealityEnvironmentController _augmentedRealityEnvironment;
        private HybridEnvironmentController _hybridEnvironment;
        private RealDeviceEnvironmentController _realDeviceEnvironment;
        
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

            FindEnvironmentComponents();
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            HideUI();
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
            _controlPanelController.menuRobotArscaraDropdownField.value=_menuRobotArscaraDropdownField.value=_gameManager.deviceUiSelected= "Jog and teach";;
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
                    LaunchVirtualEnvironment();
                    break;
                case "Augmented reality environment":
                    LaunchAugmentedRealityEnvironment();
                    break;
                case "Hybrid environment":
                    LaunchHybridEnvironment();
                    break;
                case "Real device environment":
                    LaunchRealDeviceEnvironment();
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
        private void FindEnvironmentComponents()
        {
            _virtualEnvironment=_gameManager.arscaraInstance.transform.Find("VirtualEnvironment").GetComponent<VirtualEnvironmentController>();
            _augmentedRealityEnvironment = _gameManager.arscaraInstance.transform.Find("AugmentedRealityEnvironment").GetComponent<AugmentedRealityEnvironmentController>();
            _hybridEnvironment = _gameManager.arscaraInstance.transform.Find("HybridEnvironment").GetComponent<HybridEnvironmentController>();
            _realDeviceEnvironment = _gameManager.arscaraInstance.transform.Find("RealDeviceEnvironment").GetComponent<RealDeviceEnvironmentController>();
        }
        private void LaunchVirtualEnvironment()
        {
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.EnableEnvironment();
            _augmentedRealityEnvironment.DisableEnvironment();
            _hybridEnvironment.DisableEnvironment();
            _realDeviceEnvironment.DisableEnvironment();
            _controlPanelController.menuEnvironmentDropdownField.value=_menuEnvironmentDropdownField.value=_gameManager.environmentSelected= "Virtual  environment";
        }
        private void LaunchAugmentedRealityEnvironment()
        {
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.EnableEnvironment();
            _hybridEnvironment.DisableEnvironment();
            _realDeviceEnvironment.DisableEnvironment();
            _controlPanelController.menuEnvironmentDropdownField.value=_menuEnvironmentDropdownField.value=_gameManager.environmentSelected= "Augmented reality environment";
        }
        private void LaunchHybridEnvironment()
        {
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.DisableEnvironment();
            _hybridEnvironment.EnableEnvironment();
            _realDeviceEnvironment.DisableEnvironment();
            _controlPanelController.menuEnvironmentDropdownField.value=_menuEnvironmentDropdownField.value=_gameManager.environmentSelected= "Hybrid environment";
        }
        private void LaunchRealDeviceEnvironment()
        {
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.DisableEnvironment();
            _hybridEnvironment.DisableEnvironment();
            _realDeviceEnvironment.EnableEnvironment();
            _controlPanelController.menuEnvironmentDropdownField.value=_menuEnvironmentDropdownField.value=_gameManager.environmentSelected= "Real device environment";
        }
    }
}
