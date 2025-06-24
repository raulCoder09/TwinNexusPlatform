using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using _Scripts.Models;

namespace _Scripts.Models
{
    public class S3Manager : MonoBehaviour
    {
        [Header("S3 Configuration")]
        [SerializeField] private string bucketName = "twin-nexus-platform-storage";
        [SerializeField] private long maxFileSizeBytes = 10 * 1024 * 1024; // 10MB default
        
        // S3 client
        private AmazonS3Client s3Client;
        
        // Events for S3 operations
        public event Action<bool, string, string> OnUploadComplete;
        public event Action<bool, string, byte[]> OnDownloadComplete;
        public event Action<bool, string> OnDeleteComplete;
        public event Action<bool, string, List<string>> OnListFilesComplete;
        
        // Singleton instance
        public static S3Manager Instance { get; private set; }

        private void Awake()
        {
            Debug.Log("🔧 [S3 MANAGER] Awake() called");
    
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("✅ [S3 MANAGER] Instance created successfully");
            }
            else
            {
                Debug.Log("⚠️ [S3 MANAGER] Duplicate instance destroyed");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initializes S3 client with current AWS credentials
        /// </summary>
        private void InitializeS3Client()
        {
            try
            {
                if (CognitoManager.Instance == null || CognitoManager.Instance.CurrentAWSCredentials == null)
                {
                    Debug.LogError("No AWS credentials available. Please authenticate first.");
                    return;
                }

                var regionEndpoint = RegionEndpoint.USEast1; // Use same region as Cognito
                s3Client = new AmazonS3Client(CognitoManager.Instance.CurrentAWSCredentials, regionEndpoint);
                
                Debug.Log("S3 client initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize S3 client: {ex.Message}");
            }
        }

        /// <summary>
        /// Uploads a file to S3 with user context (automatic path based on role)
        /// </summary>
        public async Task<bool> UploadFileWithUserContextAsync(string fileName, byte[] fileData, string category = "general")
        {
            try
            {
                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to upload files");
                    OnUploadComplete?.Invoke(false, "User not authenticated", null);
                    return false;
                }

                string userRole = CognitoManager.Instance.GetUserRole();
                string username = CognitoManager.Instance.CurrentUsername;
                
                // Construct path based on user role
                string filePath = userRole switch
                {
                    "super-admin" => $"super-admin/{username}/{category}/{fileName}",
                    "operadores" => $"operators/{username}/{category}/{fileName}",
                    "estudiantes" => $"students/{username}/{category}/{fileName}",
                    _ => $"basic-users/{username}/{fileName}"
                };

                Debug.Log($"Uploading file for {userRole}: {filePath}");
                return await UploadFileAsync(filePath, fileData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error uploading file with user context: {ex.Message}");
                OnUploadComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        /// <summary>
        /// Uploads a file to S3 at specified path
        /// </summary>
        public async Task<bool> UploadFileAsync(string filePath, byte[] fileData)
        {
            try
            {
                // Check if user is authenticated
                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to upload files");
                    OnUploadComplete?.Invoke(false, "User not authenticated", null);
                    return false;
                }

                // Check file size
                if (fileData.Length > maxFileSizeBytes)
                {
                    string errorMsg = $"File size ({fileData.Length} bytes) exceeds maximum allowed ({maxFileSizeBytes} bytes)";
                    Debug.LogError(errorMsg);
                    OnUploadComplete?.Invoke(false, errorMsg, null);
                    return false;
                }

                // Initialize S3 client if needed
                if (s3Client == null)
                {
                    InitializeS3Client();
                    if (s3Client == null)
                    {
                        OnUploadComplete?.Invoke(false, "Failed to initialize S3 client", null);
                        return false;
                    }
                }

                Debug.Log($"Uploading file to S3: {bucketName}/{filePath}");
                Debug.Log($"File size: {fileData.Length} bytes");
                Debug.Log($"User: {CognitoManager.Instance.CurrentUsername} ({CognitoManager.Instance.GetUserRole()})");

                // Create upload request
                var uploadRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = filePath,
                    InputStream = new MemoryStream(fileData),
                    ContentType = GetContentType(filePath),
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                // Add metadata
                uploadRequest.Metadata.Add("uploaded-by", CognitoManager.Instance.CurrentUsername);
                uploadRequest.Metadata.Add("user-role", CognitoManager.Instance.GetUserRole());
                uploadRequest.Metadata.Add("upload-timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

                // Upload file
                var response = await s3Client.PutObjectAsync(uploadRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.Log($"File uploaded successfully to {bucketName}/{filePath}");
                    OnUploadComplete?.Invoke(true, "File uploaded successfully", filePath);
                    return true;
                }
                else
                {
                    string errorMsg = $"Upload failed with status: {response.HttpStatusCode}";
                    Debug.LogError(errorMsg);
                    OnUploadComplete?.Invoke(false, errorMsg, null);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"S3 upload error: {ex.Message}");
                OnUploadComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        /// <summary>
        /// Downloads a file from S3
        /// </summary>
        public async Task<byte[]> DownloadFileAsync(string filePath)
        {
            try
            {
                // Check if user is authenticated
                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to download files");
                    OnDownloadComplete?.Invoke(false, "User not authenticated", null);
                    return null;
                }

                // Check permissions
                if (!CanUserAccessFile(filePath))
                {
                    string errorMsg = $"User doesn't have permission to access file: {filePath}";
                    Debug.LogError(errorMsg);
                    OnDownloadComplete?.Invoke(false, errorMsg, null);
                    return null;
                }

                // Initialize S3 client if needed
                if (s3Client == null)
                {
                    InitializeS3Client();
                    if (s3Client == null)
                    {
                        OnDownloadComplete?.Invoke(false, "Failed to initialize S3 client", null);
                        return null;
                    }
                }

                Debug.Log($"Downloading file from S3: {bucketName}/{filePath}");

                // Create download request
                var downloadRequest = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = filePath
                };

                // Download file
                using (var response = await s3Client.GetObjectAsync(downloadRequest))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await response.ResponseStream.CopyToAsync(memoryStream);
                        byte[] fileData = memoryStream.ToArray();
                        
                        Debug.Log($"File downloaded successfully: {fileData.Length} bytes");
                        OnDownloadComplete?.Invoke(true, "File downloaded successfully", fileData);
                        return fileData;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"S3 download error: {ex.Message}");
                OnDownloadComplete?.Invoke(false, ex.Message, null);
                return null;
            }
        }

        /// <summary>
        /// Lists files accessible to the current user
        /// </summary>
        public async Task<List<string>> ListUserFilesAsync()
        {
            try
            {
                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to list files");
                    OnListFilesComplete?.Invoke(false, "User not authenticated", new List<string>());
                    return new List<string>();
                }

                if (s3Client == null)
                {
                    InitializeS3Client();
                    if (s3Client == null)
                    {
                        OnListFilesComplete?.Invoke(false, "Failed to initialize S3 client", new List<string>());
                        return new List<string>();
                    }
                }

                string userRole = CognitoManager.Instance.GetUserRole();
                string username = CognitoManager.Instance.CurrentUsername;
                
                var accessiblePrefixes = GetAccessiblePrefixes(userRole, username);
                var allFiles = new List<string>();

                foreach (string prefix in accessiblePrefixes)
                {
                    var listRequest = new ListObjectsV2Request
                    {
                        BucketName = bucketName,
                        Prefix = prefix,
                        MaxKeys = 100
                    };

                    var response = await s3Client.ListObjectsV2Async(listRequest);
                    
                    foreach (var obj in response.S3Objects)
                    {
                        if (!obj.Key.EndsWith("/")) // Skip folder markers
                        {
                            allFiles.Add(obj.Key);
                        }
                    }
                }

                Debug.Log($"Found {allFiles.Count} files accessible to user {username} ({userRole})");
                OnListFilesComplete?.Invoke(true, "Files listed successfully", allFiles);
                return allFiles;
            }
            catch (Exception ex)
            {
                Debug.LogError($"S3 list files error: {ex.Message}");
                OnListFilesComplete?.Invoke(false, ex.Message, new List<string>());
                return new List<string>();
            }
        }

        /// <summary>
        /// Deletes a file from S3
        /// </summary>
        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to delete files");
                    OnDeleteComplete?.Invoke(false, "User not authenticated");
                    return false;
                }

