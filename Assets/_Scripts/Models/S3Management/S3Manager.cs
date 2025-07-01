using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using UnityEngine;

namespace _Scripts.Models.S3Management
{
    public class S3Manager : MonoBehaviour, IS3Service
    {
        [SerializeField] private string _bucketName = "twin-nexus-platform-storage";
        private IS3Service _uploader;
        private IS3Service _downloader;
        private IS3Service _deleter;
        private IS3Service _lister;
        private AmazonS3Client _s3Client;
        private bool _isInitialized = false;

        public event Action<bool, string> OnInitializationCompleted; // Nuevo evento
        public event Action<bool, string, string> OnUploadComplete;
        public event Action<bool, string, byte[]> OnDownloadComplete;
        public event Action<bool, string, List<S3FileInfo>> OnFileListComplete;
        public event Action<bool, string, string> OnFileDeleteComplete;

        public static S3Manager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task<bool> InitializeAsync(AWSCredentials credentials, RegionEndpoint regionEndpoint)
        {
            if (_isInitialized) return true;

            try
            {
                _s3Client = S3ClientFactory.GetS3Client(credentials, regionEndpoint);
                if (_s3Client == null)
                {
                    Debug.LogError("Failed to initialize S3 client");
                    OnInitializationCompleted?.Invoke(false, "Failed to initialize S3 client");
                    return false;
                }

                _uploader = new S3Uploader(_bucketName, _s3Client);
                _downloader = new S3Downloader(_bucketName, _s3Client);
                _deleter = new S3Deleter(_bucketName, _s3Client);
                _lister = new S3Lister(_bucketName, _s3Client);

                // Forward events
                _uploader.OnUploadComplete += (success, message, fileKey) => OnUploadComplete?.Invoke(success, message, fileKey);
                _downloader.OnDownloadComplete += (success, message, fileData) => OnDownloadComplete?.Invoke(success, message, fileData);
                _lister.OnFileListComplete += (success, message, fileList) => OnFileListComplete?.Invoke(success, message, fileList);
                _deleter.OnFileDeleteComplete += (success, message, fileKey) => OnFileDeleteComplete?.Invoke(success, message, fileKey);

                _isInitialized = true;
                Debug.Log("S3Manager initialized successfully");
                OnInitializationCompleted?.Invoke(true, "S3Manager initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize S3Manager: {ex.Message}");
                OnInitializationCompleted?.Invoke(false, ex.Message);
                return false;
            }
        }

        public Task<bool> UploadFileAsync(string fileName, byte[] fileData, string contentType = "application/octet-stream")
            => _uploader.UploadFileAsync(fileName, fileData, contentType);

        public Task<bool> UploadTextFileAsync(string fileName, string content)
            => _uploader.UploadTextFileAsync(fileName, content);

        public Task<byte[]> DownloadFileAsync(string fileKey)
            => _downloader.DownloadFileAsync(fileKey);

        public Task<bool> DownloadFileToPathAsync(string fileKey, string localPath)
            => _downloader.DownloadFileToPathAsync(fileKey, localPath);

        public Task<bool> DeleteFileAsync(string fileKey)
            => _deleter.DeleteFileAsync(fileKey);

        public Task<List<S3FileInfo>> ListFilesAsync(string prefix = null)
            => _lister.ListFilesAsync(prefix);

        private void OnDestroy()
        {
            S3ClientFactory.DisposeClient();
        }
    }
}