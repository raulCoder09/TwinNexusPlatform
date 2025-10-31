using System;
using System.Collections.Generic;
using _scripts.models.awsServices;
using Amazon;
using Amazon.CognitoIdentityProvider;
using AmazonWebServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace _scripts.controllers
{
    public class WelcomeUiController : MonoBehaviour
    {
        private string _lastAccessToken = null;
        private string _lastRefreshToken = null;
        private string _statusCognitoMessage;

        private static class Id
        {
            public const string SlidingPanels = "slidingPanels";
            public const string Scrim = "scrim";

            public const string Launch = "launchButton";
            public const string Exit = "exitButton";
        
            public const string LoginPanel = "loginPanel";
            public const string CancelLogin = "cancelLoginButton";
            public const string RegisterLogin = "registerLoginButton";
            public const string RecoverLogin = "recoverLoginButton";
            public const string Login = "loginButton";
        
            public const string RegisterPanel = "registerPanel";
            public const string CancelRegister = "cancelRegisterButton";
            public const string BackRegister = "backRegisterButton";
            public const string Register = "registerButton";
        
            public const string RecoverPanel = "recoverPasswordPanel";
            public const string CancelRecover = "cancelRecoverButton";
            public const string BackRecover = "backRecoverButton";
            public const string Recover = "recoverPasswordButton";
        
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
    
        private UIDocument _doc;
        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _scrim;
    
        private readonly Dictionary<string, VisualElement> _panels = new();
    
        private int _openPanels = 0;
    
        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;
        
            _overlay = Q<VisualElement>(Id.SlidingPanels);
            _scrim = Q<VisualElement>(Id.Scrim);
        
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
    
        private void RegisterEvents()
        {
            Q<Button>(Id.Launch)?.RegisterCallback<ClickEvent>(_ => ShowPanel(Id.LoginPanel));
            Q<Button>(Id.Exit)?.RegisterCallback<ClickEvent>(ExitApplication);
        
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
            Q<Button>(Id.Login)?.RegisterCallback<ClickEvent>(_ =>Login());
        
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
        
            Q<Button>(Id.CancelRecover)?.RegisterCallback<ClickEvent>(CancelAll);
            Q<Button>(Id.BackRecover)?.RegisterCallback<ClickEvent>(_ =>
            {
                HidePanel(Id.RecoverPanel);
                ShowPanel(Id.LoginPanel);
            });
            Q<Button>(Id.Recover)?.RegisterCallback<ClickEvent>(_ => Debug.Log("Recover Password!!"));
        
            Q<Button>(Id.CancelVerify)?.RegisterCallback<ClickEvent>(CancelAll);
            Q<Button>(Id.BackVerify)?.RegisterCallback<ClickEvent>(_ =>
            {
                HidePanel(Id.VerifyPanel);
                ShowPanel(Id.RegisterPanel);
            });
            Q<Button>(Id.ResendCode)?.RegisterCallback<ClickEvent>(_ => Debug.Log("Resend verification code!!"));
            Q<Button>(Id.Verify)?.RegisterCallback<ClickEvent>(_ => Debug.Log("Verify email"));
        }

        private void Login()
        {
            var cognito = Cognito.GetInstance(
                "62ham6j5iav40urvcooai5vka4",
                new AmazonCognitoIdentityProviderClient(RegionEndpoint.USEast1)
            );
            
            
            
            var region = Amazon.RegionEndpoint.USEast1;
            const string IDENTITY_POOL_ID = "us-east-1:e962d906-6f36-4e52-8771-a2de6a11b19a";
            const string USER_POOL_ID     = "us-east-1_eyRKiPuWJ";
            var (idToken, accessToken, refreshToken) = cognito.Login("raulCoder09", "AntoyDuna09!");
            var ctx = new IdentityContext(region, IDENTITY_POOL_ID, USER_POOL_ID).AsUser(idToken);
            var ses = SimpleEmailService.GetInstance(ctx);

            
            if (idToken?.StartsWith("error") == true)
            {
                _statusCognitoMessage = $"login failed: {idToken}";
            }
            else
            {
                _lastAccessToken = accessToken;
                _lastRefreshToken = refreshToken;
                _statusCognitoMessage =$"Login ok at {DateTime.Now}";
                var result = ses.SendEmail("mechar09@outlook.com", "mechar09@yahoo.com", "login", _statusCognitoMessage, "text");
            }
            print(_statusCognitoMessage);
            

            
        }
        private void CancelAll(ClickEvent _)
        {
            foreach (var key in _panels.Keys)
                HidePanel(key);
        }

        private void ExitApplication(ClickEvent _)
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    
        private void ShowPanel(string name)
        {
            if (!_panels.TryGetValue(name, out var panel) || panel == null) return;
        
            if (_openPanels == 0)
            {
                SetOverlayVisible(true);
            }
        
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
        
            if (panel.ClassListContains(Uss.Out)) return;

            panel.RemoveFromClassList(Uss.In);
            panel.AddToClassList(Uss.Out); 
        
            EventCallback<TransitionEndEvent> handler = null;
            handler = (TransitionEndEvent evt) =>
            {
                panel.UnregisterCallback(handler);
            
                if (!panel.ClassListContains(Uss.In))
                {
                    _openPanels = Mathf.Max(0, _openPanels - 1);
                    if (_openPanels == 0)
                        SetOverlayVisible(false);
                }
            };
            panel.RegisterCallback(handler);
        }
    
        private void SetOverlayVisible(bool on)
        {
            if (_overlay == null || _scrim == null) return;

            _overlay.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            _scrim.RemoveFromClassList(on ? Uss.ScrimTransparent : Uss.ScrimOpaque);
            _scrim.AddToClassList(on ? Uss.ScrimOpaque : Uss.ScrimTransparent);
        }

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
}
