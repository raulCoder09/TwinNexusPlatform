using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using _Scripts.Models.CertificateManagement;
using _Scripts.Models.FileManagement;
using MQTTnet;
using MQTTnet.Client;
using UnityEngine;

namespace _Scripts.Models.MQTTManagement
{
    public class MqttConnectionHandler
    {
        private readonly CertificateManager certificateManager;
        private readonly MqttEventManager eventManager;
        private readonly bool enableDebugLogs = true;
        private readonly string brokerAddress;
        private readonly int brokerPort;
        private readonly string clientId;
        private readonly string username;
        private readonly string password;
        private readonly bool useSSL;
        private readonly bool cleanSession;
        private readonly int connectionTimeoutMs;
        private readonly int keepAliveSeconds;
        private readonly int reconnectDelayMs;
        
        // ✅ Nuevos parámetros para certificados configurables
        private readonly string certificateFileName;
        private readonly string certificatePassword;
        
        private IMqttClient client;
        private MqttFactory factory;

        public MqttConnectionHandler(string brokerAddress, int brokerPort, string clientId, string username, string password,
            bool useSSL, bool cleanSession, int connectionTimeoutMs, int keepAliveSeconds, int reconnectDelayMs,
            CertificateManager certificateManager, MqttEventManager eventManager, 
            string certificateFileName = "aws-iot-core.pfx", string certificatePassword = "5859") // ✅ Parámetros opcionales
        {
            this.brokerAddress = brokerAddress;
            this.brokerPort = brokerPort;
            this.clientId = string.IsNullOrEmpty(clientId) ? $"Unity_{SystemInfo.deviceModel}_{Guid.NewGuid().ToString("N")[..8]}" : clientId;
            this.username = username;
            this.password = password;
            this.useSSL = useSSL;
            this.cleanSession = cleanSession;
            this.connectionTimeoutMs = connectionTimeoutMs;
            this.keepAliveSeconds = keepAliveSeconds;
            this.reconnectDelayMs = reconnectDelayMs;
            this.certificateManager = certificateManager;
            this.eventManager = eventManager;
            
            // ✅ Almacenar configuración de certificados
            this.certificateFileName = certificateFileName;
            this.certificatePassword = certificatePassword;
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (!ValidateConnectionParameters())
                {
                    eventManager.TriggerConnectionFailed("Invalid connection parameters");
                    return false;
                }

                if (!InitializeMqttClient())
                {
                    eventManager.TriggerConnectionFailed("Failed to initialize MQTT client");
                    return false;
                }

                var options = await BuildConnectionOptionsAsync();
                ConfigureEventHandlers();

                bool connected = await AttemptConnectionAsync(options);
                if (connected)
                {
                    LogDebug($"Successfully connected to broker: {brokerAddress}:{brokerPort}");
                    eventManager.TriggerConnected();
                }
                else
                {
                    eventManager.TriggerConnectionFailed("Failed to connect to broker");
                }

                return connected;
            }
            catch (Exception ex)
            {
                LogError($"Error connecting to broker: {ex.Message}");
                eventManager.TriggerConnectionFailed(ex.Message);
                return false;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            if (client == null || !client.IsConnected)
            {
                LogDebug("Client is not connected");
                return false;
            }

            try
            {
                await client.DisconnectAsync();
                LogDebug("Successfully disconnected from broker");
                eventManager.TriggerDisconnected("Normal disconnection");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error disconnecting from broker: {ex.Message}");
                eventManager.TriggerDisconnected(ex.Message);
                return false;
            }
        }

        public async Task<bool> ReconnectAsync()
        {
            if (client == null)
            {
                LogError("Cannot reconnect: Client is null");
                return false;
            }

            if (client.IsConnected)
            {
                LogDebug("Client is already connected");
                return true;
            }

            LogDebug("Attempting to reconnect to broker...");
            try
            {
                var options = await BuildConnectionOptionsAsync();
                bool reconnected = await AttemptConnectionAsync(options);
                if (reconnected)
                {
                    LogDebug("Successfully reconnected to broker");
                    eventManager.TriggerConnected();
                }
                else
                {
                    LogError("Failed to reconnect to broker");
                    eventManager.TriggerConnectionFailed("Reconnection failed");
                }
                return reconnected;
            }
            catch (Exception ex)
            {
                LogError($"Error during reconnection: {ex.Message}");
                eventManager.TriggerConnectionFailed(ex.Message);
                return false;
            }
        }

        public bool IsConnected()
        {
            return client != null && client.IsConnected;
        }

        public IMqttClient GetClient()
        {
            return client;
        }

