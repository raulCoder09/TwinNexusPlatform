using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers
{
    public class RegisterController: MonoBehaviour
    {
        private UIDocument registerUIDocument;
        private VisualElement registerRoot;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            registerUIDocument = FindUIDocument("Register");
            if (registerUIDocument != null)
            {
                registerRoot = registerUIDocument.rootVisualElement;
            }
        }

        private void OnEnable()
        {
            ConfigureUIElements();
        }

        private void OnDisable()
        {
            HideUi();
        }

        private void Start()
        {
            if (registerRoot == null && registerUIDocument != null)
            {
                registerRoot = registerUIDocument.rootVisualElement;
            }
            HideUi();
        
        }
        private void Update()
        {
        
        }

        private void ConfigureUIElements()
        {
            if (registerRoot != null)
            {
            }
        }

        private void ShowUi()
        {
            if (registerRoot == null) return;
            registerRoot.style.display = DisplayStyle.Flex;
        }
        private void HideUi()
        {
            if (registerRoot == null) return;
            registerRoot.style.display = DisplayStyle.None;
        }
        
        private void QuitApplication()
        {
            Application.Quit();
        }

        private UIDocument FindUIDocument(string nameUiDocument)
        {
            var uiDocument = GameObject.Find(nameUiDocument)?.GetComponent<UIDocument>();
            return uiDocument;
        }
    }
}
