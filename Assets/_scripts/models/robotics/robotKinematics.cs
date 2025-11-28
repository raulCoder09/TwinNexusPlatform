using UnityEngine;

namespace _scripts.models.robotics
{
    public class RobotKinematics
    {
        private const float Deg2Rad = Mathf.PI / 180f;
        private bool _traditional=false;
        
        internal (float x, float y, float z) DirectKinematics(string method, float linkLength1, float linkLength2, float q1, float q2, float p, string units = "m")
        {
            if (units == "mm")
            {
                linkLength1 *= 1000.0f;
                linkLength2 *= 1000.0f;
            }
            
            float x = 0f, y = 0f, z = 0f;
            
            switch (method)
            {
                case "Geometric":
                {
                    if (_traditional)
                    {
                        //logica para forma tradicional
                    }
                    else
                    {
                        // logica para unity
                        var th1 = q1 * Deg2Rad;
                        var th2 = q2 * Deg2Rad;
                    
                        var zCalc = linkLength1 * Mathf.Cos(th1) + linkLength2 * Mathf.Cos(th1 + th2);
                        var xCalc = linkLength1 * Mathf.Sin(th1) + linkLength2 * Mathf.Sin(th1 + th2);
                    
                        x = xCalc; 
                        y = p;     
                        z = zCalc+0.035375f;
                    }

                    
                    
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