                if (!CanUserDeleteFile(filePath))
                {
                    string errorMsg = $"User doesn't have permission to delete file: {filePath}";
                    Debug.LogError(errorMsg);
                    OnDeleteComplete?.Invoke(false, errorMsg);
                    return false;
                }

                if (s3Client == null)
                {
                    InitializeS3Client();
                    if (s3Client == null)
                    {
                        OnDeleteComplete?.Invoke(false, "Failed to initialize S3 client");
                        return false;
                    }
                }

                Debug.Log($"Deleting file from S3: {bucketName}/{filePath}");

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = filePath
                };

                var response = await s3Client.DeleteObjectAsync(deleteRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    Debug.Log($"File deleted successfully: {filePath}");
                    OnDeleteComplete?.Invoke(true, "File deleted successfully");
                    return true;
                }
                else
                {
                    string errorMsg = $"Delete failed with status: {response.HttpStatusCode}";
                    Debug.LogError(errorMsg);
                    OnDeleteComplete?.Invoke(false, errorMsg);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"S3 delete error: {ex.Message}");
                OnDeleteComplete?.Invoke(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Checks if user can access a specific file
        /// </summary>
        public bool CanUserAccessFile(string filePath)
        {
            if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                return false;

            string userRole = CognitoManager.Instance.GetUserRole();
            string username = CognitoManager.Instance.CurrentUsername;

            // Super admin can access everything
            if (userRole == "super-admin")
                return true;

            // Public files accessible to all
            if (filePath.StartsWith("public/"))
                return true;

            // Users can access their own files
            string userPrefix = userRole switch
            {
                "operadores" => $"operators/{username}/",
                "estudiantes" => $"students/{username}/",
                _ => $"basic-users/{username}/"
            };

            if (filePath.StartsWith(userPrefix))
                return true;

            // Additional role-specific permissions
            switch (userRole)
            {
                case "operadores":
                    return filePath.StartsWith("operators/shared/");
                case "estudiantes":
                    return filePath.StartsWith("students/shared/");
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if user can delete a specific file
        /// </summary>
        public bool CanUserDeleteFile(string filePath)
        {
            if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                return false;

            string userRole = CognitoManager.Instance.GetUserRole();
            string username = CognitoManager.Instance.CurrentUsername;

            // Super admin can delete everything except public files
            if (userRole == "super-admin")
                return !filePath.StartsWith("public/");

            // Users can only delete their own files
            string userPrefix = userRole switch
            {
                "operadores" => $"operators/{username}/",
                "estudiantes" => $"students/{username}/",
                _ => $"basic-users/{username}/"
            };

            return filePath.StartsWith(userPrefix);
        }

        /// <summary>
        /// Gets accessible prefixes for a user role
        /// </summary>
        private List<string> GetAccessiblePrefixes(string userRole, string username)
        {
            var prefixes = new List<string> { "public/" };

            switch (userRole)
            {
                case "super-admin":
                    prefixes.AddRange(new[] { "super-admin/", "operators/", "students/", "basic-users/" });
                    break;
                case "operadores":
                    prefixes.AddRange(new[] { $"operators/{username}/", "operators/shared/" });
                    break;
                case "estudiantes":
                    prefixes.AddRange(new[] { $"students/{username}/", "students/shared/" });
                    break;
                default:
                    prefixes.Add($"basic-users/{username}/");
                    break;
            }

            return prefixes;
        }

        /// <summary>
        /// Gets content type based on file extension
        /// </summary>
        private string GetContentType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".json" => "application/json",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Test method to verify S3 connectivity and permissions
        /// </summary>
        public async Task<bool> TestS3ConnectivityAsync()
        {
            Debug.Log("🧪 [S3 MANAGER] TestS3ConnectivityAsync() started");
    
            try
            {
                Debug.Log("Testing S3 connectivity...");
        
                if (CognitoManager.Instance == null)
                {
                    Debug.LogError("❌ [S3 MANAGER] CognitoManager.Instance is NULL");
                    return false;
                }
        
                if (!CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("❌ [S3 MANAGER] User is not authenticated");
                    return false;
                }
        
                Debug.Log($"✅ [S3 MANAGER] User is authenticated: {CognitoManager.Instance.CurrentUsername}");
        
                string testContent = $"S3 connectivity test from Unity\nUser: {CognitoManager.Instance?.CurrentUsername}\nRole: {CognitoManager.Instance?.GetUserRole()}\nTimestamp: {DateTime.UtcNow}";
                byte[] testData = System.Text.Encoding.UTF8.GetBytes(testContent);
        
                Debug.Log($"🔧 [S3 MANAGER] About to call UploadFileWithUserContextAsync");
                return await UploadFileWithUserContextAsync("s3-connectivity-test.txt", testData, "tests");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [S3 MANAGER] S3 connectivity test failed: {ex.Message}");
                return false;
            }
        }
        [ContextMenu("Force Initialize S3 Client")]
        public void ForceInitializeS3Client()
        {
            Debug.Log("🔧 [S3 MANAGER] Force initializing S3 client...");
            InitializeS3Client();
    
            if (s3Client != null)
            {
                Debug.Log("✅ [S3 MANAGER] S3 client initialized successfully");
            }
            else
            {
                Debug.LogError("❌ [S3 MANAGER] S3 client initialization failed");
            }
        }

        private void OnDestroy()
        {
            s3Client?.Dispose();
        }
    }
}