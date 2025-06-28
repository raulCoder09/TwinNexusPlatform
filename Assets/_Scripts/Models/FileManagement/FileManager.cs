using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Models.FileManagement
{
    public class FileManager : MonoBehaviour
    {
        #region Singleton Pattern
        private static FileManager _instance;
        public static FileManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<FileManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("FileManager");
                        _instance = go.AddComponent<FileManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
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
        #endregion

        [Header("File Management Configuration")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private bool autoCleanupOnStart = false;
        [SerializeField] private int maxTempFilesAgeDays = 7;
        [SerializeField] private long maxCacheSizeMB = 100;

        [Header("Storage Categories")]
        [SerializeField] private bool createSavesFolder = true;
        [SerializeField] private bool createImagesFolder = true;
        [SerializeField] private bool createDownloadsFolder = true;
        [SerializeField] private bool createTempFolder = true;
        [SerializeField] private bool createCertificatesFolder = true;

        private bool _isInitialized = false;
        private Dictionary<StorageInfo.StorageCategory, string> _categoryPaths = new Dictionary<StorageInfo.StorageCategory, string>();
        private PlatformAdapter _platformAdapter;
        private FileEventManager _eventManager;
        private FileOperations _fileOperations;
        private StreamingAssetsHandler _streamingAssetsHandler;
        private CacheManager _cacheManager;

        private void Start()
        {
            if (autoInitializeOnStart)
                Initialize();
        }

        public bool Initialize()
        {
            try
            {
                if (_isInitialized)
                {
                    LogDebug("FileManager already initialized");
                    return true;
                }

                LogDebug("Initializing FileManager...");
                _platformAdapter = new PlatformAdapter();
                _eventManager = new FileEventManager();
                _categoryPaths.Clear();

                // Initialize category paths
                _categoryPaths[StorageInfo.StorageCategory.Root] = _platformAdapter.GetPlatformPath(StorageInfo.StorageCategory.Root);
                _categoryPaths[StorageInfo.StorageCategory.Resources] = _platformAdapter.NormalizePath(Path.Combine(_platformAdapter.GetPlatformPath(StorageInfo.StorageCategory.Resources), "resources"));

                if (createSavesFolder) CreateCategoryFolder(StorageInfo.StorageCategory.Saves, "saves");
                if (createImagesFolder) CreateCategoryFolder(StorageInfo.StorageCategory.Images, "images");
                if (createDownloadsFolder) CreateCategoryFolder(StorageInfo.StorageCategory.Downloads, "downloads");
                if (createTempFolder) CreateCategoryFolder(StorageInfo.StorageCategory.Temp, "temp");
                if (createCertificatesFolder) CreateCategoryFolder(StorageInfo.StorageCategory.Certificates, "certificates");

                _fileOperations = new FileOperations(_platformAdapter, _eventManager, _categoryPaths);
                _streamingAssetsHandler = new StreamingAssetsHandler(_platformAdapter, _eventManager, _categoryPaths);
                _cacheManager = new CacheManager(_fileOperations, _eventManager, maxTempFilesAgeDays, maxCacheSizeMB);

                if (autoCleanupOnStart)
                {
                    LogDebug("Performing auto-cleanup...");
                    _cacheManager.CleanupTemp(StorageInfo.StorageCategory.Temp);
                }

                _isInitialized = true;
                LogDebug("FileManager initialized successfully");
                _eventManager.TriggerStorageInitialized(true, "Initialization successful");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Initialization error: {ex.Message}");
                _eventManager.TriggerStorageInitialized(false, ex.Message);
                return false;
            }
        }

        private void CreateCategoryFolder(StorageInfo.StorageCategory category, string folderName)
        {
            try
            {
                string path = _platformAdapter.NormalizePath(Path.Combine(_platformAdapter.GetPlatformPath(category), folderName));
                if (!_platformAdapter.IsRouteAccessible(path))
                    Directory.CreateDirectory(path);

                _categoryPaths[category] = path;
                _eventManager.TriggerFileCreated(category, folderName);
                LogDebug($"Created folder: {path}");
            }
            catch (Exception ex)
            {
                LogError($"Error creating folder {folderName}: {ex.Message}");
            }
        }

        public List<string> ListFiles(StorageInfo.StorageCategory category, string filter = "*")
        {
            if (!_isInitialized) Initialize();
            if (category == StorageInfo.StorageCategory.Resources)
                return new List<string>(); // Delegate to ListFilesAsync for StreamingAssets
            return _fileOperations.ListFiles(category, filter);
        }

        public void ListFilesAsync(StorageInfo.StorageCategory category, string filter, Action<List<string>> callback)
        {
            if (!_isInitialized) Initialize();
            _streamingAssetsHandler.ListFilesAsync(category, filter, this, callback);
        }

        public bool CreateFile(StorageInfo.StorageCategory category, string fileName, string content)
        {
            if (!_isInitialized) Initialize();
            return _fileOperations.CreateFile(category, fileName, content);
        }

        public bool CreateFile(StorageInfo.StorageCategory category, string fileName, byte[] content)
        {
            if (!_isInitialized) Initialize();
            return _fileOperations.CreateFile(category, fileName, content);
        }

        public bool CreateFolder(StorageInfo.StorageCategory category, string folderName)
        {
            if (!_isInitialized) Initialize();
            return _fileOperations.CreateFolder(category, folderName);
        }

        public bool DeleteFile(StorageInfo.StorageCategory category, string fileName)
        {
            if (!_isInitialized) Initialize();
            return _fileOperations.DeleteFile(category, fileName);
        }

        public bool DeleteFolder(StorageInfo.StorageCategory category, string folderName)
        {
            if (!_isInitialized) Initialize();
            return _fileOperations.DeleteFolder(category, folderName);
        }

        public string ReadFile(StorageInfo.StorageCategory category, string fileName)
        {
            if (!_isInitialized) Initialize();
            if (category == StorageInfo.StorageCategory.Resources)
                return null; // Delegate to ReadFileAsync for StreamingAssets
            return _fileOperations.ReadFile(category, fileName);
        }

        public byte[] ReadFileBytes(StorageInfo.StorageCategory category, string fileName)
        {
            if (!_isInitialized) Initialize();
            if (category == StorageInfo.StorageCategory.Resources)
                return null; // Delegate to ReadFileAsync for StreamingAssets
            return _fileOperations.ReadFileBytes(category, fileName);
        }

        public void ReadFileAsync(StorageInfo.StorageCategory category, string fileName, Action<string> callback)
        {
            if (!_isInitialized) Initialize();
            _streamingAssetsHandler.ReadFileAsync(category, fileName, this, callback);
        }

        public bool WriteFile(StorageInfo.StorageCategory category, string fileName, string content)
        {
            if (!_isInitialized) Initialize();
            return _fileOperations.WriteFile(category, fileName, content);
        }

        public bool WriteFile(StorageInfo.StorageCategory category, string fileName, byte[] content)
        {
            if (!_isInitialized) Initialize();
            return _fileOperations.WriteFile(category, fileName, content);
        }

        public bool CopyFile(StorageInfo.StorageCategory fromCategory, StorageInfo.StorageCategory toCategory, string fileName)
        {
            if (!_isInitialized) Initialize();
            return _streamingAssetsHandler.CopyToPersistent(fromCategory, fileName, toCategory);
        }

        public bool CleanupTemp()
        {
            if (!_isInitialized) Initialize();
            return _cacheManager.CleanupTemp(StorageInfo.StorageCategory.Temp);
        }

        public StorageInfo GetStorageInfo()
        {
            if (!_isInitialized) Initialize();
            try
            {
                var info = new StorageInfo
                {
                    basePath = _categoryPaths.ContainsKey(StorageInfo.StorageCategory.Root) ? _categoryPaths[StorageInfo.StorageCategory.Root] : "",
                    isInitialized = _isInitialized,
                    availableCategories = new List<StorageInfo.StorageCategory>(_categoryPaths.Keys),
                    totalFolders = _categoryPaths.Count,
                    totalFiles = 0
                };

                foreach (var category in _categoryPaths.Keys)
                {
                    if (category == StorageInfo.StorageCategory.Resources) continue;
                    info.totalFiles += _fileOperations.ListFiles(category, "*").Count;
                }

                try
                {
                    DriveInfo drive = new DriveInfo(_categoryPaths[StorageInfo.StorageCategory.Root]);
                    info.availableSpaceBytes = drive.AvailableFreeSpace;
                    info.totalSpaceBytes = drive.TotalSize;
                }
                catch
                {
                    info.availableSpaceBytes = -1;
                    info.totalSpaceBytes = -1;
                }

                LogDebug("Retrieved storage info");
                return info;
            }
            catch (Exception ex)
            {
                LogError($"Error getting storage info: {ex.Message}");
                return new StorageInfo { basePath = "", isInitialized = false };
            }
        }

        public void SubscribeToEvent<T>(string eventName, Action<T> callback)
        {
            if (!_isInitialized) Initialize();
            switch (eventName)
            {
                case nameof(FileEventManager.OnFileCreated): _eventManager.SubscribeToFileCreated(callback as Action<StorageInfo.StorageCategory, string>); break;
                case nameof(FileEventManager.OnFileDeleted): _eventManager.SubscribeToFileDeleted(callback as Action<StorageInfo.StorageCategory, string>); break;
                case nameof(FileEventManager.OnFileRead): _eventManager.SubscribeToFileRead(callback as Action<StorageInfo.StorageCategory, string, bool, string>); break;
                case nameof(FileEventManager.OnFileWritten): _eventManager.SubscribeToFileWritten(callback as Action<StorageInfo.StorageCategory, string, bool>); break;
                case nameof(FileEventManager.OnCleanupComplete): _eventManager.SubscribeToCleanupComplete(callback as Action<bool, string, long>); break;
                case nameof(FileEventManager.OnStorageInitialized): _eventManager.SubscribeToStorageInitialized(callback as Action<bool, string>); break;
                default: LogError($"Unknown event: {eventName}"); break;
            }
        }

        public void UnsubscribeFromEvent<T>(string eventName, Action<T> callback)
        {
            if (!_isInitialized) Initialize();
            switch (eventName)
            {
                case nameof(FileEventManager.OnFileCreated): _eventManager.UnsubscribeFromFileCreated(callback as Action<StorageInfo.StorageCategory, string>); break;
                case nameof(FileEventManager.OnFileDeleted): _eventManager.UnsubscribeFromFileDeleted(callback as Action<StorageInfo.StorageCategory, string>); break;
                case nameof(FileEventManager.OnFileRead): _eventManager.UnsubscribeFromFileRead(callback as Action<StorageInfo.StorageCategory, string, bool, string>); break;
                case nameof(FileEventManager.OnFileWritten): _eventManager.UnsubscribeFromFileWritten(callback as Action<StorageInfo.StorageCategory, string, bool>); break;
                case nameof(FileEventManager.OnCleanupComplete): _eventManager.UnsubscribeFromCleanupComplete(callback as Action<bool, string, long>); break;
                case nameof(FileEventManager.OnStorageInitialized): _eventManager.UnsubscribeFromStorageInitialized(callback as Action<bool, string>); break;
                default: LogError($"Unknown event: {eventName}"); break;
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[FileManager] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[FileManager] {message}");
        }
    }
}