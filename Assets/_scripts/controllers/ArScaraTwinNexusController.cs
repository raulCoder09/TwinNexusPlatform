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
        private JogAndTeachController _jogAndTeachController;
        private ControlPanelController  _controlPanelController;
        private MqttProtocol _mqttProtocol;
        private Dictionary<string, JsonElement> _data;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private MqttMessageData _lastProcessedMessage;
        private Queue<MqttMessageData> _messageHistory = new Queue<MqttMessageData>(50);
        
        private string _date;
        private string _topic;
        private string _key;
        private string _value;

        
        [SerializeField] private string endpoint;
        [SerializeField] private int port;
        [SerializeField] private string clientId;
        [SerializeField] [CanBeNull] private string username;
        [SerializeField] [CanBeNull] private string password;
        [SerializeField] private bool cleanSession;
        [SerializeField] private bool useTls;
        [SerializeField] private string pfxPath;
        [SerializeField] private string pfxPassword;
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
        }

        private void Update()
        {


            if (_topic=="ARSCARA/RobotManager/ControlPanel")
            {
                if (_key=="EmergencyStop")
                {
                    _controlPanelController.EmergencyStopLabel.text = $"Emergency stop: {_value}";
                }
                if (_key=="Safeguard")
                {
                    _controlPanelController.SafeguardLabel.text = $"Safeguard: {_value}";
                }
                if (_key=="Motors")
                {
                    _controlPanelController.MotorsLabel.text = $"Motors: {_value}";
                }
                if (_key=="Power")
                {
                    _controlPanelController.PowerLabel.text = $"Power: {_value}";
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
                ("J3", _controlPanelController.J3Toggle),
                ("J4", _controlPanelController.J4Toggle),
            };

            foreach (var (itemName, toggle) in toggles)
                WireToggle("ARSCARA/RobotManager/ControlPanel", itemName, toggle);
        }

        private void WireJogAndTeach()
        {
            var buttons = new (string itemName, Button button)[]
            {
                ("PlusX", _jogAndTeachController.PlusXButton),
                ("MinusX", _jogAndTeachController.MinusXButton),
                ("PlusY", _jogAndTeachController.PlusYButton),
                ("MinusY", _jogAndTeachController.MinusYButton),
                ("PlusZ", _jogAndTeachController.PlusZButton),
                ("MinusZ", _jogAndTeachController.MinusZButton),
                ("PlusU", _jogAndTeachController.PlusUButton),
                ("MinusU", _jogAndTeachController.MinusUButton),
                
                ("PlusJ1", _jogAndTeachController.PlusJ1Button),
                ("MinusJ1", _jogAndTeachController.MinusJ1Button),
                ("PlusJ2", _jogAndTeachController.PlusJ2Button),
                ("MinusJ2", _jogAndTeachController.MinusJ2Button),
                ("PlusJ3", _jogAndTeachController.PlusJ3Button),
                ("MinusJ3", _jogAndTeachController.MinusJ3Button),
                ("PlusJ4", _jogAndTeachController.PlusJ4Button),
                ("MinusJ4", _jogAndTeachController.MinusJ4Button),
                
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
            }, TrickleDown.TrickleDown);

            Button.RegisterCallback<PointerUpEvent>(_ => 
            {
                SendData(topic,itemName,false.ToString());
            });
        }
        private void WireToggle(string topic, string itemName, Toggle toggle)
        {
            toggle.RegisterValueChangedCallback(evt =>
                SendData(topic,itemName,evt.newValue.ToString()));
        }
        private void WireRadio(string topic, string itemName, RadioButton radioButton)
        {
            radioButton.RegisterValueChangedCallback(evt =>
                SendData(topic,itemName,evt.newValue.ToString()));
        }
        
        private void WireDropdown(string topic, string itemName, DropdownField dropdownField)
        {
            dropdownField.RegisterValueChangedCallback(evt =>
                SendData(topic,itemName,evt.newValue.ToString()));
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
                    _lastProcessedMessage = new MqttMessageData
                    {
                        ReceivedAt = e.ReceivedAt.ToLocalTime(),
                        Topic = e.Topic,
                        Key = kvp.Key,
                        Value = kvp.Value
                    };
        
                    _messageHistory.Enqueue(_lastProcessedMessage);
                    if (_messageHistory.Count > 50)
                        _messageHistory.Dequeue();
                    
                    _date=$"{_lastProcessedMessage.ReceivedAt:HH:mm:ss}";
                    _topic=$"{_lastProcessedMessage.Topic}";
                    _key=$"{_lastProcessedMessage.Key}";
                    _value=$"{_lastProcessedMessage.Value}";
                }
            };
        }
    }
}
