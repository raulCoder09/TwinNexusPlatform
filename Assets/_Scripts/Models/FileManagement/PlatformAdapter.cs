namespace _Scripts.Models.FileManagement
{
    using System;
    using System.IO;
    using UnityEngine;

    public class PlatformAdapter : IStorageProvider
    {
        private readonly bool _enableDebugLogs = true;

        public string GetBasePath(string context)
        {
            try
            {
                switch (context.ToLower())
                {
                    case "persistent":
                        return Application.persistentDataPath;
                    case "streaming":
                        return Application.streamingAssetsPath;
                    case "temp":
                        return Application.temporaryCachePath;
                    default:
                        LogError($"Unsupported context: {context}");
                        return string.Empty;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error getting base path for {context}: {ex.Message}");
                return string.Empty;
            }
        }

        public bool IsPathAccessible(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return false;

                if (path.StartsWith(Application.streamingAssetsPath))
                    return true; // StreamingAssets is read-only, assume accessible

                return Directory.Exists(path);
            }
            catch (Exception ex)
            {
                LogError($"Error checking accessibility for {path}: {ex.Message}");
                return false;
            }
        }

        public string NormalizePath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return path;

                return path.Replace("\\", "/").TrimEnd('/');
            }
            catch (Exception ex)
            {
                LogError($"Error normalizing path: {ex.Message}");
                return path;
            }
        }

        public bool RequestPermissions()
        {
            try
            {
                LogDebug("Permission request placeholder executed");
                return true; // Placeholder for Android/iOS permission requests
            }
            catch (Exception ex)
            {
                LogError($"Error requesting permissions: {ex.Message}");
                return false;
            }
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[PlatformAdapter] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[PlatformAdapter] {message}");
        }
    }
}