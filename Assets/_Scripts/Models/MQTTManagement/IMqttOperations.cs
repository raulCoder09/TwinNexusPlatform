namespace _Scripts.Models.MQTTManagement
{
    using System;
    using System.Threading.Tasks;

    public interface IMqttOperations
    {
        Task<bool> ConnectAsync(string brokerAddress, int brokerPort, string clientId, string username, string password, bool useSSL, string certificatePath, string certificateFileName, string certificatePassword);
        Task<bool> DisconnectAsync();
        Task<bool> ReconnectAsync();
        Task<bool> SubscribeAsync(string topic, MqttInfo.QoSLevel qos, Action<string, string> messageHandler);
        Task<bool> UnsubscribeAsync(string topic);
        Task<bool> PublishAsync(string topic, string message, MqttInfo.QoSLevel qos, bool retain);
        Task<bool> PublishObjectAsync<T>(string topic, T obj, MqttInfo.QoSLevel qos, bool retain);
        bool IsConnected();
    }
}