using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class ConcreteController : MonoBehaviour
    {
        private VisualElement _body;
        private DropdownField _menuEnvironment;
        private DropdownField _menuLevelMedara;
        private GameManager _gameManager;
        
        private VirtualEnvironmentController _virtualEnvironment;
        private AugmentedRealityEnvironmentController _augmentedRealityEnvironment;
        private HybridEnvironmentController _hybridEnvironment;
        private RealDeviceEnvironmentController _realDeviceEnvironment;
        private void Awake()
        {
            
            GetUiComponents();
            RegisterEvents();
            FindObjects();
            
        }
        

        private void Start()
        {
            FindEnvironmentComponents();
            _menuEnvironment.value ="Menu environment" ;
            _menuLevelMedara.value ="MEDARA Level" ;
        }

        private void FindEnvironmentComponents()
        {
            _virtualEnvironment=_gameManager.arscaraTrainingInstance.transform.Find("Concrete/VirtualEnvironment").GetComponent<VirtualEnvironmentController>();
            _augmentedRealityEnvironment = _gameManager.arscaraTrainingInstance.transform.Find("Concrete/AugmentedRealityEnvironment").GetComponent<AugmentedRealityEnvironmentController>();
            _hybridEnvironment = _gameManager.arscaraTrainingInstance.transform.Find("Concrete/HybridEnvironment").GetComponent<HybridEnvironmentController>();
            _realDeviceEnvironment = _gameManager.arscaraTrainingInstance.transform.Find("Concrete/RealDeviceEnvironment").GetComponent<RealDeviceEnvironmentController>();
        }
        
        private void FindObjects()
        {
            _gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        }

        private void RegisterEvents()
        {
            _menuEnvironment.RegisterValueChangedCallback(evt =>
            {
                SelectEnvironment(evt.newValue);
                print(evt.newValue);
            });

        }

        private void SelectEnvironment(string selectedEnvironment)
        {
            switch (selectedEnvironment)
            {
                case "Virtual  environment":
                    _body.style.backgroundColor = new Color(0, 0, 0, 0);
                    _virtualEnvironment.EnableEnvironment();
                    break;
            }
        }

        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _menuEnvironment = root.Q<DropdownField>("MenuEnvironmentDropdownField");
            _menuLevelMedara= root.Q<DropdownField>("MenuLevelMedaraDropdownField");

        }
    }
}
