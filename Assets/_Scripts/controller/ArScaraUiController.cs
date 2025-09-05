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
        _root.Q<DropdownField>("modeDropdown")?.RegisterValueChangedCallback(ModeSelected);
        _root.Q<DropdownField>("speedDropdown")?.RegisterValueChangedCallback(SpeedSelected);
    }

    private void SpeedSelected(ChangeEvent<string> evt)
    {
    }

    private void ModeSelected(ChangeEvent<string> evt)
    {
        switch (evt.newValue)
        {
            case "World":
                _root.Q<DropdownField>("speedDropdown")?.RemoveFromClassList("hideItem");
                _root.Q<DropdownField>("speedDropdown")?.AddToClassList("showItem");
                _root.Q<Button>("plusXButton")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("plusXButton")?.AddToClassList("showItem");
                _root.Q<Button>("minusXButton")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("minusXButton")?.AddToClassList("showItem");
                
                _root.Q<Button>("plusYButton")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("plusYButton")?.AddToClassList("showItem");
                _root.Q<Button>("minusYButton")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("minusYButton")?.AddToClassList("showItem");
                
                _root.Q<Button>("plusZButton")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("plusZButton")?.AddToClassList("showItem");
                _root.Q<Button>("minusZButton")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("minusZButton")?.AddToClassList("showItem");
                
                _root.Q<Button>("plusUButton")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("plusUButton")?.AddToClassList("showItem");
                _root.Q<Button>("minusUButton")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("minusUButton")?.AddToClassList("showItem");
                
                _root.Q<Button>("plusJ1Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusJ1Button")?.AddToClassList("hideItem");
                _root.Q<Button>("minusJ1Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusJ1Button")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusJ2Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusJ2Button")?.AddToClassList("hideItem");
                _root.Q<Button>("minusJ2Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusJ2Button")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusJ3Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusJ3Button")?.AddToClassList("hideItem");
                _root.Q<Button>("minusJ3Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusJ3Button")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusJ4Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusJ4Button")?.AddToClassList("hideItem");
                _root.Q<Button>("minusJ4Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusJ4Button")?.AddToClassList("hideItem");
                
                _root.Q<Label>("xLabel")?.RemoveFromClassList("hideItem");
                _root.Q<Label>("xLabel")?.AddToClassList("showItem");
                _root.Q<Label>("yLabel")?.RemoveFromClassList("hideItem");
                _root.Q<Label>("yLabel")?.AddToClassList("showItem");
                _root.Q<Label>("zLabel")?.RemoveFromClassList("hideItem");
                _root.Q<Label>("zLabel")?.AddToClassList("showItem");
                _root.Q<Label>("uLabel")?.RemoveFromClassList("hideItem");
                _root.Q<Label>("uLabel")?.AddToClassList("showItem");
                
                _root.Q<Label>("j1Label")?.RemoveFromClassList("showItem");
                _root.Q<Label>("j1Label")?.AddToClassList("hideItem");
                _root.Q<Label>("j2Label")?.RemoveFromClassList("showItem");
                _root.Q<Label>("j2Label")?.AddToClassList("hideItem");
                _root.Q<Label>("j3Label")?.RemoveFromClassList("showItem");
                _root.Q<Label>("j3Label")?.AddToClassList("hideItem");
                _root.Q<Label>("j4Label")?.RemoveFromClassList("showItem");
                _root.Q<Label>("j4Label")?.AddToClassList("hideItem");
                
                _root.Q<RadioButton>("continuousMove")?.RemoveFromClassList("hideItem");
                _root.Q<RadioButton>("continuousMove")?.AddToClassList("showItem");
                _root.Q<RadioButton>("longMove")?.RemoveFromClassList("hideItem");
                _root.Q<RadioButton>("longMove")?.AddToClassList("showItem");
                _root.Q<RadioButton>("mediumMove")?.RemoveFromClassList("hideItem");
                _root.Q<RadioButton>("mediumMove")?.AddToClassList("showItem");
                _root.Q<RadioButton>("shortMove")?.RemoveFromClassList("hideItem");
                _root.Q<RadioButton>("shortMove")?.AddToClassList("showItem");
                
                _root.Q<Button>("TeachButton")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("TeachButton")?.AddToClassList("showItem");
                _root.Q<Button>("EditButton")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("EditButton")?.AddToClassList("showItem");
                
                _root.Q<DropdownField>("CommandDropdown")?.RemoveFromClassList("hideItem");
                _root.Q<DropdownField>("CommandDropdown")?.AddToClassList("showItem");
                _root.Q<DropdownField>("DestinationDropdown")?.RemoveFromClassList("hideItem");
                _root.Q<DropdownField>("DestinationDropdown")?.AddToClassList("showItem");
                break;
            case "Joint":
                _root.Q<DropdownField>("speedDropdown")?.RemoveFromClassList("hideItem");
                _root.Q<DropdownField>("speedDropdown")?.AddToClassList("showItem");
                _root.Q<Button>("plusJ1Button")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("plusJ1Button")?.AddToClassList("showItem");
                _root.Q<Button>("minusJ1Button")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("minusJ1Button")?.AddToClassList("showItem");
                
                _root.Q<Button>("plusJ2Button")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("plusJ2Button")?.AddToClassList("showItem");
                _root.Q<Button>("minusJ2Button")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("minusJ2Button")?.AddToClassList("showItem");
                
                _root.Q<Button>("plusJ3Button")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("plusJ3Button")?.AddToClassList("showItem");
                _root.Q<Button>("minusJ3Button")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("minusJ3Button")?.AddToClassList("showItem");
                
                _root.Q<Button>("plusJ4Button")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("plusJ4Button")?.AddToClassList("showItem");
                _root.Q<Button>("minusJ4Button")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("minusJ4Button")?.AddToClassList("showItem");
                
                _root.Q<Button>("plusXButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusXButton")?.AddToClassList("hideItem");
                _root.Q<Button>("minusXButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusXButton")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusYButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusYButton")?.AddToClassList("hideItem");
                _root.Q<Button>("minusYButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusYButton")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusZButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusZButton")?.AddToClassList("hideItem");
                _root.Q<Button>("minusZButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusZButton")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusUButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusUButton")?.AddToClassList("hideItem");
                _root.Q<Button>("minusUButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusUButton")?.AddToClassList("hideItem");
                
                _root.Q<Label>("xLabel")?.RemoveFromClassList("showItem");
                _root.Q<Label>("xLabel")?.AddToClassList("hideItem");
                _root.Q<Label>("yLabel")?.RemoveFromClassList("showItem");
                _root.Q<Label>("yLabel")?.AddToClassList("hideItem");
                _root.Q<Label>("zLabel")?.RemoveFromClassList("showItem");
                _root.Q<Label>("zLabel")?.AddToClassList("hideItem");
                _root.Q<Label>("uLabel")?.RemoveFromClassList("showItem");
                _root.Q<Label>("uLabel")?.AddToClassList("hideItem");
                
                _root.Q<Label>("j1Label")?.RemoveFromClassList("hideItem");
                _root.Q<Label>("j1Label")?.AddToClassList("showItem");
                _root.Q<Label>("j2Label")?.RemoveFromClassList("hideItem");
                _root.Q<Label>("j2Label")?.AddToClassList("showItem");
                _root.Q<Label>("j3Label")?.RemoveFromClassList("hideItem");
                _root.Q<Label>("j3Label")?.AddToClassList("showItem");
                _root.Q<Label>("j4Label")?.RemoveFromClassList("hideItem");
                _root.Q<Label>("j4Label")?.AddToClassList("showItem");
                
                _root.Q<RadioButton>("continuousMove")?.RemoveFromClassList("hideItem");
                _root.Q<RadioButton>("continuousMove")?.AddToClassList("showItem");
                _root.Q<RadioButton>("longMove")?.RemoveFromClassList("hideItem");
                _root.Q<RadioButton>("longMove")?.AddToClassList("showItem");
                _root.Q<RadioButton>("mediumMove")?.RemoveFromClassList("hideItem");
                _root.Q<RadioButton>("mediumMove")?.AddToClassList("showItem");
                _root.Q<RadioButton>("shortMove")?.RemoveFromClassList("hideItem");
                _root.Q<RadioButton>("shortMove")?.AddToClassList("showItem");
                
                _root.Q<Button>("TeachButton")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("TeachButton")?.AddToClassList("showItem");
                _root.Q<Button>("EditButton")?.RemoveFromClassList("hideItem");
                _root.Q<Button>("EditButton")?.AddToClassList("showItem");
                
                _root.Q<DropdownField>("CommandDropdown")?.RemoveFromClassList("hideItem");
                _root.Q<DropdownField>("CommandDropdown")?.AddToClassList("showItem");
                _root.Q<DropdownField>("DestinationDropdown")?.RemoveFromClassList("hideItem");
                _root.Q<DropdownField>("DestinationDropdown")?.AddToClassList("showItem");
                break;
            default:
                _root.Q<DropdownField>("speedDropdown")?.RemoveFromClassList("showItem");
                _root.Q<DropdownField>("speedDropdown")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusXButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusXButton")?.AddToClassList("hideItem");
                _root.Q<Button>("minusXButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusXButton")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusYButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusYButton")?.AddToClassList("hideItem");
                _root.Q<Button>("minusYButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusYButton")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusZButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusZButton")?.AddToClassList("hideItem");
                _root.Q<Button>("minusZButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusZButton")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusUButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusUButton")?.AddToClassList("hideItem");
                _root.Q<Button>("minusUButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusUButton")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusJ1Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusJ1Button")?.AddToClassList("hideItem");
                _root.Q<Button>("minusJ1Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusJ1Button")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusJ2Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusJ2Button")?.AddToClassList("hideItem");
                _root.Q<Button>("minusJ2Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusJ2Button")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusJ3Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusJ3Button")?.AddToClassList("hideItem");
                _root.Q<Button>("minusJ3Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusJ3Button")?.AddToClassList("hideItem");
                
                _root.Q<Button>("plusJ4Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("plusJ4Button")?.AddToClassList("hideItem");
                _root.Q<Button>("minusJ4Button")?.RemoveFromClassList("showItem");
                _root.Q<Button>("minusJ4Button")?.AddToClassList("hideItem");
                
                _root.Q<Label>("xLabel")?.RemoveFromClassList("showItem");
                _root.Q<Label>("xLabel")?.AddToClassList("hideItem");
                _root.Q<Label>("yLabel")?.RemoveFromClassList("showItem");
                _root.Q<Label>("yLabel")?.AddToClassList("hideItem");
                _root.Q<Label>("zLabel")?.RemoveFromClassList("showItem");
                _root.Q<Label>("zLabel")?.AddToClassList("hideItem");
                _root.Q<Label>("uLabel")?.RemoveFromClassList("showItem");
                _root.Q<Label>("uLabel")?.AddToClassList("hideItem");
                
                _root.Q<Label>("j1Label")?.RemoveFromClassList("showItem");
                _root.Q<Label>("j1Label")?.AddToClassList("hideItem");
                _root.Q<Label>("j2Label")?.RemoveFromClassList("showItem");
                _root.Q<Label>("j2Label")?.AddToClassList("hideItem");
                _root.Q<Label>("j3Label")?.RemoveFromClassList("showItem");
                _root.Q<Label>("j3Label")?.AddToClassList("hideItem");
                _root.Q<Label>("j4Label")?.RemoveFromClassList("showItem");
                _root.Q<Label>("j4Label")?.AddToClassList("hideItem");
                
                _root.Q<RadioButton>("continuousMove")?.RemoveFromClassList("showItem");
                _root.Q<RadioButton>("continuousMove")?.AddToClassList("hideItem");
                _root.Q<RadioButton>("longMove")?.RemoveFromClassList("showItem");
                _root.Q<RadioButton>("longMove")?.AddToClassList("hideItem");
                _root.Q<RadioButton>("mediumMove")?.RemoveFromClassList("showItem");
                _root.Q<RadioButton>("mediumMove")?.AddToClassList("hideItem");
                _root.Q<RadioButton>("shortMove")?.RemoveFromClassList("showItem");
                _root.Q<RadioButton>("shortMove")?.AddToClassList("hideItem");
                
                _root.Q<Button>("TeachButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("TeachButton")?.AddToClassList("hideItem");
                _root.Q<Button>("EditButton")?.RemoveFromClassList("showItem");
                _root.Q<Button>("EditButton")?.AddToClassList("hideItem");
                
                _root.Q<DropdownField>("CommandDropdown")?.RemoveFromClassList("showItem");
                _root.Q<DropdownField>("CommandDropdown")?.AddToClassList("hideItem");
                _root.Q<DropdownField>("DestinationDropdown")?.RemoveFromClassList("showItem");
                _root.Q<DropdownField>("DestinationDropdown")?.AddToClassList("hideItem");
                break; 
        }
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
        _root.Q<DropdownField>("speedDropdown").value = "Speed";
        _root.Q<DropdownField>("CommandDropdown").value = "Command";
        _root.Q<DropdownField>("DestinationDropdown").value = "Destination";
        _root.Q<Label>("warningMessages").text = "Select a work environment";
        _root.Q<DropdownField>("pointsDropdown").value = "Points";
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
