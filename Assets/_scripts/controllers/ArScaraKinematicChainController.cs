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

        private float _angleLinkB;
        private float _angleLinkD;

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


        internal LinkController linkControllerB
        {
            get => _linkControllerB;
            set => _linkControllerB = value;
        }

        internal LinkController linkControllerD
        {
            get => _linkControllerD;
            set => _linkControllerD = value;
        }


        private void Start()
        {
            _robotKinematics = new RobotKinematics();
            _linkLengthA = GetLinkLength(_linkA);
            _linkLengthB = GetLinkLength(_linkB);
            _linkLengthC = GetLinkLength(_linkC);
            _linkLengthD = GetLinkLength(_linkD);
            _linkLengthE = GetLinkLength(_linkE);
            print(_linkLengthA);
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
                _angleLinkB = _linkControllerB.GetNormalizedAngle();
                _angleLinkD = _linkControllerD.GetNormalizedAngle();
                (_x,_y,_z)=_robotKinematics.DirectKinematics("Geometric",_linkLengthC,_linkLengthE,_angleLinkB,_angleLinkD,_linkLengthB+_linkLengthD);
                print($"End effector - X: {_currentPosition.x:F5}, Y: {_currentPosition.y:F5}, Z: {_currentPosition.z:F5}");
                print($"Compute - X: {_x:F5}, Y: {_y:F5}, Z: {_z:F5}");
            }
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
