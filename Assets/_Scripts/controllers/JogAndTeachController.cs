using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace _scripts.controllers
{
    public class JogAndTeachController : MonoBehaviour
    {
        private GameObject _arScaraUi;
        private VisualElement _root;

        private DropdownField 
            _mode, 
            _speed;
        
        private RadioButton 
            _continuousMove,
            _longMove, 
            _mediumMove,
            _shortMove;

        private Button
            _plusXButton,
            _minusXButton,
            _plusYButton,
            _minusYButton,
            _plusZButton,
            _minusZButton,
            _plusUButton,
            _minusUButton,
            _plusJ1Button,
            _minusJ1Button,
            _plusJ2Button,
            _minusJ2Button,
            _plusJ3Button,
            _minusJ3Button,
            _plusJ4Button,
            _minusJ4Button;
        
        private float 
            _xPosition,
            _yPosition,
            _zPosition, 
            _uPosition, 
            _j1Rotation,
            _j2Rotation,
            _j3Prismatic,
            _j4Rotation,
            _step = 1.25f;

        internal string Mode=>_mode.value;
        internal string Speed=>_speed.value;
        

        public float XPosition => _xPosition;

        public float YPosition => _yPosition;

        public float ZPosition => _zPosition;

        public float UPosition => _uPosition;

        public float J1Rotation => _j1Rotation;

        public float J2Rotation => _j2Rotation;

        public float J3Prismatic => _j3Prismatic;

        public float J4Rotation => _j4Rotation;

        // --- Cached handlers (IMPORTANT: same instances for unregister) ---
        private EventCallback<ChangeEvent<bool>> _onContChanged, _onLongChanged, _onMedChanged, _onShortChanged;

        private EventCallback<ClickEvent> _onPlusX, _onMinusX, _onPlusY, _onMinusY;
        private EventCallback<ClickEvent> _onPlusZ, _onMinusZ, _onPlusU, _onMinusU;

        private EventCallback<ClickEvent> _onPlusJ1, _onMinusJ1, _onPlusJ2, _onMinusJ2;
        private EventCallback<ClickEvent> _onPlusJ3, _onMinusJ3, _onPlusJ4, _onMinusJ4;

        // -------------------------------------------------------------

        private void Start()
        {
            if (_shortMove != null) _shortMove.value = true;
        }

        private void OnEnable()
        {
            RegisterUiItems();
            BuildHandlers();
            RegisterUiEvents();
        }

        private void OnDisable()
        {
            UnregisterUiEvents();
        }

        // -------------------------------------------------------------
        // UI Wiring
        // -------------------------------------------------------------

        private void RegisterUiItems()
        {
            _arScaraUi = GameObject.FindWithTag("arScaraUi");
            if (_arScaraUi == null) return;

            _root = _arScaraUi.GetComponent<UIDocument>()?.rootVisualElement;
            if (_root == null) return;
            _mode  = _root.Q<DropdownField>("modeDropdown");
            _speed = _root.Q<DropdownField>("speedDropdown");

            _plusXButton = _root.Q<Button>("plusXButton");
            _minusXButton = _root.Q<Button>("minusXButton");
            _plusYButton = _root.Q<Button>("plusYButton");
            _minusYButton = _root.Q<Button>("minusYButton");
            _plusZButton = _root.Q<Button>("plusZButton");
            _minusZButton = _root.Q<Button>("minusZButton");
            _plusUButton = _root.Q<Button>("plusUButton");
            _minusUButton = _root.Q<Button>("minusUButton");

            _plusJ1Button = _root.Q<Button>("plusJ1Button");
            _minusJ1Button = _root.Q<Button>("minusJ1Button");
            _plusJ2Button = _root.Q<Button>("plusJ2Button");
            _minusJ2Button = _root.Q<Button>("minusJ2Button");
            _plusJ3Button = _root.Q<Button>("plusJ3Button");
            _minusJ3Button = _root.Q<Button>("minusJ3Button");
            _plusJ4Button = _root.Q<Button>("plusJ4Button");
            _minusJ4Button = _root.Q<Button>("minusJ4Button");

            _continuousMove = _root.Q<RadioButton>("continuousMove");
            _longMove       = _root.Q<RadioButton>("longMove");
            _mediumMove     = _root.Q<RadioButton>("mediumMove");
            _shortMove      = _root.Q<RadioButton>("shortMove");
        }

        private void BuildHandlers()
        {
            _onContChanged  = evt => { if (evt.newValue) Debug.Log("Continuous move selected (placeholder)"); };
            _onLongChanged  = evt => { if (evt.newValue) _step = 10f;    };
            _onMedChanged   = evt => { if (evt.newValue) _step = 5f;  };
            _onShortChanged = evt => { if (evt.newValue) _step = 1f; };

            // Click handlers (WORLD)
            _onPlusX  = evt => MoveAxis(evt, "X", "+");
            _onMinusX = evt => MoveAxis(evt, "X", "-");
            _onPlusY  = evt => MoveAxis(evt, "Y", "+");
            _onMinusY = evt => MoveAxis(evt, "Y", "-");
            _onPlusZ  = evt => MoveAxis(evt, "Z", "+");
            _onMinusZ = evt => MoveAxis(evt, "Z", "-");
            _onPlusU  = evt => MoveAxis(evt, "U", "+");
            _onMinusU = evt => MoveAxis(evt, "U", "-");

            // Click handlers (JOINTS)
            _onPlusJ1  = evt => MoveJoint(evt, 1, "+");
            _onMinusJ1 = evt => MoveJoint(evt, 1, "-");
            _onPlusJ2  = evt => MoveJoint(evt, 2, "+");
            _onMinusJ2 = evt => MoveJoint(evt, 2, "-");
            _onPlusJ3  = evt => MoveJoint(evt, 3, "+");
            _onMinusJ3 = evt => MoveJoint(evt, 3, "-");
            _onPlusJ4  = evt => MoveJoint(evt, 4, "+");
            _onMinusJ4 = evt => MoveJoint(evt, 4, "-");
        }

        private void RegisterUiEvents()
        {
            if (_root == null) return;
            
            _continuousMove?.RegisterValueChangedCallback(_onContChanged);
            _longMove?.RegisterValueChangedCallback(_onLongChanged);
            _mediumMove?.RegisterValueChangedCallback(_onMedChanged);
            _shortMove?.RegisterValueChangedCallback(_onShortChanged);

            _plusXButton?.RegisterCallback(_onPlusX);
            _minusXButton?.RegisterCallback(_onMinusX);
            _plusYButton?.RegisterCallback(_onPlusY);
            _minusYButton?.RegisterCallback(_onMinusY);
            _plusZButton?.RegisterCallback(_onPlusZ);
            _minusZButton?.RegisterCallback(_onMinusZ);
            _plusUButton?.RegisterCallback(_onPlusU);
            _minusUButton?.RegisterCallback(_onMinusU);

            _plusJ1Button?.RegisterCallback(_onPlusJ1);
            _minusJ1Button?.RegisterCallback(_onMinusJ1);
            _plusJ2Button?.RegisterCallback(_onPlusJ2);
            _minusJ2Button?.RegisterCallback(_onMinusJ2);
            _plusJ3Button?.RegisterCallback(_onPlusJ3);
            _minusJ3Button?.RegisterCallback(_onMinusJ3);
            _plusJ4Button?.RegisterCallback(_onPlusJ4);
            _minusJ4Button?.RegisterCallback(_onMinusJ4);
        }

        private void UnregisterUiEvents()
        {
            if (_root == null) return;

            _continuousMove?.UnregisterValueChangedCallback(_onContChanged);
            _longMove?.UnregisterValueChangedCallback(_onLongChanged);
            _mediumMove?.UnregisterValueChangedCallback(_onMedChanged);
            _shortMove?.UnregisterValueChangedCallback(_onShortChanged);

            _plusXButton?.UnregisterCallback(_onPlusX);
            _minusXButton?.UnregisterCallback(_onMinusX);
            _plusYButton?.UnregisterCallback(_onPlusY);
            _minusYButton?.UnregisterCallback(_onMinusY);
            _plusZButton?.UnregisterCallback(_onPlusZ);
            _minusZButton?.UnregisterCallback(_onMinusZ);
            _plusUButton?.UnregisterCallback(_onPlusU);
            _minusUButton?.UnregisterCallback(_onMinusU);

            _plusJ1Button?.UnregisterCallback(_onPlusJ1);
            _minusJ1Button?.UnregisterCallback(_onMinusJ1);
            _plusJ2Button?.UnregisterCallback(_onPlusJ2);
            _minusJ2Button?.UnregisterCallback(_onMinusJ2);
            _plusJ3Button?.UnregisterCallback(_onPlusJ3);
            _minusJ3Button?.UnregisterCallback(_onMinusJ3);
            _plusJ4Button?.UnregisterCallback(_onPlusJ4);
            _minusJ4Button?.UnregisterCallback(_onMinusJ4);
        }

        // -------------------------------------------------------------
        // Movement logic
        // -------------------------------------------------------------

        private void MoveAxis(ClickEvent evt, string axis, string direction)
        {
            switch (axis)
            {
                case "X":
                    _xPosition += direction == "+" ? _step : -_step;
                    _xPosition = Mathf.Clamp(_xPosition, -90f, 90f);
                    break;

                case "Y":
                    _yPosition += direction == "+" ? _step : -_step;
                    _yPosition = Mathf.Clamp(_yPosition, -90f, 90f);
                    break;

                case "Z":
                    _zPosition += direction == "+" ? _step : -_step;
                    _zPosition = Mathf.Clamp(_zPosition, -10f, 0f);
                    break;

                case "U":
                    _uPosition += direction == "+" ? _step : -_step;
                    _uPosition = Mathf.Clamp(_uPosition, -180f, 180f);
                    break;
            }
            
        }

        private void MoveJoint(ClickEvent evt, int joint, string direction)
        {
            switch (joint)
            {
                case 1:
                    _j1Rotation += direction == "+" ? _step : -_step;
                    break;

                case 2:
                    _j2Rotation += direction == "+" ? _step : -_step;
                    break;

                case 3:
                    _j3Prismatic += direction == "+" ? _step : -_step;
                    break;

                case 4:
                    _j4Rotation += direction == "+" ? _step : -_step;
                    break;
            }
        }
    }
}
