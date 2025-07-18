using System;
using UnityEngine;

namespace _Scripts.Controllers.ArScaraRobotController
{
    /// <summary>
    /// Interface para el controlador de joints del robot ARSCARA
    /// Define el contrato para control directo de articulaciones
    /// </summary>
    public interface IJointController
    {
        #region Properties
        /// <summary>
        /// Indica si el controlador está inicializado
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Multiplicador de velocidad actual
        /// </summary>
        float CurrentSpeedMultiplier { get; }
        
        /// <summary>
        /// Número total de joints del robot
        /// </summary>
        int JointCount { get; }
        #endregion

        #region Initialization
        /// <summary>
        /// Inicializa el controlador de joints
        /// </summary>
        /// <param name="robotManager">Referencia al robot manager</param>
        /// <returns>True si la inicialización fue exitosa</returns>
        bool Initialize(IRobotManager robotManager);
        
        /// <summary>
        /// Limpia recursos del controlador
        /// </summary>
        void Cleanup();
        #endregion

        #region Joint Movement
        /// <summary>
        /// Mueve un joint específico a una posición target
        /// </summary>
        /// <param name="jointIndex">Índice del joint (0-3)</param>
        /// <param name="targetValue">Valor target (grados para rotacional, metros para lineal)</param>
        /// <param name="moveImmediate">Si true, mueve instantáneamente sin interpolación</param>
        void MoveJoint(int jointIndex, float targetValue, bool moveImmediate = false);
        
        /// <summary>
        /// Mueve un joint incrementalmente
        /// </summary>
        /// <param name="jointIndex">Índice del joint (0-3)</param>
        /// <param name="deltaValue">Valor incremental</param>
        void MoveJointIncremental(int jointIndex, float deltaValue);
        
        /// <summary>
        /// Establece todos los ángulos de joints simultáneamente
        /// </summary>
        /// <param name="j1">Ángulo joint 1 (grados)</param>
        /// <param name="j2">Ángulo joint 2 (grados)</param>
        /// <param name="j3">Posición joint 3 (metros)</param>
        /// <param name="j4">Ángulo joint 4 (grados)</param>
        /// <param name="moveImmediate">Si true, mueve instantáneamente</param>
        void SetJointAngles(float j1, float j2, float j3, float j4, bool moveImmediate = false);
        
        /// <summary>
        /// Establece todos los ángulos desde un array
        /// </summary>
        /// <param name="jointAngles">Array con 4 valores de joints</param>
        /// <param name="moveImmediate">Si true, mueve instantáneamente</param>
        void SetJointAngles(float[] jointAngles, bool moveImmediate = false);
        #endregion

        #region Joint Control
        /// <summary>
        /// Habilita o deshabilita un joint específico
        /// </summary>
        /// <param name="jointIndex">Índice del joint (0-3)</param>
        /// <param name="enabled">True para habilitar</param>
        void SetJointEnabled(int jointIndex, bool enabled);
        
        /// <summary>
        /// Establece la velocidad de movimiento
        /// </summary>
        /// <param name="speedMultiplier">Multiplicador de velocidad (0.1 - 5.0)</param>
        void SetMovementSpeed(float speedMultiplier);
        
        /// <summary>
        /// Para el movimiento de un joint específico
        /// </summary>
        /// <param name="jointIndex">Índice del joint (0-3)</param>
        void StopJoint(int jointIndex);
        
        /// <summary>
        /// Para el movimiento de todos los joints
        /// </summary>
        void StopAllJoints();
        
        /// <summary>
        /// Habilita todos los joints
        /// </summary>
        void EnableAllJoints();
        
        /// <summary>
        /// Deshabilita todos los joints
        /// </summary>
        void DisableAllJoints();
        #endregion

        #region Joint State Queries
        /// <summary>
        /// Obtiene el ángulo actual de un joint específico
        /// </summary>
        /// <param name="jointIndex">Índice del joint (0-3)</param>
        /// <returns>Ángulo actual (grados para rotacional, metros para lineal)</returns>
        float GetJointAngle(int jointIndex);
        
        /// <summary>
        /// Obtiene los ángulos actuales de todos los joints
        /// </summary>
        /// <returns>Array con 4 valores actuales</returns>
        float[] GetCurrentJointAngles();
        
        /// <summary>
        /// Obtiene los ángulos target de todos los joints
        /// </summary>
        /// <returns>Array con 4 valores target</returns>
        float[] GetTargetJointAngles();
        
        /// <summary>
        /// Verifica si un joint está habilitado
        /// </summary>
        /// <param name="jointIndex">Índice del joint (0-3)</param>
        /// <returns>True si está habilitado</returns>
        bool IsJointEnabled(int jointIndex);
        
        /// <summary>
        /// Verifica si un joint se está moviendo
        /// </summary>
        /// <param name="jointIndex">Índice del joint (0-3)</param>
        /// <returns>True si se está moviendo</returns>
        bool IsJointMoving(int jointIndex);
        
        /// <summary>
        /// Verifica si algún joint se está moviendo
        /// </summary>
        /// <returns>True si algún joint se está moviendo</returns>
        bool IsAnyJointMoving();
        
        /// <summary>
        /// Obtiene el estado completo de un joint
        /// </summary>
        /// <param name="jointIndex">Índice del joint (0-3)</param>
        /// <returns>Estado del joint</returns>
        JointState GetJointState(int jointIndex);
        
        /// <summary>
        /// Obtiene el estado de todos los joints
        /// </summary>
        /// <returns>Array con estados de todos los joints</returns>
        JointState[] GetAllJointStates();
        #endregion

        #region Validation
        /// <summary>
        /// Valida si un índice de joint es válido
        /// </summary>
        /// <param name="jointIndex">Índice a validar</param>
        /// <returns>True si es válido</returns>
        bool IsValidJointIndex(int jointIndex);
        
        /// <summary>
        /// Valida si un valor está dentro de los límites del joint
        /// </summary>
        /// <param name="jointIndex">Índice del joint</param>
        /// <param name="value">Valor a validar</param>
        /// <returns>True si está dentro de límites</returns>
        bool IsValueWithinLimits(int jointIndex, float value);
        
        /// <summary>
        /// Clampea un valor a los límites del joint
        /// </summary>
        /// <param name="jointIndex">Índice del joint</param>
        /// <param name="value">Valor a clampear</param>
        /// <returns>Valor clampeado</returns>
        float ClampToJointLimits(int jointIndex, float value);
        #endregion

        #region Events
        /// <summary>
        /// Se dispara cuando un joint se mueve
        /// </summary>
        event Action<int, float> OnJointMoved;
        
        /// <summary>
        /// Se dispara cuando cambia el estado de habilitación de un joint
        /// </summary>
        event Action<int, bool> OnJointEnabledChanged;
        
        /// <summary>
        /// Se dispara cuando se actualizan todos los joints
        /// </summary>
        event Action<float[]> OnAllJointsUpdated;
        
        /// <summary>
        /// Se dispara cuando un joint alcanza su posición target
        /// </summary>
        event Action<int> OnJointTargetReached;
        
        /// <summary>
        /// Se dispara cuando hay un error en un joint
        /// </summary>
        event Action<int, string> OnJointError;
        
        /// <summary>
        /// Se dispara cuando cambia la velocidad de movimiento
        /// </summary>
        event Action<float> OnMovementSpeedChanged;
        #endregion
    }
}