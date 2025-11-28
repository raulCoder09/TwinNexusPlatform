using UnityEngine;

namespace _scripts.controllers
{
    public class ArScaraModel3DController:MonoBehaviour
    {
        private LinkController _linkController1;
        private LinkController _linkController2;

        private float _angleLink1;
        private float _angleLink2;

        internal LinkController linkController1
        {
            get => _linkController1;
            set => _linkController1 = value;
        }

        internal LinkController linkController2
        {
            get => _linkController2;
            set => _linkController2 = value;
        }

        internal float angleLink1
        {
            get => _angleLink1;
            set => _angleLink1 = value;
        }

        internal float angleLink2
        {
            get => _angleLink2;
            set => _angleLink2 = value;
        }

        private void Start()
        {
            _linkController1 = transform.Find("Base/XL430W250T1/AxisLink1").GetComponent<LinkController>();
            _linkController2 = transform.Find("Base/XL430W250T1/AxisLink1/Link1/XL430W250T2/AxisLink2").GetComponent<LinkController>();
            if (_linkController1 != null)
            {
                _linkController1.minimumAngle = -90f;
                _linkController1.maximumAngle = 90f;
            }
            else
            {
                Debug.LogError("LinkController1 not found!");
            }
            
            if (_linkController2 != null)
            {
                _linkController2.minimumAngle = -150f;
                _linkController2.maximumAngle = 150f;
            }
            else
            {
                Debug.LogError("LinkController2 not found!");
            }
        }

        private void Update()
        {
            if (_linkController1 != null && _linkController2 != null)
            {
                _angleLink1 = _linkController1.GetNormalizedAngle();
                _angleLink2 = _linkController2.GetNormalizedAngle();
            }
        }
        
    }
    
}