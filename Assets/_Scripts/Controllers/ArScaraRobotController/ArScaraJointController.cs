using System;
using UnityEngine;

namespace _Scripts.Controllers.ArScaraRobotController
{
    /// <summary>
    /// Controlador específico para joints del robot ARSCARA
    /// Maneja el movimiento directo de cada articulación
    /// </summary>
    public class ArScaraJointController : MonoBehaviour
    {
        #region Private Fields

        private ArScaraRobotManager _robotManager;
        private bool _isInitialized = false;

        // Referencias a transforms del robot
        private Transform _link1Transform; // J1 - Rotación base
        private Transform _link2Transform; // J2 - Rotación segundo brazo  
        private Transform _endEffectorTransform; // J3 - Vertical + J4 - Rotación end-effector

        // Estado actual de joints
        private float[] _currentJointAngles = new float[4]; // J1, J2, J3, J4
        private float[] _targetJointAngles = new float[4];
        private bool[] _jointEnabled = new bool[4] { true, true, true, true };

        // Configuración de movimiento
        private float[] _jointSpeeds = new float[4] { 30f, 30f, 0.1f, 45f }; // grados/s para rotación, m/s para linear
        private float _currentSpeedMultiplier = 1.0f;

        // Posición inicial del EndEffector para movimiento vertical
        private Vector3 _endEffectorInitialPosition;

        #endregion

        #region Events

        public static event Action<int, float> OnJointMoved; // jointIndex, newAngle
        public static event Action<int, bool> OnJointEnabledChanged; // jointIndex, enabled
        public static event Action<float[]> OnAllJointsUpdated; // all angles

        #endregion

        #region Initialization

