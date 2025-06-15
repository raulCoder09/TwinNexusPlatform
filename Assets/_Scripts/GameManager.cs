using System;
using _Scripts.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject arscaraOperative;
        [SerializeField] private GameObject arscaraTraining;
        [SerializeField] private GameObject robotKit1;
        [SerializeField] private GameObject robotKit2;
        
        
        
        private string _environmentSelected;
        private string _deviceUiSelected;
        private string _viewSelected;
        private string _deviceSelected;
        private string _modeSelected;
        private string _selectedModeUiName;
        
        private GameObject _arscaraOperativeOperativeInstance;
        private GameObject _arscaraTrainingInstance;
        private GameObject _robotKit1Instance;
        private GameObject _robotKit2Instance;
        

        public GameObject arscaraTrainingInstance
        {
            get => _arscaraTrainingInstance;
            set => _arscaraTrainingInstance = value;
        }

        public string selectedModeUiName
        {
            get => _selectedModeUiName;
            set => _selectedModeUiName = value;
        }

        internal string modeSelected
        {
            get => _modeSelected;
            set => _modeSelected = value;
        }


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

        

        internal GameObject arscaraOperativeInstance
        {
            get => _arscaraOperativeOperativeInstance;
            set => _arscaraOperativeOperativeInstance = value;
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
                            _arscaraOperativeOperativeInstance = Instantiate(arscaraOperative);
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
                    switch (_deviceSelected)
                    {
                        //todo ajustar interfaces solo para entrenamientos, son mas basicas que las intefaces de las
                     //operaciones ya que solo son introductorias y para conocer los equipos, de moemento se
                     //usare los mismas que operaciones
                        case "ARSCARAButton":
                            _arscaraTrainingInstance = Instantiate(arscaraTraining);
                            break;
                        case "RobotKit1Button":
                            _robotKit1Instance= Instantiate(robotKit1);
                            break;
                        case "RobotKit2Button":
                            _robotKit2Instance= Instantiate(robotKit2);
                            break;
                    }
                    break;
            }
        }
        
    }
}