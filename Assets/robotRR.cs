using UnityEngine;

public class RobotRr : MonoBehaviour
{
    [SerializeField] private GameObject _pivot1;
    [SerializeField] private GameObject _link1;
    [SerializeField] private GameObject _pivot2;
    [SerializeField] private GameObject _link2;
    [SerializeField] private GameObject _endEffector;
    

    [Header("Joint Angles (Degrees)")]
    [SerializeField] private float _q1;
    [SerializeField] private float _q2;

    private float _l1;
    private float _l2;

    private void Start()
    {
        _l1 = GetLinkLength(_link1);
        _l2 = GetLinkLength(_link2);

        Debug.Log("L1 = " + _l1);
        Debug.Log("L2 = " + _l2);
    }

    private void Update()
    {
        Vector3 eePos = _endEffector.transform.position;
        float q1 = _q1 * Mathf.Deg2Rad;
        float q2 = _q2 * Mathf.Deg2Rad;
        
        float x = _l1 * Mathf.Cos(q1) + _l2 * Mathf.Cos(q1 + q2);
        float y = _l1 * Mathf.Sin(q1) + _l2 * Mathf.Sin(q1 + q2);
        
        Debug.Log("EE REAL (Unity): " + eePos);
        Debug.Log("EE CD (Cinemática Directa): (" + x + ", " + y + ")");
    }

    private float GetLinkLength(GameObject link)
    {
        LineRenderer lr = link.GetComponent<LineRenderer>();
        if (lr == null || lr.positionCount < 2)
            return 0f;

        Vector3 p0 = lr.GetPosition(0);
        Vector3 p1 = lr.GetPosition(1);

        return Vector3.Distance(p0, p1);
    }
}