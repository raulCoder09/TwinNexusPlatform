using System;
using System.IO;
using UnityEngine;

namespace _Scripts.Models.S3Management
{
    [System.Serializable]
    public class S3FileInfo
    {
        public string fileName;
        public string fullKey;
        public long sizeBytes;
        public DateTime lastModified;
        public string fileType;
        public string formattedSize;

        public S3FileInfo(string fileName, string fullKey, long sizeBytes, DateTime lastModified)
        {
            this.fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            this.fullKey = fullKey ?? throw new ArgumentNullException(nameof(fullKey));
            this.sizeBytes = sizeBytes;
            this.lastModified = lastModified;
            this.fileType = GetFileTypeFromExtension(fileName);
            this.formattedSize = FormatFileSize(sizeBytes);
        }

        private string GetFileTypeFromExtension(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".jpg" or ".jpeg" => "Image (JPEG)",
                ".png" => "Image (PNG)",
                ".gif" => "Image (GIF)",
                ".txt" => "Text File",
                ".json" => "JSON Data",
                ".pdf" => "PDF Document",
                ".mp4" => "Video (MP4)",
                ".mp3" => "Audio (MP3)",
                _ => "Unknown File"
            };
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1048576) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1073741824) return $"{bytes / 1048576.0:F1} MB";
            return $"{bytes / 1073741824.0:F1} GB";
        }
    }
}