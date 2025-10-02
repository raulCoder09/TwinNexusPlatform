using UnityEngine;

namespace _scripts.models.robotics
{
    public class RobotKinematics
    {
        private float _l1 = 0.10f;
        private float _l2 = 0.10f;
        
        private const float Deg2Rad = Mathf.PI / 180f;
        
        
        internal (float x, float y, float z) DirectKinematics(string method, float q1, float q2)
        {
            float x = 0f, y = 0f, z = 0f;
            switch (method)
            {
                case "Geometric":
                {
                    float th1 = q1 * Deg2Rad;
                    float th2 = q2 * Deg2Rad;
                    x = _l1 * Mathf.Cos(th1) + _l2 * Mathf.Cos(th1 + th2);
                    y = _l1 * Mathf.Sin(th1) + _l2 * Mathf.Sin(th1 + th2);
                    break;
                }
                case "HomogeneousTransformationMatrix": break;
                case "Denavit-Hartenberg": break;
                case "Quaternions": break;
            }

            return (x, y, z);
        }
        
    }
}
