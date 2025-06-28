using System;
using UnityEngine;

namespace _Scripts.Models.CertificateManagement
{
    public class CertificateEventManager
    {
        private readonly bool enableDebugLogs = true;

        // Events
        public event Action<string, CertificateInfo> OnCertificateLoaded; // pfxPath, certificateInfo
        public event Action<string, bool> OnCertificateValidated; // pfxPath, success
        public event Action<string, string> OnValidationFailed; // pfxPath, errorMessage
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

        public void TriggerCertificateLoaded(string pfxPath, CertificateInfo certificateInfo)
        {
            try
            {
                OnCertificateLoaded?.Invoke(pfxPath, certificateInfo);
                LogDebug($"Triggered OnCertificateLoaded: {pfxPath}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnCertificateLoaded: {ex.Message}");
            }
        }

        public void TriggerCertificateValidated(string pfxPath, bool success)
        {
            try
            {
                OnCertificateValidated?.Invoke(pfxPath, success);
                LogDebug($"Triggered OnCertificateValidated: {pfxPath}, Success: {success}");
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnCertificateValidated: {ex.Message}");
            }
        }

        public void TriggerValidationFailed(string pfxPath, string errorMessage)
        {
            try
            {
                OnValidationFailed?.Invoke(pfxPath, errorMessage);
                LogDebug($"Triggered OnValidationFailed: {pfxPath}, Error: {errorMessage}");
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
            if (enableDebugLogs)
                Debug.Log($"[CertificateEventManager] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[CertificateEventManager] {message}");
        }
    }
}