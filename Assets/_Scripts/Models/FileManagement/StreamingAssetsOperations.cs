namespace _Scripts.Models.FileManagement
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Networking;

    public class StreamingAssetsOperations : IFileOperations
    {
        private readonly IStorageProvider _storageProvider;
        private readonly FileEventManager _fileEventManager;
        private readonly MonoBehaviour _coroutineRunner;
        private readonly bool _enableDebugLogs = true;

        public StreamingAssetsOperations(IStorageProvider storageProvider, FileEventManager fileEventManager, MonoBehaviour coroutineRunner)
        {
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
            _fileEventManager = fileEventManager ?? throw new ArgumentNullException(nameof(fileEventManager));
            _coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));
        }

        public void ListFilesAsync(string path, string filter, Action<List<string>> callback)
        {
            try
            {
                if (!_storageProvider.IsPathAccessible(path) || !path.StartsWith(_storageProvider.GetBasePath("streaming")))
                {
                    LogError($"Invalid path for StreamingAssets: {path}");
                    callback?.Invoke(new List<string>());
                    return;
                }

                _coroutineRunner.StartCoroutine(ListFilesCoroutine(path, filter, callback));
            }
            catch (Exception ex)
            {
                LogError($"Error starting ListFilesAsync for {path}: {ex.Message}");
                callback?.Invoke(new List<string>());
            }
        }

        private IEnumerator ListFilesCoroutine(string path, string filter, Action<List<string>> callback)
        {
            List<string> fileNames = new List<string>();

            if (Application.platform == RuntimePlatform.Android)
            {
                UnityWebRequest request = UnityWebRequest.Get(Path.Combine(path, "filelist.txt"));
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    LogError($"Error listing files in {path}: {request.error}");
                    callback?.Invoke(fileNames);
                    yield break;
                }

                string[] files = request.downloadHandler.text.Split('\n');
                foreach (string file in files)
                {
                    if (string.IsNullOrEmpty(file)) continue;
                    if (filter == "*" || file.EndsWith(filter.Replace("*.", ".")))
                        fileNames.Add(Path.GetFileName(file));
                }
            }
            else
            {
                if (!_storageProvider.IsPathAccessible(path))
                {
                    LogError($"Path not accessible: {path}");
                    callback?.Invoke(fileNames);
                    yield break;
                }

                string[] files = Directory.GetFiles(path, filter, SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    fileNames.Add(Path.GetFileName(file));
                }
            }

            LogDebug($"Listed {fileNames.Count} files in {path} with filter {filter}");
            callback?.Invoke(fileNames);
        }

        public void ReadFileAsync(string path, string fileName, Action<string> callback)
        {
            try
            {
                if (!_storageProvider.IsPathAccessible(path) || !path.StartsWith(_storageProvider.GetBasePath("streaming")))
                {
                    LogError($"Invalid path for StreamingAssets: {path}");
                    _fileEventManager.TriggerFileRead(path, fileName, false, "Invalid path");
                    callback?.Invoke(null);
                    return;
                }

                _coroutineRunner.StartCoroutine(ReadFileCoroutine(path, fileName, callback));
            }
            catch (Exception ex)
            {
                LogError($"Error starting ReadFileAsync for {fileName} in {path}: {ex.Message}");
                _fileEventManager.TriggerFileRead(path, fileName, false, ex.Message);
                callback?.Invoke(null);
            }
        }

        private IEnumerator ReadFileCoroutine(string path, string fileName, Action<string> callback)
        {
            string fullPath = Path.Combine(path, fileName);
            string content = null;

            if (Application.platform == RuntimePlatform.Android)
            {
                UnityWebRequest request = UnityWebRequest.Get(fullPath);
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    LogError($"Error reading file {fileName} in {path}: {request.error}");
                    _fileEventManager.TriggerFileRead(path, fileName, false, request.error);
                    callback?.Invoke(null);
                    yield break;
                }

                content = request.downloadHandler.text;
            }
            else
            {
                if (!File.Exists(fullPath))
                {
                    LogWarning($"File not found: {fullPath}");
                    _fileEventManager.TriggerFileRead(path, fileName, false, "File not found");
                    callback?.Invoke(null);
                    yield break;
                }

                content = File.ReadAllText(fullPath);
            }

            _fileEventManager.TriggerFileRead(path, fileName, true, "Read successful");
            LogDebug($"Read file: {fullPath}");
            callback?.Invoke(content);
        }

        public void ReadFileBytesAsync(string path, string fileName, Action<byte[]> callback)
        {
            try
            {
                if (!_storageProvider.IsPathAccessible(path) || !path.StartsWith(_storageProvider.GetBasePath("streaming")))
                {
                    LogError($"Invalid path for StreamingAssets: {path}");
                    _fileEventManager.TriggerFileRead(path, fileName, false, "Invalid path");
                    callback?.Invoke(null);
                    return;
                }

                _coroutineRunner.StartCoroutine(ReadFileBytesCoroutine(path, fileName, callback));
            }
            catch (Exception ex)
            {
                LogError($"Error starting ReadFileBytesAsync for {fileName} in {path}: {ex.Message}");
                _fileEventManager.TriggerFileRead(path, fileName, false, ex.Message);
                callback?.Invoke(null);
            }
        }

        private IEnumerator ReadFileBytesCoroutine(string path, string fileName, Action<byte[]> callback)
        {
            string fullPath = Path.Combine(path, fileName);
            byte[] content = null;

            if (Application.platform == RuntimePlatform.Android)
            {
                UnityWebRequest request = UnityWebRequest.Get(fullPath);
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    LogError($"Error reading file bytes {fileName} in {path}: {request.error}");
                    _fileEventManager.TriggerFileRead(path, fileName, false, request.error);
                    callback?.Invoke(null);
                    yield break;
                }

                content = request.downloadHandler.data;
            }
            else
            {
                if (!File.Exists(fullPath))
                {
                    LogWarning($"File not found: {fullPath}");
                    _fileEventManager.TriggerFileRead(path, fileName, false, "File not found");
                    callback?.Invoke(null);
                    yield break;
                }

                content = File.ReadAllBytes(fullPath);
            }

            _fileEventManager.TriggerFileRead(path, fileName, true, "Read successful");
            LogDebug($"Read file bytes: {fullPath}");
            callback?.Invoke(content);
        }

        public void WriteFileAsync(string path, string fileName, string content, Action<bool> callback)
        {
            LogError($"Write operation not supported for StreamingAssets: {path}/{fileName}");
            _fileEventManager.TriggerFileWritten(path, fileName, false);
            callback?.Invoke(false);
        }

        public void WriteFileAsync(string path, string fileName, byte[] content, Action<bool> callback)
        {
            LogError($"Write operation not supported for StreamingAssets: {path}/{fileName}");
            _fileEventManager.TriggerFileWritten(path, fileName, false);
            callback?.Invoke(false);
        }

        public void CreateFileAsync(string path, string fileName, string content, Action<bool> callback)
        {
            LogError($"Create operation not supported for StreamingAssets: {path}/{fileName}");
            callback?.Invoke(false);
        }

        public void CreateFileAsync(string path, string fileName, byte[] content, Action<bool> callback)
        {
            LogError($"Create operation not supported for StreamingAssets: {path}/{fileName}");
            callback?.Invoke(false);
        }

        public void DeleteFileAsync(string path, string fileName, Action<bool> callback)
        {
            LogError($"Delete file operation not supported for StreamingAssets: {path}/{fileName}");
            callback?.Invoke(false);
        }

        public void CreateFolderAsync(string path, string folderName, Action<bool> callback)
        {
            LogError($"Create folder operation not supported for StreamingAssets: {path}/{folderName}");
            callback?.Invoke(false);
        }

        public void DeleteFolderAsync(string path, string folderName, Action<bool> callback)
        {
            LogError($"Delete folder operation not supported for StreamingAssets: {path}/{folderName}");
            callback?.Invoke(false);
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[StreamingAssetsOperations] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[StreamingAssetsOperations] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[StreamingAssetsOperations] {message}");
        }
    }
}