using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace _Scripts.Models.LambdaManagement
{
    public class LambdaPermissionChecker : ILambdaService
    {
        private readonly string _defaultFunctionName;
        private string _userRole = "unknown"; // Valor por defecto, se inyectará dinámicamente
        public event Action<bool, string, object> OnExecutionComplete;

        public LambdaPermissionChecker(string defaultFunctionName)
        {
            _defaultFunctionName = defaultFunctionName ?? throw new ArgumentNullException(nameof(defaultFunctionName));
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
                "operators" => functionName == _defaultFunctionName,
                "students" => functionName.Contains("Student") || functionName.Contains("Learning") || functionName == _defaultFunctionName,
                _ => functionName.Contains("Public") || functionName.Contains("Basic") || functionName == _defaultFunctionName
            };
        }

        public List<string> GetAvailableFunctions()
        {
            return GetAvailableFunctions(_userRole);
        }

        private List<string> GetAvailableFunctions(string userRole)
        {
            var availableFunctions = new List<string>();
            switch (userRole.ToLower())
            {
                case "super-admin":
                    availableFunctions.AddRange(new[] { _defaultFunctionName, "TwinNexus-UserManagement", "TwinNexus-SystemAdmin", "TwinNexus-IoTControl", "TwinNexus-DataAnalytics", "TwinNexus-S3Operations" });
                    break;
                case "operators":
                    availableFunctions.AddRange(new[] { _defaultFunctionName, "TwinNexus-IoTMonitor", "TwinNexus-OperationalData", "TwinNexus-DeviceControl" });
                    break;
                case "students":
                    availableFunctions.AddRange(new[] { _defaultFunctionName, "TwinNexus-StudentPortal", "TwinNexus-LearningContent", "TwinNexus-PersonalData" });
                    break;
                default:
                    availableFunctions.AddRange(new[] { _defaultFunctionName, "TwinNexus-PublicInfo", "TwinNexus-BasicOperations" });
                    break;
            }
            return availableFunctions;
        }

        public Task<bool> ExecuteFunctionAsync(string functionName, object payload = null) => throw new NotImplementedException();
        public Task<bool> ExecuteFunctionWithContextAsync(string functionName, object additionalPayload = null) => throw new NotImplementedException();
        public Task<bool> TestConnectivityAsync() => throw new NotImplementedException();
    }
}