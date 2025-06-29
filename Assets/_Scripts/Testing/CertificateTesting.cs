namespace _Scripts.Models.CertificateManagement
{
    using System.IO;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using _Scripts.Models.FileManagement;

    public class CertificateTesting : CertificateManager
    {
        [Header("Input Configuration")]
        [SerializeField] private string testKeyLoadPersistent = "c"; // Tecla para cargar desde persistentDataPath
        [SerializeField] private string testKeyValidatePersistent = "v"; // Tecla para validar en persistentDataPath
        [SerializeField] private string testKeyGetInfo = "b"; // Tecla para obtener información
        [SerializeField] private string testKeyLoadStreaming = "n"; // Tecla para cargar desde StreamingAssets
        [SerializeField] private string testKeyValidateStreaming = "m"; // Tecla para validar en StreamingAssets
        [SerializeField] private string testKeyEvents = "e"; // Tecla para probar eventos

        [Header("Test Configuration")]
        [SerializeField] private string testFileName = "aws-iot-core.pfx";
        [SerializeField] private string testPassword = "5859"; // Contraseña del .pfx
        [SerializeField] private string testPath = "certificates"; // Subcarpeta en persistentDataPath o StreamingAssets

        private InputAction actionLoadPersistent;
        private InputAction actionValidatePersistent;
        private InputAction actionGetInfo;
        private InputAction actionLoadStreaming;
        private InputAction actionValidateStreaming;
        private InputAction actionEvents;

        private void OnEnable()
        {
            // Configurar acciones de entrada
            actionLoadPersistent = new InputAction("loadPersistent", InputActionType.Button, $"<Keyboard>/{testKeyLoadPersistent}");
            actionLoadPersistent.performed += OnLoadPersistentPerformed;
            actionLoadPersistent.Enable();

            actionValidatePersistent = new InputAction("validatePersistent", InputActionType.Button, $"<Keyboard>/{testKeyValidatePersistent}");
            actionValidatePersistent.performed += OnValidatePersistentPerformed;
            actionValidatePersistent.Enable();

            actionGetInfo = new InputAction("getInfo", InputActionType.Button, $"<Keyboard>/{testKeyGetInfo}");
            actionGetInfo.performed += OnGetInfoPerformed;
            actionGetInfo.Enable();

            actionLoadStreaming = new InputAction("loadStreaming", InputActionType.Button, $"<Keyboard>/{testKeyLoadStreaming}");
            actionLoadStreaming.performed += OnLoadStreamingPerformed;
            actionLoadStreaming.Enable();

            actionValidateStreaming = new InputAction("validateStreaming", InputActionType.Button, $"<Keyboard>/{testKeyValidateStreaming}");
            actionValidateStreaming.performed += OnValidateStreamingPerformed;
            actionValidateStreaming.Enable();

            actionEvents = new InputAction("events", InputActionType.Button, $"<Keyboard>/{testKeyEvents}");
            actionEvents.performed += OnEventsPerformed;
            actionEvents.Enable();
        }

        private void OnDisable()
        {
            // Desactivar acciones
            actionLoadPersistent.performed -= OnLoadPersistentPerformed;
            actionLoadPersistent.Disable();

            actionValidatePersistent.performed -= OnValidatePersistentPerformed;
            actionValidatePersistent.Disable();

            actionGetInfo.performed -= OnGetInfoPerformed;
            actionGetInfo.Disable();

            actionLoadStreaming.performed -= OnLoadStreamingPerformed;
            actionLoadStreaming.Disable();

            actionValidateStreaming.performed -= OnValidateStreamingPerformed;
            actionValidateStreaming.Disable();

            actionEvents.performed -= OnEventsPerformed;
            actionEvents.Disable();
        }

        private async void OnLoadPersistentPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[CertificateTesting] Testing load certificate from persistentDataPath...");
            string persistentPath = Path.Combine(FileManager.Instance.GetBasePath("persistent"), testPath);
            var certificate = await LoadX509CertificateAsync(persistentPath, testFileName, testPassword);
            Debug.Log($"[CertificateTesting] Load certificate {testFileName} from {persistentPath}: {(certificate != null ? $"Success: {certificate.Subject}" : "Failed")}");
        }

        private async void OnValidatePersistentPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[CertificateTesting] Testing validate certificate in persistentDataPath...");
            string persistentPath = Path.Combine(FileManager.Instance.GetBasePath("persistent"), testPath);
            var isValid = await ValidateCertificateAsync(persistentPath, testFileName);
            Debug.Log($"[CertificateTesting] Validate certificate {testFileName} in {persistentPath}: {(isValid ? "Success" : "Failed")}");
        }

        private async void OnGetInfoPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[CertificateTesting] Testing get certificate info...");
            string persistentPath = Path.Combine(FileManager.Instance.GetBasePath("persistent"), testPath);
            var certificate = await LoadX509CertificateAsync(persistentPath, testFileName, testPassword);
            if (certificate != null)
            {
                string certInfo = GetCertificateInfo(certificate);
                Debug.Log($"[CertificateTesting] Certificate info for {testFileName}: {certInfo}");
            }
            else
            {
                Debug.Log($"[CertificateTesting] Failed to load certificate {testFileName} for info");
            }
        }

        private async void OnLoadStreamingPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[CertificateTesting] Testing load certificate from StreamingAssets...");
            string streamingPath = Path.Combine(FileManager.Instance.GetBasePath("streaming"), testPath);
            var certificate = await LoadX509CertificateAsync(streamingPath, testFileName, testPassword);
            Debug.Log($"[CertificateTesting] Load certificate {testFileName} from {streamingPath}: {(certificate != null ? $"Success: {certificate.Subject}" : "Failed")}");
        }

        private async void OnValidateStreamingPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[CertificateTesting] Testing validate certificate in StreamingAssets...");
            string streamingPath = Path.Combine(FileManager.Instance.GetBasePath("streaming"), testPath);
            var isValid = await ValidateCertificateAsync(streamingPath, testFileName);
            Debug.Log($"[CertificateTesting] Validate certificate {testFileName} in {streamingPath}: {(isValid ? "Success" : "Failed")}");
        }

        private async void OnEventsPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[CertificateTesting] Testing certificate events...");
            string persistentPath = Path.Combine(FileManager.Instance.GetBasePath("persistent"), testPath);

            SubscribeToCertificateLoaded((path, info) =>
            {
                Debug.Log($"[CertificateTesting] Event: Certificate loaded from {path}: {info?.GetCertificateInfo() ?? "Null"}");
            });
            SubscribeToCertificateValidated((path, success) =>
            {
                Debug.Log($"[CertificateTesting] Event: Certificate validation in {path}: {(success ? "Success" : "Failed")}");
            });
            SubscribeToValidationFailed((path, error) =>
            {
                Debug.Log($"[CertificateTesting] Event: Validation failed in {path}: {error}");
            });

            await LoadX509CertificateAsync(persistentPath, testFileName, testPassword);
        }
    }
}