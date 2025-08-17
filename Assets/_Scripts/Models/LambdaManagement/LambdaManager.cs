using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda;
using UnityEngine;
using _Scripts.Controller;
using _Scripts.Controllers.ServiceManagement;
using Amazon.Runtime;

namespace _Scripts.Models.LambdaManagement
{
    public class LambdaManager : MonoBehaviour, ILambdaService
    {
        [SerializeField] private string _defaultFunctionName = "test";
        private ILambdaService _executor;
        private ILambdaService _permissionChecker;
        private AmazonLambdaClient _lambdaClient;
        private bool _isInitialized = false;

        public event Action<bool, string, object> OnExecutionComplete;

        public static LambdaManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task<bool> InitializeAsync(AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            if (_isInitialized) return true;

            try
            {
                _lambdaClient = LambdaClientFactory.GetLambdaClient(credentials, regionEndpoint);
                if (_lambdaClient == null)
                {
                    Debug.LogError("Failed to initialize Lambda client");
                    return false;
                }

                _executor = new LambdaExecutor(_lambdaClient);
                _permissionChecker = new LambdaPermissionChecker(_defaultFunctionName, _lambdaClient);
                if (ServiceController.Instance != null && ServiceController.Instance.CognitoManager != null)
                {
                    ((LambdaPermissionChecker)_permissionChecker).UserRole = ServiceController.Instance.GetUserRole();
                }

                _executor.OnExecutionComplete += (success, message, data) => OnExecutionComplete?.Invoke(success, message, data);

                _isInitialized = true;
                Debug.Log("LambdaManager initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize LambdaManager: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExecuteFunctionAsync(string functionName, object payload = null)
        {
            if (!_isInitialized) await InitializeAsync(null, null);
            return await _executor.ExecuteFunctionAsync(functionName, payload);
        }

        public async Task<bool> ExecuteFunctionWithContextAsync(string functionName, object additionalPayload = null)
        {
            if (!_isInitialized) await InitializeAsync(null, null);
            var userContext = new
            {
                username = ServiceController.Instance?.GetUserInfo().username ?? "unknown",
                userGroup = ServiceController.Instance?.GetUserRole() ?? "unknown",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                additionalData = additionalPayload
            };
            return await _executor.ExecuteFunctionAsync(functionName, userContext);
        }

        public async Task<List<string>> GetAvailableFunctionsAsync()
        {
            if (!_isInitialized) await InitializeAsync(null, null);
            return await _permissionChecker.GetAvailableFunctionsAsync();
        }

        public List<string> GetAvailableFunctions()
        {
            return GetAvailableFunctionsAsync().GetAwaiter().GetResult();
        }

        public async Task<bool> TestConnectivityAsync()
        {
            if (!_isInitialized) await InitializeAsync(null, null);
            return await _executor.TestConnectivityAsync();
        }

        private void OnDestroy()
        {
            LambdaClientFactory.DisposeClient();
        }
    }
}