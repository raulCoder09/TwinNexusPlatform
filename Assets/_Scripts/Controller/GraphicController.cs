using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class GraphicController : MonoBehaviour
    {
        private VisualElement _body;
        private DropdownField _menuLevelMedara;
        private void Awake()
        {
            GetUiComponents();
            RegisterEvents();
            FindObjects();
        }

        private void FindObjects()
        {
            throw new System.NotImplementedException();
        }

        private void RegisterEvents()
        {
            throw new System.NotImplementedException();
        }

        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _menuLevelMedara= root.Q<DropdownField>("MenuLevelMedaraDropdownField");
        }

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
