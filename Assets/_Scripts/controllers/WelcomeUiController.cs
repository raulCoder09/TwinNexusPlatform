using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class WelcomeUiController : MonoBehaviour
{
    // =======================
    // Constantes
    // =======================
    private static class Id
    {
        public const string SlidingPanels = "slidingPanels";
        public const string Scrim = "scrim";

        public const string Launch = "launchButton";
        public const string Exit = "exitButton";

        // Login panel
        public const string LoginPanel = "loginPanel";
        public const string CancelLogin = "cancelLoginButton";
        public const string RegisterLogin = "registerLoginButton";
        public const string RecoverLogin = "recoverLoginButton";
        public const string Login = "loginButton";

        // Register panel
        public const string RegisterPanel = "registerPanel";
        public const string CancelRegister = "cancelRegisterButton";
        public const string BackRegister = "backRegisterButton";
        public const string Register = "registerButton";

        // Recover panel
        public const string RecoverPanel = "recoverPasswordPanel";
        public const string CancelRecover = "cancelRecoverButton";
        public const string BackRecover = "backRecoverButton";
        public const string Recover = "recoverPasswordButton";

        // Email verification panel
        public const string VerifyPanel = "emailVerificationPanel";
        public const string CancelVerify = "cancelVerifyEmailButton";
        public const string BackVerify = "backVerifyEmailButton";
        public const string ResendCode = "resendVerificationCodeButton";
        public const string Verify = "verifyEmailButton";
    }

    private static class Uss
    {
        public const string In = "subpanelPanelIn";
        public const string Out = "subpanelPanelOut";
        public const string ScrimOpaque = "scrimOpaque";
        public const string ScrimTransparent = "scrimTransparent";
    }

    // =======================
    // Estado/UI cache
    // =======================
    private UIDocument _doc;
    private VisualElement _root;
    private VisualElement _overlay; // slidingPanels
    private VisualElement _scrim;

    // Diccionario con los subpaneles
    private readonly Dictionary<string, VisualElement> _panels = new();

    // Lleva la cuenta de cuántos paneles están “mostrándose” (o animando a In)
    private int _openPanels = 0;

    // =======================
    // Unity
    // =======================
    private void Awake()
    {
        _doc = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;

        // Cache overlay/scrim
        _overlay = Q<VisualElement>(Id.SlidingPanels);
        _scrim = Q<VisualElement>(Id.Scrim);

        // Cache subpaneles existentes
        CachePanel(Id.LoginPanel);
        CachePanel(Id.RegisterPanel);
        CachePanel(Id.RecoverPanel);
        CachePanel(Id.VerifyPanel);

        RegisterEvents();
    }

    private void Start()
    {
        // Overlay oculto al iniciar
        if (_overlay != null) _overlay.style.display = DisplayStyle.None;

        // Asegurar que todos los subpaneles queden en OUT al inicio
        foreach (var p in _panels.Values)
        {
            if (p == null) continue;
            p.RemoveFromClassList(Uss.In);
            if (!p.ClassListContains(Uss.Out)) p.AddToClassList(Uss.Out);
        }
    }

    // =======================
    // Registro de eventos
    // =======================
    private void RegisterEvents()
    {
        // Main UI
        Q<Button>(Id.Launch)?.RegisterCallback<ClickEvent>(_ => ShowPanel(Id.LoginPanel));
        Q<Button>(Id.Exit)?.RegisterCallback<ClickEvent>(ExitApplication);

        // Login
        Q<Button>(Id.CancelLogin)?.RegisterCallback<ClickEvent>(CancelAll);
        Q<Button>(Id.RegisterLogin)?.RegisterCallback<ClickEvent>(_ =>
        {
            HidePanel(Id.LoginPanel);
            HidePanel(Id.RecoverPanel);
            ShowPanel(Id.RegisterPanel);
        });
        Q<Button>(Id.RecoverLogin)?.RegisterCallback<ClickEvent>(_ =>
        {
            HidePanel(Id.LoginPanel);
            HidePanel(Id.RegisterPanel);
            ShowPanel(Id.RecoverPanel);
        });
        Q<Button>(Id.Login)?.RegisterCallback<ClickEvent>(_ => Debug.Log("Login!!"));

        // Register
        Q<Button>(Id.CancelRegister)?.RegisterCallback<ClickEvent>(CancelAll);
        Q<Button>(Id.BackRegister)?.RegisterCallback<ClickEvent>(_ =>
        {
            HidePanel(Id.RegisterPanel);
            ShowPanel(Id.LoginPanel);
        });
        Q<Button>(Id.Register)?.RegisterCallback<ClickEvent>(_ =>
        {
            HidePanel(Id.RegisterPanel);
            ShowPanel(Id.VerifyPanel);
            Debug.Log("Register!!");
        });

        // Recover
        Q<Button>(Id.CancelRecover)?.RegisterCallback<ClickEvent>(CancelAll);
        Q<Button>(Id.BackRecover)?.RegisterCallback<ClickEvent>(_ =>
        {
            HidePanel(Id.RecoverPanel);
            ShowPanel(Id.LoginPanel);
        });
        Q<Button>(Id.Recover)?.RegisterCallback<ClickEvent>(_ => Debug.Log("Recover Password!!"));

        // Verify email
        Q<Button>(Id.CancelVerify)?.RegisterCallback<ClickEvent>(CancelAll);
        Q<Button>(Id.BackVerify)?.RegisterCallback<ClickEvent>(_ =>
        {
            HidePanel(Id.VerifyPanel);
            ShowPanel(Id.RegisterPanel);
        });
        Q<Button>(Id.ResendCode)?.RegisterCallback<ClickEvent>(_ => Debug.Log("Resend verification code!!"));
        Q<Button>(Id.Verify)?.RegisterCallback<ClickEvent>(_ => Debug.Log("Verify email"));
    }

    // =======================
    // Acciones de alto nivel
    // =======================
    private void CancelAll(ClickEvent _)
    {
        // Oculta todos los paneles; el overlay se ocultará cuando el último termine su transición.
        foreach (var key in _panels.Keys)
            HidePanel(key);
    }

    private void ExitApplication(ClickEvent _)
    {
        Application.Quit();
        // #if UNITY_EDITOR
        // UnityEditor.EditorApplication.isPlaying = false;
        // #endif
    }

    // =======================
    // Mostrar/Ocultar con animaciones
    // =======================
    private void ShowPanel(string name)
    {
        if (!_panels.TryGetValue(name, out var panel) || panel == null) return;

        // Muestra overlay + scrim si es el primer panel en entrar
        if (_openPanels == 0)
        {
            SetOverlayVisible(true);
        }

        // Si ya estaba en IN, no dupliques conteo
        if (!panel.ClassListContains(Uss.In))
        {
            panel.RemoveFromClassList(Uss.Out);
            panel.AddToClassList(Uss.In);
            _openPanels++;
        }
    }

    private void HidePanel(string name)
    {
        if (!_panels.TryGetValue(name, out var panel) || panel == null) return;

        // Si ya está OUT no hacemos nada
        if (panel.ClassListContains(Uss.Out)) return;

        panel.RemoveFromClassList(Uss.In);
        panel.AddToClassList(Uss.Out);

        // Esperar a que termine la transición de este panel para decrementar el contador
        EventCallback<TransitionEndEvent> handler = null;
        handler = (TransitionEndEvent evt) =>
        {
            panel.UnregisterCallback(handler);

            // Sólo cuenta si realmente quedó OUT
            if (!panel.ClassListContains(Uss.In))
            {
                _openPanels = Mathf.Max(0, _openPanels - 1);
                if (_openPanels == 0)
                    SetOverlayVisible(false);
            }
        };
        panel.RegisterCallback(handler);
    }

    // =======================
    // Overlay/Scrim helpers
    // =======================
    private void SetOverlayVisible(bool on)
    {
        if (_overlay == null || _scrim == null) return;

        _overlay.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        _scrim.RemoveFromClassList(on ? Uss.ScrimTransparent : Uss.ScrimOpaque);
        _scrim.AddToClassList(on ? Uss.ScrimOpaque : Uss.ScrimTransparent);
    }

    // =======================
    // Utilidades
    // =======================
    private T Q<T>(string name) where T : VisualElement => _root.Q<T>(name);

    private void CachePanel(string name)
    {
        var ve = Q<VisualElement>(name);
        if (ve == null)
        {
            Debug.LogWarning($"[WelcomeUiController] No se encontró el subpanel '{name}' en el UXML.");
            return;
        }
        _panels[name] = ve;
    }
}
