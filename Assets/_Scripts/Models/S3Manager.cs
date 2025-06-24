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
        [SerializeField] private string bucketName = "twin-nexus-storage";
        [SerializeField] private string testImagePath = "StreamingAssets/test-image.jpg"; // Path relativo desde proyecto
        
        [Header("Download Configuration")]
        [SerializeField] private string downloadPath = "downloads"; // Carpeta de descarga relativa a persistentDataPath
        [SerializeField] private string specificFileKey = ""; // Archivo específico para descargar (ej: "super-admin/test-image-20250624-143052.jpg")
        
        // S3 client
        private AmazonS3Client s3Client;
        
        // Events for S3 operations
        public event Action<bool, string, string> OnUploadComplete; // success, message, fileKey
        public event Action<bool, string, byte[]> OnDownloadComplete; // success, message, fileData
        
        // Store the last uploaded file key for testing downloads
        private string lastUploadedFileKey;
        
        // Singleton instance
        public static S3Manager Instance { get; private set; }

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

                var regionEndpoint = RegionEndpoint.USEast1; // Same region as other services
                s3Client = new AmazonS3Client(CognitoManager.Instance.CurrentAWSCredentials, regionEndpoint);
                
                Debug.Log("S3 client initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize S3 client: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the folder path based on user role
        /// </summary>
        private string GetUserFolderPath()
        {
            if (CognitoManager.Instance == null)
                return "basic-users/";

            var userRole = CognitoManager.Instance.GetUserRole();
            return userRole switch
            {
                "super-admin" => "super-admin/",
                "operadores" => "operators/",
                "estudiantes" => "students/",
                "usuarios-basicos" => "basic-users/",
                _ => "basic-users/"
            };
        }

        /// <summary>
        /// Uploads a file (any type) to S3 from byte array
        /// </summary>
        public async Task<bool> UploadFileAsync(string fileName, byte[] fileData, string contentType = "application/octet-stream")
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

                // Create the full key (path + filename)
                string userFolder = GetUserFolderPath();
                string fileKey = $"{userFolder}{fileName}";

                Debug.Log($"Uploading file to S3...");
                Debug.Log($"Bucket: {bucketName}");
                Debug.Log($"Key: {fileKey}");
                Debug.Log($"Size: {fileData.Length} bytes");
                Debug.Log($"Content Type: {contentType}");

                // Create the upload request
                using (var stream = new MemoryStream(fileData))
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = bucketName,
                        Key = fileKey,
                        InputStream = stream,
                        ContentType = contentType,
                        ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                    };

                    // Upload the file
                    var response = await s3Client.PutObjectAsync(putRequest);

                    if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Debug.Log($"File uploaded successfully to: {fileKey}");
                        lastUploadedFileKey = fileKey; // Store for testing downloads
                        OnUploadComplete?.Invoke(true, "File uploaded successfully", fileKey);
                        return true;
                    }
                    else
                    {
                        Debug.LogError($"Upload failed with status: {response.HttpStatusCode}");
                        OnUploadComplete?.Invoke(false, $"Upload failed: {response.HttpStatusCode}", null);
                        return false;
                    }
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
        /// Uploads a text file to S3
        /// </summary>
        public async Task<bool> UploadTextFileAsync(string fileName, string content)
        {
            byte[] textData = System.Text.Encoding.UTF8.GetBytes(content);
            return await UploadFileAsync(fileName, textData, "text/plain");
        }

        /// <summary>
        /// Gets the correct file path for different platforms (Android compatible)
        /// </summary>
        private string GetPlatformFilePath(string relativePath)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
                // En Android, StreamingAssets se accede vía UnityWebRequest
                return Path.Combine(Application.streamingAssetsPath, relativePath);
            #else
                // En Editor y otras plataformas
                return Path.Combine(Application.streamingAssetsPath, relativePath);
            #endif
        }

        /// <summary>
        /// Loads file data from path (Android compatible)
        /// </summary>
        private async Task<byte[]> LoadFileDataAsync(string filePath)
        {
            try
            {
                #if UNITY_ANDROID && !UNITY_EDITOR
                    // En Android, usar UnityWebRequest para leer StreamingAssets
                    using (var www = UnityEngine.Networking.UnityWebRequest.Get(filePath))
                    {
                        var operation = www.SendWebRequest();
                        while (!operation.isDone)
                            await Task.Yield();

                        if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                        {
                            return www.downloadHandler.data;
                        }
                        else
                        {
                            Debug.LogError($"Failed to load file from Android: {www.error}");
                            return null;
                        }
                    }
                #else
                    // En Editor y otras plataformas, usar File.ReadAllBytes
                    if (File.Exists(filePath))
                    {
                        return await Task.Run(() => File.ReadAllBytes(filePath));
                    }
                    else
                    {
                        Debug.LogError($"File not found: {filePath}");
                        return null;
                    }
                #endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Detects content type based on file extension
        /// </summary>
        private string GetContentType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".txt" => "text/plain",
                ".json" => "application/json",
                ".pdf" => "application/pdf",
                ".mp4" => "video/mp4",
                ".mp3" => "audio/mpeg",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Downloads a file from S3 by file key
        /// </summary>
        public async Task<byte[]> DownloadFileAsync(string fileKey)
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

                Debug.Log($"Downloading file from S3...");
                Debug.Log($"Bucket: {bucketName}");
                Debug.Log($"Key: {fileKey}");

                // Create the download request
                var getRequest = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileKey
                };

                // Download the file
                using (var response = await s3Client.GetObjectAsync(getRequest))
                {
                    if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
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
                    else
                    {
                        Debug.LogError($"Download failed with status: {response.HttpStatusCode}");
                        OnDownloadComplete?.Invoke(false, $"Download failed: {response.HttpStatusCode}", null);
                        return null;
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
        /// Downloads a file and saves it to the configured download path
        /// </summary>
        public async Task<bool> DownloadFileToConfiguredPathAsync(string fileKey)
        {
            try
            {
                byte[] fileData = await DownloadFileAsync(fileKey);
                
                if (fileData == null || fileData.Length == 0)
                {
                    Debug.LogError("No data received from download");
                    return false;
                }

                // Use configured download path
                string downloadFolder = Path.Combine(Application.persistentDataPath, downloadPath);
                string fileName = Path.GetFileName(fileKey);
                string localPath = Path.Combine(downloadFolder, $"downloaded_{fileName}");

                // Ensure directory exists
                if (!Directory.Exists(downloadFolder))
                {
                    Directory.CreateDirectory(downloadFolder);
                }

                // Write file to disk
                await Task.Run(() => File.WriteAllBytes(localPath, fileData));
                
                Debug.Log($"✅ File saved to configured path: {localPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving downloaded file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Downloads the specific file configured in the inspector
        /// </summary>
        public async Task<bool> DownloadSpecificFileAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(specificFileKey))
                {
                    Debug.LogWarning("No specific file key configured in inspector");
                    OnDownloadComplete?.Invoke(false, "No file key specified", null);
                    return false;
                }

                Debug.Log($"Downloading specific file: {specificFileKey}");
                
                byte[] fileData = await DownloadFileAsync(specificFileKey);
                
                if (fileData != null && fileData.Length > 0)
                {
                    bool saved = await DownloadFileToConfiguredPathAsync(specificFileKey);
                    
                    if (saved)
                    {
                        Debug.Log($"✅ Specific file download completed!");
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Specific file download failed: {ex.Message}");
                OnDownloadComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        /// <summary>
        /// Test method to download the last uploaded file
        /// </summary>
        public async Task<bool> DownloadLastUploadedFileAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(lastUploadedFileKey))
                {
                    Debug.LogWarning("No file has been uploaded yet to download");
                    OnDownloadComplete?.Invoke(false, "No previous upload found", null);
                    return false;
                }

                Debug.Log($"Downloading last uploaded file: {lastUploadedFileKey}");
                
                byte[] fileData = await DownloadFileAsync(lastUploadedFileKey);
                
                if (fileData != null && fileData.Length > 0)
                {
                    bool saved = await DownloadFileToConfiguredPathAsync(lastUploadedFileKey);
                    
                    if (saved)
                    {
                        Debug.Log($"✅ Last uploaded file download completed!");
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test download failed: {ex.Message}");
                OnDownloadComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }
        public async Task<bool> UploadTestImageAsync()
        {
            try
            {
                Debug.Log($"Loading test image from: {testImagePath}");
                
                string fullPath = GetPlatformFilePath(testImagePath);
                byte[] imageData = await LoadFileDataAsync(fullPath);
                
                if (imageData == null || imageData.Length == 0)
                {
                    Debug.LogError("Failed to load test image data");
                    OnUploadComplete?.Invoke(false, "Failed to load image file", null);
                    return false;
                }

                string fileName = $"test-image-{DateTime.UtcNow:yyyyMMdd-HHmmss}.jpg";
                string contentType = GetContentType(fileName);
                
                Debug.Log($"Image loaded: {imageData.Length} bytes");
                return await UploadFileAsync(fileName, imageData, contentType);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Test image upload failed: {ex.Message}");
                OnUploadComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        private void OnDestroy()
        {
            s3Client?.Dispose();
        }
    }
}