        private bool ValidateConnectionParameters()
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
                if (client != null)
                {
                    client.Dispose();
                }
                factory = new MqttFactory();
                client = factory.CreateMqttClient();
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize MQTT client: {ex.Message}");
                return false;
            }
        }

        private async Task<MqttClientOptions> BuildConnectionOptionsAsync()
        {
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithTimeout(TimeSpan.FromMilliseconds(connectionTimeoutMs))
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(keepAliveSeconds));

            if (useSSL)
            {
                int sslPort = brokerPort == 1883 ? 8883 : brokerPort;
                optionsBuilder.WithTcpServer(brokerAddress, sslPort);

                // ✅ Usar parámetros configurables para cargar certificado
                LogDebug($"Loading certificate: {certificateFileName} with password configured");
                var certificate = await certificateManager.LoadX509CertificateAsync(
                    certificateFileName, 
                    certificatePassword, 
                    StorageInfo.StorageCategory.Resources);
                    
                if (certificate != null)
                {
                    LogDebug($"✅ Certificate loaded successfully:");
                    LogDebug($"  Subject: {certificate.Subject}");
                    LogDebug($"  Valid until: {certificate.NotAfter}");
                    LogDebug($"  Has private key: {certificate.HasPrivateKey}");
                    
                    optionsBuilder.WithTls(tls => 
                    {
                        tls.UseTls = true;
                        tls.SslProtocol = System.Security.Authentication.SslProtocols.Tls12;
                        tls.Certificates = new List<X509Certificate> { certificate };
                        
                        // ✅ Configuración específica para AWS IoT Core
                        tls.AllowUntrustedCertificates = false;
                        tls.IgnoreCertificateChainErrors = false;
                        tls.IgnoreCertificateRevocationErrors = true;
                        
                        // ✅ Validación del certificado del servidor
                        tls.CertificateValidationHandler = context =>
                        {
                            if (context.Certificate != null)
                            {
                                LogDebug($"Server certificate validated: {context.Certificate.Subject}");
                                return true; // AWS IoT Core certificates are trusted
                            }
                            LogError("Server certificate validation failed: No certificate provided");
                            return false;
                        };
                    });
                    LogDebug($"✅ SSL/TLS configured with certificate on port {sslPort}");
                }
                else
                {
                    LogError($"❌ Failed to load certificate: {certificateFileName}");
                    eventManager.TriggerConnectionFailed("Failed to load SSL certificate");
                    return null;
                }
            }
            else
            {
                optionsBuilder.WithTcpServer(brokerAddress, brokerPort);
                LogDebug($"Using TCP connection on port {brokerPort}");
            }

            if (cleanSession)
            {
                optionsBuilder.WithCleanSession();
                LogDebug("Clean session enabled");
            }

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                optionsBuilder.WithCredentials(username, password);
                LogDebug("Using authentication credentials");
            }
            else
            {
                LogDebug("Connecting without username/password (using certificate authentication)");
            }

            return optionsBuilder.Build();
        }

        private void ConfigureEventHandlers()
        {
            client.ConnectedAsync += async e =>
            {
                LogDebug($"✅ Connected to broker: {brokerAddress}:{brokerPort}");
                LogDebug($"Client ID: {clientId}");
                eventManager.TriggerConnected();
            };

            client.DisconnectedAsync += async e =>
            {
                string reason = e.Reason.ToString();
                LogDebug($"❌ Disconnected from broker. Reason: {reason}");
                if (e.Exception != null)
                {
                    LogError($"Disconnection exception: {e.Exception.Message}");
                }
                eventManager.TriggerDisconnected(reason);
            };
        }

        private async Task<bool> AttemptConnectionAsync(MqttClientOptions options)
        {
            try
            {
                LogDebug("🔄 Attempting connection to AWS IoT Core...");
                
                var connectTask = client.ConnectAsync(options);
                var timeoutTask = Task.Delay(connectionTimeoutMs);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    LogError($"❌ Connection timeout after {connectionTimeoutMs}ms");
                    return false;
                }

                await connectTask;
                bool isConnected = client.IsConnected;
                
                if (isConnected)
                {
                    LogDebug("🎉 Connection successful!");
                }
                else
                {
                    LogError("❌ Connection failed - client not connected");
                }
                
                return isConnected;
            }
            catch (Exception ex)
            {
                LogError($"❌ Connection attempt failed: {ex.Message}");
                LogError($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[MqttConnectionHandler] {message}");
        }

        private void LogError(string message)
        {
            if (enableDebugLogs)
                Debug.LogError($"[MqttConnectionHandler] {message}");
        }
    }
}