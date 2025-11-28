using UnityEngine;

namespace _scripts.models.robotics
{
    public class RobotKinematics
    {
        private const float Deg2Rad = Mathf.PI / 180f;
        
        public enum CoordinateSystem
        {
            Unity,        // Y arriba, X-Z plano horizontal
            Traditional   // Z arriba, X-Y plano horizontal
        }
        
        private CoordinateSystem _coordinateSystem;

        public RobotKinematics(CoordinateSystem coordinateSystem = CoordinateSystem.Unity)
        {
            _coordinateSystem = coordinateSystem;
        }

        public void SetCoordinateSystem(CoordinateSystem system)
        {
            _coordinateSystem = system;
        }
        
        internal (float x, float y, float z) DirectKinematics(
            string method, 
            float linkLength1, 
            float linkLength2, 
            float q1, 
            float q2, 
            float p, 
            float offsetX=0.0f,
            float offsetY=0.0f,
            float offsetZ=0.0f,
            string units = "m")
        {
            if (units == "mm")
            {
                linkLength1 *= 1000.0f;
                linkLength2 *= 1000.0f;
                p *= 1000.0f;
            }
            
            float x = 0f, y = 0f, z = 0f;
            
            switch (method)
            {
                case "Geometric":
                {
                    var th1 = q1 * Deg2Rad;
                    var th2 = q2 * Deg2Rad;
                   
                    var cosComponent = linkLength1 * Mathf.Cos(th1) + linkLength2 * Mathf.Cos(th1 + th2);
                    var sinComponent = linkLength1 * Mathf.Sin(th1) + linkLength2 * Mathf.Sin(th1 + th2);
                    
                    if (_coordinateSystem == CoordinateSystem.Unity)
                    {
                        x = sinComponent+offsetX;
                        y = p+offsetY;
                        z = cosComponent+offsetZ;
                    }
                    else 
                    {
                        x = cosComponent+offsetX;
                        y = sinComponent+offsetY;
                        z = p+offsetZ;
                    }
                    
                    break;
                }
                case "HomogeneousTransformationMatrix":
                    // TODO: Implementar HTM
                    Debug.LogWarning("HomogeneousTransformationMatrix not implemented yet");
                    break;
                case "Denavit-Hartenberg":
                    // TODO: Implementar D-H
                    Debug.LogWarning("Denavit-Hartenberg not implemented yet");
                    break;
                case "Quaternions":
                    // TODO: Implementar Quaternions
                    Debug.LogWarning("Quaternions method not implemented yet");
                    break;
            }
        
            return (x, y, z);
        }
    }
}