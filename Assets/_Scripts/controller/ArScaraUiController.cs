using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ArScaraUiController : MonoBehaviour
{
    [Header("Environment Prefabs")]
    [SerializeField] private GameObject virtualEnvironment;
    [SerializeField] private GameObject augmentedEnvironment;
    [SerializeField] private GameObject hybridEnvironment;
    [SerializeField] private GameObject realEnvironment;

    private enum EnvType { None, Virtual, Augmented, Hybrid, Real }
    private enum ModeType { None, World, Joint }
    private enum ArPanel { None, Control, JogTeach, Points }

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
        public const string ArMenu = "arscaraMenu";
        public const string Views = "views";
        public const string Mode = "modeDropdown";
        public const string Speed = "speedDropdown";
        public const string Command = "CommandDropdown";
        public const string Destination = "DestinationDropdown";
        public const string Points = "pointsDropdown";

        public const string ControlPanel = "controlPanel";
        public const string JogTeachPanel = "jogAndTeachPanel";
        public const string PointsPanel = "pointsPanel";

        // Botones cartesianos
        public const string BpX = "plusXButton";    public const string BmX = "minusXButton";
        public const string BpY = "plusYButton";    public const string BmY = "minusYButton";
        public const string BpZ = "plusZButton";    public const string BmZ = "minusZButton";
        public const string BpU = "plusUButton";    public const string BmU = "minusUButton";

        // Botones juntas
        public const string BpJ1 = "plusJ1Button";  public const string BmJ1 = "minusJ1Button";
        public const string BpJ2 = "plusJ2Button";  public const string BmJ2 = "minusJ2Button";
        public const string BpJ3 = "plusJ3Button";  public const string BmJ3 = "minusJ3Button";
        public const string BpJ4 = "plusJ4Button";  public const string BmJ4 = "minusJ4Button";

        // Labels cartesianos
        public const string LX = "xLabel"; public const string LY = "yLabel";
        public const string LZ = "zLabel"; public const string LU = "uLabel";
        // Labels juntas
        public const string LJ1 = "j1Label"; public const string LJ2 = "j2Label";
        public const string LJ3 = "j3Label"; public const string LJ4 = "j4Label";

        public const string Teach = "TeachButton";
        public const string Edit = "EditButton";

        public const string MoveCont = "continuousMove";
        public const string MoveLong = "longMove";
        public const string MoveMed  = "mediumMove";
        public const string MoveShort= "shortMove";
    }

    // UI cache
    private UIDocument _doc;
    private VisualElement _root;
    private VisualElement _body, _slidingPanels, _scrim, _navPanel;
    private Label _warning;
    private DropdownField _envMenu, _arMenu, _views, _mode, _speed, _command, _destination, _points;
    private VisualElement _controlPanel, _jogTeachPanel, _pointsPanel;

    // Groups
    private readonly List<VisualElement> _worldButtons = new();
    private readonly List<VisualElement> _jointButtons = new();
    private readonly List<VisualElement> _worldLabels  = new();
    private readonly List<VisualElement> _jointLabels  = new();
    private readonly List<VisualElement> _moveRadios   = new();
    private readonly List<VisualElement> _teachEdit    = new();
    private readonly List<VisualElement> _commanding   = new();

    // Environments
    private readonly Dictionary<EnvType, GameObject> _envPrefabs = new();
    private GameObject _currentEnv;

    private void Awake()
    {
        _doc = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;

        // Prefabs
        _envPrefabs[EnvType.Virtual] = virtualEnvironment;
        _envPrefabs[EnvType.Augmented] = augmentedEnvironment;
        _envPrefabs[EnvType.Hybrid] = hybridEnvironment;
        _envPrefabs[EnvType.Real] = realEnvironment;

        // Cache nodes
        _body = Q<VisualElement>(Id.Body);
        _slidingPanels = Q<VisualElement>(Id.SlidingPanels);
        _scrim = Q<VisualElement>(Id.Scrim);
        _navPanel = Q<VisualElement>(Id.NavPanel);

        _warning = Q<Label>(Id.Warning);

        _envMenu = Q<DropdownField>(Id.EnvMenu);
        _arMenu = Q<DropdownField>(Id.ArMenu);
        _views = Q<DropdownField>(Id.Views);
        _mode = Q<DropdownField>(Id.Mode);
        _speed = Q<DropdownField>(Id.Speed);
        _command = Q<DropdownField>(Id.Command);
        _destination = Q<DropdownField>(Id.Destination);
        _points = Q<DropdownField>(Id.Points);

        _controlPanel = Q<VisualElement>(Id.ControlPanel);
        _jogTeachPanel = Q<VisualElement>(Id.JogTeachPanel);
        _pointsPanel = Q<VisualElement>(Id.PointsPanel);

        // Events
        Q<Button>(Id.ShowMenu)?.RegisterCallback<ClickEvent>(_ => ShowPanelSliding(_navPanel));
        Q<Button>(Id.HideMenu)?.RegisterCallback<ClickEvent>(_ => HidePanelSliding(_navPanel));

        _navPanel?.RegisterCallback<TransitionEndEvent>(_ =>
        {
            if (_navPanel.ClassListContains(Uss.MainMenuBase + Uss.PanelOut))
                _slidingPanels.style.display = DisplayStyle.None;
        });

        _envMenu?.RegisterValueChangedCallback(e => OnEnvironmentChanged(ParseEnv(e.newValue)));
        _arMenu?.RegisterValueChangedCallback(e => OnArMenuChanged(ParseArPanel(e.newValue)));
        _mode?.RegisterValueChangedCallback(e => ApplyModeUi(ParseMode(e.newValue)));
        _speed?.RegisterValueChangedCallback(_ => { /* hook futuro */ });

        BuildGroups();
    }

    private void Start()
    {
        // Estado inicial seguro
        _slidingPanels.style.display = DisplayStyle.None;

        SetValue(_envMenu, "environment");
        SetValue(_arMenu, "ARSCARA menu");
        SetValue(_views, "Select view");
        SetValue(_mode, "Mode");
        SetValue(_speed, "Speed");
        SetValue(_command, "Command");
        SetValue(_destination, "Destination");
        SetValue(_points, "Point"); // en tu UXML es "Point" (no "Points")

        _warning.text = "Select a work environment";
        _arMenu?.SetEnabled(false);

        ShowOnlyArPanel(ArPanel.None);
        SetBodyOpaque(true);
        ApplyModeUi(ModeType.None);
    }

    // ---------- UI helpers ----------
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
        _body.RemoveFromClassList(opaque ? "backgroundTransparent" : "backgroundOpaque");
        _body.AddToClassList(opaque ? "backgroundOpaque" : "backgroundTransparent");
    }

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
        _slidingPanels.style.display = DisplayStyle.Flex; // se oculta en TransitionEnd
        _scrim?.RemoveFromClassList(Uss.ScrimOpaque);
        _scrim?.AddToClassList(Uss.ScrimTransparent);
    }

    private void ShowOnlyArPanel(ArPanel which)
    {
        SetArPanel(_controlPanel, which == ArPanel.Control);
        SetArPanel(_jogTeachPanel, which == ArPanel.JogTeach);
        SetArPanel(_pointsPanel, which == ArPanel.Points);
    }

    private static void SetArPanel(VisualElement panel, bool visible)
    {
        if (panel == null) return;
        var add = visible ? "arScaraPanelsIn" : "arScaraPanelsOut";
        var rem = visible ? "arScaraPanelsOut" : "arScaraPanelsIn";
        panel.RemoveFromClassList(rem);
        panel.AddToClassList(add);
    }

    // ---------- Grouping ----------
    private void BuildGroups()
    {
        // World buttons
        Add(_worldButtons, Q<Button>(Id.BpX), Q<Button>(Id.BmX), Q<Button>(Id.BpY), Q<Button>(Id.BmY),
                           Q<Button>(Id.BpZ), Q<Button>(Id.BmZ), Q<Button>(Id.BpU), Q<Button>(Id.BmU));
        // Joint buttons
        Add(_jointButtons, Q<Button>(Id.BpJ1), Q<Button>(Id.BmJ1), Q<Button>(Id.BpJ2), Q<Button>(Id.BmJ2),
                           Q<Button>(Id.BpJ3), Q<Button>(Id.BmJ3), Q<Button>(Id.BpJ4), Q<Button>(Id.BmJ4));
        // Labels
        Add(_worldLabels, Q<Label>(Id.LX), Q<Label>(Id.LY), Q<Label>(Id.LZ), Q<Label>(Id.LU));
        Add(_jointLabels, Q<Label>(Id.LJ1), Q<Label>(Id.LJ2), Q<Label>(Id.LJ3), Q<Label>(Id.LJ4));
        // Radios
        Add(_moveRadios, Q<RadioButton>(Id.MoveCont), Q<RadioButton>(Id.MoveLong),
                         Q<RadioButton>(Id.MoveMed),  Q<RadioButton>(Id.MoveShort));
        // Teach/Edit
        Add(_teachEdit, Q<Button>(Id.Teach), Q<Button>(Id.Edit));
        // Commanding
        Add(_commanding, _command, _destination);
    }

    private static void Add(List<VisualElement> list, params VisualElement[] items)
    {
        foreach (var it in items) if (it != null) list.Add(it);
    }

    // ---------- State application ----------
    private void ApplyModeUi(ModeType mode)
    {
        bool common = mode is ModeType.World or ModeType.Joint;
        SetVisible(_speed, common);
        SetVisible(_moveRadios, common);
        SetVisible(_teachEdit, common);
        SetVisible(_commanding, common);

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

    private void OnEnvironmentChanged(EnvType env)
    {
        bool none = env == EnvType.None;
        _arMenu?.SetEnabled(!none);
        _warning.text = none ? "Select a work environment" : "Select ARSCARA robot interface";
        SetBodyOpaque(none);
        SpawnEnvironment(env);
    }

    private void OnArMenuChanged(ArPanel panel)
    {
        if (panel == ArPanel.None)
        {
            _warning.style.display = DisplayStyle.Flex;
            _warning.text = _envMenu?.value == "environment"
                ? "Select a work environment"
                : "Select ARSCARA robot interface";
            ShowOnlyArPanel(ArPanel.None);
            return;
        }

        _warning.style.display = DisplayStyle.None;
        ShowOnlyArPanel(panel);
    }

    // ---------- Environments ----------
    private void SpawnEnvironment(EnvType env)
    {
        if (_currentEnv != null) { Destroy(_currentEnv); _currentEnv = null; }
        if (env == EnvType.None) return;

        if (_envPrefabs.TryGetValue(env, out var prefab) && prefab != null)
            _currentEnv = Instantiate(prefab);
        else
            Debug.LogWarning($"Prefab not set for environment {env}");
    }

    // ---------- Parsers (robustos) ----------
    private static EnvType ParseEnv(string raw)
    {
        var s = (raw ?? "").Trim(); // protege " Real"
        return s switch
        {
            "Virtual"   => EnvType.Virtual,
            "Augmented" => EnvType.Augmented,
            "Hybrid"    => EnvType.Hybrid,
            "Real"      => EnvType.Real,
            _           => EnvType.None
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

    private static ArPanel ParseArPanel(string raw)
    {
        var s = (raw ?? "").Trim();
        return s switch
        {
            "Control panel" => ArPanel.Control,
            "Jog and teach" => ArPanel.JogTeach,
            "Points"        => ArPanel.Points,
            _               => ArPanel.None
        };
    }
}
