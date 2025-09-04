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
        
        _root.Q<DropdownField>("enviromentMenu")?.RegisterValueChangedCallback(evt =>
        {
            
            EnviromentMenu(evt.newValue);
        });
        
        _root.Q<DropdownField>("arscaraMenu")?.RegisterValueChangedCallback(evt =>
        {

            ArscaraMenu(evt.newValue);
        });
        
    }

    private void EnviromentMenu(string enviroment)
    {
        switch (enviroment)
        {
            case "Virtual":
                _root.Q<DropdownField>("arscaraMenu").SetEnabled(true);
                _root.Q<Label>("warningMessages").text = "Select ARSCARA robot interface";
                break;
            case "Augmented":
                _root.Q<DropdownField>("arscaraMenu").SetEnabled(true);
                _root.Q<Label>("warningMessages").text = "Select ARSCARA robot interface";
                break;
            case "Hybrid":
                _root.Q<DropdownField>("arscaraMenu").SetEnabled(true);
                _root.Q<Label>("warningMessages").text = "Select ARSCARA robot interface";
                break;
            case "Real":
                _root.Q<DropdownField>("arscaraMenu").SetEnabled(true);
                _root.Q<Label>("warningMessages").text = "Select ARSCARA robot interface";
                break;
            default:
                _root.Q<DropdownField>("arscaraMenu").SetEnabled(false);
                _root.Q<Label>("warningMessages").text = "Select a work environment";
                break;
        }
    }

    private void ArscaraMenu(string value)
    {
        switch (value)
        {
            case "Control panel":
                _root.Q<Label>("warningMessages").style.display = DisplayStyle.None;
                _root.Q<VisualElement>("controlPanel").RemoveFromClassList("arScaraPanelsOut");
                _root.Q<VisualElement>("controlPanel").AddToClassList("arScaraPanelsIn");
                _root.Q<VisualElement>("jogAndTeachPanel").RemoveFromClassList("arScaraPanelsIn");
                _root.Q<VisualElement>("jogAndTeachPanel").AddToClassList("arScaraPanelsOut");
                _root.Q<VisualElement>("pointsPanel").RemoveFromClassList("arScaraPanelsIn");
                _root.Q<VisualElement>("pointsPanel").AddToClassList("arScaraPanelsOut");
                break;
            case "Jog and teach":
                _root.Q<Label>("warningMessages").style.display = DisplayStyle.None;
                _root.Q<VisualElement>("controlPanel").RemoveFromClassList("arScaraPanelsIn");
                _root.Q<VisualElement>("controlPanel").AddToClassList("arScaraPanelsOut");
                _root.Q<VisualElement>("jogAndTeachPanel").RemoveFromClassList("arScaraPanelsOut");
                _root.Q<VisualElement>("jogAndTeachPanel").AddToClassList("arScaraPanelsIn");
                _root.Q<VisualElement>("pointsPanel").RemoveFromClassList("arScaraPanelsIn");
                _root.Q<VisualElement>("pointsPanel").AddToClassList("arScaraPanelsOut");
                break;
            case "Points":
                _root.Q<Label>("warningMessages").style.display = DisplayStyle.None;
                _root.Q<VisualElement>("controlPanel").RemoveFromClassList("arScaraPanelsIn");
                _root.Q<VisualElement>("controlPanel").AddToClassList("arScaraPanelsOut");
                _root.Q<VisualElement>("jogAndTeachPanel").RemoveFromClassList("arScaraPanelsIn");
                _root.Q<VisualElement>("jogAndTeachPanel").AddToClassList("arScaraPanelsOut");
                _root.Q<VisualElement>("pointsPanel").RemoveFromClassList("arScaraPanelsOut");
                _root.Q<VisualElement>("pointsPanel").AddToClassList("arScaraPanelsIn");
                break;
            default:
                _root.Q<Label>("warningMessages").style.display = DisplayStyle.Flex;
                if (value=="Enviroment")
                {
                    _root.Q<Label>("warningMessages").text = "Select a work environment";
                }
                else
                {
                    _root.Q<Label>("warningMessages").text = "Select ARSCARA robot interface";
                }
                
                _root.Q<VisualElement>("controlPanel").RemoveFromClassList("arScaraPanelsIn");
                _root.Q<VisualElement>("controlPanel").AddToClassList("arScaraPanelsOut");
                _root.Q<VisualElement>("jogAndTeachPanel").RemoveFromClassList("arScaraPanelsIn");
                _root.Q<VisualElement>("jogAndTeachPanel").AddToClassList("arScaraPanelsOut");
                _root.Q<VisualElement>("pointsPanel").RemoveFromClassList("arScaraPanelsIn");
                _root.Q<VisualElement>("pointsPanel").AddToClassList("arScaraPanelsOut");
                break;
        }
    }

    private void Start()
    {
        _root.Q<VisualElement>("slidingPanels").style.display = DisplayStyle.None;
        _root.Q<DropdownField>("enviromentMenu").value = "Enviroment";
        _root.Q<DropdownField>("arscaraMenu").value = "ARSCARA menu";
        _root.Q<DropdownField>("views").value = "Select view";
        _root.Q<DropdownField>("modeDropdown").value = "Mode";
        _root.Q<Label>("warningMessages").text = "Select a work environment";
    }

    private void ShowMainMenu(ClickEvent evt)
    {
        ShowPanel("navigationMenuPanel","mainMenuPanel");
        _root.Q<DropdownField>("enviromentMenu").value = "Enviroment";
        _root.Q<DropdownField>("arscaraMenu").value = "ARSCARA menu";
        _root.Q<Label>("warningMessages").text = "Select a work environment";
        _root.Q<VisualElement>("controlPanel").RemoveFromClassList("arScaraPanelsIn");
        _root.Q<VisualElement>("controlPanel").AddToClassList("arScaraPanelsOut");
        _root.Q<VisualElement>("jogAndTeachPanel").RemoveFromClassList("arScaraPanelsIn");
        _root.Q<VisualElement>("jogAndTeachPanel").AddToClassList("arScaraPanelsOut");
        _root.Q<VisualElement>("pointsPanel").RemoveFromClassList("arScaraPanelsIn");
        _root.Q<VisualElement>("pointsPanel").AddToClassList("arScaraPanelsOut");
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
