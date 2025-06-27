using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using _ScriptableObjects;
using UnityEngine;

namespace _Scripts.Models
{
    public class SaveDataManager : MonoBehaviour
    {
        #region Singleton Implementation
        private static SaveDataManager _instance;
        private static readonly object _lock = new object();

        public static SaveDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // Buscar instancia existente en la escena
                            _instance = FindObjectOfType<SaveDataManager>();
                            
                            if (_instance == null)
                            {
                                // Crear nuevo GameObject con SaveDataManager
                                GameObject saveObject = new GameObject("SaveDataManager");
                                _instance = saveObject.AddComponent<SaveDataManager>();
                                DontDestroyOnLoad(saveObject);
                            }
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Serialized Configuration Fields
        [Header("Configuration Data")]
        [SerializeField] private IotConfigurationData iotConfigurationData;
        [SerializeField] private List<ScriptableObject> additionalDataObjects = new List<ScriptableObject>();

        [Header("Auto Save Settings")]
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private float autoSaveInterval = 30f; // seconds
        [SerializeField] private bool saveOnApplicationPause = true;
        [SerializeField] private bool saveOnApplicationQuit = true;
        [SerializeField] private bool saveOnApplicationFocus = true;

        [Header("Backup Settings")]
        [SerializeField] private bool enableBackup = true;
        [SerializeField] private bool createBackupBeforeSave = true;
        [SerializeField] private int maxBackupFiles = 5;
        [SerializeField] private bool enableTimestampedBackups = false;

        [Header("File Settings")]
        [SerializeField] private string saveFileName = "iotConfig.json";
        [SerializeField] private string backupFileName = "iotConfig_backup.json";
        [SerializeField] private bool useCompression = false;
        [SerializeField] private bool useEncryption = false;
        [SerializeField] private string encryptionKey = "";

        [Header("Validation Settings")]
        [SerializeField] private bool validateJsonOnLoad = true;
        [SerializeField] private bool createDefaultIfInvalid = true;
        [SerializeField] private float minAutoSaveInterval = 5f;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool logFileOperations = true;
        [SerializeField] private bool logAutoSave = false;
        #endregion

        #region Private Fields
        private string _savePath;
        private string _backupPath;
        private string _backupDirectory;
        private float _lastSaveTime;
        private bool _hasUnsavedChanges;
        private bool _isInitialized = false;
        private Dictionary<string, object> _runtimeData = new Dictionary<string, object>();
        
        private const string TEMP_FILE_SUFFIX = ".tmp";
        private const string TIMESTAMP_FORMAT = "yyyyMMdd_HHmmss";
        #endregion

        #region Events
        public static event System.Action OnDataSaved;
        public static event System.Action OnDataLoaded;
        public static event System.Action<string> OnSaveError;
        public static event System.Action<string> OnLoadError;
        public static event System.Action OnBackupCreated;
        public static event System.Action OnConfigurationReset;
        public static event System.Action<float> OnAutoSaveTriggered;
        #endregion

        #region Enums
        public enum SaveResult
        {
            Success,
            Failed,
            NoData,
            AccessDenied,
            InvalidData
        }

        public enum LoadResult
        {
            Success,
            LoadedFromBackup,
            Failed,
            FileNotFound,
            InvalidData,
            UsingDefaults
        }

        public enum DataType
        {
            Main,
            Backup,
            RuntimeCache
        }
        #endregion

        #region Properties
        public bool HasUnsavedChanges => _hasUnsavedChanges;
        public bool IsAutoSaveEnabled => enableAutoSave;
        public float NextAutoSaveIn => enableAutoSave ? Mathf.Max(0, autoSaveInterval - (Time.time - _lastSaveTime)) : -1;
        public bool IsInitialized => _isInitialized;
        public IotConfigurationData ConfigurationData => iotConfigurationData;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Implementar Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManager();
            }
            else if (_instance != this)
            {
                LogDebug("Another SaveDataManager instance already exists. Destroying this one.");
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (_instance == this)
            {
                LoadData();
            }
        }

