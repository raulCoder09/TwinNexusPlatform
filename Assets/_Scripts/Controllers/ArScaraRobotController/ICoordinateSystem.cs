using System;
using UnityEngine;

namespace _Scripts.Controllers.ArScaraRobotController
{
    /// <summary>
    /// Interface para el sistema de coordenadas del robot ARSCARA
    /// Define el contrato para transformaciones de coordenadas y workspace
    /// </summary>
    public interface ICoordinateSystem
    {
        #region Properties
        /// <summary>
        /// Indica si el sistema está inicializado
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Posición cartesiana actual del end-effector (X, Y, Z, U)
        /// </summary>
        Vector4 CurrentCartesianPosition { get; }
        
        /// <summary>
        /// Alcance máximo del robot desde el origen
        /// </summary>
        float MaxReach { get; }
        
        /// <summary>
        /// Alcance mínimo del robot desde el origen
        /// </summary>
        float MinReach { get; }
        
        /// <summary>
        /// Límites del workspace en X
        /// </summary>
        Vector2 WorkspaceXLimits { get; }
        
        /// <summary>
        /// Límites del workspace en Y
        /// </summary>
        Vector2 WorkspaceYLimits { get; }
        
        /// <summary>
        /// Límites del workspace en Z
        /// </summary>
        Vector2 WorkspaceZLimits { get; }
        
        /// <summary>
        /// Parámetros físicos del robot
        /// </summary>
        RobotPhysicalParameters PhysicalParameters { get; }
        #endregion

        #region Initialization
        /// <summary>
        /// Inicializa el sistema de coordenadas
        /// </summary>
        /// <param name="robotManager">Referencia al robot manager</param>
        /// <returns>True si la inicialización fue exitosa</returns>
        bool Initialize(IRobotManager robotManager);
        
        /// <summary>
        /// Limpia recursos del sistema
        /// </summary>
        void Cleanup();
        
        /// <summary>
        /// Recalcula los límites del workspace basándose en parámetros actuales
        /// </summary>
        void RecalculateWorkspaceLimits();
        #endregion

        #region Position Tracking
        /// <summary>
        /// Actualiza la posición cartesiana actual basándose en joints
        /// </summary>
        void UpdateCurrentCartesianPosition();
        
        /// <summary>
        /// Obtiene la posición cartesiana actual
        /// </summary>
        /// <returns>Vector4 con posición (X, Y, Z, U)</returns>
        Vector4 GetCurrentCartesianPosition();
        
        /// <summary>
        /// Obtiene la posición del end-effector en coordenadas mundiales
        /// </summary>
        /// <returns>Posición mundial del end-effector</returns>
        Vector3 GetEndEffectorWorldPosition();
        
        /// <summary>
        /// Fuerza una actualización inmediata de la posición
        /// </summary>
        void ForcePositionUpdate();
        #endregion

        #region Coordinate Transformations
        /// <summary>
        /// Convierte de coordenadas del robot a coordenadas mundiales
        /// </summary>
        /// <param name="robotCoords">Coordenadas en espacio del robot</param>
        /// <returns>Coordenadas en espacio mundial</returns>
        Vector3 RobotToWorldCoordinates(Vector3 robotCoords);
        
        /// <summary>
        /// Convierte de coordenadas mundiales a coordenadas del robot
        /// </summary>
        /// <param name="worldCoords">Coordenadas en espacio mundial</param>
        /// <returns>Coordenadas en espacio del robot</returns>
        Vector3 WorldToRobotCoordinates(Vector3 worldCoords);
        
        /// <summary>
        /// Convierte posición cartesiana a coordenadas relativas al robot
        /// </summary>
        /// <param name="cartesianPos">Posición cartesiana</param>
        /// <returns>Coordenadas relativas al robot</returns>
        Vector3 CartesianToRobotSpace(Vector3 cartesianPos);
        
        /// <summary>
        /// Convierte coordenadas del robot a posición cartesiana absoluta
        /// </summary>
        /// <param name="robotPos">Posición en espacio del robot</param>
        /// <returns>Posición cartesiana absoluta</returns>
        Vector3 RobotSpaceToCartesian(Vector3 robotPos);
        
        /// <summary>
        /// Transforma un vector de dirección del robot a mundo
        /// </summary>
        /// <param name="robotDirection">Dirección en espacio del robot</param>
        /// <returns>Dirección en espacio mundial</returns>
        Vector3 TransformDirectionToWorld(Vector3 robotDirection);
        
        /// <summary>
        /// Transforma un vector de dirección del mundo al robot
        /// </summary>
        /// <param name="worldDirection">Dirección en espacio mundial</param>
        /// <returns>Dirección en espacio del robot</returns>
        Vector3 TransformDirectionToRobot(Vector3 worldDirection);
        #endregion

