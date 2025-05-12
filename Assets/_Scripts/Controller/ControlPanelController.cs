using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class ControlPanelController : MonoBehaviour
    {
        private JogAndTeachController _jogAndTeachController;
        private GameManager _gameManager;
        
        private VisualElement _body;
        private DropdownField _menuEnvironmentDropdownField;
        private DropdownField _menuRobotArscaraDropdownField;
        
        private VirtualEnvironmentController _virtualEnvironment;
        private AugmentedRealityEnvironmentController _augmentedRealityEnvironment;
        private HybridEnvironmentController _hybridEnvironment;
        private RealDeviceEnvironmentController _realDeviceEnvironment;

        internal DropdownField menuEnvironmentDropdownField
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
            ShowUI();
            _menuRobotArscaraDropdownField.value = "Menu environment";
            _menuEnvironmentDropdownField.value =  "Menu ARSCARA";
            
            FindEnvironmentComponents();

        }
        internal void ShowUI()
        {
            _jogAndTeachController.menuRobotArscaraDropdownField.value= _menuRobotArscaraDropdownField.value=_gameManager.deviceUiSelected = "Control panel";
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
        private void GetUiComponents()
        {
            var root= GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _menuEnvironmentDropdownField = root.Q<DropdownField>("MenuEnvironmentDropdownField");
            _menuRobotArscaraDropdownField=root.Q<DropdownField>("MenuRobotARSCARADropdownField");
        }
        private void RegisterEvents()
        {
            _menuEnvironmentDropdownField.RegisterValueChangedCallback(evt =>
            {
                EnableEnvironment(evt.newValue);
            });
            
           
            _menuRobotArscaraDropdownField.RegisterValueChangedCallback(evt=>
            {
                SelectArscaraUi(evt.newValue);
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
            _jogAndTeachController.menuEnvironmentDropdownField.value=_menuEnvironmentDropdownField.value=_gameManager.environmentSelected= "Virtual  environment";
        }
        private void LaunchAugmentedRealityEnvironment()
        {
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.EnableEnvironment();
            _hybridEnvironment.DisableEnvironment();
            _realDeviceEnvironment.DisableEnvironment();
            _jogAndTeachController.menuEnvironmentDropdownField.value=_menuEnvironmentDropdownField.value=_gameManager.environmentSelected= "Augmented reality environment";
        }
        private void LaunchHybridEnvironment()
        {
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.DisableEnvironment();
            _hybridEnvironment.EnableEnvironment();
            _realDeviceEnvironment.DisableEnvironment();
            _jogAndTeachController.menuEnvironmentDropdownField.value=_menuEnvironmentDropdownField.value=_gameManager.environmentSelected= "Hybrid environment";
        }
        private void LaunchRealDeviceEnvironment()
        {
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.DisableEnvironment();
            _hybridEnvironment.DisableEnvironment();
            _realDeviceEnvironment.EnableEnvironment();
            _jogAndTeachController.menuEnvironmentDropdownField.value=_menuEnvironmentDropdownField.value=_gameManager.environmentSelected= "Real device environment";
        }
    }
}
