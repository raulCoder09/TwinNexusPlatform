using System;
using System.Collections;
using _scripts.models.robotics;
using UnityEngine;
using UnityEngine.UIElements;

namespace _scripts.controllers
{
    [RequireComponent(typeof(JogAndTeachController))]
    public class ArScaraVirtualController : MonoBehaviour
    {
        private JogAndTeachController _jogAndTeachController;
        private ControlPanelController _controlPanelController;
        
        private ArScaraModel3DController _arScaraModel3DController;
        private ArScaraKinematicChainController _arScaraKinematicChainController;
        
        private string _modeMotion;
        private float _speed;
        private string _displayMode;
        private void Awake()
        {
            _controlPanelController = GameObject.FindWithTag("VirtualEnvironmentArScara").GetComponent<ControlPanelController>();
            _jogAndTeachController = GameObject.FindWithTag("VirtualEnvironmentArScara").GetComponent<JogAndTeachController>();
            _arScaraModel3DController = GameObject.FindWithTag("ArScaraModel3D").GetComponent<ArScaraModel3DController>();
            _arScaraKinematicChainController = GameObject.FindWithTag("AeScaraKinematicChain").GetComponent<ArScaraKinematicChainController>();
            
        }

        private void Start()
        {
            _displayMode = "3D";
            if (_jogAndTeachController.ContinuousMove.value)
            {
                _modeMotion = "Continuous";    
            }
            
            if (_controlPanelController != null)
            {
                WireControlPanel();
            }
            else
            {
                print("No control panel found");
            }

            if (_jogAndTeachController != null)
            {
                WireJogAndTeach();
            }
            else
            {
                print("No _jogAndTeachController found");
            }
        }

        private void Update()
        {
            if (_arScaraModel3DController.linkController1 != null && _arScaraModel3DController.linkController2 != null)
            {
                _jogAndTeachController.J1Label.text = $"J1: {_arScaraModel3DController.angleLink1:F2} deg";
                _jogAndTeachController.J2Label.text = $"J2: {_arScaraModel3DController.angleLink2:F2} deg";
            }
        }
        
        private void WireControlPanel()
        {
            var buttons = new (string itemName, Button button)[]
            {
                ("MotorsOff", _controlPanelController.MotorsOffButton),
                ("MotorsOn", _controlPanelController.MotorsOnButton),
                ("PowerLow", _controlPanelController.PowerLowButton),
                ("PowerHigh", _controlPanelController.PowerHighButton),
                ("Home", _controlPanelController.HomeButton),
                ("Reset", _controlPanelController.ResetButton),
                ("FreeAll", _controlPanelController.FreeAllButton),
                ("LockAll", _controlPanelController.LockAllButton),
            };

            foreach (var (itemName, button) in buttons)
                WireButton(itemName, button);
            
            var toggles = new (string itemName, Toggle toggle)[]
            {
                ("J1", _controlPanelController.J1Toggle),
                ("J2", _controlPanelController.J2Toggle),
            };

            foreach (var (itemName, toggle) in toggles)
                WireToggle(itemName, toggle);
        }

        private void WireJogAndTeach()
        {
            var buttons = new (string itemName, Button button)[]
            {
                ("PlusX", _jogAndTeachController.PlusXButton),
                ("MinusX", _jogAndTeachController.MinusXButton),
                ("PlusY", _jogAndTeachController.PlusYButton),
                ("MinusY", _jogAndTeachController.MinusYButton),
                
                ("PlusJ1", _jogAndTeachController.PlusJ1Button),
                ("MinusJ1", _jogAndTeachController.MinusJ1Button),
                ("PlusJ2", _jogAndTeachController.PlusJ2Button),
                ("MinusJ2", _jogAndTeachController.MinusJ2Button),
                
                ("Teach", _jogAndTeachController.TeachButton),
                ("Edit", _jogAndTeachController.EditButton),
                ("Stop", _jogAndTeachController.StopButton),
                ("Chain/3D", _jogAndTeachController.ChainOr3DButton)
                
            };
            
            foreach (var (itemName, button) in buttons)
                WireButton(itemName, button);

            var radios = new (string itemName, RadioButton radioButton)[]
            {
                ("Continuous", _jogAndTeachController.ContinuousMove),
                ("Long", _jogAndTeachController.LongMove),
                ("Medium", _jogAndTeachController.MediumMove),
                ("Short", _jogAndTeachController.ShortMove),
            };
            
            foreach (var (itemName, radioButton) in radios)
                WireRadio(itemName, radioButton);

            var dropdowns = new (string itemName, DropdownField dropdownField)[]
            {
                ("Mode", _jogAndTeachController.Mode),
                ("Speed", _jogAndTeachController.Speed),
                ("Command", _jogAndTeachController.Command),
                ("Destination", _jogAndTeachController.Destination)
            };
            
            foreach (var (itemName, dropdownField) in dropdowns)
                WireDropdown(itemName, dropdownField);
        }

