using System;
using System.Collections.Generic;
using System.Linq;
using _Scripts.Models;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.Testing
{
    public class S3Test : MonoBehaviour
    {
        private InputAction uploadTextAction;
        private InputAction uploadImageAction;
        private InputAction downloadLastAction;
        private InputAction downloadSpecificAction;
        private InputAction listFilesAction;
        private InputAction deleteAction;

        void Start()
        {
            uploadTextAction = new InputAction("UploadText", InputActionType.Button, "<Keyboard>/u");
            uploadImageAction = new InputAction("UploadImage", InputActionType.Button, "<Keyboard>/i");
            downloadLastAction = new InputAction("DownloadLast", InputActionType.Button, "<Keyboard>/d");
            downloadSpecificAction = new InputAction("DownloadSpecific", InputActionType.Button, "<Keyboard>/s");
            listFilesAction = new InputAction("ListFiles", InputActionType.Button, "<Keyboard>/l");
            deleteAction = new InputAction("DeleteFile", InputActionType.Button, "<Keyboard>/x");
            
            uploadTextAction.performed += OnUploadTextPressed;
            uploadImageAction.performed += OnUploadImagePressed;
            downloadLastAction.performed += OnDownloadLastPressed;
            downloadSpecificAction.performed += OnDownloadSpecificPressed;
            listFilesAction.performed += OnListFilesPressed;
            deleteAction.performed += OnDeletePressed;
            
            uploadTextAction.Enable();
            uploadImageAction.Enable();
            downloadLastAction.Enable();
            downloadSpecificAction.Enable();
            listFilesAction.Enable();
            deleteAction.Enable();
            
            if (S3Manager.Instance != null)
            {
                S3Manager.Instance.OnUploadComplete += OnUploadResult;
                S3Manager.Instance.OnDownloadComplete += OnDownloadResult;
                S3Manager.Instance.OnFileListComplete += OnFileListResult;
                S3Manager.Instance.OnFileDeleteComplete += OnDeleteResult;
            }
            
            Debug.Log("🚀 S3Test iniciado:");
            Debug.Log("📝 Presiona 'U' para subir archivo de texto");
            Debug.Log("🖼️ Presiona 'I' para subir imagen de prueba");
            Debug.Log("📥 Presiona 'D' para descargar último archivo subido");
            Debug.Log("🎯 Presiona 'S' para descargar archivo específico (configurado en Inspector)");
            Debug.Log("📋 Presiona 'L' para listar archivos de tu carpeta");
            Debug.Log("🗑️ Presiona 'X' para eliminar archivo específico (configurado en Inspector)");
        }

        void OnDestroy()
        {
            if (uploadTextAction != null)
            {
                uploadTextAction.performed -= OnUploadTextPressed;
                uploadTextAction.Disable();
                uploadTextAction.Dispose();
            }
            
            if (uploadImageAction != null)
            {
                uploadImageAction.performed -= OnUploadImagePressed;
                uploadImageAction.Disable();
                uploadImageAction.Dispose();
            }
            
            if (downloadLastAction != null)
            {
                downloadLastAction.performed -= OnDownloadLastPressed;
                downloadLastAction.Disable();
                downloadLastAction.Dispose();
            }
            
            if (downloadSpecificAction != null)
            {
                downloadSpecificAction.performed -= OnDownloadSpecificPressed;
                downloadSpecificAction.Disable();
                downloadSpecificAction.Dispose();
            }
            
            if (listFilesAction != null)
            {
                listFilesAction.performed -= OnListFilesPressed;
                listFilesAction.Disable();
                listFilesAction.Dispose();
            }
            
            if (deleteAction != null)
            {
                deleteAction.performed -= OnDeletePressed;
                deleteAction.Disable();
                deleteAction.Dispose();
            }
        }

        private void OnUploadTextPressed(InputAction.CallbackContext context)
        {
            ExecuteTextUploadTest();
        }

        private void OnUploadImagePressed(InputAction.CallbackContext context)
        {
            ExecuteImageUploadTest();
        }

        private void OnDownloadLastPressed(InputAction.CallbackContext context)
        {
            ExecuteDownloadLastTest();
        }

        private void OnDownloadSpecificPressed(InputAction.CallbackContext context)
        {
            ExecuteDownloadSpecificTest();
        }

        private void OnListFilesPressed(InputAction.CallbackContext context)
        {
            ExecuteListFilesTest();
        }

        private void OnDeletePressed(InputAction.CallbackContext context)
        {
            ExecuteDeleteTest();
        }

        async void ExecuteDownloadLastTest()
        {
            if (S3Manager.Instance == null)
            {
                Debug.LogError("S3Manager no disponible.");
                return;
            }
            
            Debug.Log("📥 Iniciando descarga del último archivo subido...");
            bool success = await S3Manager.Instance.DownloadLastUploadedFileAsync();
            Debug.Log(success ? "✅ Download último completado" : "❌ Download último falló");
        }

        async void ExecuteDownloadSpecificTest()
        {
            if (S3Manager.Instance == null)
            {
                Debug.LogError("S3Manager no disponible.");
                return;
            }
            
            Debug.Log("🎯 Iniciando descarga de archivo específico...");
            bool success = await S3Manager.Instance.DownloadSpecificFileAsync();
            Debug.Log(success ? "✅ Download específico completado" : "❌ Download específico falló");
        }

        async void ExecuteListFilesTest()
        {
            if (S3Manager.Instance == null)
            {
                Debug.LogError("S3Manager no disponible.");
                return;
            }

            Debug.Log("📋 Iniciando listado de archivos...");
            var fileList = await S3Manager.Instance.ListUserFilesAsync();
            Debug.Log(fileList.Count > 0 ? "✅ Listado completado" : "❌ Listado falló o carpeta vacía");
        }

        async void ExecuteDeleteTest()
        {
            if (S3Manager.Instance == null)
            {
                Debug.LogError("S3Manager no disponible.");
                return;
            }

            Debug.Log("🗑️ Iniciando eliminación de archivo específico...");
            Debug.LogWarning("⚠️ ATENCIÓN: Esta acción eliminará permanentemente el archivo configurado!");
            bool success = await S3Manager.Instance.DeleteSpecificFileAsync();
            Debug.Log(success ? "✅ Eliminación completada" : "❌ Eliminación falló");
        }

        async void ExecuteTextUploadTest()
        {
            if (S3Manager.Instance == null)
            {
                Debug.LogError("S3Manager no disponible.");
                return;
            }
            
            Debug.Log("📝 Iniciando upload de archivo de texto...");
            
            string testContent = $@"¡Hola desde Unity!
Usuario: {CognitoManager.Instance?.CurrentUsername ?? "unknown"}
Rol: {CognitoManager.Instance?.GetUserRole() ?? "unknown"}
Timestamp: {System.DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
Test ID: {System.Guid.NewGuid()}

Este es un archivo de prueba subido desde Unity a AWS S3.
¡Funciona perfectamente! 🚀";

            string fileName = $"test-file-{System.DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
            bool success = await S3Manager.Instance.UploadTextFileAsync(fileName, testContent);
            Debug.Log(success ? "✅ Upload de texto completado" : "❌ Upload de texto falló");
        }

        async void ExecuteImageUploadTest()
        {
            if (S3Manager.Instance == null)
            {
                Debug.LogError("S3Manager no disponible.");
                return;
            }
            
            Debug.Log("🖼️ Iniciando upload de imagen de prueba...");
            bool success = await S3Manager.Instance.UploadConfiguredFileAsync();
            Debug.Log(success ? "✅ Upload de imagen completado" : "❌ Upload de imagen falló");
        }

        void OnUploadResult(bool success, string message, string fileKey)
        {
            if (success)
            {
                Debug.Log($"🎉 ¡Archivo subido exitosamente!");
                Debug.Log($"📍 Ubicación: {fileKey}");
                Debug.Log($"💬 Mensaje: {message}");
            }
            else
            {
                Debug.LogError($"❌ Error en upload: {message}");
            }
        }

        void OnDownloadResult(bool success, string message, byte[] fileData)
        {
            if (success)
            {
                Debug.Log($"📥 ¡Archivo descargado exitosamente!");
                Debug.Log($"📊 Tamaño: {fileData?.Length ?? 0} bytes");
                Debug.Log($"💬 Mensaje: {message}");
                
                // Mostrar info adicional si es una imagen
                if (fileData != null && fileData.Length > 0)
                {
                    // Detectar tipo de archivo por los primeros bytes
                    string fileType = DetectFileType(fileData);
                    Debug.Log($"🔍 Tipo detectado: {fileType}");
                }
            }
            else
            {
                Debug.LogError($"❌ Error en download: {message}");
            }
        }

        void OnFileListResult(bool success, string message, List<S3FileInfo> fileList)
        {
            if (success)
            {
                Debug.Log($"📋 ¡Listado completado exitosamente!");
                Debug.Log($"📊 Total de archivos: {fileList?.Count ?? 0}");
                Debug.Log($"💬 Mensaje: {message}");

                if (fileList != null && fileList.Count > 0)
                {
                    Debug.Log("📁 Vista resumida de archivos:");
                    for (int i = 0; i < Math.Min(fileList.Count, 5); i++) // Solo mostrar los primeros 5
                    {
                        var file = fileList[i];
                        Debug.Log($"   {i + 1}. {file.fileName} ({file.formattedSize})");
                    }

                    if (fileList.Count > 5)
                    {
                        Debug.Log($"   ... y {fileList.Count - 5} archivos más");
                    }
                }
            }
            else
            {
                Debug.LogError($"❌ Error en listado: {message}");
            }
        }

        void OnDeleteResult(bool success, string message, string deletedFileKey)
        {
            if (success)
            {
                Debug.Log($"🗑️ ¡Archivo eliminado exitosamente!");
                Debug.Log($"🗂️ Archivo eliminado: {deletedFileKey}");
                Debug.Log($"💬 Mensaje: {message}");
                Debug.Log("✨ El archivo ya no existe en S3");
            }
            else
            {
                Debug.LogError($"❌ Error en eliminación: {message}");
            }
        }

        // Helper method to detect file type from byte signature
        private string DetectFileType(byte[] data)
        {
            if (data.Length < 4) return "Unknown";
            
            // Check for common file signatures
            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                return "JPEG Image";
            else if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return "PNG Image";
            else if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
                return "GIF Image";
            else if (System.Text.Encoding.UTF8.GetString(data, 0, Math.Min(10, data.Length)).All(c => c < 128))
                return "Text File";
            else
                return "Binary File";
        }
    }
}