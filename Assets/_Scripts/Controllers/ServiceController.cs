namespace _Scripts.Controllers.WelcomeController
{
    using System;
    using UnityEngine;
    using _Scripts.Models.CognitoManagement;

    public class ServiceController : MonoBehaviour
    {
        #region Singleton Pattern
        private static ServiceController _instance;
        public static ServiceController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ServiceController>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ServiceController");
                        _instance = go.AddComponent<ServiceController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                WelcomeOrchestrator.Instance.OnAuthenticationSuccess -= OnAuthenticationSuccess;
                _instance = null;
            }
        }
        #endregion

        private bool _isInitialized = false;

        private void Start() 
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                if (_isInitialized)
                {
                    Debug.Log("ServiceController already initialized");
                    return;
                }

                Debug.Log("Initializing ServiceController...");
                // Verificar que WelcomeOrchestrator esté inicializado antes de suscribirse
                if (WelcomeOrchestrator.Instance != null && WelcomeOrchestrator.Instance.isActiveAndEnabled)
                {
                    WelcomeOrchestrator.Instance.OnAuthenticationSuccess += OnAuthenticationSuccess;
                    Debug.Log("ServiceController subscribed to OnAuthenticationSuccess");
                }
                else
                {
                    Debug.LogWarning("WelcomeOrchestrator not ready, delaying subscription...");
                }
                _isInitialized = true;
                Debug.Log("ServiceController initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Initialization error: {ex.Message}");
            }
        }

        private void OnAuthenticationSuccess()
        {
            Debug.Log("Authentication successful - Activating services...");
            ActivateServices();
        }

        private void ActivateServices()
        {
            var credentials = CognitoManager.Instance.CurrentAWSCredentials;
            if (credentials != null)
            {
                Debug.Log("AWS Credentials obtained, activating services...");
                // Aquí puedes agregar la lógica para instanciar y activar managers como SESManager
                // Ejemplo: SESManager.Instance.Initialize(credentials);
            }
            else
            {
                Debug.LogWarning("No AWS credentials available. Services not activated.");
            }
        }
    }
}