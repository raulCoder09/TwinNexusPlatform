using System;
using System.Threading.Tasks;

namespace _scripts.models.communicationProtocols
{
    public interface ICommunicationProtocols
    {
        DateTime DateTime{ get; }
        bool IsConnected { get; }
        bool IsSentData { get; }
        bool IsReceivedData { get; }
        bool IsAutomaticConnecting { get; }
        Task Connect();
        Task CheckConnection();
        Task Reconnect();
        Task SendData();
        Task ReceiveData();
        Task Disconnect();
    }
}