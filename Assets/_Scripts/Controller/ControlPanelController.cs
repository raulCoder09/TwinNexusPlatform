using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class ControlPanelController : MonoBehaviour
    {
        
        [SerializeField] private GameObject[] environments;
        
        private JogAndTeachController _jogAndTeachController;
        private GameManager _gameManager;
        
        private VisualElement _body;
        private DropdownField _menuEnvironmentDropdownField;
        private DropdownField _menuRobotArscaraDropdownField;

        public DropdownField menuEnvironmentDropdownField
        {
            get => _menuEnvironmentDropdownField;
            set => _menuEnvironmentDropdownField = value;
        }

        public DropdownField menuRobotArscaraDropdownField
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
        }
        internal void ShowUI()
        {
            _gameManager.arscaraUiSelected = "Control panel";
            _menuRobotArscaraDropdownField.value=_gameManager.arscaraUiSelected;
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
    }
}
