using System;
using UnityEngine;

namespace _Scripts.Controllers.ArScaraRobotController
{
    /// <summary>
    /// Sistema de coordenadas para robot ARSCARA
    /// Maneja conversiones entre coordenadas de joints y cartesianas
    /// Tracking de posición actual del end-effector
    /// </summary>
    public class ArScaraCoordinateSystem : MonoBehaviour
    {
        #region Private Fields
        private ArScaraRobotManager _robotManager;
        private bool _isInitialized = false;
        
        // Parámetros del robot SCARA
        private float _link1Length;
        private float _link2Length;
        private float _baseHeight;
        
        // Posición cartesiana actual (X, Y, Z, U)
        private Vector4 _currentCartesianPosition;
        
        // Workspace limits (calculados basándose en parámetros del robot)
        private float _maxReach;
        private float _minReach;
        private Vector2 _workspaceXLimits;
        private Vector2 _workspaceYLimits;
        private Vector2 _workspaceZLimits;
        
        // Referencias de transforms para cálculos
        private Transform _robotBase;
        private Transform _endEffectorTransform;
        #endregion

        #region Events
        public static event Action<Vector4> OnCartesianPositionChanged; // X, Y, Z, U
        public static event Action<Vector3> OnWorkspaceViolation;        // posición fuera de workspace
        public static event Action<bool> OnReachabilityChanged;          // si la posición es alcanzable
        #endregion

