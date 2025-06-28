using System;
using System.Collections.Generic;
using _Scripts.Models.FileManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.Testing
{
    public class FileTesting : MonoBehaviour
    {
        [SerializeField] private bool enableDebugLogs = true;

        private InputAction createFileAction;
        private InputAction createFolderAction;
        private InputAction listFilesAction;
        private InputAction readFileAction;
        private InputAction readFileAsyncAction;
        private InputAction deleteFileAction;
        private InputAction deleteFolderAction;
        private InputAction copyFileAction;
        private InputAction cleanupAction;
        private InputAction getInfoAction;

        private void Awake()
        {
            if (!FileManager.Instance.Initialize())
            {
                LogError("Failed to initialize FileManager");
            }
        }

        private void OnEnable()
        {
            createFileAction = new InputAction("CreateFile", InputActionType.Button, "<Keyboard>/c");
            createFolderAction = new InputAction("CreateFolder", InputActionType.Button, "<Keyboard>/f");
            listFilesAction = new InputAction("ListFiles", InputActionType.Button, "<Keyboard>/l");
            readFileAction = new InputAction("ReadFile", InputActionType.Button, "<Keyboard>/r");
            readFileAsyncAction = new InputAction("ReadFileAsync", InputActionType.Button, "<Keyboard>/a");
            deleteFileAction = new InputAction("DeleteFile", InputActionType.Button, "<Keyboard>/d");
            deleteFolderAction = new InputAction("DeleteFolder", InputActionType.Button, "<Keyboard>/e");
            copyFileAction = new InputAction("CopyFile", InputActionType.Button, "<Keyboard>/p");
            cleanupAction = new InputAction("Cleanup", InputActionType.Button, "<Keyboard>/t");
            getInfoAction = new InputAction("GetInfo", InputActionType.Button, "<Keyboard>/i");

            createFileAction.performed += _ => TestCreateFile();
            createFolderAction.performed += _ => TestCreateFolder();
            listFilesAction.performed += _ => TestListFiles();
            readFileAction.performed += _ => TestReadFile();
            readFileAsyncAction.performed += _ => TestReadFileAsync();
            deleteFileAction.performed += _ => TestDeleteFile();
            deleteFolderAction.performed += _ => TestDeleteFolder();
            copyFileAction.performed += _ => TestCopyFile();
            cleanupAction.performed += _ => TestCleanup();
            getInfoAction.performed += _ => TestGetStorageInfo();

            createFileAction.Enable();
            createFolderAction.Enable();
            listFilesAction.Enable();
            readFileAction.Enable();
            readFileAsyncAction.Enable();
            deleteFileAction.Enable();
            deleteFolderAction.Enable();
            copyFileAction.Enable();
            cleanupAction.Enable();
            getInfoAction.Enable();

            // Subscribe to FileManager events with object casting
            FileManager.Instance.SubscribeToEvent(nameof(FileEventManager.OnFileCreated), (object args) =>
            {
                var (category, fileName) = ((StorageInfo.StorageCategory, string))args;
                OnFileCreated(category, fileName);
            });
            FileManager.Instance.SubscribeToEvent(nameof(FileEventManager.OnFileDeleted), (object args) =>
            {
                var (category, fileName) = ((StorageInfo.StorageCategory, string))args;
                OnFileDeleted(category, fileName);
            });
            FileManager.Instance.SubscribeToEvent(nameof(FileEventManager.OnFileRead), (object args) =>
            {
                var (category, fileName, success, message) = ((StorageInfo.StorageCategory, string, bool, string))args;
                OnFileRead(category, fileName, success, message);
            });
            FileManager.Instance.SubscribeToEvent(nameof(FileEventManager.OnFileWritten), (object args) =>
            {
                var (category, fileName, success) = ((StorageInfo.StorageCategory, string, bool))args;
                OnFileWritten(category, fileName, success);
            });
            FileManager.Instance.SubscribeToEvent(nameof(FileEventManager.OnCleanupComplete), (object args) =>
            {
                var (success, message, freedBytes) = ((bool, string, long))args;
                OnCleanupComplete(success, message, freedBytes);
            });
            FileManager.Instance.SubscribeToEvent(nameof(FileEventManager.OnStorageInitialized), (object args) =>
            {
                var (success, message) = ((bool, string))args;
                OnStorageInitialized(success, message);
            });
        }

        private void OnDisable()
        {
            // Unsubscribe from events with object casting
            FileManager.Instance.UnsubscribeFromEvent(nameof(FileEventManager.OnFileCreated), (object args) =>
            {
                var (category, fileName) = ((StorageInfo.StorageCategory, string))args;
                OnFileCreated(category, fileName);
            });
            FileManager.Instance.UnsubscribeFromEvent(nameof(FileEventManager.OnFileDeleted), (object args) =>
            {
                var (category, fileName) = ((StorageInfo.StorageCategory, string))args;
                OnFileDeleted(category, fileName);
            });
            FileManager.Instance.UnsubscribeFromEvent(nameof(FileEventManager.OnFileRead), (object args) =>
            {
                var (category, fileName, success, message) = ((StorageInfo.StorageCategory, string, bool, string))args;
                OnFileRead(category, fileName, success, message);
            });
            FileManager.Instance.UnsubscribeFromEvent(nameof(FileEventManager.OnFileWritten), (object args) =>
            {
                var (category, fileName, success) = ((StorageInfo.StorageCategory, string, bool))args;
                OnFileWritten(category, fileName, success);
            });
            FileManager.Instance.UnsubscribeFromEvent(nameof(FileEventManager.OnCleanupComplete), (object args) =>
            {
                var (success, message, freedBytes) = ((bool, string, long))args;
                OnCleanupComplete(success, message, freedBytes);
            });
            FileManager.Instance.UnsubscribeFromEvent(nameof(FileEventManager.OnStorageInitialized), (object args) =>
            {
                var (success, message) = ((bool, string))args;
                OnStorageInitialized(success, message);
            });

            createFileAction.Disable();
            createFolderAction.Disable();
            listFilesAction.Disable();
            readFileAction.Disable();
            readFileAsyncAction.Disable();
            deleteFileAction.Disable();
            deleteFolderAction.Disable();
            copyFileAction.Disable();
            cleanupAction.Disable();
            getInfoAction.Disable();
        }

        private void TestCreateFile()
        {
            string content = "Test content at " + DateTime.Now;
            bool success = FileManager.Instance.CreateFile(StorageInfo.StorageCategory.Saves, "test.txt", content);
            LogDebug($"TestCreateFile: {(success ? "Success" : "Failed")}");
        }

        private void TestCreateFolder()
        {
            bool success = FileManager.Instance.CreateFolder(StorageInfo.StorageCategory.Images, "testFolder");
            LogDebug($"TestCreateFolder: {(success ? "Success" : "Failed")}");
        }

        private void TestListFiles()
        {
            List<string> files = FileManager.Instance.ListFiles(StorageInfo.StorageCategory.Saves, "*.txt");
            LogDebug($"TestListFiles (Saves): Found {files.Count} files");
            foreach (string file in files)
            {
                LogDebug($" - {file}");
            }

            FileManager.Instance.ListFilesAsync(StorageInfo.StorageCategory.Resources, "*.txt", filesAsync =>
            {
                LogDebug($"TestListFilesAsync (Resources): Found {filesAsync.Count} files");
                foreach (string file in filesAsync)
                {
                    LogDebug($" - {file}");
                }
            });
        }

        private void TestReadFile()
        {
            string content = FileManager.Instance.ReadFile(StorageInfo.StorageCategory.Saves, "test.txt");
            LogDebug($"TestReadFile: {(content != null ? $"Content: {content}" : "Failed")}");
        }

        private void TestReadFileAsync()
        {
            FileManager.Instance.ReadFileAsync(StorageInfo.StorageCategory.Resources, "test.txt", content =>
            {
                LogDebug($"TestReadFileAsync: {(content != null ? $"Content: {content}" : "Failed")}");
            });
        }

        private void TestDeleteFile()
        {
            bool success = FileManager.Instance.DeleteFile(StorageInfo.StorageCategory.Saves, "test.txt");
            LogDebug($"TestDeleteFile: {(success ? "Success" : "Failed")}");
        }

        private void TestDeleteFolder()
        {
            bool success = FileManager.Instance.DeleteFolder(StorageInfo.StorageCategory.Images, "testFolder");
            LogDebug($"TestDeleteFolder: {(success ? "Success" : "Failed")}");
        }

        private void TestCopyFile()
        {
            bool success = FileManager.Instance.CopyFile(StorageInfo.StorageCategory.Resources, StorageInfo.StorageCategory.Downloads, "test.txt");
            LogDebug($"TestCopyFile: {(success ? "Success" : "Failed")}");
        }

        private void TestCleanup()
        {
            bool success = FileManager.Instance.CleanupTemp();
            LogDebug($"TestCleanup: {(success ? "Success" : "Failed")}");
        }

        private void TestGetStorageInfo()
        {
            StorageInfo info = FileManager.Instance.GetStorageInfo();
            LogDebug("TestGetStorageInfo:");
            LogDebug($" - Base Path: {info.basePath}");
            LogDebug($" - Initialized: {info.isInitialized}");
            LogDebug($" - Total Folders: {info.totalFolders}");
            LogDebug($" - Total Files: {info.totalFiles}");
            LogDebug($" - Available Space: {info.GetFormattedAvailableSpace()}");
            LogDebug($" - Total Space: {info.GetFormattedTotalSpace()}");
            LogDebug($" - Categories: {string.Join(", ", info.availableCategories)}");
        }

        private void OnFileCreated(StorageInfo.StorageCategory category, string fileName)
        {
            LogDebug($"Event: File created in {category}/{fileName}");
        }

        private void OnFileDeleted(StorageInfo.StorageCategory category, string fileName)
        {
            LogDebug($"Event: File deleted in {category}/{fileName}");
        }

        private void OnFileRead(StorageInfo.StorageCategory category, string fileName, bool success, string message)
        {
            LogDebug($"Event: File read in {category}/{fileName}: {(success ? "Success" : $"Failed - {message}")}");
        }

        private void OnFileWritten(StorageInfo.StorageCategory category, string fileName, bool success)
        {
            LogDebug($"Event: File written in {category}/{fileName}: {(success ? "Success" : "Failed")}");
        }

        private void OnCleanupComplete(bool success, string message, long freedBytes)
        {
            LogDebug($"Event: Cleanup completed: {(success ? $"Success, freed {freedBytes} bytes" : $"Failed - {message}")}");
        }

        private void OnStorageInitialized(bool success, string message)
        {
            LogDebug($"Event: Storage initialized: {(success ? "Success" : $"Failed - {message}")}");
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[FileTesting] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[FileTesting] {message}");
        }
    }
}