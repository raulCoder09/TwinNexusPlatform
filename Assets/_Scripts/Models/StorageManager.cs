using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Models
{
    public class StorageManager : MonoBehaviour
    {
        #region Singleton Pattern
        private static StorageManager _instance;

        public static StorageManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<StorageManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("StorageManager");
                        _instance = go.AddComponent<StorageManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Serialized Configuration
        [Header("Storage Configuration")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private bool autoCleanupOnStart = false;

        [Header("Internal Storage Categories")]
        [SerializeField] private bool createCertificatesFolder = true;
        [SerializeField] private bool createDocumentsFolder = true;
        [SerializeField] private bool createImagesFolder = true;
        [SerializeField] private bool createVideosFolder = true;
        [SerializeField] private bool createAudioFolder = true;
        [SerializeField] private bool createDataFolder = true;
        [SerializeField] private bool createTempFolder = true;
        [SerializeField] private bool createDownloadsFolder = true;

        [Header("Cache Management")]
        [SerializeField] private int maxTempFilesAgeDays = 7;
        [SerializeField] private long maxCacheSizeMB = 100;
        #endregion

        #region Storage Categories Enum
        public enum StorageCategory
        {
            Certificates,
            Documents,
            Images,
            Videos,
            Audio,
            Data,
            Temporary,
            Downloads,
            Root
        }
        #endregion

        #region Events
        public event Action<bool, string> OnStorageInitialized; // success, message
        public event Action<StorageCategory, string> OnFolderCreated; // category, path
        public event Action<bool, string, long> OnCleanupComplete; // success, message, freedBytes
        #endregion

        #region Private Fields
        private bool _isInitialized = false;
        private Dictionary<StorageCategory, string> _categoryPaths = new Dictionary<StorageCategory, string>();
        private string _basePath;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (autoInitializeOnStart)
            {
                InitializeInternalStructure();
            }
        }
        #endregion

        #region Public Methods - Fase 1

        /// <summary>
        /// Initializes the internal storage structure creating all necessary folders
        /// </summary>
        /// <returns>True if initialization was successful</returns>
        public bool InitializeInternalStructure()
        {
            try
            {
                if (_isInitialized)
                {
                    LogDebug("Storage already initialized");
                    return true;
                }

                LogDebug("🚀 Initializing Storage Manager...");

                // Set base path to persistent data path
                _basePath = Application.persistentDataPath;
                LogDebug($"📁 Base storage path: {_basePath}");

                // Clear previous paths
                _categoryPaths.Clear();

                // Create folder structure
                bool success = CreateFolderStructure();

                if (success)
                {
                    _isInitialized = true;
                    LogDebug("✅ Storage Manager initialized successfully");
                    
                    // Optional cleanup on start
                    if (autoCleanupOnStart)
                    {
                        LogDebug("🧹 Auto-cleanup enabled, performing cleanup...");
                        CleanupTempFiles();
                    }

                    // Log storage info
                    LogStorageInfo();
                    
                    OnStorageInitialized?.Invoke(true, "Storage initialized successfully");
                }
                else
                {
                    LogError("❌ Failed to initialize storage structure");
                    OnStorageInitialized?.Invoke(false, "Failed to initialize storage structure");
                }

                return success;
            }
            catch (Exception ex)
            {
                LogError($"Storage initialization error: {ex.Message}");
                OnStorageInitialized?.Invoke(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Gets the path for a specific storage category
        /// </summary>
        /// <param name="category">Storage category</param>
        /// <returns>Full path to the category folder</returns>
        public string GetCategoryPath(StorageCategory category)
        {
            if (!_isInitialized)
            {
                LogWarning("Storage not initialized, attempting to initialize...");
                InitializeInternalStructure();
            }

            if (_categoryPaths.ContainsKey(category))
            {
                return _categoryPaths[category];
            }

            LogWarning($"Category path not found: {category}");
            return string.Empty;
        }

        /// <summary>
        /// Gets information about the current storage state
        /// </summary>
        /// <returns>Storage information object</returns>
        public StorageInfo GetStorageInfo()
        {
            try
            {
                var info = new StorageInfo
                {
                    basePath = _basePath,
                    isInitialized = _isInitialized,
                    availableCategories = new List<StorageCategory>(_categoryPaths.Keys),
                    totalFolders = _categoryPaths.Count
                };

                // Get available space (cross-platform)
                try
                {
                    DriveInfo drive = new DriveInfo(_basePath);
                    info.availableSpaceBytes = drive.AvailableFreeSpace;
                    info.totalSpaceBytes = drive.TotalSize;
                }
                catch
                {
                    // Fallback for platforms where DriveInfo doesn't work
                    info.availableSpaceBytes = -1;
                    info.totalSpaceBytes = -1;
                }

                return info;
            }
            catch (Exception ex)
            {
                LogError($"Error getting storage info: {ex.Message}");
                return new StorageInfo { basePath = _basePath, isInitialized = false };
            }
        }

        /// <summary>
        /// Checks if storage is properly initialized
        /// </summary>
        /// <returns>True if storage is ready to use</returns>
        public bool IsStorageReady()
        {
            return _isInitialized && !string.IsNullOrEmpty(_basePath) && _categoryPaths.Count > 0;
        }

        /// <summary>
        /// Test method to verify storage initialization
        /// </summary>
        /// <returns>True if test passes</returns>
        public bool TestStorageInitialization()
        {
            try
            {
                LogDebug("🧪 Testing storage initialization...");

                if (!InitializeInternalStructure())
                {
                    LogError("❌ Storage initialization test failed");
                    return false;
                }

                // Test that all enabled folders exist
                int foldersChecked = 0;
                int foldersFound = 0;

                foreach (var kvp in _categoryPaths)
                {
                    foldersChecked++;
                    if (Directory.Exists(kvp.Value))
                    {
                        foldersFound++;
                        LogDebug($"✅ {kvp.Key}: {kvp.Value}");
                    }
                    else
                    {
                        LogError($"❌ {kvp.Key}: {kvp.Value} - NOT FOUND");
                    }
                }

                bool testPassed = foldersChecked == foldersFound;
                LogDebug($"🧪 Test Results: {foldersFound}/{foldersChecked} folders found");
                
                if (testPassed)
                {
                    LogDebug("✅ Storage initialization test PASSED");
                }
                else
                {
                    LogError("❌ Storage initialization test FAILED");
                }

                return testPassed;
            }
            catch (Exception ex)
            {
                LogError($"Storage test error: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Creates the folder structure based on enabled categories
        /// </summary>
        /// <returns>True if all folders were created successfully</returns>
        private bool CreateFolderStructure()
        {
            try
            {
                LogDebug("📁 Creating folder structure...");

                // Add root path
                _categoryPaths[StorageCategory.Root] = _basePath;

                // Create category folders based on configuration
                if (createCertificatesFolder)
                    CreateCategoryFolder(StorageCategory.Certificates, "certificates");

                if (createDocumentsFolder)
                    CreateCategoryFolder(StorageCategory.Documents, "documents");

                if (createImagesFolder)
                    CreateCategoryFolder(StorageCategory.Images, "images");

                if (createVideosFolder)
                    CreateCategoryFolder(StorageCategory.Videos, "videos");

                if (createAudioFolder)
                    CreateCategoryFolder(StorageCategory.Audio, "audio");

                if (createDataFolder)
                    CreateCategoryFolder(StorageCategory.Data, "data");

                if (createTempFolder)
                    CreateCategoryFolder(StorageCategory.Temporary, "temp");

                if (createDownloadsFolder)
                    CreateCategoryFolder(StorageCategory.Downloads, "downloads");

                LogDebug($"✅ Created {_categoryPaths.Count} storage categories");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error creating folder structure: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a specific category folder
        /// </summary>
        /// <param name="category">Storage category</param>
        /// <param name="folderName">Folder name</param>
        private void CreateCategoryFolder(StorageCategory category, string folderName)
        {
            try
            {
                string fullPath = Path.Combine(_basePath, folderName);
                
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    LogDebug($"📁 Created folder: {folderName}");
                }
                else
                {
                    LogDebug($"📁 Folder exists: {folderName}");
                }

                _categoryPaths[category] = fullPath;
                OnFolderCreated?.Invoke(category, fullPath);
            }
            catch (Exception ex)
            {
                LogError($"Error creating folder {folderName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs current storage information
        /// </summary>
        private void LogStorageInfo()
        {
            try
            {
                var info = GetStorageInfo();
                
                LogDebug("📊 Storage Information:");
                LogDebug($"   🏠 Base Path: {info.basePath}");
                LogDebug($"   ✅ Initialized: {info.isInitialized}");
                LogDebug($"   📁 Total Folders: {info.totalFolders}");
                
                if (info.availableSpaceBytes >= 0)
                {
                    LogDebug($"   💾 Available Space: {FormatBytes(info.availableSpaceBytes)}");
                    LogDebug($"   💿 Total Space: {FormatBytes(info.totalSpaceBytes)}");
                }

                LogDebug("📋 Available Categories:");
                foreach (var category in info.availableCategories)
                {
                    LogDebug($"   📂 {category}: {GetCategoryPath(category)}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error logging storage info: {ex.Message}");
            }
        }

        /// <summary>
        /// Basic cleanup of temporary files (placeholder for now)
        /// </summary>
        private void CleanupTempFiles()
        {
            try
            {
                LogDebug("🧹 Performing temp files cleanup...");
                
                // For now, just log - actual cleanup will be implemented in later phases
                long freedBytes = 0;
                
                LogDebug($"🧹 Cleanup completed - {FormatBytes(freedBytes)} freed");
                OnCleanupComplete?.Invoke(true, "Cleanup completed", freedBytes);
            }
            catch (Exception ex)
            {
                LogError($"Cleanup error: {ex.Message}");
                OnCleanupComplete?.Invoke(false, ex.Message, 0);
            }
        }

        /// <summary>
        /// Formats bytes to human readable format
        /// </summary>
        /// <param name="bytes">Number of bytes</param>
        /// <returns>Formatted string</returns>
        private string FormatBytes(long bytes)
        {
            if (bytes < 0) return "Unknown";
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1048576) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1073741824) return $"{bytes / 1048576.0:F1} MB";
            return $"{bytes / 1073741824.0:F1} GB";
        }

        /// <summary>
        /// Debug logging method
        /// </summary>
        /// <param name="message">Message to log</param>
        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[StorageManager] {message}");
        }

        /// <summary>
        /// Warning logging method
        /// </summary>
        /// <param name="message">Message to log</param>
        private void LogWarning(string message)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[StorageManager] {message}");
        }

        /// <summary>
        /// Error logging method
        /// </summary>
        /// <param name="message">Message to log</param>
        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[StorageManager] {message}");
        }
        #endregion

        #region Data Classes
        [System.Serializable]
        public class StorageInfo
        {
            public string basePath;
            public bool isInitialized;
            public List<StorageCategory> availableCategories = new List<StorageCategory>();
            public int totalFolders;
            public long availableSpaceBytes;
            public long totalSpaceBytes;

            public string GetFormattedAvailableSpace()
            {
                if (availableSpaceBytes < 0) return "Unknown";
                if (availableSpaceBytes < 1024) return $"{availableSpaceBytes} B";
                if (availableSpaceBytes < 1048576) return $"{availableSpaceBytes / 1024.0:F1} KB";
                if (availableSpaceBytes < 1073741824) return $"{availableSpaceBytes / 1048576.0:F1} MB";
                return $"{availableSpaceBytes / 1073741824.0:F1} GB";
            }
        }
        #endregion
    }
}