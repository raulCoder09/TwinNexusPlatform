using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace _scripts.models.communicationProtocols
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public string Topic { get; }
        public Dictionary<string, JsonElement>? Data { get; }
        public DateTime ReceivedAt { get; }

        public MessageReceivedEventArgs(string topic, Dictionary<string, JsonElement>? data, DateTime receivedAt)
        {
            Topic = topic;
            Data = data;
            ReceivedAt = receivedAt;
        }
    }

    public sealed class MqttProtocol:CommunicationProtocols
    {
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
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
        private readonly IMqttClient _mqttClient;
        private string?
            _endpoint,
            _clientId,
            _username,
            _password,
            _pfxPath,
            _pfxPassword,
            _exceptionMessage,
            _dataLoading,
            _topicReceived,
            _topic,
            _jsonExceptionMessageError,
            _jsonReceived;

        private object _payload;
    
        private bool 
            _isConnected,
            _cleanSession, 
            _useTls,
            _retained = false;
    
        private int 
            _port,
            _qos = 1;
        private byte[] _payloadReceived;
        private readonly ConcurrentDictionary<string, InboundMessage> _lastByTopic = new ConcurrentDictionary<string, InboundMessage>();
        private readonly AsyncQueue<InboundMessage> _inbox = new AsyncQueue<InboundMessage>();
    
        internal string Topic
        {
            get => _topic;
            set => _topic = value;
        }
    
        internal Object Payload
        {
            get => _payload;
            set => _payload = value;
        }
    
        internal string DataLoading
        {
            get => _dataLoading;
            set => _dataLoading = value;
        }
    
        
        
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
    
        private static MqttProtocol _instance = null;
        private static object _lock = new object();

        public static MqttProtocol GetInstance(
            string endpoint,
            int port,
            string? clientId,
            string? username,
            string? password,
            bool cleanSession,
            bool useTls,
            CancellationToken cancellationToken,
            string? pfxPath,
            string? pfxPassword
        )
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance=new MqttProtocol(endpoint,port,clientId,username,password,cleanSession,useTls,cancellationToken,pfxPath,pfxPassword);
                }
            }
            return _instance;
        }
    
        private MqttProtocol(
            string endpoint,
            int port,
            string? clientId,
            string? username,
            string? password,
            bool cleanSession,
            bool useTls,
            CancellationToken cancellationToken,
            string? pfxPath,
            string? pfxPassword
        )
        {
            var factory = new MqttFactory();
            _endpoint = endpoint;
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
    
    
    
        protected override async Task ConnectCore()
        {
            var builder = new MqttClientOptionsBuilder()
                .WithClientId(_clientId)
                .WithTcpServer(_endpoint, _port)
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
            {
                await _mqttClient.ConnectAsync(options, _cancellationToken).ConfigureAwait(false);
                if (_mqttClient.IsConnected)
                {
                    _topic = "Connection status";
                    _payload = new { message= "Connection established"};
                    await SendDataCore();
                    _ = ReceiveDataCore();
                    _topic = "Listen status";
                    _payload = new { message= "listening"};
                    await SendDataCore();
                }
            }
        }

        protected override Task CheckConnectionCore()
        {
            Console.WriteLine("Checking connection...");
            return Task.CompletedTask;
        }
    
        internal async Task Subscribe()
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
                throw new InvalidOperationException("Unable to subscribe, client is not connected.");
            try
            {
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(_topic)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)_qos)
                    .Build()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _exceptionMessage = $"Error subscribing to {_topic}: {ex.Message}";
            }
        }
    
        protected override async Task SendDataCore()
        {
            if (_payload == null)
                throw new ArgumentNullException(nameof(_payload));

            var jsonPayload = JsonSerializer.Serialize(_payload, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            if (_mqttClient == null || !_mqttClient.IsConnected)
                throw new InvalidOperationException("Unable to publish, client is not connected.");
            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(_topic)
                    .WithPayload(jsonPayload)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)_qos)
                    .WithRetainFlag(_retained)
                    .Build();

                await _mqttClient.PublishAsync(message, _cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _exceptionMessage = $"Error publishing in {_topic}: {ex.Message}";
            }
        }
    
        protected override async Task ReceiveDataCore()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                await foreach (var msg in ReadAllAsync(_cancellationTokenSource.Token))
                { 
                    _topicReceived = msg.Topic;
                    _dataLoading = Encoding.UTF8.GetString(msg.Payload.ToArray());
                
                    var data = ProcessData();
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(
                        _topicReceived, 
                        data, 
                        msg.ReceivedAtUtc
                    ));
                }
            }
            catch (OperationCanceledException)
            {
                _exceptionMessage = "Listener cancelado";
            }
            catch (Exception ex)
            {
                _exceptionMessage = $"Error: {ex.Message}";
            }
        }
    
        internal async Task Unsubscribe(string topic)
        {
            if (_mqttClient is not { IsConnected: true })
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

        protected override async Task DisconnectCore()
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
        public override void Dispose()
        {
            base.Dispose();
        }
        public Dictionary<string, JsonElement>? ProcessData()
        {
            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(DataLoading);
                return data;
            }
            catch (JsonException ex)
            {
                _jsonExceptionMessageError=$"Error deserializando JSON: {ex.Message}";
                _jsonReceived=$"JSON recibido: {DataLoading}";
            }

            return null;
        }
        private IAsyncEnumerable<InboundMessage> ReadAllAsync(CancellationToken cancellationToken = default)
            => _inbox.ReadAllAsync(cancellationToken);
    }
}