        /// <summary>
        /// Inicializa el controlador de joints
        /// </summary>
        public void Initialize(ArScaraRobotManager robotManager)
        {
            _robotManager = robotManager ?? throw new ArgumentNullException(nameof(robotManager));

            try
            {
                // Obtener referencias de transforms
                _link1Transform = _robotManager.Link1Transform;
                _link2Transform = _robotManager.Link2Transform;
                _endEffectorTransform = _robotManager.EndEffectorTransform;

                // Validar referencias
                ValidateTransforms();

                // Guardar posición inicial del EndEffector
                if (_endEffectorTransform != null)
                {
                    _endEffectorInitialPosition = _endEffectorTransform.localPosition;
                }

                // Inicializar ángulos actuales basados en transforms actuales
                ReadCurrentJointAngles();

                // Copiar a target angles
                Array.Copy(_currentJointAngles, _targetJointAngles, 4);

                _isInitialized = true;
                Debug.Log("[ArScaraJointController] Joint controller initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArScaraJointController] Initialization failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Valida que todas las referencias de transforms estén correctas
        /// </summary>
        private void ValidateTransforms()
        {
            if (_link1Transform == null)
                throw new Exception("Link1 Transform not found!");

            if (_link2Transform == null)
                throw new Exception("Link2 Transform not found!");

            if (_endEffectorTransform == null)
                throw new Exception("EndEffector Transform not found!");

            Debug.Log("[ArScaraJointController] All transforms validated");
        }

        /// <summary>
        /// Lee los ángulos actuales de los transforms
        /// </summary>
        private void ReadCurrentJointAngles()
        {
            if (_link1Transform != null)
                _currentJointAngles[0] = _link1Transform.localEulerAngles.y;

            if (_link2Transform != null)
                _currentJointAngles[1] = _link2Transform.localEulerAngles.y;

            if (_endEffectorTransform != null)
            {
                // J3 - posición vertical relativa
                _currentJointAngles[2] = _endEffectorTransform.localPosition.y - _endEffectorInitialPosition.y;
                // J4 - rotación del end-effector
                _currentJointAngles[3] = _endEffectorTransform.localEulerAngles.y;
            }

            // Normalizar ángulos a rango -180 a 180
            for (int i = 0; i < 4; i++)
            {
                if (i != 2) // J3 es lineal, no normalizar
                {
                    _currentJointAngles[i] = NormalizeAngle(_currentJointAngles[i]);
                }
            }
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            if (!_isInitialized || _robotManager == null) return;

            // Solo actualizar si los motores están activos
            if (!_robotManager.RobotState.MotorsEnabled) return;

            UpdateJointMovements();
        }

        /// <summary>
        /// Actualiza el movimiento de todos los joints hacia sus targets
        /// </summary>
        private void UpdateJointMovements()
        {
            bool anyMovementOccurred = false;

            for (int i = 0; i < 4; i++)
            {
                if (!_jointEnabled[i]) continue;

                float currentAngle = _currentJointAngles[i];
                float targetAngle = _targetJointAngles[i];

                // Verificar si hay diferencia significativa
                float difference = Mathf.Abs(targetAngle - currentAngle);
                if (difference < 0.01f) continue; // Umbral mínimo

                // Calcular nueva posición
                float speed = _jointSpeeds[i] * _currentSpeedMultiplier * Time.deltaTime;
                float newAngle = Mathf.MoveTowards(currentAngle, targetAngle, speed);

                // Aplicar límites de joint
                newAngle = ApplyJointLimits(i, newAngle);

                // Actualizar joint si cambió
                if (Mathf.Abs(newAngle - currentAngle) > 0.001f)
                {
                    SetJointAngle(i, newAngle);
                    anyMovementOccurred = true;
                }
            }

            // Notificar si hubo movimiento
            if (anyMovementOccurred)
            {
                OnAllJointsUpdated?.Invoke(_currentJointAngles);
            }
        }

        #endregion

        #region Joint Control Public Methods

        /// <summary>
        /// Mueve un joint específico a un ángulo/posición
        /// </summary>
        public void MoveJoint(int jointIndex, float targetValue)
        {
            if (!IsValidJointIndex(jointIndex)) return;
            if (!_jointEnabled[jointIndex])
            {
                Debug.LogWarning($"[ArScaraJointController] Joint J{jointIndex + 1} is disabled");
                return;
            }

            // Aplicar límites antes de establecer target
            float clampedValue = ApplyJointLimits(jointIndex, targetValue);
            _targetJointAngles[jointIndex] = clampedValue;

            Debug.Log($"[ArScaraJointController] J{jointIndex + 1} target set to {clampedValue:F2}");
        }

        /// <summary>
        /// Mueve un joint incrementalmente
        /// </summary>
        public void MoveJointIncremental(int jointIndex, float deltaValue)
        {
            if (!IsValidJointIndex(jointIndex)) return;

            float newTarget = _targetJointAngles[jointIndex] + deltaValue;
            MoveJoint(jointIndex, newTarget);
        }

        /// <summary>
        /// Establece todos los ángulos de joints de una vez
        /// </summary>
        public void SetJointAngles(float j1, float j2, float j3, float j4)
        {
            if (!_isInitialized) return;

            float[] newAngles = { j1, j2, j3, j4 };

            for (int i = 0; i < 4; i++)
            {
                if (_jointEnabled[i])
                {
                    _targetJointAngles[i] = ApplyJointLimits(i, newAngles[i]);
                }
            }

            Debug.Log(
                $"[ArScaraJointController] All joint targets set: J1={j1:F2}, J2={j2:F2}, J3={j3:F3}, J4={j4:F2}");
        }

        /// <summary>
        /// Establece directamente el ángulo actual de un joint (sin interpolación)
        /// </summary>
        public void SetJointAngle(int jointIndex, float angle)
        {
            if (!IsValidJointIndex(jointIndex)) return;

            _currentJointAngles[jointIndex] = angle;
            ApplyAngleToTransform(jointIndex, angle);

            OnJointMoved?.Invoke(jointIndex, angle);
        }

        /// <summary>
        /// Habilita/deshabilita un joint específico
        /// </summary>
        public void SetJointEnabled(int jointIndex, bool enabled)
        {
            if (!IsValidJointIndex(jointIndex)) return;

            _jointEnabled[jointIndex] = enabled;
            Debug.Log($"[ArScaraJointController] J{jointIndex + 1} {(enabled ? "enabled" : "disabled")}");

            OnJointEnabledChanged?.Invoke(jointIndex, enabled);
        }

        /// <summary>
        /// Establece la velocidad de movimiento para todos los joints
        /// </summary>
        public void SetMovementSpeed(float speedMultiplier)
        {
            _currentSpeedMultiplier = Mathf.Clamp(speedMultiplier, 0.1f, 5.0f);
            Debug.Log($"[ArScaraJointController] Movement speed set to {_currentSpeedMultiplier:F1}x");
        }

        #endregion

        #region Joint Control Private Methods

        /// <summary>
        /// Aplica un ángulo al transform correspondiente
        /// </summary>
        private void ApplyAngleToTransform(int jointIndex, float angle)
        {
            switch (jointIndex)
            {
                case 0: // J1 - Link1 rotación Y
                    if (_link1Transform != null)
                    {
                        _link1Transform.localRotation = Quaternion.Euler(0, angle, 0);
                    }

                    break;

                case 1: // J2 - Link2 rotación Y
                    if (_link2Transform != null)
                    {
                        _link2Transform.localRotation = Quaternion.Euler(0, angle, 0);
                    }

                    break;

                case 2: // J3 - EndEffector posición vertical
                    if (_endEffectorTransform != null)
                    {
                        Vector3 newPos = _endEffectorInitialPosition;
                        newPos.y += angle; // angle es realmente un desplazamiento en Y
                        _endEffectorTransform.localPosition = newPos;
                    }

                    break;

                case 3: // J4 - EndEffector rotación Y
                    if (_endEffectorTransform != null)
                    {
                        Vector3 currentEuler = _endEffectorTransform.localEulerAngles;
                        _endEffectorTransform.localRotation = Quaternion.Euler(currentEuler.x, angle, currentEuler.z);
                    }

                    break;
            }
        }

        /// <summary>
        /// Aplica límites a un valor de joint
        /// </summary>
        private float ApplyJointLimits(int jointIndex, float value)
        {
            Vector2 limits = _robotManager.GetJointLimits(jointIndex);
            return Mathf.Clamp(value, limits.x, limits.y);
        }

        /// <summary>
        /// Normaliza un ángulo al rango -180 a 180
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        /// <summary>
        /// Verifica si un índice de joint es válido
        /// </summary>
        private bool IsValidJointIndex(int jointIndex)
        {
            if (jointIndex < 0 || jointIndex > 3)
            {
                Debug.LogError($"[ArScaraJointController] Invalid joint index: {jointIndex}");
                return false;
            }

            return true;
        }

        #endregion

        #region Public Properties and Getters

        /// <summary>
        /// Obtiene los ángulos actuales de todos los joints
        /// </summary>
        public float[] GetCurrentJointAngles()
        {
            return (float[])_currentJointAngles.Clone();
        }

        /// <summary>
        /// Obtiene los ángulos target de todos los joints
        /// </summary>
        public float[] GetTargetJointAngles()
        {
            return (float[])_targetJointAngles.Clone();
        }

        /// <summary>
        /// Obtiene el ángulo actual de un joint específico
        /// </summary>
        public float GetJointAngle(int jointIndex)
        {
            if (!IsValidJointIndex(jointIndex)) return 0f;
            return _currentJointAngles[jointIndex];
        }

        /// <summary>
        /// Verifica si un joint está habilitado
        /// </summary>
        public bool IsJointEnabled(int jointIndex)
        {
            if (!IsValidJointIndex(jointIndex)) return false;
            return _jointEnabled[jointIndex];
        }

        /// <summary>
        /// Verifica si algún joint se está moviendo
        /// </summary>
        public bool IsAnyJointMoving()
        {
            for (int i = 0; i < 4; i++)
            {
                if (Mathf.Abs(_targetJointAngles[i] - _currentJointAngles[i]) > 0.01f)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Verifica si un joint específico se está moviendo
        /// </summary>
        public bool IsJointMoving(int jointIndex)
        {
            if (!IsValidJointIndex(jointIndex)) return false;
            return Mathf.Abs(_targetJointAngles[jointIndex] - _currentJointAngles[jointIndex]) > 0.01f;
        }

        /// <summary>
        /// Obtiene la velocidad actual de movimiento
        /// </summary>
        public float CurrentSpeedMultiplier => _currentSpeedMultiplier;

        /// <summary>
        /// Verifica si el controlador está inicializado
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Debug and Utilities

        /// <summary>
        /// Para el movimiento de todos los joints
        /// </summary>
        public void StopAllJoints()
        {
            for (int i = 0; i < 4; i++)
            {
                _targetJointAngles[i] = _currentJointAngles[i];
            }

            Debug.Log("[ArScaraJointController] All joints stopped");
        }

        /// <summary>
        /// Para el movimiento de un joint específico
        /// </summary>
        public void StopJoint(int jointIndex)
        {
            if (!IsValidJointIndex(jointIndex)) return;

            _targetJointAngles[jointIndex] = _currentJointAngles[jointIndex];
            Debug.Log($"[ArScaraJointController] J{jointIndex + 1} stopped");
        }

        [ContextMenu("Debug Joint States")]
        public void DebugJointStates()
        {
            if (!_isInitialized)
            {
                Debug.Log("Joint controller not initialized");
                return;
            }

            Debug.Log("=== Joint Controller Debug ===");
            for (int i = 0; i < 4; i++)
            {
                string jointType = i == 2 ? "Linear" : "Rotational";
                string unit = i == 2 ? "m" : "°";

                Debug.Log($"J{i + 1} ({jointType}): " +
                          $"Current={_currentJointAngles[i]:F3}{unit}, " +
                          $"Target={_targetJointAngles[i]:F3}{unit}, " +
                          $"Enabled={_jointEnabled[i]}, " +
                          $"Moving={IsJointMoving(i)}");
            }

            Debug.Log($"Speed Multiplier: {_currentSpeedMultiplier:F2}x");
        }

        /// <summary>
        /// Resetea todos los joints a posición cero
        /// </summary>
        [ContextMenu("Reset All Joints")]
        public void ResetAllJoints()
        {
            SetJointAngles(0f, 0f, 0f, 0f);
            Debug.Log("[ArScaraJointController] All joints reset to zero");
        }

        #endregion
    }
}