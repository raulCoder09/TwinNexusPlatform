using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class ControlPanelController : MonoBehaviour
    {
        private JogAndTeachController _jogAndTeachController;
        private GameManager _gameManager;
        private GameObject _virtualEnvironmentCamera;
        
        private VisualElement _body;
        private DropdownField _menuEnvironment;
        private DropdownField _menuRobotArscara;
        private DropdownField _menuViews;
        
        private VirtualEnvironmentController _virtualEnvironment;
        private AugmentedRealityEnvironmentController _augmentedRealityEnvironment;
        private HybridEnvironmentController _hybridEnvironment;
        private RealDeviceEnvironmentController _realDeviceEnvironment;

        internal DropdownField menuEnvironment
        {
            get => _menuEnvironment;
            set => _menuEnvironment = value;
        }
        internal DropdownField menuRobotArscara
        {
            get => _menuRobotArscara;
            set => _menuRobotArscara = value;
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
            ShowUI();
            _menuRobotArscara.value = "Menu environment";
            _menuEnvironment.value =  "Menu ARSCARA";
            FindEnvironmentComponents();

        }
        internal void ShowUI()
        {
            _jogAndTeachController.menuRobotArscaraDropdownField.value= _menuRobotArscara.value=_gameManager.deviceUiSelected = "Control panel";
            _jogAndTeachController.menuViews.value = _gameManager.viewSelected;
            _body.style.display = DisplayStyle.Flex;
        }
        private void HideUI()
        {
            _body.style.display = DisplayStyle.None;
        }
        private void SelectArscaraUi(string evtNewValue)
        {
            switch (evtNewValue)
            {
                case "Control panel":
                    break;
                case "Jog and teach":
                    _jogAndTeachController.ShowUI();
                    HideUI();
                    break;
                case "Points":
                    break;
                default:
                    break;
            }
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
        private void SelectView(string evtNewValue)
        {
            switch (evtNewValue)
            {
                case "Top":
                    LaunchView(new Vector3(0f, 0.35f, 0.13f),new Vector3(90f, 0f, 0f),evtNewValue);
                    break;
                case "Front":
                    LaunchView( new Vector3(0f, 0.03f, 0.5f),new Vector3(-10f, 180f, 0f),evtNewValue);
                    break;
                case "Back":
                    LaunchView( new Vector3(0f, 0.06f, -0.25f),new Vector3(-10f, 0f, 0f),evtNewValue);
                    break;
                case "Left":
                    LaunchView( new Vector3(-0.35f, 0.03f, .12f),new Vector3(-10f, 90f, 0f),evtNewValue);
                    break;
                case "Right":
                    LaunchView( new Vector3(0.35f, 0.03f, .12f),new Vector3(-10f, 270f, 0f),evtNewValue);
                    break;
                default:
                    LaunchView( new Vector3(0f, 0.325f, 0f),new Vector3(66f, 0f, 0f),"Default");
                    break;
            }
        }
        private void GetUiComponents()
        {
            var root= GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _menuEnvironment = root.Q<DropdownField>("MenuEnvironmentDropdownField");
            _menuRobotArscara=root.Q<DropdownField>("MenuRobotARSCARADropdownField");
            _menuViews=root.Q<DropdownField>("Views");
        }
        private void RegisterEvents()
        {
            _menuEnvironment.RegisterValueChangedCallback(evt =>
            {
                EnableEnvironment(evt.newValue);
            });
            
           
            _menuRobotArscara.RegisterValueChangedCallback(evt=>
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
            _jogAndTeachController=GameObject.FindGameObjectWithTag("JogAndTeach").GetComponent<JogAndTeachController>();
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
            _jogAndTeachController.menuEnvironmentDropdownField.value=_menuEnvironment.value=_gameManager.environmentSelected= "Virtual  environment";
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
            _jogAndTeachController.menuEnvironmentDropdownField.value=_menuEnvironment.value=_gameManager.environmentSelected= "Augmented reality environment";
            _menuViews.style.display = DisplayStyle.None;
        }
        private void LaunchHybridEnvironment()
        {
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.DisableEnvironment();
            _hybridEnvironment.EnableEnvironment();
            _realDeviceEnvironment.DisableEnvironment();
            _jogAndTeachController.menuEnvironmentDropdownField.value=_menuEnvironment.value=_gameManager.environmentSelected= "Hybrid environment";
            _menuViews.style.display = DisplayStyle.None;
        }
        private void LaunchRealDeviceEnvironment()
        {
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.DisableEnvironment();
            _hybridEnvironment.DisableEnvironment();
            _realDeviceEnvironment.EnableEnvironment();
            _jogAndTeachController.menuEnvironmentDropdownField.value=_menuEnvironment.value=_gameManager.environmentSelected= "Real device environment";
            _menuViews.style.display = DisplayStyle.None;
        }
        private void LaunchView(Vector3 position,Vector3 rotation,string view)
        {
            _virtualEnvironmentCamera.transform.position = position;
            _virtualEnvironmentCamera.transform.eulerAngles = rotation;
            _jogAndTeachController.menuViews.value=_gameManager.viewSelected = view;
        }
    }
}
