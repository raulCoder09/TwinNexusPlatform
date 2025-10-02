using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using _scripts.models.communicationProtocols;
using UnityEngine;
using UnityEngine.UIElements;

namespace _scripts.controllers
{
    public class ArScaraTwinNexusController : MonoBehaviour
    {
        private JogAndTeachController _jogAndTeachController;
        private MqttManager _mqttIoTCore;
        private string _deviceName;
        private void Awake()
        {
            _deviceName = "ARSCARA";
            
        }

        private void Start()
        {
            _jogAndTeachController = GameObject.FindWithTag("TwinNexusEnvironmentArScara").GetComponent<JogAndTeachController>();
            RegisterEvents();
        }

        
        private void RegisterEvents()
        {
            _jogAndTeachController.PlusJ1Button.RegisterCallback<PointerDownEvent>(_ =>
            {
                if (_mqttIoTCore.IsConnected)
                {
                    ButtonControl("+J1",true);
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
                    ButtonControl("+J1",false);
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
                    ButtonControl("-J1",true);
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
                    ButtonControl("-J1",false);
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
                    ButtonControl("+J2",true);
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
                    ButtonControl("+J2",false);
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
                    ButtonControl("-J2",true);
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
                    ButtonControl("-J2",false);
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
                    ButtonControl("+J3",true);
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
                    ButtonControl("+J3",false);
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
                    ButtonControl("-J3",true);
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
                    ButtonControl("-J3",false);
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
                    ButtonControl("+J4",true);
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
                    ButtonControl("+J4",false);
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
                    ButtonControl("-J4",true);
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
                    ButtonControl("-J4",false);
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
                    ButtonControl("+X",true);
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
                    ButtonControl("+X",false);
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
                    ButtonControl("-X",true);
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
                    ButtonControl("-X",false);
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
                    ButtonControl("+Y",true);
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
                    ButtonControl("+Y",false);
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
                    ButtonControl("-Y",true);
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
                    ButtonControl("-Y",false);
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
                    ButtonControl("+Z",true);
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
                    ButtonControl("+Z",false);
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
                    ButtonControl("-Z",true);
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
                    ButtonControl("-Z",false);
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
                    ButtonControl("+U",true);
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
                    ButtonControl("+U",false);
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
                    ButtonControl("-U",true);
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
                    ButtonControl("-U",false);
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
                    ButtonControl("Teach",true);
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
                    ButtonControl("Teach",false);
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
                    ButtonControl("Edit",true);
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
                    ButtonControl("Edit",false);
                }
                else
                {
                    print("not connected");
                }
            });
        }
        private void ButtonControl(string buttonName, bool state)
        {
            var data = new Dictionary<string, object>
            {
                { buttonName, state }
            };
    
            _ = Publish($"{_deviceName}", data, 1);
        }

        private void OnEnable()
        {
            StartCoroutine(StartConnectIotCore());
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
            _ = Publish($"{_deviceName}, Connection status",new { message= "Twin Nexus Platform - AWS IoT Core end connection"},1);
            await _mqttIoTCore.Disconnect();
            print("Disconnected");
        }
        private IEnumerator StartConnectIotCore() 
        {
            _ = Connect();
            yield return new WaitForSeconds(2f);
            if (_mqttIoTCore.IsConnected)
            {
                _ = Publish($"{_deviceName}",new { message= "Twin Nexus Platform - AWS IoT Core connection established"},1);
                print("connection established");
            }
            else
            {
                _mqttIoTCore.ExceptionMessage = "connection not established";
                print("connection not established");
            }
        }
    }
}

#region revisar esta logica


// private void MoveAxis(ClickEvent evt, string axis, string direction)
// {
//     switch (axis)
//     {
//         case "X":
//             _xPosition += direction == "+" ? _step : -_step;
//             _xPosition = Mathf.Clamp(_xPosition, -90f, 90f);
//             break;
//
//         case "Y":
//             _yPosition += direction == "+" ? _step : -_step;
//             _yPosition = Mathf.Clamp(_yPosition, -90f, 90f);
//             break;
//
//         case "Z":
//             _zPosition += direction == "+" ? _step : -_step;
//             _zPosition = Mathf.Clamp(_zPosition, -10f, 0f);
//             break;
//
//         case "U":
//             _uPosition += direction == "+" ? _step : -_step;
//             _uPosition = Mathf.Clamp(_uPosition, -180f, 180f);
//             break;
//     }
//             
// }
//
// private void MoveJoint(ClickEvent evt, int joint, string direction)
// {
//     switch (joint)
//     {
//         case 1:
//             _j1Rotation += direction == "+" ? _step : -_step;
//             break;
//
//         case 2:
//             _j2Rotation += direction == "+" ? _step : -_step;
//             break;
//
//         case 3:
//             _j3Prismatic += direction == "+" ? _step : -_step;
//             break;
//
//         case 4:
//             _j4Rotation += direction == "+" ? _step : -_step;
//             break;
//     }
// }

#endregion

