using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace _scripts.controllers
{
    public class TwinNexusController:MonoBehaviour
    {
        private GameObject _arScaraUi;
        private VisualElement _root;
        private DropdownField _mode,_speed;
        private RadioButton _continuousMove, _longMove, _mediumMove, _shortMove;
        private Button _plusXButton, _minusXButton,_plusYButton,_minusYButton,_plusZButton,_minusZButton,_plusUButton,
                       _minusUButton, _plusJ1Button, _minusJ1Button, _plusJ2Button, _minusJ2Button, _plusJ3Button,
                       _minusJ3Button, _plusJ4Button, _minusJ4Button;
        private float _xPosition, _yPosition, _zPosition,_uPosition,_j1Position,_j2Position,_j3Position,_j4Position,_step;
        private void Awake()
        {
            RegisterUiItems();
            RegisterUiEvents();
        }
        private void MoveAxis(ClickEvent evt, string axis, string direction )
        {
            switch (axis)
            {
                case "X":
                    _xPosition += direction == "+" ? _step : -_step;
                    _xPosition = Mathf.Clamp(_xPosition, -180f, 180f);
                    print($"Axis: {axis}, Direction: {direction}, Step: {_step}, XPosition: {_xPosition}");
                    break;
                case "Y":
                    _yPosition += direction == "+" ? _step : -_step;
                    _yPosition = Mathf.Clamp(_yPosition, -180f, 180f);
                    print($"Axis: {axis}, Direction: {direction}, Step: {_step}, YPosition: {_yPosition}");
                    break;
                case "Z":
                    _zPosition += direction == "+" ? _step : -_step;
                    _zPosition = Mathf.Clamp(_zPosition, -10f, 10f);
                    print($"Axis: {axis}, Direction: {direction}, Step: {_step}, ZPosition: {_zPosition}");
                    break;
                case "U":
                    _uPosition += direction == "+" ? _step : -_step;
                    _uPosition = Mathf.Clamp(_uPosition, -180f, 180f);
                    print($"Axis: {axis}, Direction: {direction}, Step: {_step}, UPosition: {_uPosition}");
                    break;
            }
        }
        private void MoveJoint(ClickEvent evt, int joint, string direction)
        {
            switch (joint)
            {
                case 1:
                    _j1Position += direction == "+" ? _step : -_step;
                    _j1Position = Mathf.Clamp(_j1Position, -180f, 180f);
                    print($"join:{joint}, direction:{direction}, Step: {_step}, XPosition: {_j1Position}");
                    break;
                case 2:
                    _j2Position += direction == "+" ? _step : -_step;
                    _j2Position = Mathf.Clamp(_j2Position, -180f, 180f);
                    print($"join:{joint}, direction:{direction}, Step: {_step}, YPosition: {_j2Position}");
                    break;
                case 3:
                    _j3Position += direction == "+" ? _step : -_step;
                    _j3Position = Mathf.Clamp(_j3Position, -10f, 10f);
                    print($"join:{joint}, direction:{direction}, Step: {_step}, ZPosition: {_j3Position}");
                    break;
                case 4:
                    _j4Position += direction == "+" ? _step : -_step;
                    _j4Position = Mathf.Clamp(_j4Position, -180f, 180f);
                    print($"join:{joint}, direction:{direction}, Step: {_step}, UPosition: {_j4Position}");
                    break;
            }
        }
        private void RegisterUiItems()
        {
            _arScaraUi = GameObject.FindWithTag("arScaraUi");
            _root = _arScaraUi.GetComponent<UIDocument>().rootVisualElement;
            _mode= _root.Q<DropdownField>("modeDropdown");
            _speed = _root.Q<DropdownField>("speedDropdown");
            _plusXButton= _root.Q<Button>("plusXButton");
            _minusXButton= _root.Q<Button>("minusXButton");
            _plusYButton= _root.Q<Button>("plusYButton");
            _minusYButton= _root.Q<Button>("minusYButton");
            _plusZButton= _root.Q<Button>("plusZButton");
            _minusZButton= _root.Q<Button>("minusZButton");
            _plusUButton= _root.Q<Button>("plusUButton");
            _minusUButton= _root.Q<Button>("minusUButton");
            _plusJ1Button = _root.Q<Button>("plusJ1Button");
            _minusJ1Button = _root.Q<Button>("minusJ1Button");
            _plusJ2Button = _root.Q<Button>("plusJ2Button");
            _minusJ2Button = _root.Q<Button>("minusJ2Button");
            _plusJ3Button = _root.Q<Button>("plusJ3Button");
            _minusJ3Button = _root.Q<Button>("minusJ3Button");
            _plusJ4Button = _root.Q<Button>("plusJ4Button");
            _minusJ4Button = _root.Q<Button>("minusJ4Button");
            _continuousMove=_root.Q<RadioButton>("continuousMove");
            _longMove=_root.Q<RadioButton>("longMove");
            _mediumMove=_root.Q<RadioButton>("mediumMove");
            _shortMove=_root.Q<RadioButton>("shortMove");
        }
        private void RegisterUiEvents()
        {
            _mode.RegisterValueChangedCallback(value => print(value.newValue));
            _speed.RegisterValueChangedCallback(value => print(value.newValue));
            _continuousMove.RegisterValueChangedCallback(evt=>print("spare"));
            _longMove.RegisterValueChangedCallback(evt => {if (evt.newValue) _step = 5f;});
            _mediumMove.RegisterValueChangedCallback(evt => {if (evt.newValue) _step = 2.5f;});
            _shortMove.RegisterValueChangedCallback(evt => {if (evt.newValue) _step = 1.25f;});
            _plusXButton.RegisterCallback<ClickEvent>(evt=>MoveAxis(evt,"X", "+"));
            _minusXButton.RegisterCallback<ClickEvent>(evt=>MoveAxis(evt,"X", "-"));
            _plusYButton.RegisterCallback<ClickEvent>(evt=>MoveAxis(evt,"Y", "+"));
            _minusYButton.RegisterCallback<ClickEvent>(evt=>MoveAxis(evt,"Y", "-"));
            _plusZButton.RegisterCallback<ClickEvent>(evt=>MoveAxis(evt,"Z", "+"));
            _minusZButton.RegisterCallback<ClickEvent>(evt=>MoveAxis(evt,"Z", "-"));
            _plusUButton.RegisterCallback<ClickEvent>(evt=>MoveAxis(evt,"U", "+"));
            _minusUButton.RegisterCallback<ClickEvent>(evt=>MoveAxis(evt,"U", "-"));
            _plusJ1Button.RegisterCallback<ClickEvent>(evt=>MoveJoint(evt,1, "+"));
            _minusJ1Button.RegisterCallback<ClickEvent>(evt=>MoveJoint(evt,1, "-"));
            _plusJ2Button.RegisterCallback<ClickEvent>(evt=>MoveJoint(evt,2, "+"));
            _minusJ2Button.RegisterCallback<ClickEvent>(evt=>MoveJoint(evt,2, "-"));
            _plusJ3Button.RegisterCallback<ClickEvent>(evt=>MoveJoint(evt,3, "+"));
            _minusJ3Button.RegisterCallback<ClickEvent>(evt=>MoveJoint(evt,3, "-"));
            _plusJ4Button.RegisterCallback<ClickEvent>(evt=>MoveJoint(evt,4, "+"));
            _minusJ4Button.RegisterCallback<ClickEvent>(evt=>MoveJoint(evt,4, "-"));
        }
    }
}