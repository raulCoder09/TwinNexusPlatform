namespace _Scripts.Models.FileManagement
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    public class CacheManager
    {
        private readonly IFileOperations _fileOperations;
        private readonly FileEventManager _fileEventManager;
        private readonly string _tempPath;
        private readonly int _maxTempFilesAgeDays;
        private readonly long _maxCacheSizeBytes;
        private readonly bool _enableDebugLogs = true;

        public CacheManager(IFileOperations fileOperations, FileEventManager fileEventManager, string tempPath, int maxDays = 7, long maxSizeMB = 100)
        {
            _fileOperations = fileOperations ?? throw new ArgumentNullException(nameof(fileOperations));
            _fileEventManager = fileEventManager ?? throw new ArgumentNullException(nameof(fileEventManager));
            _tempPath = tempPath ?? throw new ArgumentNullException(nameof(tempPath));
            _maxTempFilesAgeDays = maxDays;
            _maxCacheSizeBytes = maxSizeMB * 1024 * 1024; // Convert MB to bytes
        }

        public void CleanupTempAsync(Action<bool, long> callback)
        {
            _fileOperations.ListFilesAsync(_tempPath, "*", files =>
            {
                try
                {
                    long freedBytes = 0;
                    DateTime now = DateTime.Now;

                    foreach (string fileName in files)
                    {
                        string filePath = Path.Combine(_tempPath, fileName);
                        if (!File.Exists(filePath)) continue;

                        FileInfo fileInfo = new FileInfo(filePath);
                        if ((now - fileInfo.LastWriteTime).TotalDays > _maxTempFilesAgeDays)
                        {
                            long fileSize = fileInfo.Length;
                            _fileOperations.DeleteFileAsync(_tempPath, fileName, success =>
                            {
                                if (success) freedBytes += fileSize;
                            });
                        }
                    }

                    long currentSize = GetTempFolderSize();
                    if (currentSize > _maxCacheSizeBytes)
                    {
                        _fileOperations.ListFilesAsync(_tempPath, "*", sortedFiles =>
                        {
                            sortedFiles.Sort((a, b) => File.GetLastWriteTime(Path.Combine(_tempPath, a)).CompareTo(File.GetLastWriteTime(Path.Combine(_tempPath, b))));

                            foreach (string fileName in sortedFiles)
                            {
                                string filePath = Path.Combine(_tempPath, fileName);
                                if (!File.Exists(filePath)) continue;

                                FileInfo fileInfo = new FileInfo(filePath);
                                long fileSize = fileInfo.Length;
                                _fileOperations.DeleteFileAsync(_tempPath, fileName, success =>
                                {
                                    if (success)
                                    {
                                        freedBytes += fileSize;
                                        currentSize -= fileSize;
                                    }
                                });

                                if (currentSize <= _maxCacheSizeBytes) break;
                            }

                            LogDebug($"Cleanup completed for {_tempPath}. Freed {freedBytes} bytes");
                            _fileEventManager.TriggerCleanupComplete(true, "Cleanup successful", freedBytes);
                            callback?.Invoke(true, freedBytes);
                        });
                    }
                    else
                    {
                        LogDebug($"Cleanup completed for {_tempPath}. Freed {freedBytes} bytes");
                        _fileEventManager.TriggerCleanupComplete(true, "Cleanup successful", freedBytes);
                        callback?.Invoke(true, freedBytes);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error during cleanup for {_tempPath}: {ex.Message}");
                    _fileEventManager.TriggerCleanupComplete(false, ex.Message, 0);
                    callback?.Invoke(false, 0);
                }
            });
        }

        public long GetTempFolderSize()
        {
            long totalSize = 0;
            _fileOperations.ListFilesAsync(_tempPath, "*", files =>
            {
                try
                {
                    foreach (string fileName in files)
                    {
                        string filePath = Path.Combine(_tempPath, fileName);
                        if (File.Exists(filePath))
                        {
                            FileInfo fileInfo = new FileInfo(filePath);
                            totalSize += fileInfo.Length;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error calculating folder size for {_tempPath}: {ex.Message}");
                }
            });

            LogDebug($"Calculated size for {_tempPath}: {totalSize} bytes");
            return totalSize;
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[CacheManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[CacheManager] {message}");
        }
    }
}