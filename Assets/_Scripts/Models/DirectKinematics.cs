using System;
using System.Text.RegularExpressions;
using _Scripts.Controller;
using UnityEngine;

namespace _Scripts.Models
{
    public class DirectKinematics:MonoBehaviour
    {
        private GameManager _gameManager;
        private ConcreteController _concreteController;

        private void Awake()
        {
            FindObjects();
        }
        private void Start()
        {
            
            FindEnvironmentComponents();
        }
        

        private void FindObjects()
        {
            _gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        }
        
        private void FindEnvironmentComponents()
        {
            _concreteController=_gameManager.arscaraTrainingInstance.transform.Find("Concrete").GetComponent<ConcreteController>();
        }


        public void GeometricMethod()
        {

            // // Calcular la posición del efector final en el plano XY
            // double x = L1 * Math.Cos(theta1) + L2 * Math.Cos(theta1 + theta2);
            // double y = L1 * Math.Sin(theta1) + L2 * Math.Sin(theta1 + theta2);
            // double z = -d3; // Asumimos que z=0 cuando d3=0, y baja cuando d3 aumenta
            //
            // // Calcular la orientación del efector
            // double phi = theta1 + theta2 + theta4;
            //
            // // // Devolver el resultado
            // // return new KinematicResult(x, y, z, phi);
        }

        private void HomogeneousTransformationMatrixMethod(){}

        private void DenavitHartenbergAlgorithm(){}

        private void QuaternialMethod(){}
    }
}