using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject arscara;
        
        private string _environmentSelected;
        private string _deviceUiSelected;

        private GameObject _arscaraInstance;

        internal GameObject arscaraInstance
        {
            get => _arscaraInstance;
            set => _arscaraInstance = value;
        }
        
        public string environmentSelected
        {
            get => _environmentSelected;
            set => _environmentSelected = value;
        }

        public string deviceUiSelected
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