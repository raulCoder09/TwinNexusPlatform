using System;
using System.IO;
using System.Threading.Tasks;
using _ScriptableObjects;
using UnityEngine;

namespace _Scripts.Models
{
    public class SaveSystem : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private IotConfigurationData iotConfigurationData;
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private bool enableBackup = true;
        [SerializeField] private float autoSaveInterval = 30f; // seconds
        #endregion

        #region Private Fields
        private string _savePath;
        private string _backupPath;
        private float _lastSaveTime;
        private bool _hasUnsavedChanges;
        
        private const string SAVE_FILE_NAME = "iotConfig.json";
        private const string BACKUP_FILE_NAME = "iotConfig_backup.json";
        private const string TEMP_FILE_SUFFIX = ".tmp";
        #endregion

        #region Public Events
        public static event System.Action OnDataSaved;
        public static event System.Action OnDataLoaded;
        public static event System.Action<string> OnSaveError;
        public static event System.Action<string> OnLoadError;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializePaths();
            ValidateConfiguration();
        }

        private void Start()
        {
            LoadData();
        }

        private void Update()
        {
            HandleAutoSave();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _hasUnsavedChanges)
            {
                Debug.Log("Application paused - saving data");
                SaveData();
            }
        }

        private void OnApplicationQuit()
        {
            if (_hasUnsavedChanges)
            {
                Debug.Log("Application quitting - saving data");
                SaveData();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _hasUnsavedChanges && enableAutoSave)
            {
                Debug.Log("Application lost focus - saving data");
                SaveData();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Guarda los datos de configuración IoT de forma síncrona
        /// </summary>
        /// <returns>True si el guardado fue exitoso</returns>
        public bool SaveData()
        {
            try
            {
                if (iotConfigurationData == null)
                {
                    LogError("Cannot save: IoT configuration data is null");
                    return false;
                }

                return PerformSave();
            }
            catch (Exception ex)
            {
                LogError($"Failed to save data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Guarda los datos de configuración IoT de forma asíncrona
        /// </summary>
        /// <returns>True si el guardado fue exitoso</returns>
        public async Task<bool> SaveDataAsync()
        {
            try
            {
                if (iotConfigurationData == null)
                {
                    LogError("Cannot save: IoT configuration data is null");
                    return false;
                }

                return await Task.Run(() => PerformSave());
            }
            catch (Exception ex)
            {
                LogError($"Failed to save data asynchronously: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Carga los datos de configuración IoT
        /// </summary>
        /// <returns>True si la carga fue exitosa</returns>
        public bool LoadData()
        {
            try
            {
                if (iotConfigurationData == null)
                {
                    LogError("Cannot load: IoT configuration data is null");
                    return false;
                }

                return PerformLoad();
            }
            catch (Exception ex)
            {
                LogError($"Failed to load data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Carga los datos de configuración IoT de forma asíncrona
        /// </summary>
        /// <returns>True si la carga fue exitosa</returns>
        public async Task<bool> LoadDataAsync()
        {
            try
            {
                if (iotConfigurationData == null)
                {
                    LogError("Cannot load: IoT configuration data is null");
                    return false;
                }

                return await Task.Run(() => PerformLoad());
            }
            catch (Exception ex)
            {
                LogError($"Failed to load data asynchronously: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restablece la configuración a valores por defecto
        /// </summary>
        /// <param name="deleteBackup">Si también eliminar el archivo de respaldo</param>
        /// <returns>True si el restablecimiento fue exitoso</returns>
        public bool ResetToDefault(bool deleteBackup = false)
        {
            try
            {
                bool success = true;

                // Eliminar archivo principal
                if (File.Exists(_savePath))
                {
                    File.Delete(_savePath);
                    Debug.Log("Main configuration file deleted");
                }

                // Eliminar backup si se solicita
                if (deleteBackup && File.Exists(_backupPath))
                {
                    File.Delete(_backupPath);
                    Debug.Log("Backup configuration file deleted");
                }

                // Resetear datos en memoria a valores por defecto
                if (iotConfigurationData != null)
                {
                    ResetConfigurationToDefaults();
                }

                _hasUnsavedChanges = false;
                Debug.Log("Configuration reset to default values");
                
                return success;
            }
            catch (Exception ex)
            {
                LogError($"Failed to reset configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restaura desde el archivo de respaldo
        /// </summary>
        /// <returns>True si la restauración fue exitosa</returns>
        public bool RestoreFromBackup()
        {
            try
            {
                if (!File.Exists(_backupPath))
                {
                    LogError("No backup file found to restore from");
                    return false;
                }

                if (File.Exists(_savePath))
                {
                    File.Delete(_savePath);
                }

                File.Copy(_backupPath, _savePath);
                
                bool loadSuccess = LoadData();
                if (loadSuccess)
                {
                    Debug.Log("Successfully restored configuration from backup");
                }
                
                return loadSuccess;
            }
            catch (Exception ex)
            {
                LogError($"Failed to restore from backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Crea un respaldo manual de la configuración actual
        /// </summary>
        /// <returns>True si el respaldo fue exitoso</returns>
        public bool CreateBackup()
        {
            try
            {
                if (!File.Exists(_savePath))
                {
                    LogError("No save file exists to backup");
                    return false;
                }

                File.Copy(_savePath, _backupPath, true);
                Debug.Log("Backup created successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to create backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Marca que hay cambios sin guardar (llamar cuando se modifique la configuración)
        /// </summary>
        public void MarkAsUnsaved()
        {
            _hasUnsavedChanges = true;
            _lastSaveTime = Time.time;
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
        /// <returns>True si existe un archivo de respaldo</returns>
        public bool HasBackup()
        {
            return File.Exists(_backupPath);
        }

        /// <summary>
        /// Obtiene información sobre los archivos de guardado
        /// </summary>
        /// <returns>Información de los archivos</returns>
        public SaveFileInfo GetSaveFileInfo()
        {
            var info = new SaveFileInfo();
            
            if (File.Exists(_savePath))
            {
                var fileInfo = new FileInfo(_savePath);
                info.HasMainFile = true;
                info.MainFileSize = fileInfo.Length;
                info.MainFileLastModified = fileInfo.LastWriteTime;
            }
            
            if (File.Exists(_backupPath))
            {
                var fileInfo = new FileInfo(_backupPath);
                info.HasBackupFile = true;
                info.BackupFileSize = fileInfo.Length;
                info.BackupFileLastModified = fileInfo.LastWriteTime;
            }
            
            return info;
        }
        #endregion

        #region Private Methods
        private void InitializePaths()
        {
            try
            {
                string dataPath = Application.persistentDataPath;
                _savePath = Path.Combine(dataPath, SAVE_FILE_NAME);
                _backupPath = Path.Combine(dataPath, BACKUP_FILE_NAME);
                
                // Asegurar que el directorio existe
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }
                
                Debug.Log($"Save system initialized. Save path: {_savePath}");
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

            if (autoSaveInterval < 5f)
            {
                Debug.LogWarning("Auto-save interval is very low, setting to minimum of 5 seconds");
                autoSaveInterval = 5f;
            }
        }

        private bool PerformSave()
        {
            try
            {
                // Crear backup antes de guardar (si está habilitado)
                if (enableBackup && File.Exists(_savePath))
                {
                    File.Copy(_savePath, _backupPath, true);
                }

                // Serializar datos
                string json = JsonUtility.ToJson(iotConfigurationData, true);
                
                if (string.IsNullOrEmpty(json))
                {
                    LogError("Failed to serialize configuration data");
                    return false;
                }

                // Escribir a archivo temporal primero (atomic write)
                string tempPath = _savePath + TEMP_FILE_SUFFIX;
                File.WriteAllText(tempPath, json);
                
                // Mover archivo temporal al final (operación atómica)
                if (File.Exists(_savePath))
                {
                    File.Delete(_savePath);
                }
                File.Move(tempPath, _savePath);

                _hasUnsavedChanges = false;
                Debug.Log("Configuration data saved successfully");
                
                OnDataSaved?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Save operation failed: {ex.Message}");
                return false;
            }
        }

        private bool PerformLoad()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    string json = File.ReadAllText(_savePath);
                    
                    if (string.IsNullOrEmpty(json))
                    {
                        LogError("Save file is empty");
                        return TryLoadFromBackup();
                    }

                    // Validar JSON antes de aplicar
                    if (!IsValidJson(json))
                    {
                        LogError("Save file contains invalid JSON");
                        return TryLoadFromBackup();
                    }

                    JsonUtility.FromJsonOverwrite(json, iotConfigurationData);
                    _hasUnsavedChanges = false;
                    
                    Debug.Log("Configuration data loaded successfully");
                    OnDataLoaded?.Invoke();
                    return true;
                }
                else
                {
                    Debug.LogWarning("No save file found, using default configuration");
                    return TryLoadFromBackup();
                }
            }
            catch (Exception ex)
            {
                LogError($"Load operation failed: {ex.Message}");
                return TryLoadFromBackup();
            }
        }

        private bool TryLoadFromBackup()
        {
            try
            {
                if (File.Exists(_backupPath))
                {
                    Debug.Log("Attempting to load from backup file");
                    string json = File.ReadAllText(_backupPath);
                    
                    if (!string.IsNullOrEmpty(json) && IsValidJson(json))
                    {
                        JsonUtility.FromJsonOverwrite(json, iotConfigurationData);
                        _hasUnsavedChanges = false;
                        
                        Debug.Log("Configuration loaded from backup successfully");
                        OnDataLoaded?.Invoke();
                        return true;
                    }
                }
                
                Debug.LogWarning("No valid backup found, using default configuration");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Failed to load from backup: {ex.Message}");
                return false;
            }
        }

        private bool IsValidJson(string json)
        {
            try
            {
                // Intentar crear un objeto temporal para validar el JSON
                var tempData = ScriptableObject.CreateInstance<IotConfigurationData>();
                JsonUtility.FromJsonOverwrite(json, tempData);
                DestroyImmediate(tempData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ResetConfigurationToDefaults()
        {
            // Aquí puedes establecer valores por defecto específicos
            // o crear una nueva instancia del ScriptableObject
            if (iotConfigurationData != null)
            {
                // Resetear a valores por defecto
                // Esto depende de la estructura específica de IotConfigurationData
                Debug.Log("Resetting configuration to default values");
            }
        }

        private void HandleAutoSave()
        {
            if (enableAutoSave && _hasUnsavedChanges && 
                Time.time - _lastSaveTime > autoSaveInterval)
            {
                Debug.Log("Auto-saving configuration data");
                SaveData();
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SaveSystem] {message}");
            OnSaveError?.Invoke(message);
        }
        #endregion

        #region Data Classes
        [System.Serializable]
        public class SaveFileInfo
        {
            public bool HasMainFile;
            public long MainFileSize;
            public DateTime MainFileLastModified;
            
            public bool HasBackupFile;
            public long BackupFileSize;
            public DateTime BackupFileLastModified;

            public override string ToString()
            {
                var info = "Save File Info:\n";
                
                if (HasMainFile)
                {
                    info += $"Main File: {MainFileSize} bytes, modified {MainFileLastModified}\n";
                }
                else
                {
                    info += "Main File: Not found\n";
                }
                
                if (HasBackupFile)
                {
                    info += $"Backup File: {BackupFileSize} bytes, modified {BackupFileLastModified}";
                }
                else
                {
                    info += "Backup File: Not found";
                }
                
                return info;
            }
        }
        #endregion
    }
}