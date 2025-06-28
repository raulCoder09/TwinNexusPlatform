using System;
using System.Threading.Tasks;
using Amazon.IoT;
using Amazon.IoT.Model;
using UnityEngine;

namespace _Scripts.Models.IoTCoreManagement
{
    public class IoTPolicyHandler
    {
        private readonly IoTClientHandler clientHandler;
        private readonly IoTEventManager eventManager;
        private readonly IoTInfo ioTInfo;
        private readonly bool enableDebugLogs = true;
        [SerializeField] private string newPolicyName = "Unity-IoT-Policy";
        [SerializeField] private string policyDocument = "";
        [SerializeField] private string policyNameToAttach = "";
        [SerializeField] private string certificateIdForPolicy = "";

        public IoTPolicyHandler(IoTClientHandler clientHandler, IoTEventManager eventManager, IoTInfo ioTInfo)
        {
            this.clientHandler = clientHandler;
            this.eventManager = eventManager;
            this.ioTInfo = ioTInfo;
        }

        public async Task<bool> CreatePolicyAsync(string policyName = null, string customPolicyDocument = null)
        {
            try
            {
                if (!CheckAuthentication()) return false;

                AmazonIoTClient client = clientHandler.GetClient();
                if (client == null)
                {
                    eventManager.TriggerPolicyCreated(false, "IoT client not initialized", policyName ?? newPolicyName);
                    return false;
                }

                string finalPolicyName = policyName ?? IoTInfo.GeneratePolicyName(newPolicyName, ioTInfo.UseTimestampInNames);
                string policyDoc = customPolicyDocument ?? policyDocument ?? ioTInfo.GetDefaultPolicyDocument();

                LogDebug($"Creating IoT Policy: {finalPolicyName}");
                var createPolicyRequest = new CreatePolicyRequest
                {
                    PolicyName = finalPolicyName,
                    PolicyDocument = policyDoc
                };

                var response = await client.CreatePolicyAsync(createPolicyRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    LogDebug($"Policy created successfully: {finalPolicyName}, ARN: {response.PolicyArn}");
                    eventManager.TriggerPolicyCreated(true, "Policy created successfully", finalPolicyName);
                    return true;
                }

                LogError($"Failed to create policy: {response.HttpStatusCode}");
                eventManager.TriggerPolicyCreated(false, $"Creation failed: {response.HttpStatusCode}", finalPolicyName);
                return false;
            }
            catch (Exception ex)
            {
                LogError($"IoT Create Policy error: {ex.Message}");
                eventManager.TriggerPolicyCreated(false, ex.Message, policyName ?? newPolicyName);
                return false;
            }
        }

        public async Task<bool> AttachPolicyAsync(string policyName = null, string certificateId = null)
        {
            try
            {
                string finalPolicyName = policyName ?? policyNameToAttach;
                string finalCertificateId = certificateId ?? certificateIdForPolicy;

                if (string.IsNullOrEmpty(finalPolicyName) || string.IsNullOrEmpty(finalCertificateId))
                {
                    LogError("Policy name and Certificate ID are required");
                    eventManager.TriggerPolicyAttached(false, "Policy name and Certificate ID required", finalPolicyName, finalCertificateId);
                    return false;
                }

                if (!CheckAuthentication()) return false;

                AmazonIoTClient client = clientHandler.GetClient();
                if (client == null)
                {
                    eventManager.TriggerPolicyAttached(false, "IoT client not initialized", finalPolicyName, finalCertificateId);
                    return false;
                }

                LogDebug($"Attaching policy {finalPolicyName} to certificate {finalCertificateId.Substring(0, Math.Min(8, finalCertificateId.Length))}...");
                var attachRequest = new AttachPolicyRequest
                {
                    PolicyName = finalPolicyName,
                    Target = $"arn:aws:iot:us-east-1:156041417101:cert/{finalCertificateId}"
                };

                var response = await client.AttachPolicyAsync(attachRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    LogDebug($"Policy attached successfully: {finalPolicyName} to {finalCertificateId.Substring(0, Math.Min(8, finalCertificateId.Length))}...");
                    eventManager.TriggerPolicyAttached(true, "Policy attached successfully", finalPolicyName, finalCertificateId);
                    return true;
                }

                LogError($"Failed to attach policy: {response.HttpStatusCode}");
                eventManager.TriggerPolicyAttached(false, $"Attach failed: {response.HttpStatusCode}", finalPolicyName, finalCertificateId);
                return false;
            }
            catch (Exception ex)
            {
                LogError($"IoT Attach Policy error: {ex.Message}");
                eventManager.TriggerPolicyAttached(false, ex.Message, policyName ?? policyNameToAttach, certificateId ?? certificateIdForPolicy);
                return false;
            }
        }

        private bool CheckAuthentication()
        {
            if (CognitoManager.Instance == null || !CognitoManager.Instance.IsUserAuthenticated)
            {
                LogError("User must be authenticated to perform IoT operations");
                return false;
            }
            return true;
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[IoTPolicyHandler] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[IoTPolicyHandler] {message}");
        }
    }
}