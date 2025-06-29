namespace _Scripts.Models.FileManagement
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

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
        [SerializeField] private bool createParentFolders = true;
        [SerializeField] private int maxTempFilesAgeDays = 7;
        [SerializeField] private long maxCacheSizeMB = 100;

        private bool _isInitialized = false;
        private Dictionary<string, string> _basePaths;
        private IStorageProvider _storageProvider;
        private FileEventManager _eventManager;
        private IFileOperations _persistentOperations;
        private IFileOperations _streamingOperations;
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
                _storageProvider = new PlatformAdapter();
                _eventManager = new FileEventManager();
                _basePaths = new Dictionary<string, string>
                {
                    { "persistent", _storageProvider.GetBasePath("persistent") },
                    { "streaming", _storageProvider.GetBasePath("streaming") },
                    { "temp", _storageProvider.GetBasePath("temp") }
                };

                _persistentOperations = new PersistentFileOperations(_storageProvider, _eventManager);
                _streamingOperations = new StreamingAssetsOperations(_storageProvider, _eventManager, this);
                _cacheManager = new CacheManager(_persistentOperations, _eventManager, _basePaths["temp"], maxTempFilesAgeDays, maxCacheSizeMB);

                if (autoCleanupOnStart)
                {
                    LogDebug("Performing auto-cleanup...");
                    _cacheManager.CleanupTempAsync((success, freedBytes) => { });
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

        public void ListFilesAsync(string path, string filter, Action<List<string>> callback)
        {
            if (!_isInitialized) Initialize();
            GetFileOperations(path).ListFilesAsync(path, filter, callback);
        }

        public void ReadFileAsync(string path, string fileName, Action<string> callback)
        {
            if (!_isInitialized) Initialize();
            GetFileOperations(path).ReadFileAsync(path, fileName, callback);
        }

        public void WriteFileAsync(string path, string fileName, string content, Action<bool> callback)
        {
            if (!_isInitialized) Initialize();
            if (createParentFolders)
                EnsureParentFolderExists(path);
            GetFileOperations(path).WriteFileAsync(path, fileName, content, callback);
        }

        public void WriteFileAsync(string path, string fileName, byte[] content, Action<bool> callback)
        {
            if (!_isInitialized) Initialize();
            if (createParentFolders)
                EnsureParentFolderExists(path);
            GetFileOperations(path).WriteFileAsync(path, fileName, content, callback);
        }

        public void CreateFileAsync(string path, string fileName, string content, Action<bool> callback)
        {
            if (!_isInitialized) Initialize();
            if (createParentFolders)
                EnsureParentFolderExists(path);
            GetFileOperations(path).CreateFileAsync(path, fileName, content, callback);
        }

        public void CreateFileAsync(string path, string fileName, byte[] content, Action<bool> callback)
        {
            if (!_isInitialized) Initialize();
            if (createParentFolders)
                EnsureParentFolderExists(path);
            GetFileOperations(path).CreateFileAsync(path, fileName, content, callback);
        }

        public void DeleteFileAsync(string path, string fileName, Action<bool> callback)
        {
            if (!_isInitialized) Initialize();
            GetFileOperations(path).DeleteFileAsync(path, fileName, callback);
        }

        public void CreateFolderAsync(string path, string folderName, Action<bool> callback)
        {
            if (!_isInitialized) Initialize();
            GetFileOperations(path).CreateFolderAsync(path, folderName, callback);
        }

        public void DeleteFolderAsync(string path, string folderName, Action<bool> callback)
        {
            if (!_isInitialized) Initialize();
            GetFileOperations(path).DeleteFolderAsync(path, folderName, callback);
        }

        public void CleanupTempAsync(Action<bool, long> callback)
        {
            if (!_isInitialized) Initialize();
            _cacheManager.CleanupTempAsync(callback);
        }

        public StorageInfo GetStorageInfo(string path)
        {
            if (!_isInitialized) Initialize();
            try
            {
                var info = new StorageInfo
                {
                    Path = path,
                    IsInitialized = _isInitialized
                };

                try
                {
                    DriveInfo drive = new DriveInfo(path);
                    info.AvailableSpaceBytes = drive.AvailableFreeSpace;
                    info.TotalSpaceBytes = drive.TotalSize;
                }
                catch
                {
                    info.AvailableSpaceBytes = -1;
                    info.TotalSpaceBytes = -1;
                }

                LogDebug($"Retrieved storage info for {path}");
                return info;
            }
            catch (Exception ex)
            {
                LogError($"Error getting storage info for {path}: {ex.Message}");
                return new StorageInfo { Path = path, IsInitialized = false };
            }
        }

        public void SubscribeToEvent<T>(string eventName, Action<T> callback)
        {
            if (!_isInitialized) Initialize();
            switch (eventName)
            {
                case nameof(FileEventManager.OnFileCreated):
                    _eventManager.SubscribeToFileCreated(callback as Action<string, string>);
                    break;
                case nameof(FileEventManager.OnFileDeleted):
                    _eventManager.SubscribeToFileDeleted(callback as Action<string, string>);
                    break;
                case nameof(FileEventManager.OnFileRead):
                    _eventManager.SubscribeToFileRead(callback as Action<string, string, bool, string>);
                    break;
                case nameof(FileEventManager.OnFileWritten):
                    _eventManager.SubscribeToFileWritten(callback as Action<string, string, bool>);
                    break;
                case nameof(FileEventManager.OnCleanupComplete):
                    _eventManager.SubscribeToCleanupComplete(callback as Action<bool, string, long>);
                    break;
                case nameof(FileEventManager.OnStorageInitialized):
                    _eventManager.SubscribeToStorageInitialized(callback as Action<bool, string>);
                    break;
                default:
                    LogError($"Unknown event: {eventName}");
                    break;
            }
        }

        public void UnsubscribeFromEvent<T>(string eventName, Action<T> callback)
        {
            if (!_isInitialized) Initialize();
            switch (eventName)
            {
                case nameof(FileEventManager.OnFileCreated):
                    _eventManager.UnsubscribeFromFileCreated(callback as Action<string, string>);
                    break;
                case nameof(FileEventManager.OnFileDeleted):
                    _eventManager.UnsubscribeFromFileDeleted(callback as Action<string, string>);
                    break;
                case nameof(FileEventManager.OnFileRead):
                    _eventManager.UnsubscribeFromFileRead(callback as Action<string, string, bool, string>);
                    break;
                case nameof(FileEventManager.OnFileWritten):
                    _eventManager.UnsubscribeFromFileWritten(callback as Action<string, string, bool>);
                    break;
                case nameof(FileEventManager.OnCleanupComplete):
                    _eventManager.UnsubscribeFromCleanupComplete(callback as Action<bool, string, long>);
                    break;
                case nameof(FileEventManager.OnStorageInitialized):
                    _eventManager.UnsubscribeFromStorageInitialized(callback as Action<bool, string>);
                    break;
                default:
                    LogError($"Unknown event: {eventName}");
                    break;
            }
        }

        public string GetBasePath(string context)
        {
            return _storageProvider.GetBasePath(context);
        }

        private IFileOperations GetFileOperations(string path)
        {
            if (path.StartsWith(_basePaths["streaming"]))
                return _streamingOperations;
            return _persistentOperations;
        }

        private void EnsureParentFolderExists(string path)
        {
            try
            {
                string parentPath = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(parentPath) && !_storageProvider.IsPathAccessible(parentPath))
                {
                    Directory.CreateDirectory(parentPath);
                    _eventManager.TriggerFileCreated(parentPath, Path.GetFileName(parentPath));
                    LogDebug($"Created parent folder: {parentPath}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error creating parent folder for {path}: {ex.Message}");
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