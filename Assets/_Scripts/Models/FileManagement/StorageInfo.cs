using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Models.FileManagement
{
    [System.Serializable]
    public class StorageInfo
    {
        // Storage properties
        public string basePath;
        public bool isInitialized;
        public List<StorageCategory> availableCategories = new List<StorageCategory>();
        public int totalFolders;
        public int totalFiles;
        public long availableSpaceBytes;
        public long totalSpaceBytes;

        // Enum for storage categories
        public enum StorageCategory
        {
            Resources,  // StreamingAssets/resources
            Saves,      // persistentDataPath/saves
            Images,     // persistentDataPath/images
            Downloads,  // persistentDataPath/downloads
            Temp,       // persistentDataPath/temp or temporaryCachePath
            Root,       // Base path
            Certificates // persistentDataPath/certificates
        }

        /// <summary>
        /// Formats available space in human-readable format (B, KB, MB, GB)
        /// </summary>
        /// <returns>Formatted string</returns>
        public string GetFormattedAvailableSpace()
        {
            try
            {
                if (availableSpaceBytes < 0) return "Unknown";
                if (availableSpaceBytes < 1024) return $"{availableSpaceBytes} B";
                if (availableSpaceBytes < 1048576) return $"{availableSpaceBytes / 1024.0:F1} KB";
                if (availableSpaceBytes < 1073741824) return $"{availableSpaceBytes / 1048576.0:F1} MB";
                return $"{availableSpaceBytes / 1073741824.0:F1} GB";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StorageInfo] Error formatting available space: {ex.Message}");
                return "Error";
            }
        }

        /// <summary>
        /// Formats total space in human-readable format (B, KB, MB, GB)
        /// </summary>
        /// <returns>Formatted string</returns>
        public string GetFormattedTotalSpace()
        {
            try
            {
                if (totalSpaceBytes < 0) return "Unknown";
                if (totalSpaceBytes < 1024) return $"{totalSpaceBytes} B";
                if (totalSpaceBytes < 1048576) return $"{totalSpaceBytes / 1024.0:F1} KB";
                if (totalSpaceBytes < 1073741824) return $"{totalSpaceBytes / 1048576.0:F1} MB";
                return $"{totalSpaceBytes / 1073741824.0:F1} GB";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StorageInfo] Error formatting total space: {ex.Message}");
                return "Error";
            }
        }
    }
}