        private void WireButton(string itemName, Button button)
        {
            button.RegisterCallback<PointerDownEvent>(_ =>
            {
                if (itemName=="Chain/3D")
                {
                    if (_displayMode=="3D")
                    {
                        _displayMode = "Chain";
                        _arScaraKinematicChainController.HideKinematicChain();
                        _arScaraModel3DController.ShowModel3D();
                    }
                    else
                    {
                        _displayMode = "3D";
                        _arScaraKinematicChainController.ShowKinematicChain();
                        _arScaraModel3DController.HideModel3D();
                    }
                }
                LinkController model3dTargetLink = null;
                LinkController kinematicChainTargetLink = null;
                if (itemName == "Stop")
                {
                    _arScaraKinematicChainController.linkControllerB?.StopMotion();
                    _arScaraModel3DController.linkController1?.StopMotion();
                    _arScaraKinematicChainController.linkControllerD?.StopMotion();
                    _arScaraModel3DController.linkController2?.StopMotion();
                    return;
                }

                if (itemName.Contains("J1"))
                {
                    kinematicChainTargetLink = _arScaraKinematicChainController.linkControllerB;
                    model3dTargetLink =_arScaraModel3DController.linkController1;   
                }
                else if (itemName.Contains("J2"))
                {
                    kinematicChainTargetLink = _arScaraKinematicChainController.linkControllerD;
                    model3dTargetLink = _arScaraModel3DController.linkController2;
                }
                if (model3dTargetLink == null) return;
                if (kinematicChainTargetLink == null) return;
                
                kinematicChainTargetLink.speed= _speed;
                model3dTargetLink.speed = _speed;
                
                switch (itemName)
                {
                    case "PlusJ1":
                    case "PlusJ2":
                        ExecuteMotion(kinematicChainTargetLink, true);
                        ExecuteMotion(model3dTargetLink, true);
                        break;
                    case "MinusJ1":
                    case "MinusJ2":
                        ExecuteMotion(kinematicChainTargetLink, false);
                        ExecuteMotion(model3dTargetLink, false);
                        break;
                }
            }, TrickleDown.TrickleDown);

            button.RegisterCallback<PointerUpEvent>(_ => 
            {
                if (_modeMotion == "Continuous")
                {
                    if (itemName.Contains("J1"))
                    {
                        _arScaraKinematicChainController.linkControllerB?.StopMotion();
                        _arScaraModel3DController.linkController1?.StopMotion();
                    }
                    else if (itemName.Contains("J2"))
                    {
                        _arScaraKinematicChainController.linkControllerD?.StopMotion();
                        _arScaraModel3DController.linkController2?.StopMotion();
                    }
                }
            });
        }

        private void ExecuteMotion(LinkController link, bool isPositive)
        {
            switch (_modeMotion)
            {
                case "Continuous":
                    link.StartContinuousMotion(isPositive);
                    break;
                case "Long":
                    link.StartStepMotion(isPositive ? 20f : -20f, 0.1f);
                    break;
                case "Medium":
                    link.StartStepMotion(isPositive ? 10f : -10f, 0.1f);
                    break;
                case "Short":
                    link.StartStepMotion(isPositive ? 1f : -1f, 0.1f);
                    break;
            }
        }
        
        private void WireToggle(string itemName, Toggle toggle)
        {
            toggle.RegisterValueChangedCallback(evt =>
            {
                print("Toggle changed: " + itemName);
            });
        }
        
        private void WireRadio(string itemName, RadioButton radioButton)
        {
            radioButton.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _modeMotion = itemName;
                }
            });
        }
        
        private void WireDropdown(string itemName, DropdownField dropdownField)
        {
            dropdownField.RegisterValueChangedCallback(evt =>
            {
                switch (itemName)
                {
                    case "Speed":
                        switch (evt.newValue)
                        {
                            case "High":
                                _speed = 0.001f;
                                break;
                            case "Low":
                                _speed = 0.15f;
                                break;
                        }
                        break;
                }
            });
        }
    }
}