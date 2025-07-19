using UnityEngine;

namespace _Scripts.Controllers.ArScaraRobotController
{
    public class ArScaraRobotOrchestrator : MonoBehaviour, IArScaraRobotOps
    {
        private bool _areMotorsOn = false;
        private GameObject _arScaraRobot; // Referencia al prefab ARSCARA
        private Transform _xl430W250T1; // Referencia al motor 1
        private Transform _xl430W250T2; // Referencia al motor 2

        public bool AreMotorsOn => _areMotorsOn;
        public bool IsRobotInstantiated => _arScaraRobot != null;

        private void Awake()
        {
            // Buscar el prefab ARSCARA en la escena
            InitializeRobotReferences();
        }

        private void InitializeRobotReferences()
        {
            _arScaraRobot = GameObject.Find("ARSCARA");
            if (_arScaraRobot == null)
            {
                Debug.LogWarning("[ArScaraRobotOrchestrator] ARSCARA prefab not found in scene.");
                return;
            }

            _xl430W250T1 = _arScaraRobot.transform.Find("XL430W250T1");
            _xl430W250T2 = _arScaraRobot.transform.Find("XL430W250T2");

            if (_xl430W250T1 == null || _xl430W250T2 == null)
            {
                Debug.LogWarning("[ArScaraRobotOrchestrator] One or both motors (XL430W250T1/XL430W250T2) not found in ARSCARA prefab.");
            }
        }

        public void TurnMotorsOn()
        {
            if (!IsRobotInstantiated)
            {
                Debug.LogError("[ArScaraRobotOrchestrator] Cannot turn motors on: ARSCARA prefab not instantiated.");
                return;
            }

            _areMotorsOn = true;
            Debug.Log("[ArScaraRobotOrchestrator] Motors turned ON.");
            // TODO: Aquí se añadiría lógica para enviar comandos a los motores reales o simular movimiento
        }

        public void TurnMotorsOff()
        {
            if (!IsRobotInstantiated)
            {
                Debug.LogError("[ArScaraRobotOrchestrator] Cannot turn motors off: ARSCARA prefab not instantiated.");
                return;
            }

            _areMotorsOn = false;
            Debug.Log("[ArScaraRobotOrchestrator] Motors turned OFF.");
            // TODO: Aquí se añadiría lógica para apagar motores reales o simular detención
        }
    }
}