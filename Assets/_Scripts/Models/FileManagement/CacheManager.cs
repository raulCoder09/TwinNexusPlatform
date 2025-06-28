using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace _Scripts.Models.FileManagement
{
    public class CacheManager
    {
        private readonly FileOperations fileOperations;
        private readonly FileEventManager _fileEventManager;
        private readonly bool enableDebugLogs = true;
        private readonly int maxTempFilesAgeDays;
        private readonly long maxCacheSizeBytes;

        public CacheManager(FileOperations operations, FileEventManager fileEvents, int maxDays = 7, long maxSizeMB = 100)
        {
            fileOperations = operations;
            _fileEventManager = fileEvents;
            maxTempFilesAgeDays = maxDays;
            maxCacheSizeBytes = maxSizeMB * 1024 * 1024; // Convert MB to bytes
        }

        public bool CleanupTemp(StorageInfo.StorageCategory category)
        {
            try
            {
                if (category != StorageInfo.StorageCategory.Temp)
                {
                    LogError($"Invalid category for cleanup: {category}");
                    _fileEventManager.TriggerCleanupComplete(false, "Invalid category", 0);
                    return false;
                }

                List<string> files = fileOperations.ListFiles(category, "*");
                long freedBytes = 0;
                DateTime now = DateTime.Now;

                foreach (string fileName in files)
                {
                    string filePath = fileOperations.ReadFile(category, fileName) != null ? Path.Combine(categoryPaths[category], fileName) : null;
                    if (filePath == null || !File.Exists(filePath)) continue;

                    FileInfo fileInfo = new FileInfo(filePath);
                    if ((now - fileInfo.LastWriteTime).TotalDays > maxTempFilesAgeDays)
                    {
                        long fileSize = fileInfo.Length;
                        fileOperations.DeleteFile(category, fileName);
                        freedBytes += fileSize;
                    }
                }

                long currentSize = GetTempFolderSize(category);
                if (currentSize > maxCacheSizeBytes)
                {
                    files = fileOperations.ListFiles(category, "*");
                    files.Sort((a, b) => File.GetLastWriteTime(Path.Combine(categoryPaths[category], a)).CompareTo(File.GetLastWriteTime(Path.Combine(categoryPaths[category], b))));

                    foreach (string fileName in files)
                    {
                        string filePath = Path.Combine(categoryPaths[category], fileName);
                        if (!File.Exists(filePath)) continue;

                        FileInfo fileInfo = new FileInfo(filePath);
                        long fileSize = fileInfo.Length;
                        fileOperations.DeleteFile(category, fileName);
                        freedBytes += fileSize;

                        currentSize -= fileSize;
                        if (currentSize <= maxCacheSizeBytes) break;
                    }
                }

                LogDebug($"Cleanup completed for {category}. Freed {freedBytes} bytes");
                _fileEventManager.TriggerCleanupComplete(true, "Cleanup successful", freedBytes);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error during cleanup for {category}: {ex.Message}");
                _fileEventManager.TriggerCleanupComplete(false, ex.Message, 0);
                return false;
            }
        }

        public long GetTempFolderSize(StorageInfo.StorageCategory category)
        {
            try
            {
                if (category != StorageInfo.StorageCategory.Temp)
                {
                    LogError($"Invalid category for size calculation: {category}");
                    return 0;
                }

                long totalSize = 0;
                List<string> files = fileOperations.ListFiles(category, "*");

                foreach (string fileName in files)
                {
                    string filePath = Path.Combine(categoryPaths[category], fileName);
                    if (File.Exists(filePath))
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        totalSize += fileInfo.Length;
                    }
                }

                LogDebug($"Calculated size for {category}: {totalSize} bytes");
                return totalSize;
            }
            catch (Exception ex)
            {
                LogError($"Error calculating folder size for {category}: {ex.Message}");
                return 0;
            }
        }

        private Dictionary<StorageInfo.StorageCategory, string> categoryPaths
        {
            get
            {
                // Access categoryPaths from FileOperations (assuming it's accessible)
                // In practice, this would be passed or accessed via FileManager
                return (Dictionary<StorageInfo.StorageCategory, string>)typeof(FileOperations)
                    .GetField("categoryPaths", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(fileOperations);
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[CacheManager] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[CacheManager] {message}");
        }
    }
}