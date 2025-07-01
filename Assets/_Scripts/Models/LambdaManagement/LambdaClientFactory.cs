using System;
using Amazon;
using Amazon.Lambda;
using Amazon.Runtime;
using UnityEngine;

namespace _Scripts.Models.LambdaManagement
{
    public class LambdaClientFactory
    {
        private static AmazonLambdaClient _lambdaClient;
        private static readonly object _lock = new object();

        public static AmazonLambdaClient GetLambdaClient(AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            lock (_lock)
            {
                if (_lambdaClient == null)
                {
                    try
                    {
                        _lambdaClient = new AmazonLambdaClient(credentials ?? throw new ArgumentNullException(nameof(credentials)),
                            regionEndpoint ?? RegionEndpoint.USEast1);
                        Debug.Log("Lambda client created successfully");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to create Lambda client: {ex.Message}");
                        throw;
                    }
                }
                return _lambdaClient;
            }
        }

        public static void DisposeClient()
        {
            lock (_lock)
            {
                _lambdaClient?.Dispose();
                _lambdaClient = null;
                Debug.Log("Lambda client disposed");
            }
        }
    }
}