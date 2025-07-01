using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using UnityEngine;

namespace _Scripts.Models.S3Management
{
    public class S3Uploader : IS3Service
    {
        private readonly string _bucketName;
        private AmazonS3Client _s3Client;
        public event Action<bool, string, string> OnUploadComplete;

        public S3Uploader(string bucketName, AmazonS3Client s3Client)
        {
            _bucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        }

        public async Task<bool> UploadFileAsync(string fileName, byte[] fileData, string contentType = "application/octet-stream")
        {
            try
            {
                Debug.Log($"Uploading file to S3: {fileName}");
                using (var stream = new MemoryStream(fileData))
                {
                    var request = new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = fileName,
                        InputStream = stream,
                        ContentType = contentType,
                        ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                    };

                    var response = await _s3Client.PutObjectAsync(request);
                    if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Debug.Log($"File uploaded successfully: {fileName}");
                        OnUploadComplete?.Invoke(true, "File uploaded successfully", fileName);
                        return true;
                    }
                    Debug.LogError($"Upload failed: {response.HttpStatusCode}");
                    OnUploadComplete?.Invoke(false, $"Upload failed: {response.HttpStatusCode}", null);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"S3 upload error: {ex.Message}");
                OnUploadComplete?.Invoke(false, ex.Message, null);
                return false;
            }
        }

        public async Task<bool> UploadTextFileAsync(string fileName, string content)
        {
            byte[] textData = System.Text.Encoding.UTF8.GetBytes(content);
            return await UploadFileAsync(fileName, textData, "text/plain");
        }

        public Task<byte[]> DownloadFileAsync(string fileKey) => throw new NotImplementedException();
        public Task<bool> DownloadFileToPathAsync(string fileKey, string localPath) => throw new NotImplementedException();
        public Task<bool> DeleteFileAsync(string fileKey) => throw new NotImplementedException();
        public Task<List<S3FileInfo>> ListFilesAsync(string prefix = null) => throw new NotImplementedException();
        public event Action<bool, string, byte[]> OnDownloadComplete { add { } remove { } }
        public event Action<bool, string, List<S3FileInfo>> OnFileListComplete { add { } remove { } }
        public event Action<bool, string, string> OnFileDeleteComplete { add { } remove { } }
    }
}