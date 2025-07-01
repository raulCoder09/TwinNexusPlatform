using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Models.IoTCoreManagement
{
    public class IoTEventManager
    {
        private readonly bool enableDebugLogs = true;

        // Eventos existentes
        public event Action<bool, string, List<IoTInfo.ThingInfo>> OnThingsListed;
        public event Action<bool, string, string> OnThingCreated;
        public event Action<bool, string, IoTInfo.ThingInfo> OnThingRetrieved;
        public event Action<bool, string, IoTInfo.CertificateData> OnCertificateCreated;
        public event Action<bool, string, string, string> OnCertificateAttached;
        public event Action<bool, string, string, List<string>> OnThingCertificatesListed;
        public event Action<bool, string, string> OnPolicyCreated;
        public event Action<bool, string, string, string> OnPolicyAttached;

        // Nuevo evento para el estado general del servicio
        public event Action<bool, string> OnServiceStateChanged;

        public void SubscribeToThingsListed(Action<bool, string, List<IoTInfo.ThingInfo>> callback)
        {
            OnThingsListed += callback;
            LogDebug("Subscribed to OnThingsListed");
        }

        public void UnsubscribeFromThingsListed(Action<bool, string, List<IoTInfo.ThingInfo>> callback)
        {
            OnThingsListed -= callback;
            LogDebug("Unsubscribed from OnThingsListed");
        }

        public void SubscribeToThingCreated(Action<bool, string, string> callback)
        {
            OnThingCreated += callback;
            LogDebug("Subscribed to OnThingCreated");
        }

        public void UnsubscribeFromThingCreated(Action<bool, string, string> callback)
        {
            OnThingCreated -= callback;
            LogDebug("Unsubscribed from OnThingCreated");
        }

        public void SubscribeToThingRetrieved(Action<bool, string, IoTInfo.ThingInfo> callback)
        {
            OnThingRetrieved += callback;
            LogDebug("Subscribed to OnThingRetrieved");
        }

        public void UnsubscribeFromThingRetrieved(Action<bool, string, IoTInfo.ThingInfo> callback)
        {
            OnThingRetrieved -= callback;
            LogDebug("Unsubscribed from OnThingRetrieved");
        }

        public void SubscribeToCertificateCreated(Action<bool, string, IoTInfo.CertificateData> callback)
        {
            OnCertificateCreated += callback;
            LogDebug("Subscribed to OnCertificateCreated");
        }

        public void UnsubscribeFromCertificateCreated(Action<bool, string, IoTInfo.CertificateData> callback)
        {
            OnCertificateCreated -= callback;
            LogDebug("Unsubscribed from OnCertificateCreated");
        }

        public void SubscribeToCertificateAttached(Action<bool, string, string, string> callback)
        {
            OnCertificateAttached += callback;
            LogDebug("Subscribed to OnCertificateAttached");
        }

        public void UnsubscribeFromCertificateAttached(Action<bool, string, string, string> callback)
        {
            OnCertificateAttached -= callback;
            LogDebug("Unsubscribed from OnCertificateAttached");
        }

        public void SubscribeToThingCertificatesListed(Action<bool, string, string, List<string>> callback)
        {
            OnThingCertificatesListed += callback;
            LogDebug("Subscribed to OnThingCertificatesListed");
        }

        public void UnsubscribeFromThingCertificatesListed(Action<bool, string, string, List<string>> callback)
        {
            OnThingCertificatesListed -= callback;
            LogDebug("Unsubscribed from OnThingCertificatesListed");
        }

        public void SubscribeToPolicyCreated(Action<bool, string, string> callback)
        {
            OnPolicyCreated += callback;
            LogDebug("Subscribed to OnPolicyCreated");
        }

        public void UnsubscribeFromPolicyCreated(Action<bool, string, string> callback)
        {
            OnPolicyCreated -= callback;
            LogDebug("Unsubscribed from OnPolicyCreated");
        }

        public void SubscribeToPolicyAttached(Action<bool, string, string, string> callback)
        {
            OnPolicyAttached += callback;
            LogDebug("Subscribed to OnPolicyAttached");
        }

        public void UnsubscribeFromPolicyAttached(Action<bool, string, string, string> callback)
        {
            OnPolicyAttached -= callback;
            LogDebug("Unsubscribed from OnPolicyAttached");
        }

        public void TriggerThingsListed(bool success, string message, List<IoTInfo.ThingInfo> things)
        {
            try
            {
                OnThingsListed?.Invoke(success, message, things);
                LogDebug($"Triggered OnThingsListed: Success={success}, Message={message}");
                OnServiceStateChanged?.Invoke(success, message);
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnThingsListed: {ex.Message}");
                OnServiceStateChanged?.Invoke(false, ex.Message);
            }
        }

        public void TriggerThingCreated(bool success, string message, string thingName)
        {
            try
            {
                OnThingCreated?.Invoke(success, message, thingName);
                LogDebug($"Triggered OnThingCreated: Success={success}, Thing={thingName}");
                OnServiceStateChanged?.Invoke(success, message);
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnThingCreated: {ex.Message}");
                OnServiceStateChanged?.Invoke(false, ex.Message);
            }
        }

        public void TriggerThingRetrieved(bool success, string message, IoTInfo.ThingInfo thingInfo)
        {
            try
            {
                OnThingRetrieved?.Invoke(success, message, thingInfo);
                LogDebug($"Triggered OnThingRetrieved: Success={success}, Thing={thingInfo?.ThingName}");
                OnServiceStateChanged?.Invoke(success, message);
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnThingRetrieved: {ex.Message}");
                OnServiceStateChanged?.Invoke(false, ex.Message);
            }
        }

        public void TriggerCertificateCreated(bool success, string message, IoTInfo.CertificateData certificateData)
        {
            try
            {
                OnCertificateCreated?.Invoke(success, message, certificateData);
                LogDebug($"Triggered OnCertificateCreated: Success={success}, Certificate={certificateData?.CertificateId?.Substring(0, Math.Min(8, certificateData?.CertificateId?.Length ?? 0))}...");
                OnServiceStateChanged?.Invoke(success, message);
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnCertificateCreated: {ex.Message}");
                OnServiceStateChanged?.Invoke(false, ex.Message);
            }
        }

        public void TriggerCertificateAttached(bool success, string message, string thingName, string certificateId)
        {
            try
            {
                OnCertificateAttached?.Invoke(success, message, thingName, certificateId);
                LogDebug($"Triggered OnCertificateAttached: Success={success}, Thing={thingName}, Certificate={certificateId?.Substring(0, Math.Min(8, certificateId?.Length ?? 0))}...");
                OnServiceStateChanged?.Invoke(success, message);
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnCertificateAttached: {ex.Message}");
                OnServiceStateChanged?.Invoke(false, ex.Message);
            }
        }

        public void TriggerThingCertificatesListed(bool success, string message, string thingName, List<string> certificateArns)
        {
            try
            {
                OnThingCertificatesListed?.Invoke(success, message, thingName, certificateArns);
                LogDebug($"Triggered OnThingCertificatesListed: Success={success}, Thing={thingName}, Certificates={certificateArns?.Count}");
                OnServiceStateChanged?.Invoke(success, message);
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnThingCertificatesListed: {ex.Message}");
                OnServiceStateChanged?.Invoke(false, ex.Message);
            }
        }

        public void TriggerPolicyCreated(bool success, string message, string policyName)
        {
            try
            {
                OnPolicyCreated?.Invoke(success, message, policyName);
                LogDebug($"Triggered OnPolicyCreated: Success={success}, Policy={policyName}");
                OnServiceStateChanged?.Invoke(success, message);
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnPolicyCreated: {ex.Message}");
                OnServiceStateChanged?.Invoke(false, ex.Message);
            }
        }

        public void TriggerPolicyAttached(bool success, string message, string policyName, string certificateId)
        {
            try
            {
                OnPolicyAttached?.Invoke(success, message, policyName, certificateId);
                LogDebug($"Triggered OnPolicyAttached: Success={success}, Policy={policyName}, Certificate={certificateId?.Substring(0, Math.Min(8, certificateId?.Length ?? 0))}...");
                OnServiceStateChanged?.Invoke(success, message);
            }
            catch (Exception ex)
            {
                LogError($"Error triggering OnPolicyAttached: {ex.Message}");
                OnServiceStateChanged?.Invoke(false, ex.Message);
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[IoTEventManager] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[IoTEventManager] {message}");
        }
    }
}