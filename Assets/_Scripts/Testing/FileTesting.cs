namespace _Scripts.Models.FileManagement
{
    using System.IO;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class FileTesting : FileManager
    {
        [SerializeField] private string testKey = "t"; // Tecla por defecto para la acción
        [SerializeField] private string testFileName = "test.txt";
        [SerializeField] private string testFolderName = "testFolder";
        [SerializeField] private string testContent = "Hello, World!";

        private InputAction action;

        private void OnEnable()
        {
            // Configurar la acción de entrada
            action = new InputAction("action", InputActionType.Button, $"<Keyboard>/{testKey}");
            action.performed += OnActionPerformed;
            action.Enable();
        }

        private void OnDisable()
        {
            action.performed -= OnActionPerformed;
            action.Disable();
        }

        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[FileTesting] Starting file operations test...");

            // Test 1: Listar archivos en persistentDataPath
            string persistentPath = Path.Combine(GetBasePath("persistent"), "test");
            ListFilesAsync(persistentPath, "*.txt", files =>
            {
                Debug.Log($"[FileTesting] Files in {persistentPath}: {string.Join(", ", files)}");
            });

            // Test 2: Crear y escribir un archivo en persistentDataPath
            CreateFileAsync(persistentPath, testFileName, testContent, success =>
            {
                Debug.Log($"[FileTesting] Create file {testFileName} in {persistentPath}: {(success ? "Success" : "Failed")}");
            });

            // Test 3: Leer el archivo creado
            ReadFileAsync(persistentPath, testFileName, content =>
            {
                Debug.Log($"[FileTesting] Read file {testFileName} from {persistentPath}: {(content != null ? content : "Failed")}");
            });

            // Test 4: Crear una carpeta en persistentDataPath
            CreateFolderAsync(persistentPath, testFolderName, success =>
            {
                Debug.Log($"[FileTesting] Create folder {testFolderName} in {persistentPath}: {(success ? "Success" : "Failed")}");
            });

            // Test 5: Listar archivos en streamingAssetsPath
            string streamingPath = Path.Combine(GetBasePath("streaming"), "resources");
            ListFilesAsync(streamingPath, "*.txt", files =>
            {
                Debug.Log($"[FileTesting] Files in {streamingPath}: {string.Join(", ", files)}");
            });

            // Test 6: Leer un archivo desde streamingAssetsPath
            ReadFileAsync(streamingPath, testFileName, content =>
            {
                Debug.Log($"[FileTesting] Read file {testFileName} from {streamingPath}: {(content != null ? content : "Failed")}");
            });

            // Test 7: Eliminar el archivo creado
            DeleteFileAsync(persistentPath, testFileName, success =>
            {
                Debug.Log($"[FileTesting] Delete file {testFileName} in {persistentPath}: {(success ? "Success" : "Failed")}");
            });

            // Test 8: Limpiar archivos temporales
            CleanupTempAsync((success, freedBytes) =>
            {
                Debug.Log($"[FileTesting] Cleanup temp: {(success ? $"Success, freed {freedBytes} bytes" : "Failed")}");
            });

            // Test 9: Obtener información de almacenamiento
            StorageInfo info = GetStorageInfo(persistentPath);
            Debug.Log($"[FileTesting] Storage info for {persistentPath}: Available {info.GetFormattedAvailableSpace()}, Total {info.GetFormattedTotalSpace()}");
        }
    }
}