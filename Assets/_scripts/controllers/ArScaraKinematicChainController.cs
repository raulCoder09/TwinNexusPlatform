using System;
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

        [SerializeField] private Method _method;
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
        private JogAndTeachController _jogAndTeachController;

        private enum Method
        {
            DirectGeometric,
            DirectHTM,
            DirectDH,
            DirectQuaternions
        }

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


        private void Awake()
        {
            _jogAndTeachController = GameObject.FindWithTag("VirtualEnvironmentArScara").GetComponent<JogAndTeachController>();
        }

        private void Start()
        {
            HideKinematicChain(); 
            _robotKinematics = new RobotKinematics(RobotKinematics.CoordinateSystem.Unity);

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
                _angleLinkB = _linkControllerB.GetNormalizedAngle();
                _angleLinkD = _linkControllerD.GetNormalizedAngle();
                switch (_method)
                {
                    case Method.DirectGeometric:
                        (_x,_y,_z)=_robotKinematics.DirectGeometric(_linkLengthC,
                            _linkLengthE,
                            _angleLinkB,
                            _angleLinkD,
                            _linkLengthB+_linkLengthD,
                            0.0f,
                            0.0f,
                            _linkLengthA);
                        break;
                    case Method.DirectHTM:
                        (_x,_y,_z)=_robotKinematics.DirectHTM(_linkLengthC,
                            _linkLengthE,
                            _angleLinkB,
                            _angleLinkD,
                            _linkLengthB+_linkLengthD,
                            0.0f,
                            0.0f,
                            _linkLengthA);
                        break;
                    case Method.DirectDH:
                        (_x,_y,_z)=_robotKinematics.DirectDH(_linkLengthC,
                            _linkLengthE,
                            _angleLinkB,
                            _angleLinkD,
                            _linkLengthB+_linkLengthD,
                            0.0f,
                            0.0f,
                            _linkLengthA);
                        break;
                    case Method.DirectQuaternions:
                        (_x,_y,_z)=_robotKinematics.DirectQuaternions(_linkLengthC,
                            _linkLengthE,
                            _angleLinkB,
                            _angleLinkD,
                            _linkLengthB+_linkLengthD,
                            0.0f,
                            0.0f,
                            _linkLengthA);
                        break;
                }

                _jogAndTeachController.XLabel.text = $"X: {1000*_currentPosition.x:F5} mm";
                _jogAndTeachController.YLabel.text = $"Y: {1000*_currentPosition.z:F5} mm";
                _jogAndTeachController.Q1Label.text = $"Q1: {_linkControllerB.GetNormalizedAngle():F5} deg";
                _jogAndTeachController.Q2Label.text = $"Q2: {_linkControllerD.GetNormalizedAngle():F5} deg";
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
        
        
        internal void HideKinematicChain()
        {
            LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>();
            foreach (var lr in lineRenderers)
            {
                lr.enabled = false;
            }
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (!(renderer is LineRenderer))
                {
                    renderer.enabled = false;
                }
            }
        }

        internal void ShowKinematicChain()
        {
            LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>();
            foreach (var lr in lineRenderers)
            {
                lr.enabled = true;
            }
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (!(renderer is LineRenderer))
                {
                    renderer.enabled = true;
                }
            }
        }
    }
}
