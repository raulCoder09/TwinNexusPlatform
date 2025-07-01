namespace _Scripts.Models.MQTTManagement
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using MQTTnet;
    using MQTTnet.Client;
    using UnityEngine;
    using _Scripts.Models.CertificateManagement;

    public class MqttConnectionHandler : IMqttOperations
    {
        private readonly CertificateManager _certificateManager;
        private readonly MqttEventManager _eventManager;
        private readonly bool _enableDebugLogs = true;
        private IMqttClient _client;
        private MqttFactory _factory;

        public MqttConnectionHandler(CertificateManager certificateManager, MqttEventManager eventManager)
        {
            _certificateManager = certificateManager ?? throw new ArgumentNullException(nameof(certificateManager));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        }

        public IMqttClient GetClient()
        {
            return _client;
        }

        public async Task<bool> ConnectAsync(string brokerAddress, int brokerPort, string clientId, string username, string password, bool useSSL, string certificatePath, string certificateFileName, string certificatePassword)
        {
            try
            {
                if (!ValidateConnectionParameters(brokerAddress, brokerPort))
                {
                    _eventManager.TriggerConnectionFailed("Invalid connection parameters");
                    return false;
                }

                if (!InitializeMqttClient())
                {
                    _eventManager.TriggerConnectionFailed("Failed to initialize MQTT client");
                    return false;
                }

                var options = await BuildConnectionOptionsAsync(brokerAddress, brokerPort, clientId, username, password, useSSL, certificatePath, certificateFileName, certificatePassword);
                if (options == null)
                {
                    _eventManager.TriggerConnectionFailed("Failed to build connection options");
                    return false;
                }

                ConfigureEventHandlers();

                bool connected = await AttemptConnectionAsync(options);
                if (connected)
                {
                    LogDebug($"Successfully connected to broker: {brokerAddress}:{brokerPort}");
                    _eventManager.TriggerConnected();
                }
                else
                {
                    _eventManager.TriggerConnectionFailed("Failed to connect to broker");
                }

                return connected;
            }
            catch (Exception ex)
            {
                LogError($"Error connecting to broker: {ex.Message}");
                _eventManager.TriggerConnectionFailed(ex.Message);
                return false;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            if (_client == null || !_client.IsConnected)
            {
                LogDebug("Client is not connected");
                return false;
            }

            try
            {
                await _client.DisconnectAsync();
                LogDebug("Successfully disconnected from broker");
                _eventManager.TriggerDisconnected("Normal disconnection");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from broker: {ex.Message}");
                _eventManager.TriggerDisconnected(ex.Message);
                return false;
            }
        }

        public async Task<bool> ReconnectAsync()
        {
            if (_client == null)
            {
                LogError("Cannot reconnect: Client is null");
                return false;
            }

            if (_client.IsConnected)
            {
                LogDebug("Client is already connected");
                return true;
            }

            LogDebug("Attempting to reconnect to broker...");
            try
            {
                bool reconnected = await AttemptConnectionAsync(_client.Options);
                if (reconnected)
                {
                    LogDebug("Successfully reconnected to broker");
                    _eventManager.TriggerConnected();
                }
                else
                {
                    LogError("Failed to reconnect to broker");
                    _eventManager.TriggerConnectionFailed("Reconnection failed");
                }
                return reconnected;
            }
            catch (Exception ex)
            {
                LogError($"Error during reconnection: {ex.Message}");
                _eventManager.TriggerConnectionFailed(ex.Message);
                return false;
            }
        }

        public Task<bool> SubscribeAsync(string topic, MqttInfo.QoSLevel qos, Action<string, string> messageHandler)
        {
            throw new NotImplementedException("Subscription handled by MqttMessageHandler");
        }

        public Task<bool> UnsubscribeAsync(string topic)
        {
            throw new NotImplementedException("Unsubscription handled by MqttMessageHandler");
        }

        public Task<bool> PublishAsync(string topic, string message, MqttInfo.QoSLevel qos, bool retain)
        {
            throw new NotImplementedException("Publishing handled by MqttMessageHandler");
        }

        public Task<bool> PublishObjectAsync<T>(string topic, T obj, MqttInfo.QoSLevel qos, bool retain)
        {
            throw new NotImplementedException("Publishing handled by MqttMessageHandler");
        }

        public bool IsConnected()
        {
            return _client != null && _client.IsConnected;
        }

        private bool ValidateConnectionParameters(string brokerAddress, int brokerPort)
        {
            if (string.IsNullOrEmpty(brokerAddress))
            {
                LogError("Broker address is required");
                return false;
            }

            if (brokerPort <= 0 || brokerPort > 65535)
            {
                LogError($"Invalid port {brokerPort}");
                return false;
            }

            return true;
        }

        private bool InitializeMqttClient()
        {
            try
            {
                if (_client != null)
                {
                    _client.Dispose();
                }
                _factory = new MqttFactory();
                _client = _factory.CreateMqttClient();
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize MQTT client: {ex.Message}");
                return false;
            }
        }

        private async Task<MqttClientOptions> BuildConnectionOptionsAsync(string brokerAddress, int brokerPort, string clientId, string username, string password, bool useSSL, string certificatePath, string certificateFileName, string certificatePassword)
        {
            try
            {
                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithClientId(clientId)
                    .WithTimeout(TimeSpan.FromSeconds(30))
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(60));

                if (useSSL)
                {
                    int sslPort = brokerPort == 1883 ? 8883 : brokerPort;
                    optionsBuilder.WithTcpServer(brokerAddress, sslPort);

                    if (!string.IsNullOrEmpty(certificatePath) && !string.IsNullOrEmpty(certificateFileName))
                    {
                        LogDebug($"Loading certificate: {certificateFileName} from {certificatePath}");
                        var certificate = await _certificateManager.LoadX509CertificateAsync(certificatePath, certificateFileName, certificatePassword);
                        if (certificate != null)
                        {
                            LogDebug($"Certificate loaded successfully: {certificate.Subject}");
                            optionsBuilder.WithTls(tls =>
                            {
                                tls.UseTls = true;
                                tls.SslProtocol = System.Security.Authentication.SslProtocols.Tls12;
                                tls.Certificates = new List<X509Certificate> { certificate };
                                tls.AllowUntrustedCertificates = false;
                                tls.IgnoreCertificateChainErrors = false;
                                tls.IgnoreCertificateRevocationErrors = true;
                                tls.CertificateValidationHandler = context =>
                                {
                                    if (context.Certificate != null)
                                    {
                                        LogDebug($"Server certificate validated: {context.Certificate.Subject}");
                                        return true;
                                    }
                                    LogError("Server certificate validation failed: No certificate provided");
                                    return false;
                                };
                            });
                        }
                        else
                        {
                            LogError($"Failed to load certificate: {certificateFileName}");
                            return null;
                        }
                    }
                    else
                    {
                        optionsBuilder.WithTls(tls => tls.UseTls = true);
                        LogDebug("Using SSL without client certificate");
                    }
                }
                else
                {
                    optionsBuilder.WithTcpServer(brokerAddress, brokerPort);
                    LogDebug($"Using TCP connection on port {brokerPort}");
                }

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    optionsBuilder.WithCredentials(username, password);
                    LogDebug("Using authentication credentials");
                }
                else
                {
                    LogDebug("Connecting without username/password");
                }

                return optionsBuilder.Build();
            }
            catch (Exception ex)
            {
                LogError($"Error building connection options: {ex.Message}");
                return null;
            }
        }

        private void ConfigureEventHandlers()
        {
            _client.ConnectedAsync += async e =>
            {
                LogDebug($"Connected to broker");
                _eventManager.TriggerConnected();
            };

            _client.DisconnectedAsync += async e =>
            {
                string reason = e.Reason.ToString();
                LogDebug($"Disconnected from broker. Reason: {reason}");
                if (e.Exception != null)
                {
                    LogError($"Disconnection exception: {e.Exception.Message}");
                }
                _eventManager.TriggerDisconnected(reason);
            };
        }

        private async Task<bool> AttemptConnectionAsync(MqttClientOptions options)
        {
            try
            {
                LogDebug("Attempting connection to broker...");
                var connectTask = _client.ConnectAsync(options);
                var timeoutTask = Task.Delay(30000);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    LogError($"Connection timeout after 30000ms");
                    return false;
                }

                await connectTask;
                bool isConnected = _client.IsConnected;
                if (isConnected)
                {
                    LogDebug("Connection successful!");
                }
                else
                {
                    LogError("Connection failed - client not connected");
                }
                return isConnected;
            }
            catch (Exception ex)
            {
                LogError($"Connection attempt failed: {ex.Message}");
                return false;
            }
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[MqttConnectionHandler] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[MqttConnectionHandler] {message}");
        }
    }
}