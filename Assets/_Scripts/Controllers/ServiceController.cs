using Amazon;

namespace _Scripts.Controllers.WelcomeController
{
    using System;
    using System.Threading.Tasks;
    using UnityEngine;
    using _Scripts.Models.CognitoManagement;
    using _Scripts.Models.SESManagement;

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
        private SESManager _sesManager;

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
                _sesManager = transform.Find("SESManager")?.GetComponent<SESManager>();
                if (_sesManager == null)
                {
                    Debug.LogWarning("SESManager not found as a child. Creating it...");
                    GameObject sesGo = new GameObject("SESManager");
                    sesGo.transform.parent = gameObject.transform;
                    _sesManager = sesGo.AddComponent<SESManager>();
                    _sesManager._senderEmail = "noreply@twinnexus.com";
                    _sesManager._senderName = "Twin Nexus Platform";
                    _sesManager._region = RegionEndpoint.USEast1;
                }

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

        private async void OnAuthenticationSuccess()
        {
            Debug.Log("Authentication successful - Activating SESManager...");
            await ActivateSESManager();
        }

        private async Task ActivateSESManager()
        {
            if (_sesManager != null)
            {
                var cognitoManager = CognitoManager.Instance;
                if (cognitoManager != null)
                {
                    Debug.Log("Waiting for AWS credentials...");
                    while (cognitoManager.CurrentAWSCredentials == null)
                    {
                        await Task.Delay(100); // Esperar 100ms y reintentar
                        Debug.Log("Retrying to get credentials...");
                    }
                    Debug.Log("AWS Credentials obtained, initializing SESManager...");
                    bool success = await _sesManager.InitializeAsync();
                    if (success)
                    {
                        Debug.Log("SESManager initialized successfully");
                        await _sesManager.SendTestEmailAsync(); // Probar con un email
                    }
                    else
                    {
                        Debug.LogError("Failed to initialize SESManager");
                    }
                }
                else
                {
                    Debug.LogError("CognitoManager instance is null");
                }
            }
            else
            {
                Debug.LogError("SESManager reference is null");
            }
        }
    }
}