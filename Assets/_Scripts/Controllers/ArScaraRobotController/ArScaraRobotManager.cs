using System;
using UnityEngine;

namespace _Scripts.Controllers.ArScaraRobotController
{
    /// <summary>
    /// Singleton central que coordina todo el sistema del robot ARSCARA
    /// Punto único de comunicación entre UIs y robot
    /// </summary>
    public class ArScaraRobotManager : MonoBehaviour
    {
        #region Singleton Pattern
        private static ArScaraRobotManager _instance;
        public static ArScaraRobotManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ArScaraRobotManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ArScaraRobotManager");
                        _instance = go.AddComponent<ArScaraRobotManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Robot Components
        [Header("Robot Structure References")]
        [SerializeField] private Transform _robotBase;
        [SerializeField] private Transform _motor1Transform;    // Controla Link1 
        [SerializeField] private Transform _link1Transform;     // Rota con Motor1 (J1)
        [SerializeField] private Transform _motor2Transform;    // Controla Link2
        [SerializeField] private Transform _link2Transform;     // Rota con Motor2 (J2)
        [SerializeField] private Transform _bracketTransform;   // Bracket intermedio
        [SerializeField] private Transform _endEffectorTransform; // J3 (vertical) + J4 (rotación)

        [Header("Controllers")]
        [SerializeField] private ArScaraJointController _jointController;
        [SerializeField] private ArScaraSystemController _systemController;
        [SerializeField] private ArScaraCoordinateSystem _coordinateSystem;
        #endregion

        #region Robot Configuration
        [Header("Robot Parameters")]
        [SerializeField] private float _link1Length = 1.0f;     // Distancia Motor1 a Motor2
        [SerializeField] private float _link2Length = 1.0f;     // Distancia Motor2 a EndEffector
        [SerializeField] private float _baseHeight = 0.5f;      // Altura de la base
        [SerializeField] private float _maxVerticalRange = 0.3f; // Rango máximo J3

        [Header("Joint Limits")]
        [SerializeField] private Vector2 _j1Limits = new Vector2(-180f, 180f);  // J1 límites (grados)
        [SerializeField] private Vector2 _j2Limits = new Vector2(-180f, 180f);  // J2 límites (grados)
        [SerializeField] private Vector2 _j3Limits = new Vector2(0f, 0.3f);     // J3 límites (metros)
        [SerializeField] private Vector2 _j4Limits = new Vector2(-180f, 180f);  // J4 límites (grados)
        #endregion

        #region Robot State
        private ArScaraRobotState _robotState;
        private bool _isInitialized = false;
        #endregion

        #region Events
        public static event Action<ArScaraRobotState> OnRobotStateChanged;
        public static event Action<string> OnRobotError;
        public static event Action OnRobotInitialized;
        public static event Action<Vector3, float> OnPositionChanged; // X,Y,Z,U
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeRobot();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (_isInitialized)
            {
                Debug.Log("[ArScaraRobotManager] Robot ready for operation");
            }
        }

