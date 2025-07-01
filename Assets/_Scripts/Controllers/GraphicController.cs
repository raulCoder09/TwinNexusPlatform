using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class GraphicController : MonoBehaviour
    {
        private VisualElement _body;
        private DropdownField _menuLevelMedara;
        #region menu

        private VisualElement _subpanelsAndSmokeMaskContainer;
        private VisualElement _navigationMenuPanel;
        private VisualElement _scrim;
        private Button _menuButton;
        private Button _hideMenuButton;

        #endregion
        private void Awake()
        {
            GetUiComponents();
            RegisterEvents();
            FindObjects();
        }

        private void Start()
        {
            #region menu

            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;

            #endregion
        }

        private void FindObjects()
        {

        }

        private void RegisterEvents()
        {
            #region menu

            _menuButton.RegisterCallback<ClickEvent>(ShowMenu);
            _hideMenuButton.RegisterCallback<ClickEvent>(HideMenu);
            _navigationMenuPanel.RegisterCallback<TransitionEndEvent>(OnNavigationMenuTransitionComplete);
            #endregion
        }

        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _menuLevelMedara= root.Q<DropdownField>("MenuLevelMedaraDropdownField");
            #region menu

            _menuButton=root.Q<Button>("MenuButton");
            _subpanelsAndSmokeMaskContainer=root.Q<VisualElement>("SubpanelsAndSmokeMaskContainer");
            _navigationMenuPanel=root.Q<VisualElement>("NavigationMenuPanel");
            _scrim = root.Q<VisualElement>("Scrim");
            _hideMenuButton=root.Q<Button>("HideMenuButton");
            #endregion
        }
        
        #region menu

        private void OnNavigationMenuTransitionComplete(TransitionEndEvent evt)
        {
            if (!_navigationMenuPanel.ClassListContains("NavigationMenuPanelInMainScreen"))
            {
                _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.None;
            }
        }

        private void HideMenu(ClickEvent evt)
        {
            _navigationMenuPanel.RemoveFromClassList("NavigationMenuPanelInMainScreen");
            _scrim.RemoveFromClassList("ScrimOpaque");
        }

        private void ShowMenu(ClickEvent evt)
        {
            _subpanelsAndSmokeMaskContainer.style.display = DisplayStyle.Flex;
            _navigationMenuPanel.AddToClassList("NavigationMenuPanelInMainScreen");
            _scrim.AddToClassList("ScrimOpaque");
        }

        #endregion

        internal void EnableLevel()
        {
            gameObject.SetActive(true);
        }

        internal void DisableLevel()
        {
            gameObject.SetActive(false);
        }
    }
}
