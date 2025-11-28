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
        
        private LinkController _linkController1;
        private LinkController _linkController2;
        
        private string _modeMotion;
        private float _speed;

        private void Awake()
        {
            _controlPanelController = GameObject.FindWithTag("VirtualEnvironmentArScara").GetComponent<ControlPanelController>();
            _jogAndTeachController = GameObject.FindWithTag("VirtualEnvironmentArScara").GetComponent<JogAndTeachController>();
        }

        private void Start()
        {
            if (_jogAndTeachController.ContinuousMove.value)
            {
                _modeMotion = "Continuous";    
            }
            
            _linkController1 = transform.Find("Model3D/Base/XL430W250T1/AxisLink1").GetComponent<LinkController>();
            _linkController2 = transform.Find("Model3D/Base/XL430W250T1/AxisLink1/Link1/XL430W250T2/AxisLink2").GetComponent<LinkController>();
            
            if (_linkController1 != null)
            {
                _linkController1.minimumAngle = -90f;
                _linkController1.maximumAngle = 90f;
            }
            else
            {
                Debug.LogError("LinkController1 not found!");
            }
            
            if (_linkController2 != null)
            {
                _linkController2.minimumAngle = -150f;
                _linkController2.maximumAngle = 150f;
            }
            else
            {
                Debug.LogError("LinkController2 not found!");
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
            if (_linkController1 != null && _linkController2 != null)
            {
                var angleLink1 = _linkController1.GetNormalizedAngle();
                var angleLink2 = _linkController2.GetNormalizedAngle();
                
                _jogAndTeachController.J1Label.text = $"J1: {angleLink1:F2} deg";
                _jogAndTeachController.J2Label.text = $"J2: {angleLink2:F2} deg";
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
                ("Stop", _jogAndTeachController.StopButton)
                
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
                LinkController targetLink = null;
                
                if (itemName == "Stop")
                {
                    // modelo 3d
                    // cadena cinematica
                    _linkController1?.StopMotion();
                    // modelo 3d
                    // cadena cinematica
                    _linkController2?.StopMotion();
                    return;
                }
                
                if (itemName.Contains("J1"))
                    // modelo 3d
                    // cadena cinematica
                    targetLink = _linkController1;
                else if (itemName.Contains("J2"))
                    // modelo 3d
                    // cadena cinematica
                    targetLink = _linkController2;
                
                if (targetLink == null) return;
                // modelo 3d
                // cadena cinematica
                targetLink.speed = _speed;
                
                switch (itemName)
                {
                    case "PlusJ1":
                    case "PlusJ2":
                        // modelo 3d
                        // cadena cinematica
                        ExecuteMotion(targetLink, true);
                        break;
                    case "MinusJ1":
                    case "MinusJ2":
                        // modelo 3d
                        // cadena cinematica
                        ExecuteMotion(targetLink, false);
                        break;
                }
            }, TrickleDown.TrickleDown);

            button.RegisterCallback<PointerUpEvent>(_ => 
            {
                if (_modeMotion == "Continuous")
                {
                    if (itemName.Contains("J1"))
                        // modelo 3d
                        // cadena cinematica
                        _linkController1?.StopMotion();
                    else if (itemName.Contains("J2"))
                        // modelo 3d
                        // cadena cinematica
                        _linkController2?.StopMotion();
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