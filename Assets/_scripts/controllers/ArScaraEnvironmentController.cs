using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.XR.Management;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _scripts.controllers
{
    public class ArScaraEnvironmentController : MonoBehaviour
    {
        [Header("Environment Prefabs")]
        [SerializeField] private GameObject virtualEnvironment;
        [SerializeField] private GameObject augmentedEnvironment;
        [SerializeField] private GameObject twinNexusEnvironment;
        
        public enum EnvType { None, Virtual, Augmented, TwinNexus }

        private EnvType _currentEnvType = EnvType.None;
        private GameObject _currentEnv;

        private readonly System.Collections.Generic.Dictionary<EnvType, GameObject> _envPrefabs = new();

        private GameObject _simulationElements;
        private List<string> _namesSimulationElements= new List<string>();
        private GameObject _arPointCloud;
        private GameObject _simulationCamera;
        private UnityEngine.SceneManagement.Scene _simulatedEnvironmentScene;

        #region Unity Lifecycle

        private void Awake()
        {
            // Inicializar dictionary de prefabs
            _envPrefabs[EnvType.Virtual] = virtualEnvironment;
            _envPrefabs[EnvType.Augmented] = augmentedEnvironment;
            _envPrefabs[EnvType.TwinNexus] = twinNexusEnvironment;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Crea un nuevo entorno del tipo especificado.
        /// Destruye el entorno actual si existe.
        /// </summary>
        /// <param name="envType">Tipo de entorno a crear</param>
        public void CreateEnvironment(EnvType envType)
        {
            if (_currentEnv != null)
            {
                StartCoroutine(SafeDestroyEnvironment(_currentEnv));
                _currentEnv = null;
            }

            _currentEnvType = envType;

            if (envType == EnvType.None) return;
            
            if (!_envPrefabs.TryGetValue(envType, out var prefab) || prefab == null)
            {
                Debug.LogWarning($"Prefab not set for environment {envType}");
                return;
            }

            if (envType == EnvType.Augmented)
            {
                StartCoroutine(StartXRThenSpawn(prefab));
                return;
            }

            _currentEnv = Instantiate(prefab);
            var arscaraParent = GameObject.FindGameObjectWithTag("ARSCARA");
            if (arscaraParent != null)
            {
                _currentEnv.transform.SetParent(arscaraParent.transform);
            }
        }

        /// <summary>
        /// Destruye el entorno actual de forma segura
        /// </summary>
        public void DestroyCurrentEnvironment()
        {
            if (_currentEnv != null)
            {
                StartCoroutine(SafeDestroyEnvironment(_currentEnv));
                _currentEnv = null;
            }

            _currentEnvType = EnvType.None;
        }
        
        public EnvType GetCurrentEnvironmentType()
        {
            return _currentEnvType;
        }
        
        public GameObject GetCurrentEnvironment()
        {
            return _currentEnv;
        }
        
        public bool HasActiveEnvironment()
        {
            return _currentEnv != null && _currentEnvType != EnvType.None;
        }

        #endregion

        #region Private Methods - Environment Management
        
        private IEnumerator SafeDestroyEnvironment(GameObject root)
        {
            if (!root) yield break;

#if UNITY_EDITOR
            // Deseleccionar en Editor si está seleccionado
            var sel = Selection.activeGameObject;
            if (sel && (sel == root || sel.transform.IsChildOf(root.transform)))
                Selection.activeObject = null;
#endif
            
            if (root.name == "TwinNexusEnvironmentArScara(Clone)")
            {
                var twinNexus = root.GetComponent<ArScaraTwinNexusController>();
                if (twinNexus != null && twinNexus.mqttProtocol.IsConnected)
                {
                    twinNexus.mqttProtocol.Topic = "ARSCARA/StatusConnection";
                    twinNexus.mqttProtocol.Payload = new { Status = "Disconnected" };
                    twinNexus.mqttProtocol.SendData();
                }
                yield return new WaitForSeconds(1f);
                twinNexus.mqttProtocol.Disconnect();
            }
            root.SetActive(false);
            yield return null;
            yield return new WaitForEndOfFrame();
            
            if (root) Destroy(root);
        }
        
        private IEnumerator StartXRThenSpawn(GameObject prefab)
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            var mgr = XRGeneralSettings.Instance?.Manager;
            if (mgr != null)
            {
                mgr.InitializeLoaderSync();
                yield return null;
                mgr.StartSubsystems();
            }
            
            yield return new WaitForEndOfFrame();
            
            _currentEnv = Instantiate(prefab);
            var arscara = GameObject.FindGameObjectWithTag("ARSCARA");
            if (arscara != null)
            {
                if (Application.isEditor)
                {
                    StartCoroutine(FindSimulationElements());
                }
            }
        }

        private IEnumerator FindSimulationElements()
        {
            yield return new WaitForSeconds(10f);

            foreach (var t in FindObjectsOfType<Transform>(true))
            {
                var n = t.name;
                if (n.StartsWith("ARPointCloud"))_namesSimulationElements.Add($"{n}");
                if (n.StartsWith("SimulationCamera"))_namesSimulationElements.Add($"{n}");
            }
            
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.name.StartsWith("Simulated Environment Scene"))_namesSimulationElements.Add($"{scene.name}");
            }


            _arPointCloud = GameObject.Find(_namesSimulationElements[0]);
            _simulationCamera = GameObject.Find(_namesSimulationElements[1]);
            _simulatedEnvironmentScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(_namesSimulationElements[2]);

            if (_simulatedEnvironmentScene.IsValid())
            {
                print($"Scene encontrada: {_simulatedEnvironmentScene.name}");
            }
            else
            {
                print("Scene no encontrada o no está cargada");
            }
        }

        private IEnumerator StartXRNextFrame()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            var mgr = XRGeneralSettings.Instance?.Manager;
            if (mgr != null)
            {
                mgr.InitializeLoaderSync();
                yield return null;
                mgr.StartSubsystems();
            }
        }

        #endregion

        #region Utility Methods
        public bool IsXRAvailable()
        {
            return XRGeneralSettings.Instance?.Manager != null;
        }

        #endregion
    }
}