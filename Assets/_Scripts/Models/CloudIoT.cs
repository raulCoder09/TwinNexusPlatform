using UnityEngine;

namespace _Scripts.Models
{
    public class CloudIoT : MonoBehaviour
    {
        private string _endpoint;
        private string _clientId;
        private string _thingName;
        private string _caFilePath;
        private string _clientCertPath;
        private string _clientKeyPath;
        private string _modeConnection;
        private string _port;

        internal string endpoint
        {
            get => _endpoint;
            set => _endpoint = value;
        }

        internal string clientId
        {
            get => _clientId;
            set => _clientId = value;
        }

        internal string thingName
        {
            get => _thingName;
            set => _thingName = value;
        }

        public string port
        {
            get => _port;
            set => _port = value;
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

        internal string modeConnection
        {
            get => _modeConnection;
            set => _modeConnection = value;
        }

        internal void LoadCertificates()
        {
            
        }
    }
}