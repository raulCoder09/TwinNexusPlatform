using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controller
{
    public class SettingsController : MonoBehaviour
    {
        private IoTController _ioTController;
        private VisualElement _body;
        private Button _ioTButton;
        private void Awake()
        {
            GetUiComponents();
            RegisterEvents();
            FindObjects();
        }
        
        private void Start()
        {
            HideUi();
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
        }

        private void GetUiComponents()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _body = root.Q<VisualElement>("Body");
            _ioTButton = root.Q<Button>("IoTButton");
        }
    }
}