        private void Update()
        {
            if (_instance == this)
            {
                HandleAutoSave();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (_instance == this && pauseStatus && saveOnApplicationPause && _hasUnsavedChanges)
            {
                LogDebug("Application paused - saving data");
                SaveData();
            }
        }

        private void OnApplicationQuit()
        {
            if (_instance == this && saveOnApplicationQuit && _hasUnsavedChanges)
            {
                LogDebug("Application quitting - saving data");
                SaveData();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_instance == this && !hasFocus && saveOnApplicationFocus && 
                _hasUnsavedChanges && enableAutoSave)
            {
                LogDebug("Application lost focus - saving data");
                SaveData();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                if (_hasUnsavedChanges)
                {
                    SaveData();
                }
                _instance = null;
            }
        }
        #endregion

        #region Public Methods - Main API
        /// <summary>
        /// Inicializa el SaveDataManager
        /// </summary>
        public void InitializeManager()
        {
            if (_isInitialized) return;

            try
            {
                InitializePaths();
                ValidateConfiguration();
                _runtimeData.Clear();
                _isInitialized = true;
                LogDebug("SaveDataManager initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize SaveDataManager: {ex.Message}");
            }
        }

        /// <summary>
        /// Guarda los datos de configuración de forma síncrona
        /// </summary>
        /// <param name="dataType">Tipo de datos a guardar</param>
        /// <returns>Resultado de la operación de guardado</returns>
        public SaveResult SaveData(DataType dataType = DataType.Main)
        {
            try
            {
                if (!_isInitialized)
                {
                    InitializeManager();
                }

                if (iotConfigurationData == null && dataType == DataType.Main)
                {
                    LogError("Cannot save: IoT configuration data is null");
                    return SaveResult.NoData;
                }

                return PerformSave(dataType);
            }
            catch (UnauthorizedAccessException)
            {
                LogError("Access denied while saving data");
                return SaveResult.AccessDenied;
            }
            catch (Exception ex)
            {
                LogError($"Failed to save data: {ex.Message}");
                return SaveResult.Failed;
            }
        }

        /// <summary>
        /// Guarda los datos de configuración de forma asíncrona
        /// </summary>
        /// <param name="dataType">Tipo de datos a guardar</param>
        /// <returns>Resultado de la operación de guardado</returns>
        public async Task<SaveResult> SaveDataAsync(DataType dataType = DataType.Main)
        {
            try
            {
                if (!_isInitialized)
                {
                    InitializeManager();
                }

                if (iotConfigurationData == null && dataType == DataType.Main)
                {
                    LogError("Cannot save: IoT configuration data is null");
                    return SaveResult.NoData;
                }

                return await Task.Run(() => PerformSave(dataType));
            }
            catch (UnauthorizedAccessException)
            {
                LogError("Access denied while saving data asynchronously");
                return SaveResult.AccessDenied;
            }
            catch (Exception ex)
            {
                LogError($"Failed to save data asynchronously: {ex.Message}");
                return SaveResult.Failed;
            }
        }

        /// <summary>
        /// Carga los datos de configuración
        /// </summary>
        /// <param name="dataType">Tipo de datos a cargar</param>
        /// <returns>Resultado de la operación de carga</returns>
        public LoadResult LoadData(DataType dataType = DataType.Main)
        {
            try
            {
                if (!_isInitialized)
                {
                    InitializeManager();
                }

                if (iotConfigurationData == null && dataType == DataType.Main)
                {
                    LogError("Cannot load: IoT configuration data is null");
                    return LoadResult.Failed;
                }

                return PerformLoad(dataType);
            }
            catch (Exception ex)
            {
                LogError($"Failed to load data: {ex.Message}");
                return LoadResult.Failed;
            }
        }

        /// <summary>
        /// Carga los datos de configuración de forma asíncrona
        /// </summary>
        /// <param name="dataType">Tipo de datos a cargar</param>
        /// <returns>Resultado de la operación de carga</returns>
        public async Task<LoadResult> LoadDataAsync(DataType dataType = DataType.Main)
        {
            try
            {
                if (!_isInitialized)
                {
                    InitializeManager();
                }

                if (iotConfigurationData == null && dataType == DataType.Main)
                {
                    LogError("Cannot load: IoT configuration data is null");
                    return LoadResult.Failed;
                }

                return await Task.Run(() => PerformLoad(dataType));
            }
            catch (Exception ex)
            {
                LogError($"Failed to load data asynchronously: {ex.Message}");
                return LoadResult.Failed;
            }
        }

        /// <summary>
        /// Guarda datos adicionales del runtime
        /// </summary>
        /// <param name="key">Clave del dato</param>
        /// <param name="value">Valor a guardar</param>
        /// <param name="markUnsaved">Si marcar como datos sin guardar</param>
        public void SetRuntimeData<T>(string key, T value, bool markUnsaved = true)
        {
            if (string.IsNullOrEmpty(key)) return;

            _runtimeData[key] = value;
            
            if (markUnsaved)
            {
                MarkAsUnsaved();
            }

            LogDebug($"Runtime data set: {key}");
        }

        /// <summary>
        /// Obtiene datos del runtime
        /// </summary>
        /// <param name="key">Clave del dato</param>
        /// <param name="defaultValue">Valor por defecto si no existe</param>
        /// <returns>Valor almacenado o valor por defecto</returns>
        public T GetRuntimeData<T>(string key, T defaultValue = default(T))
        {
            if (string.IsNullOrEmpty(key) || !_runtimeData.ContainsKey(key))
            {
                return defaultValue;
            }

            try
            {
                return (T)_runtimeData[key];
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Elimina datos del runtime
        /// </summary>
        /// <param name="key">Clave del dato a eliminar</param>
        /// <returns>True si se eliminó correctamente</returns>
        public bool RemoveRuntimeData(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            
            bool removed = _runtimeData.Remove(key);
            if (removed)
            {
                MarkAsUnsaved();
                LogDebug($"Runtime data removed: {key}");
            }
            
            return removed;
        }

        /// <summary>
        /// Restablece la configuración a valores por defecto
        /// </summary>
        /// <param name="deleteBackup">Si también eliminar archivos de respaldo</param>
        /// <param name="clearRuntimeData">Si limpiar datos del runtime</param>
        /// <returns>True si el restablecimiento fue exitoso</returns>
        public bool ResetToDefault(bool deleteBackup = false, bool clearRuntimeData = true)
        {
            try
            {
                bool success = true;

                // Eliminar archivo principal
                if (File.Exists(_savePath))
                {
                    File.Delete(_savePath);
                    LogFileOperation("Main configuration file deleted");
                }

                // Eliminar backups si se solicita
                if (deleteBackup)
                {
                    DeleteAllBackups();
                }

                // Limpiar datos del runtime
                if (clearRuntimeData)
                {
                    _runtimeData.Clear();
                }

                // Resetear datos en memoria a valores por defecto
                if (iotConfigurationData != null)
                {
                    ResetConfigurationToDefaults();
                }

                _hasUnsavedChanges = false;
                LogDebug("Configuration reset to default values");
                OnConfigurationReset?.Invoke();
                
                return success;
            }
            catch (Exception ex)
            {
                LogError($"Failed to reset configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restaura desde el archivo de respaldo más reciente
        /// </summary>
        /// <returns>Resultado de la operación de restauración</returns>
        public LoadResult RestoreFromBackup()
        {
            try
            {
                string backupToUse = FindLatestBackup();
                
                if (string.IsNullOrEmpty(backupToUse))
                {
                    LogError("No backup file found to restore from");
                    return LoadResult.FileNotFound;
                }

                if (File.Exists(_savePath))
                {
                    File.Delete(_savePath);
                }

                File.Copy(backupToUse, _savePath);
                
                LoadResult loadResult = PerformLoad(DataType.Main);
                if (loadResult == LoadResult.Success)
                {
                    LogDebug($"Successfully restored configuration from backup: {Path.GetFileName(backupToUse)}");
                    return LoadResult.LoadedFromBackup;
                }
                
                return loadResult;
            }
            catch (Exception ex)
            {
                LogError($"Failed to restore from backup: {ex.Message}");
                return LoadResult.Failed;
            }
        }

        /// <summary>
        /// Crea un respaldo manual de la configuración actual
        /// </summary>
        /// <param name="useTimestamp">Si usar timestamp en el nombre del archivo</param>
        /// <returns>True si el respaldo fue exitoso</returns>
        public bool CreateBackup(bool useTimestamp = false)
        {
            try
            {
                if (!File.Exists(_savePath))
                {
                    LogError("No save file exists to backup");
                    return false;
                }

                string backupPath = useTimestamp || enableTimestampedBackups 
                    ? GetTimestampedBackupPath() 
                    : _backupPath;

                // Crear directorio de backup si no existe
                string backupDir = Path.GetDirectoryName(backupPath);
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                File.Copy(_savePath, backupPath, true);
                LogFileOperation($"Backup created: {Path.GetFileName(backupPath)}");
                
                // Limpiar backups antiguos si es necesario
                CleanupOldBackups();
                
                OnBackupCreated?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to create backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Marca que hay cambios sin guardar
        /// </summary>
        /// <param name="updateTimestamp">Si actualizar el timestamp</param>
        public void MarkAsUnsaved(bool updateTimestamp = true)
        {
            _hasUnsavedChanges = true;
            
            if (updateTimestamp)
            {
                _lastSaveTime = Time.time;
            }
        }

        /// <summary>
        /// Fuerza un guardado automático inmediato
        /// </summary>
        /// <returns>Resultado del guardado</returns>
        public SaveResult ForceAutoSave()
        {
            if (!enableAutoSave || !_hasUnsavedChanges)
            {
                return SaveResult.NoData;
            }

            LogDebug("Forcing auto-save");
            OnAutoSaveTriggered?.Invoke(Time.time - _lastSaveTime);
            return SaveData();
        }

        /// <summary>
        /// Verifica si existen datos guardados
        /// </summary>
        /// <returns>True si existe un archivo de guardado</returns>
        public bool HasSavedData()
        {
            return File.Exists(_savePath);
        }

        /// <summary>
        /// Verifica si existe un archivo de respaldo
        /// </summary>
        /// <returns>True si existe al menos un archivo de respaldo</returns>
        public bool HasBackup()
        {
            return File.Exists(_backupPath) || GetBackupFiles().Length > 0;
        }

        /// <summary>
        /// Obtiene información detallada sobre los archivos de guardado
        /// </summary>
        /// <returns>Información completa de los archivos</returns>
        public SaveFileInfo GetSaveFileInfo()
        {
            var info = new SaveFileInfo
            {
                HasUnsavedChanges = _hasUnsavedChanges,
                AutoSaveEnabled = enableAutoSave,
                NextAutoSaveIn = NextAutoSaveIn,
                RuntimeDataCount = _runtimeData.Count
            };
            
            // Información del archivo principal
            if (File.Exists(_savePath))
            {
                var fileInfo = new FileInfo(_savePath);
                info.HasMainFile = true;
                info.MainFileSize = fileInfo.Length;
                info.MainFileLastModified = fileInfo.LastWriteTime;
                info.MainFilePath = _savePath;
            }
            
            // Información de backups
            var backupFiles = GetBackupFiles();
            info.BackupCount = backupFiles.Length;
            
            if (backupFiles.Length > 0)
            {
                var latestBackup = backupFiles[0]; // Los archivos están ordenados por fecha
                var fileInfo = new FileInfo(latestBackup);
                info.HasBackupFile = true;
                info.BackupFileSize = fileInfo.Length;
                info.BackupFileLastModified = fileInfo.LastWriteTime;
                info.BackupFilePath = latestBackup;
            }
            
            return info;
        }

        /// <summary>
        /// Obtiene estadísticas de uso del sistema de guardado
        /// </summary>
        /// <returns>Estadísticas detalladas</returns>
        public SaveStatistics GetSaveStatistics()
        {
            var stats = new SaveStatistics();
            var info = GetSaveFileInfo();
            
            stats.TotalBackups = info.BackupCount;
            stats.MainFileExists = info.HasMainFile;
            stats.HasBackups = info.HasBackupFile;
            stats.UnsavedChanges = info.HasUnsavedChanges;
            stats.RuntimeDataEntries = info.RuntimeDataCount;
            
            if (info.HasMainFile)
            {
                stats.MainFileSizeKB = info.MainFileSize / 1024f;
            }
            
            return stats;
        }
        #endregion

        #region Private Helper Methods
        private void InitializePaths()
        {
            try
            {
                string dataPath = Application.persistentDataPath;
                _savePath = Path.Combine(dataPath, saveFileName);
                _backupPath = Path.Combine(dataPath, backupFileName);
                _backupDirectory = Path.Combine(dataPath, "Backups");
                
                // Asegurar que los directorios existen
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }
                
                if (enableTimestampedBackups && !Directory.Exists(_backupDirectory))
                {
                    Directory.CreateDirectory(_backupDirectory);
                }
                
                LogDebug($"Save system initialized. Save path: {_savePath}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize save paths: {ex.Message}");
            }
        }

        private void ValidateConfiguration()
        {
            if (iotConfigurationData == null)
            {
                LogError("IoT Configuration Data is not assigned in the inspector!");
            }

            if (autoSaveInterval < minAutoSaveInterval)
            {
                Debug.LogWarning($"Auto-save interval is too low, setting to minimum of {minAutoSaveInterval} seconds");
                autoSaveInterval = minAutoSaveInterval;
            }

            if (useEncryption && string.IsNullOrEmpty(encryptionKey))
            {
                Debug.LogWarning("Encryption enabled but no key provided. Disabling encryption.");
                useEncryption = false;
            }

            if (maxBackupFiles < 1)
            {
                maxBackupFiles = 1;
            }
        }

        private SaveResult PerformSave(DataType dataType)
        {
            try
            {
                // Crear backup antes de guardar (si está habilitado)
                if (enableBackup && createBackupBeforeSave && File.Exists(_savePath))
                {
                    CreateBackup();
                }

                string dataToSave = "";
                
                switch (dataType)
                {
                    case DataType.Main:
                        dataToSave = JsonUtility.ToJson(iotConfigurationData, true);
                        break;
                    case DataType.RuntimeCache:
                        dataToSave = JsonUtility.ToJson(_runtimeData, true);
                        break;
                    default:
                        LogError($"Unsupported data type for saving: {dataType}");
                        return SaveResult.InvalidData;
                }
                
                if (string.IsNullOrEmpty(dataToSave))
                {
                    LogError("Failed to serialize configuration data");
                    return SaveResult.InvalidData;
                }

                // Aplicar encriptación si está habilitada
                if (useEncryption)
                {
                    dataToSave = EncryptData(dataToSave);
                }

                // Aplicar compresión si está habilitada
                if (useCompression)
                {
                    dataToSave = CompressData(dataToSave);
                }

                // Escribir usando operación atómica
                string targetPath = dataType == DataType.RuntimeCache 
                    ? Path.ChangeExtension(_savePath, ".runtime.json")
                    : _savePath;
                    
                if (!WriteFileAtomic(targetPath, dataToSave))
                {
                    return SaveResult.Failed;
                }

                _hasUnsavedChanges = false;
                LogFileOperation($"Configuration data saved successfully ({dataType})");
                
                OnDataSaved?.Invoke();
                return SaveResult.Success;
            }
            catch (UnauthorizedAccessException)
            {
                return SaveResult.AccessDenied;
            }
            catch (Exception ex)
            {
                LogError($"Save operation failed: {ex.Message}");
                return SaveResult.Failed;
            }
        }

        private LoadResult PerformLoad(DataType dataType)
        {
            try
            {
                string targetPath = dataType == DataType.RuntimeCache 
                    ? Path.ChangeExtension(_savePath, ".runtime.json")
                    : _savePath;

                if (File.Exists(targetPath))
                {
                    string data = File.ReadAllText(targetPath);
                    
                    if (string.IsNullOrEmpty(data))
                    {
                        LogError("Save file is empty");
                        return TryLoadFromBackup();
                    }

                    // Aplicar descompresión si es necesario
                    if (useCompression)
                    {
                        data = DecompressData(data);
                    }

                    // Aplicar desencriptación si es necesario
                    if (useEncryption)
                    {
                        data = DecryptData(data);
                    }

                    // Validar JSON antes de aplicar
                    if (validateJsonOnLoad && !IsValidJson(data, dataType))
                    {
                        LogError("Save file contains invalid JSON");
                        return TryLoadFromBackup();
                    }

                    // Aplicar datos según el tipo
                    switch (dataType)
                    {
                        case DataType.Main:
                            JsonUtility.FromJsonOverwrite(data, iotConfigurationData);
                            break;
                        case DataType.RuntimeCache:
                            _runtimeData = JsonUtility.FromJson<Dictionary<string, object>>(data) ?? new Dictionary<string, object>();
                            break;
                    }

                    _hasUnsavedChanges = false;
                    
                    LogFileOperation($"Configuration data loaded successfully ({dataType})");
                    OnDataLoaded?.Invoke();
                    return LoadResult.Success;
                }
                else
                {
                    LogDebug($"No save file found for {dataType}, checking alternatives");
                    return dataType == DataType.Main ? TryLoadFromBackup() : LoadResult.FileNotFound;
                }
            }
            catch (Exception ex)
            {
                LogError($"Load operation failed: {ex.Message}");
                return dataType == DataType.Main ? TryLoadFromBackup() : LoadResult.Failed;
            }
        }

        private LoadResult TryLoadFromBackup()
        {
            try
            {
                string backupToUse = FindLatestBackup();
                
                if (!string.IsNullOrEmpty(backupToUse))
                {
                    LogDebug($"Attempting to load from backup file: {Path.GetFileName(backupToUse)}");
                    string data = File.ReadAllText(backupToUse);
                    
                    if (!string.IsNullOrEmpty(data))
                    {
                        // Aplicar descompresión/desencriptación si es necesario
                        if (useCompression) data = DecompressData(data);
                        if (useEncryption) data = DecryptData(data);
                        
                        if (!validateJsonOnLoad || IsValidJson(data, DataType.Main))
                        {
                            JsonUtility.FromJsonOverwrite(data, iotConfigurationData);
                            _hasUnsavedChanges = false;
                            
                            LogDebug("Configuration loaded from backup successfully");
                            OnDataLoaded?.Invoke();
                            return LoadResult.LoadedFromBackup;
                        }
                    }
                }
                
                if (createDefaultIfInvalid)
                {
                    LogDebug("No valid backup found, creating default configuration");
                    ResetConfigurationToDefaults();
                    return LoadResult.UsingDefaults;
                }
                
                LogDebug("No valid backup found, keeping current configuration");
                return LoadResult.FileNotFound;
            }
            catch (Exception ex)
            {
                LogError($"Failed to load from backup: {ex.Message}");
                return LoadResult.Failed;
            }
        }

        private bool IsValidJson(string json, DataType dataType)
        {
            try
            {
                switch (dataType)
                {
                    case DataType.Main:
                        var tempData = ScriptableObject.CreateInstance<IotConfigurationData>();
                        JsonUtility.FromJsonOverwrite(json, tempData);
                        DestroyImmediate(tempData);
                        break;
                    case DataType.RuntimeCache:
                        JsonUtility.FromJson<Dictionary<string, object>>(json);
                        break;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ResetConfigurationToDefaults()
        {
            if (iotConfigurationData != null)
            {
                // Aquí puedes implementar la lógica específica para resetear
                // los valores por defecto del ScriptableObject
                LogDebug("Resetting configuration to default values");
            }
        }

        private void HandleAutoSave()
        {
            if (enableAutoSave && _hasUnsavedChanges && 
                Time.time - _lastSaveTime > autoSaveInterval)
            {
                if (logAutoSave)
                {
                    LogDebug("Auto-saving configuration data");
                }
                
                OnAutoSaveTriggered?.Invoke(Time.time - _lastSaveTime);
                SaveData();
            }
        }

        private bool WriteFileAtomic(string filePath, string content)
        {
            try
            {
                string tempPath = filePath + TEMP_FILE_SUFFIX;
                File.WriteAllText(tempPath, content);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.Move(tempPath, filePath);
                
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Atomic write failed: {ex.Message}");
                return false;
            }
        }

        private string GetTimestampedBackupPath()
        {
            string timestamp = DateTime.Now.ToString(TIMESTAMP_FORMAT);
            string fileName = Path.GetFileNameWithoutExtension(saveFileName);
            string extension = Path.GetExtension(saveFileName);
            return Path.Combine(_backupDirectory, $"{fileName}_backup_{timestamp}{extension}");
        }

        private string FindLatestBackup()
        {
            if (File.Exists(_backupPath))
            {
                return _backupPath;
            }

            var backupFiles = GetBackupFiles();
            return backupFiles.Length > 0 ? backupFiles[0] : null;
        }

        private string[] GetBackupFiles()
        {
            try
            {
                if (!Directory.Exists(_backupDirectory))
                {
                    return new string[0];
                }

                string pattern = $"{Path.GetFileNameWithoutExtension(saveFileName)}_backup_*{Path.GetExtension(saveFileName)}";
                var files = Directory.GetFiles(_backupDirectory, pattern);
                
                // Ordenar por fecha de modificación (más reciente primero)
                Array.Sort(files, (x, y) => File.GetLastWriteTime(y).CompareTo(File.GetLastWriteTime(x)));
                
                return files;
            }
            catch (Exception ex)
            {
                LogError($"Failed to get backup files: {ex.Message}");
                return new string[0];
            }
        }

        private void CleanupOldBackups()
        {
            try
            {
                var backupFiles = GetBackupFiles();
                
                if (backupFiles.Length > maxBackupFiles)
                {
                    for (int i = maxBackupFiles; i < backupFiles.Length; i++)
                    {
                        File.Delete(backupFiles[i]);
                        LogFileOperation($"Deleted old backup: {Path.GetFileName(backupFiles[i])}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to cleanup old backups: {ex.Message}");
            }
        }

        private void DeleteAllBackups()
        {
            try
            {
                // Eliminar backup principal
                if (File.Exists(_backupPath))
                {
                    File.Delete(_backupPath);
                    LogFileOperation("Main backup file deleted");
                }

                // Eliminar backups con timestamp
                var backupFiles = GetBackupFiles();
                foreach (string file in backupFiles)
                {
                    File.Delete(file);
                    LogFileOperation($"Timestamped backup deleted: {Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to delete all backups: {ex.Message}");
            }
        }

        private string EncryptData(string data)
        {
            try
            {
                // Implementación básica de encriptación (puedes mejorar esto)
                // Por simplicidad, usamos XOR con la clave
                if (string.IsNullOrEmpty(encryptionKey))
                    return data;

                char[] chars = data.ToCharArray();
                char[] keyChars = encryptionKey.ToCharArray();
                
                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] = (char)(chars[i] ^ keyChars[i % keyChars.Length]);
                }
                
                return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(chars));
            }
            catch (Exception ex)
            {
                LogError($"Encryption failed: {ex.Message}");
                return data;
            }
        }

        private string DecryptData(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptionKey))
                    return data;

                char[] chars = System.Text.Encoding.UTF8.GetChars(Convert.FromBase64String(data));
                char[] keyChars = encryptionKey.ToCharArray();
                
                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] = (char)(chars[i] ^ keyChars[i % keyChars.Length]);
                }
                
                return new string(chars);
            }
            catch (Exception ex)
            {
                LogError($"Decryption failed: {ex.Message}");
                return data;
            }
        }

        private string CompressData(string data)
        {
            try
            {
                // Implementación básica de compresión usando GZip
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
                
                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new System.IO.Compression.GZipStream(memoryStream, System.IO.Compression.CompressionMode.Compress))
                    {
                        gzipStream.Write(bytes, 0, bytes.Length);
                    }
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                LogError($"Compression failed: {ex.Message}");
                return data;
            }
        }

        private string DecompressData(string data)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(data);
                
                using (var memoryStream = new MemoryStream(bytes))
                {
                    using (var gzipStream = new System.IO.Compression.GZipStream(memoryStream, System.IO.Compression.CompressionMode.Decompress))
                    {
                        using (var resultStream = new MemoryStream())
                        {
                            gzipStream.CopyTo(resultStream);
                            return System.Text.Encoding.UTF8.GetString(resultStream.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Decompression failed: {ex.Message}");
                return data;
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SaveSystem] {message}");
            OnSaveError?.Invoke(message);
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[SaveSystem] {message}");
            }
        }

        private void LogFileOperation(string message)
        {
            if (logFileOperations)
            {
                LogDebug(message);
            }
        }
        #endregion

        #region Data Classes
        [System.Serializable]
        public class SaveFileInfo
        {
            [Header("Main File")]
            public bool HasMainFile;
            public long MainFileSize;
            public DateTime MainFileLastModified;
            public string MainFilePath;
            
            [Header("Backup Files")]
            public bool HasBackupFile;
            public long BackupFileSize;
            public DateTime BackupFileLastModified;
            public string BackupFilePath;
            public int BackupCount;
            
            [Header("Runtime Status")]
            public bool HasUnsavedChanges;
            public bool AutoSaveEnabled;
            public float NextAutoSaveIn;
            public int RuntimeDataCount;

            public override string ToString()
            {
                var info = "=== Save File Info ===\n";
                
                // Main file info
                if (HasMainFile)
                {
                    info += $"📄 Main File: {FormatFileSize(MainFileSize)}, modified {MainFileLastModified:yyyy-MM-dd HH:mm:ss}\n";
                    info += $"   Path: {MainFilePath}\n";
                }
                else
                {
                    info += "📄 Main File: Not found\n";
                }
                
                // Backup info
                if (HasBackupFile)
                {
                    info += $"💾 Latest Backup: {FormatFileSize(BackupFileSize)}, modified {BackupFileLastModified:yyyy-MM-dd HH:mm:ss}\n";
                    info += $"   Path: {BackupFilePath}\n";
                    info += $"   Total Backups: {BackupCount}\n";
                }
                else
                {
                    info += "💾 Backup Files: Not found\n";
                }
                
                // Runtime status
                info += $"⚡ Unsaved Changes: {(HasUnsavedChanges ? "Yes" : "No")}\n";
                info += $"🔄 Auto-Save: {(AutoSaveEnabled ? $"Enabled (next in {NextAutoSaveIn:F1}s)" : "Disabled")}\n";
                info += $"🗃️ Runtime Data Entries: {RuntimeDataCount}";
                
                return info;
            }

            private string FormatFileSize(long bytes)
            {
                if (bytes < 1024) return $"{bytes} B";
                if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            }
        }

        [System.Serializable]
        public class SaveStatistics
        {
            [Header("File Statistics")]
            public bool MainFileExists;
            public float MainFileSizeKB;
            public bool HasBackups;
            public int TotalBackups;
            
            [Header("Runtime Statistics")]
            public bool UnsavedChanges;
            public int RuntimeDataEntries;
            
            public override string ToString()
            {
                var stats = "=== Save Statistics ===\n";
                stats += $"📊 Main File: {(MainFileExists ? $"Exists ({MainFileSizeKB:F1} KB)" : "Not found")}\n";
                stats += $"📊 Backups: {TotalBackups} files available\n";
                stats += $"📊 Runtime Data: {RuntimeDataEntries} entries\n";
                stats += $"📊 Status: {(UnsavedChanges ? "Has unsaved changes" : "All data saved")}";
                return stats;
            }
        }

        [System.Serializable]
        public class RuntimeDataEntry
        {
            public string key;
            public string type;
            public string value;
            public DateTime lastModified;

            public RuntimeDataEntry(string key, object value)
            {
                this.key = key;
                this.type = value?.GetType().Name ?? "null";
                this.value = value?.ToString() ?? "null";
                this.lastModified = DateTime.Now;
            }
        }
        #endregion

        #region Public Utility Methods
        /// <summary>
        /// Exporta los datos de configuración a un archivo específico
        /// </summary>
        /// <param name="filePath">Ruta del archivo de destino</param>
        /// <param name="includeRuntimeData">Si incluir datos del runtime</param>
        /// <returns>True si la exportación fue exitosa</returns>
        public bool ExportConfiguration(string filePath, bool includeRuntimeData = false)
        {
            try
            {
                var exportData = new ExportData
                {
                    configurationData = iotConfigurationData,
                    runtimeData = includeRuntimeData ? _runtimeData : null,
                    exportTimestamp = DateTime.Now,
                    version = Application.version
                };

                string json = JsonUtility.ToJson(exportData, true);
                File.WriteAllText(filePath, json);
                
                LogDebug($"Configuration exported to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to export configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Importa configuración desde un archivo específico
        /// </summary>
        /// <param name="filePath">Ruta del archivo de origen</param>
        /// <param name="createBackupBeforeImport">Si crear backup antes de importar</param>
        /// <returns>True si la importación fue exitosa</returns>
        public bool ImportConfiguration(string filePath, bool createBackupBeforeImport = true)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    LogError($"Import file not found: {filePath}");
                    return false;
                }

                if (createBackupBeforeImport && HasSavedData())
                {
                    CreateBackup(true);
                }

                string json = File.ReadAllText(filePath);
                var importData = JsonUtility.FromJson<ExportData>(json);

                if (importData?.configurationData != null)
                {
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(importData.configurationData), iotConfigurationData);
                    
                    if (importData.runtimeData != null)
                    {
                        _runtimeData = importData.runtimeData;
                    }

                    MarkAsUnsaved();
                    LogDebug($"Configuration imported from: {filePath}");
                    return true;
                }

                LogError("Invalid import file format");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Failed to import configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene una lista detallada de todos los datos del runtime
        /// </summary>
        /// <returns>Lista de entradas del runtime</returns>
        public List<RuntimeDataEntry> GetRuntimeDataEntries()
        {
            var entries = new List<RuntimeDataEntry>();
            
            foreach (var kvp in _runtimeData)
            {
                entries.Add(new RuntimeDataEntry(kvp.Key, kvp.Value));
            }
            
            return entries;
        }

        /// <summary>
        /// Limpia todos los datos del runtime
        /// </summary>
        /// <param name="markUnsaved">Si marcar como datos sin guardar</param>
        public void ClearRuntimeData(bool markUnsaved = true)
        {
            int count = _runtimeData.Count;
            _runtimeData.Clear();
            
            if (markUnsaved && count > 0)
            {
                MarkAsUnsaved();
            }
            
            LogDebug($"Cleared {count} runtime data entries");
        }

        /// <summary>
        /// Valida la integridad de los archivos de guardado
        /// </summary>
        /// <returns>Resultado de la validación</returns>
        public ValidationResult ValidateFiles()
        {
            var result = new ValidationResult();
            
            // Validar archivo principal
            if (File.Exists(_savePath))
            {
                try
                {
                    string content = File.ReadAllText(_savePath);
                    result.MainFileValid = IsValidJson(content, DataType.Main);
                    result.MainFileExists = true;
                }
                catch (Exception ex)
                {
                    result.MainFileValid = false;
                    result.ValidationErrors.Add($"Main file error: {ex.Message}");
                }
            }
            
            // Validar backups
            var backups = GetBackupFiles();
            result.BackupCount = backups.Length;
            
            foreach (string backup in backups)
            {
                try
                {
                    string content = File.ReadAllText(backup);
                    if (IsValidJson(content, DataType.Main))
                    {
                        result.ValidBackups++;
                    }
                }
                catch (Exception ex)
                {
                    result.ValidationErrors.Add($"Backup {Path.GetFileName(backup)} error: {ex.Message}");
                }
            }
            
            result.IsValid = result.MainFileValid || result.ValidBackups > 0;
            
            return result;
        }
        #endregion

        #region Additional Data Classes
        [System.Serializable]
        public class ExportData
        {
            public IotConfigurationData configurationData;
            public Dictionary<string, object> runtimeData;
            public DateTime exportTimestamp;
            public string version;
        }

        [System.Serializable]
        public class ValidationResult
        {
            public bool IsValid;
            public bool MainFileExists;
            public bool MainFileValid;
            public int BackupCount;
            public int ValidBackups;
            public List<string> ValidationErrors = new List<string>();

            public override string ToString()
            {
                var result = "=== Validation Result ===\n";
                result += $"✅ Overall Status: {(IsValid ? "Valid" : "Invalid")}\n";
                result += $"📄 Main File: {(MainFileExists ? (MainFileValid ? "Valid" : "Invalid") : "Not found")}\n";
                result += $"💾 Backups: {ValidBackups}/{BackupCount} valid\n";
                
                if (ValidationErrors.Count > 0)
                {
                    result += "❌ Errors:\n";
                    foreach (string error in ValidationErrors)
                    {
                        result += $"   • {error}\n";
                    }
                }
                
                return result.TrimEnd();
            }
        }
        #endregion
    }
}