using System;
using System.Collections.Generic;
using _scripts.models.awsServices;
using _scripts.scriptableObjects;
using Amazon;
using Amazon.CognitoIdentityProvider;
using AmazonWebServices;
using Unity.Tutorials.Core.Editor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace _scripts.controllers
{
    public class WelcomeUiController : MonoBehaviour
    {
        private string _idToken, _accessToken, _refreshToken;

        private Cognito _cognito;
        private IdentityContext _ctx;
        private SimpleEmailService _ses;
        [SerializeField] private UserData userData;
        
        private ArScaraUiController _arScaraUiController;


        #region esto solo para pruebas
            [Header("Datos para pruebas")]

            [SerializeField] private string sourceEmail="mechar09@outlook.com";
            [SerializeField] private string destinationMail="mechar09@yahoo.com";
            [SerializeField] private string subjectMail;
        
            // private RegionEndpoint _region=Amazon.RegionEndpoint.USEast1;
            // private string _identityPoolID = "us-east-1:e962d906-6f36-4e52-8771-a2de6a11b19a";
            // private string _userPoolID     = "us-east-1_eyRKiPuWJ"; 
        
        #endregion


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
            
            public const string UsernameLogin = "usernameLoginField";
            public const string PasswordLogin = "passwordLoginField";
            
            
            public const string UsernameRegister = "usernameRegisterField";
            public const string EmailRegister = "emailRegisterField";
            public const string PasswordRegister = "passwordRegisterField";
            public const string RepeatPasswordRegister = "repeatPasswordRegisterField";
            public const string VerificationCode = "verificationCodeField";
            public const string EmailRecover = "emailRecoverField";
            public const string Body = "body";
            
            
            
            
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
            // #region solo para pruebas
            //     userData.region=_region;
            //     userData.identityPoolID=_identityPoolID;
            //     userData.userPoolID = _userPoolID;
            // #endregion
            
            
            
            
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

        private void OnEnable()
        {
            _cognito = Cognito.GetInstance(
                "62ham6j5iav40urvcooai5vka4",
                new AmazonCognitoIdentityProviderClient(RegionEndpoint.USEast1)
            );
        }

        private void Start()
        {
            _arScaraUiController = GameObject.FindWithTag("arScaraUi").GetComponent<ArScaraUiController>();
            LoadData();
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
            foreach (var p in _panels.Values)
            {
                if (p == null) continue;
                p.RemoveFromClassList(Uss.In);
                if (!p.ClassListContains(Uss.Out)) p.AddToClassList(Uss.Out);
            }
            ShowUI();
        }

        private void LoadData()
        {
            userData.Load();
            Q<TextField>(Id.UsernameLogin).value = userData.username;
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
            Q<Button>(Id.Login)?.RegisterCallback<ClickEvent>(_ =>
            {
                if (Login())
                {
                    HidePanel(Id.LoginPanel);
                    HideUI();
                    _arScaraUiController.ShowUI();
                }
            }
                );
        
            Q<Button>(Id.CancelRegister)?.RegisterCallback<ClickEvent>(CancelAll);
            Q<Button>(Id.BackRegister)?.RegisterCallback<ClickEvent>(_ =>
            {
                HidePanel(Id.RegisterPanel);
                ShowPanel(Id.LoginPanel);
            });
            Q<Button>(Id.Register)?.RegisterCallback<ClickEvent>(_ =>
            {
                if (!Register()) return;
                HidePanel(Id.RegisterPanel);
                ShowPanel(Id.VerifyPanel);
            });
        
            Q<Button>(Id.CancelRecover)?.RegisterCallback<ClickEvent>(CancelAll);
            Q<Button>(Id.BackRecover)?.RegisterCallback<ClickEvent>(_ =>
            {
                HidePanel(Id.RecoverPanel);
                ShowPanel(Id.LoginPanel);
            });
            Q<Button>(Id.Recover)?.RegisterCallback<ClickEvent>(_ =>
            {
                if (RecoverPassword())
                {
                    print("Recover password");
                    
                }
            }

            );
        
            Q<Button>(Id.CancelVerify)?.RegisterCallback<ClickEvent>(CancelAll);
            Q<Button>(Id.BackVerify)?.RegisterCallback<ClickEvent>(_ =>
            {
                HidePanel(Id.VerifyPanel);
                ShowPanel(Id.RegisterPanel);
            });
            Q<Button>(Id.ResendCode)?.RegisterCallback<ClickEvent>(_ => Debug.Log("Resend verification code!!"));
            Q<Button>(Id.Verify)?.RegisterCallback<ClickEvent>(_ => 
            {
                if (VerifyEmail())
                {
                    HidePanel(Id.VerifyPanel);
                    ShowPanel(Id.LoginPanel);
                }
            }
            );
        }

        private void ActivateAwsServices(){
            _ctx = new IdentityContext(userData.region, userData.identityPoolID, userData.userPoolID).AsUser(_idToken);
            _ses = SimpleEmailService.GetInstance(_ctx);
        }
        private bool Login()
        {
            if (!string.IsNullOrEmpty(Q<TextField>(Id.UsernameLogin).value) && !string.IsNullOrEmpty(Q<TextField>(Id.PasswordLogin).value))
            {
                if (Q<TextField>(Id.UsernameLogin).value != userData.username)
                {
                    userData.username=Q<TextField>(Id.UsernameLogin).value;
                }
                
                (_idToken, _accessToken, _refreshToken) = _cognito.Login(userData.username, Q<TextField>(Id.PasswordLogin).value);
                
            
                if (_idToken?.StartsWith("error") == true)
                {
                    _statusCognitoMessage = $"login failed: {_idToken}";
                    print(_statusCognitoMessage);
                    return false;
                }
                else
                {
                    _lastAccessToken = _accessToken;
                    _lastRefreshToken = _refreshToken;
                    _statusCognitoMessage =$"User {userData.username} logged in at {DateTime.Now}";
                    ActivateAwsServices();
                    subjectMail = "login";
                    var result = _ses.SendEmail(sourceEmail, destinationMail, subjectMail, _statusCognitoMessage, "text");
                    print(_statusCognitoMessage);
                }
                return true;
            }

            _statusCognitoMessage = "algun campo esta vacio";
            print(_statusCognitoMessage);
            return false;
        }
        private bool Register()
        {
            {
                if (
                    !string.IsNullOrEmpty(Q<TextField>(Id.UsernameRegister).value) &&
                    !string.IsNullOrEmpty(Q<TextField>(Id.EmailRegister).value) &&
                    !string.IsNullOrEmpty(Q<TextField>(Id.PasswordRegister).value) &&
                    !string.IsNullOrEmpty(Q<TextField>(Id.RepeatPasswordRegister).value)
                )
                {
                    if (Q<TextField>(Id.PasswordRegister).value.Equals(Q<TextField>(Id.RepeatPasswordRegister).value))
                    {
                        var (ok, error, medium, dest) = _cognito.SignUp(
                            Q<TextField>(Id.UsernameRegister).value,
                            Q<TextField>(Id.PasswordRegister).value,
                            new Dictionary<string, string>
                            {
                                ["email"] = Q<TextField>(Id.EmailRegister).value,
                                ["preferred_username"] = Q<TextField>(Id.UsernameRegister).value
                            }
                        );

                        if (string.IsNullOrEmpty(error)) return true;
                        print($"Error: {error}");
                        return false;
                    }
                    else
                    {
                        print("no son iguales");
                        return false;
                    }
                }
                else
                {
                    print("revisa algun campo esta vacio");
                    return false;
                }
            }
        }
        private bool VerifyEmail()
        {
            var result=_cognito.ConfirmSignUp(Q<TextField>(Id.UsernameRegister).value,Q<TextField>(Id.VerificationCode).value);
            return result.ok;
        }
        private bool RecoverPassword()
        {
            var forgot = _cognito.ForgotPasswordStart(Q<TextField>(Id.EmailRecover).value);
            if (forgot.ok)
            {
                print($"Código enviado a {forgot.destination} ({forgot.deliveryMedium})");
                return true;
            }
            else
            {
                print($"Error: {forgot.error}");
                return false;
            }
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

        private void ShowUI()
        {
            Q<VisualElement>(Id.Body).style.display = DisplayStyle.Flex;
        }

        private void HideUI()
        {
            Q<VisualElement>(Id.Body).style.display = DisplayStyle.None;
        }
    }
}
