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
        private RobotKinematics  _kinematics;
        
        private Transform _axisLink1;
        private Transform _axisLink2;
        
        private float _rotationAxisLink1;
        private float _rotationAxisLink2;
        private string _speedMotion;
        private string _modeMotion;
        private Coroutine _motionCoroutine;

        private float _speed;
        
        private float _minimumAngleJ1;
        private float _maximumAngleJ1;
        private float _minimumAngleJ2;
        private float _maximumAngleJ2;
        private void Awake()
        {
            _controlPanelController= GameObject.FindWithTag("VirtualEnvironmentArScara").GetComponent<ControlPanelController>();
            _jogAndTeachController = GameObject.FindWithTag("VirtualEnvironmentArScara").GetComponent<JogAndTeachController>();
            _kinematics = new RobotKinematics();
        }
        private void Start()
        {
            if (_jogAndTeachController.ContinuousMove.value)
            {
                _modeMotion = "Continuous";    
            }
            
            _minimumAngleJ1 = -90f;
            _maximumAngleJ1 = 90f;
            _minimumAngleJ2 = -150f;
            _maximumAngleJ2 = 150f;
            if (_controlPanelController!=null)
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

            _axisLink1 = transform.Find("Model3D/XL430W250T1/AxisLink1");
            _axisLink2 = transform.Find("Model3D/XL430W250T1/AxisLink1/Link1/XL430W250T2/AxisLink2");
        }

        private void Update()
        {
            var angleLink1 = _axisLink1.transform.localEulerAngles.y;
            var angleLink2 = _axisLink2.transform.localEulerAngles.y;
            
            if (angleLink1 > 180f) angleLink1 -= 360f;
            if (angleLink2 > 180f) angleLink2 -= 360f;
            
            _jogAndTeachController.J1Label.text = $"J1: {angleLink1} deg";
            _jogAndTeachController.J2Label.text = $"J2: {angleLink2} deg";
        }

        private void FixedUpdate()
        {
            _axisLink1.transform.localRotation = Quaternion.Euler(0, _rotationAxisLink1, 0);
            _axisLink2.transform.localRotation = Quaternion.Euler(0, _rotationAxisLink2, 0); 
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
                ("J3", _controlPanelController.J3Toggle),
                ("J4", _controlPanelController.J4Toggle),
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
                ("PlusZ", _jogAndTeachController.PlusZButton),
                ("MinusZ", _jogAndTeachController.MinusZButton),
                ("PlusU", _jogAndTeachController.PlusUButton),
                ("MinusU", _jogAndTeachController.MinusUButton),
                
                ("PlusJ1", _jogAndTeachController.PlusJ1Button),
                ("MinusJ1", _jogAndTeachController.MinusJ1Button),
                ("PlusJ2", _jogAndTeachController.PlusJ2Button),
                ("MinusJ2", _jogAndTeachController.MinusJ2Button),
                ("PlusJ3", _jogAndTeachController.PlusJ3Button),
                ("MinusJ3", _jogAndTeachController.MinusJ3Button),
                ("PlusJ4", _jogAndTeachController.PlusJ4Button),
                ("MinusJ4", _jogAndTeachController.MinusJ4Button),
                
                ("Teach", _jogAndTeachController.TeachButton),
                ("Edit", _jogAndTeachController.EditButton)
                
            };
            foreach (var (itemName, button) in buttons)
                WireButton(itemName, button);


            var radios = new (string itemName, RadioButton radioButton)[]
            {
                ("Continuous",_jogAndTeachController.ContinuousMove),
                ("Long",_jogAndTeachController.LongMove),
                ("Medium",_jogAndTeachController.MediumMove),
                ("Short",_jogAndTeachController.ShortMove),
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

        // establecer velocidad
        private void WireButton(string itemName, Button Button)
        {
            Button.RegisterCallback<PointerDownEvent>(_ =>
            {
                switch (itemName)
                {
                    case "PlusJ1":
                        switch (_modeMotion)
                        {
                            case "Continuous":
                                _motionCoroutine ??= StartCoroutine(ContinuousMotion(true,1));
                                break;
                            case "Long":
                                _motionCoroutine??= StartCoroutine(StepMotion(20,0.1f,1));
                                break;
                            case "Medium":
                                _motionCoroutine??= StartCoroutine(StepMotion(10,0.1f,1));
                                break;
                            case "Short":
                                _motionCoroutine??= StartCoroutine(StepMotion(1f,0.1f,1));
                                break;
                        }
                        break;
                    case "MinusJ1":
                        switch (_modeMotion)
                        {
                            case "Continuous":
                                _motionCoroutine ??= StartCoroutine(ContinuousMotion(false,1));
                                break;
                            case "Long":
                                _motionCoroutine??= StartCoroutine(StepMotion(-20,0.1f,1));
                                break;
                            case "Medium":
                                _motionCoroutine??= StartCoroutine(StepMotion(-10,0.1f,1));
                                break;
                            case "Short":
                                _motionCoroutine??= StartCoroutine(StepMotion(-1f,0.1f,1));
                                break;
                        }
                        break;
                    case "PlusJ2":
                        switch (_modeMotion)
                        {
                            case "Continuous":
                                _motionCoroutine ??= StartCoroutine(ContinuousMotion(true, 2));
                                break;
                            case "Long":
                                _motionCoroutine??= StartCoroutine(StepMotion(20, 0.1f, 2));
                                break;
                            case "Medium":
                                _motionCoroutine??= StartCoroutine(StepMotion(10, 0.1f, 2));
                                break;
                            case "Short":
                                _motionCoroutine??= StartCoroutine(StepMotion(1f, 0.1f, 2));
                                break;
                        }
                        break;
                    case "MinusJ2":
                        switch (_modeMotion)
                        {
                            case "Continuous":
                                _motionCoroutine ??= StartCoroutine(ContinuousMotion(false, 2));
                                break;
                            case "Long":
                                _motionCoroutine??= StartCoroutine(StepMotion(-20, 0.1f, 2));
                                break;
                            case "Medium":
                                _motionCoroutine??= StartCoroutine(StepMotion(-10, 0.1f, 2));
                                break;
                            case "Short":
                                _motionCoroutine??= StartCoroutine(StepMotion(-1f, 0.1f, 2));
                                break;
                        }
                        break;
                    
                    
                }
            }, TrickleDown.TrickleDown);

            Button.RegisterCallback<PointerUpEvent>(_ => 
            {
                if (_modeMotion=="Continuous")
                {
                    if (_motionCoroutine != null)
                    {
                        StopCoroutine(_motionCoroutine);
                        _motionCoroutine = null;
                    }
                }
            });
        }
        
        private void WireToggle(string itemName, Toggle toggle)
        {
            toggle.RegisterValueChangedCallback(evt =>
            {
                print("holi");
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
                _speedMotion = evt.newValue;
                switch (itemName)
                {
                    case "Speed":
                        switch (_speedMotion)
                        {
                            case "High":
                                _speed = 0.001f ;
                                break;
                            case "Low":
                                _speed = 0.15f;
                                break;
                        }
                        break;
                }
            });
        }

        private IEnumerator ContinuousMotion(bool direction, int axisLink)
        {
            yield return new WaitForSeconds(0.3f);
            if (_speedMotion is "High" or "Low")
            {
                while (true)
                {
                    float increment = direction ? 0.25f : -0.25f;
        
                    switch (axisLink)
                    {
                        case 1:
                            float newRotation1 = _rotationAxisLink1 + increment;
                            if (newRotation1 >= _minimumAngleJ1 && newRotation1 <= _maximumAngleJ1)
                            {
                                _rotationAxisLink1 = newRotation1;
                            }
                            else
                            {
                                _rotationAxisLink1 = Mathf.Clamp(_rotationAxisLink1, _minimumAngleJ1, _maximumAngleJ1);
                                _motionCoroutine = null;
                                yield break;
                            }
                            break;
                        case 2:
                            float newRotation2 = _rotationAxisLink2 + increment;
                            if (newRotation2 >= _minimumAngleJ2 && newRotation2 <= _maximumAngleJ2)
                            {
                                _rotationAxisLink2 = newRotation2;
                            }
                            else
                            {
                                _rotationAxisLink2 = Mathf.Clamp(_rotationAxisLink2, _minimumAngleJ2, _maximumAngleJ2);
                                _motionCoroutine = null;
                                yield break;
                            }
                            break;
                    }
        
                    yield return new WaitForSeconds(_speed);
                }   
            }
            
        }
        
        private IEnumerator StepMotion(float target, float step, int axisLink)
        {
            yield return new WaitForSeconds(0.3f);
    
            float startPosition = axisLink == 1 ? _rotationAxisLink1 : _rotationAxisLink2;
            float endPosition = startPosition + target;
            
            switch (axisLink)
            {
                case 1:
                    endPosition = Mathf.Clamp(endPosition, _minimumAngleJ1, _maximumAngleJ1);
                    break;
                case 2:
                    endPosition = Mathf.Clamp(endPosition, _minimumAngleJ2, _maximumAngleJ2);
                    break;
            }
    
            float currentPosition = startPosition;

            while (Mathf.Abs(endPosition - currentPosition) > 0.1f)
            {
                float direction = Mathf.Sign(endPosition - currentPosition);
                currentPosition += step * direction;
        
                if (direction > 0)
                    currentPosition = Mathf.Min(currentPosition, endPosition);
                else
                    currentPosition = Mathf.Max(currentPosition, endPosition);
    
                switch (axisLink)
                {
                    case 1:
                        _rotationAxisLink1 = Mathf.Clamp(currentPosition, _minimumAngleJ1, _maximumAngleJ1);
                        break;
                    case 2:
                        _rotationAxisLink2 = Mathf.Clamp(currentPosition, _minimumAngleJ2, _maximumAngleJ2);
                        break;
                }
    
                yield return new WaitForSeconds(_speed);
            }
    
            switch (axisLink)
            {
                case 1:
                    _rotationAxisLink1 = Mathf.Clamp(endPosition, _minimumAngleJ1, _maximumAngleJ1);
                    break;
                case 2:
                    _rotationAxisLink2 = Mathf.Clamp(endPosition, _minimumAngleJ2, _maximumAngleJ2);
                    break;
            }

            _motionCoroutine = null;
        }
    }
}