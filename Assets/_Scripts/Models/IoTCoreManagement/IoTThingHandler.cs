using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.IoT;
using Amazon.IoT.Model;
using UnityEngine;

namespace _Scripts.Models.IoTCoreManagement
{
    public class IoTThingHandler
    {
        private readonly IoTClientHandler clientHandler;
        private readonly IoTEventManager eventManager;
        private readonly IoTInfo ioTInfo;
        private readonly bool enableDebugLogs = true;
        [SerializeField] private string newThingName = "Unity-Test-Device";
        [SerializeField] private string newThingType = "";
        [SerializeField] private string thingNameToGet = "";

        public IoTThingHandler(IoTClientHandler clientHandler, IoTEventManager eventManager, IoTInfo ioTInfo)
        {
            this.clientHandler = clientHandler;
            this.eventManager = eventManager;
            this.ioTInfo = ioTInfo;
        }

        public async Task<List<IoTInfo.ThingInfo>> ListThingsAsync()
        {
            try
            {
                if (!CheckAuthentication()) return new List<IoTInfo.ThingInfo>();

                AmazonIoTClient client = clientHandler.GetClient();
                if (client == null)
                {
                    eventManager.TriggerThingsListed(false, "IoT client not initialized", new List<IoTInfo.ThingInfo>());
                    return new List<IoTInfo.ThingInfo>();
                }

                LogDebug("Listing IoT Things...");
                var listThingsRequest = new ListThingsRequest { MaxResults = 50 };
                var response = await client.ListThingsAsync(listThingsRequest);
                var thingInfoList = new List<IoTInfo.ThingInfo>();

                foreach (var thing in response.Things)
                {
                    var thingInfo = new IoTInfo.ThingInfo
                    {
                        ThingName = thing.ThingName,
                        ThingTypeName = thing.ThingTypeName,
                        ThingArn = thing.ThingArn,
                        Version = thing.Version ?? 0,
                        CreationDate = DateTime.UtcNow,
                        LastModifiedDate = DateTime.UtcNow
                    };

                    if (thing.Attributes != null && thing.Attributes.Count > 0)
                    {
                        thingInfo.AttributesInfo = string.Join("; ", thing.Attributes.Select(attr => $"{attr.Key}={attr.Value}"));
                        LogDebug($"Attributes found for {thing.ThingName}: {thingInfo.AttributesInfo}");
                    }

                    thingInfoList.Add(thingInfo);
                }

                LogDebug($"Found {thingInfoList.Count} IoT Things");
                if (thingInfoList.Count > 0)
                {
                    foreach (var thing in thingInfoList)
                    {
                        LogDebug($"Thing: {thing.ThingName}, Type: {thing.ThingTypeName ?? "None"}, ARN: {thing.ThingArn}, Attributes: {thing.AttributesInfo ?? "None"}");
                    }
                }
                else
                {
                    LogDebug("No IoT Things found in registry");
                }

                eventManager.TriggerThingsListed(true, $"Found {thingInfoList.Count} Things", thingInfoList);
                return thingInfoList;
            }
            catch (Exception ex)
            {
                LogError($"IoT List Things error: {ex.Message}");
                eventManager.TriggerThingsListed(false, ex.Message, new List<IoTInfo.ThingInfo>());
                return new List<IoTInfo.ThingInfo>();
            }
        }

        public async Task<bool> CreateThingAsync(string thingName = null, string thingType = null)
        {
            try
            {
                if (!CheckAuthentication()) return false;

                AmazonIoTClient client = clientHandler.GetClient();
                if (client == null)
                {
                    eventManager.TriggerThingCreated(false, "IoT client not initialized", thingName ?? newThingName);
                    return false;
                }

                string finalThingName = thingName ?? IoTInfo.GenerateThingName(newThingName, ioTInfo.PlatformPrefix, ioTInfo.UseTimestampInNames);
                string finalThingType = thingType ?? newThingType;

                LogDebug($"Creating IoT Thing: {finalThingName}" + (string.IsNullOrEmpty(finalThingType) ? "" : $", Type: {finalThingType}"));

                var createThingRequest = new CreateThingRequest { ThingName = finalThingName };
                if (!string.IsNullOrEmpty(finalThingType))
                {
                    createThingRequest.ThingTypeName = finalThingType;
                }

                var response = await client.CreateThingAsync(createThingRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    LogDebug($"Thing created successfully: {finalThingName}, ARN: {response.ThingArn}");
                    eventManager.TriggerThingCreated(true, "Thing created successfully", finalThingName);
                    return true;
                }

                LogError($"Failed to create Thing: {response.HttpStatusCode}");
                eventManager.TriggerThingCreated(false, $"Creation failed: {response.HttpStatusCode}", finalThingName);
                return false;
            }
            catch (Exception ex)
            {
                LogError($"IoT Create Thing error: {ex.Message}");
                eventManager.TriggerThingCreated(false, ex.Message, thingName ?? newThingName);
                return false;
            }
        }

        public async Task<IoTInfo.ThingInfo> GetThingAsync(string thingName = null)
        {
            try
            {
                string finalThingName = thingName ?? thingNameToGet;
                if (string.IsNullOrEmpty(finalThingName))
                {
                    LogError("Thing name is required");
                    eventManager.TriggerThingRetrieved(false, "Thing name is required", null);
                    return null;
                }

                if (!CheckAuthentication()) return null;

                AmazonIoTClient client = clientHandler.GetClient();
                if (client == null)
                {
                    eventManager.TriggerThingRetrieved(false, "IoT client not initialized", null);
                    return null;
                }

                LogDebug($"Getting Thing details: {finalThingName}");
                var describeThingRequest = new DescribeThingRequest { ThingName = finalThingName };
                var response = await client.DescribeThingAsync(describeThingRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    var thingInfo = new IoTInfo.ThingInfo
                    {
                        ThingName = response.ThingName,
                        ThingTypeName = response.ThingTypeName,
                        ThingArn = response.ThingArn,
                        Version = response.Version ?? 0,
                        CreationDate = DateTime.UtcNow,
                        LastModifiedDate = DateTime.UtcNow
                    };

                    if (response.Attributes != null && response.Attributes.Count > 0)
                    {
                        thingInfo.AttributesInfo = string.Join("; ", response.Attributes.Select(attr => $"{attr.Key}={attr.Value}"));
                        LogDebug($"Attributes found: {thingInfo.AttributesInfo}");
                    }

                    LogDebug($"Thing details retrieved: {thingInfo.ThingName}, Type: {thingInfo.ThingTypeName ?? "None"}, ARN: {thingInfo.ThingArn}");
                    eventManager.TriggerThingRetrieved(true, "Thing details retrieved successfully", thingInfo);
                    return thingInfo;
                }

                LogError($"Failed to get Thing: {response.HttpStatusCode}");
                eventManager.TriggerThingRetrieved(false, $"Failed to get Thing: {response.HttpStatusCode}", null);
                return null;
            }
            catch (Exception ex)
            {
                LogError($"IoT Get Thing error: {ex.Message}");
                eventManager.TriggerThingRetrieved(false, ex.Message, null);
                return null;
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
                Debug.Log($"[IoTThingHandler] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[IoTThingHandler] {message}");
        }
    }
}