        private void Update()
        {
            if (_isInitialized && _systemController.IsSystemActive)
            {
                UpdateRobotState();
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Inicializa todo el sistema del robot
        /// </summary>
        private void InitializeRobot()
        {
            try
            {
                Debug.Log("[ArScaraRobotManager] Initializing ARSCARA robot system...");

                // Inicializar estado del robot
                _robotState = new ArScaraRobotState();
                
                // Buscar componentes si no están asignados
                FindRobotComponents();
                
                // Inicializar controladores
                InitializeControllers();
                
                // Configurar posición inicial (Home)
                SetToHomePosition();
                
                _isInitialized = true;
                
                Debug.Log("[ArScaraRobotManager] Robot system initialized successfully");
                OnRobotInitialized?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArScaraRobotManager] Initialization failed: {ex.Message}");
                OnRobotError?.Invoke($"Initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Busca automáticamente los componentes del robot en la escena
        /// </summary>
        private void FindRobotComponents()
        {
            // Buscar por nombres específicos en la jerarquía
            if (_robotBase == null)
                _robotBase = GameObject.Find("ARSCARA")?.transform;

            if (_link1Transform == null)
                _link1Transform = GameObject.Find("Link1")?.transform;

            if (_link2Transform == null)
                _link2Transform = GameObject.Find("Link2")?.transform;

            if (_endEffectorTransform == null)
                _endEffectorTransform = GameObject.Find("EndEffector")?.transform;

            // Buscar motores
            if (_motor1Transform == null)
                _motor1Transform = GameObject.Find("XL430W250T1")?.transform;

            if (_motor2Transform == null)
                _motor2Transform = GameObject.Find("XL430W250T2")?.transform;

            // Validar que encontramos los componentes críticos
            ValidateRobotComponents();
        }

        /// <summary>
        /// Valida que todos los componentes necesarios estén presentes
        /// </summary>
        private void ValidateRobotComponents()
        {
            bool allComponentsFound = true;

            if (_link1Transform == null)
            {
                Debug.LogError("[ArScaraRobotManager] Link1 Transform not found!");
                allComponentsFound = false;
            }

            if (_link2Transform == null)
            {
                Debug.LogError("[ArScaraRobotManager] Link2 Transform not found!");
                allComponentsFound = false;
            }

            if (_endEffectorTransform == null)
            {
                Debug.LogError("[ArScaraRobotManager] EndEffector Transform not found!");
                allComponentsFound = false;
            }

            if (!allComponentsFound)
            {
                throw new Exception("Critical robot components missing!");
            }

            Debug.Log("[ArScaraRobotManager] All robot components validated successfully");
        }

        /// <summary>
        /// Inicializa los controladores del robot
        /// </summary>
        private void InitializeControllers()
        {
            // Crear controladores si no existen
            if (_jointController == null)
            {
                _jointController = gameObject.AddComponent<ArScaraJointController>();
            }

            if (_systemController == null)
            {
                _systemController = gameObject.AddComponent<ArScaraSystemController>();
            }

            if (_coordinateSystem == null)
            {
                _coordinateSystem = gameObject.AddComponent<ArScaraCoordinateSystem>();
            }

            // Configurar controladores con referencias
            _jointController.Initialize(this);
            _systemController.Initialize(this);
            _coordinateSystem.Initialize(this);

            Debug.Log("[ArScaraRobotManager] Controllers initialized");
        }
        #endregion

        #region Robot Control Interface
        /// <summary>
        /// Establece el robot en posición Home (0,0,0,0)
        /// </summary>
        public void SetToHomePosition()
        {
            if (!_isInitialized) return;

            Debug.Log("[ArScaraRobotManager] Moving to Home position");
            
            _jointController.SetJointAngles(0f, 0f, 0f, 0f);
            _robotState.IsAtHome = true;
            
            NotifyStateChanged();
        }

        /// <summary>
        /// Activa/desactiva los motores del robot
        /// </summary>
        public void SetMotorsEnabled(bool enabled)
        {
            if (!_isInitialized) return;

            _systemController.SetMotorsEnabled(enabled);
            _robotState.MotorsEnabled = enabled;
            
            Debug.Log($"[ArScaraRobotManager] Motors {(enabled ? "enabled" : "disabled")}");
            NotifyStateChanged();
        }

        /// <summary>
        /// Establece el nivel de potencia (velocidad)
        /// </summary>
        public void SetPowerLevel(PowerLevel level)
        {
            if (!_isInitialized) return;

            _systemController.SetPowerLevel(level);
            _robotState.CurrentPowerLevel = level;
            
            Debug.Log($"[ArScaraRobotManager] Power level set to {level}");
            NotifyStateChanged();
        }

        /// <summary>
        /// Habilita/deshabilita un joint específico
        /// </summary>
        public void SetJointEnabled(int jointIndex, bool enabled)
        {
            if (!_isInitialized || jointIndex < 0 || jointIndex > 3) return;

            _jointController.SetJointEnabled(jointIndex, enabled);
            _robotState.JointStates[jointIndex].IsEnabled = enabled;
            
            Debug.Log($"[ArScaraRobotManager] Joint J{jointIndex + 1} {(enabled ? "enabled" : "disabled")}");
            NotifyStateChanged();
        }

        /// <summary>
        /// Habilita todos los joints
        /// </summary>
        public void EnableAllJoints()
        {
            for (int i = 0; i < 4; i++)
            {
                SetJointEnabled(i, true);
            }
            Debug.Log("[ArScaraRobotManager] All joints enabled");
        }

        /// <summary>
        /// Deshabilita todos los joints
        /// </summary>
        public void LockAllJoints()
        {
            for (int i = 0; i < 4; i++)
            {
                SetJointEnabled(i, false);
            }
            Debug.Log("[ArScaraRobotManager] All joints locked");
        }

        /// <summary>
        /// Ejecuta reset del sistema (limpia alarmas, NO mueve el robot)
        /// </summary>
        public void ResetSystem()
        {
            if (!_isInitialized) return;

            _systemController.ResetAlarms();
            _robotState.HasAlarms = false;
            _robotState.EmergencyStopActive = false;
            
            Debug.Log("[ArScaraRobotManager] System reset completed");
            NotifyStateChanged();
        }
        #endregion

        #region State Management
        /// <summary>
        /// Actualiza el estado del robot en cada frame
        /// </summary>
        private void UpdateRobotState()
        {
            // Actualizar posiciones de joints
            var jointAngles = _jointController.GetCurrentJointAngles();
            for (int i = 0; i < 4; i++)
            {
                _robotState.JointStates[i].CurrentAngle = jointAngles[i];
            }

            // Actualizar posición cartesiana
            var cartesianPos = _coordinateSystem.GetCurrentCartesianPosition();
            _robotState.CurrentPosition = cartesianPos;

            // Verificar si está en Home
            _robotState.IsAtHome = IsAtHomePosition();

            // Notificar cambio de posición
            OnPositionChanged?.Invoke(
                new Vector3(cartesianPos.x, cartesianPos.y, cartesianPos.z), 
                cartesianPos.w
            );
        }

        /// <summary>
        /// Verifica si el robot está en posición Home
        /// </summary>
        private bool IsAtHomePosition()
        {
            var angles = _jointController.GetCurrentJointAngles();
            return Mathf.Abs(angles[0]) < 0.1f && 
                   Mathf.Abs(angles[1]) < 0.1f && 
                   Mathf.Abs(angles[2]) < 0.01f && 
                   Mathf.Abs(angles[3]) < 0.1f;
        }

        /// <summary>
        /// Notifica cambio de estado
        /// </summary>
        private void NotifyStateChanged()
        {
            OnRobotStateChanged?.Invoke(_robotState);
        }
        #endregion

        #region Public Properties
        public bool IsInitialized => _isInitialized;
        public ArScaraRobotState RobotState => _robotState;
        public ArScaraJointController JointController => _jointController;
        public ArScaraSystemController SystemController => _systemController;
        public ArScaraCoordinateSystem CoordinateSystem => _coordinateSystem;

        // Transform References
        public Transform RobotBase => _robotBase;
        public Transform Link1Transform => _link1Transform;
        public Transform Link2Transform => _link2Transform;
        public Transform EndEffectorTransform => _endEffectorTransform;
        public Transform Motor1Transform => _motor1Transform;
        public Transform Motor2Transform => _motor2Transform;

        // Robot Parameters
        public float Link1Length => _link1Length;
        public float Link2Length => _link2Length;
        public float BaseHeight => _baseHeight;
        public float MaxVerticalRange => _maxVerticalRange;
        public Vector2 GetJointLimits(int jointIndex)
        {
            return jointIndex switch
            {
                0 => _j1Limits,
                1 => _j2Limits,
                2 => _j3Limits,
                3 => _j4Limits,
                _ => Vector2.zero
            };
        }
        #endregion

        #region Debug
        [ContextMenu("Debug Robot State")]
        public void DebugRobotState()
        {
            if (!_isInitialized)
            {
                Debug.Log("Robot not initialized");
                return;
            }

            Debug.Log("=== ARSCARA Robot State ===");
            Debug.Log($"Motors Enabled: {_robotState.MotorsEnabled}");
            Debug.Log($"Power Level: {_robotState.CurrentPowerLevel}");
            Debug.Log($"Is At Home: {_robotState.IsAtHome}");
            Debug.Log($"Emergency Stop: {_robotState.EmergencyStopActive}");
            Debug.Log($"Current Position: {_robotState.CurrentPosition}");
            
            for (int i = 0; i < 4; i++)
            {
                var joint = _robotState.JointStates[i];
                Debug.Log($"J{i + 1}: {joint.CurrentAngle:F2}° (Enabled: {joint.IsEnabled})");
            }
        }
        #endregion
    }

    #region Data Classes
    /// <summary>
    /// Estado completo del robot ARSCARA
    /// </summary>
    [System.Serializable]
    public class ArScaraRobotState
    {
        public bool MotorsEnabled;
        public PowerLevel CurrentPowerLevel = PowerLevel.Low;
        public bool EmergencyStopActive;
        public bool SafeguardActive;
        public bool HasAlarms;
        public bool IsAtHome;
        public Vector4 CurrentPosition; // X, Y, Z, U
        public JointState[] JointStates;

        public ArScaraRobotState()
        {
            JointStates = new JointState[4];
            for (int i = 0; i < 4; i++)
            {
                JointStates[i] = new JointState();
            }
        }
    }

    /// <summary>
    /// Estado de un joint individual
    /// </summary>
    [System.Serializable]
    public class JointState
    {
        public float CurrentAngle;
        public float TargetAngle;
        public bool IsEnabled = true;
        public bool IsMoving;
        public bool HasError;
    }

    /// <summary>
    /// Niveles de potencia del robot
    /// </summary>
    public enum PowerLevel
    {
        Low,
        High
    }
    #endregion
}