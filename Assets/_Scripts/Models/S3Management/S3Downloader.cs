using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using UnityEngine;

namespace _Scripts.Models.S3Management
{
    public class S3Downloader : IS3Service
    {
        private readonly string _bucketName;
        private AmazonS3Client _s3Client;
        public event Action<bool, string, byte[]> OnDownloadComplete;

        public S3Downloader(string bucketName, AmazonS3Client s3Client)
        {
            _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        }

        public async Task<byte[]> DownloadFileAsync(string fileKey)
        {
            try
            {
                Debug.Log($"Downloading file from S3: {fileKey}");
                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileKey
                };

                using (var response = await _s3Client.GetObjectAsync(request))
                {
                    if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await response.ResponseStream.CopyToAsync(memoryStream);
                            byte[] fileData = memoryStream.ToArray();
                            Debug.Log($"File downloaded successfully: {fileData.Length} bytes");
                            OnDownloadComplete?.Invoke(true, "File downloaded successfully", fileData);
                            return fileData;
                        }
                    }
                    Debug.LogError($"Download failed: {response.HttpStatusCode}");
                    OnDownloadComplete?.Invoke(false, $"Download failed: {response.HttpStatusCode}", null);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"S3 download error: {ex.Message}");
                OnDownloadComplete?.Invoke(false, ex.Message, null);
                return null;
            }
        }

        public async Task<bool> DownloadFileToPathAsync(string fileKey, string localPath)
        {
            try
            {
                byte[] fileData = await DownloadFileAsync(fileKey);
                if (fileData == null || fileData.Length == 0) return false;

                string directory = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllBytesAsync(localPath, fileData);
                Debug.Log($"File saved to: {localPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving downloaded file: {ex.Message}");
                return false;
            }
        }

        public Task<bool> UploadFileAsync(string fileName, byte[] fileData, string contentType = "application/octet-stream") => throw new NotImplementedException();
        public Task<bool> UploadTextFileAsync(string fileName, string content) => throw new NotImplementedException();
        public Task<bool> DeleteFileAsync(string fileKey) => throw new NotImplementedException();
        public Task<List<S3FileInfo>> ListFilesAsync(string prefix = null) => throw new NotImplementedException();
        public event Action<bool, string, string> OnUploadComplete { add { } remove { } }
        public event Action<bool, string, List<S3FileInfo>> OnFileListComplete { add { } remove { } }
        public event Action<bool, string, string> OnFileDeleteComplete { add { } remove { } }
    }
}