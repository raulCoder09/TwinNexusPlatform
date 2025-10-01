using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MQTTnet;
using MQTTnet.Client;

namespace _scripts.Protocols
{
    public class MqttManager
    {
        private readonly IMqttClient _mqttClient;
        private bool _isConnected;
        private string _host;
        private int _port;
        [CanBeNull] private string _clientId;
        [CanBeNull] private string _username;
        [CanBeNull] private string _password;
        private bool _cleanSession;
        private bool _useTls;
        private CancellationToken _cancellationToken;

        internal bool IsConnected
        {
            get => _isConnected;
            set => _isConnected = value;
        }

        internal string Host
        {
            get => _host;
            set => _host = value;
        }

        internal int Port
        {
            get => _port;
            set => _port = value;
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

        internal bool CleanSession
        {
            get => _cleanSession;
            set => _cleanSession = value;
        }

        internal bool UseTls
        {
            get => _useTls;
            set => _useTls = value;
        }

        internal CancellationToken CancellationToken
        {
            get => _cancellationToken;
            set => _cancellationToken = value;
        }

        public MqttManager(
            string host="localhost",
            int port=1883, 
            string clientId=null,
            string username=null,
            string password=null,
            bool cleanSession = true,
            bool useTls = false,
            CancellationToken cancellationToken = default
            )
        {
            var factory = new MqttFactory();
            _host = host;
            _port = port;
            _clientId = clientId;
            _username = username;
            _password = password;
            _cleanSession = cleanSession;
            _useTls = useTls;
            _cancellationToken = cancellationToken;
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.ConnectedAsync += arg =>
            {
                _isConnected = true;
                return Task.CompletedTask;
            };
            _mqttClient.DisconnectedAsync += arg =>
            {
                _isConnected = false;
                return Task.CompletedTask;
            };
        }

        #region Conexión
        
        internal async Task Connect()
        {
            var builder = new MqttClientOptionsBuilder()
                .WithClientId(_clientId)
                .WithTcpServer(_host, _port)
                .WithCleanSession(_cleanSession)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30));
            
            var options = builder.Build();
            
            if (_mqttClient.IsConnected)return;
            await _mqttClient.ConnectAsync(options, _cancellationToken);
        }


        #endregion

        #region Mensajes

        // Mensajes

        #endregion

        #region Suscripción

        // Suscripción

        #endregion

        #region Mantenimiento de sesión

        // Mantenimiento de sesión

        #endregion
    }
}