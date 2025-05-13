using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject arscara;
        
        private string _environmentSelected;
        private string _deviceUiSelected;
        private string _viewSelected;

        public string viewSelected
        {
            get => _viewSelected;
            set => _viewSelected = value;
        }

        private GameObject _arscaraInstance;

        internal GameObject arscaraInstance
        {
            get => _arscaraInstance;
            set => _arscaraInstance = value;
        }
        
        internal string environmentSelected
        {
            get => _environmentSelected;
            set => _environmentSelected = value;
        }

        internal string deviceUiSelected
        {
            get => _deviceUiSelected;
            set => _deviceUiSelected = value;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "ArscaraScene")
            {
                 _arscaraInstance = Instantiate(arscara);
            }
        }
    }
}