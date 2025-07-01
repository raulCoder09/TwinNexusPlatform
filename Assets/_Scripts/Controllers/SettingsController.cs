using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class SettingsController : MonoBehaviour
    {
        private IoTController _ioTController;
        private VisualElement _body;
        private Button _ioTButton;
        private Button _menuButton;
        private VisualElement _navigationMenuPanel;
        private VisualElement _scrim;
        private Button _hideMenuButton;
        
        private VisualElement _subpanelsAndSmokeMaskContainer;
        private void Awake()
        {
            GetUiComponents();
            RegisterEvents();
            FindObjects();
        }
        
        private void Start()
        {
            HideUi();
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
        }
        
        internal void HideUi()
        {
            _body.style.display = DisplayStyle.None;
        }
        internal void ShowUi()
        {
            _body.style.display = DisplayStyle.Flex;
        }

        private void FindObjects()
        {
            _ioTController=GameObject.FindGameObjectWithTag("IoT").GetComponent<IoTController>();
        }

        private void RegisterEvents()
        {
            _ioTButton.RegisterCallback<ClickEvent>(_ =>
            {
                HideUi();
                _ioTController.ShowUi();
            }  );
            _menuButton.RegisterCallback<ClickEvent>(ShowMenu);
            _navigationMenuPanel.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);
            _hideMenuButton.RegisterCallback<ClickEvent>(HideMenu);
        }

        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _ioTButton = root.Q<Button>("IoTButton");
            _subpanelsAndSmokeMaskContainer=root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _menuButton=root.Q<Button>("MenuButton");
            _navigationMenuPanel=root.Q<VisualElement>("NavigationMenuPanel");
            _scrim = root.Q<VisualElement>("Scrim");
            _hideMenuButton=root.Q<Button>("HideMenuButton");
        }
        
        private void ShowMenu(ClickEvent evt)
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.Flex;
            
            

            _navigationMenuPanel.AddToClassList("NavigationMenuPanelInMainScreen");
            _scrim.AddToClassList("ScrimOpaque");
        }
        private void HideMenu(ClickEvent evt)
        {
            _navigationMenuPanel.RemoveFromClassList("NavigationMenuPanelInMainScreen");
            _scrim.RemoveFromClassList("ScrimOpaque");
        }
        
        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            if (!_navigationMenuPanel.ClassListContains("NavigationMenuPanelInMainScreen"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }
    }
}
