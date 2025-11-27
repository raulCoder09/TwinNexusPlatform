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
        private enum ArScaraPanel { None, Control, JogTeach, Points }
        // private enum Placement { None, Raycast, QRMarker }

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
            // public const string Placement = "placementMenu";
            public const string Views = "views";
            public const string Mode = "modeDropdown";
            public const string Speed = "speedDropdown";
            public const string Destination = "DestinationDropdown";
            public const string Points = "pointsDropdown";

            public const string ControlPanel = "controlPanel";
            public const string JogTeachPanel = "jogAndTeachPanel";
            public const string PointsPanel = "pointsPanel";

            public const string BpX = "plusXButton";    public const string BmX = "minusXButton";
            public const string BpY = "plusYButton";    public const string BmY = "minusYButton";

            public const string BpJ1 = "plusJ1Button";  public const string BmJ1 = "minusJ1Button";
            public const string BpJ2 = "plusJ2Button";  public const string BmJ2 = "minusJ2Button";

            public const string LX = "xLabel"; public const string LY = "yLabel";

            public const string LJ1 = "j1Label"; public const string LJ2 = "j2Label";

            public const string Teach = "TeachButton";
            public const string Edit = "EditButton";
            public const string Run = "RunButton";
            public const string Stop = "StopButton";
            public const string Emergency = "EmergencyButton";
            public const string Medara = "MEDARAButton";
            public const string Glass = "GlassButton";
            public const string Results = "ResultsButton";

            public const string MoveCont = "continuousMove";
            public const string MoveLong = "longMove";
            public const string MoveMed  = "mediumMove";
            public const string MoveShort= "shortMove";
            
            public const string Kinematics= "kinematicsDropdown";
            public const string Method= "methodDropdown";
            
            
        }

        private UIDocument _doc;
        private VisualElement _root;
        private VisualElement _body, _slidingPanels, _scrim, _navPanel;
        private Label _warning;
        private DropdownField _environmentMenu, _arScaraMenu, _views, _mode, _speed, _destination, _points; //,_placementMenu
        private VisualElement _controlPanel, _jogTeachPanel, _pointsPanel;

        private readonly List<VisualElement> _worldButtons = new();
        private readonly List<VisualElement> _jointButtons = new();
        private readonly List<VisualElement> _worldLabels  = new();
        private readonly List<VisualElement> _jointLabels  = new();
        private readonly List<VisualElement> _moveRadios   = new();
        private readonly List<VisualElement> _teachEdit    = new();
        private readonly List<VisualElement> _runStop    = new();
        private readonly List<VisualElement> _commanding   = new();
        private readonly List<VisualElement> _medara   = new();

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
            _arScaraMenu = Q<DropdownField>(Id.ArScaraMenu);
            // _placementMenu = Q<DropdownField>(Id.Placement);

            _views = Q<DropdownField>(Id.Views);
            _mode = Q<DropdownField>(Id.Mode);
            _speed = Q<DropdownField>(Id.Speed);
            _destination = Q<DropdownField>(Id.Destination);
            _points = Q<DropdownField>(Id.Points);

            _controlPanel = Q<VisualElement>(Id.ControlPanel);
            _jogTeachPanel = Q<VisualElement>(Id.JogTeachPanel);
            _pointsPanel = Q<VisualElement>(Id.PointsPanel);

            Q<Button>(Id.ShowMenu)?.RegisterCallback<ClickEvent>(_ =>
            {
                SetValue(_environmentMenu, "environment");
                SetValue(_arScaraMenu, "ARSCARA menu");
                // SetValue(_placementMenu, "Placement");

                ShowOnlyArScaraPanel(ArScaraPanel.None);

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
            _arScaraMenu?.RegisterValueChangedCallback(e => OnArScaraMenuChanged(ParseArScaraPanel(e.newValue)));
            // _placementMenu?.RegisterValueChangedCallback(e => OnPlacementMenuChanged(ParsePlacement(e.newValue)));
            _mode?.RegisterValueChangedCallback(e => ApplyModeUi(ParseMode(e.newValue)));
            _speed?.RegisterValueChangedCallback(_ => { /* hook futuro */ });

            BuildGroups();
        }

        private void Start()
        {
            HideUI();
            _slidingPanels.style.display = DisplayStyle.None;

            SetValue(_environmentMenu, "environment");
            SetValue(_arScaraMenu, "ARSCARA menu");
            // SetValue(_placementMenu, "Placement");
            SetValue(_views, "Select view");
            SetValue(_mode, "Mode");
            SetValue(_speed, "Speed");
            SetValue(_destination, "Destination");
            SetValue(_points, "Point");


            _warning.text = "Select a work environment";
            _arScaraMenu?.SetEnabled(false);
            // _placementMenu?.SetEnabled(false);

            ShowOnlyArScaraPanel(ArScaraPanel.None);
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

        private void ShowOnlyArScaraPanel(ArScaraPanel which)
        {
            SetArScaraPanel(_controlPanel, which == ArScaraPanel.Control);
            SetArScaraPanel(_jogTeachPanel, which == ArScaraPanel.JogTeach);
            SetArScaraPanel(_pointsPanel, which == ArScaraPanel.Points);
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
            
            Add(_worldButtons, Q<Button>(Id.BpX), Q<Button>(Id.BmX), Q<Button>(Id.BpY), Q<Button>(Id.BmY));

            Add(_jointButtons, Q<Button>(Id.BpJ1), Q<Button>(Id.BmJ1), Q<Button>(Id.BpJ2), Q<Button>(Id.BmJ2));

            Add(_worldLabels, Q<Label>(Id.LX), Q<Label>(Id.LY),Q<DropdownField>(Id.Kinematics),Q<DropdownField>(Id.Method),Q<Button>(Id.Results));
            Add(_jointLabels, Q<Label>(Id.LJ1), Q<Label>(Id.LJ2));
        
            Add(_moveRadios, Q<RadioButton>(Id.MoveCont), Q<RadioButton>(Id.MoveLong),
                Q<RadioButton>(Id.MoveMed),  Q<RadioButton>(Id.MoveShort));
        
            Add(_teachEdit, Q<Button>(Id.Teach), Q<Button>(Id.Edit));
            Add(_runStop, Q<Button>(Id.Run), Q<Button>(Id.Stop),Q<Button>(Id.Emergency));
            Add(_medara,Q<Button>(Id.Medara),Q<Button>(Id.Glass));
            
            Add(_commanding, _destination);
        }

        private static void Add(List<VisualElement> list, params VisualElement[] items)
        {
            foreach (var it in items) if (it != null) list.Add(it);
        }

        #endregion

        #region Mode UI

        private void ApplyModeUi(ModeType mode)
        {
            bool common = mode is ModeType.World or ModeType.Joint;
            SetVisible(_speed, common);
            SetVisible(_moveRadios, common);
            SetVisible(_teachEdit, common);
            SetVisible(_commanding, common);
            SetVisible(_runStop, common);
            SetVisible(_medara, common);

            SetVisible(_worldButtons, mode == ModeType.World);
            SetVisible(_worldLabels,  mode == ModeType.World);

            SetVisible(_jointButtons, mode == ModeType.Joint);
            SetVisible(_jointLabels,  mode == ModeType.Joint);

            if (mode == ModeType.None)
            {
                SetVisible(_worldButtons, false);
                SetVisible(_jointButtons, false);
                SetVisible(_worldLabels,  false);
                SetVisible(_jointLabels,  false);
            }
        }

        #endregion

        #region Event Handlers

        private void OnEnvironmentChanged(ArScaraEnvironmentController.EnvType env)
        {
            bool none = env == ArScaraEnvironmentController.EnvType.None;
            
            // Actualizar UI según selección
            _arScaraMenu?.SetEnabled(!none);
            // _placementMenu?.SetEnabled(!none && (env == ArScaraEnvironmentController.EnvType.Augmented));
            _warning.text = none ? "Select a work environment" : "Select ARSCARA robot interface";
            SetBodyOpaque(none);
            
            if (none)
            {
                SetValue(_arScaraMenu, "ARSCARA menu");
            }

            // Verificar disponibilidad de XR para AR
            if (env == ArScaraEnvironmentController.EnvType.Augmented && !_environmentController.IsXRAvailable())
            {
                _warning.text = "AR not available: enable a provider in Project Settings > XR Plug-in Management.";
            }
            
            if (_environmentController != null)
            {
                _environmentController.CreateEnvironment(env);
            }
            else
            {
                Debug.LogError("Environment Controller not assigned!");
            }
        }

        private void OnArScaraMenuChanged(ArScaraPanel panel)
        {
            if (panel == ArScaraPanel.None)
            {
                _warning.style.display = DisplayStyle.Flex;
                _warning.text = _environmentMenu?.value == "environment"
                    ? "Select a work environment"
                    : "Select ARSCARA robot interface";
                ShowOnlyArScaraPanel(ArScaraPanel.None);
                return;
            }

            _warning.style.display = DisplayStyle.None;
            ShowOnlyArScaraPanel(panel);
        }

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

        private static ArScaraPanel ParseArScaraPanel(string raw)
        {
            var s = (raw ?? "").Trim();
            return s switch
            {
                "Control panel" => ArScaraPanel.Control,
                "Jog and teach" => ArScaraPanel.JogTeach,
                "Points"        => ArScaraPanel.Points,
                _               => ArScaraPanel.None
            };
        }

        // private static Placement ParsePlacement(string raw)
        // {
        //     var s = (raw ?? "").Trim();
        //     return s switch
        //     {
        //         "Raycast"   => Placement.Raycast,
        //         "QR marker" => Placement.QRMarker,
        //         _           => Placement.None
        //     };
        // }

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