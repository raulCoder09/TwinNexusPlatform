using System;
using System.IO;
using UnityEngine;

namespace _Scripts.Models.FileManagement
{
    public class PlatformAdapter
    {
        private readonly bool _enableDebugLogs = true;

        /// <summary>
        /// Gets the platform-specific path for a given route type
        /// </summary>
        public string GetPlatformPath(StorageInfo.StorageCategory category)
        {
            try
            {
                switch (category)
                {
                    case StorageInfo.StorageCategory.Resources:
                        return Application.streamingAssetsPath;
                    case StorageInfo.StorageCategory.Temp:
                        return Application.temporaryCachePath;
                    case StorageInfo.StorageCategory.Saves:
                    case StorageInfo.StorageCategory.Images:
                    case StorageInfo.StorageCategory.Downloads:
                    case StorageInfo.StorageCategory.Root:
                        return Application.persistentDataPath;
                    default:
                        throw new ArgumentException($"Unsupported category: {category}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error getting path for {category}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Normalizes path separators for the current platform
        /// </summary>
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

        /// <summary>
        /// Checks if a route is accessible
        /// </summary>
        public bool IsRouteAccessible(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return false;

                // For StreamingAssets, assume read-only access
                if (path.StartsWith(Application.streamingAssetsPath))
                    return true; // Actual access will be checked during operations

                // For persistentDataPath or temporaryCachePath, check directory existence
                return Directory.Exists(path);
            }
            catch (Exception ex)
            {
                LogError($"Error checking route accessibility for {path}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Requests storage permissions (placeholder for Android/iOS)
        /// </summary>
        public bool RequestPermissions()
        {
            try
            {
                // Placeholder: Actual implementation depends on platform
                // Android: Use AndroidJavaClass for permission requests
                // iOS: Generally not needed for persistentDataPath
                LogDebug("Permission request placeholder executed");
                return true; // Assume success for now
            }
            catch (Exception ex)
            {
                LogError($"Error requesting permissions: {ex.Message}");
                return false;
            }
        }

        // Logging methods
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