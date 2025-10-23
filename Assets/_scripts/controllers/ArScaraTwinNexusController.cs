using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using _scripts.models.communicationProtocols;
using UnityEngine;
using UnityEngine.UIElements;
using System.Text.Json;
using System.Threading;
using JetBrains.Annotations;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace _scripts.controllers
{
    public class ArScaraTwinNexusController : MonoBehaviour
    {
        private JogAndTeachController _jogAndTeachController;
        private ControlPanelController  _controlPanelController;
        private MqttProtocol _mqttProtocol;
        private Dictionary<string, JsonElement> _data;
        private string _dataReceived;
        private string _lastProcessedData = string.Empty;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private string _lastProcessedMessage = string.Empty;
        
        [SerializeField] private string endpoint;
        [SerializeField] private int port;
        [SerializeField] private string clientId;
        [SerializeField] [CanBeNull] private string username;
        [SerializeField] [CanBeNull] private string password;
        [SerializeField] private bool cleanSession;
        [SerializeField] private bool useTls;
        [SerializeField] private string pfxPath;
        [SerializeField] private string pfxPassword;

        
        private void OnEnable()
        {
            StartCoroutine(ConnectToAws());
        }

        private void OnDestroy()
        {
            _mqttProtocol?.Dispose();
            _mqttProtocol?.Disconnect();
        }


        private void Start()
        {
            try
            {
                _jogAndTeachController = GameObject.FindWithTag("TwinNexusEnvironmentArScara").GetComponent<JogAndTeachController>();
                _controlPanelController= GameObject.FindWithTag("TwinNexusEnvironmentArScara").GetComponent<ControlPanelController>();
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private IEnumerator ConnectToAws()
        {
            _mqttProtocol=MqttProtocol.GetInstance(
                endpoint,
                port,
                clientId,
                username,
                password,
                cleanSession,
                useTls,
                _cancellationTokenSource.Token,
                pfxPath,
                pfxPassword
                );
            _mqttProtocol.Connect();
            yield return new WaitForSeconds(2f);
            if (_mqttProtocol.IsConnected)
            {
                _mqttProtocol.Topic = "StatusConnection";
                _mqttProtocol?.Subscribe();
                _mqttProtocol.Topic = "RobotManager";
                _mqttProtocol?.Subscribe();
                _mqttProtocol.Topic = "RobotManager/ControlPanel";
                _mqttProtocol?.Subscribe();
                _mqttProtocol.Topic = "RobotManager/JogAndTeach";
                _mqttProtocol?.Subscribe();
                _mqttProtocol.Topic = "RobotManager/Points";
                _mqttProtocol?.Subscribe();
                
                _mqttProtocol.Payload = new { message = "Connected" };
                _mqttProtocol?.SendData();
                _mqttProtocol.MessageReceived += (sender, e) =>
                {
                    if (e.Data == null) return;
                    foreach (var kvp in e.Data)
                    {
                        _lastProcessedMessage =
                            $"\n[{e.ReceivedAt.ToLocalTime():HH:mm:ss}] '{e.Topic}':{kvp.Key}: {kvp.Value}";
                    }
                };
            }
        }
    }
}