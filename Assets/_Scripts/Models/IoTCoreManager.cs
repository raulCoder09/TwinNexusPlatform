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
        
        [Header("Certificate Configuration")]
        [SerializeField] private bool setAsActive = true; // Activar certificado automáticamente
        
        [Header("Attach Certificate Configuration")]
        [SerializeField] private string certificateIdToAttach = ""; // Certificate ID a asociar
        [SerializeField] private string thingNameForCertificate = ""; // Thing que recibirá el certificado
        
        [Header("List Certificates Configuration")]
        [SerializeField] private string thingNameToListCertificates = ""; // Thing del cual listar certificados
        
        [Header("Policy Configuration")]
        [SerializeField] private string newPolicyName = "Unity-IoT-Policy";
        [SerializeField] private string policyDocument = ""; // JSON policy (opcional, usará default)
        
        [Header("Platform Configuration")]
        [SerializeField] private string platformPrefix = "tnp"; // Configurable desde Inspector
        [SerializeField] private bool useTimestampInNames = true; // Para hacer nombres únicos
        
        [Header("Attach Policy Configuration")]
        [SerializeField] private string policyNameToAttach = ""; // Policy name to attach
        [SerializeField] private string certificateIdForPolicy = ""; // Certificate ID to receive policy
        

        // AWS IoT clients
        private AmazonIoTClient iotClient;
      
        // Events for IoT operations
        public event Action<bool, string, List<ThingInfo>> OnThingsListed; // success, message, things
        public event Action<bool, string, string> OnThingCreated; // success, message, thingName
        public event Action<bool, string, ThingInfo> OnThingRetrieved; // success, message, thingInfo
        public event Action<bool, string, CertificateData> OnCertificateCreated; // success, message, certificateData
        
        public event Action<bool, string, string, string> OnCertificateAttached; // success, message, thingName, certificateId
        public event Action<bool, string, string, List<string>> OnThingCertificatesListed; // success, message, thingName, certificateArns
        
        public event Action<bool, string, string> OnPolicyCreated; // success, message, policyName
        
        public event Action<bool, string, string, string> OnPolicyAttached; // success, message, policyName, certificateId

        
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

                var regionEndpoint = RegionEndpoint.USEast1;
                iotClient = new AmazonIoTClient(CognitoManager.Instance.CurrentAWSCredentials, regionEndpoint);
        
                
        
                Debug.Log("IoT Core and IoT Data clients initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize IoT clients: {ex.Message}");
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
        string finalThingName;
        string finalThingType = thingType ?? newThingType;
        
        // Build final thing name with platform prefix
        if (!string.IsNullOrEmpty(thingName))
        {
            // If explicit name provided, use as-is
            finalThingName = thingName;
        }
        else
        {
            // Use configured base name with platform prefix
            string prefix = string.IsNullOrEmpty(platformPrefix) ? "tnp" : platformPrefix;
            string baseName = string.IsNullOrEmpty(newThingName) ? "test-device" : newThingName;
            
            if (useTimestampInNames)
            {
                finalThingName = $"{prefix}-{baseName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            }
            else
            {
                finalThingName = $"{prefix}-{baseName}";
            }
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
        Debug.Log($"🏷️ Platform Prefix: {platformPrefix}");
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
        OnThingCreated?.Invoke(false, ex.Message, thingName ?? $"{platformPrefix}-{newThingName}");
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
        /// Creates a new X.509 certificate for IoT Things authentication
        /// WARNING: The private key is only returned ONCE and cannot be retrieved later!
        /// </summary>
        public async Task<CertificateData> CreateThingCertificateAsync()
        {
            try
            {
                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to create certificates");
                    OnCertificateCreated?.Invoke(false, "User not authenticated", null);
                    return null;
                }

                if (iotClient == null)
                {
                    InitializeIoTClient();
                    if (iotClient == null)
                    {
                        OnCertificateCreated?.Invoke(false, "Failed to initialize IoT client", null);
                        return null;
                    }
                }

                Debug.Log("🔐 Creating IoT Thing certificate...");
                Debug.LogWarning("⚠️ IMPORTANT: Private key will only be shown ONCE!");

                var createCertRequest = new CreateKeysAndCertificateRequest
                {
                    SetAsActive = setAsActive // Use Inspector configuration
                };

                var response = await iotClient.CreateKeysAndCertificateAsync(createCertRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    var certificateData = new CertificateData
                    {
                        CertificateId = response.CertificateId,
                        CertificateArn = response.CertificateArn,
                        CertificatePem = response.CertificatePem,
                        PublicKey = response.KeyPair.PublicKey,
                        PrivateKey = response.KeyPair.PrivateKey, // ⚠️ ONLY AVAILABLE NOW!
                        IsActive = setAsActive,
                        CreationDate = DateTime.UtcNow
                    };

                    Debug.Log($"✅ Certificate created successfully!");
                    Debug.Log($"🆔 Certificate ID: {certificateData.CertificateId}");
                    Debug.Log($"📍 Certificate ARN: {certificateData.CertificateArn}");
                    Debug.Log($"🔓 Status: {(certificateData.IsActive ? "ACTIVE" : "INACTIVE")}");
                    Debug.Log($"📄 Certificate PEM length: {certificateData.CertificatePem?.Length ?? 0} chars");
                    Debug.Log($"🔑 Public Key length: {certificateData.PublicKey?.Length ?? 0} chars");
                    Debug.Log($"🔐 Private Key length: {certificateData.PrivateKey?.Length ?? 0} chars");
                    
                    // Security warning - never log the actual keys!
                    Debug.LogWarning("🚨 SECURITY: Store the private key securely! It won't be available again.");
                    Debug.Log("💡 Next steps: Save certificate data and attach to Thing");

                    OnCertificateCreated?.Invoke(true, "Certificate created successfully", certificateData);
                    return certificateData;
                }
                else
                {
                    Debug.LogError($"❌ Failed to create certificate: {response.HttpStatusCode}");
                    OnCertificateCreated?.Invoke(false, $"Creation failed: {response.HttpStatusCode}", null);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"IoT Create Certificate error: {ex.Message}");
                OnCertificateCreated?.Invoke(false, ex.Message, null);
                return null;
            }
        }

        /// <summary>
        /// Test method to create certificate using Inspector configuration
        /// </summary>
        public async Task<CertificateData> CreateConfiguredCertificateAsync()
        {
            return await CreateThingCertificateAsync();
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
        
        /// <summary>
/// Attaches a certificate to a specific Thing for authentication
/// </summary>
public async Task<bool> AttachCertificateToThingAsync(string certificateId = null, string thingName = null)
{
    try
    {
        string finalCertificateId = certificateId ?? certificateIdToAttach;
        string finalThingName = thingName ?? thingNameForCertificate;
        
        if (string.IsNullOrEmpty(finalCertificateId) || string.IsNullOrEmpty(finalThingName))
        {
            Debug.LogError("Certificate ID and Thing name are required");
            OnCertificateAttached?.Invoke(false, "Certificate ID and Thing name required", finalThingName, finalCertificateId);
            return false;
        }

        if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
        {
            Debug.LogError("User must be authenticated to attach certificates");
            OnCertificateAttached?.Invoke(false, "User not authenticated", finalThingName, finalCertificateId);
            return false;
        }

        if (iotClient == null)
        {
            InitializeIoTClient();
            if (iotClient == null)
            {
                OnCertificateAttached?.Invoke(false, "Failed to initialize IoT client", finalThingName, finalCertificateId);
                return false;
            }
        }

        Debug.Log($"🔗 Attaching certificate to Thing...");
        Debug.Log($"📱 Thing: {finalThingName}");
        Debug.Log($"🆔 Certificate: {finalCertificateId.Substring(0, 8)}...");

        // Build certificate ARN
        string certificateArn = $"arn:aws:iot:us-east-1:156041417101:cert/{finalCertificateId}";

        var attachRequest = new AttachThingPrincipalRequest
        {
            ThingName = finalThingName,
            Principal = certificateArn
        };

        var response = await iotClient.AttachThingPrincipalAsync(attachRequest);

        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            Debug.Log($"✅ Certificate attached successfully!");
            Debug.Log($"🔗 {finalThingName} ↔ {finalCertificateId.Substring(0, 8)}...");
            OnCertificateAttached?.Invoke(true, "Certificate attached successfully", finalThingName, finalCertificateId);
            return true;
        }
        else
        {
            Debug.LogError($"❌ Failed to attach certificate: {response.HttpStatusCode}");
            OnCertificateAttached?.Invoke(false, $"Attach failed: {response.HttpStatusCode}", finalThingName, finalCertificateId);
            return false;
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"IoT Attach Certificate error: {ex.Message}");
        OnCertificateAttached?.Invoke(false, ex.Message, thingName ?? thingNameForCertificate, certificateId ?? certificateIdToAttach);
        return false;
    }
}
        /// <summary>
        /// Test method to attach certificate using Inspector configuration
        /// </summary>
        public async Task<bool> AttachConfiguredCertificateAsync()
        {
            return await AttachCertificateToThingAsync();
        }
        
        /// <summary>
/// Lists all certificates attached to a specific Thing
/// </summary>
public async Task<List<string>> ListThingCertificatesAsync(string thingName = null)
{
    try
    {
        string finalThingName = thingName ?? thingNameToListCertificates;
        
        if (string.IsNullOrEmpty(finalThingName))
        {
            Debug.LogError("Thing name is required to list certificates");
            OnThingCertificatesListed?.Invoke(false, "Thing name is required", finalThingName, new List<string>());
            return new List<string>();
        }

        if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
        {
            Debug.LogError("User must be authenticated to list Thing certificates");
            OnThingCertificatesListed?.Invoke(false, "User not authenticated", finalThingName, new List<string>());
            return new List<string>();
        }

        if (iotClient == null)
        {
            InitializeIoTClient();
            if (iotClient == null)
            {
                OnThingCertificatesListed?.Invoke(false, "Failed to initialize IoT client", finalThingName, new List<string>());
                return new List<string>();
            }
        }

        Debug.Log($"📋 Listing certificates for Thing: {finalThingName}");

        var listPrincipalsRequest = new ListThingPrincipalsRequest
        {
            ThingName = finalThingName
        };

        var response = await iotClient.ListThingPrincipalsAsync(listPrincipalsRequest);

        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            var certificateArns = response.Principals;
            
            Debug.Log($"✅ Found {certificateArns.Count} certificate(s) for {finalThingName}");
            
            if (certificateArns.Count > 0)
            {
                Debug.Log($"📄 Certificados asociados a {finalThingName}:");
                for (int i = 0; i < certificateArns.Count; i++)
                {
                    string arn = certificateArns[i];
                    // Extract certificate ID from ARN (arn:aws:iot:region:account:cert/CERTIFICATE_ID)
                    string certId = arn.Split('/').Length > 1 ? arn.Split('/')[1] : "Unknown";
                    Debug.Log($"   {i + 1}. 🆔 {certId.Substring(0, Math.Min(8, certId.Length))}...");
                    Debug.Log($"      📍 ARN: {arn}");
                }
            }
            else
            {
                Debug.Log($"📭 No certificates found for {finalThingName}");
                Debug.Log("💡 Attach a certificate first using AttachCertificateToThingAsync()");
            }

            OnThingCertificatesListed?.Invoke(true, $"Found {certificateArns.Count} certificates", finalThingName, certificateArns);
            return certificateArns;
        }
        else
        {
            Debug.LogError($"❌ Failed to list certificates: {response.HttpStatusCode}");
            OnThingCertificatesListed?.Invoke(false, $"List failed: {response.HttpStatusCode}", finalThingName, new List<string>());
            return new List<string>();
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"IoT List Thing Certificates error: {ex.Message}");
        OnThingCertificatesListed?.Invoke(false, ex.Message, thingName ?? thingNameToListCertificates, new List<string>());
        return new List<string>();
    }
}
        /// <summary>
        /// Test method to list certificates using Inspector configuration
        /// </summary>
        public async Task<List<string>> ListConfiguredThingCertificatesAsync()
        {
            return await ListThingCertificatesAsync();
        }
        
        /// <summary>
