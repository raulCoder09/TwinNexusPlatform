using UnityEngine;
using Amazon;

namespace _Scripts.Models.CognitoManagement
{
    [CreateAssetMenu(fileName = "SettingsCognitoParametersData", menuName = "AWS/Cognito/Settings Data")]
    [System.Serializable]
    public class SettingsCognitoParametersData : ScriptableObject
    {
        [Header("AWS Cognito Configuration")]
        [SerializeField] private string userPoolId = "";
        [SerializeField] private string clientId = "";
        [SerializeField] private string identityPoolId = "";
        [SerializeField] private string awsRegionCode = "us-east-1";
        
        [Header("Validation")]
        [SerializeField] private bool isConfigurationValid = false;
        
        // Public properties
        public string UserPoolId
        {
            get => userPoolId;
            set => userPoolId = value;
        }
        
        public string ClientId
        {
            get => clientId;
            set => clientId = value;
        }
        
        public string IdentityPoolId
        {
            get => identityPoolId;
            set => identityPoolId = value;
        }
        
        public string AwsRegionCode
        {
            get => awsRegionCode;
            set => awsRegionCode = value;
        }
        
        public bool IsConfigurationValid
        {
            get => isConfigurationValid;
            private set => isConfigurationValid = value;
        }
        
        // Methods
        public RegionEndpoint GetRegionEndpoint()
        {
            return awsRegionCode switch
            {
                "us-east-1" => RegionEndpoint.USEast1,
                "us-east-2" => RegionEndpoint.USEast2,
                "us-west-1" => RegionEndpoint.USWest1,
                "us-west-2" => RegionEndpoint.USWest2,
                "eu-west-1" => RegionEndpoint.EUWest1,
                "eu-central-1" => RegionEndpoint.EUCentral1,
                "ap-southeast-1" => RegionEndpoint.APSoutheast1,
                "ap-northeast-1" => RegionEndpoint.APNortheast1,
                _ => RegionEndpoint.USEast1
            };
        }
        
        public void SetConfiguration(string userPoolId, string clientId, string identityPoolId, string regionCode)
        {
            this.userPoolId = userPoolId;
            this.clientId = clientId;
            this.identityPoolId = identityPoolId;
            this.awsRegionCode = regionCode;
            
            ValidateConfiguration();
        }
        
        public void ValidateConfiguration()
        {
            isConfigurationValid = !string.IsNullOrEmpty(userPoolId) &&
                                   !string.IsNullOrEmpty(clientId) &&
                                   !string.IsNullOrEmpty(identityPoolId) &&
                                   !string.IsNullOrEmpty(awsRegionCode);
        }
        
        public void ResetToDefaults()
        {
            userPoolId = "";
            clientId = "";
            identityPoolId = "";
            awsRegionCode = "us-east-1";
            isConfigurationValid = false;
        }
        
        // For JSON serialization
        [System.Serializable]
        public class SerializableData
        {
            public string userPoolId;
            public string clientId;
            public string identityPoolId;
            public string awsRegionCode;
            
            public SerializableData(SettingsCognitoParametersData data)
            {
                userPoolId = data.UserPoolId;
                clientId = data.ClientId;
                identityPoolId = data.IdentityPoolId;
                awsRegionCode = data.AwsRegionCode;
            }
        }
        
        public SerializableData ToSerializableData()
        {
            return new SerializableData(this);
        }
        
        public void FromSerializableData(SerializableData data)
        {
            SetConfiguration(data.userPoolId, data.clientId, data.identityPoolId, data.awsRegionCode);
        }
    }
}