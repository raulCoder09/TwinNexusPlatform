using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using _scripts.models.communicationProtocols;
using UnityEngine;
using UnityEngine.UIElements;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace _scripts.controllers
{
    public class ArScaraTwinNexusController : MonoBehaviour
    {
        private JogAndTeachController _jogAndTeachController;
        private ControlPanelController  _controlPanelController;
        private MqttManager _mqttIoTCore;
        private Dictionary<string, System.Text.Json.JsonElement> _data;
        private string _dataReceived;
        private string _lastProcessedData = string.Empty;

        private void Awake()
        {
        }
        private void Start()
        {
            try
            {
                _jogAndTeachController = GameObject.FindWithTag("TwinNexusEnvironmentArScara").GetComponent<JogAndTeachController>();
                _controlPanelController= GameObject.FindWithTag("TwinNexusEnvironmentArScara").GetComponent<ControlPanelController>();
                RegisterEvents();
            }
            catch (Exception e)
            {
                throw;
            }
        }
        
        private void Update()
        {
            if (_mqttIoTCore != null && 
                !string.IsNullOrEmpty(_mqttIoTCore.DataLoading) && 
                _mqttIoTCore.DataLoading != _lastProcessedData)
            {
                ProcessMqttData();
                _lastProcessedData = _mqttIoTCore.DataLoading;
            }
        }
        
        private void ProcessMqttData()
        {
            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(_mqttIoTCore.DataLoading);

                foreach (var item in data)
                {
                    if (_mqttIoTCore.CurrentTopic=="TwinNexus/ARSCARA/ControlPanel")
                    {
                        if (data.ContainsKey("EmergencyStop") && data.ContainsKey("Safeguard") && 
                            data.ContainsKey("Motors") && data.ContainsKey("Power"))
                        {
                            _controlPanelController.EmergencyStopLabel.text=$"Emergency stop: {data["EmergencyStop"].GetString()}";
                            _controlPanelController.SafeguardLabel.text=$"Safeguard: {data["Safeguard"].GetString()}";
                            _controlPanelController.MotorsLabel.text=$"Motors: {data["Motors"].GetString()}";
                            _controlPanelController.PowerLabel.text=$"Power: {data["Power"].GetString()}";
                        }
                    }

                    if (_mqttIoTCore.CurrentTopic == "TwinNexus/ARSCARA/JogAndTeach/World")
                    {
                        if (data.ContainsKey("XData") && data.ContainsKey("YData") &&
                            data.ContainsKey("ZData") && data.ContainsKey("UData")) 
                        { 
                            _jogAndTeachController.XLabel.text =$"X: {data["XData"].GetDouble().ToString()} mm";
                            _jogAndTeachController.YLabel.text =$"Y: {data["YData"].GetDouble().ToString()} mm";
                            _jogAndTeachController.ZLabel.text =$"Z: {data["ZData"].GetDouble().ToString()} mm";
                            _jogAndTeachController.ULabel.text =$"U: {data["UData"].GetDouble().ToString()} deg";
                        }
                    }
                    if (_mqttIoTCore.CurrentTopic == "TwinNexus/ARSCARA/JogAndTeach/Joint")
                    {
                        if (data.ContainsKey("J1Data") && data.ContainsKey("J2Data") &&
                            data.ContainsKey("J3Data") && data.ContainsKey("J4Data")) 
                        { 
                            _jogAndTeachController.J1Label.text =$"J1: {data["J1Data"].GetDouble().ToString()} deg";
                            _jogAndTeachController.J2Label.text =$"J2: {data["J2Data"].GetDouble().ToString()} deg";
                            _jogAndTeachController.J3Label.text =$"J3: {data["J3Data"].GetDouble().ToString()} deg";
                            _jogAndTeachController.J4Label.text =$"J4: {data["J4Data"].GetDouble().ToString()} deg";
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Error deserializando JSON: {ex.Message}");
                Debug.LogError($"JSON recibido: {_mqttIoTCore.DataLoading}");
            }
        }


        private void RegisterEvents()
        {
            #region ControlPanel

                _controlPanelController.MotorsOffButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","MotorsOff",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
            
                _controlPanelController.MotorsOffButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","MotorsOff",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                
                _controlPanelController.MotorsOnButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","MotorsOn",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
            
                _controlPanelController.MotorsOnButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","MotorsOn",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                
                _controlPanelController.PowerLowButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","PowerLow",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
            
                _controlPanelController.PowerLowButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","PowerLow",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                
                _controlPanelController.PowerHighButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","PowerHigh",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
            
                _controlPanelController.PowerHighButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","PowerHigh",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });

                _controlPanelController.HomeButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","Home",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
            
                _controlPanelController.HomeButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","Home",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                
                _controlPanelController.ResetButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","Reset",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
            
                _controlPanelController.ResetButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","Reset",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                _controlPanelController.FreeAllButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","FreeAll",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
            
                _controlPanelController.FreeAllButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","FreeAll",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                _controlPanelController.LockAllButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","LockAll",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
            
                _controlPanelController.LockAllButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","LockAll",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });

                _controlPanelController.J1Toggle.RegisterValueChangedCallback(evt =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","J1",evt.newValue);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                
                _controlPanelController.J2Toggle.RegisterValueChangedCallback(evt =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","J2",evt.newValue);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                
                _controlPanelController.J3Toggle.RegisterValueChangedCallback(evt =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","J3",evt.newValue);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                
                _controlPanelController.J4Toggle.RegisterValueChangedCallback(evt =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("ControlPanel","J4",evt.newValue);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
            #endregion

            #region JogAndTeach

            _jogAndTeachController.Mode.RegisterValueChangedCallback(evt =>
            {
                if (_mqttIoTCore.IsConnected)
                {
                    UiItemControl("JogAndTeach","Mode",evt.newValue);
                }
                else
                {
                    print("not connected");
                }
            });
            _jogAndTeachController.Speed.RegisterValueChangedCallback(evt =>
            {
                if (_mqttIoTCore.IsConnected)
                {
                    UiItemControl("JogAndTeach","Speed",evt.newValue);
                }
                else
                {
                    print("not connected");
                }
            });
            _jogAndTeachController.ContinuousMove.RegisterValueChangedCallback(evt =>
            {
                if (_mqttIoTCore.IsConnected)
                {
                    UiItemControl("JogAndTeach","ContinuousMove",evt.newValue);
                }
                else
                {
                    print("not connected");
                }
            });
            _jogAndTeachController.LongMove.RegisterValueChangedCallback(evt =>
            {
                if (_mqttIoTCore.IsConnected)
                {
                    UiItemControl("JogAndTeach","LongMove",evt.newValue);
                }
                else
                {
                    print("not connected");
                }
            });
            _jogAndTeachController.MediumMove.RegisterValueChangedCallback(evt =>
            {
                if (_mqttIoTCore.IsConnected)
                {
                    UiItemControl("JogAndTeach","MediumMove",evt.newValue);
                }
                else
                {
                    print("not connected");
                }
            });
            _jogAndTeachController.ShortMove.RegisterValueChangedCallback(evt =>
            {
                if (_mqttIoTCore.IsConnected)
                {
                    UiItemControl("JogAndTeach","ShortMove",evt.newValue);
                }
                else
                {
                    print("not connected");
                }
            });
            _jogAndTeachController.Command.RegisterValueChangedCallback(evt =>
            {
                if (_mqttIoTCore.IsConnected)
                {
                    UiItemControl("JogAndTeach","Command",evt.newValue);
                }
                else
                {
                    print("not connected");
                }
            });
            _jogAndTeachController.Destination.RegisterValueChangedCallback(evt =>
            {
                if (_mqttIoTCore.IsConnected)
                {
                    UiItemControl("JogAndTeach","Destination",evt.newValue);
                }
                else
                {
                    print("not connected");
                }
            });

            #endregion
            
            
            #region JogAndTeachJoint
                _jogAndTeachController.PlusJ1Button.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","+J1",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
        
                _jogAndTeachController.PlusJ1Button.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","+J1",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });

                _jogAndTeachController.MinusJ1Button.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","-J1",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);

                _jogAndTeachController.MinusJ1Button.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","-J1",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                }); 
                    
                _jogAndTeachController.PlusJ2Button.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","+J2",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
        
                _jogAndTeachController.PlusJ2Button.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","+J2",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                
                _jogAndTeachController.MinusJ2Button.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","-J2",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
        
                _jogAndTeachController.MinusJ2Button.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","-J2",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                }); 
                
                _jogAndTeachController.PlusJ3Button.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","+J3",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
        
                _jogAndTeachController.PlusJ3Button.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","+J3",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                
                _jogAndTeachController.MinusJ3Button.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","-J3",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
        
                _jogAndTeachController.MinusJ3Button.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","-J3",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                }); 
                
                _jogAndTeachController.PlusJ4Button.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","+J4",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
        
                _jogAndTeachController.PlusJ4Button.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","+J4",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                
                _jogAndTeachController.MinusJ4Button.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","-J4",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
        
                _jogAndTeachController.MinusJ4Button.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Joint","-J4",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                }); 
                
                #endregion

            #region JogAndTeachWorld
            
                
                _jogAndTeachController.PlusXButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","+X",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
        
                _jogAndTeachController.PlusXButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","+X",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });

                _jogAndTeachController.MinusXButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","-X",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);

                _jogAndTeachController.MinusXButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","-X",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                }); 
                
                _jogAndTeachController.PlusYButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","+Y",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
        
                _jogAndTeachController.PlusYButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","+Y",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });

                _jogAndTeachController.MinusYButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","-Y",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);

                _jogAndTeachController.MinusYButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","-Y",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                }); 
                
                _jogAndTeachController.PlusZButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","+Z",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
        
                _jogAndTeachController.PlusZButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","+Z",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });

                _jogAndTeachController.MinusZButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","-Z",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);

                _jogAndTeachController.MinusZButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","-Z",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                }); 
                
                _jogAndTeachController.PlusUButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","+U",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);
        
                _jogAndTeachController.PlusUButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","+U",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });

                _jogAndTeachController.MinusUButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","-U",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);

                _jogAndTeachController.MinusUButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","World","-U",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                
                _jogAndTeachController.TeachButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Teach",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);

                _jogAndTeachController.TeachButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Teach",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });
                
                _jogAndTeachController.EditButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Edit",true);
                    }
                    else
                    {
                        print("not connected");
                    }
                }, TrickleDown.TrickleDown);

                _jogAndTeachController.EditButton.RegisterCallback<PointerUpEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("JogAndTeach","Edit",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });

            #endregion
            
        }
        private void UiItemControl(string userInterface,string uiItemName, bool value)
        {
            var data = new Dictionary<string, object>
            {
                { uiItemName, value }
            };
    
            _ = Publish($"TwinNexus/ARSCARA/{userInterface}", data, 1);
        }
        
        private void UiItemControl(string userInterface,string uiItemName, string value)
        {
            var data = new Dictionary<string, object>
            {
                { uiItemName, value }
            };
    
            _ = Publish($"TwinNexus/ARSCARA/{userInterface}", data, 1);
        }
        
        private void UiItemControl(string userInterface,string motionMode,string uiItemName, bool value)
        {
            var data = new Dictionary<string, object>
            {
                { uiItemName, value }
            };
    
            _ = Publish($"TwinNexus/ARSCARA/{userInterface}/{motionMode}", data, 1);
        }
        private void UiItemControl(string userInterface,string motionMode,string uiItemName, string value)
        {
            var data = new Dictionary<string, object>
            {
                { uiItemName, value }
            };
    
            _ = Publish($"TwinNexus/ARSCARA/{userInterface}/{motionMode}", data, 1);
        }
        private void OnEnable()
        {
            StartCoroutine(ActivateMqtt());
        }
        private void OnDisable()
        {
            _ = Unsubscribe("Connection status");
            _ = Unsubscribe("Listen status");
            _ = Unsubscribe("TwinNexus/ARSCARA/ControlPanel");
            _ = Unsubscribe("TwinNexus/ARSCARA/JogAndTeach");
            _ = Unsubscribe("TwinNexus/ARSCARA/JogAndTeach/World");
            _ = Unsubscribe("TwinNexus/ARSCARA/JogAndTeach/Joint");
            _mqttIoTCore.DisableMqtt();
        }
        private async Task Connect()
        {
            _mqttIoTCore = new MqttManager(
                endpoint: "aqloxhiemdroo-ats.iot.us-east-1.amazonaws.com",
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
        private IEnumerator ActivateMqtt() 
        {
            _ = Connect();
            yield return new WaitForSeconds(2f);
            if (_mqttIoTCore.IsConnected)
            {
                _ = Subscribe("Connection status");
                _ = Subscribe("Listen status");
                _ = Subscribe("TwinNexus/ARSCARA/ControlPanel");
                _ = Subscribe("TwinNexus/ARSCARA/JogAndTeach");
                _ = Subscribe("TwinNexus/ARSCARA/JogAndTeach/World");
                _ = Subscribe("TwinNexus/ARSCARA/JogAndTeach/Joint");
            }
            else
            {
                _mqttIoTCore.ExceptionMessage = "connection not established";
            }
        }
    }
}