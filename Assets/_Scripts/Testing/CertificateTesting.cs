using System;
using System.Security.Cryptography.X509Certificates;
using _Scripts.Models.CertificateManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using _Scripts.Models.FileManagement;

namespace _Scripts.Testing
{
    public class CertificateTesting : MonoBehaviour
    {
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private string testPfxFileName = "aws-iot-core.pfx";
        [SerializeField] private string testPassword = "";

        private InputAction validateAction;
        private InputAction loadAction;
        private InputAction infoAction;
        private InputAction optionsAction;

        private void Awake()
        {
            CertificateManager.Instance.Initialize();
            if (!CertificateManager.Instance.IsInitialized())
            {
                LogError("Failed to initialize CertificateManager");
            }
        }

        private void OnEnable()
        {
            validateAction = new InputAction("ValidateCertificate", InputActionType.Button, "<Keyboard>/v");
            loadAction = new InputAction("LoadCertificate", InputActionType.Button, "<Keyboard>/l");
            infoAction = new InputAction("GetCertificateInfo", InputActionType.Button, "<Keyboard>/i");
            optionsAction = new InputAction("SetValidationOptions", InputActionType.Button, "<Keyboard>/o");

            validateAction.performed += _ => TestValidateCertificate();
            loadAction.performed += _ => TestLoadCertificate();
            infoAction.performed += _ => TestGetCertificateInfo();
            optionsAction.performed += _ => TestSetValidationOptions();

            validateAction.Enable();
            loadAction.Enable();
            infoAction.Enable();
            optionsAction.Enable();

            CertificateManager.Instance.SubscribeToCertificateLoaded(OnCertificateLoaded);
            CertificateManager.Instance.SubscribeToCertificateValidated(OnCertificateValidated);
            CertificateManager.Instance.SubscribeToValidationFailed(OnValidationFailed);
            CertificateManager.Instance.SubscribeToServerCertificateValidated(OnServerCertificateValidated);
            CertificateManager.Instance.SubscribeToStorageInitialized(OnStorageInitialized);
        }

        private void OnDisable()
        {
            CertificateManager.Instance.UnsubscribeFromCertificateLoaded(OnCertificateLoaded);
            CertificateManager.Instance.UnsubscribeFromCertificateValidated(OnCertificateValidated);
            CertificateManager.Instance.UnsubscribeFromValidationFailed(OnValidationFailed);
            CertificateManager.Instance.UnsubscribeFromServerCertificateValidated(OnServerCertificateValidated);
            CertificateManager.Instance.UnsubscribeFromStorageInitialized(OnStorageInitialized);

            validateAction.Disable();
            loadAction.Disable();
            infoAction.Disable();
            optionsAction.Disable();
        }

        private async void TestValidateCertificate()
        {
            bool success = await CertificateManager.Instance.ValidateCertificatesAsync(testPfxFileName, StorageInfo.StorageCategory.Resources);
            LogDebug($"TestValidateCertificate: {(success ? "Success" : "Failed")}");
        }

        private async void TestLoadCertificate()
        {
            X509Certificate2 certificate = await CertificateManager.Instance.LoadX509CertificateAsync(testPfxFileName, testPassword, StorageInfo.StorageCategory.Resources);
            LogDebug($"TestLoadCertificate: {(certificate != null ? "Success" : "Failed")}");
        }

        private async void TestGetCertificateInfo()
        {
            X509Certificate2 certificate = await CertificateManager.Instance.LoadX509CertificateAsync(testPfxFileName, testPassword, StorageInfo.StorageCategory.Resources);
            if (certificate != null)
            {
                string info = CertificateManager.Instance.GetCertificateInfo(certificate);
                LogDebug($"TestGetCertificateInfo:\n{info}");
            }
            else
            {
                LogDebug("TestGetCertificateInfo: Failed to load certificate");
            }
        }

        private void TestSetValidationOptions()
        {
            CertificateManager.Instance.SetValidationOptions(true, true, true);
            LogDebug("TestSetValidationOptions: Validation options set to allow all");
        }

        private void OnCertificateLoaded(string pfxPath, CertificateInfo certificateInfo)
        {
            LogDebug($"Event: Certificate loaded: {pfxPath}\n{certificateInfo.GetCertificateInfo()}");
        }

        private void OnCertificateValidated(string pfxPath, bool success)
        {
            LogDebug($"Event: Certificate validated: {pfxPath}, Success: {success}");
        }

        private void OnValidationFailed(string pfxPath, string errorMessage)
        {
            LogDebug($"Event: Validation failed: {pfxPath}, Error: {errorMessage}");
        }

        private void OnServerCertificateValidated(bool success, string message)
        {
            LogDebug($"Event: Server certificate validated: Success: {success}, Message: {message}");
        }

        private void OnStorageInitialized(bool success, string message)
        {
            LogDebug($"Event: Storage initialized: Success: {success}, Message: {message}");
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[CertificateTesting] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[CertificateTesting] {message}");
        }
    }
}