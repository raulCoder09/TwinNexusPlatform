using System.Collections;
using System.Threading.Tasks;
using _scripts.Protocols;
using UnityEngine;

namespace _scripts.controllers
{
    public class ArScaraTwinNexusController : MonoBehaviour
    {
        private MqttManager _mqttIoTCore;
        private void Awake()
        {

        }
        private void OnEnable()
        {
            StartCoroutine(StartConnectIotCore("ARSCARA"));
        }

        private void OnDisable()
        {
            _ = Unsubscribe("test");
            _ = Disconnect();
        }
        private async Task Connect()
        {
            _mqttIoTCore = new MqttManager(
                host: "aqloxhiemdroo-ats.iot.us-east-1.amazonaws.com",
                port: 8883,
                clientId: "TNPSGA52",
                cleanSession: true,
                useTls: true,
                pfxPath: @"Assets/StreamingAssets/resources/certificates/aws-iot-core.pfx",
                pfxPassword: "5859"
            );
            await _mqttIoTCore.Connect();
        }
        
        private async Task Subscribe(string topic)
        {
            await _mqttIoTCore.Subscribe(topic);
        }
        
        private async Task Publish(string topic, object message, int qos)
        {
            await _mqttIoTCore.Publish(topic, message,qos);
        }
        
        private async Task Unsubscribe(string topic)
        {
            await _mqttIoTCore.Unsubscribe(topic);
        }
        private async Task Disconnect()
        {
            await _mqttIoTCore.Disconnect();
        }
        private IEnumerator StartConnectIotCore(string topic) 
        {
            _ = Connect();
            yield return new WaitForSeconds(0.010f);
            if (_mqttIoTCore.IsConnected)
            {
                _ = Publish(topic,new { message= "Twin Nexus Platform - AWS IoT Core connection established"},1);
            }
            else
            {
                _mqttIoTCore.ExceptionMessage = "connection not established";
            }
        }
    }
}
