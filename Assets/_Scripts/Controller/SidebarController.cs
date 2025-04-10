using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class SidebarController : MonoBehaviour
    {
        private VisualElement root;
        private VisualElement sidebar;
        private Button hamburgerButton;
        private Button closeSidebarButton;
        private VisualElement mainContent;

        private void OnEnable()
        {
            // Get the root element of the UI document
            root = GetComponent<UIDocument>().rootVisualElement;
        
            // Get references to the elements we need to control
            sidebar = root.Q<VisualElement>("Sidebar");
            hamburgerButton = root.Q<Button>("HamburgerButton");
            closeSidebarButton = root.Q<Button>("CloseSidebarButton");
            mainContent = root.Q<VisualElement>("MainContent");
        
            // Register click event handlers
            hamburgerButton.clicked += ShowSidebar;
            closeSidebarButton.clicked += HideSidebar;
        
            // Initialize the UI (sidebar expanded by default)
            hamburgerButton.style.display = DisplayStyle.None;
        
            // Ensure the hamburger button is always at the front
            hamburgerButton.BringToFront();
        }
    
        private void ShowSidebar()
        {
            // Remove collapsed class and add expanded class
            sidebar.RemoveFromClassList("sidebar-collapsed");
            sidebar.AddToClassList("sidebar-expanded");
        
            // Hide hamburger button
            hamburgerButton.style.display = DisplayStyle.None;
        
            // Adjust main content margin to accommodate the sidebar
            mainContent.style.marginLeft = 0;
            print("sidebar show");
        }
    
        private void HideSidebar()
        {
            // Remove expanded class and add collapsed class
            sidebar.RemoveFromClassList("sidebar-expanded");
            sidebar.AddToClassList("sidebar-collapsed");
        
            // Show hamburger button and bring it to front
            hamburgerButton.style.display = DisplayStyle.Flex;
            hamburgerButton.BringToFront();
        
            // Adjust main content to use full width
            mainContent.style.marginLeft = 320; // Space for the hamburger button
            print("sidebar-collapsed");
        }
    }
}
