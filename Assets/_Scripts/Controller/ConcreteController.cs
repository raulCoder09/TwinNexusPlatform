using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class ConcreteController : MonoBehaviour
    {
        private VisualElement _body;
        private VisualElement _main;
        private VisualElement _footer;
        private DropdownField _menuEnvironment;
        private DropdownField _menuLevelMedara;
        private GameManager _gameManager;
        private Label _initialMessageLabel;
        private RadioButton _directKinematicsRadioButton;
        private RadioButton _geometricMethodRadioButton;
        private RadioButton _homogeneousTransformationMatrixMethodRadioButton;

        private RadioButton _inverseKinematicsRadioButton;
        private RadioButton _denavitHartenbergAlgorithmRadioButton;
        private RadioButton _quaternialMethodRadioButton;


        private RadioButton _ikGeometricMethod;
        private RadioButton _ikHomogeneousMatrixDecomposition;
        private RadioButton _ikKinematicDecoupling;
        private RadioButton _ikNumericalMethods;
        
        
        
        private Slider _angleQ1Slider;
        private Slider _angleQ2Slider;
        private Slider _displacementD3Slider;
        private Slider _angleQ4Slider;

        private Label _xCoordinateLabel;
        private Label _yCoordinateLabel;
        private Label _zCoordinateLabel;

        private Label _lengthOfLink1Label;

        #region menu

        private VisualElement _subpanelsAndSmokeMaskContainer;
        private VisualElement _navigationMenuPanel;
        private VisualElement _scrim;
        private Button _menuButton;
        private Button _hideMenuButton;

        #endregion

        internal Label lengthOfLink1Label
        {
            get => _lengthOfLink1Label;
            set => _lengthOfLink1Label = value;
        }

        internal Label lengthOfLink2Label
        {
            get => _lengthOfLink2Label;
            set => _lengthOfLink2Label = value;
        }

        internal Label lengthOfLink3Label
        {
            get => _lengthOfLink3Label;
            set => _lengthOfLink3Label = value;
        }

        private Label _lengthOfLink2Label;
        private Label _lengthOfLink3Label;
        
        
        private Label _helperMessageLabel;
        
        private VirtualEnvironmentController _virtualEnvironment;
        private AugmentedRealityEnvironmentController _augmentedRealityEnvironment;
        private HybridEnvironmentController _hybridEnvironment;
        private RealDeviceEnvironmentController _realDeviceEnvironment;
        

        private bool _isDkMethodSelected;
        private bool _isIkMethodSelected;
        
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
            _main.style.display = DisplayStyle.None;
            _footer.style.display = DisplayStyle.None;
            _menuEnvironment.style.display = DisplayStyle.None;
            _directKinematicsRadioButton.style.display = DisplayStyle.None;
            _inverseKinematicsRadioButton.style.display = DisplayStyle.None;
            _lengthOfLink1Label.style.display = DisplayStyle.None;
            _lengthOfLink2Label.style.display = DisplayStyle.None;
            _lengthOfLink3Label.style.display = DisplayStyle.None;
            _helperMessageLabel.style.display = DisplayStyle.None;
            _menuEnvironment.style.height = 60;
            _menuLevelMedara.style.height = 60;

            #region menu

            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;

            #endregion
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
            _menuEnvironment.RegisterValueChangedCallback(evt => SelectEnvironment(evt.newValue));
            _menuLevelMedara.RegisterValueChangedCallback(evt => SelectMedaraLevel(evt.newValue));

            _directKinematicsRadioButton.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _inverseKinematicsRadioButton.value = false;
                    _isIkMethodSelected = false;
                    EnableMethodsInverseKinematics(false);
                    EnableMethodsDirectKinematics(true);
                }
                else
                {
                    EnableMethodsDirectKinematics(_isDkMethodSelected);
                }
            });

    _inverseKinematicsRadioButton.RegisterValueChangedCallback(evt =>
    {
        if (evt.newValue)
        {
            _directKinematicsRadioButton.value = false;
            _isDkMethodSelected = false;
            EnableMethodsDirectKinematics(false);
            EnableMethodsInverseKinematics(true);
        }
        else
        {
            EnableMethodsInverseKinematics(_isIkMethodSelected);
        }
    });

    _geometricMethodRadioButton.RegisterValueChangedCallback(evt =>
    {
        if (evt.newValue)
        {
            _isDkMethodSelected = true;
            _homogeneousTransformationMatrixMethodRadioButton.value = false;
            _denavitHartenbergAlgorithmRadioButton.value = false;
            _quaternialMethodRadioButton.value = false;
            EnableMethodsDirectKinematics(true);
        }
        else if (!_homogeneousTransformationMatrixMethodRadioButton.value &&
                 !_denavitHartenbergAlgorithmRadioButton.value &&
                 !_quaternialMethodRadioButton.value)
        {
            _isDkMethodSelected = false;
            EnableMethodsDirectKinematics(_directKinematicsRadioButton.value);
        }
    });

    _homogeneousTransformationMatrixMethodRadioButton.RegisterValueChangedCallback(evt =>
    {
        if (evt.newValue)
        {
            _isDkMethodSelected = true;
            _geometricMethodRadioButton.value = false;
            _denavitHartenbergAlgorithmRadioButton.value = false;
            _quaternialMethodRadioButton.value = false;
            EnableMethodsDirectKinematics(true);
        }
        else if (!_geometricMethodRadioButton.value &&
                 !_denavitHartenbergAlgorithmRadioButton.value &&
                 !_quaternialMethodRadioButton.value)
        {
            _isDkMethodSelected = false;
            EnableMethodsDirectKinematics(_directKinematicsRadioButton.value);
        }
    });

    _denavitHartenbergAlgorithmRadioButton.RegisterValueChangedCallback(evt =>
    {
        if (evt.newValue)
        {
            _isDkMethodSelected = true;
            _geometricMethodRadioButton.value = false;
            _homogeneousTransformationMatrixMethodRadioButton.value = false;
            _quaternialMethodRadioButton.value = false;
            EnableMethodsDirectKinematics(true);
        }
        else if (!_geometricMethodRadioButton.value &&
                 !_homogeneousTransformationMatrixMethodRadioButton.value &&
                 !_quaternialMethodRadioButton.value)
        {
            _isDkMethodSelected = false;
            EnableMethodsDirectKinematics(_directKinematicsRadioButton.value);
        }
    });

    _quaternialMethodRadioButton.RegisterValueChangedCallback(evt =>
    {
        if (evt.newValue)
        {
            _isDkMethodSelected = true;
            _geometricMethodRadioButton.value = false;
            _homogeneousTransformationMatrixMethodRadioButton.value = false;
            _denavitHartenbergAlgorithmRadioButton.value = false;
            EnableMethodsDirectKinematics(true);
        }
        else if (!_geometricMethodRadioButton.value &&
                 !_homogeneousTransformationMatrixMethodRadioButton.value &&
                 !_denavitHartenbergAlgorithmRadioButton.value)
        {
            _isDkMethodSelected = false;
            EnableMethodsDirectKinematics(_directKinematicsRadioButton.value);
        }
    });

    _ikGeometricMethod.RegisterValueChangedCallback(evt =>
    {
        if (evt.newValue)
        {
            _isIkMethodSelected = true;
            _ikHomogeneousMatrixDecomposition.value = false;
            _ikKinematicDecoupling.value = false;
            _ikNumericalMethods.value = false;
            EnableMethodsInverseKinematics(true);
        }
        else if (!_ikHomogeneousMatrixDecomposition.value &&
                 !_ikKinematicDecoupling.value &&
                 !_ikNumericalMethods.value)
        {
            _isIkMethodSelected = false;
            EnableMethodsInverseKinematics(_inverseKinematicsRadioButton.value);
        }
    });

    _ikHomogeneousMatrixDecomposition.RegisterValueChangedCallback(evt =>
    {
        if (evt.newValue)
        {
            _isIkMethodSelected = true;
            _ikGeometricMethod.value = false;
            _ikKinematicDecoupling.value = false;
            _ikNumericalMethods.value = false;
            EnableMethodsInverseKinematics(true);
        }
        else if (!_ikGeometricMethod.value &&
                 !_ikKinematicDecoupling.value &&
                 !_ikNumericalMethods.value)
        {
            _isIkMethodSelected = false;
            EnableMethodsInverseKinematics(_inverseKinematicsRadioButton.value);
        }
    });

    _ikKinematicDecoupling.RegisterValueChangedCallback(evt =>
    {
        if (evt.newValue)
        {
            _isIkMethodSelected = true;
            _ikGeometricMethod.value = false;
            _ikHomogeneousMatrixDecomposition.value = false;
            _ikNumericalMethods.value = false;
            EnableMethodsInverseKinematics(true);
        }
        else if (!_ikGeometricMethod.value &&
                 !_ikHomogeneousMatrixDecomposition.value &&
                 !_ikNumericalMethods.value)
        {
            _isIkMethodSelected = false;
            EnableMethodsInverseKinematics(_inverseKinematicsRadioButton.value);
        }
    });

    _ikNumericalMethods.RegisterValueChangedCallback(evt =>
    {
        if (evt.newValue)
        {
            _isIkMethodSelected = true;
            _ikGeometricMethod.value = false;
            _ikHomogeneousMatrixDecomposition.value = false;
            _ikKinematicDecoupling.value = false;
            EnableMethodsInverseKinematics(true);
        }
        else if (!_ikGeometricMethod.value &&
                 !_ikHomogeneousMatrixDecomposition.value &&
                 !_ikKinematicDecoupling.value)
        {
            _isIkMethodSelected = false;
            EnableMethodsInverseKinematics(_inverseKinematicsRadioButton.value);
        }
    });
    
    #region menu

    _menuButton.RegisterCallback<ClickEvent>(ShowMenu);
    _hideMenuButton.RegisterCallback<ClickEvent>(HideMenu);
    _navigationMenuPanel.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);
    #endregion
}

        private void EnableMethodsInverseKinematics(bool evtNewValue)
        {
            if (evtNewValue)
            {
                _ikGeometricMethod.style.display = DisplayStyle.Flex;
                _ikHomogeneousMatrixDecomposition.style.display = DisplayStyle.Flex;
                _ikKinematicDecoupling.style.display = DisplayStyle.Flex;
                _ikNumericalMethods.style.display = DisplayStyle.Flex;
            }
            else
            {
                _ikGeometricMethod.style.display = DisplayStyle.None;
                _ikHomogeneousMatrixDecomposition.style.display = DisplayStyle.None;
                _ikKinematicDecoupling.style.display = DisplayStyle.None;
                _ikNumericalMethods.style.display = DisplayStyle.None;
            }
        }

        private void EnableMethodsDirectKinematics(bool evtNewValue)
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
                _angleQ4Slider.style.display =DisplayStyle.Flex;
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
                _angleQ4Slider.style.display =DisplayStyle.None;
            }
        }

        private void SelectMedaraLevel(string selectMedaraLevel)
        {
            switch (selectMedaraLevel)
            {
                case "Graphic":
                    _initialMessageLabel.style.display = DisplayStyle.None;
                    _helperMessageLabel.style.display = DisplayStyle.Flex;
                    _graphic.EnableLevel();
                    _abstract.DisableLevel();
                    DisableLevel();
                    break;
                case "Abstract":
                    _initialMessageLabel.style.display = DisplayStyle.None;
                    _helperMessageLabel.style.display = DisplayStyle.Flex;
                    _graphic.DisableLevel();
                    _abstract.EnableLevel();
                    DisableLevel();
                    break;
                case "Concrete":
                    _initialMessageLabel.style.display = DisplayStyle.None;
                    _helperMessageLabel.style.display = DisplayStyle.Flex;
                    _graphic.DisableLevel();
                    _abstract.DisableLevel();
                    _menuEnvironment.style.display = DisplayStyle.Flex;
                    _main.style.display = DisplayStyle.Flex;
                    _directKinematicsRadioButton.style.display = DisplayStyle.Flex;
                    _inverseKinematicsRadioButton.style.display = DisplayStyle.Flex;
                    _lengthOfLink1Label.style.display = DisplayStyle.Flex;
                    _lengthOfLink2Label.style.display = DisplayStyle.Flex;
                    _lengthOfLink3Label.style.display = DisplayStyle.Flex;
                    EnableLevel();
                    break;
            }
        }

        private void SelectEnvironment(string selectedEnvironment)
        {
            switch (selectedEnvironment)
            {
                
                case "Virtual  environment":
                    _helperMessageLabel.style.display = DisplayStyle.None;
                    LaunchVirtualEnvironment();
                    break;
                case "Augmented reality environment":
                    _helperMessageLabel.style.display = DisplayStyle.None;
                    LaunchAugmentedRealityEnvironment();
                    break;
                case "Hybrid environment":
                    _helperMessageLabel.style.display = DisplayStyle.None;
                    LaunchHybridEnvironment();
                    break;
                case "Real device environment":
                    _helperMessageLabel.style.display = DisplayStyle.None;
                    LaunchRealDeviceEnvironment();
                    break;
            }
        }

        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _main=root.Q<VisualElement>("Main");
            _menuEnvironment = root.Q<DropdownField>("MenuEnvironmentDropdownField");
            _menuLevelMedara= root.Q<DropdownField>("MenuLevelMedaraDropdownField");
            _initialMessageLabel = root.Q<Label>("InitialMessageLabel");
            _directKinematicsRadioButton= root.Q<RadioButton>("DirectKinematicsRadioButton");
            _inverseKinematicsRadioButton= root.Q<RadioButton>("InverseKinematicsRadioButton");
            _geometricMethodRadioButton= root.Q<RadioButton>("DKGeometricMethodRadioButton");
            _homogeneousTransformationMatrixMethodRadioButton= root.Q<RadioButton>("DKHomogeneousTransformationMatrixMethodRadioButton");
            _denavitHartenbergAlgorithmRadioButton= root.Q<RadioButton>("DKDenavitHartenbergAlgorithmRadioButton");
            _quaternialMethodRadioButton= root.Q<RadioButton>("DKQuaternialMethodRadioButton");
            _angleQ1Slider=root.Q<Slider>("AngleQ1Slider");
            _angleQ2Slider=root.Q<Slider>("AngleQ2Slider");
            _displacementD3Slider=root.Q<Slider>("DisplacementD3Slider");
            _angleQ4Slider=root.Q<Slider>("AngleQ4Slider");
            _xCoordinateLabel=root.Q<Label>("xCoordinateLabel");
            _yCoordinateLabel=root.Q<Label>("yCoordinateLabel");
            _zCoordinateLabel=root.Q<Label>("zCoordinateLabel");
            _ikGeometricMethod = root.Q<RadioButton>("IKGeometricMethod");
            _ikHomogeneousMatrixDecomposition= root.Q<RadioButton>("IKHomogeneousMatrixDecomposition");
            _ikKinematicDecoupling= root.Q<RadioButton>("IKKinematicDecoupling");
            _ikNumericalMethods= root.Q<RadioButton>("IKNumericalMethods");
            _footer=root.Q<VisualElement>("Footer");
            
            _lengthOfLink1Label=root.Q<Label>("LengthOfLink1Label");
            _lengthOfLink2Label=root.Q<Label>("LengthOfLink2Label");
            _lengthOfLink3Label=root.Q<Label>("LengthOfLink3Label");
            _helperMessageLabel=root.Q<Label>("HelperMessageLabel");
            
            
            #region menu

            _menuButton=root.Q<Button>("MenuButton");
            _subpanelsAndSmokeMaskContainer=root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _navigationMenuPanel=root.Q<VisualElement>("NavigationMenuPanel");
            _scrim = root.Q<VisualElement>("Scrim");
            _hideMenuButton=root.Q<Button>("HideMenuButton");
            #endregion
        }

        #region menu

        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            if (!_navigationMenuPanel.ClassListContains("NavigationMenuPanelInMainScreen"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }

        private void HideMenu(ClickEvent evt)
        {
            _navigationMenuPanel.RemoveFromClassList("NavigationMenuPanelInMainScreen");
            _scrim.RemoveFromClassList("ScrimOpaque");
        }

        private void ShowMenu(ClickEvent evt)
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.Flex;
            _navigationMenuPanel.AddToClassList("NavigationMenuPanelInMainScreen");
            _scrim.AddToClassList("ScrimOpaque");
        }

        #endregion
        
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
