using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ArScaraUiController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _root;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;
        _root.Q<Button>("showMenuButton")?.RegisterCallback<ClickEvent>(ShowMainMenu);
        _root.Q<Button>("hideMenuButton")?.RegisterCallback<ClickEvent>(HideMainMenu);
        
        _root.Q<VisualElement>("navigationMenuPanel")?.RegisterCallback<TransitionEndEvent>(evt =>
        {
            if (_root.Q<VisualElement>("navigationMenuPanel").ClassListContains("mainMenuPanelOut"))
                _root.Q<VisualElement>("slidingPanels").style.display = DisplayStyle.None;
        });
        
    }
    private void Start()
    {
        _root.Q<VisualElement>("slidingPanels").style.display = DisplayStyle.None;
        _root.Q<DropdownField>("enviromentMenu").value = "Enviroment";
        _root.Q<DropdownField>("arscaraMenu").value = "ARSCARA menu";
    }

    private void ShowMainMenu(ClickEvent evt)
    {
        ShowPanel("navigationMenuPanel","mainMenuPanel");
    }

    private void HideMainMenu(ClickEvent evt)
    {
        HidePanel("navigationMenuPanel","mainMenuPanel");
    }
    
    private void EnableSlidingPanels()
    {
        _root.Q<VisualElement>("slidingPanels").style.display = DisplayStyle.Flex;
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
    
    private void ShowPanel(string panelName,string position)
    {
        if (panelName=="navigationMenuPanel")
        {
            EnableSlidingPanels();
            ScrimMakeOpaque();
        }
        _root.Q<VisualElement>(panelName).RemoveFromClassList(position+"Out");
        _root.Q<VisualElement>(panelName).AddToClassList(position+"In");
    }
    
    private void HidePanel(string panelName,string position)
    {
        _root.Q<VisualElement>(panelName).RemoveFromClassList(position+"In");
        _root.Q<VisualElement>(panelName).AddToClassList(position+"Out");
        if (panelName=="navigationMenuPanel")
        {
            EnableSlidingPanels();
            ScrimMakeTransparent();
        }

    }
    
}
