using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace _scripts.controllers
{
    public class ControlPanelController:MonoBehaviour
    {
        private GameObject _arScaraUi;
        private VisualElement _root;


        private Label 
            _emergencyStopLabel, 
            _safeguardLabel, 
            _motorsLabel,
            _powerLabel;
        
        private Button 
            _motorsOffButton,
            _motorsOnButton,
            _powerLowButton,
            _powerHighButton,
            _homeButton,
            _resetButton,
            _freeAllButton,
            _lockAllButton;
       
        private Toggle  
            _j1Toggle,
            _j2Toggle,
            _j3Toggle,
            _j4Toggle;

        internal Button MotorsOffButton
        {
            get => _motorsOffButton;
            set => _motorsOffButton = value;
        }

        internal Button MotorsOnButton
        {
            get => _motorsOnButton;
            set => _motorsOnButton = value;
        }

        internal Button PowerLowButton
        {
            get => _powerLowButton;
            set => _powerLowButton = value;
        }

        internal Button PowerHighButton
        {
            get => _powerHighButton;
            set => _powerHighButton = value;
        }

        internal Button HomeButton
        {
            get => _homeButton;
            set => _homeButton = value;
        }

        internal Button ResetButton
        {
            get => _resetButton;
            set => _resetButton = value;
        }

        internal Button FreeAllButton
        {
            get => _freeAllButton;
            set => _freeAllButton = value;
        }

        internal Button LockAllButton
        {
            get => _lockAllButton;
            set => _lockAllButton = value;
        }

        internal Toggle J1Toggle
        {
            get => _j1Toggle;
            set => _j1Toggle = value;
        }

        internal Toggle J2Toggle
        {
            get => _j2Toggle;
            set => _j2Toggle = value;
        }

        internal Toggle J3Toggle
        {
            get => _j3Toggle;
            set => _j3Toggle = value;
        }

        internal Toggle J4Toggle
        {
            get => _j4Toggle;
            set => _j4Toggle = value;
        }

        internal Label EmergencyStopLabel
        {
            get => _emergencyStopLabel;
            set => _emergencyStopLabel = value;
        }

        internal Label SafeguardLabel
        {
            get => _safeguardLabel;
            set => _safeguardLabel = value;
        }

        internal Label MotorsLabel
        {
            get => _motorsLabel;
            set => _motorsLabel = value;
        }

        internal Label PowerLabel
        {
            get => _powerLabel;
            set => _powerLabel = value;
        }

        private void OnEnable()
        {
            RegisterUiItems();
        }

        private void RegisterUiItems()
        {
            _arScaraUi = GameObject.FindWithTag("UserInterfaceArScara");
            if (_arScaraUi == null) return;
            _root = _arScaraUi.GetComponent<UIDocument>()?.rootVisualElement;
            if (_root == null) return;
            
            _motorsOffButton=_root.Q<Button>("motorsOffButton");
            _motorsOnButton=_root.Q<Button>("motorsOnButton");
            _powerLowButton=_root.Q<Button>("powerLowButton");
            _powerHighButton=_root.Q<Button>("powerHighButton");
            _homeButton=_root.Q<Button>("homeButton");
            _resetButton=_root.Q<Button>("resetButton");
            _freeAllButton=_root.Q<Button>("FreeAllButton");
            _lockAllButton=_root.Q<Button>("LockAllButton");
            
            _j1Toggle=_root.Q<Toggle>("J1Toggle");
            _j2Toggle=_root.Q<Toggle>("J2Toggle");
            _j3Toggle=_root.Q<Toggle>("J3Toggle");
            _j4Toggle=_root.Q<Toggle>("J4Toggle");
            
            _emergencyStopLabel=_root.Q<Label>("EmergencyStopLabel");
            _safeguardLabel=_root.Q<Label>("SafeguardLabel");
            _motorsLabel=_root.Q<Label>("MotorsLabel");
            _powerLabel=_root.Q<Label>("PowerLabel");
            
            
        }
    }
}