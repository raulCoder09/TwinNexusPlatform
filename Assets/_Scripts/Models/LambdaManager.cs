using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon;
using Newtonsoft.Json;

namespace _Scripts.Models
{
    public class LambdaManager : MonoBehaviour
    {
        [Header("Lambda Configuration")]
        [SerializeField] private string defaultFunctionName = "test";
        
        // Lambda client
        private AmazonLambdaClient lambdaClient;
        
        // Events for Lambda responses
        public event Action<bool, string, object> OnLambdaExecutionComplete;
        
        // Singleton instance
        public static LambdaManager Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern
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

        /// <summary>
        /// Initializes Lambda client with current AWS credentials
        /// </summary>
        private void InitializeLambdaClient()
        {
            try
            {
                if (CognitoManager.Instance == null || CognitoManager.Instance.CurrentAWSCredentials == null)
                {
                    Debug.LogError("No AWS credentials available. Please authenticate first.");
                    return;
                }

                var regionEndpoint = RegionEndpoint.USEast1; // Use same region as Cognito
                lambdaClient = new AmazonLambdaClient(CognitoManager.Instance.CurrentAWSCredentials, regionEndpoint);
                
                Debug.Log("Lambda client initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize Lambda client: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes Lambda response and extracts meaningful data
        /// </summary>
        private object ProcessLambdaResponse(string responsePayload)
        {
            try
            {
                // Parse the main response as JObject for safe property access
                var mainResponse = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(responsePayload);
                
                // Check if it's an HTTP response format (has statusCode and body)
                if (mainResponse.ContainsKey("statusCode") && mainResponse.ContainsKey("body"))
                {
                    int statusCode = mainResponse["statusCode"].ToObject<int>();
                    Debug.Log($"HTTP Response - Status Code: {statusCode}");
                    
                    // Extract and parse the body
                    string bodyString = mainResponse["body"].ToString();
                    var bodyObject = JsonConvert.DeserializeObject(bodyString);
                    
                    // Return a clean response object
                    return new
                    {
                        statusCode = statusCode,
                        headers = mainResponse["headers"],
                        data = bodyObject, // This is the actual Lambda function response
                        rawBody = bodyString
                    };
                }
                else
                {
                    // If it's a direct response (not HTTP format), return as is
                    return mainResponse;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not process Lambda response as structured data: {ex.Message}");
                // Return the raw response if parsing fails
                return responsePayload;
            }
        }

        /// <summary>
        /// Executes a Lambda function with default settings
        /// </summary>
        public async Task<bool> ExecuteDefaultFunctionAsync()
        {
            return await ExecuteLambdaFunctionAsync(defaultFunctionName, new { test = "Hello from Unity!" });
        }

        /// <summary>
        /// Executes a Lambda function with custom payload
        /// </summary>
        public async Task<bool> ExecuteLambdaFunctionAsync(string functionName, object payload = null)
        {
            try
            {
                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to execute Lambda functions");
                    OnLambdaExecutionComplete?.Invoke(false, "User not authenticated", null);
                    return false;
                }
                
                if (!CanUserExecuteFunction(functionName))
                {
                    Debug.LogError($"User does not have permission to execute function: {functionName}");
                    OnLambdaExecutionComplete?.Invoke(false, $"Permission denied for function: {functionName}", null);
                    return false;
                }

                // Initialize Lambda client if needed
                if (lambdaClient == null)
                {
                    InitializeLambdaClient();
                    if (lambdaClient == null)
                    {
                        OnLambdaExecutionComplete?.Invoke(false, "Failed to initialize Lambda client", null);
                        return false;
                    }
                }

                // Prepare payload
                string jsonPayload = "{}";
                if (payload != null)
                {
                    jsonPayload = JsonConvert.SerializeObject(payload);
                }

                Debug.Log($"Executing Lambda function: {functionName}");
                Debug.Log($"Payload: {jsonPayload}");
                Debug.Log($"User group: {CognitoManager.Instance.CurrentUserGroup}");

                // Create invoke request
                var invokeRequest = new InvokeRequest
                {
                    FunctionName = functionName,
                    InvocationType = InvocationType.RequestResponse,
                    Payload = jsonPayload
                };

                // Execute function
                var response = await lambdaClient.InvokeAsync(invokeRequest);

                // Process response
                if (response.StatusCode == 200)
                {
                    var responsePayload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
                    Debug.Log($"Lambda execution successful. Response: {responsePayload}");

                    // Process the response using the new method
                    object processedResponse = ProcessLambdaResponse(responsePayload);
                    
                    OnLambdaExecutionComplete?.Invoke(true, "Function executed successfully", processedResponse);
                    return true;
                }
                else
                {
                    var errorPayload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
                    Debug.LogError($"Lambda execution failed. Status: {response.StatusCode}, Error: {errorPayload}");
                    OnLambdaExecutionComplete?.Invoke(false, $"Execution failed: {errorPayload}", null);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lambda execution error: {ex.Message}");
                OnLambdaExecutionComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        /// <summary>
        /// Executes Lambda function with user context (includes user info in payload)
        /// </summary>
        public async Task<bool> ExecuteFunctionWithUserContextAsync(string functionName, object additionalPayload = null)
        {
            try
            {
                var userContext = new
                {
                    username = CognitoManager.Instance.CurrentUsername,
                    userGroup = CognitoManager.Instance.CurrentUserGroup,
                    userGroups = CognitoManager.Instance.UserGroups,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    additionalData = additionalPayload
                };

                return await ExecuteLambdaFunctionAsync(functionName, userContext);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing function with user context: {ex.Message}");
                OnLambdaExecutionComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        /// <summary>
        /// Checks if user has permission to execute specific Lambda functions based on their group
        /// </summary>
        public bool CanUserExecuteFunction(string functionName)
        {
            if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                return false;

            var userRole = CognitoManager.Instance.GetUserRole();

            // Define function permissions based on user roles
            switch (userRole)
            {
                case "super-admin":
                    return true; // Super admin can execute all functions

                case "operators":
                    // Operators can execute operational and monitoring functions
                    return functionName == defaultFunctionName;

                case "students":
                    // Students can execute learning and personal functions
                    return functionName.Contains("Student") ||
                           functionName.Contains("Learning") ||
                           functionName == defaultFunctionName;

                case "usuarios-basicos":
                default:
                    // Basic users can only execute public/basic functions
                    return functionName.Contains("Public") || 
                           functionName.Contains("Basic") ||
                           functionName == defaultFunctionName;
            }
        }

        /// <summary>
        /// Gets available functions for current user based on their role
        /// </summary>
        public List<string> GetAvailableFunctions()
        {
            if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                return new List<string>();

            var userRole = CognitoManager.Instance.GetUserRole();
            var availableFunctions = new List<string>();

            switch (userRole)
            {
                case "super-admin":
                    availableFunctions.AddRange(new[]
                    {
                        defaultFunctionName,
                        "TwinNexus-UserManagement",
                        "TwinNexus-SystemAdmin",
                        "TwinNexus-IoTControl",
                        "TwinNexus-DataAnalytics",
                        "TwinNexus-S3Operations"
                    });
                    break;

                case "operadores":
                    availableFunctions.AddRange(new[]
                    {
                        defaultFunctionName,
                        "TwinNexus-IoTMonitor",
                        "TwinNexus-OperationalData",
                        "TwinNexus-DeviceControl"
                    });
                    break;

                case "estudiantes":
                    availableFunctions.AddRange(new[]
                    {
                        defaultFunctionName,
                        "TwinNexus-StudentPortal",
                        "TwinNexus-LearningContent",
                        "TwinNexus-PersonalData"
                    });
                    break;

                case "usuarios-basicos":
                default:
                    availableFunctions.AddRange(new[]
                    {
                        defaultFunctionName,
                        "TwinNexus-PublicInfo",
                        "TwinNexus-BasicOperations"
                    });
                    break;
            }

            return availableFunctions;
        }

        /// <summary>
        /// Test method to verify Lambda connectivity and permissions
        /// </summary>
        public async Task<bool> TestLambdaConnectivityAsync()
        {
            try
            {
                Debug.Log("Testing Lambda connectivity...");
                
                var testPayload = new
                {
                    test = true,
                    message = "Connectivity test from Unity",
                    userRole = CognitoManager.Instance?.GetUserRole() ?? "unknown",
                    timestamp = DateTime.UtcNow
                };

                return await ExecuteLambdaFunctionAsync(defaultFunctionName, testPayload);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lambda connectivity test failed: {ex.Message}");
                return false;
            }
        }

        private void OnDestroy()
        {
            lambdaClient?.Dispose();
        }
    }
}