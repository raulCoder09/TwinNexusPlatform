using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.IoT;
using Amazon.IoT.Model;
using UnityEngine;

namespace _Scripts.Models.IoTCoreManagement
{
    public class IoTCertificateHandler
    {
        private readonly IoTClientHandler clientHandler;
        private readonly IoTEventManager eventManager;
        private readonly bool enableDebugLogs = true;
        [SerializeField] private bool setAsActive = true;
        [SerializeField] private string certificateIdToAttach = "";
        [SerializeField] private string thingNameForCertificate = "";

        public IoTCertificateHandler(IoTClientHandler clientHandler, IoTEventManager eventManager)
        {
            this.clientHandler = clientHandler;
            this.eventManager = eventManager;
        }

        public async Task<IoTInfo.CertificateData> CreateThingCertificateAsync()
        {
            try
            {
                if (!CheckAuthentication()) return null;

                AmazonIoTClient client = clientHandler.GetClient();
                if (client == null)
                {
                    eventManager.TriggerCertificateCreated(false, "IoT client not initialized", null);
                    return null;
                }

                LogDebug("Creating IoT Thing certificate...");
                LogWarning("IMPORTANT: Private key will only be shown ONCE!");

                var createCertRequest = new CreateKeysAndCertificateRequest
                {
                    SetAsActive = setAsActive
                };

                var response = await client.CreateKeysAndCertificateAsync(createCertRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    var certificateData = new IoTInfo.CertificateData
                    {
                        CertificateId = response.CertificateId,
                        CertificateArn = response.CertificateArn,
                        CertificatePem = response.CertificatePem,
                        PublicKey = response.KeyPair.PublicKey,
                        PrivateKey = response.KeyPair.PrivateKey,
                        IsActive = setAsActive,
                        CreationDate = DateTime.UtcNow
                    };

                    LogDebug($"Certificate created successfully: {certificateData.GetSafeInfo()}");
                    LogWarning("SECURITY: Store the private key securely! It won't be available again.");
                    eventManager.TriggerCertificateCreated(true, "Certificate created successfully", certificateData);
                    return certificateData;
                }

                LogError($"Failed to create certificate: {response.HttpStatusCode}");
                eventManager.TriggerCertificateCreated(false, $"Creation failed: {response.HttpStatusCode}", null);
                return null;
            }
            catch (Exception ex)
            {
                LogError($"IoT Create Certificate error: {ex.Message}");
                eventManager.TriggerCertificateCreated(false, ex.Message, null);
                return null;
            }
        }

        public async Task<bool> AttachCertificateToThingAsync(string certificateId = null, string thingName = null)
        {
            try
            {
                string finalCertificateId = certificateId ?? certificateIdToAttach;
                string finalThingName = thingName ?? thingNameForCertificate;

                if (string.IsNullOrEmpty(finalCertificateId) || string.IsNullOrEmpty(finalThingName))
                {
                    LogError("Certificate ID and Thing name are required");
                    eventManager.TriggerCertificateAttached(false, "Certificate ID and Thing name required", finalThingName, finalCertificateId);
                    return false;
                }

                if (!CheckAuthentication()) return false;

                AmazonIoTClient client = clientHandler.GetClient();
                if (client == null)
                {
                    eventManager.TriggerCertificateAttached(false, "IoT client not initialized", finalThingName, finalCertificateId);
                    return false;
                }

                LogDebug($"Attaching certificate {finalCertificateId.Substring(0, Math.Min(8, finalCertificateId.Length))}... to Thing: {finalThingName}");
                string certificateArn = $"arn:aws:iot:us-east-1:156041417101:cert/{finalCertificateId}";

                var attachRequest = new AttachThingPrincipalRequest
                {
                    ThingName = finalThingName,
                    Principal = certificateArn
                };

                var response = await client.AttachThingPrincipalAsync(attachRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    LogDebug($"Certificate attached successfully: {finalCertificateId.Substring(0, Math.Min(8, finalCertificateId.Length))}... to {finalThingName}");
                    eventManager.TriggerCertificateAttached(true, "Certificate attached successfully", finalThingName, finalCertificateId);
                    return true;
                }

                LogError($"Failed to attach certificate: {response.HttpStatusCode}");
                eventManager.TriggerCertificateAttached(false, $"Attach failed: {response.HttpStatusCode}", finalThingName, finalCertificateId);
                return false;
            }
            catch (Exception ex)
            {
                LogError($"IoT Attach Certificate error: {ex.Message}");
                eventManager.TriggerCertificateAttached(false, ex.Message, thingName ?? thingNameForCertificate, certificateId ?? certificateIdToAttach);
                return false;
            }
        }

        public async Task<List<string>> ListThingCertificatesAsync(string thingName = null)
        {
            try
            {
                string finalThingName = thingName ?? thingNameForCertificate;

                if (string.IsNullOrEmpty(finalThingName))
                {
                    LogError("Thing name is required to list certificates");
                    eventManager.TriggerThingCertificatesListed(false, "Thing name is required", finalThingName, new List<string>());
                    return new List<string>();
                }

                if (!CheckAuthentication()) return new List<string>();

                AmazonIoTClient client = clientHandler.GetClient();
                if (client == null)
                {
                    eventManager.TriggerThingCertificatesListed(false, "IoT client not initialized", finalThingName, new List<string>());
                    return new List<string>();
                }

                LogDebug($"Listing certificates for Thing: {finalThingName}");
                var listPrincipalsRequest = new ListThingPrincipalsRequest { ThingName = finalThingName };
                var response = await client.ListThingPrincipalsAsync(listPrincipalsRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    var certificateArns = response.Principals;

                    LogDebug($"Found {certificateArns.Count} certificate(s) for {finalThingName}");
                    if (certificateArns.Count > 0)
                    {
                        for (int i = 0; i < certificateArns.Count; i++)
                        {
                            string arn = certificateArns[i];
                            string certId = arn.Split('/').Length > 1 ? arn.Split('/')[1] : "Unknown";
                            LogDebug($"Certificate {i + 1}: {certId.Substring(0, Math.Min(8, certId.Length))}..., ARN: {arn}");
                        }
                    }
                    else
                    {
                        LogDebug($"No certificates found for {finalThingName}");
                    }

                    eventManager.TriggerThingCertificatesListed(true, $"Found {certificateArns.Count} certificates", finalThingName, certificateArns);
                    return certificateArns;
                }

                LogError($"Failed to list certificates: {response.HttpStatusCode}");
                eventManager.TriggerThingCertificatesListed(false, $"List failed: {response.HttpStatusCode}", finalThingName, new List<string>());
                return new List<string>();
            }
            catch (Exception ex)
            {
                LogError($"IoT List Thing Certificates error: {ex.Message}");
                eventManager.TriggerThingCertificatesListed(false, ex.Message, thingName ?? thingNameForCertificate, new List<string>());
                return new List<string>();
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
                Debug.Log($"[IoTCertificateHandler] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[IoTCertificateHandler] {message}");
        }

        private void LogWarning(string message)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[IoTCertificateHandler] {message}");
        }
    }
}