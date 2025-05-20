using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class JogAndTeachController : MonoBehaviour
    {
        private ControlPanelController _controlPanelController;
        private GameManager _gameManager;
        private GameObject _virtualEnvironmentCamera;
        
        private Button _menuButton;
        private Button _hideMenuButton;
        private Button _logoutButton;
        
        private VisualElement _body;
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private VisualElement _scrim;
        private VisualElement _navigationMenuPanel;
        
        private DropdownField _menuEnvironmentDropdownField;
        private DropdownField _menuRobotArscaraDropdownField;
        private DropdownField _menuViews;

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
        
        internal DropdownField menuViews
        {
            get => _menuViews;
            set => _menuViews = value;
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
            _controlPanelController.menuRobotArscara.value=_menuRobotArscaraDropdownField.value=_gameManager.deviceUiSelected= "Jog and teach";;
            _controlPanelController.menuViews.value = _gameManager.viewSelected;
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
        private void SelectView(string evtNewValue)
        {
            switch (evtNewValue)
            {
                case "Top":
                    LaunchView(new Vector3(0f, 0.35f, 0.13f),new Vector3(90f, 0f, 0f),evtNewValue);
                    break;
                case "Front":
                    LaunchView(new Vector3(0f, 0.03f, 0.5f),new Vector3(-10f, 180f, 0f),evtNewValue);
                    break;
                case "Back":
                    LaunchView(new Vector3(0f, 0.06f, -0.25f),new Vector3(-10f, 0f, 0f),evtNewValue);
                    break;
                case "Left":
                    LaunchView(new Vector3(-0.35f, 0.03f, .12f),new Vector3(-10f, 90f, 0f),evtNewValue);
                    break;
                case "Right":
                    LaunchView(new Vector3(0.35f, 0.03f, .12f),new Vector3(-10f, 270f, 0f),evtNewValue);
                    break;
                default:
                    LaunchView(new Vector3(0f, 0.325f, 0f),new Vector3(66f, 0f, 0f),"Default");
                    break;
            }
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
            _menuViews=root.Q<DropdownField>("Views");
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
            
            _menuViews.RegisterValueChangedCallback(evt =>
            {
                SelectView(evt.newValue);
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
            _controlPanelController.menuEnvironment.value=_menuEnvironmentDropdownField.value=_gameManager.environmentSelected= "Virtual  environment";
            _virtualEnvironmentCamera=GameObject.FindGameObjectWithTag("VirtualEnvironmentCamera");
            _menuViews.value = "Select view";
        }
        private void LaunchAugmentedRealityEnvironment()
        {
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.EnableEnvironment();
            _hybridEnvironment.DisableEnvironment();
            _realDeviceEnvironment.DisableEnvironment();
            _controlPanelController.menuEnvironment.value=_menuEnvironmentDropdownField.value=_gameManager.environmentSelected= "Augmented reality environment";
            _menuViews.style.display = DisplayStyle.None;
        }
        private void LaunchHybridEnvironment()
        {
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.DisableEnvironment();
            _hybridEnvironment.EnableEnvironment();
            _realDeviceEnvironment.DisableEnvironment();
            _controlPanelController.menuEnvironment.value=_menuEnvironmentDropdownField.value=_gameManager.environmentSelected= "Hybrid environment";
            _menuViews.style.display = DisplayStyle.None;
        }
        private void LaunchRealDeviceEnvironment()
        {
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.DisableEnvironment();
            _hybridEnvironment.DisableEnvironment();
            _realDeviceEnvironment.EnableEnvironment();
            _controlPanelController.menuEnvironment.value=_menuEnvironmentDropdownField.value=_gameManager.environmentSelected= "Real device environment";
            _menuViews.style.display = DisplayStyle.None;
        }
        private void LaunchView(Vector3 position,Vector3 rotation,string view)
        {
            _virtualEnvironmentCamera.transform.position = position;
            _virtualEnvironmentCamera.transform.eulerAngles = rotation;
            _controlPanelController.menuViews.value=_gameManager.viewSelected = view;
        }
        
    }
}
