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
        public string ModeConnection
        {
            get => _modeConnection;
            set => _modeConnection = value;
        }
    }
}
