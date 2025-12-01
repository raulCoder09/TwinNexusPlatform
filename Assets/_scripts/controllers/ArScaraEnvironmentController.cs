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
        private void Awake()
        {
            // Inicializar dictionary de prefabs
            _envPrefabs[EnvType.Virtual] = virtualEnvironment;
            _envPrefabs[EnvType.Augmented] = augmentedEnvironment;
            _envPrefabs[EnvType.TwinNexus] = twinNexusEnvironment;
        }
        public void CreateEnvironment(EnvType envType)
        {
            if (_currentEnv != null)
            {
                DestroyEnvironment(_currentEnv);
                _currentEnv = null;
            }

            _currentEnvType = envType;

            if (envType == EnvType.None) return;
            
            if (!_envPrefabs.TryGetValue(envType, out var prefab) || prefab == null) return;
            
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
        private void DestroyEnvironment(GameObject root)
        {
            if (root.name == "TwinNexusEnvironmentArScara(Clone)")
            {
                
            }
            if (Application.isEditor)
            {
                if (_arPointCloud!=null && _simulationCamera!=null)
                {
                    Destroy(_arPointCloud);
                    Destroy(_simulationCamera);
                }
            }
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
            if (Application.isEditor)
            {
                yield return new WaitForSeconds(.5f);
                FindSimulationElements();
            }
        }

        private void FindSimulationElements()
        {
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
        }

        public bool IsXRAvailable()
        {
            return XRGeneralSettings.Instance?.Manager != null;
        }
    }
}