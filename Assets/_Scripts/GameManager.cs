using System;
using _Scripts.Controller;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts
{
    public class GameManager : MonoBehaviour
    {
        private JogAndTeachController _jogAndTeachController;
        private ControlPanelController _controlPanelController;

        private string _environmentSelected;
        private string _arscaraUiSelected;

        public string environmentSelected
        {
            get => _environmentSelected;
            set => _environmentSelected = value;
        }

        public string arscaraUiSelected
        {
            get => _arscaraUiSelected;
            set => _arscaraUiSelected = value;
        }


        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
