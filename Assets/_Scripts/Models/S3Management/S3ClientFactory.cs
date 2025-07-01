using System;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using UnityEngine;

namespace _Scripts.Models.S3Management
{
    public class S3ClientFactory
    {
        private static AmazonS3Client _s3Client;
        private static readonly object _lock = new object();

        public static AmazonS3Client GetS3Client(AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            lock (_lock)
            {
                if (_s3Client == null)
                {
                    try
                    {
                        _s3Client = new AmazonS3Client(credentials ?? throw new ArgumentNullException(nameof(credentials)), 
                            regionEndpoint ?? RegionEndpoint.USEast1);
                        Debug.Log("S3 client created successfully");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to create S3 client: {ex.Message}");
                        throw; // Re-lanzar para que el llamador maneje el error
                    }
                }
                return _s3Client;
            }
        }

        public static void DisposeClient()
        {
            lock (_lock)
            {
                _s3Client?.Dispose();
                _s3Client = null;
                Debug.Log("S3 client disposed");
            }
        }
    }
}