        #region Workspace Validation
        /// <summary>
        /// Verifica si una posición está dentro del workspace
        /// </summary>
        /// <param name="targetPosition">Posición a verificar</param>
        /// <returns>True si está dentro del workspace</returns>
        bool IsPositionReachable(Vector3 targetPosition);
        
        /// <summary>
        /// Verifica si una posición está dentro del workspace (con orientación)
        /// </summary>
        /// <param name="targetPosition">Posición a verificar (X,Y,Z,U)</param>
        /// <returns>True si está dentro del workspace</returns>
        bool IsPositionReachable(Vector4 targetPosition);
        
        /// <summary>
        /// Clampea una posición a los límites del workspace
        /// </summary>
        /// <param name="position">Posición a clampear</param>
        /// <returns>Posición clampeada dentro del workspace</returns>
        Vector3 ClampToWorkspace(Vector3 position);
        
        /// <summary>
        /// Clampea una posición con orientación a los límites del workspace
        /// </summary>
        /// <param name="position">Posición a clampear (X,Y,Z,U)</param>
        /// <returns>Posición clampeada dentro del workspace</returns>
        Vector4 ClampToWorkspace(Vector4 position);
        
        /// <summary>
        /// Valida posición y notifica si hay violación del workspace
        /// </summary>
        /// <param name="targetPosition">Posición a validar</param>
        /// <returns>Resultado de la validación</returns>
        WorkspaceValidationResult ValidatePosition(Vector3 targetPosition);
        
        /// <summary>
        /// Obtiene información detallada sobre por qué una posición no es alcanzable
        /// </summary>
        /// <param name="targetPosition">Posición a analizar</param>
        /// <returns>Información detallada de alcanzabilidad</returns>
        ReachabilityInfo GetReachabilityInfo(Vector3 targetPosition);
        #endregion

        #region Distance and Geometry Calculations
        /// <summary>
        /// Calcula la distancia del end-effector a una posición target
        /// </summary>
        /// <param name="targetPosition">Posición target</param>
        /// <returns>Distancia en metros</returns>
        float DistanceToTarget(Vector3 targetPosition);
        
        /// <summary>
        /// Calcula el ángulo requerido para alcanzar una posición
        /// </summary>
        /// <param name="targetPosition">Posición target</param>
        /// <returns>Ángulo en grados</returns>
        float AngleToTarget(Vector3 targetPosition);
        
        /// <summary>
        /// Calcula la distancia radial desde el origen del robot
        /// </summary>
        /// <param name="position">Posición a evaluar</param>
        /// <returns>Distancia radial en metros</returns>
        float RadialDistanceFromOrigin(Vector3 position);
        
        /// <summary>
        /// Verifica si dos posiciones están dentro de una tolerancia
        /// </summary>
        /// <param name="pos1">Primera posición</param>
        /// <param name="pos2">Segunda posición</param>
        /// <param name="tolerance">Tolerancia en metros</param>
        /// <returns>True si están dentro de la tolerancia</returns>
        bool IsWithinTolerance(Vector3 pos1, Vector3 pos2, float tolerance = 0.001f);
        
        /// <summary>
        /// Calcula el punto más cercano en el workspace a una posición dada
        /// </summary>
        /// <param name="targetPosition">Posición target</param>
        /// <returns>Punto más cercano alcanzable</returns>
        Vector3 GetClosestReachablePoint(Vector3 targetPosition);
        
        /// <summary>
        /// Calcula la distancia mínima entre dos puntos considerando el workspace
        /// </summary>
        /// <param name="startPos">Posición inicial</param>
        /// <param name="endPos">Posición final</param>
        /// <returns>Distancia mínima en el workspace</returns>
        float GetWorkspaceDistance(Vector3 startPos, Vector3 endPos);
        #endregion

        #region Reference Frame Management
        /// <summary>
        /// Establece un frame de referencia personalizado
        /// </summary>
        /// <param name="frameId">ID del frame</param>
        /// <param name="origin">Origen del frame</param>
        /// <param name="rotation">Rotación del frame</param>
        void SetReferenceFrame(string frameId, Vector3 origin, Quaternion rotation);
        
        /// <summary>
        /// Obtiene un frame de referencia
        /// </summary>
        /// <param name="frameId">ID del frame</param>
        /// <returns>Información del frame de referencia</returns>
        ReferenceFrame GetReferenceFrame(string frameId);
        
        /// <summary>
        /// Transforma una posición a un frame de referencia específico
        /// </summary>
        /// <param name="position">Posición a transformar</param>
        /// <param name="frameId">ID del frame destino</param>
        /// <returns>Posición en el frame especificado</returns>
        Vector3 TransformToFrame(Vector3 position, string frameId);
        
