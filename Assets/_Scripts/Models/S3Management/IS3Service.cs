using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.S3.Model;

namespace _Scripts.Models.S3Management
{
    public interface IS3Service
    {
        Task<bool> UploadFileAsync(string fileName, byte[] fileData, string contentType = "application/octet-stream");
        Task<bool> UploadTextFileAsync(string fileName, string content);
        Task<byte[]> DownloadFileAsync(string fileKey);
        Task<bool> DownloadFileToPathAsync(string fileKey, string localPath);
        Task<bool> DeleteFileAsync(string fileKey);
        Task<List<S3FileInfo>> ListFilesAsync(string prefix = null);
        event Action<bool, string, string> OnUploadComplete; // success, message, fileKey
        event Action<bool, string, byte[]> OnDownloadComplete; // success, message, fileData
        event Action<bool, string, List<S3FileInfo>> OnFileListComplete; // success, message, fileList
        event Action<bool, string, string> OnFileDeleteComplete; // success, message, fileKey
    }
}