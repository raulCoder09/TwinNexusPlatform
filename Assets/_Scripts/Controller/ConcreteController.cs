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
        private Label _initialMessageLabel;
        private RadioButton _directKinematicsRadioButton;
        private RadioButton _geometricMethodRadioButton;
        private RadioButton _homogeneousTransformationMatrixMethodRadioButton;
        private RadioButton _denavitHartenbergAlgorithmRadioButton;
        private RadioButton _quaternialMethodRadioButton;
        private Slider _angleQ1Slider;
        private Slider _angleQ2Slider;
        private Slider _displacementD3Slider;

        private Label _xCoordinateLabel;
        private Label _yCoordinateLabel;
        private Label _zCoordinateLabel;
        
        
        private VirtualEnvironmentController _virtualEnvironment;
        private AugmentedRealityEnvironmentController _augmentedRealityEnvironment;
        private HybridEnvironmentController _hybridEnvironment;
        private RealDeviceEnvironmentController _realDeviceEnvironment;
        

        private GraphicController _graphic;
        private AbstractController _abstract;
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
            _graphic=_gameManager.arscaraTrainingInstance.transform.Find("Graphic").GetComponent<GraphicController>();
            _abstract=_gameManager.arscaraTrainingInstance.transform.Find("Abstract").GetComponent<AbstractController>();
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
            });
            
            _menuLevelMedara.RegisterValueChangedCallback(evt =>
            {
                SelectMedaraLevel(evt.newValue);
            });
            _directKinematicsRadioButton.RegisterValueChangedCallback(evt =>
            {
                EnableMethotsDirectKinematics(evt.newValue);

            });

        }

        private void EnableMethotsDirectKinematics(bool evtNewValue)
        {
            if (evtNewValue)
            {
                _geometricMethodRadioButton.style.display = DisplayStyle.Flex;
                _homogeneousTransformationMatrixMethodRadioButton.style.display = DisplayStyle.Flex;
                _denavitHartenbergAlgorithmRadioButton.style.display = DisplayStyle.Flex;
                _quaternialMethodRadioButton.style.display = DisplayStyle.Flex;
                _angleQ1Slider.style.display =DisplayStyle.Flex;
                _angleQ2Slider.style.display =DisplayStyle.Flex;
                _displacementD3Slider.style.display =DisplayStyle.Flex;
                _xCoordinateLabel.style.display =DisplayStyle.Flex;
                _yCoordinateLabel.style.display =DisplayStyle.Flex;
                _zCoordinateLabel.style.display =DisplayStyle.Flex;
            }
            else
            {
                _geometricMethodRadioButton.style.display = DisplayStyle.None;
                _homogeneousTransformationMatrixMethodRadioButton.style.display = DisplayStyle.None;
                _denavitHartenbergAlgorithmRadioButton.style.display = DisplayStyle.None;
                _quaternialMethodRadioButton.style.display = DisplayStyle.None;
                _angleQ1Slider.style.display = DisplayStyle.None;
                _angleQ2Slider.style.display = DisplayStyle.None;
                _displacementD3Slider.style.display = DisplayStyle.None;
                _xCoordinateLabel.style.display =DisplayStyle.None;
                _yCoordinateLabel.style.display =DisplayStyle.None;
                _zCoordinateLabel.style.display =DisplayStyle.None;
            }
        }

        private void SelectMedaraLevel(string selectMedaraLevel)
        {
            print(selectMedaraLevel);
            switch (selectMedaraLevel)
            {
                case "Graphic":
                    _graphic.EnableLevel();
                    _abstract.DisableLevel();
                    DisableLevel();
                    break;
                case "Abstract":
                    _graphic.DisableLevel();
                    _abstract.EnableLevel();
                    DisableLevel();
                    break;
                case "Concrete":
                    _graphic.DisableLevel();
                    _abstract.DisableLevel();
                    EnableLevel();
                    break;
                default:
                    break;
            }
        }

        private void SelectEnvironment(string selectedEnvironment)
        {
            switch (selectedEnvironment)
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
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _menuEnvironment = root.Q<DropdownField>("MenuEnvironmentDropdownField");
            _menuLevelMedara= root.Q<DropdownField>("MenuLevelMedaraDropdownField");
            _initialMessageLabel = root.Q<Label>("InitialMessageLabel");
            _directKinematicsRadioButton= root.Q<RadioButton>("DirectKinematicsRadioButton");
            _geometricMethodRadioButton= root.Q<RadioButton>("GeometricMethodRadioButton");
            _homogeneousTransformationMatrixMethodRadioButton= root.Q<RadioButton>("HomogeneousTransformationMatrixMethodRadioButton");
            _denavitHartenbergAlgorithmRadioButton= root.Q<RadioButton>("DenavitHartenbergAlgorithmRadioButton");
            _quaternialMethodRadioButton= root.Q<RadioButton>("QuaternialMethodRadioButton");
            _angleQ1Slider=root.Q<Slider>("AngleQ1Slider");
            _angleQ2Slider=root.Q<Slider>("AngleQ2Slider");
            _displacementD3Slider=root.Q<Slider>("DisplacementD3Slider");
            _xCoordinateLabel=root.Q<Label>("xCoordinateLabel");
            _yCoordinateLabel=root.Q<Label>("yCoordinateLabel");
            _zCoordinateLabel=root.Q<Label>("zCoordinateLabel");
        }
        
        private void LaunchRealDeviceEnvironment()
        {
            _initialMessageLabel.style.display = DisplayStyle.None;
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.DisableEnvironment();
            _hybridEnvironment.DisableEnvironment();
            _realDeviceEnvironment.EnableEnvironment();
        }

        private void LaunchHybridEnvironment()
        {
            _initialMessageLabel.style.display = DisplayStyle.None;
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.DisableEnvironment();
            _hybridEnvironment.EnableEnvironment();
            _realDeviceEnvironment.DisableEnvironment();
        }

        private void LaunchAugmentedRealityEnvironment()
        {
            _initialMessageLabel.style.display = DisplayStyle.None;
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.DisableEnvironment();
            _augmentedRealityEnvironment.EnableEnvironment();
            _hybridEnvironment.DisableEnvironment();
            _realDeviceEnvironment.DisableEnvironment();
        }

        private void LaunchVirtualEnvironment()
        {
            _initialMessageLabel.style.display = DisplayStyle.None;
            _body.style.backgroundColor = new Color(0, 0, 0, 0);
            _virtualEnvironment.EnableEnvironment();
            _augmentedRealityEnvironment.DisableEnvironment();
            _hybridEnvironment.DisableEnvironment();
            _realDeviceEnvironment.DisableEnvironment();
        }
        
        internal void EnableLevel()
        {
            gameObject.SetActive(true);
        }

        internal void DisableLevel()
        {
            gameObject.SetActive(false);
        }
    }
}
