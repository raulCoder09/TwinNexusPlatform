using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Amazon.IoT;
using Amazon.IoT.Model;
using Amazon.IotData;
using Amazon.IotData.Model;
using Amazon;
using _Scripts.Models;

namespace _Scripts.Models
{
    public class IoTCoreManager : MonoBehaviour
    {
        [Header("AWS IoT Core Configuration")]
        [SerializeField] private string domainName = "d06815421so7uq8jzy8ln-ats.iot.us-east-1.amazonaws.com";
        [SerializeField] private string domainArn = "arn:aws:iot:us-east-1:156041417101:domainconfiguration/TwinNexusPlatformDomainConfiguration/axnbn";
        
        [Header("Create Thing Configuration")]
        [SerializeField] private string newThingName = "Unity-Test-Device";
        [SerializeField] private string newThingType = ""; // Optional
        
        [Header("Get Thing Configuration")]
        [SerializeField] private string thingNameToGet = ""; // Thing específico a consultar
        
        // AWS IoT clients
        private AmazonIoTClient iotClient;
        
        // Events for IoT operations
        public event Action<bool, string, List<ThingInfo>> OnThingsListed; // success, message, things
        public event Action<bool, string, string> OnThingCreated; // success, message, thingName
        public event Action<bool, string, ThingInfo> OnThingRetrieved; // success, message, thingInfo
        