        #region Initialization
        /// <summary>
        /// Inicializa el sistema de coordenadas
        /// </summary>
        public void Initialize(ArScaraRobotManager robotManager)
        {
            _robotManager = robotManager ?? throw new ArgumentNullException(nameof(robotManager));
            
            try
            {
                // Obtener parámetros del robot
                _link1Length = _robotManager.Link1Length;
                _link2Length = _robotManager.Link2Length;
                _baseHeight = _robotManager.BaseHeight;
                
                // Obtener referencias de transforms
                _robotBase = _robotManager.RobotBase;
                _endEffectorTransform = _robotManager.EndEffectorTransform;
                
                // Calcular límites del workspace
                CalculateWorkspaceLimits();
                
                // Calcular posición inicial
                UpdateCurrentCartesianPosition();
                
                _isInitialized = true;
                Debug.Log("[ArScaraCoordinateSystem] Coordinate system initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArScaraCoordinateSystem] Initialization failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Calcula los límites del workspace basándose en parámetros del robot
        /// </summary>
        private void CalculateWorkspaceLimits()
        {
            // Para robot SCARA:
            _maxReach = _link1Length + _link2Length;  // Alcance máximo
            _minReach = Mathf.Abs(_link1Length - _link2Length); // Alcance mínimo
            
            // Límites cartesianos (círculo)
            _workspaceXLimits = new Vector2(-_maxReach, _maxReach);
            _workspaceYLimits = new Vector2(-_maxReach, _maxReach);
            
            // Límites verticales
            Vector2 j3Limits = _robotManager.GetJointLimits(2);
            _workspaceZLimits = new Vector2(_baseHeight + j3Limits.x, _baseHeight + j3Limits.y);
            
            Debug.Log($"[ArScaraCoordinateSystem] Workspace calculated - " +
                     $"Reach: {_minReach:F3}m to {_maxReach:F3}m, " +
                     $"Z: {_workspaceZLimits.x:F3}m to {_workspaceZLimits.y:F3}m");
        }
        #endregion

        #region Update Loop
        private void Update()
        {
            if (!_isInitialized) return;
            
            UpdateCurrentCartesianPosition();
        }

        /// <summary>
        /// Actualiza la posición cartesiana actual basándose en joints
        /// </summary>
        private void UpdateCurrentCartesianPosition()
        {
            if (_robotManager?.JointController == null) return;
            
            // Obtener ángulos actuales de joints
            float[] jointAngles = _robotManager.JointController.GetCurrentJointAngles();
            
            // Calcular posición cartesiana usando cinemática directa
            Vector4 newPosition = CalculateForwardKinematics(jointAngles);
            
            // Verificar si cambió significativamente
            if (Vector4.Distance(newPosition, _currentCartesianPosition) > 0.001f)
            {
                _currentCartesianPosition = newPosition;
                OnCartesianPositionChanged?.Invoke(_currentCartesianPosition);
            }
        }
        #endregion

        #region Forward Kinematics
        /// <summary>
        /// Calcula la cinemática directa para robot SCARA
        /// </summary>
        /// <param name="jointAngles">Array con ángulos [J1, J2, J3, J4]</param>
        /// <returns>Vector4 con posición cartesiana (X, Y, Z, U)</returns>
        public Vector4 CalculateForwardKinematics(float[] jointAngles)
        {
            if (jointAngles == null || jointAngles.Length != 4)
            {
                Debug.LogError("[ArScaraCoordinateSystem] Invalid joint angles array");
                return Vector4.zero;
            }
            
            // Convertir ángulos a radianes
            float j1Rad = jointAngles[0] * Mathf.Deg2Rad;
            float j2Rad = jointAngles[1] * Mathf.Deg2Rad;
            float j3Linear = jointAngles[2]; // Ya está en unidades lineales
            float j4Angle = jointAngles[3];  // Rotación del end-effector
            
            // Cinemática directa SCARA
            // Posición del extremo del Link1
            float x1 = _link1Length * Mathf.Cos(j1Rad);
            float y1 = _link1Length * Mathf.Sin(j1Rad);
            
            // Posición del end-effector
            float x = x1 + _link2Length * Mathf.Cos(j1Rad + j2Rad);
            float y = y1 + _link2Length * Mathf.Sin(j1Rad + j2Rad);
            float z = _baseHeight + j3Linear;
            float u = j4Angle; // Orientación del end-effector
            
            return new Vector4(x, y, z, u);
        }

        /// <summary>
        /// Obtiene la posición cartesiana actual
        /// </summary>
        public Vector4 GetCurrentCartesianPosition()
        {
            return _currentCartesianPosition;
        }

        /// <summary>
        /// Obtiene la posición del end-effector desde su transform
        /// </summary>
        public Vector3 GetEndEffectorWorldPosition()
        {
            if (_endEffectorTransform == null) return Vector3.zero;
            return _endEffectorTransform.position;
        }
        #endregion

        #region Workspace Validation
        /// <summary>
        /// Verifica si una posición está dentro del workspace
        /// </summary>
        public bool IsPositionReachable(Vector3 targetPosition)
        {
            // Calcular distancia desde el origen
            float distance = Mathf.Sqrt(targetPosition.x * targetPosition.x + targetPosition.y * targetPosition.y);
            
            // Verificar alcance
            if (distance < _minReach || distance > _maxReach)
                return false;
            
            // Verificar límites Z
            if (targetPosition.z < _workspaceZLimits.x || targetPosition.z > _workspaceZLimits.y)
                return false;
            
            return true;
        }

        /// <summary>
        /// Verifica si una posición está dentro del workspace (Vector4 con orientación)
        /// </summary>
        public bool IsPositionReachable(Vector4 targetPosition)
        {
            Vector3 pos = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);
            return IsPositionReachable(pos);
        }

        /// <summary>
        /// Clampea una posición a los límites del workspace
        /// </summary>
        public Vector3 ClampToWorkspace(Vector3 position)
        {
            Vector3 clampedPos = position;
            
            // Clampear a alcance circular
            float distance = Mathf.Sqrt(position.x * position.x + position.y * position.y);
            if (distance > _maxReach)
            {
                float scale = _maxReach / distance;
                clampedPos.x *= scale;
                clampedPos.y *= scale;
            }
            else if (distance < _minReach && distance > 0.001f)
            {
                float scale = _minReach / distance;
                clampedPos.x *= scale;
                clampedPos.y *= scale;
            }
            
            // Clampear Z
            clampedPos.z = Mathf.Clamp(clampedPos.z, _workspaceZLimits.x, _workspaceZLimits.y);
            
            return clampedPos;
        }

        /// <summary>
        /// Valida posición y notifica si hay violación del workspace
        /// </summary>
        public bool ValidateAndNotifyPosition(Vector3 targetPosition)
        {
            bool isReachable = IsPositionReachable(targetPosition);
            
            if (!isReachable)
            {
                OnWorkspaceViolation?.Invoke(targetPosition);
                Debug.LogWarning($"[ArScaraCoordinateSystem] Position outside workspace: {targetPosition}");
            }
            
            OnReachabilityChanged?.Invoke(isReachable);
            return isReachable;
        }
        #endregion

        #region Coordinate Transformations
        /// <summary>
        /// Convierte de coordenadas del robot a coordenadas mundiales
        /// </summary>
        public Vector3 RobotToWorldCoordinates(Vector3 robotCoords)
        {
            if (_robotBase == null) return robotCoords;
            
            // Aplicar transformación de la base del robot
            return _robotBase.TransformPoint(robotCoords);
        }

        /// <summary>
        /// Convierte de coordenadas mundiales a coordenadas del robot
        /// </summary>
        public Vector3 WorldToRobotCoordinates(Vector3 worldCoords)
        {
            if (_robotBase == null) return worldCoords;
            
            // Aplicar transformación inversa de la base del robot
            return _robotBase.InverseTransformPoint(worldCoords);
        }

        /// <summary>
        /// Convierte posición cartesiana (X,Y,Z) a coordenadas relativas al robot
        /// </summary>
        public Vector3 CartesianToRobotSpace(Vector3 cartesianPos)
        {
            // Para SCARA, las coordenadas cartesianas ya están en espacio del robot
            // Solo ajustar la Z relativa a la base
            Vector3 robotPos = cartesianPos;
            robotPos.z -= _baseHeight;
            return robotPos;
        }

        /// <summary>
        /// Convierte coordenadas del robot a posición cartesiana absoluta
        /// </summary>
        public Vector3 RobotSpaceToCartesian(Vector3 robotPos)
        {
            Vector3 cartesianPos = robotPos;
            cartesianPos.z += _baseHeight;
            return cartesianPos;
        }
        #endregion

        #region Distance and Geometry Calculations
        /// <summary>
        /// Calcula la distancia del end-effector a una posición target
        /// </summary>
        public float DistanceToTarget(Vector3 targetPosition)
        {
            Vector3 currentPos = new Vector3(_currentCartesianPosition.x, 
                                           _currentCartesianPosition.y, 
                                           _currentCartesianPosition.z);
            return Vector3.Distance(currentPos, targetPosition);
        }

        /// <summary>
        /// Calcula el ángulo requerido para alcanzar una posición
        /// </summary>
        public float AngleToTarget(Vector3 targetPosition)
        {
            return Mathf.Atan2(targetPosition.y, targetPosition.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Calcula la distancia radial desde el origen
        /// </summary>
        public float RadialDistanceFromOrigin(Vector3 position)
        {
            return Mathf.Sqrt(position.x * position.x + position.y * position.y);
        }

        /// <summary>
        /// Verifica si dos posiciones están dentro de una tolerancia
        /// </summary>
        public bool IsWithinTolerance(Vector3 pos1, Vector3 pos2, float tolerance = 0.001f)
        {
            return Vector3.Distance(pos1, pos2) <= tolerance;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Posición cartesiana actual del end-effector
        /// </summary>
        public Vector4 CurrentCartesianPosition => _currentCartesianPosition;

        /// <summary>
        /// Alcance máximo del robot
        /// </summary>
        public float MaxReach => _maxReach;

        /// <summary>
        /// Alcance mínimo del robot
        /// </summary>
        public float MinReach => _minReach;

        /// <summary>
        /// Límites del workspace en X
        /// </summary>
        public Vector2 WorkspaceXLimits => _workspaceXLimits;

        /// <summary>
        /// Límites del workspace en Y
        /// </summary>
        public Vector2 WorkspaceYLimits => _workspaceYLimits;

        /// <summary>
        /// Límites del workspace en Z
        /// </summary>
        public Vector2 WorkspaceZLimits => _workspaceZLimits;

        /// <summary>
        /// Longitud del primer eslabón
        /// </summary>
        public float Link1Length => _link1Length;

        /// <summary>
        /// Longitud del segundo eslabón
        /// </summary>
        public float Link2Length => _link2Length;

        /// <summary>
        /// Altura de la base
        /// </summary>
        public float BaseHeight => _baseHeight;

        /// <summary>
        /// Si el sistema está inicializado
        /// </summary>
        public bool IsInitialized => _isInitialized;
        #endregion

        #region Debug and Utilities
        [ContextMenu("Debug Coordinate System")]
        public void DebugCoordinateSystem()
        {
            if (!_isInitialized)
            {
                Debug.Log("Coordinate system not initialized");
                return;
            }

            Debug.Log("=== Coordinate System Debug ===");
            Debug.Log($"Robot Parameters:");
            Debug.Log($"  Link1 Length: {_link1Length:F3}m");
            Debug.Log($"  Link2 Length: {_link2Length:F3}m");
            Debug.Log($"  Base Height: {_baseHeight:F3}m");
            
            Debug.Log($"Workspace Limits:");
            Debug.Log($"  Reach: {_minReach:F3}m to {_maxReach:F3}m");
            Debug.Log($"  X: {_workspaceXLimits.x:F3}m to {_workspaceXLimits.y:F3}m");
            Debug.Log($"  Y: {_workspaceYLimits.x:F3}m to {_workspaceYLimits.y:F3}m");
            Debug.Log($"  Z: {_workspaceZLimits.x:F3}m to {_workspaceZLimits.y:F3}m");
            
            Debug.Log($"Current Position:");
            Debug.Log($"  Cartesian: X={_currentCartesianPosition.x:F3}, Y={_currentCartesianPosition.y:F3}, Z={_currentCartesianPosition.z:F3}, U={_currentCartesianPosition.w:F1}°");
            
            if (_robotManager?.JointController != null)
            {
                float[] joints = _robotManager.JointController.GetCurrentJointAngles();
                Debug.Log($"  Joints: J1={joints[0]:F1}°, J2={joints[1]:F1}°, J3={joints[2]:F3}m, J4={joints[3]:F1}°");
            }
            
            Vector3 currentPos3D = new Vector3(_currentCartesianPosition.x, _currentCartesianPosition.y, _currentCartesianPosition.z);
            Debug.Log($"  Reachable: {IsPositionReachable(currentPos3D)}");
            Debug.Log($"  Distance from origin: {RadialDistanceFromOrigin(currentPos3D):F3}m");
        }

        [ContextMenu("Test Forward Kinematics")]
        public void TestForwardKinematics()
        {
            if (!_isInitialized) return;

            // Test con diferentes configuraciones de joints
            float[][] testConfigurations = {
                new float[] { 0, 0, 0, 0 },          // Home
                new float[] { 45, 0, 0, 0 },         // J1 a 45°
                new float[] { 0, 45, 0, 0 },         // J2 a 45°
                new float[] { 45, 45, 0, 0 },        // J1 y J2 a 45°
                new float[] { 90, -90, 0.1f, 180 },  // Configuración extendida
            };

            Debug.Log("=== Forward Kinematics Test ===");
            for (int i = 0; i < testConfigurations.Length; i++)
            {
                Vector4 result = CalculateForwardKinematics(testConfigurations[i]);
                Debug.Log($"Config {i + 1}: J1={testConfigurations[i][0]}°, J2={testConfigurations[i][1]}°, J3={testConfigurations[i][2]}m, J4={testConfigurations[i][3]}°");
                Debug.Log($"  Result: X={result.x:F3}, Y={result.y:F3}, Z={result.z:F3}, U={result.w:F1}°");
                Debug.Log($"  Reachable: {IsPositionReachable(new Vector3(result.x, result.y, result.z))}");
            }
        }

        [ContextMenu("Test Workspace Limits")]
        public void TestWorkspaceLimits()
        {
            Debug.Log("=== Workspace Limits Test ===");
            
            Vector3[] testPositions = {
                Vector3.zero,                           // Origen
                new Vector3(_maxReach, 0, _baseHeight), // Alcance máximo
                new Vector3(_minReach, 0, _baseHeight), // Alcance mínimo
                new Vector3(0, 0, _workspaceZLimits.y), // Z máximo
                new Vector3(0, 0, _workspaceZLimits.x), // Z mínimo
                new Vector3(_maxReach + 0.1f, 0, _baseHeight), // Fuera de alcance
            };

            for (int i = 0; i < testPositions.Length; i++)
            {
                bool reachable = IsPositionReachable(testPositions[i]);
                Vector3 clamped = ClampToWorkspace(testPositions[i]);
                float distance = RadialDistanceFromOrigin(testPositions[i]);
                
                Debug.Log($"Position {i + 1}: {testPositions[i]}");
                Debug.Log($"  Distance: {distance:F3}m, Reachable: {reachable}");
                Debug.Log($"  Clamped: {clamped}");
            }
        }

        /// <summary>
        /// Recalcula la posición actual forzadamente
        /// </summary>
        [ContextMenu("Force Update Position")]
        public void ForceUpdatePosition()
        {
            UpdateCurrentCartesianPosition();
            Debug.Log($"Position updated: {_currentCartesianPosition}");
        }

        /// <summary>
        /// Obtiene información completa del estado actual
        /// </summary>
        public string GetCurrentStateInfo()
        {
            if (!_isInitialized) return "Not initialized";

            return $"Position: X={_currentCartesianPosition.x:F3}, Y={_currentCartesianPosition.y:F3}, Z={_currentCartesianPosition.z:F3}, U={_currentCartesianPosition.w:F1}°\n" +
                   $"Reachable: {IsPositionReachable(new Vector3(_currentCartesianPosition.x, _currentCartesianPosition.y, _currentCartesianPosition.z))}\n" +
                   $"Distance: {RadialDistanceFromOrigin(new Vector3(_currentCartesianPosition.x, _currentCartesianPosition.y, _currentCartesianPosition.z)):F3}m";
        }
        #endregion
    }
}