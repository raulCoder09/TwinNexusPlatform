using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject arscara;
        [SerializeField] private GameObject robotKit1;
        [SerializeField] private GameObject robotKit2;
        
        
        private string _environmentSelected;
        private string _deviceUiSelected;
        private string _viewSelected;
        private string _deviceSelected;

        private GameObject _robotKit1Instance;
        private GameObject _robotKit2Instance;
        internal string deviceSelected
        {
            get => _deviceSelected;
            set => _deviceSelected = value;
        }

        internal string viewSelected
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
            switch (scene.name)
            {
                case "Operations":
                    switch (_deviceSelected)
                    {
                        case "ARSCARAButton":
                            _arscaraInstance = Instantiate(arscara);
                            break;
                        case "RobotKit1Button":
                            _robotKit1Instance= Instantiate(robotKit1);
                            break;
                        case "RobotKit2Button":
                            _robotKit2Instance= Instantiate(robotKit2);
                            break;
                    }

                    break;
                case "Training":
                    print("entrenamiento");
                    break;
            }
        }
        
    }
}