using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace _scripts.Protocols
{
    internal class MqttManager
    {
        private readonly IMqttClient _mqttClient;
        private bool _isConnected;
        private string _host;
        private int _port;
        private string _clientId;
        private string _username;
        private string _password;
        private bool _cleanSession;
        private bool _useTls;
        private CancellationToken _cancellationToken;
        private string _pfxPath;
        private string _pfxPassword;
        private string _exceptionMessage;
        

        private byte[] _payloadReceived;
        public readonly struct InboundMessage
        {
            public readonly string Topic;
            public readonly ReadOnlyMemory<byte> Payload;
            public readonly DateTime ReceivedAtUtc;
            public InboundMessage(string topic, ReadOnlyMemory<byte> payload, DateTime receivedAtUtc)
            {
                Topic = topic;
                Payload = payload;
                ReceivedAtUtc = receivedAtUtc;
            }
        }
        private sealed class AsyncQueue<T>
        {
            private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
            private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

            public void Enqueue(T item)
            {
                _queue.Enqueue(item);
                _signal.Release();
            }

            public async IAsyncEnumerable<T> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
            {
                while (!ct.IsCancellationRequested)
                {
                    await _signal.WaitAsync(ct).ConfigureAwait(false);
                    while (_queue.TryDequeue(out var item))
                        yield return item;
                }
            }
        }

        private readonly AsyncQueue<InboundMessage> _inbox = new AsyncQueue<InboundMessage>();
        private readonly ConcurrentDictionary<string, InboundMessage> _lastByTopic = new ConcurrentDictionary<string, InboundMessage>();

        public IAsyncEnumerable<InboundMessage> ReadAllAsync(CancellationToken ct = default)
            => _inbox.ReadAllAsync(ct);

        public bool TryGetLast(string topic, out InboundMessage last)
            => _lastByTopic.TryGetValue(topic, out last);

        internal bool IsConnected
        {
            get => _isConnected;
            set => _isConnected = value;
        }

        internal string Host { get => _host; set => _host = value; }
        internal int Port { get => _port; set => _port = value; }
        internal string ClientId { get => _clientId; set => _clientId = value; }
        internal string Username { get => _username; set => _username = value; }
        internal string Password { get => _password; set => _password = value; }
        internal bool CleanSession { get => _cleanSession; set => _cleanSession = value; }
        internal bool UseTls { get => _useTls; set => _useTls = value; }
        internal CancellationToken CancellationToken { get => _cancellationToken; set => _cancellationToken = value; }
        internal string ExceptionMessage { get => _exceptionMessage; set => _exceptionMessage = value; }
        internal byte[] PayloadReceived => _payloadReceived;

        public MqttManager(
            string host = "localhost",
            int port = 1883,
            string clientId = null,
            string username = null,
            string password = null,
            bool cleanSession = true,
            bool useTls = false,
            CancellationToken cancellationToken = default,
            string pfxPath = null,
            string pfxPassword = null
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
            _pfxPath = pfxPath;
            _pfxPassword = pfxPassword;

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

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                try
                {
                    var app = e.ApplicationMessage;

                    _payloadReceived = app.Payload ?? app.PayloadSegment.ToArray();

                    var inbound = new InboundMessage(
                        topic: app.Topic,
                        payload: app.PayloadSegment,
                        receivedAtUtc: DateTime.UtcNow
                    );

                    _lastByTopic[app.Topic] = inbound;
                    _inbox.Enqueue(inbound);
                }
                catch (Exception exception)
                {
                    _exceptionMessage = $"error {exception.Message}";
                }
                return Task.CompletedTask;
            };
        }

        #region Connection
        internal async Task Connect()
        {
            var builder = new MqttClientOptionsBuilder()
                .WithClientId(_clientId)
                .WithTcpServer(_host, _port)
                .WithCleanSession(_cleanSession)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30));

            if (_useTls)
            {
                if (string.IsNullOrWhiteSpace(_pfxPath))
                    throw new InvalidOperationException("TLS was enabled but no pfxPath was specified.");

                var clientCert = new X509Certificate2(
                    _pfxPath,
                    string.IsNullOrEmpty(_pfxPassword) ? null : _pfxPassword,
                    X509KeyStorageFlags.UserKeySet |
                    X509KeyStorageFlags.PersistKeySet |
                    X509KeyStorageFlags.Exportable);

                if (!clientCert.HasPrivateKey)
                    throw new InvalidOperationException("The .pfx does not contain a private key.");

                builder.WithTlsOptions(tls =>
                {
                    tls.UseTls();
                    tls.WithSslProtocols(SslProtocols.Tls12);
                    tls.WithClientCertificates(new[] { clientCert });
                    tls.WithAllowUntrustedCertificates(false);
                    tls.WithIgnoreCertificateChainErrors(false);
                    tls.WithIgnoreCertificateRevocationErrors(true);
                    tls.WithCertificateValidationHandler(ctx =>
                    {
                        return ctx.SslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
                    });
                });
            }

            var options = builder.Build();

            if (!_mqttClient.IsConnected)
                await _mqttClient.ConnectAsync(options, _cancellationToken).ConfigureAwait(false);
        }

        internal async Task Disconnect()
        {
            if (_mqttClient == null || !_mqttClient.IsConnected) return;

            try
            {
                await _mqttClient.DisconnectAsync().ConfigureAwait(false);
                IsConnected = false;
            }
            catch (Exception e)
            {
                _exceptionMessage = $"error disconnecting = {e}";
                throw;
            }
        }
        #endregion

        #region Message
        internal async Task Publish(string topic, string payload, int qos = 1, bool retained = false)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
                throw new InvalidOperationException("Unable to publish, client is not connected.");

            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .WithRetainFlag(retained)
                    .Build();

                await _mqttClient.PublishAsync(message, _cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _exceptionMessage = $"Error publishing in {topic}: {ex.Message}";
            }
        }

        internal async Task Publish(string topic, object payload, int qos = 1, bool retained = false)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            string jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            await Publish(topic, jsonPayload, qos, retained).ConfigureAwait(false);
        }
        #endregion

        #region Subscription
        
        internal async Task Subscribe(string topic, int qos = 1)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
                throw new InvalidOperationException("Unable to subscribe, client is not connected.");

            try
            {
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                    .Build()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _exceptionMessage = $"Error subscribing to {topic}: {ex.Message}";
            }
        }

        internal async Task Unsubscribe(string topic)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
                throw new InvalidOperationException("Unable to unsubscribe, client is not connected.");

            try
            {
                var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder()
                    .WithTopicFilter(topic)
                    .Build();

                await _mqttClient.UnsubscribeAsync(unsubscribeOptions).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _exceptionMessage = $"Error unsubscribing from {topic}: {ex.Message}";
            }
        }

        internal async Task Unsubscribe(params string[] topics)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
                throw new InvalidOperationException("Unable to unsubscribe, client is not logged in.");

            try
            {
                var builder = new MqttClientUnsubscribeOptionsBuilder();

                foreach (var topic in topics)
                    builder.WithTopicFilter(topic);

                var unsubscribeOptions = builder.Build();
                await _mqttClient.UnsubscribeAsync(unsubscribeOptions).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _exceptionMessage = $"Error unsubscribing: {ex.Message}";
            }
        }
        #endregion

        #region extra
        // TODO: funciones extra
        #endregion
    }
}
