using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using UnityEngine;

namespace _Scripts.Models.S3Management
{
    public class S3Lister : IS3Service
    {
        private readonly string _bucketName;
        private AmazonS3Client _s3Client;
        public event Action<bool, string, List<S3FileInfo>> OnFileListComplete;

        public S3Lister(string bucketName, AmazonS3Client s3Client)
        {
            _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        }

        public async Task<List<S3FileInfo>> ListFilesAsync(string prefix = null)
        {
            try
            {
                Debug.Log($"Listing files in S3 bucket: {_bucketName}");
                var request = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = prefix,
                    MaxKeys = 100
                };

                var response = await _s3Client.ListObjectsV2Async(request);
                var fileList = new List<S3FileInfo>();

                foreach (var obj in response.S3Objects)
                {
                    if (obj.Key.EndsWith("/")) continue;
                    string fileName = Path.GetFileName(obj.Key);
                    var fileInfo = new S3FileInfo(fileName, obj.Key, obj.Size ?? 0, obj.LastModified ?? DateTime.UtcNow);
                    fileList.Add(fileInfo);
                }

                Debug.Log($"Found {fileList.Count} files");
                OnFileListComplete?.Invoke(true, $"Found {fileList.Count} files", fileList);
                return fileList;
            }
            catch (Exception ex)
            {
                Debug.LogError($"S3 list error: {ex.Message}");
                OnFileListComplete?.Invoke(false, ex.Message, new List<S3FileInfo>());
                return new List<S3FileInfo>();
            }
        }

        public Task<bool> UploadFileAsync(string fileName, byte[] fileData, string contentType = "application/octet-stream") => throw new NotImplementedException();
        public Task<bool> UploadTextFileAsync(string fileName, string content) => throw new NotImplementedException();
        public Task<byte[]> DownloadFileAsync(string fileKey) => throw new NotImplementedException();
        public Task<bool> DownloadFileToPathAsync(string fileKey, string localPath) => throw new NotImplementedException();
        public Task<bool> DeleteFileAsync(string fileKey) => throw new NotImplementedException();
        public event Action<bool, string, string> OnUploadComplete { add { } remove { } }
        public event Action<bool, string, byte[]> OnDownloadComplete { add { } remove { } }
        public event Action<bool, string, string> OnFileDeleteComplete { add { } remove { } }
    }
}