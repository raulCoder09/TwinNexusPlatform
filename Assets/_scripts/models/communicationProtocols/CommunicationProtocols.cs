using System;
using System.Threading;
using System.Threading.Tasks;

namespace _scripts.models.communicationProtocols
{
    public abstract class CommunicationProtocols : ICommunicationProtocols, IDisposable
    {
        protected CancellationTokenSource _cancellationTokenSource;
        protected CancellationToken _cancellationToken;
        public DateTime DateTime { get; protected set; }
        public bool IsConnected { get; protected set; }
        public bool IsSentData { get; protected set; }
        public bool IsReceivedData { get; protected set; }
        public bool IsAutomaticConnecting { get; protected set;}
    
        protected CommunicationProtocols()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
        }

        public async Task Connect()
        {
            await ConnectCore();
            IsConnected = true;
            DateTime = DateTime.Now;
        }

        public async Task CheckConnection()
        {
            await CheckConnectionCore();
        }

        public async Task Reconnect()
        {
            await DisconnectCore();
            await ConnectCore();
            IsConnected = true;
            DateTime = DateTime.Now;
        }

        public async Task SendData()
        {
            await SendDataCore();
            IsSentData = true;
        }

        public async Task ReceiveData()
        {
            await ReceiveDataCore();
            IsReceivedData = true;
        }

        public async Task Disconnect()
        {
            Dispose();
            await DisconnectCore();
            IsConnected = false;
        }
        public virtual void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    
        protected abstract  Task ConnectCore();
        protected abstract Task CheckConnectionCore();
        protected abstract Task SendDataCore();
        protected abstract Task ReceiveDataCore();
        protected abstract Task DisconnectCore();
    }
}