namespace _Scripts.Models.FileManagement
{
    using System;
    using System.Collections.Generic;

    public interface IFileOperations
    {
        void ListFilesAsync(string path, string filter, Action<List<string>> callback);
        void ReadFileAsync(string path, string fileName, Action<string> callback);
        void ReadFileBytesAsync(string path, string fileName, Action<byte[]> callback);
        void WriteFileAsync(string path, string fileName, string content, Action<bool> callback);
        void WriteFileAsync(string path, string fileName, byte[] content, Action<bool> callback);
        void CreateFileAsync(string path, string fileName, string content, Action<bool> callback);
        void CreateFileAsync(string path, string fileName, byte[] content, Action<bool> callback);
        void DeleteFileAsync(string path, string fileName, Action<bool> callback);
        void CreateFolderAsync(string path, string folderName, Action<bool> callback);
        void DeleteFolderAsync(string path, string folderName, Action<bool> callback);
    }
}