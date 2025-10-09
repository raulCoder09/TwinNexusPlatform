using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using _scripts.models.communicationProtocols;
using UnityEngine;
using UnityEngine.UIElements;

namespace _scripts.controllers
{
    public class ArScaraTwinNexusController : MonoBehaviour
    {
        private JogAndTeachController _jogAndTeachController;
        private ControlPanelController  _controlPanelController;
        private MqttManager _mqttIoTCore;
        private string _deviceName;
        
        private CancellationTokenSource _cts;
        
        private Dictionary<string, System.Text.Json.JsonElement> _data;
        
        private void Awake()
        {
            _deviceName = "ARSCARA";
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
        
        
        // analizar si este metodo se queda aqui o lo pasamos a mqtt manager
        private async Task ListenForMessages(CancellationToken ct)
        {
            try
            {
                await foreach (var msg in _mqttIoTCore.ReadAllAsync(ct))
                {
                    var payload = Encoding.UTF8.GetString(msg.Payload.ToArray()); 
                    print($"Topic: {msg.Topic} | Payload: {payload}");
                    _data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(payload);
                }
            }
            catch (OperationCanceledException)
            {
                print("Listener cancelado");
            }
            catch (Exception ex)
            {
                print($"Error: {ex.Message}");
            }
        }

        private void RegisterEvents()
        {
            #region ControlPanel

                _controlPanelController.MotorsOffButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("MotorsOff",true);
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
                        UiItemControl("MotorsOff",false);
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
                        UiItemControl("MotorsOn",true);
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
                        UiItemControl("MotorsOn",false);
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
                        UiItemControl("PowerLow",true);
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
                        UiItemControl("PowerLow",false);
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
                        UiItemControl("PowerHigh",true);
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
                        UiItemControl("PowerHigh",false);
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
                        UiItemControl("Home",true);
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
                        UiItemControl("Home",false);
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
                        UiItemControl("Reset",true);
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
                        UiItemControl("Reset",false);
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
                        UiItemControl("FreeAll",true);
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
                        UiItemControl("FreeAll",false);
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
                        UiItemControl("LockAll",true);
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
                        UiItemControl("LockAll",false);
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
                        UiItemControl("J1",evt.newValue);
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
                        UiItemControl("J2",evt.newValue);
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
                        UiItemControl("J3",evt.newValue);
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
                        UiItemControl("J4",evt.newValue);
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
                    UiItemControl("Mode",evt.newValue);
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
                    UiItemControl("Speed",evt.newValue);
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
                    UiItemControl("ContinuousMove",evt.newValue);
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
                    UiItemControl("LongMove",evt.newValue);
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
                    UiItemControl("MediumMove",evt.newValue);
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
                    UiItemControl("ShortMove",evt.newValue);
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
                    UiItemControl("Command",evt.newValue);
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
                    UiItemControl("Destination",evt.newValue);
                }
                else
                {
                    print("not connected");
                }
            });

                _jogAndTeachController.PlusJ1Button.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("+J1",true);
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
                        UiItemControl("+J1",false);
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
                        UiItemControl("-J1",true);
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
                        UiItemControl("-J1",false);
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
                        UiItemControl("+J2",true);
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
                        UiItemControl("+J2",false);
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
                        UiItemControl("-J2",true);
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
                        UiItemControl("-J2",false);
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
                        UiItemControl("+J3",true);
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
                        UiItemControl("+J3",false);
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
                        UiItemControl("-J3",true);
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
                        UiItemControl("-J3",false);
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
                        UiItemControl("+J4",true);
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
                        UiItemControl("+J4",false);
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
                        UiItemControl("-J4",true);
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
                        UiItemControl("-J4",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                }); 
                
                _jogAndTeachController.PlusXButton.RegisterCallback<PointerDownEvent>(_ =>
                {
                    if (_mqttIoTCore.IsConnected)
                    {
                        UiItemControl("+X",true);
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
                        UiItemControl("+X",false);
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
                        UiItemControl("-X",true);
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
                        UiItemControl("-X",false);
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
                        UiItemControl("+Y",true);
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
                        UiItemControl("+Y",false);
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
                        UiItemControl("-Y",true);
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
                        UiItemControl("-Y",false);
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
                        UiItemControl("+Z",true);
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
                        UiItemControl("+Z",false);
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
                        UiItemControl("-Z",true);
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
                        UiItemControl("-Z",false);
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
                        UiItemControl("+U",true);
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
                        UiItemControl("+U",false);
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
                        UiItemControl("-U",true);
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
                        UiItemControl("-U",false);
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
                        UiItemControl("Teach",true);
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
                        UiItemControl("Teach",false);
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
                        UiItemControl("Edit",true);
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
                        UiItemControl("Edit",false);
                    }
                    else
                    {
                        print("not connected");
                    }
                });

            #endregion
            
        }
        
        private void UiItemControl(string uiItemName, bool value)
        {
            var data = new Dictionary<string, object>
            {
                { uiItemName, value }
            };
    
            _ = Publish($"{_deviceName}", data, 1);
        }
        
        private void UiItemControl(string uiItemName, string value)
        {
            var data = new Dictionary<string, object>
            {
                { uiItemName, value }
            };
    
            _ = Publish($"{_deviceName}", data, 1);
        }
        
        private void OnEnable()
        {
            StartCoroutine(ActivateMQTT());
        }
        
        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
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
            _ = Publish($"{_deviceName}, Connection status",new { message= "Twin Nexus Platform - AWS IoT Core end connection"},1);
            await _mqttIoTCore.Disconnect();
            print("Disconnected");
        }
        
        private IEnumerator ActivateMQTT() 
        {
            _ = Connect();
            yield return new WaitForSeconds(2f);
            if (_mqttIoTCore.IsConnected)
            {
                _ = Publish($"{_deviceName}",new { message= "Twin Nexus Platform - AWS IoT Core connection established"},1);
                print("connection established");
                _ = Subscribe("test");
                _ = Subscribe("ARSCARA");
                _cts = new CancellationTokenSource();
                _ = ListenForMessages(_cts.Token);
            }
            else
            {
                _mqttIoTCore.ExceptionMessage = "connection not established";
                print("connection not established");
            }
        }
    }
}