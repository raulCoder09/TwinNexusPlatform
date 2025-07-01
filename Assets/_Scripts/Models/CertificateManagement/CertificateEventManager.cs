namespace _Scripts.Models.CertificateManagement
{
    using System;
    using UnityEngine;

    public class CertificateEventManager
    {
        private readonly bool _enableDebugLogs = true;

        public event Action<string, CertificateInfo> OnCertificateLoaded; // path, certificateInfo
        public event Action<string, bool> OnCertificateValidated; // path, success
        public event Action<string, string> OnValidationFailed; // path, errorMessage
        public event Action<bool, string> OnServerCertificateValidated; // success, message
        public event Action<bool, string> OnStorageInitialized; // success, message

        public void SubscribeToCertificateLoaded(Action<string, CertificateInfo> callback)
        {
            OnCertificateLoaded += callback;
            LogDebug("Subscribed to OnCertificateLoaded");
        }

        public void UnsubscribeFromCertificateLoaded(Action<string, CertificateInfo> callback)
        {
            OnCertificateLoaded -= callback;
            LogDebug("Unsubscribed from OnCertificateLoaded");
        }

        public void SubscribeToCertificateValidated(Action<string, bool> callback)
        {
            OnCertificateValidated += callback;
            LogDebug("Subscribed to OnCertificateValidated");
        }

        public void UnsubscribeFromCertificateValidated(Action<string, bool> callback)
        {
            OnCertificateValidated -= callback;
            LogDebug("Unsubscribed from OnCertificateValidated");
        }

        public void SubscribeToValidationFailed(Action<string, string> callback)
        {
            OnValidationFailed += callback;
            LogDebug("Subscribed to OnValidationFailed");
        }

        public void UnsubscribeFromValidationFailed(Action<string, string> callback)
        {
            OnValidationFailed -= callback;
            LogDebug("Unsubscribed from OnValidationFailed");
        }

        public void SubscribeToServerCertificateValidated(Action<bool, string> callback)
        {
            OnServerCertificateValidated += callback;
            LogDebug("Subscribed to OnServerCertificateValidated");
        }

        public void UnsubscribeFromServerCertificateValidated(Action<bool, string> callback)
        {
            OnServerCertificateValidated -= callback;
            LogDebug("Unsubscribed from OnServerCertificateValidated");
        }

        public void SubscribeToStorageInitialized(Action<bool, string> callback)
        {
            OnStorageInitialized += callback;
            LogDebug("Subscribed to OnStorageInitialized");
        }

        public void UnsubscribeFromStorageInitialized(Action<bool, string> callback)
        {
            OnStorageInitialized -= callback;
            LogDebug("Unsubscribed from OnStorageInitialized");
        }

        public void TriggerCertificateLoaded(string path, CertificateInfo certificateInfo)
        {
            try
            {
                OnCertificateLoaded?.Invoke(path, certificateInfo);
                LogDebug($"Triggered OnCertificateLoaded: {path}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnCertificateLoaded: {ex.Message}");
            }
        }

        public void TriggerCertificateValidated(string path, bool success)
        {
            try
            {
                OnCertificateValidated?.Invoke(path, success);
                LogDebug($"Triggered OnCertificateValidated: {path}, Success: {success}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnCertificateValidated: {ex.Message}");
            }
        }

        public void TriggerValidationFailed(string path, string errorMessage)
        {
            try
            {
                OnValidationFailed?.Invoke(path, errorMessage);
                LogDebug($"Triggered OnValidationFailed: {path}, Error: {errorMessage}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnValidationFailed: {ex.Message}");
            }
        }

        public void TriggerServerCertificateValidated(bool success, string message)
        {
            try
            {
                OnServerCertificateValidated?.Invoke(success, message);
                LogDebug($"Triggered OnServerCertificateValidated: Success: {success}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnServerCertificateValidated: {ex.Message}");
            }
        }

        public void TriggerStorageInitialized(bool success, string message)
        {
            try
            {
                OnStorageInitialized?.Invoke(success, message);
                LogDebug($"Triggered OnStorageInitialized: Success: {success}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnStorageInitialized: {ex.Message}");
            }
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[CertificateEventManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[CertificateEventManager] {message}");
        }
    }
}