using UnityEngine;
using UnityEngine.Serialization;

namespace _ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewIotConfigurationData", menuName = "ScriptableObjects/IotConfigurationData")]
    public class IotConfigurationData : ScriptableObject
    {
        [SerializeField] private string _brokerAddress;
        [SerializeField] private int _brokerPort; 
        [SerializeField] private string _clientId;
        [SerializeField] private string _username; 
        [SerializeField] private string _password;
        [SerializeField] private string _modeConnection;
        
        
        [SerializeField] private string _endpoint;
        [SerializeField] private string _thingName;
        [SerializeField] private string _cloudPort;
        [SerializeField] private string _caFilePath;
        [SerializeField] private string _clientCertPath;
        [SerializeField] private string _clientKeyPath;
        [SerializeField] private string _cloudModeConnection;
        [SerializeField] private string _pfxFilePath;

        public string pfxFilePath
        {
            get => _pfxFilePath;
            set => _pfxFilePath = value;
        }

        internal string brokerAddress
        {
            get => _brokerAddress;
            set => _brokerAddress = value;
        }

        internal int BrokerPort
        {
            get => _brokerPort;
            set => _brokerPort = value;
        }

        internal string ClientId
        {
            get => _clientId;
            set => _clientId = value;
        }

        internal string Username
        {
            get => _username;
            set => _username = value;
        }

        internal string Password
        {
            get => _password;
            set => _password = value;
        }
        internal string ModeConnection
        {
            get => _modeConnection;
            set => _modeConnection = value;
        }

        internal string endpoint
        {
            get => _endpoint;
            set => _endpoint = value;
        }

        internal string thingName
        {
            get => _thingName;
            set => _thingName = value;
        }

        public string cloudPort
        {
            get => _cloudPort;
            set => _cloudPort = value;
        }

        internal string caFilePath
        {
            get => _caFilePath;
            set => _caFilePath = value;
        }

        internal string clientCertPath
        {
            get => _clientCertPath;
            set => _clientCertPath = value;
        }

        internal string clientKeyPath
        {
            get => _clientKeyPath;
            set => _clientKeyPath = value;
        }

        internal string cloudModeConnection
        {
            get => _cloudModeConnection;
            set => _cloudModeConnection = value;
        }
    }
}
