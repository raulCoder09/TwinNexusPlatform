namespace _Scripts.Models.FileManagement
{
    using System;
    using UnityEngine;

    [System.Serializable]
    public class StorageInfo
    {
        public string Path;
        public bool IsInitialized;
        public long AvailableSpaceBytes;
        public long TotalSpaceBytes;

        public string GetFormattedAvailableSpace()
        {
            try
            {
                if (AvailableSpaceBytes < 0) return "Unknown";
                if (AvailableSpaceBytes < 1024) return $"{AvailableSpaceBytes} B";
                if (AvailableSpaceBytes < 1048576) return $"{AvailableSpaceBytes / 1024.0:F1} KB";
                if (AvailableSpaceBytes < 1073741824) return $"{AvailableSpaceBytes / 1048576.0:F1} MB";
                return $"{AvailableSpaceBytes / 1073741824.0:F1} GB";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StorageInfo] Error formatting available space: {ex.Message}");
                return "Error";
            }
        }

        public string GetFormattedTotalSpace()
        {
            try
            {
                if (TotalSpaceBytes < 0) return "Unknown";
                if (TotalSpaceBytes < 1024) return $"{TotalSpaceBytes} B";
                if (TotalSpaceBytes < 1048576) return $"{TotalSpaceBytes / 1024.0:F1} KB";
                if (TotalSpaceBytes < 1073741824) return $"{TotalSpaceBytes / 1048576.0:F1} MB";
                return $"{TotalSpaceBytes / 1073741824.0:F1} GB";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StorageInfo] Error formatting total space: {ex.Message}");
                return "Error";
            }
        }
    }
}