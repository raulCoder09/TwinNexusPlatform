namespace _Scripts.Models.FileManagement
{
    using System;
    using UnityEngine;

    public class FileEventManager
    {
        private readonly bool _enableDebugLogs = true;

        public event Action<string, string> OnFileCreated; // path, fileName
        public event Action<string, string> OnFileDeleted; // path, fileName
        public event Action<string, string, bool, string> OnFileRead; // path, fileName, success, message
        public event Action<string, string, bool> OnFileWritten; // path, fileName, success
        public event Action<bool, string, long> OnCleanupComplete; // success, message, freedBytes
        public event Action<bool, string> OnStorageInitialized; // success, message

        public void SubscribeToFileCreated(Action<string, string> callback)
        {
            OnFileCreated += callback;
            LogDebug("Subscribed to OnFileCreated");
        }

        public void UnsubscribeFromFileCreated(Action<string, string> callback)
        {
            OnFileCreated -= callback;
            LogDebug("Unsubscribed from OnFileCreated");
        }

        public void SubscribeToFileDeleted(Action<string, string> callback)
        {
            OnFileDeleted += callback;
            LogDebug("Subscribed to OnFileDeleted");
        }

        public void UnsubscribeFromFileDeleted(Action<string, string> callback)
        {
            OnFileDeleted -= callback;
            LogDebug("Unsubscribed from OnFileDeleted");
        }

        public void SubscribeToFileRead(Action<string, string, bool, string> callback)
        {
            OnFileRead += callback;
            LogDebug("Subscribed to OnFileRead");
        }

        public void UnsubscribeFromFileRead(Action<string, string, bool, string> callback)
        {
            OnFileRead -= callback;
            LogDebug("Unsubscribed from OnFileRead");
        }

        public void SubscribeToFileWritten(Action<string, string, bool> callback)
        {
            OnFileWritten += callback;
            LogDebug("Subscribed to OnFileWritten");
        }

        public void UnsubscribeFromFileWritten(Action<string, string, bool> callback)
        {
            OnFileWritten -= callback;
            LogDebug("Unsubscribed from OnFileWritten");
        }

        public void SubscribeToCleanupComplete(Action<bool, string, long> callback)
        {
            OnCleanupComplete += callback;
            LogDebug("Subscribed to OnCleanupComplete");
        }

        public void UnsubscribeFromCleanupComplete(Action<bool, string, long> callback)
        {
            OnCleanupComplete -= callback;
            LogDebug("Unsubscribed from OnCleanupComplete");
        }

        public void SubscribeToStorageInitialized(Action<bool, string> callback)
        {
            OnStorageInitialized += callback;
            LogDebug("Subscribed to OnStorageInitialized");
        }

        public void UnsubscribeFromStorageInitialized(Action<bool, string> callback)
        {
            OnStorageInitialized -= callback;
            LogDebug("Unsubscribed from OnStorageInitialized");
        }

        public void TriggerFileCreated(string path, string fileName)
        {
            try
            {
                OnFileCreated?.Invoke(path, fileName);
                LogDebug($"Triggered OnFileCreated: {path}/{fileName}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnFileCreated: {ex.Message}");
            }
        }

        public void TriggerFileDeleted(string path, string fileName)
        {
            try
            {
                OnFileDeleted?.Invoke(path, fileName);
                LogDebug($"Triggered OnFileDeleted: {path}/{fileName}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnFileDeleted: {ex.Message}");
            }
        }

        public void TriggerFileRead(string path, string fileName, bool success, string message)
        {
            try
            {
                OnFileRead?.Invoke(path, fileName, success, message);
                LogDebug($"Triggered OnFileRead: {path}/{fileName}, Success: {success}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnFileRead: {ex.Message}");
            }
        }

        public void TriggerFileWritten(string path, string fileName, bool success)
        {
            try
            {
                OnFileWritten?.Invoke(path, fileName, success);
                LogDebug($"Triggered OnFileWritten: {path}/{fileName}, Success: {success}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnFileWritten: {ex.Message}");
            }
        }

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

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[FileEventManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[FileEventManager] {message}");
        }
    }
}