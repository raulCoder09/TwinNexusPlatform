using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace _Scripts.Models.FileManagement
{
    public class StreamingAssetsHandler
    {
        private readonly PlatformAdapter platformAdapter;
        private readonly FileEventManager _fileEventManager;
        private readonly Dictionary<StorageInfo.StorageCategory, string> categoryPaths;
        private readonly bool enableDebugLogs = true;

        public StreamingAssetsHandler(PlatformAdapter adapter, FileEventManager fileEvents, Dictionary<StorageInfo.StorageCategory, string> paths)
        {
            platformAdapter = adapter;
            _fileEventManager = fileEvents;
            categoryPaths = paths;
        }

        public void ListFilesAsync(StorageInfo.StorageCategory category, string filter, MonoBehaviour coroutineRunner, Action<List<string>> callback)
        {
            try
            {
                if (!categoryPaths.ContainsKey(category) || category != StorageInfo.StorageCategory.Resources)
                {
                    LogError($"Invalid category for StreamingAssets: {category}");
                    callback?.Invoke(new List<string>());
                    return;
                }

                coroutineRunner.StartCoroutine(ListFilesCoroutine(category, filter, callback));
            }
            catch (Exception ex)
            {
                LogError($"Error starting ListFilesAsync for {category}: {ex.Message}");
                callback?.Invoke(new List<string>());
            }
        }

        private IEnumerator ListFilesCoroutine(StorageInfo.StorageCategory category, string filter, Action<List<string>> callback)
        {
            string path = categoryPaths[category];
            List<string> fileNames = new List<string>();

            if (Application.platform == RuntimePlatform.Android)
            {
                // Android requires UnityWebRequest to list StreamingAssets
                UnityWebRequest request = UnityWebRequest.Get(Path.Combine(path, "filelist.txt"));
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    LogError($"Error listing files in {category}: {request.error}");
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
                // Non-Android platforms can use Directory.GetFiles
                if (!platformAdapter.IsRouteAccessible(path))
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

            LogDebug($"Listed {fileNames.Count} files in {category} with filter {filter}");
            callback?.Invoke(fileNames);
        }

        public void ReadFileAsync(StorageInfo.StorageCategory category, string fileName, MonoBehaviour coroutineRunner, Action<string> callback)
        {
            try
            {
                if (!categoryPaths.ContainsKey(category) || category != StorageInfo.StorageCategory.Resources)
                {
                    LogError($"Invalid category for StreamingAssets: {category}");
                    _fileEventManager.TriggerFileRead(category, fileName, false, "Invalid category");
                    callback?.Invoke(null);
                    return;
                }

                coroutineRunner.StartCoroutine(ReadFileCoroutine(category, fileName, callback));
            }
            catch (Exception ex)
            {
                LogError($"Error starting ReadFileAsync for {fileName} in {category}: {ex.Message}");
                _fileEventManager.TriggerFileRead(category, fileName, false, ex.Message);
                callback?.Invoke(null);
            }
        }

        private IEnumerator ReadFileCoroutine(StorageInfo.StorageCategory category, string fileName, Action<string> callback)
        {
            string path = Path.Combine(categoryPaths[category], fileName);
            string content = null;

            if (Application.platform == RuntimePlatform.Android)
            {
                UnityWebRequest request = UnityWebRequest.Get(path);
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    LogError($"Error reading file {fileName} in {category}: {request.error}");
                    _fileEventManager.TriggerFileRead(category, fileName, false, request.error);
                    callback?.Invoke(null);
                    yield break;
                }

                content = request.downloadHandler.text;
            }
            else
            {
                if (!File.Exists(path))
                {
                    LogWarning($"File not found: {path}");
                    _fileEventManager.TriggerFileRead(category, fileName, false, "File not found");
                    callback?.Invoke(null);
                    yield break;
                }

                content = File.ReadAllText(path);
            }

            _fileEventManager.TriggerFileRead(category, fileName, true, "Read successful");
            LogDebug($"Read file: {path}");
            callback?.Invoke(content);
        }

        public bool CopyToPersistent(StorageInfo.StorageCategory sourceCategory, string fileName, StorageInfo.StorageCategory targetCategory)
        {
            try
            {
                if (!categoryPaths.ContainsKey(sourceCategory) || sourceCategory != StorageInfo.StorageCategory.Resources)
                {
                    LogError($"Invalid source category for StreamingAssets: {sourceCategory}");
                    return false;
                }

                if (!categoryPaths.ContainsKey(targetCategory))
                {
                    LogError($"Invalid target category: {targetCategory}");
                    return false;
                }

                string sourcePath = Path.Combine(categoryPaths[sourceCategory], fileName);
                string targetPath = Path.Combine(categoryPaths[targetCategory], fileName);

                if (!File.Exists(sourcePath))
                {
                    LogWarning($"Source file not found: {sourcePath}");
                    return false;
                }

                File.Copy(sourcePath, targetPath, true);
                _fileEventManager.TriggerFileCreated(targetCategory, fileName);
                LogDebug($"Copied file from {sourcePath} to {targetPath}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error copying file {fileName} from {sourceCategory} to {targetCategory}: {ex.Message}");
                return false;
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[StreamingAssetsHandler] {message}");
        }

        private void LogWarning(string message)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[StreamingAssetsHandler] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[StreamingAssetsHandler] {message}");
        }
    }
}