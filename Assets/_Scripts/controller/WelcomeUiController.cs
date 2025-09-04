using System;
using UnityEngine;
using UnityEngine.UIElements;

public class WelcomeUiController : MonoBehaviour
{
    private UIDocument _uiDocument; 
    private VisualElement _root;
    private void Awake()
    {
        RegisterEvents();
    }

    private void Start()
    {
        _root.Q<VisualElement>("slidingPanels").style.display = DisplayStyle.None;
    }

    private void RegisterEvents()
    {
        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;

        #region events main welcome ui
        _root.Q<Button>("launchButton")?.RegisterCallback<ClickEvent>(evt =>
        {
            ShowSubpanel("loginPanel");
        });
        
        _root.Q<Button>("exitButton")?.RegisterCallback<ClickEvent>(ExitApplication);
        #endregion

        #region events login panel buttons
            _root.Q<Button>("cancelLoginButton")?.RegisterCallback<ClickEvent>(CancelAll);
            _root.Q<Button>("registerLoginButton")?.RegisterCallback<ClickEvent>(evt =>
            {
                HideSubpanel("loginPanel");
                HideSubpanel("recoverPasswordPanel");
                ShowSubpanel("registerPanel");
            });
            _root.Q<Button>("recoverLoginButton")?.RegisterCallback<ClickEvent>(evt =>
            {
                HideSubpanel("loginPanel");
                HideSubpanel("registerPanel");
                ShowSubpanel("recoverPasswordPanel");
            });
            
            _root.Q<Button>("loginButton")?.RegisterCallback<ClickEvent>(Login);
        #endregion

        #region events register panel  buttons
            _root.Q<Button>("cancelRegisterButton")?.RegisterCallback<ClickEvent>(CancelAll);
            _root.Q<Button>("backRegisterButton")?.RegisterCallback<ClickEvent>(evt =>
            {
                HideSubpanel("registerPanel");
                ShowSubpanel("loginPanel");
            });
            _root.Q<Button>("registerButton")?.RegisterCallback<ClickEvent>(evt =>
            {
                HideSubpanel("registerPanel");
                ShowSubpanel("emailVerificationPanel");
                print("Register!!");
            });
        #endregion

        #region events recover Password Panel

            _root.Q<Button>("cancelRecoverButton")?.RegisterCallback<ClickEvent>(CancelAll);
            _root.Q<Button>("backRecoverButton")?.RegisterCallback<ClickEvent>(evt =>
            {
                HideSubpanel("recoverPasswordPanel");
                ShowSubpanel("loginPanel");
            });
            _root.Q<Button>("recoverPasswordButton")?.RegisterCallback<ClickEvent>(RecoverPassword);
        #endregion

        #region events email verification panel
            _root.Q<Button>("cancelVerifyEmailButton")?.RegisterCallback<ClickEvent>(CancelAll);
            _root.Q<Button>("backVerifyEmailButton")?.RegisterCallback<ClickEvent>(evt =>
            {
                HideSubpanel("emailVerificationPanel");
                ShowSubpanel("registerPanel");
            });
            _root.Q<Button>("resendVerificationCodeButton")?.RegisterCallback<ClickEvent>(ResendVerificationCode);
            _root.Q<Button>("verifyEmailButton")?.RegisterCallback<ClickEvent>(VerifyEmail);
        #endregion
        
    }
    
    private void VerifyEmail(ClickEvent evt)
    {
        print("Verify email");
    }

    private void ResendVerificationCode(ClickEvent evt)
    {
        print("Resend verification code!!");
    }

    private void RecoverPassword(ClickEvent evt)
    {
        print("Recover Password!!");
    }
    
    private void CancelAll(ClickEvent evt)
    {
        HideSubpanel("loginPanel");
        HideSubpanel("recoverPasswordPanel");
        HideSubpanel("registerPanel");
        HideSubpanel("emailVerificationPanel");
        DisableSlidingPanels("loginPanel");
        DisableSlidingPanels("registerPanel");
        DisableSlidingPanels("recoverPasswordPanel");
        DisableSlidingPanels("emailVerificationPanel");

    }

    private void EnableSlidingPanels()
    {
        _root.Q<VisualElement>("slidingPanels").style.display = DisplayStyle.Flex;
    }
    
    private void DisableSlidingPanels(string subpanelName)
    {
        var subpanel = _root.Q<VisualElement>(subpanelName);
        if (subpanel == null) return;
        
        EventCallback<TransitionEndEvent> handler = null;
        handler = (TransitionEndEvent evt) =>
        {
            subpanel.UnregisterCallback(handler);
            if (subpanel.ClassListContains("subpanelPanelOut"))
            {
                _root.Q<VisualElement>("slidingPanels").style.display = DisplayStyle.None;
                ScrimMakeTransparent();
            }
        };

        subpanel.RegisterCallback(handler);
    }


    private void ExitApplication(ClickEvent evt)
    {
        Application.Quit();
    }

    private void Login(ClickEvent evt)
    {
        print("Login!!");
    }
    
    private void ShowSubpanel(string subpanelName)
    {
        EnableSlidingPanels();
        ScrimMakeOpaque();
        _root.Q<VisualElement>(subpanelName).RemoveFromClassList("subpanelPanelOut");
        _root.Q<VisualElement>(subpanelName).AddToClassList("subpanelPanelIn");
    }

    private void HideSubpanel(string subpanelName)
    {
        _root.Q<VisualElement>(subpanelName).RemoveFromClassList("subpanelPanelIn");
        _root.Q<VisualElement>(subpanelName).AddToClassList("subpanelPanelOut");
    }

    private void ScrimMakeTransparent()
    {
        _root.Q<VisualElement>("scrim").RemoveFromClassList("scrimOpaque");
        _root.Q<VisualElement>("scrim").AddToClassList("scrimTransparent");
    }
    private void ScrimMakeOpaque()
    {
        _root.Q<VisualElement>("scrim").RemoveFromClassList("scrimTransparent");
        _root.Q<VisualElement>("scrim").AddToClassList("scrimOpaque");
    }
}
