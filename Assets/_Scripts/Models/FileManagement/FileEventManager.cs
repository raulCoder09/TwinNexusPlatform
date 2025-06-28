using System;
using UnityEngine;

namespace _Scripts.Models.FileManagement
{
    public class FileEventManager
    {
        // Configuration
        private readonly bool _enableDebugLogs = true;

        // Events
        public event Action<StorageInfo.StorageCategory, string> OnFileCreated; // category, fileName
        public event Action<StorageInfo.StorageCategory, string> OnFileDeleted; // category, fileName
        public event Action<StorageInfo.StorageCategory, string, bool, string> OnFileRead; // category, fileName, success, message
        public event Action<StorageInfo.StorageCategory, string, bool> OnFileWritten; // category, fileName, success
        public event Action<bool, string, long> OnCleanupComplete; // success, message, freedBytes
        public event Action<bool, string> OnStorageInitialized; // success, message

        /// <summary>
        /// Subscribes to the FileCreated event
        /// </summary>
        public void SubscribeToFileCreated(Action<StorageInfo.StorageCategory, string> callback)
        {
            OnFileCreated += callback;
            LogDebug("Subscribed to OnFileCreated");
        }

        /// <summary>
        /// Unsubscribes from the FileCreated event
        /// </summary>
        public void UnsubscribeFromFileCreated(Action<StorageInfo.StorageCategory, string> callback)
        {
            OnFileCreated -= callback;
            LogDebug("Unsubscribed from OnFileCreated");
        }

        /// <summary>
        /// Subscribes to the FileDeleted event
        /// </summary>
        public void SubscribeToFileDeleted(Action<StorageInfo.StorageCategory, string> callback)
        {
            OnFileDeleted += callback;
            LogDebug("Subscribed to OnFileDeleted");
        }

        /// <summary>
        /// Unsubscribes from the FileDeleted event
        /// </summary>
        public void UnsubscribeFromFileDeleted(Action<StorageInfo.StorageCategory, string> callback)
        {
            OnFileDeleted -= callback;
            LogDebug("Unsubscribed from OnFileDeleted");
        }

        /// <summary>
        /// Subscribes to the FileRead event
        /// </summary>
        public void SubscribeToFileRead(Action<StorageInfo.StorageCategory, string, bool, string> callback)
        {
            OnFileRead += callback;
            LogDebug("Subscribed to OnFileRead");
        }

        /// <summary>
        /// Unsubscribes from the FileRead event
        /// </summary>
        public void UnsubscribeFromFileRead(Action<StorageInfo.StorageCategory, string, bool, string> callback)
        {
            OnFileRead -= callback;
            LogDebug("Unsubscribed from OnFileRead");
        }

        /// <summary>
        /// Subscribes to the FileWritten event
        /// </summary>
        public void SubscribeToFileWritten(Action<StorageInfo.StorageCategory, string, bool> callback)
        {
            OnFileWritten += callback;
            LogDebug("Subscribed to OnFileWritten");
        }

        /// <summary>
        /// Unsubscribes from the FileWritten event
        /// </summary>
        public void UnsubscribeFromFileWritten(Action<StorageInfo.StorageCategory, string, bool> callback)
        {
            OnFileWritten -= callback;
            LogDebug("Unsubscribed from OnFileWritten");
        }

        /// <summary>
        /// Subscribes to the CleanupComplete event
        /// </summary>
        public void SubscribeToCleanupComplete(Action<bool, string, long> callback)
        {
            OnCleanupComplete += callback;
            LogDebug("Subscribed to OnCleanupComplete");
        }

        /// <summary>
        /// Unsubscribes from the CleanupComplete event
        /// </summary>
        public void UnsubscribeFromCleanupComplete(Action<bool, string, long> callback)
        {
            OnCleanupComplete -= callback;
            LogDebug("Unsubscribed from OnCleanupComplete");
        }

        /// <summary>
        /// Subscribes to the StorageInitialized event
        /// </summary>
        public void SubscribeToStorageInitialized(Action<bool, string> callback)
        {
            OnStorageInitialized += callback;
            LogDebug("Subscribed to OnStorageInitialized");
        }

        /// <summary>
        /// Unsubscribes from the StorageInitialized event
        /// </summary>
        public void UnsubscribeFromStorageInitialized(Action<bool, string> callback)
        {
            OnStorageInitialized -= callback;
            LogDebug("Unsubscribed from OnStorageInitialized");
        }

        /// <summary>
        /// Triggers the FileCreated event
        /// </summary>
        public void TriggerFileCreated(StorageInfo.StorageCategory category, string fileName)
        {
            try
            {
                OnFileCreated?.Invoke(category, fileName);
                LogDebug($"Triggered OnFileCreated: {category}/{fileName}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnFileCreated: {ex.Message}");
            }
        }

        /// <summary>
        /// Triggers the FileDeleted event
        /// </summary>
        public void TriggerFileDeleted(StorageInfo.StorageCategory category, string fileName)
        {
            try
            {
                OnFileDeleted?.Invoke(category, fileName);
                LogDebug($"Triggered OnFileDeleted: {category}/{fileName}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnFileDeleted: {ex.Message}");
            }
        }

        /// <summary>
        /// Triggers the FileRead event
        /// </summary>
        public void TriggerFileRead(StorageInfo.StorageCategory category, string fileName, bool success, string message)
        {
            try
            {
                OnFileRead?.Invoke(category, fileName, success, message);
                LogDebug($"Triggered OnFileRead: {category}/{fileName}, Success: {success}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnFileRead: {ex.Message}");
            }
        }

        /// <summary>
        /// Triggers the FileWritten event
        /// </summary>
        public void TriggerFileWritten(StorageInfo.StorageCategory category, string fileName, bool success)
        {
            try
            {
                OnFileWritten?.Invoke(category, fileName, success);
                LogDebug($"Triggered OnFileWritten: {category}/{fileName}, Success: {success}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnFileWritten: {ex.Message}");
            }
        }

        /// <summary>
        /// Triggers the CleanupComplete event
        /// </summary>
        public void TriggerCleanupComplete(bool success, string message, long freedBytes)
        {
            try
            {
                OnCleanupComplete?.Invoke(success, message, freedBytes);
                LogDebug($"Triggered OnCleanupComplete: Success: {success}, Freed: {freedBytes} bytes");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnCleanupComplete: {ex.Message}");
            }
        }

        /// <summary>
        /// Triggers the StorageInitialized event
        /// </summary>
        public void TriggerStorageInitialized(bool success, string message)
        {
            try
            {
                OnStorageInitialized?.Invoke(success, message);
                LogDebug($"Triggered OnStorageInitialized: Success: {success}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnStorageInitialized: {ex.Message}");
            }
        }

        // Logging methods
        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[EventManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[EventManager] {message}");
        }
    }
}