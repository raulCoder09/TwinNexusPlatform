using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using UnityEngine;

namespace _Scripts.Models.S3Management
{
    public class S3Deleter : IS3Service
    {
        private readonly string _bucketName;
        private AmazonS3Client _s3Client;
        public event Action<bool, string, string> OnFileDeleteComplete;

        public S3Deleter(string bucketName, AmazonS3Client s3Client)
        {
            _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        }

        public async Task<bool> DeleteFileAsync(string fileKey)
        {
            try
            {
                Debug.Log($"Deleting file from S3: {fileKey}");
                var request = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileKey
                };

                var response = await _s3Client.DeleteObjectAsync(request);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    Debug.Log($"File deleted successfully: {fileKey}");
                    OnFileDeleteComplete?.Invoke(true, "File deleted successfully", fileKey);
                    return true;
                }
                Debug.LogError($"Delete failed: {response.HttpStatusCode}");
                OnFileDeleteComplete?.Invoke(false, $"Delete failed: {response.HttpStatusCode}", null);
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"S3 delete error: {ex.Message}");
                OnFileDeleteComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        public Task<bool> UploadFileAsync(string fileName, byte[] fileData, string contentType = "application/octet-stream") => throw new NotImplementedException();
        public Task<bool> UploadTextFileAsync(string fileName, string content) => throw new NotImplementedException();
        public Task<byte[]> DownloadFileAsync(string fileKey) => throw new NotImplementedException();
        public Task<bool> DownloadFileToPathAsync(string fileKey, string localPath) => throw new NotImplementedException();
        public Task<List<S3FileInfo>> ListFilesAsync(string prefix = null) => throw new NotImplementedException();
        public event Action<bool, string, string> OnUploadComplete { add { } remove { } }
        public event Action<bool, string, byte[]> OnDownloadComplete { add { } remove { } }
        public event Action<bool, string, List<S3FileInfo>> OnFileListComplete { add { } remove { } }
    }
}