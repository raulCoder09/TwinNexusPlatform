using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Newtonsoft.Json;
using UnityEngine;
using _Scripts.Controller;
using _Scripts.Controllers.ServiceManagement;

namespace _Scripts.Models.LambdaManagement
{
    public class LambdaExecutor : ILambdaService
    {
        private readonly AmazonLambdaClient _lambdaClient;
        public event Action<bool, string, object> OnExecutionComplete;

        public LambdaExecutor(AmazonLambdaClient lambdaClient)
        {
            _lambdaClient = lambdaClient ?? throw new ArgumentNullException(nameof(lambdaClient));
        }

        private object ProcessLambdaResponse(string responsePayload)
        {
            try
            {
                var mainResponse = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(responsePayload);
                if (mainResponse.ContainsKey("statusCode") && mainResponse.ContainsKey("body"))
                {
                    int statusCode = mainResponse["statusCode"].ToObject<int>();
                    string bodyString = mainResponse["body"].ToString();
                    var bodyObject = JsonConvert.DeserializeObject(bodyString);
                    return new { statusCode, data = bodyObject, rawBody = bodyString };
                }
                return mainResponse;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not process Lambda response: {ex.Message}");
                return responsePayload;
            }
        }

        public async Task<bool> ExecuteFunctionAsync(string functionName, object payload = null)
        {
            try
            {
                string jsonPayload = payload != null ? JsonConvert.SerializeObject(payload) : "{}";
                Debug.Log($"Executing Lambda function: {functionName}, Payload: {jsonPayload}");

                var request = new InvokeRequest
                {
                    FunctionName = functionName,
                    InvocationType = InvocationType.RequestResponse,
                    Payload = jsonPayload
                };

                var response = await _lambdaClient.InvokeAsync(request);
                if (response.StatusCode == 200)
                {
                    string responsePayload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
                    Debug.Log($"Lambda execution successful. Response: {responsePayload}");
                    OnExecutionComplete?.Invoke(true, "Function executed successfully", ProcessLambdaResponse(responsePayload));
                    return true;
                }
                string errorPayload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
                Debug.LogError($"Lambda execution failed. Status: {response.StatusCode}, Error: {errorPayload}");
                OnExecutionComplete?.Invoke(false, $"Execution failed: {errorPayload}", null);
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lambda execution error: {ex.Message}");
                OnExecutionComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        public async Task<bool> ExecuteFunctionWithContextAsync(string functionName, object additionalPayload = null)
        {
            try
            {
                var userContext = new
                {
                    username = ServiceController.Instance?.GetUserInfo().username ?? "unknown",
                    userGroup = ServiceController.Instance?.GetUserRole() ?? "unknown",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    additionalData = additionalPayload
                };
                string jsonPayload = JsonConvert.SerializeObject(userContext);
                Debug.Log($"Executing Lambda function with context: {functionName}, Payload: {jsonPayload}");

                var request = new InvokeRequest
                {
                    FunctionName = functionName,
                    InvocationType = InvocationType.RequestResponse,
                    Payload = jsonPayload
                };

                var response = await _lambdaClient.InvokeAsync(request);
                if (response.StatusCode == 200)
                {
                    string responsePayload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
                    Debug.Log($"Lambda execution with context successful. Response: {responsePayload}");
                    OnExecutionComplete?.Invoke(true, "Function executed with context successfully", ProcessLambdaResponse(responsePayload));
                    return true;
                }
                string errorPayload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
                Debug.LogError($"Lambda execution with context failed. Status: {response.StatusCode}, Error: {errorPayload}");
                OnExecutionComplete?.Invoke(false, $"Execution failed: {errorPayload}", null);
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lambda execution with context error: {ex.Message}");
                OnExecutionComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        public List<string> GetAvailableFunctions()
        {
            throw new NotImplementedException(); // Delegated to LambdaPermissionChecker
        }

        public async Task<List<string>> GetAvailableFunctionsAsync()
        {
            throw new NotImplementedException(); // Delegated to LambdaPermissionChecker
        }

        public async Task<bool> TestConnectivityAsync()
        {
            try
            {
                var payload = new { test = true, message = "Connectivity test" };
                string jsonPayload = JsonConvert.SerializeObject(payload);
                Debug.Log($"Testing Lambda connectivity with test function, Payload: {jsonPayload}");

                var request = new InvokeRequest
                {
                    FunctionName = "test", // Default test function
                    InvocationType = InvocationType.RequestResponse,
                    Payload = jsonPayload
                };

                var response = await _lambdaClient.InvokeAsync(request);
                if (response.StatusCode == 200)
                {
                    string responsePayload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
                    Debug.Log($"Lambda connectivity test successful. Response: {responsePayload}");
                    OnExecutionComplete?.Invoke(true, "Connectivity test successful", ProcessLambdaResponse(responsePayload));
                    return true;
                }
                string errorPayload = System.Text.Encoding.UTF8.GetString(response.Payload.ToArray());
                Debug.LogError($"Lambda connectivity test failed. Status: {response.StatusCode}, Error: {errorPayload}");
                OnExecutionComplete?.Invoke(false, $"Connectivity test failed: {errorPayload}", null);
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Lambda connectivity test error: {ex.Message}");
                OnExecutionComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }
    }
}