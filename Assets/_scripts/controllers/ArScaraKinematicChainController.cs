using _scripts.models.robotics;
using UnityEngine;

namespace _scripts.controllers
{
    public class ArScaraKinematicChainController : MonoBehaviour
    {
        [SerializeField] private GameObject _node1;
        [SerializeField] private GameObject _linkA;
        [SerializeField] private GameObject _node2;
        [SerializeField] private GameObject _linkB;
        [SerializeField] private GameObject _node3;
        [SerializeField] private GameObject _linkC;
        [SerializeField] private GameObject _node4;
        [SerializeField] private GameObject _linkD;
        [SerializeField] private GameObject _node5;
        [SerializeField] private GameObject _linkE;
        [SerializeField] private GameObject _node6;

        private float _q1;
        private float _q2;

        private Vector3 _currentPosition;

        private float _linkLengthA;
        private float _linkLengthB;
        private float _linkLengthC;
        private float _linkLengthD;
        private float _linkLengthE;
        
        private float _x;
        private float _y;
        private float _z;

        private RobotKinematics _robotKinematics;
        private LinkController _linkControllerB;
        private LinkController _linkControllerD;
        private void Start()
        {
            _robotKinematics = new RobotKinematics();
            _linkLengthA = GetLinkLength(_linkA);
            _linkLengthB = GetLinkLength(_linkB);
            _linkLengthC = GetLinkLength(_linkC);
            _linkLengthD = GetLinkLength(_linkD);
            _linkLengthE = GetLinkLength(_linkE);
            
            _linkControllerB = transform.Find("Node1/LinkA/Node2/LinkB").GetComponent<LinkController>();
            _linkControllerD = transform.Find("Node1/LinkA/Node2/LinkB/Node3/LinkC/Node4/LinkD").GetComponent<LinkController>();
            
            if (_linkControllerB != null)
            {
                _linkControllerB.minimumAngle = -90f;
                _linkControllerB.maximumAngle = 90f;
            }
            else
            {
                Debug.LogError("Link 3 not found!");
            }
            
            if (_linkControllerD != null)
            {
                _linkControllerD.minimumAngle = -150f;
                _linkControllerD.maximumAngle = 150f;
            }
            else
            {
                Debug.LogError("Link 5 not found!");
            }

        }

        private void Update()
        {
            _currentPosition = _node6.transform.position;
            if (_linkControllerB != null && _linkControllerD != null)
            {
                var angleLinkB = _linkControllerB.GetNormalizedAngle(); //este angulo entraran en q1
                var angleLinkD = _linkControllerD.GetNormalizedAngle(); //este angulo entraran en q1
                print($"q1: {angleLinkB:F2} deg");
                print($"q1: {angleLinkD:F2} deg");
            }
            
            
            // (_x,_y,_z)=_robotKinematics.DirectKinematics("Geometric",_linkLengthC,_linkLengthE,_q1,_q2); //ver donde llamamos este metodo
        }
        private float GetLinkLength(GameObject link)
        {
            var lineRenderer = link.GetComponent<LineRenderer>();
            if (lineRenderer == null || lineRenderer.positionCount < 2)
                return 0f;

            var startingPoint = lineRenderer.GetPosition(0);
            var endingPoint = lineRenderer.GetPosition(1);

            return Vector3.Distance(startingPoint, endingPoint);
        }
    }
}
