namespace _Scripts.Models.FileManagement
{
    public interface IStorageProvider
    {
        string GetBasePath(string context);
        bool IsPathAccessible(string path);
        string NormalizePath(string path);
        bool RequestPermissions();
    }
}