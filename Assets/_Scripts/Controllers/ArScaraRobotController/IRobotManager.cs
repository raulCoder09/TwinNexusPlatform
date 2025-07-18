using System;
using UnityEngine;

namespace _Scripts.Controllers.ArScaraRobotController
{
    /// <summary>
    /// Interface principal para el gestor del robot ARSCARA
    /// Define el contrato para todas las operaciones de alto nivel del robot
    /// </summary>
    public interface IRobotManager
    {
        #region Properties
        /// <summary>
        /// Indica si el robot está inicializado y listo para operar
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Estado actual completo del robot
        /// </summary>
        ArScaraRobotState RobotState { get; }
        
        /// <summary>
        /// Controlador de joints asociado
        /// </summary>
        IJointController JointController { get; }
        
        /// <summary>
        /// Controlador de sistema asociado
        /// </summary>
        ISystemController SystemController { get; }
        
        /// <summary>
        /// Sistema de coordenadas asociado
        /// </summary>
        ICoordinateSystem CoordinateSystem { get; }
        #endregion

        #region Core Operations
        /// <summary>
        /// Inicializa completamente el sistema del robot
        /// </summary>
        /// <returns>True si la inicialización fue exitosa</returns>
        bool Initialize();
        
        /// <summary>
        /// Ejecuta shutdown seguro del sistema
        /// </summary>
        void Shutdown();
        
        /// <summary>
        /// Actualiza el estado del robot (llamado desde Update loop)
        /// </summary>
        void UpdateRobotState();
        #endregion

        #region Robot Control
        /// <summary>
        /// Mueve el robot a la posición Home
        /// </summary>
        void SetToHomePosition();
        
        /// <summary>
        /// Habilita o deshabilita los motores del robot
        /// </summary>
        /// <param name="enabled">True para habilitar motores</param>
        void SetMotorsEnabled(bool enabled);
        
        /// <summary>
        /// Establece el nivel de potencia del robot
        /// </summary>
        /// <param name="level">Nivel de potencia a establecer</param>
        void SetPowerLevel(PowerLevel level);
        
        /// <summary>
        /// Habilita o deshabilita un joint específico
        /// </summary>
        /// <param name="jointIndex">Índice del joint (0-3)</param>
        /// <param name="enabled">True para habilitar</param>
        void SetJointEnabled(int jointIndex, bool enabled);
        
        /// <summary>
        /// Habilita todos los joints del robot
        /// </summary>
        void EnableAllJoints();
        
        /// <summary>
        /// Deshabilita todos los joints del robot
        /// </summary>
        void LockAllJoints();
        
        /// <summary>
        /// Ejecuta reset del sistema (limpia alarmas, no mueve robot)
        /// </summary>
        void ResetSystem();
        #endregion

        #region Configuration Access
        /// <summary>
        /// Obtiene los límites de un joint específico
        /// </summary>
        /// <param name="jointIndex">Índice del joint (0-3)</param>
        /// <returns>Vector2 con límites min/max</returns>
        Vector2 GetJointLimits(int jointIndex);
        
        /// <summary>
        /// Obtiene los parámetros físicos del robot
        /// </summary>
        /// <returns>Parámetros de configuración del robot</returns>
        RobotPhysicalParameters GetRobotParameters();
        #endregion

        #region Events
        /// <summary>
        /// Se dispara cuando cambia el estado general del robot
        /// </summary>
        event Action<ArScaraRobotState> OnRobotStateChanged;
        
        /// <summary>
        /// Se dispara cuando ocurre un error en el robot
        /// </summary>
        event Action<string> OnRobotError;
        
        /// <summary>
        /// Se dispara cuando el robot se inicializa correctamente
        /// </summary>
        event Action OnRobotInitialized;
        
        /// <summary>
        /// Se dispara cuando cambia la posición del robot
        /// </summary>
        event Action<Vector3, float> OnPositionChanged;
        
        /// <summary>
        /// Se dispara cuando el robot alcanza la posición Home
        /// </summary>
        event Action OnHomePositionReached;
        
        /// <summary>
        /// Se dispara cuando se completa una operación crítica
        /// </summary>
        event Action<string> OnOperationCompleted;
        #endregion
    }

    /// <summary>
    /// Parámetros físicos del robot ARSCARA
    /// </summary>
    [System.Serializable]
    public struct RobotPhysicalParameters
    {
        public float Link1Length;
        public float Link2Length;
        public float BaseHeight;
        public float MaxVerticalRange;
        public Vector2 J1Limits;
        public Vector2 J2Limits;
        public Vector2 J3Limits;
        public Vector2 J4Limits;
    }
}