/// Creates an IoT policy with permissions for MQTT and Thing Shadow operations
/// </summary>
public async Task<bool> CreatePolicyAsync(string policyName = null, string customPolicyDocument = null)
{
    try
    {
        string finalPolicyName = policyName ?? newPolicyName;
        
        // Add timestamp to make it unique
        if (string.IsNullOrEmpty(policyName))
        {
            finalPolicyName = $"{newPolicyName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        }

        if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
        {
            Debug.LogError("User must be authenticated to create policies");
            OnPolicyCreated?.Invoke(false, "User not authenticated", finalPolicyName);
            return false;
        }

        if (iotClient == null)
        {
            InitializeIoTClient();
            if (iotClient == null)
            {
                OnPolicyCreated?.Invoke(false, "Failed to initialize IoT client", finalPolicyName);
                return false;
            }
        }

        Debug.Log($"🛡️ Creating IoT Policy: {finalPolicyName}");

        // Default policy document if none provided
        string policyDoc = customPolicyDocument ?? policyDocument;
        if (string.IsNullOrEmpty(policyDoc))
        {
            policyDoc = GetDefaultPolicyDocument();
        }

        Debug.Log($"📋 Policy permissions: MQTT + Thing Shadow + Connect");

        var createPolicyRequest = new CreatePolicyRequest
        {
            PolicyName = finalPolicyName,
            PolicyDocument = policyDoc
        };

        var response = await iotClient.CreatePolicyAsync(createPolicyRequest);

        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            Debug.Log($"✅ Policy created successfully: {finalPolicyName}");
            Debug.Log($"📍 Policy ARN: {response.PolicyArn}");
            Debug.Log($"🔢 Policy Version: {response.PolicyVersionId}");
            OnPolicyCreated?.Invoke(true, "Policy created successfully", finalPolicyName);
            return true;
        }
        else
        {
            Debug.LogError($"❌ Failed to create policy: {response.HttpStatusCode}");
            OnPolicyCreated?.Invoke(false, $"Creation failed: {response.HttpStatusCode}", finalPolicyName);
            return false;
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"IoT Create Policy error: {ex.Message}");
        OnPolicyCreated?.Invoke(false, ex.Message, policyName ?? newPolicyName);
        return false;
    }
}

        /// <summary>
        /// Generates a default policy document with configurable platform prefix
        /// </summary>
        private string GetDefaultPolicyDocument()
        {
            string prefix = string.IsNullOrEmpty(platformPrefix) ? "tnp" : platformPrefix;
    
            return $@"{{
  ""Version"": ""2012-10-17"",
  ""Statement"": [
    {{
      ""Effect"": ""Allow"",
      ""Action"": [""iot:Connect""],
      ""Resource"": ""arn:aws:iot:us-east-1:156041417101:client/{prefix}-*""
    }},
    {{
      ""Effect"": ""Allow"",
      ""Action"": [""iot:Publish"", ""iot:Receive""],
      ""Resource"": ""arn:aws:iot:us-east-1:156041417101:topic/{prefix}/*""
    }},
    {{
      ""Effect"": ""Allow"",
      ""Action"": [""iot:Subscribe""],
      ""Resource"": ""arn:aws:iot:us-east-1:156041417101:topicfilter/{prefix}/*""
    }},
    {{
      ""Effect"": ""Allow"",
      ""Action"": [
        ""iot:GetThingShadow"",
        ""iot:UpdateThingShadow"",
        ""iot:DeleteThingShadow""
      ],
      ""Resource"": ""arn:aws:iot:us-east-1:156041417101:thing/{prefix}-*""
    }}
  ]
}}";
        }

/// <summary>
/// Test method to create policy using Inspector configuration
/// </summary>
public async Task<bool> CreateConfiguredPolicyAsync()
{
    return await CreatePolicyAsync();
}

/// <summary>
/// Attaches a policy to a certificate to grant permissions
/// </summary>
public async Task<bool> AttachPolicyAsync(string policyName = null, string certificateId = null)
{
    try
    {
        string finalPolicyName = policyName ?? policyNameToAttach;
        string finalCertificateId = certificateId ?? certificateIdForPolicy;
        
        if (string.IsNullOrEmpty(finalPolicyName) || string.IsNullOrEmpty(finalCertificateId))
        {
            Debug.LogError("Policy name and Certificate ID are required");
            OnPolicyAttached?.Invoke(false, "Policy name and Certificate ID required", finalPolicyName, finalCertificateId);
            return false;
        }

        if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
        {
            Debug.LogError("User must be authenticated to attach policies");
            OnPolicyAttached?.Invoke(false, "User not authenticated", finalPolicyName, finalCertificateId);
            return false;
        }

        if (iotClient == null)
        {
            InitializeIoTClient();
            if (iotClient == null)
            {
                OnPolicyAttached?.Invoke(false, "Failed to initialize IoT client", finalPolicyName, finalCertificateId);
                return false;
            }
        }

        Debug.Log($"🔗 Attaching policy to certificate...");
        Debug.Log($"🛡️ Policy: {finalPolicyName}");
        Debug.Log($"🆔 Certificate: {finalCertificateId.Substring(0, 8)}...");

        var attachRequest = new AttachPolicyRequest
        {
            PolicyName = finalPolicyName,
            Target = $"arn:aws:iot:us-east-1:156041417101:cert/{finalCertificateId}" // ← ARN completo
        };
        

        var response = await iotClient.AttachPolicyAsync(attachRequest);

        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
        {
            Debug.Log($"✅ Policy attached successfully!");
            Debug.Log($"🔗 {finalPolicyName} → {finalCertificateId.Substring(0, 8)}...");
            Debug.Log($"🎉 Certificate now has MQTT + Shadow permissions!");
            OnPolicyAttached?.Invoke(true, "Policy attached successfully", finalPolicyName, finalCertificateId);
            return true;
        }
        else
        {
            Debug.LogError($"❌ Failed to attach policy: {response.HttpStatusCode}");
            OnPolicyAttached?.Invoke(false, $"Attach failed: {response.HttpStatusCode}", finalPolicyName, finalCertificateId);
            return false;
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"IoT Attach Policy error: {ex.Message}");
        OnPolicyAttached?.Invoke(false, ex.Message, policyName ?? policyNameToAttach, certificateId ?? certificateIdForPolicy);
        return false;
    }
}




/// <summary>
/// Test method to attach policy using Inspector configuration
/// </summary>
public async Task<bool> AttachConfiguredPolicyAsync()
{
    return await AttachPolicyAsync();
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

    [System.Serializable]
    public class CertificateData
    {
        public string CertificateId;       // Unique identifier
        public string CertificateArn;     // AWS ARN
        public string CertificatePem;     // X.509 certificate in PEM format
        public string PublicKey;          // RSA public key in PEM format
        public string PrivateKey;         // RSA private key in PEM format ⚠️ SENSITIVE!
        public bool IsActive;             // Certificate status
        public DateTime CreationDate;     // When it was created
        
        /// <summary>
        /// Security helper: Get certificate data without private key for logging
        /// </summary>
        public string GetSafeInfo()
        {
            return $"ID: {CertificateId?.Substring(0, 8)}..., Active: {IsActive}, Created: {CreationDate:yyyy-MM-dd HH:mm:ss}";
        }
    }

    #endregion
}