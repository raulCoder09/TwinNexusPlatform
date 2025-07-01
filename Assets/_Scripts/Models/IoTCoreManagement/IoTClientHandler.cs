using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.IoT;
using Amazon.Runtime;
using UnityEngine;

namespace _Scripts.Models.IoTCoreManagement
{
    public class IoTClientHandler
    {
        private readonly bool enableDebugLogs = true;
        private AmazonIoTClient iotClient;
        private readonly RegionEndpoint regionEndpoint;

        public IoTClientHandler()
        {
            regionEndpoint = RegionEndpoint.USEast1;
        }

        public async Task<bool> InitializeIoTClientAsync(AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            try
            {
                if (credentials == null)
                {
                    LogError("No AWS credentials available. Please authenticate first.");
                    return false;
                }

                // Usar la región proporcionada o la predeterminada si no se especifica
                var finalRegion = regionEndpoint ?? this.regionEndpoint;
                iotClient = new AmazonIoTClient(credentials, finalRegion);
                LogDebug("IoT Core client initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize IoT client: {ex.Message}");
                return false;
            }
        }

        public AmazonIoTClient GetClient()
        {
            if (iotClient == null)
            {
                LogWarning("IoT client is not initialized. Initialization required before use.");
                // No intentará inicializarse automáticamente; depende de InitializeIoTClientAsync
            }
            return iotClient;
        }

        public bool IsClientInitialized()
        {
            return iotClient != null;
        }

        public void DisposeClient()
        {
            try
            {
                iotClient?.Dispose();
                iotClient = null;
                LogDebug("IoT client disposed successfully");
            }
            catch (Exception ex)
            {
                LogError($"Error disposing IoT client: {ex.Message}");
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[IoTClientHandler] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[IoTClientHandler] {message}");
        }

        private void LogWarning(string message)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[IoTClientHandler] {message}");
        }
    }
}