using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace _Scripts.Models.FileManagement
{
    public class FileOperations
    {
        private readonly PlatformAdapter platformAdapter;
        private readonly FileEventManager _fileEventManager;
        private readonly Dictionary<StorageInfo.StorageCategory, string> categoryPaths;
        private readonly bool enableDebugLogs = true;

        public FileOperations(PlatformAdapter adapter, FileEventManager fileEvents, Dictionary<StorageInfo.StorageCategory, string> paths)
        {
            platformAdapter = adapter;
            _fileEventManager = fileEvents;
            categoryPaths = paths;
        }

        public List<string> ListFiles(StorageInfo.StorageCategory category, string filter = "*")
        {
            try
            {
                if (!categoryPaths.ContainsKey(category))
                {
                    LogError($"Category not found: {category}");
                    return new List<string>();
                }

                string path = categoryPaths[category];
                if (!platformAdapter.IsRouteAccessible(path))
                {
                    LogError($"Path not accessible: {path}");
                    return new List<string>();
                }

                string[] files = Directory.GetFiles(path, filter, SearchOption.TopDirectoryOnly);
                List<string> fileNames = new List<string>();
                foreach (string file in files)
                {
                    fileNames.Add(Path.GetFileName(file));
                }

                LogDebug($"Listed {fileNames.Count} files in {category} with filter {filter}");
                return fileNames;
            }
            catch (Exception ex)
            {
                LogError($"Error listing files in {category}: {ex.Message}");
                return new List<string>();
            }
        }

        public bool CreateFile(StorageInfo.StorageCategory category, string fileName, string content)
        {
            try
            {
                if (!categoryPaths.ContainsKey(category))
                {
                    LogError($"Category not found: {category}");
                    return false;
                }

                string path = Path.Combine(categoryPaths[category], fileName);
                File.WriteAllText(path, content);
                _fileEventManager.TriggerFileCreated(category, fileName);
                LogDebug($"Created file: {path}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error creating file {fileName} in {category}: {ex.Message}");
                return false;
            }
        }

        public bool CreateFile(StorageInfo.StorageCategory category, string fileName, byte[] content)
        {
            try
            {
                if (!categoryPaths.ContainsKey(category))
                {
                    LogError($"Category not found: {category}");
                    return false;
                }

                string path = Path.Combine(categoryPaths[category], fileName);
                File.WriteAllBytes(path, content);
                _fileEventManager.TriggerFileCreated(category, fileName);
                LogDebug($"Created file: {path}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error creating file {fileName} in {category}: {ex.Message}");
                return false;
            }
        }

        public bool CreateFolder(StorageInfo.StorageCategory category, string folderName)
        {
            try
            {
                if (!categoryPaths.ContainsKey(category))
                {
                    LogError($"Category not found: {category}");
                    return false;
                }

                string path = Path.Combine(categoryPaths[category], folderName);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    _fileEventManager.TriggerFileCreated(category, folderName);
                    LogDebug($"Created folder: {path}");
                }
                else
                {
                    LogDebug($"Folder already exists: {path}");
                }
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error creating folder {folderName} in {category}: {ex.Message}");
                return false;
            }
        }

        public bool DeleteFile(StorageInfo.StorageCategory category, string fileName)
        {
            try
            {
                if (!categoryPaths.ContainsKey(category))
                {
                    LogError($"Category not found: {category}");
                    return false;
                }

                string path = Path.Combine(categoryPaths[category], fileName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    _fileEventManager.TriggerFileDeleted(category, fileName);
                    LogDebug($"Deleted file: {path}");
                    return true;
                }

                LogWarning($"File not found: {path}");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error deleting file {fileName} in {category}: {ex.Message}");
                return false;
            }
        }

        public bool DeleteFolder(StorageInfo.StorageCategory category, string folderName)
        {
            try
            {
                if (!categoryPaths.ContainsKey(category))
                {
                    LogError($"Category not found: {category}");
                    return false;
                }

                string path = Path.Combine(categoryPaths[category], folderName);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    _fileEventManager.TriggerFileDeleted(category, folderName);
                    LogDebug($"Deleted folder: {path}");
                    return true;
                }

                LogWarning($"Folder not found: {path}");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Error deleting folder {folderName} in {category}: {ex.Message}");
                return false;
            }
        }

        public string ReadFile(StorageInfo.StorageCategory category, string fileName)
        {
            try
            {
                if (!categoryPaths.ContainsKey(category))
                {
                    LogError($"Category not found: {category}");
                    _fileEventManager.TriggerFileRead(category, fileName, false, "Category not found");
                    return null;
                }

                string path = Path.Combine(categoryPaths[category], fileName);
                if (!File.Exists(path))
                {
                    LogWarning($"File not found: {path}");
                    _fileEventManager.TriggerFileRead(category, fileName, false, "File not found");
                    return null;
                }

                string content = File.ReadAllText(path);
                _fileEventManager.TriggerFileRead(category, fileName, true, "Read successful");
                LogDebug($"Read file: {path}");
                return content;
            }
            catch (Exception ex)
            {
                LogError($"Error reading file {fileName} in {category}: {ex.Message}");
                _fileEventManager.TriggerFileRead(category, fileName, false, ex.Message);
                return null;
            }
        }

        public byte[] ReadFileBytes(StorageInfo.StorageCategory category, string fileName)
        {
            try
            {
                if (!categoryPaths.ContainsKey(category))
                {
                    LogError($"Category not found: {category}");
                    _fileEventManager.TriggerFileRead(category, fileName, false, "Category not found");
                    return null;
                }

                string path = Path.Combine(categoryPaths[category], fileName);
                if (!File.Exists(path))
                {
                    LogWarning($"File not found: {path}");
                    _fileEventManager.TriggerFileRead(category, fileName, false, "File not found");
                    return null;
                }

                byte[] content = File.ReadAllBytes(path);
                _fileEventManager.TriggerFileRead(category, fileName, true, "Read successful");
                LogDebug($"Read file: {path}");
                return content;
            }
            catch (Exception ex)
            {
                LogError($"Error reading file {fileName} in {category}: {ex.Message}");
                _fileEventManager.TriggerFileRead(category, fileName, false, ex.Message);
                return null;
            }
        }

        public bool WriteFile(StorageInfo.StorageCategory category, string fileName, string content)
        {
            try
            {
                if (!categoryPaths.ContainsKey(category))
                {
                    LogError($"Category not found: {category}");
                    _fileEventManager.TriggerFileWritten(category, fileName, false);
                    return false;
                }

                string path = Path.Combine(categoryPaths[category], fileName);
                File.WriteAllText(path, content);
                _fileEventManager.TriggerFileWritten(category, fileName, true);
                LogDebug($"Wrote file: {path}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error writing file {fileName} in {category}: {ex.Message}");
                _fileEventManager.TriggerFileWritten(category, fileName, false);
                return false;
            }
        }

        public bool WriteFile(StorageInfo.StorageCategory category, string fileName, byte[] content)
        {
            try
            {
                if (!categoryPaths.ContainsKey(category))
                {
                    LogError($"Category not found: {category}");
                    _fileEventManager.TriggerFileWritten(category, fileName, false);
                    return false;
                }

                string path = Path.Combine(categoryPaths[category], fileName);
                File.WriteAllBytes(path, content);
                _fileEventManager.TriggerFileWritten(category, fileName, true);
                LogDebug($"Wrote file: {path}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error writing file {fileName} in {category}: {ex.Message}");
                _fileEventManager.TriggerFileWritten(category, fileName, false);
                return false;
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[FileOperations] {message}");
        }

        private void LogWarning(string message)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[FileOperations] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[FileOperations] {message}");
        }
    }
}