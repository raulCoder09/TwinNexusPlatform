using UnityEngine;
using _scripts.Models;
namespace _scripts.controllers
{
    [RequireComponent(typeof(JogAndTeachController))]
    public class ArScaraVirtualController : MonoBehaviour
    {
        private JogAndTeachController _jogAndTeachController;
        private ScaraRobotKinematics  _kinematics;
        
        private Transform _axisLink1;
        private Transform _axisLink2;
        private void Awake()
        {
            _jogAndTeachController = GetComponent<JogAndTeachController>();
            _kinematics = new ScaraRobotKinematics();
        }
        private void Start()
        {
            _axisLink1 = transform.Find("ARSCARA/XL430W250T1/AxisLink1");
            _axisLink2 = transform.Find("ARSCARA/XL430W250T1/AxisLink1/Link1/XL430W250T2/AxisLink2");
        }

        private void Update()
        {
            var q1=_jogAndTeachController.J1Rotation;
            var q2 =_jogAndTeachController.J2Rotation;
            var (x, y, z) = _kinematics.DirectKinematics("Geometric", q1, q2);
            
            print($"Angles, Q1: {q1} deg, Q2: {q2} deg, Coordinates, X:{x:F3} m Y:{y:F3} m Z:{z:F3} m");
            if (_axisLink1 != null)
                _axisLink1.localRotation = Quaternion.Euler(0f, q1, 0f);

            if (_axisLink2 != null)
                _axisLink2.localRotation = Quaternion.Euler(0f, q2, 0f);
        }
    }
}