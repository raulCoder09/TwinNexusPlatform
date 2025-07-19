using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using UnityEngine;

namespace _Scripts.Models.LambdaManagement
{
    public class LambdaPermissionChecker : ILambdaService
    {
        private readonly string _defaultFunctionName;
        private string _userRole = "unknown";
        private readonly AmazonLambdaClient _lambdaClient;
        public event Action<bool, string, object> OnExecutionComplete;

        public LambdaPermissionChecker(string defaultFunctionName, AmazonLambdaClient lambdaClient)
        {
            _defaultFunctionName = defaultFunctionName ?? throw new ArgumentNullException(nameof(defaultFunctionName));
            _lambdaClient = lambdaClient ?? throw new ArgumentNullException(nameof(lambdaClient));
        }

        public string UserRole
        {
            get => _userRole;
            set => _userRole = value ?? "unknown";
        }

        public bool CanUserExecuteFunction(string functionName)
        {
            return CanUserExecuteFunction(functionName, _userRole);
        }

        private bool CanUserExecuteFunction(string functionName, string userRole)
        {
            return userRole switch
            {
                "super-admin" => true,
                "operators" => functionName == _defaultFunctionName || functionName.Contains("IoT") || functionName.Contains("Operational"),
                "students" => functionName.Contains("Student") || functionName.Contains("Learning") || functionName == _defaultFunctionName,
                _ => functionName.Contains("Public") || functionName.Contains("Basic") || functionName == _defaultFunctionName
            };
        }

        public async Task<List<string>> GetAvailableFunctionsAsync()
        {
            try
            {
                var functions = new List<string>();
                string nextMarker = null;

                do
                {
                    var request = new ListFunctionsRequest { Marker = nextMarker };
                    var response = await _lambdaClient.ListFunctionsAsync(request);
                    foreach (var function in response.Functions)
                    {
                        if (CanUserExecuteFunction(function.FunctionName, _userRole))
                        {
                            functions.Add(function.FunctionName);
                        }
                    }
                    nextMarker = response.NextMarker;
                } while (nextMarker != null);

                Debug.Log($"[LambdaPermissionChecker] Retrieved {functions.Count} available functions for role {_userRole}");
                return functions;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LambdaPermissionChecker] Error listing functions: {ex.Message}");
                return new List<string> { _defaultFunctionName };
            }
        }

        public List<string> GetAvailableFunctions()
        {
            // Mantener compatibilidad con la interfaz original
            return GetAvailableFunctionsAsync().GetAwaiter().GetResult();
        }

        public Task<bool> ExecuteFunctionAsync(string functionName, object payload = null) => throw new NotImplementedException();
        public Task<bool> ExecuteFunctionWithContextAsync(string functionName, object additionalPayload = null) => throw new NotImplementedException();
        public Task<bool> TestConnectivityAsync() => throw new NotImplementedException();
    }
}