using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _scripts.controllers
{
    public class ArScaraUiController : MonoBehaviour
    {
        [Header("Controllers")]
        private ArScaraEnvironmentController _environmentController;

        private enum ModeType { None, World, Joint }
        private enum KinematicsType { None, Forward, Reverse}
        
        private enum MethodForwardType { None, Geometric, HTM, DenavitHartenberg }
        private enum MethodReverseType {None,a,b,c }
        
        private enum ArScaraUiMenu { None, Control, JogTeach, Points }
        private enum Placement { None, Raycast, QRMarker }

        private static class Uss
        {
            public const string Show = "showItem";
            public const string Hide = "hideItem";

            public const string ScrimOpaque = "scrimOpaque";
            public const string ScrimTransparent = "scrimTransparent";

            public const string BgOpaque = "backgroundOpaque";
            public const string BgTransparent = "backgroundTransparent";

            public const string MainMenuBase = "mainMenuPanel";
            public const string PanelIn = "In";
            public const string PanelOut = "Out";
        }

        private static class Id
        {
            public const string Body = "body";
            public const string SlidingPanels = "slidingPanels";
            public const string Scrim = "scrim";
            public const string NavPanel = "navigationMenuPanel";

            public const string ShowMenu = "showMenuButton";
            public const string HideMenu = "hideMenuButton";

            public const string Warning = "warningMessages";

            public const string EnvMenu = "environmentMenu";
            public const string ArScaraMenu = "arscaraMenu";
            public const string Placement = "placementMenu";
            public const string Views = "views";
            public const string Mode = "modeDropdown";
            public const string Speed = "speedDropdown";
            public const string Destination = "DestinationDropdown";
            public const string Points = "pointsDropdown";

            public const string ControlPanel = "controlPanel";
            public const string JogTeachPanel = "jogAndTeachPanel";
            public const string PointsPanel = "pointsPanel";

            public const string BpQ1 = "plusQ1Button";    public const string BmQ1 = "minusQ1Button";
            public const string BpQ2 = "plusQ2Button";    public const string BmQ2 = "minusQ2Button";
            
            public const string BpX = "plusXButton";    public const string BmX = "minusXButton";
            public const string BpY = "plusYButton";    public const string BmY = "minusYButton";

            public const string BpJ1 = "plusJ1Button";  public const string BmJ1 = "minusJ1Button";
            public const string BpJ2 = "plusJ2Button";  public const string BmJ2 = "minusJ2Button";

            public const string LX = "xLabel"; public const string LY = "yLabel";
            public const string LQ1 = "q1Label"; public const string LQ2 = "q2Label";

            public const string LJ1 = "j1Label"; public const string LJ2 = "j2Label";

            public const string Teach = "TeachButton";
            public const string Edit = "EditButton";
            public const string Run = "RunButton";
            public const string Stop = "StopButton";
            public const string Emergency = "EmergencyButton";
            public const string ChainOr3D = "ChainOr3DButton";
            public const string Results = "ResultsButton";

            public const string MoveCont = "continuousMove";
            public const string MoveLong = "longMove";
            public const string MoveMed  = "mediumMove";
            public const string MoveShort= "shortMove";
            
            public const string Kinematics= "KinematicsDropdown";
            public const string MethodForward= "MethodForwardDropdown";
            public const string MethodReverse= "MethodReverseDropdown";
            
            public const string Equations= "equationsPanel";
            
            
        }

        private UIDocument _doc;
        private VisualElement _root;
        private VisualElement _body, _slidingPanels, _scrim, _navPanel,_equations;
        private Label _warning;
        private DropdownField _environmentMenu, _arScaraUiMenu, _views, _mode, _speed, _destination, _points,_kinematics,_methodForward,_placementMenu,_methodReverse;
        private VisualElement _controlPanel, _jogTeachPanel, _pointsPanel;
        private Button _chainOr3D,_bpQ1,_bmQ1,_bpQ2, _bmQ2,_results,_run,_stop,_emergency,_teach,_edit,_bpX,_bmX, _bpY,
            _bmY,_bpJ1,_bmJ1,_bpJ2,_bmJ2;
        private Label _labelX, _labelY, _labelQ1, _labelQ2,_labelJ1,_labelJ2;
        private RadioButton _moveContinuous, _moveLong, _moveHalf, _moveShort;
        
        private readonly List<VisualElement> _worldControlsGroup = new();
        private readonly List<VisualElement> _jointControlsGroup = new();
        
        private readonly List<VisualElement> _kinematicsReverseGroup = new();
        private readonly List<VisualElement> _methodForwardControlsGroup = new();
        private readonly List<VisualElement> _virtualControlsGroup = new();
        private readonly List<VisualElement> _arControlsGroup = new();
        private readonly List<VisualElement> _twinNexusControlsGroup = new();

        internal DropdownField Views
        {
            get => _views;
            set => _views = value;
        }

        private void Awake()
        {
            if (_environmentController == null)
            {
                _environmentController = GameObject.FindWithTag("EnvironmentControllerArScara").GetComponent<ArScaraEnvironmentController>();
                if (_environmentController == null)
                {
                    Debug.LogError("ArScaraEnvironmentController not found! Please assign it in the Inspector.");
                }
            }

            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;

            _body = Q<VisualElement>(Id.Body);
            _slidingPanels = Q<VisualElement>(Id.SlidingPanels);
            _scrim = Q<VisualElement>(Id.Scrim);
            _navPanel = Q<VisualElement>(Id.NavPanel);

            _warning = Q<Label>(Id.Warning);

            _environmentMenu = Q<DropdownField>(Id.EnvMenu);
            _arScaraUiMenu = Q<DropdownField>(Id.ArScaraMenu);
            _placementMenu = Q<DropdownField>(Id.Placement);

            _views = Q<DropdownField>(Id.Views);
            _mode = Q<DropdownField>(Id.Mode);
            _speed = Q<DropdownField>(Id.Speed);
            _destination = Q<DropdownField>(Id.Destination);
            _points = Q<DropdownField>(Id.Points);

            _controlPanel = Q<VisualElement>(Id.ControlPanel);
            _jogTeachPanel = Q<VisualElement>(Id.JogTeachPanel);
            _pointsPanel = Q<VisualElement>(Id.PointsPanel);
            
            _kinematics = Q<DropdownField>(Id.Kinematics);
            _methodForward = Q<DropdownField>(Id.MethodForward);
            _methodReverse = Q<DropdownField>(Id.MethodReverse);

            _chainOr3D = Q<Button>(Id.ChainOr3D);

            _labelX = Q<Label>(Id.LX);
            _labelY=Q<Label>(Id.LY);
            _labelQ1 = Q<Label>(Id.LQ1);
            _labelQ2=Q<Label>(Id.LQ2);
            
            _labelJ1 = Q<Label>(Id.LJ1);
            _labelJ2 = Q<Label>(Id.LJ2);
            
            _bpQ1 = Q<Button>(Id.BpQ1);
            _bmQ1 = Q<Button>(Id.BmQ1);
            _bpQ2 = Q<Button>(Id.BpQ2);
            _bmQ2=Q<Button>(Id.BmQ2);
            
            _results = Q<Button>(Id.Results);
            _run = Q<Button>(Id.Run);
            _stop = Q<Button>(Id.Stop);
            _emergency = Q<Button>(Id.Emergency);
            _teach = Q<Button>(Id.Teach);
            _edit = Q<Button>(Id.Edit);
            
            _moveContinuous= Q<RadioButton>(Id.MoveCont);
            _moveLong = Q<RadioButton>(Id.MoveLong);
            _moveHalf = Q<RadioButton>(Id.MoveMed);
            _moveShort = Q<RadioButton>(Id.MoveShort);
            
            _equations=Q<VisualElement>(Id.Equations);
            
            _bpX = Q<Button>(Id.BpX);
            _bmX = Q<Button>(Id.BmX);
            _bpY = Q<Button>(Id.BpY);
            _bmY = Q<Button>(Id.BmY);
            
            
            _bpJ1=Q<Button>(Id.BpJ1);
            _bmJ1=Q<Button>(Id.BmJ1);
            _bpJ2=Q<Button>(Id.BpJ2);
            _bmJ2=Q<Button>(Id.BmJ2);




            Q<Button>(Id.Results).RegisterCallback<ClickEvent>(_ =>
            {
                if (_equations.ClassListContains(Uss.Hide) || !_equations.ClassListContains(Uss.Show))
                {
                    SetVisible(_equations, true);
                }
                else
                {
                    SetVisible(_equations, false);
                }
                
            } );

            Q<Button>(Id.ShowMenu)?.RegisterCallback<ClickEvent>(_ =>
            {
                SetValue(_environmentMenu, "environment");
                SetValue(_arScaraUiMenu, "ARSCARA menu");
                SetValue(_placementMenu, "Placement");

                ShowOnlyArScaraPanel(ArScaraUiMenu.None);

                _warning.style.display = DisplayStyle.Flex;
                _warning.text = "Select a work environment";

                ShowPanelSliding(_navPanel);
            });

            Q<Button>(Id.HideMenu)?.RegisterCallback<ClickEvent>(_ => HidePanelSliding(_navPanel));

            _navPanel?.RegisterCallback<TransitionEndEvent>(_ =>
            {
                if (_navPanel.ClassListContains(Uss.MainMenuBase + Uss.PanelOut))
                    _slidingPanels.style.display = DisplayStyle.None;
            });

            _environmentMenu?.RegisterValueChangedCallback(e => OnEnvironmentChanged(ParseEnv(e.newValue)));
            _arScaraUiMenu?.RegisterValueChangedCallback(e => OnArScaraUiMenuChanged(ParseArScaraUiMenu(e.newValue)));
            _mode?.RegisterValueChangedCallback(e => OnModeUiMenuChanged(ParseMode(e.newValue)));
            _kinematics?.RegisterValueChangedCallback(e => OnKinematicsMenuChanged(ParseKinematics(e.newValue)));
            // // _placementMenu?.RegisterValueChangedCallback(e => OnPlacementMenuChanged(ParsePlacement(e.newValue)));
            // _mode?.RegisterValueChangedCallback(e => ApplyModeUi(ParseMode(e.newValue)));
            
            // _methodForward?.RegisterValueChangedCallback(e => ApplyMethodForward(ParseMethodForward(e.newValue)));
            //
            // _methodReverse?.RegisterValueChangedCallback(e => ApplyMethodReverse(ParseMethodReverse(e.newValue)));
            
            BuildGroups();
        }

        private void Start()
        {
            HideUI();
            _slidingPanels.style.display = DisplayStyle.None;
            SetValue(_environmentMenu, "environment");
            SetValue(_arScaraUiMenu, "ARSCARA menu");
            SetValue(_placementMenu, "Placement");
            SetValue(_views, "Select view");
            SetValue(_mode, "Mode");
            SetValue(_speed, "Speed");
            SetValue(_destination, "Destination");
            SetValue(_points, "Point");
            SetValue(_kinematics, "Kinematics");
            SetValue(_methodForward, "Method");
            SetValue(_methodReverse, "Method");
            _warning.text = "Select a work environment";
            ShowOnlyArScaraPanel(ArScaraUiMenu.None);
            SetBodyOpaque(true);
            ApplyModeUi(ModeType.None);
        }
        #region UI Query & Manipulation

        private T Q<T>(string name) where T : VisualElement => _root.Q<T>(name);

        private static void SetValue(DropdownField df, string v) { if (df != null) df.value = v; }

        private static void SetVisible(VisualElement ve, bool on)
        {
            if (ve == null) return;
            if (on) { ve.RemoveFromClassList(Uss.Hide); ve.AddToClassList(Uss.Show); }
            else    { ve.RemoveFromClassList(Uss.Show); ve.AddToClassList(Uss.Hide); }
        }

        private static void SetVisible(IEnumerable<VisualElement> list, bool on)
        {
            foreach (var ve in list) SetVisible(ve, on);
        }

        private void SetBodyOpaque(bool opaque)
        {
            if (_body == null) return;
            _body.RemoveFromClassList(opaque ? Uss.BgTransparent : Uss.BgOpaque);
            _body.AddToClassList(opaque ? Uss.BgOpaque : Uss.BgTransparent);
        }

        #endregion

        #region Panel Animations

        private void ShowPanelSliding(VisualElement panel)
        {
            if (panel == null) return;
            _slidingPanels.style.display = DisplayStyle.Flex;
            _scrim?.RemoveFromClassList(Uss.ScrimTransparent);
            _scrim?.AddToClassList(Uss.ScrimOpaque);
            panel.RemoveFromClassList(Uss.MainMenuBase + Uss.PanelOut);
            panel.AddToClassList(Uss.MainMenuBase + Uss.PanelIn);
        }

        private void HidePanelSliding(VisualElement panel)
        {
            if (panel == null) return;
            panel.RemoveFromClassList(Uss.MainMenuBase + Uss.PanelIn);
            panel.AddToClassList(Uss.MainMenuBase + Uss.PanelOut);
            _slidingPanels.style.display = DisplayStyle.Flex; 
            _scrim?.RemoveFromClassList(Uss.ScrimOpaque);
            _scrim?.AddToClassList(Uss.ScrimTransparent);
        }

        private void ShowOnlyArScaraPanel(ArScaraUiMenu which)
        {
            SetArScaraPanel(_controlPanel, which == ArScaraUiMenu.Control);
            SetArScaraPanel(_jogTeachPanel, which == ArScaraUiMenu.JogTeach);
            SetArScaraPanel(_pointsPanel, which == ArScaraUiMenu.Points);
        }

        private static void SetArScaraPanel(VisualElement panel, bool visible)
        {
            if (panel == null) return;
            var add = visible ? "arScaraPanelsIn" : "arScaraPanelsOut";
            var rem = visible ? "arScaraPanelsOut" : "arScaraPanelsIn";
            panel.RemoveFromClassList(rem);
            panel.AddToClassList(add);
        }

        #endregion

        #region UI Groups Setup

        private void BuildGroups()
        {
            Add(_virtualControlsGroup, _views,_arScaraUiMenu);
            Add(_arControlsGroup,_arScaraUiMenu, _placementMenu);
            Add(_worldControlsGroup,_speed,_chainOr3D,_kinematics,_labelX,_labelY,_labelQ1,_labelQ2);
            Add(_jointControlsGroup,_speed,_chainOr3D,_run,_stop,_emergency,_teach,_edit,_moveContinuous,
                _moveLong,_moveHalf,_moveShort,_destination,_labelJ1,_labelJ2,_bpJ1,_bmJ1,_bpJ2,_bmJ2);
            Add(_methodForwardControlsGroup,_methodForward,_bpQ1,_bmQ1,_bpQ2,_bmQ2,_results,_run,_stop,_emergency,_teach
                ,_edit,_moveContinuous,_moveLong,_moveHalf,_moveShort,_destination);
            Add(_kinematicsReverseGroup,_methodReverse,_bpX,_bmX,_bpY,_bmY,_results,_run,_stop,_emergency,_teach
                ,_edit,_moveContinuous,_moveLong,_moveHalf,_moveShort,_destination);
        }

        private static void Add(List<VisualElement> list, params VisualElement[] items)
        {
            foreach (var it in items) if (it != null) list.Add(it);
        }

        #endregion
        

        private void OnEnvironmentChanged(ArScaraEnvironmentController.EnvType env)
        {
            switch (env)
            {
                case ArScaraEnvironmentController.EnvType.Virtual:
                case ArScaraEnvironmentController.EnvType.TwinNexus:
                    SetVisible(_arControlsGroup, false);
                    SetVisible(_virtualControlsGroup, true);
                    SetBodyOpaque(false);
                    _environmentController.CreateEnvironment(env);
                    _warning.text = "Select ARSCARA robot interface";
                    break;
                case ArScaraEnvironmentController.EnvType.Augmented:
                    SetVisible(_virtualControlsGroup, false);
                    SetVisible(_arControlsGroup, true);
                    SetBodyOpaque(false);
                    _environmentController.CreateEnvironment(env);
                    break;
                default:
                    SetBodyOpaque(true);
                    SetVisible(_arControlsGroup, false);
                    SetVisible(_virtualControlsGroup, false);
                    SetValue(_arScaraUiMenu, "ARSCARA menu");
                    _warning.text = "Select a work environment";
                    break;
            }
        }
        
        private void OnArScaraUiMenuChanged(ArScaraUiMenu uiMenu)
        {
            if (uiMenu == ArScaraUiMenu.None)
            {
                _warning.style.display = DisplayStyle.Flex;
                _warning.text = _environmentMenu?.value == "environment"
                    ? "Select a work environment"
                    : "Select ARSCARA robot interface";
                ShowOnlyArScaraPanel(ArScaraUiMenu.None);
                return;
            }

            _warning.style.display = DisplayStyle.None;
            ShowOnlyArScaraPanel(uiMenu);
        }

        private void OnModeUiMenuChanged(ModeType mode)
        {
            switch (mode)
            {
                case ModeType.World:
                    SetVisible(_methodForwardControlsGroup, false);
                    SetVisible(_kinematicsReverseGroup, false);
                    SetVisible(_jointControlsGroup, false);
                    SetVisible(_worldControlsGroup, true);
                    break;
                case ModeType.Joint:
                    SetVisible(_methodForwardControlsGroup, false);
                    SetVisible(_kinematicsReverseGroup, false);
                    SetVisible(_worldControlsGroup, false);
                    SetVisible(_jointControlsGroup, true);
                    break;
                case ModeType.None:
                    SetVisible(_worldControlsGroup, false);
                    break;
                default:
                    SetVisible(_worldControlsGroup, false);
                    break;
            }
            
        }
        private void OnKinematicsMenuChanged(KinematicsType kinematics)
        {
            switch (kinematics)
            {
                case KinematicsType.Forward:
                    SetVisible(_kinematicsReverseGroup, false);
                    SetVisible(_methodForwardControlsGroup, true);
                    break;
                case KinematicsType.Reverse:
                    SetVisible(_methodForwardControlsGroup, false);
                    SetVisible(_kinematicsReverseGroup, true);
                    break;
                default:
                    SetVisible(_methodForwardControlsGroup, false);
                    SetVisible(_kinematicsReverseGroup, false);
                    _methodForward.value = "Method";
                    break;
                
            }
        }
        
        
        #region Mode UI

        // private void ApplyKinematics(KinematicsType kinematics)
        // {
        //     switch (kinematics)
        //     {
        //         case KinematicsType.Forward:
        //             SetVisible(_kinematicsReverseGroup,false);
        //             SetVisible(_methodReverseControlsGroup, false);
        //             SetVisible(_kinematicsForwardGroup,true);
        //             break;
        //         case KinematicsType.Reverse:
        //             SetVisible(_kinematicsForwardGroup,false);
        //             SetVisible(_methodForwardControlsGroup, false);
        //             SetVisible(_kinematicsReverseGroup,true);
        //             break;
        //         default:
        //             SetVisible(_kinematicsForwardGroup, false);
        //             SetVisible(_kinematicsReverseGroup,false);
        //             SetVisible(_methodForwardControlsGroup, false);
        //             SetVisible(_methodReverseControlsGroup, false);
        //             _methodForward.value = "Method";
        //             break;
        //         
        //     }
        // }

        private void ApplyMethodForward(MethodForwardType methodForward)
        {
            SetVisible(_methodForwardControlsGroup, methodForward != MethodForwardType.None);
            if (methodForward == MethodForwardType.None)
            {
                SetVisible(_methodForwardControlsGroup, false);
            }
        }
        
        

        private void ApplyModeUi(ModeType mode)
        {
            bool common = mode is ModeType.World or ModeType.Joint;
            SetVisible(_speed, common);
            SetVisible(_worldControlsGroup, mode == ModeType.World);
            SetVisible(_jointControlsGroup, mode == ModeType.Joint);
            _kinematics.value = "Kinematics";
            _methodForward.value="Method";
            _methodReverse.value="Method";
            
            if (mode == ModeType.None)
            {
                SetVisible(_worldControlsGroup, false);
                SetVisible(_jointControlsGroup, false);
            }
        }
        #endregion

        #region Event Handlers
        

        // private void OnPlacementMenuChanged(Placement placement)
        // {
        //     Debug.Log($"Placement changed to: {placement}");
        //     // TODO: Implementar lógica de placement (Raycast/QR Marker)
        // }

        #endregion

        #region Parsers

        private static ArScaraEnvironmentController.EnvType ParseEnv(string raw)
        {
            var s = (raw ?? "").Trim();
            return s switch
            {
                "Virtual"    => ArScaraEnvironmentController.EnvType.Virtual,
                "Augmented"  => ArScaraEnvironmentController.EnvType.Augmented,
                "Twin Nexus" => ArScaraEnvironmentController.EnvType.TwinNexus,
                _            => ArScaraEnvironmentController.EnvType.None
            };
        }

        private static ModeType ParseMode(string raw)
        {
            var s = (raw ?? "").Trim();
            return s switch
            {
                "World" => ModeType.World,
                "Joint" => ModeType.Joint,
                _       => ModeType.None
            };
        }
        
        private static KinematicsType ParseKinematics(string raw)
        {
            var s = (raw ?? "").Trim();
            return s switch
            {
                "Forward" => KinematicsType.Forward,
                "Reverse" => KinematicsType.Reverse,
                _       => KinematicsType.None
            };
        }
        
        

        private static MethodForwardType ParseMethodForward(string raw)
        {
            var s = (raw ?? "").Trim();
            return s switch
            {
                "Geometric" => MethodForwardType.Geometric,
                "HTM" => MethodForwardType.HTM,
                "Denavit-Hartenberg" => MethodForwardType.DenavitHartenberg,
                _       => MethodForwardType.None
            };
        }
        

        private static MethodReverseType ParseMethodReverse(string raw)
        {
            var s = (raw ?? "").Trim();
            return s switch
            {
                "a" => MethodReverseType.a,
                "b" => MethodReverseType.b,
                "c" => MethodReverseType.c,
                _       => MethodReverseType.None
            };
        }
        

        private static ArScaraUiMenu ParseArScaraUiMenu(string raw)
        {
            var s = (raw ?? "").Trim();
            return s switch
            {
                "Control panel" => ArScaraUiMenu.Control,
                "Jog and teach" => ArScaraUiMenu.JogTeach,
                "Points"        => ArScaraUiMenu.Points,
                _               => ArScaraUiMenu.None
            };
        }

        private static Placement ParsePlacement(string raw)
        {
            var s = (raw ?? "").Trim();
            return s switch
            {
                "Raycast"   => Placement.Raycast,
                "QR marker" => Placement.QRMarker,
                _           => Placement.None
            };
        }

        #endregion

        #region Public API

        internal void ShowUI()
        {
            Q<VisualElement>(Id.Body).style.display = DisplayStyle.Flex;
        }

        internal void HideUI()
        {
            Q<VisualElement>(Id.Body).style.display = DisplayStyle.None;
        }

        #endregion
    }
}