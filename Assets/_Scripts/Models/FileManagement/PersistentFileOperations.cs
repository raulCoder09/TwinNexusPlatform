namespace _Scripts.Models.FileManagement
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    public class PersistentFileOperations : IFileOperations
    {
        private readonly IStorageProvider _storageProvider;
        private readonly FileEventManager _fileEventManager;
        private readonly bool _enableDebugLogs = true;

        public PersistentFileOperations(IStorageProvider storageProvider, FileEventManager fileEventManager)
        {
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            _fileEventManager = fileEventManager ?? throw new ArgumentNullException(nameof(fileEventManager));
        }

        public void ListFilesAsync(string path, string filter, Action<List<string>> callback)
        {
            try
            {
                if (!_storageProvider.IsPathAccessible(path))
                {
                    LogError($"Path not accessible: {path}");
                    callback?.Invoke(new List<string>());
                    return;
                }

                string[] files = Directory.GetFiles(path, filter, SearchOption.TopDirectoryOnly);
                List<string> fileNames = new List<string>();
                foreach (string file in files)
                {
                    fileNames.Add(Path.GetFileName(file));
                }

                LogDebug($"Listed {fileNames.Count} files in {path} with filter {filter}");
                callback?.Invoke(fileNames);
            }
            catch (Exception ex)
            {
                LogError($"Error listing files in {path}: {ex.Message}");
                callback?.Invoke(new List<string>());
            }
        }

        public void ReadFileAsync(string path, string fileName, Action<string> callback)
        {
            try
            {
                string fullPath = Path.Combine(path, fileName);
                if (!File.Exists(fullPath))
                {
                    LogWarning($"File not found: {fullPath}");
                    _fileEventManager.TriggerFileRead(path, fileName, false, "File not found");
                    callback?.Invoke(null);
                    return;
                }

                string content = File.ReadAllText(fullPath);
                _fileEventManager.TriggerFileRead(path, fileName, true, "Read successful");
                LogDebug($"Read file: {fullPath}");
                callback?.Invoke(content);
            }
            catch (Exception ex)
            {
                LogError($"Error reading file {fileName} in {path}: {ex.Message}");
                _fileEventManager.TriggerFileRead(path, fileName, false, ex.Message);
                callback?.Invoke(null);
            }
        }

        public void ReadFileBytesAsync(string path, string fileName, Action<byte[]> callback)
        {
            try
            {
                string fullPath = Path.Combine(path, fileName);
                if (!File.Exists(fullPath))
                {
                    LogWarning($"File not found: {fullPath}");
                    _fileEventManager.TriggerFileRead(path, fileName, false, "File not found");
                    callback?.Invoke(null);
                    return;
                }

                byte[] content = File.ReadAllBytes(fullPath);
                _fileEventManager.TriggerFileRead(path, fileName, true, "Read successful");
                LogDebug($"Read file bytes: {fullPath}");
                callback?.Invoke(content);
            }
            catch (Exception ex)
            {
                LogError($"Error reading file bytes {fileName} in {path}: {ex.Message}");
                _fileEventManager.TriggerFileRead(path, fileName, false, ex.Message);
                callback?.Invoke(null);
            }
        }

        public void WriteFileAsync(string path, string fileName, string content, Action<bool> callback)
        {
            try
            {
                string fullPath = Path.Combine(path, fileName);
                File.WriteAllText(fullPath, content);
                _fileEventManager.TriggerFileWritten(path, fileName, true);
                LogDebug($"Wrote file: {fullPath}");
                callback?.Invoke(true);
            }
            catch (Exception ex)
            {
                LogError($"Error writing file {fileName} in {path}: {ex.Message}");
                _fileEventManager.TriggerFileWritten(path, fileName, false);
                callback?.Invoke(false);
            }
        }

        public void WriteFileAsync(string path, string fileName, byte[] content, Action<bool> callback)
        {
            try
            {
                string fullPath = Path.Combine(path, fileName);
                File.WriteAllBytes(fullPath, content);
                _fileEventManager.TriggerFileWritten(path, fileName, true);
                LogDebug($"Wrote file: {fullPath}");
                callback?.Invoke(true);
            }
            catch (Exception ex)
            {
                LogError($"Error writing file {fileName} in {path}: {ex.Message}");
                _fileEventManager.TriggerFileWritten(path, fileName, false);
                callback?.Invoke(false);
            }
        }

        public void CreateFileAsync(string path, string fileName, string content, Action<bool> callback)
        {
            try
            {
                string fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                {
                    LogWarning($"File already exists: {fullPath}");
                    callback?.Invoke(false);
                    return;
                }

                File.WriteAllText(fullPath, content);
                _fileEventManager.TriggerFileCreated(path, fileName);
                LogDebug($"Created file: {fullPath}");
                callback?.Invoke(true);
            }
            catch (Exception ex)
            {
                LogError($"Error creating file {fileName} in {path}: {ex.Message}");
                callback?.Invoke(false);
            }
        }

        public void CreateFileAsync(string path, string fileName, byte[] content, Action<bool> callback)
        {
            try
            {
                string fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                {
                    LogWarning($"File already exists: {fullPath}");
                    callback?.Invoke(false);
                    return;
                }

                File.WriteAllBytes(fullPath, content);
                _fileEventManager.TriggerFileCreated(path, fileName);
                LogDebug($"Created file: {fullPath}");
                callback?.Invoke(true);
            }
            catch (Exception ex)
            {
                LogError($"Error creating file {fileName} in {path}: {ex.Message}");
                callback?.Invoke(false);
            }
        }

        public void DeleteFileAsync(string path, string fileName, Action<bool> callback)
        {
            try
            {
                string fullPath = Path.Combine(path, fileName);
                if (!File.Exists(fullPath))
                {
                    LogWarning($"File not found: {fullPath}");
                    callback?.Invoke(false);
                    return;
                }

                File.Delete(fullPath);
                _fileEventManager.TriggerFileDeleted(path, fileName);
                LogDebug($"Deleted file: {fullPath}");
                callback?.Invoke(true);
            }
            catch (Exception ex)
            {
                LogError($"Error deleting file {fileName} in {path}: {ex.Message}");
                callback?.Invoke(false);
            }
        }

        public void CreateFolderAsync(string path, string folderName, Action<bool> callback)
        {
            try
            {
                string fullPath = Path.Combine(path, folderName);
                if (Directory.Exists(fullPath))
                {
                    LogDebug($"Folder already exists: {fullPath}");
                    callback?.Invoke(true);
                    return;
                }

                Directory.CreateDirectory(fullPath);
                _fileEventManager.TriggerFileCreated(path, folderName);
                LogDebug($"Created folder: {fullPath}");
                callback?.Invoke(true);
            }
            catch (Exception ex)
            {
                LogError($"Error creating folder {folderName} in {path}: {ex.Message}");
                callback?.Invoke(false);
            }
        }

        public void DeleteFolderAsync(string path, string folderName, Action<bool> callback)
        {
            try
            {
                string fullPath = Path.Combine(path, folderName);
                if (!Directory.Exists(fullPath))
                {
                    LogWarning($"Folder not found: {fullPath}");
                    callback?.Invoke(false);
                    return;
                }

                Directory.Delete(fullPath, true);
                _fileEventManager.TriggerFileDeleted(path, folderName);
                LogDebug($"Deleted folder: {fullPath}");
                callback?.Invoke(true);
            }
            catch (Exception ex)
            {
                LogError($"Error deleting folder {folderName} in {path}: {ex.Message}");
                callback?.Invoke(false);
            }
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[PersistentFileOperations] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[PersistentFileOperations] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[PersistentFileOperations] {message}");
        }
    }
}