using System.Collections;
using _scripts.models.robotics;
using UnityEngine;

namespace _scripts.controllers
{
    [RequireComponent(typeof(JogAndTeachController))]
    public class ArScaraVirtualController : MonoBehaviour
    {
        [SerializeField] private bool _activate=false;
        private JogAndTeachController _jogAndTeachController;
        private RobotKinematics  _kinematics;
        
        private Transform _axisLink1;
        private Transform _axisLink2;
        private void Awake()
        {
            _jogAndTeachController = GetComponent<JogAndTeachController>();
            _kinematics = new RobotKinematics();
        }
        private void Start()
        {
            _axisLink1 = transform.Find("ARSCARA/XL430W250T1/AxisLink1");
            _axisLink2 = transform.Find("ARSCARA/XL430W250T1/AxisLink1/Link1/XL430W250T2/AxisLink2");
        }


        IEnumerator Sleep(float delay)
        {
            yield return new WaitForSeconds(delay);
        }
    }
}