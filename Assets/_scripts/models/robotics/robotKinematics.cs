using UnityEngine;

namespace _scripts.models.robotics
{
    public class RobotKinematics
    {
        private const float Deg2Rad = Mathf.PI / 180f;
        
        public enum CoordinateSystem
        {
            Unity,      
            Traditional  
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
        
        internal (float x, float y, float z) DirectGeometric(
            float linkLength1, 
            float linkLength2, 
            float q1, 
            float q2, 
            float p, 
            float offsetX=0.0f,
            float offsetY=0.0f,
            float offsetZ=0.0f
            )
        {
            float x = 0f, y = 0f, z = 0f;
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

            return (x, y, z);
        }

        internal (float x, float y, float z) DirectHTM(
            float linkLength1, 
            float linkLength2, 
            float q1, 
            float q2, 
            float p, 
            float offsetX=0.0f,
            float offsetY=0.0f,
            float offsetZ=0.0f
            )
        {
            float x = 0f, y = 0f, z = 0f;
            var th1 = q1 * Deg2Rad;
            var th2 = q2 * Deg2Rad;

                                
            var c1 = Mathf.Cos(th1);
            var s1 = Mathf.Sin(th1);
            
            var T01 = new float[4,4];
            T01[0,0] = c1;   T01[0,1] = -s1;  T01[0,2] = 0f; T01[0,3] = linkLength1 * c1;
            T01[1,0] = s1;   T01[1,1] =  c1;  T01[1,2] = 0f; T01[1,3] = linkLength1 * s1;
            T01[2,0] = 0f;   T01[2,1] = 0f;    T01[2,2] = 1f; T01[2,3] = 0f;
            T01[3,0] = 0f;   T01[3,1] = 0f;    T01[3,2] = 0f; T01[3,3] = 1f;

            var c2 = Mathf.Cos(th2);
            var s2 = Mathf.Sin(th2);

            var T12 = new float[4,4];
            T12[0,0] = c2;   T12[0,1] = -s2;  T12[0,2] = 0f; T12[0,3] = linkLength2 * c2;
            T12[1,0] = s2;   T12[1,1] =  c2;  T12[1,2] = 0f; T12[1,3] = linkLength2 * s2;
            T12[2,0] = 0f;   T12[2,1] = 0f;    T12[2,2] = 1f; T12[2,3] = 0f;
            T12[3,0] = 0f;   T12[3,1] = 0f;    T12[3,2] = 0f; T12[3,3] = 1f;
            
            var T02 = new float[4,4];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    var sum = 0f;
                    for (int k = 0; k < 4; k++)
                        sum += T01[i,k] * T12[k,j];
                    T02[i,j] = sum;
                }
            
            var cosComponent = T02[0,3];
            var sinComponent = T02[1,3];
            
            if (_coordinateSystem == CoordinateSystem.Unity)
            {
                x = sinComponent + offsetX;
                y = p + offsetY;   
                z = cosComponent + offsetZ;  
            }
            else
            {
                x = cosComponent + offsetX;
                y = sinComponent + offsetY;
                z = p + offsetZ;
            }
            
            return (x, y, z);
        }

        internal (float x, float y, float z) DirectDH(
            float linkLength1,
            float linkLength2,
            float q1,
            float q2,
            float p,
            float offsetX = 0.0f,
            float offsetY = 0.0f,
            float offsetZ = 0.0f
            )
        {
            var th1 = q1 * Deg2Rad;
            var th2 = q2 * Deg2Rad;
            float x = 0f, y = 0f, z = 0f;
            float[,] BuildA(float a, float alpha, float d, float theta)
            {
                float ca = Mathf.Cos(alpha);
                float sa = Mathf.Sin(alpha);
                float ct = Mathf.Cos(theta);
                float st = Mathf.Sin(theta);

                var A = new float[4,4];
                A[0,0] = ct;    A[0,1] = -st * ca;   A[0,2] = st * sa;    A[0,3] = a * ct;
                A[1,0] = st;    A[1,1] = ct * ca;    A[1,2] = -ct * sa;   A[1,3] = a * st;
                A[2,0] = 0f;    A[2,1] = sa;         A[2,2] = ca;         A[2,3] = d;
                A[3,0] = 0f;    A[3,1] = 0f;         A[3,2] = 0f;         A[3,3] = 1f;
                return A;
            }
            
            var A1 = BuildA(linkLength1, 0f, 0f, th1);
            var A2 = BuildA(linkLength2, 0f, 0f, th2);
            
            var T02 = new float[4,4];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    float sum = 0f;
                    for (int k = 0; k < 4; k++)
                        sum += A1[i,k] * A2[k,j];
                    T02[i,j] = sum;
                }
            
            var cosComponent = T02[0,3];
            var sinComponent = T02[1,3]; 
            
            if (_coordinateSystem == CoordinateSystem.Unity)
            {
                x = sinComponent + offsetX;   
                y = p + offsetY;           
                z = cosComponent + offsetZ;  
            }
            else
            {
                x = cosComponent + offsetX;
                y = sinComponent + offsetY;
                z = p + offsetZ;
            }
            return (x, y, z);
        }

        internal (float x, float y, float z) DirectQuaternions(
            float linkLength1,
            float linkLength2,
            float q1,
            float q2,
            float p,
            float offsetX = 0.0f,
            float offsetY = 0.0f,
            float offsetZ = 0.0f
            )
        {
            float x = 0f, y = 0f, z = 0f;
            Vector3 baseDir;
            Vector3 rotAxis; 
            if (_coordinateSystem == CoordinateSystem.Unity)
            {
                baseDir = Vector3.forward; 
                rotAxis = Vector3.up;       
            }
            else
            {
                baseDir = Vector3.right;
                rotAxis = Vector3.forward;
            }
            var q1Quat = Quaternion.AngleAxis(q1, rotAxis);
            var q2Quat = Quaternion.AngleAxis(q2, rotAxis);
            
            var p1 = q1Quat * (baseDir * linkLength1);
            var p2 = (q1Quat * q2Quat) * (baseDir * linkLength2);    

            var pos = p1 + p2; 
            var endOrientation = q1Quat * q2Quat;
            if (_coordinateSystem == CoordinateSystem.Unity)
            {
                x = pos.x + offsetX;
                y = p + offsetY;
                z = pos.z + offsetZ;
            }
            else
            {
                x = pos.x + offsetX;
                y = pos.y + offsetY;
                z = p + offsetZ;
            }
            return (x, y, z);
                    
        }


    }
}