        // Singleton instance
        public static IoTCoreManager Instance { get; private set; }

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
        /// Initializes IoT client with current AWS credentials
        /// </summary>
        private void InitializeIoTClient()
        {
            try
            {
                if (CognitoManager.Instance == null || CognitoManager.Instance.CurrentAWSCredentials == null)
                {
                    Debug.LogError("No AWS credentials available. Please authenticate first.");
                    return;
                }

                var regionEndpoint = RegionEndpoint.USEast1; // Same region as other services
                iotClient = new AmazonIoTClient(CognitoManager.Instance.CurrentAWSCredentials, regionEndpoint);
                
                Debug.Log("IoT Core client initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize IoT Core client: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Lists all Things in the AWS IoT registry
        /// </summary>
        public async Task<List<ThingInfo>> ListThingsAsync()
        {
            try
            {
                // Check if user is authenticated
                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to list IoT Things");
                    OnThingsListed?.Invoke(false, "User not authenticated", new List<ThingInfo>());
                    return new List<ThingInfo>();
                }

                // Initialize IoT client if needed
                if (iotClient == null)
                {
                    InitializeIoTClient();
                    if (iotClient == null)
                    {
                        OnThingsListed?.Invoke(false, "Failed to initialize IoT client", new List<ThingInfo>());
                        return new List<ThingInfo>();
                    }
                }

                Debug.Log("📋 Listing IoT Things...");

                var listThingsRequest = new ListThingsRequest
                {
                    MaxResults = 50 // Limit to 50 things for now
                };

                var response = await iotClient.ListThingsAsync(listThingsRequest);
                var thingInfoList = new List<ThingInfo>();

                foreach (var thing in response.Things)
                {
                    var thingInfo = new ThingInfo
                    {
                        ThingName = thing.ThingName,
                        ThingTypeName = thing.ThingTypeName,
                        ThingArn = thing.ThingArn,
                        Version = thing.Version ?? 0,
                        CreationDate = DateTime.UtcNow, // AWS IoT Thing doesn't expose creation date directly
                        LastModifiedDate = DateTime.UtcNow // We'll get this from describe operation later
                    };

                    // Add attributes if available
                    if (thing.Attributes != null && thing.Attributes.Count > 0)
                    {
                        thingInfo.AttributesInfo = "";
                        foreach (var attr in thing.Attributes)
                        {
                            thingInfo.AttributesInfo += $"{attr.Key}={attr.Value}; ";
                        }
                    }

                    thingInfoList.Add(thingInfo);
                }

                Debug.Log($"📊 Found {thingInfoList.Count} IoT Things");
                
                // Display results in console
                if (thingInfoList.Count > 0)
                {
                    Debug.Log($"🔍 IoT Things encontrados:");
                    for (int i = 0; i < thingInfoList.Count; i++)
                    {
                        var thing = thingInfoList[i];
                        Debug.Log($"   {i + 1}. 📱 {thing.ThingName}");
                        if (!string.IsNullOrEmpty(thing.ThingTypeName))
                        {
                            Debug.Log($"      🏷️ Type: {thing.ThingTypeName}");
                        }
                        Debug.Log($"      🕐 Created: {thing.CreationDate:yyyy-MM-dd HH:mm:ss}");
                        Debug.Log($"      📍 ARN: {thing.ThingArn}");
                        if (!string.IsNullOrEmpty(thing.AttributesInfo))
                        {
                            Debug.Log($"      📋 Attributes: {thing.AttributesInfo}");
                        }
                        Debug.Log("      ---");
                    }
                }
                else
                {
                    Debug.Log("📭 No IoT Things found in registry");
                }

                OnThingsListed?.Invoke(true, $"Found {thingInfoList.Count} Things", thingInfoList);
                return thingInfoList;
            }
            catch (Exception ex)
            {
                Debug.LogError($"IoT List Things error: {ex.Message}");
                OnThingsListed?.Invoke(false, ex.Message, new List<ThingInfo>());
                return new List<ThingInfo>();
            }
        }

        /// <summary>
        /// Creates a new Thing in AWS IoT Core
        /// </summary>
        public async Task<bool> CreateThingAsync(string thingName = null, string thingType = null)
        {
            try
            {
                // Use configured values if not provided
                string finalThingName = thingName ?? newThingName;
                string finalThingType = thingType ?? newThingType;
                
                // Add timestamp to make it unique
                if (string.IsNullOrEmpty(thingName))
                {
                    finalThingName = $"{newThingName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                }

                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to create Things");
                    OnThingCreated?.Invoke(false, "User not authenticated", finalThingName);
                    return false;
                }

                if (iotClient == null)
                {
                    InitializeIoTClient();
                    if (iotClient == null)
                    {
                        OnThingCreated?.Invoke(false, "Failed to initialize IoT client", finalThingName);
                        return false;
                    }
                }

                Debug.Log($"📱 Creating IoT Thing: {finalThingName}");
                if (!string.IsNullOrEmpty(finalThingType))
                {
                    Debug.Log($"🏷️ Thing Type: {finalThingType}");
                }

                var createThingRequest = new CreateThingRequest
                {
                    ThingName = finalThingName
                };

                // Add thing type if specified
                if (!string.IsNullOrEmpty(finalThingType))
                {
                    createThingRequest.ThingTypeName = finalThingType;
                }

                var response = await iotClient.CreateThingAsync(createThingRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.Log($"✅ Thing created successfully: {finalThingName}");
                    Debug.Log($"📍 Thing ARN: {response.ThingArn}");
                    OnThingCreated?.Invoke(true, "Thing created successfully", finalThingName);
                    return true;
                }
                else
                {
                    Debug.LogError($"❌ Failed to create Thing: {response.HttpStatusCode}");
                    OnThingCreated?.Invoke(false, $"Creation failed: {response.HttpStatusCode}", finalThingName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"IoT Create Thing error: {ex.Message}");
                OnThingCreated?.Invoke(false, ex.Message, thingName ?? newThingName);
                return false;
            }
        }

        /// <summary>
        /// Gets detailed information about a specific Thing
        /// </summary>
        public async Task<ThingInfo> GetThingAsync(string thingName = null)
        {
            try
            {
                // Use configured value if not provided
                string finalThingName = thingName ?? thingNameToGet;
                
                if (string.IsNullOrEmpty(finalThingName))
                {
                    Debug.LogError("Thing name is required");
                    OnThingRetrieved?.Invoke(false, "Thing name is required", null);
                    return null;
                }

                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to get Thing details");
                    OnThingRetrieved?.Invoke(false, "User not authenticated", null);
                    return null;
                }

                if (iotClient == null)
                {
                    InitializeIoTClient();
                    if (iotClient == null)
                    {
                        OnThingRetrieved?.Invoke(false, "Failed to initialize IoT client", null);
                        return null;
                    }
                }

                Debug.Log($"🔍 Getting Thing details: {finalThingName}");

                var describeThingRequest = new DescribeThingRequest
                {
                    ThingName = finalThingName
                };

                var response = await iotClient.DescribeThingAsync(describeThingRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    var thingInfo = new ThingInfo
                    {
                        ThingName = response.ThingName,
                        ThingTypeName = response.ThingTypeName,
                        ThingArn = response.ThingArn,
                        Version = response.Version ?? 0,
                        CreationDate = DateTime.UtcNow, // DescribeThingResponse doesn't expose creation date
                        LastModifiedDate = DateTime.UtcNow // DescribeThingResponse doesn't expose modification date
                    };

                    // Add attributes if available
                    if (response.Attributes != null && response.Attributes.Count > 0)
                    {
                        thingInfo.AttributesInfo = "";
                        foreach (var attr in response.Attributes)
                        {
                            thingInfo.AttributesInfo += $"{attr.Key}={attr.Value}; ";
                        }
                        Debug.Log($"📋 Attributes found: {thingInfo.AttributesInfo}");
                    }
                    else
                    {
                        Debug.Log("📭 No attributes found for this Thing");
                    }

                    Debug.Log($"✅ Thing details retrieved successfully:");
                    Debug.Log($"   📱 Name: {thingInfo.ThingName}");
                    Debug.Log($"   🏷️ Type: {thingInfo.ThingTypeName ?? "No type"}");
                    Debug.Log($"   📍 ARN: {thingInfo.ThingArn}");
                    Debug.Log($"   🔢 Version: {thingInfo.Version}");
                    Debug.Log($"   🕐 Created: {thingInfo.CreationDate:yyyy-MM-dd HH:mm:ss}");
                    Debug.Log($"   🕐 Modified: {thingInfo.LastModifiedDate:yyyy-MM-dd HH:mm:ss}");

                    OnThingRetrieved?.Invoke(true, "Thing details retrieved successfully", thingInfo);
                    return thingInfo;
                }
                else
                {
                    Debug.LogError($"❌ Failed to get Thing: {response.HttpStatusCode}");
                    OnThingRetrieved?.Invoke(false, $"Failed to get Thing: {response.HttpStatusCode}", null);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"IoT Get Thing error: {ex.Message}");
                OnThingRetrieved?.Invoke(false, ex.Message, null);
                return null;
            }
        }
        
        /// <summary>
        /// Test method to verify IoT connectivity
        /// </summary>
        public async Task<bool> TestIoTConnectivityAsync()
        {
            try
            {
                Debug.Log("🧪 Testing IoT Core connectivity...");
                
                var things = await ListThingsAsync();
                
                if (things.Count >= 0) // Even 0 is a successful connection
                {
                    Debug.Log("✅ IoT Core connectivity test successful");
                    return true;
                }
                else
                {
                    Debug.LogWarning("⚠️ IoT Core connectivity test - no data returned");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"IoT connectivity test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test method to create Thing using Inspector configuration
        /// </summary>
        public async Task<bool> CreateConfiguredThingAsync()
        {
            return await CreateThingAsync();
        }

        /// <summary>
        /// Test method to get Thing using Inspector configuration
        /// </summary>
        public async Task<ThingInfo> GetConfiguredThingAsync()
        {
            return await GetThingAsync();
        }

        private void OnDestroy()
        {
            iotClient?.Dispose();
        }
    }

    #region Data Classes

    [System.Serializable]
    public class ThingInfo
    {
        public string ThingName;
        public string ThingTypeName;
        public string ThingArn;
        public long Version;
        public DateTime CreationDate;
        public DateTime LastModifiedDate;
        public string AttributesInfo;
    }

    #endregion
}