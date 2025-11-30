using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
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
    public class MqttMessageData
    {
        public DateTime ReceivedAt { get; set; }
        public string Topic { get; set; }
        public string Key { get; set; }
        public JsonElement Value { get; set; }
    }
    
    public class ArScaraTwinNexusController : MonoBehaviour
    {
        private AwsController _awsController;
        private JogAndTeachController _jogAndTeachController;
        private ControlPanelController  _controlPanelController;
        private MqttProtocol _mqttProtocol;
        private Dictionary<string, JsonElement> _data;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Queue<MqttMessageData> _messageHistory = new Queue<MqttMessageData>(50);
        private readonly Queue<MqttMessageData> _messageQueue = new Queue<MqttMessageData>();
        private readonly object _queueLock = new object();
        
        [SerializeField] private string endpoint;
        [SerializeField] private int port;
        [SerializeField] private string clientId;
        [SerializeField] [CanBeNull] private string username;
        [SerializeField] [CanBeNull] private string password;
        [SerializeField] private bool cleanSession;
        [SerializeField] private bool useTls;
        [SerializeField] private string pfxPath;
        [SerializeField] private string pfxPassword;

        internal MqttProtocol mqttProtocol
        {
            get => _mqttProtocol;
            set => _mqttProtocol = value;
        }

        private void Awake()
        {
            try
            {
                _controlPanelController= GameObject.FindWithTag("TwinNexusEnvironmentArScara").GetComponent<ControlPanelController>();
                _jogAndTeachController = GameObject.FindWithTag("TwinNexusEnvironmentArScara").GetComponent<JogAndTeachController>();
            }
            catch (Exception e)
            {
                throw;
            }
        }
        
        private void OnEnable()
        {
            StartCoroutine(ConnectToAws());
        }
        
        private void Start()
        {
            WireControlPanel();
            WireJogAndTeach();
            _awsController= GameObject.FindWithTag("AwsController").GetComponent<AwsController>();
        }

        private void Update()
        {
            lock (_queueLock)
            {
                while (_messageQueue.Count > 0)
                {
                    var message = _messageQueue.Dequeue();
                    ProcessMessage(message);
                }
            }
        }

        private void OnDisable()
        {
            StopCoroutine(ConnectToAws());
        }
        
        private void OnDestroy()
        {
            _mqttProtocol?.Dispose();
            _mqttProtocol?.Disconnect();
        }
        
        private void ProcessMessage(MqttMessageData message)
        {
            if (message.Topic == "ARSCARA/RobotManager/ControlPanel")
            {
                var valueStr = message.Value.ToString();
                switch (message.Key)
                {
                    case "EmergencyStop":
                        _controlPanelController.EmergencyStopLabel.text = $"Emergency stop: {valueStr}";
                        _awsController.SendLogAsync(message.Topic,"EmergencyStop",valueStr);
                        break;
                    case "Safeguard":
                        _controlPanelController.SafeguardLabel.text = $"Safeguard: {valueStr}";
                        _awsController.SendLogAsync(message.Topic,"Safeguard",valueStr);
                        break;
                    case "Motors":
                        _controlPanelController.MotorsLabel.text = $"Motors: {valueStr}";
                        _awsController.SendLogAsync(message.Topic,"Motors",valueStr);
                        break;
                    case "Power":
                        _controlPanelController.PowerLabel.text = $"Power: {valueStr}";
                        _awsController.SendLogAsync(message.Topic,"Power",valueStr);
                        break;
                }
            }
            
            if (message.Topic == "ARSCARA/RobotManager/JogAndTeach")
            {
                var valueStr = message.Value.ToString();
                
                    switch (message.Key)
                    {
                        case "X":
                            _jogAndTeachController.XLabel.text = $"X: {valueStr} mm";
                            _awsController.SendLogAsync(message.Topic,"X",$"{valueStr} mm");
                            break;
                        case "Y":
                            _jogAndTeachController.YLabel.text = $"Y: {valueStr} mm";
                            _awsController.SendLogAsync(message.Topic,"Y",$"{valueStr} mm");
                            break;
                        case "Z":
                            _jogAndTeachController.ZLabel.text = $"Z: {valueStr} mm";
                            _awsController.SendLogAsync(message.Topic,"Z",$"{valueStr} mm");
                            break;
                        case "U":
                            _jogAndTeachController.ULabel.text = $"U: {valueStr} deg";
                            _awsController.SendLogAsync(message.Topic,"U",$"{valueStr} deg");
                            break;
                        case "J1":
                            _jogAndTeachController.J1Label.text = $"J1: {valueStr} deg";
                            _awsController.SendLogAsync(message.Topic,"J1",$"{valueStr} deg");
                            break;
                        case "J2":
                            _jogAndTeachController.J2Label.text = $"J2: {valueStr} deg";
                            _awsController.SendLogAsync(message.Topic,"J2",$"{valueStr} deg");
                            break;
                        case "J3":
                            _jogAndTeachController.J3Label.text = $"J3: {valueStr} deg";
                            _awsController.SendLogAsync(message.Topic,"J3",$"{valueStr} deg");
                            break;
                        case "J4":
                            _jogAndTeachController.J4Label.text = $"J4: {valueStr} deg";
                            _awsController.SendLogAsync(message.Topic,"J4",$"{valueStr} deg");
                            break;
                    }
            }
        }
        
        private void WireControlPanel()
        {
            var buttons = new (string itemName, Button button)[]
            {
                ("MotorsOff", _controlPanelController.MotorsOffButton),
                ("MotorsOn", _controlPanelController.MotorsOnButton),
                ("PowerLow", _controlPanelController.PowerLowButton),
                ("PowerHigh", _controlPanelController.PowerHighButton),
                ("Home", _controlPanelController.HomeButton),
                ("Reset", _controlPanelController.ResetButton),
                ("FreeAll", _controlPanelController.FreeAllButton),
                ("LockAll", _controlPanelController.LockAllButton),
            };

            foreach (var (itemName, button) in buttons)
                WireButton("ARSCARA/RobotManager/ControlPanel",itemName, button);
            
            
            var toggles = new (string itemName, Toggle toggle)[]
            {
                ("J1", _controlPanelController.J1Toggle),
                ("J2", _controlPanelController.J2Toggle),
            };

            foreach (var (itemName, toggle) in toggles)
                WireToggle("ARSCARA/RobotManager/ControlPanel", itemName, toggle);
        }

        private void WireJogAndTeach()
        {
            var buttons = new (string itemName, Button button)[]
            {
                ("PlusX", _jogAndTeachController.PlusQ1Button),
                ("MinusX", _jogAndTeachController.MinusQ1Button),
                ("PlusY", _jogAndTeachController.PlusQ2Button),
                ("MinusY", _jogAndTeachController.MinusQ2Button),
                
                ("PlusJ1", _jogAndTeachController.PlusJ1Button),
                ("MinusJ1", _jogAndTeachController.MinusJ1Button),
                ("PlusJ2", _jogAndTeachController.PlusJ2Button),
                ("MinusJ2", _jogAndTeachController.MinusJ2Button),
                
                ("Teach", _jogAndTeachController.TeachButton),
                ("Edit", _jogAndTeachController.EditButton)
                
            };
            foreach (var (itemName, button) in buttons)
                WireButton("ARSCARA/RobotManager/JogAndTeach",itemName, button);


            var radios = new (string itemName, RadioButton radioButton)[]
            {
                ("Continuous",_jogAndTeachController.ContinuousMove),
                ("Long",_jogAndTeachController.LongMove),
                ("Medium",_jogAndTeachController.MediumMove),
                ("Short",_jogAndTeachController.ShortMove),
            };
            foreach (var (itemName, radioButton) in radios)
                WireRadio("ARSCARA/RobotManager/JogAndTeach",itemName, radioButton);


            var dropdowns = new (string itemName, DropdownField dropdownField)[]
            {
                ("Mode", _jogAndTeachController.Mode),
                ("Speed", _jogAndTeachController.Speed),
                ("Command", _jogAndTeachController.Command),
                ("Destination", _jogAndTeachController.Destination)
            };
            
            foreach (var (itemName, dropdownField) in dropdowns)
                WireDropdown("ARSCARA/RobotManager/JogAndTeach",itemName, dropdownField);
            
        }

        private void WireButton(string topic,string itemName, Button Button)
        {
            Button.RegisterCallback<PointerDownEvent>(_ =>
            {
                SendData(topic,itemName,true.ToString());
                _awsController.SendLogAsync(topic, itemName, true);
            }, TrickleDown.TrickleDown);

            Button.RegisterCallback<PointerUpEvent>(_ => 
            {
                SendData(topic,itemName,false.ToString());
                _awsController.SendLogAsync(topic, itemName, false);
            });
        }
        
        private void WireToggle(string topic, string itemName, Toggle toggle)
        {
            toggle.RegisterValueChangedCallback(evt =>
            {
                SendData(topic, itemName, evt.newValue.ToString());
                _awsController.SendLogAsync(topic, itemName, evt.newValue.ToString());
            });
        }
        
        private void WireRadio(string topic, string itemName, RadioButton radioButton)
        {
            radioButton.RegisterValueChangedCallback(evt =>
            {
                SendData(topic, itemName, evt.newValue.ToString());
                _awsController.SendLogAsync(topic, itemName, evt.newValue.ToString());
            });
        }
        
        private void WireDropdown(string topic, string itemName, DropdownField dropdownField)
        {
            dropdownField.RegisterValueChangedCallback(evt =>
            {
                SendData(topic, itemName, evt.newValue.ToString());
                _awsController.SendLogAsync(topic, itemName, evt.newValue.ToString());
            });
        }
        
        private void SendData(string topic, string fieldName, string payload)
        {
            _mqttProtocol.Topic = topic;
            _mqttProtocol?.Subscribe();
            
            var data = new ExpandoObject();
            var dataDict = (IDictionary<string, object>)data;
            dataDict[fieldName] = payload;
            
            _mqttProtocol.Payload = data;
            _mqttProtocol?.SendData();
        }
        
        private void Subscribe(string topic)
        {
            _mqttProtocol.Topic = topic;
            _mqttProtocol?.Subscribe();
            _mqttProtocol.Payload = new { message = $"subscribed to {topic}" };
            _mqttProtocol?.SendData();
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
            if (!_mqttProtocol.IsConnected) yield break;
            SendData("ARSCARA/StatusConnection","Status" ,"Connected");
            Subscribe("ARSCARA/RobotManager");
            Subscribe("ARSCARA/RobotManager/ControlPanel");
            Subscribe("ARSCARA/RobotManager/JogAndTeach");
            Subscribe("ARSCARA/RobotManager/Points");
            
            _mqttProtocol.MessageReceived += (sender, e) =>
            {
                if (e.Data == null) return;
    
                foreach (var kvp in e.Data)
                {
                    var messageData = new MqttMessageData
                    {
                        ReceivedAt = e.ReceivedAt.ToLocalTime(),
                        Topic = e.Topic,
                        Key = kvp.Key,
                        Value = kvp.Value
                    };
        
                    _messageHistory.Enqueue(messageData);
                    if (_messageHistory.Count > 50)
                        _messageHistory.Dequeue();
                    
                    lock (_queueLock)
                    {
                        _messageQueue.Enqueue(messageData);
                    }
                }
            };
        }
    }
}