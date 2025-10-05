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
            _speed,
            _command,
            _destination;
        
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
            _minusJ4Button,
            _teachButton,
            _editButton;


        internal DropdownField Mode
        {
            get => _mode;
            set => _mode = value;
        }

        internal DropdownField Speed
        {
            get => _speed;
            set => _speed = value;
        }

        internal RadioButton ContinuousMove
        {
            get => _continuousMove;
            set => _continuousMove = value;
        }

        internal RadioButton LongMove
        {
            get => _longMove;
            set => _longMove = value;
        }

        internal RadioButton MediumMove
        {
            get => _mediumMove;
            set => _mediumMove = value;
        }

        internal RadioButton ShortMove
        {
            get => _shortMove;
            set => _shortMove = value;
        }

        internal Button PlusXButton
        {
            get => _plusXButton;
            set => _plusXButton = value;
        }

        internal Button MinusXButton
        {
            get => _minusXButton;
            set => _minusXButton = value;
        }

        internal Button PlusYButton
        {
            get => _plusYButton;
            set => _plusYButton = value;
        }

        internal Button MinusYButton
        {
            get => _minusYButton;
            set => _minusYButton = value;
        }

        internal Button PlusZButton
        {
            get => _plusZButton;
            set => _plusZButton = value;
        }

        internal Button MinusZButton
        {
            get => _minusZButton;
            set => _minusZButton = value;
        }

        internal Button PlusUButton
        {
            get => _plusUButton;
            set => _plusUButton = value;
        }

        internal Button MinusUButton
        {
            get => _minusUButton;
            set => _minusUButton = value;
        }

        internal Button PlusJ1Button
        {
            get => _plusJ1Button;
            set => _plusJ1Button = value;
        }

        internal Button MinusJ1Button
        {
            get => _minusJ1Button;
            set => _minusJ1Button = value;
        }

        internal Button PlusJ2Button
        {
            get => _plusJ2Button;
            set => _plusJ2Button = value;
        }

        internal Button MinusJ2Button
        {
            get => _minusJ2Button;
            set => _minusJ2Button = value;
        }

        internal Button PlusJ3Button
        {
            get => _plusJ3Button;
            set => _plusJ3Button = value;
        }

        internal Button MinusJ3Button
        {
            get => _minusJ3Button;
            set => _minusJ3Button = value;
        }

        internal Button PlusJ4Button
        {
            get => _plusJ4Button;
            set => _plusJ4Button = value;
        }

        internal Button MinusJ4Button
        {
            get => _minusJ4Button;
            set => _minusJ4Button = value;
        }

        internal Button TeachButton
        {
            get => _teachButton;
            set => _teachButton = value;
        }

        internal Button EditButton
        {
            get => _editButton;
            set => _editButton = value;
        }
        
        internal DropdownField Command
        {
            get => _command;
            set => _command = value;
        }
        internal DropdownField Destination
        {
            get => _destination;
            set => _destination = value;
        }
        private void Start()
        {
            if (_shortMove != null) _shortMove.value = true;
        }

        private void OnEnable()
        {
            RegisterUiItems();
        }
        
        
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

            _teachButton= _root.Q<Button>("TeachButton");
            _editButton= _root.Q<Button>("EditButton");
            
            _continuousMove = _root.Q<RadioButton>("continuousMove");
            _longMove       = _root.Q<RadioButton>("longMove");
            _mediumMove     = _root.Q<RadioButton>("mediumMove");
            _shortMove      = _root.Q<RadioButton>("shortMove");
            
            _command= _root.Q<DropdownField>("CommandDropdown");
            _destination= _root.Q<DropdownField>("DestinationDropdown");
        }
    }
}
