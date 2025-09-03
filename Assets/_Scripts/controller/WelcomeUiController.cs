using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.WSA;

public class WelcomeUiController : MonoBehaviour
{
    private UIDocument _uiDocument; 
    private VisualElement _root;
        

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;
        _root.Q<VisualElement>("slidingPanels").style.display = DisplayStyle.None;
        _root.Q<Button>("launchButton")?.RegisterCallback<ClickEvent>(abrelogin);
        _root.Q<Button>("cancelLoginButton")?.RegisterCallback<ClickEvent>(cierralogin);
        _root.Q<Button>("registerLoginButton")?.RegisterCallback<ClickEvent>(abreregister);
        _root.Q<Button>("recoverPasswordLoginButton")?.RegisterCallback<ClickEvent>(abrerecover);
    }

    private void abrelogin(ClickEvent evt)
    {
        _root.Q<VisualElement>("slidingPanels").style.display = DisplayStyle.Flex;
        _root.Q<VisualElement>("scrim").RemoveFromClassList("scrimTransparent");
        _root.Q<VisualElement>("scrim").AddToClassList("scrimOpaque");
        _root.Q<VisualElement>("loginPanel").RemoveFromClassList("subpanelPanelOut");
        _root.Q<VisualElement>("loginPanel").AddToClassList("subpanelPanelIn");
    }
    private void cierralogin(ClickEvent evt)
    {
        _root.Q<VisualElement>("slidingPanels").style.display = DisplayStyle.None;
        _root.Q<VisualElement>("loginPanel").RemoveFromClassList("subpanelPanelIn");
        _root.Q<VisualElement>("loginPanel").AddToClassList("subpanelPanelOut");
        _root.Q<VisualElement>("scrim").RemoveFromClassList("scrimOpaque");
        _root.Q<VisualElement>("scrim").AddToClassList("scrimTransparent");
    }

    private void abreregister(ClickEvent evt)
    {
        _root.Q<VisualElement>("loginPanel").RemoveFromClassList("subpanelPanelIn");
        _root.Q<VisualElement>("loginPanel").AddToClassList("subpanelPanelOut");
        _root.Q<VisualElement>("registerPanel").AddToClassList("subpanelPanelIn");
        _root.Q<VisualElement>("registerPanel").RemoveFromClassList("subpanelPanelOut");
    }

    private void abrerecover(ClickEvent evt)
    {
        _root.Q<VisualElement>("loginPanel").RemoveFromClassList("subpanelPanelIn");
        _root.Q<VisualElement>("loginPanel").AddToClassList("subpanelPanelOut");
        _root.Q<VisualElement>("recoverPasswordPanel").AddToClassList("subpanelPanelIn");
        _root.Q<VisualElement>("recoverPasswordPanel").RemoveFromClassList("subpanelPanelOut");
    }
}



