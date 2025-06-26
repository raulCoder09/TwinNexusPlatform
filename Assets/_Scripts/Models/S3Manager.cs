using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using UnityEngine.Serialization;

namespace _Scripts.Models
{
    // Data class for S3 file information
    [System.Serializable]
    public class S3FileInfo
    {
        public string fileName;
        public string fullKey;
        public long sizeBytes;
        public DateTime lastModified;
        public string fileType;
        public string formattedSize;

        public S3FileInfo(string fileName, string fullKey, long sizeBytes, DateTime lastModified)
        {
            this.fileName = fileName;
            this.fullKey = fullKey;
            this.sizeBytes = sizeBytes;
            this.lastModified = lastModified;
            this.fileType = GetFileTypeFromExtension(fileName);
            this.formattedSize = FormatFileSize(sizeBytes);
        }

        private string GetFileTypeFromExtension(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".jpg" or ".jpeg" => "Image (JPEG)",
                ".png" => "Image (PNG)",
                ".gif" => "Image (GIF)",
                ".txt" => "Text File",
                ".json" => "JSON Data",
                ".pdf" => "PDF Document",
                ".mp4" => "Video (MP4)",
                ".mp3" => "Audio (MP3)",
                _ => "Unknown File"
            };
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1048576) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1073741824) return $"{bytes / 1048576.0:F1} MB";
            return $"{bytes / 1073741824.0:F1} GB";
        }
    }

    public class S3Manager : MonoBehaviour
    {
        [Header("S3 Configuration")]
        [SerializeField] private string bucketName = "twin-nexus-platform-storage";
        [SerializeField] private string sourceFilePath = "Test/sample-file.jpg"; // Path relativo desde proyecto
        
        [Header("Download Configuration")]
        [SerializeField] private string downloadPath = "downloads"; // Carpeta de descarga relativa a persistentDataPath
        [SerializeField] private string specificFileKey = ""; // Archivo específico para descargar (ej: "super-admin/test-image-20250624-143052.jpg")
        
        [Header("Delete Configuration")]
        [SerializeField] private string fileToDelete = ""; // Archivo específico para eliminar (ej: "super-admin/old-file.jpg")
        [Header("Upload Category")]
        [SerializeField] private ContentCategory uploadCategory = ContentCategory.Documents;

        public enum ContentCategory
        {
            Images,
            Videos, 
            Documents,
            Texts,
            Assignments,
            General
        }
        
        // S3 client
        private AmazonS3Client s3Client;
        
        // Events for S3 operations
        public event Action<bool, string, string> OnUploadComplete; // success, message, fileKey
        public event Action<bool, string, byte[]> OnDownloadComplete; // success, message, fileData
        public event Action<bool, string, List<S3FileInfo>> OnFileListComplete; // success, message, fileList
        public event Action<bool, string, string> OnFileDeleteComplete; // success, message, deletedFileKey
        
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
        private string GetUserFolderPath(ContentCategory category = ContentCategory.General)
        {
            if (CognitoManager.Instance == null)
                return "basic-users/unknown/general/";

            var userRole = CognitoManager.Instance.GetUserRole();
            var username = CognitoManager.Instance.CurrentUsername ?? "unknown";
    
            string baseFolder = userRole switch
            {
                "super-admin" => $"super-admin/{username}/",
                "students" => $"students/{username}/",
                _ => $"basic-users/{username}/"
            };

            string categoryFolder = category switch
            {
                ContentCategory.Images => "images/",
                ContentCategory.Videos => "videos/",
                ContentCategory.Documents => "documents/", 
                ContentCategory.Texts => "texts/",
                ContentCategory.Assignments => "assignments/",
                _ => "general/"
            };

            return baseFolder + categoryFolder;
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
                string userFolder = GetUserFolderPath(uploadCategory);
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
        /// Downloads a file and saves it maintaining folder structure
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

                // *** CAMBIO: Mantener estructura de carpetas ***
                string downloadFolder = Path.Combine(Application.persistentDataPath, downloadPath);
        
                // Extraer la estructura de carpetas del fileKey
                string relativePath = Path.GetDirectoryName(fileKey); // "super-admin/username/images"
                string fileName = Path.GetFileName(fileKey); // "uploaded-20250626-003958.jpeg"
        
                // Crear ruta completa manteniendo estructura
                string fullDownloadPath = Path.Combine(downloadFolder, relativePath);
                string localPath = Path.Combine(fullDownloadPath, $"downloaded_{fileName}");

                // Crear todos los directorios necesarios
                if (!Directory.Exists(fullDownloadPath))
                {
                    Directory.CreateDirectory(fullDownloadPath);
                }

                // Write file to disk
                await Task.Run(() => File.WriteAllBytes(localPath, fileData));
        
                Debug.Log($"✅ File saved maintaining structure: {localPath}");
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

        /// <summary>
        /// Deletes a file from S3 by file key
        /// </summary>
        public async Task<bool> DeleteFileAsync(string fileKey)
        {
            try
            {
                // Check if user is authenticated
                if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
                {
                    Debug.LogError("User must be authenticated to delete files");
                    OnFileDeleteComplete?.Invoke(false, "User not authenticated", null);
                    return false;
                }

                // Initialize S3 client if needed
                if (s3Client == null)
                {
                    InitializeS3Client();
                    if (s3Client == null)
                    {
                        OnFileDeleteComplete?.Invoke(false, "Failed to initialize S3 client", null);
                        return false;
                    }
                }

                // Security check: only allow deletion of files in user's folder
                string userFolder = GetUserFolderPath();
                if (!fileKey.StartsWith(userFolder) && CognitoManager.Instance.GetUserRole() != "super-admin")
                {
                    Debug.LogError($"Access denied: Cannot delete files outside your folder ({userFolder})");
                    OnFileDeleteComplete?.Invoke(false, "Access denied - can only delete files in your folder", null);
                    return false;
                }

                Debug.Log($"Deleting file from S3...");
                Debug.Log($"Bucket: {bucketName}");
                Debug.Log($"Key: {fileKey}");

                // Create delete request
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileKey
                };

                // Delete the file
                var response = await s3Client.DeleteObjectAsync(deleteRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    Debug.Log($"File deleted successfully: {fileKey}");
                    OnFileDeleteComplete?.Invoke(true, "File deleted successfully", fileKey);
                    return true;
                }
                else
                {
                    Debug.LogError($"Delete failed with status: {response.HttpStatusCode}");
                    OnFileDeleteComplete?.Invoke(false, $"Delete failed: {response.HttpStatusCode}", null);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"S3 delete error: {ex.Message}");
                OnFileDeleteComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        /// <summary>
        /// Deletes the specific file configured in the inspector
        /// </summary>
        public async Task<bool> DeleteSpecificFileAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(fileToDelete))
                {
                    Debug.LogWarning("No file specified for deletion in inspector");
                    OnFileDeleteComplete?.Invoke(false, "No file specified for deletion", null);
                    return false;
                }

                Debug.Log($"Preparing to delete specific file: {fileToDelete}");
                
                // Safety confirmation (in a real app, you'd show a UI dialog)
                Debug.LogWarning($"⚠️ ATTENTION: About to delete file: {fileToDelete}");
                Debug.LogWarning("⚠️ This action cannot be undone!");
                
                bool success = await DeleteFileAsync(fileToDelete);
                
                if (success)
                {
                    Debug.Log($"✅ Specific file deletion completed!");
                    // Clear the field after successful deletion to prevent accidental re-deletion
                    fileToDelete = "";
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Specific file deletion failed: {ex.Message}");
                OnFileDeleteComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        /// <summary>
        /// Test method to delete the last uploaded file (with safety confirmation)
        /// </summary>
        public async Task<bool> DeleteLastUploadedFileAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(lastUploadedFileKey))
                {
                    Debug.LogWarning("No file has been uploaded yet to delete");
                    OnFileDeleteComplete?.Invoke(false, "No previous upload found to delete", null);
                    return false;
                }

                Debug.Log($"⚠️ Preparing to delete last uploaded file: {lastUploadedFileKey}");
                Debug.LogWarning("⚠️ This will permanently delete your last uploaded file!");
                
                bool success = await DeleteFileAsync(lastUploadedFileKey);
                
                if (success)
                {
                    Debug.Log($"✅ Last uploaded file deleted successfully!");
                    lastUploadedFileKey = ""; // Clear the reference
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Delete last uploaded file failed: {ex.Message}");
                OnFileDeleteComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }
        public async Task<List<S3FileInfo>> ListUserFilesAsync()
{
    try
    {
        // Check if user is authenticated
        if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
        {
            Debug.LogError("User must be authenticated to list files");
            OnFileListComplete?.Invoke(false, "User not authenticated", new List<S3FileInfo>());
            return new List<S3FileInfo>();
        }

        // Initialize S3 client if needed
        if (s3Client == null)
        {
            InitializeS3Client();
            if (s3Client == null)
            {
                OnFileListComplete?.Invoke(false, "Failed to initialize S3 client", new List<S3FileInfo>());
                return new List<S3FileInfo>();
            }
        }

        // Get base user folder (without category) to list ALL user files
        string userFolder = GetBaseUserFolder();
        Debug.Log($"📋 Iniciando listado de archivos del usuario...");
        Debug.Log($"Listing files in user folder: {userFolder}");

        // Create list request for user's folder
        var listRequest = new ListObjectsV2Request
        {
            BucketName = bucketName,
            Prefix = userFolder,
            MaxKeys = 100 // Limit to 100 files for performance
        };

        var response = await s3Client.ListObjectsV2Async(listRequest);
        var fileList = new List<S3FileInfo>();

        foreach (var obj in response.S3Objects)
        {
            // Skip folder entries (keys ending with /)
            if (obj.Key.EndsWith("/")) continue;

            string fileName = Path.GetFileName(obj.Key);
            var fileInfo = new S3FileInfo(fileName, obj.Key, obj.Size ?? 0, obj.LastModified ?? DateTime.UtcNow);
            fileList.Add(fileInfo);
        }

        Debug.Log($"Found {fileList.Count} files in user folder");
        
        // Display results in console
        if (fileList.Count > 0)
        {
            Debug.Log($"📁 Archivos encontrados en tu carpeta:");
            for (int i = 0; i < fileList.Count; i++)
            {
                var file = fileList[i];
                Debug.Log($"{i + 1}. 📄 {file.fileName}");
                Debug.Log($"   📊 Tamaño: {file.formattedSize}");
                Debug.Log($"   🕐 Modificado: {file.lastModified:yyyy-MM-dd HH:mm:ss}");
                Debug.Log($"   🔍 Tipo: {file.fileType}");
                Debug.Log($"   📍 Ruta: {file.fullKey}");
                Debug.Log("   ---");
            }
        }
        else
        {
            Debug.Log("📭 No se encontraron archivos en tu carpeta");
        }

        OnFileListComplete?.Invoke(true, $"Found {fileList.Count} files", fileList);
        return fileList;
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error listing user files: {ex.Message}");
        OnFileListComplete?.Invoke(false, ex.Message, new List<S3FileInfo>());
        return new List<S3FileInfo>();
    }
}


        private string GetBaseUserFolder()
        {
            if (CognitoManager.Instance == null)
                return "basic-users/unknown/";

            var userRole = CognitoManager.Instance.GetUserRole();
            var username = CognitoManager.Instance.CurrentUsername ?? "unknown";
    
            return userRole switch
            {
                "super-admin" => $"super-admin/{username}/",
                "students" => $"students/{username}/",
                _ => $"basic-users/{username}/"
            };
        }

        /// <summary>
        /// Test method to list user files and display results
        /// </summary>
        

        public async Task<bool> UploadConfiguredFileAsync()
        {
            try
            {
                Debug.Log($"Loading file from: {sourceFilePath}");

                string fullPath = GetPlatformFilePath(sourceFilePath);
                byte[] fileData = await LoadFileDataAsync(fullPath);

                if (fileData == null || fileData.Length == 0)
                {
                    Debug.LogError("Failed to load file data");
                    OnUploadComplete?.Invoke(false, "Failed to load file", null);
                    return false;
                }

                // *** MEJORAR ESTO: ***
                // Detectar automáticamente el nombre y tipo desde el archivo configurado
                string originalFileName = Path.GetFileName(sourceFilePath);
                string fileExtension = Path.GetExtension(originalFileName);
                string fileName = $"uploaded-{DateTime.UtcNow:yyyyMMdd-HHmmss}{fileExtension}";
                string contentType = GetContentType(fileName);

                Debug.Log($"File loaded: {fileData.Length} bytes");
                Debug.Log($"Using category: {uploadCategory}");

                // Usar la categoría configurada directamente
                bool result = await UploadFileAsync(fileName, fileData, contentType);

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"File upload failed: {ex.Message}");
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