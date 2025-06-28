using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Models.IoTCoreManagement
{
    [System.Serializable]
    public class IoTInfo
    {
        [SerializeField] private string platformPrefix = "tnp";
        [SerializeField] private bool useTimestampInNames = true;

        public string PlatformPrefix => platformPrefix;
        public bool UseTimestampInNames => useTimestampInNames;

        public class ThingInfo
        {
            public string ThingName;
            public string ThingTypeName;
            public string ThingArn;
            public long Version;
            public DateTime CreationDate;
            public DateTime LastModifiedDate;
            public string AttributesInfo;
        }

        [System.Serializable]
        public class CertificateData
        {
            public string CertificateId;
            public string CertificateArn;
            public string CertificatePem;
            public string PublicKey;
            public string PrivateKey;
            public bool IsActive;
            public DateTime CreationDate;

            public string GetSafeInfo()
            {
                try
                {
                    return $"ID: {CertificateId?.Substring(0, Math.Min(8, CertificateId?.Length ?? 0))}..., Active: {IsActive}, Created: {CreationDate:yyyy-MM-dd HH:mm:ss}";
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[IoTInfo] Error formatting CertificateData: {ex.Message}");
                    return $"Error: {ex.Message}";
                }
            }
        }

        public string GetDefaultPolicyDocument()
        {
            try
            {
                string prefix = string.IsNullOrEmpty(platformPrefix) ? "tnp" : platformPrefix;
                return $@"{{
  ""Version"": ""2012-10-17"",
  ""Statement"": [
    {{
      ""Effect"": ""Allow"",
      ""Action"": [""iot:Connect""],
      ""Resource"": ""arn:aws:iot:us-east-1:156041417101:client/{prefix}-*""
    }},
    {{
      ""Effect"": ""Allow"",
      ""Action"": [""iot:Publish"", ""iot:Receive""],
      ""Resource"": ""arn:aws:iot:us-east-1:156041417101:topic/{prefix}/*""
    }},
    {{
      ""Effect"": ""Allow"",
      ""Action"": [""iot:Subscribe""],
      ""Resource"": ""arn:aws:iot:us-east-1:156041417101:topicfilter/{prefix}/*""
    }},
    {{
      ""Effect"": ""Allow"",
      ""Action"": [
        ""iot:GetThingShadow"",
        ""iot:UpdateThingShadow"",
        ""iot:DeleteThingShadow""
      ],
      ""Resource"": ""arn:aws:iot:us-east-1:156041417101:thing/{prefix}-*""
    }}
  ]
}}";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IoTInfo] Error generating default policy: {ex.Message}");
                return "";
            }
        }

        public static string GenerateThingName(string baseName, string prefix, bool useTimestamp)
        {
            try
            {
                string finalPrefix = string.IsNullOrEmpty(prefix) ? "tnp" : prefix;
                string finalBaseName = string.IsNullOrEmpty(baseName) ? "test-device" : baseName;
                return useTimestamp
                    ? $"{finalPrefix}-{finalBaseName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}"
                    : $"{finalPrefix}-{finalBaseName}";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IoTInfo] Error generating thing name: {ex.Message}");
                return $"{prefix}-error";
            }
        }

        public static string GeneratePolicyName(string baseName, bool useTimestamp)
        {
            try
            {
                string finalBaseName = string.IsNullOrEmpty(baseName) ? "Unity-IoT-Policy" : baseName;
                return useTimestamp
                    ? $"{finalBaseName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}"
                    : finalBaseName;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IoTInfo] Error generating policy name: {ex.Message}");
                return "error-policy";
            }
        }
    }
}