        /// <summary>
        /// Transforma una posición desde un frame de referencia específico
        /// </summary>
        /// <param name="position">Posición en el frame</param>
        /// <param name="frameId">ID del frame origen</param>
        /// <returns>Posición en coordenadas base del robot</returns>
        Vector3 TransformFromFrame(Vector3 position, string frameId);
        
        /// <summary>
        /// Elimina un frame de referencia
        /// </summary>
        /// <param name="frameId">ID del frame a eliminar</param>
        /// <returns>True si se eliminó exitosamente</returns>
        bool RemoveReferenceFrame(string frameId);
        #endregion

        #region Calibration and Offsets
        /// <summary>
        /// Establece un offset de calibración para el sistema
        /// </summary>
        /// <param name="offset">Offset de posición</param>
        /// <param name="rotationOffset">Offset de rotación</param>
        void SetCalibrationOffset(Vector3 offset, Quaternion rotationOffset);
        
        /// <summary>
        /// Obtiene el offset de calibración actual
        /// </summary>
        /// <returns>Offset de calibración</returns>
        CalibrationOffset GetCalibrationOffset();
        
        /// <summary>
        /// Aplica calibración a una posición
        /// </summary>
        /// <param name="position">Posición a calibrar</param>
        /// <returns>Posición calibrada</returns>
        Vector3 ApplyCalibration(Vector3 position);
        
        /// <summary>
        /// Remueve calibración de una posición
        /// </summary>
        /// <param name="calibratedPosition">Posición calibrada</param>
        /// <returns>Posición sin calibración</returns>
        Vector3 RemoveCalibration(Vector3 calibratedPosition);
        #endregion

        #region Events
        /// <summary>
        /// Se dispara cuando cambia la posición cartesiana
        /// </summary>
        event Action<Vector4> OnCartesianPositionChanged;
        
        /// <summary>
        /// Se dispara cuando hay una violación del workspace
        /// </summary>
        event Action<Vector3, string> OnWorkspaceViolation;
        
        /// <summary>
        /// Se dispara cuando cambia la alcanzabilidad de una posición
        /// </summary>
        event Action<bool> OnReachabilityChanged;
        
        /// <summary>
        /// Se dispara cuando se recalculan los límites del workspace
        /// </summary>
        event Action OnWorkspaceLimitsUpdated;
        
        /// <summary>
        /// Se dispara cuando se establece un nuevo frame de referencia
        /// </summary>
        event Action<string, ReferenceFrame> OnReferenceFrameSet;
        
        /// <summary>
        /// Se dispara cuando se actualiza la calibración
        /// </summary>
        event Action<CalibrationOffset> OnCalibrationUpdated;
        #endregion
    }

    #region Supporting Data Types
    
    /// <summary>
    /// Resultado de validación del workspace
    /// </summary>
    [System.Serializable]
    public struct WorkspaceValidationResult
    {
        public bool IsValid;
        public string[] Violations;
        public Vector3 SuggestedPosition;
        public float DistanceToWorkspace;
    }

    /// <summary>
    /// Información detallada de alcanzabilidad
    /// </summary>
    [System.Serializable]
    public struct ReachabilityInfo
    {
        public bool IsReachable;
        public float DistanceFromOrigin;
        public bool WithinRadialLimits;
        public bool WithinVerticalLimits;
        public string[] LimitingFactors;
        public Vector3 ClosestReachablePoint;
    }

    /// <summary>
    /// Frame de referencia personalizado
    /// </summary>
    [System.Serializable]
    public struct ReferenceFrame
    {
        public string FrameId;
        public Vector3 Origin;
        public Quaternion Rotation;
        public bool IsActive;
        public DateTime CreatedTime;
    }

    /// <summary>
    /// Offset de calibración
    /// </summary>
    [System.Serializable]
    public struct CalibrationOffset
    {
        public Vector3 PositionOffset;
        public Quaternion RotationOffset;
        public bool IsActive;
        public DateTime LastUpdated;
    }

    /// <summary>
    /// Tipo de coordenadas
    /// </summary>
    public enum CoordinateType
    {
        Robot,      // Coordenadas relativas al robot
        World,      // Coordenadas mundiales
        Tool,       // Coordenadas del tool/end-effector
        User        // Frame de usuario definido
    }

    /// <summary>
    /// Información de transformación entre frames
    /// </summary>
    [System.Serializable]
    public struct TransformationInfo
    {
        public CoordinateType SourceFrame;
        public CoordinateType TargetFrame;
        public Matrix4x4 TransformationMatrix;
        public bool IsValid;
    }

    #endregion
}