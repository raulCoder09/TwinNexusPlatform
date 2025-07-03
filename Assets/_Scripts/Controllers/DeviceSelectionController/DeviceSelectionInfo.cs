using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.DeviceSelectionController
{
    /// <summary>
    /// Clases de configuración e información para el sistema Device Selection
    /// Maneja contextos, dispositivos y configuración UI
    /// </summary>
    [System.Serializable]
    public class DeviceSelectionInfo
    {
        /// <summary>
        /// Configuración de la UI del Device Selection
        /// </summary>
        public class UIConfiguration
        {
            // Contenedores principales
            public VisualElement Body { get; set; }
            public VisualElement SubpanelsContainer { get; set; }
            public VisualElement Scrim { get; set; }
            
            // Paneles configurables
            public Dictionary<IDeviceSelectionOps.PanelType, PanelData> Panels { get; set; } 
                = new Dictionary<IDeviceSelectionOps.PanelType, PanelData>();
            
            // Referencias específicas del Device Selection
            public VisualElement NavigationMenuPanel { get; set; }
            public VisualElement MainContentArea { get; set; }
            public VisualElement DeviceGridContainer { get; set; }
            public Label SelectedModeLabel { get; set; }
            public Label ContextTitleLabel { get; set; }
            
            // Botones de dispositivos
            public Dictionary<string, Button> DeviceButtons { get; set; } = new Dictionary<string, Button>();
            
            /// <summary>
            /// Configuración individual de cada panel
            /// </summary>
            [System.Serializable]
            public class PanelData
            {
                public VisualElement Panel { get; set; }
                public string ShowClass { get; set; }
                public string HideClass { get; set; }
                
                // Propiedades específicas para diferentes tipos de panel
                public bool IsModal { get; set; } = false;
                public bool RequiresScrim { get; set; } = true;
                public float AnimationDuration { get; set; } = 0.3f;
            }
        }

        /// <summary>
        /// Información del contexto de lanzamiento actual
        /// </summary>
        public class ContextData
        {
            public IDeviceSelectionOps.LaunchContext CurrentContext { get; set; } = IDeviceSelectionOps.LaunchContext.None;
            public IDeviceSelectionOps.LaunchContext PreviousContext { get; set; } = IDeviceSelectionOps.LaunchContext.None;
            public string SourceController { get; set; } = "Unknown";
            public DateTime ContextSetTime { get; set; } = DateTime.Now;
            
            // Configuración contextual
            public string ContextTitle { get; set; }
            public string ContextDescription { get; set; }
            public Color ContextColor { get; set; } = Color.white;
            public List<string> AllowedDevices { get; set; } = new List<string>();
            
            /// <summary>
            /// Obtiene el título apropiado para el contexto actual
            /// </summary>
            public string GetContextualTitle()
            {
                return CurrentContext switch
                {
                    IDeviceSelectionOps.LaunchContext.Training => "Devices available for learning",
                    IDeviceSelectionOps.LaunchContext.Operations => "Devices available for operate",
                    _ => "Select a device"
                };
            }
            
            /// <summary>
            /// Obtiene la descripción del contexto actual
            /// </summary>
            public string GetContextualDescription()
            {
                return CurrentContext switch
                {
                    IDeviceSelectionOps.LaunchContext.Training => "Choose a device to start your training session. You can practice and learn safely.",
                    IDeviceSelectionOps.LaunchContext.Operations => "Select a device to begin industrial operations. Ensure all safety protocols are followed.",
                    _ => "Please select a device from the available options below."
                };
            }
            
            /// <summary>
            /// Obtiene el color temático del contexto
            /// </summary>
            public Color GetContextualColor()
            {
                return CurrentContext switch
                {
                    IDeviceSelectionOps.LaunchContext.Training => new Color(0, 1, 1, 1),      // Cyan para entrenamiento
                    IDeviceSelectionOps.LaunchContext.Operations => new Color(1, 0.5f, 0, 1), // Naranja para operaciones
                    _ => Color.white
                };
            }
        }

        /// <summary>
        /// Información detallada de un dispositivo
        /// </summary>
        [System.Serializable]
        public class DeviceInfo
        {
            public string DeviceId { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public DeviceType Type { get; set; }
            public DeviceStatus Status { get; set; }
            public bool IsAvailable { get; set; } = true;
            
            // Configuración visual
            public string IconPath { get; set; }
            public Color StatusColor { get; set; } = Color.green;
            public string ButtonClass { get; set; } = "device-button";
            
            // Compatibilidad con contextos
            public List<IDeviceSelectionOps.LaunchContext> SupportedContexts { get; set; } 
                = new List<IDeviceSelectionOps.LaunchContext>();
            
            // Información técnica
            public Dictionary<string, object> TechnicalSpecs { get; set; } = new Dictionary<string, object>();
            public DateTime LastUsed { get; set; }
            public int UsageCount { get; set; }
            
            /// <summary>
            /// Verifica si el dispositivo es compatible con un contexto específico
            /// </summary>
            public bool IsCompatibleWith(IDeviceSelectionOps.LaunchContext context)
            {
                return SupportedContexts.Contains(context) || SupportedContexts.Count == 0;
            }
            
            /// <summary>
            /// Obtiene el texto de estado del dispositivo
            /// </summary>
            public string GetStatusText()
            {
                return Status switch
                {
                    DeviceStatus.Online => "Online",
                    DeviceStatus.Offline => "Offline", 
                    DeviceStatus.Busy => "Busy",
                    DeviceStatus.Maintenance => "Maintenance",
                    DeviceStatus.Error => "Error",
                    _ => "Unknown"
                };
            }
            
            /// <summary>
            /// Obtiene el color apropiado para el estado
            /// </summary>
            public Color GetStatusColor()
            {
                return Status switch
                {
                    DeviceStatus.Online => Color.green,
                    DeviceStatus.Offline => Color.red,
                    DeviceStatus.Busy => Color.yellow,
                    DeviceStatus.Maintenance => Color.blue,
                    DeviceStatus.Error => new Color(1, 0.5f, 0, 1), // Naranja
                    _ => Color.gray
                };
            }
        }

        /// <summary>
        /// Estado del Device Selection Controller
        /// </summary>
        public class DeviceSelectionState
        {
            public bool IsInitialized { get; set; }
            public bool IsNavigationMenuOpen { get; set; }
            public IDeviceSelectionOps.PanelType CurrentActivePanel { get; set; } = IDeviceSelectionOps.PanelType.None;
            public string SelectedDeviceId { get; set; }
            public string CurrentSection { get; set; } = "DeviceSelection";
            
            // Estado de dispositivos
            public Dictionary<string, DeviceStatus> DeviceStates { get; set; } = new Dictionary<string, DeviceStatus>();
            public DateTime LastRefresh { get; set; } = DateTime.Now;
            
            // Eventos de estado
            public event Action<IDeviceSelectionOps.PanelType> OnPanelChanged;
            public event Action<bool> OnNavigationMenuToggled;
            public event Action<string> OnDeviceSelected;
            public event Action<Dictionary<string, DeviceStatus>> OnDeviceStatesUpdated;
        }

        /// <summary>
        /// Configuración de dispositivos disponibles
        /// </summary>
        public class DeviceConfiguration
        {
            public List<DeviceInfo> AvailableDevices { get; set; } = new List<DeviceInfo>();
            public Dictionary<string, DeviceInfo> DeviceRegistry { get; set; } = new Dictionary<string, DeviceInfo>();
            
            /// <summary>
            /// Inicializa la configuración por defecto de dispositivos
            /// </summary>
            public static DeviceConfiguration CreateDefault()
            {
                var config = new DeviceConfiguration();
                
                // ARSCARA
                config.AddDevice(new DeviceInfo
                {
                    DeviceId = "ARSCARA",
                    DisplayName = "ARSCARA Robot",
                    Description = "Industrial robotic arm for precision tasks and automation",
                    Type = DeviceType.RoboticArm,
                    Status = DeviceStatus.Online,
                    IsAvailable = true,
                    IconPath = "Icons/arscara_icon",
                    SupportedContexts = { IDeviceSelectionOps.LaunchContext.Training, IDeviceSelectionOps.LaunchContext.Operations },
                    TechnicalSpecs = {
                        ["DOF"] = 6,
                        ["Payload"] = "3kg",
                        ["Reach"] = "850mm",
                        ["Repeatability"] = "±0.1mm"
                    }
                });
                
                // Robot Kit 1
                config.AddDevice(new DeviceInfo
                {
                    DeviceId = "RobotKit1",
                    DisplayName = "Robotics Kit 1",
                    Description = "Educational robotics platform for learning and experimentation",
                    Type = DeviceType.EducationalKit,
                    Status = DeviceStatus.Online,
                    IsAvailable = true,
                    IconPath = "Icons/robotkit_icon",
                    SupportedContexts = { IDeviceSelectionOps.LaunchContext.Training, IDeviceSelectionOps.LaunchContext.Operations },
                    TechnicalSpecs = {
                        ["Type"] = "Educational",
                        ["Programming"] = "Visual/Text",
                        ["Sensors"] = "Multiple",
                        ["Connectivity"] = "Wireless"
                    }
                });
                
                // Robot Kit 2
                config.AddDevice(new DeviceInfo
                {
                    DeviceId = "RobotKit2",
                    DisplayName = "Robotics Kit 2",
                    Description = "Advanced robotics platform with enhanced capabilities",
                    Type = DeviceType.EducationalKit,
                    Status = DeviceStatus.Offline,
                    IsAvailable = false,
                    IconPath = "Icons/robotkit2_icon",
                    SupportedContexts = { IDeviceSelectionOps.LaunchContext.Training },
                    TechnicalSpecs = {
                        ["Type"] = "Advanced Educational",
                        ["Programming"] = "Full SDK",
                        ["AI"] = "Computer Vision",
                        ["Connectivity"] = "5G/WiFi"
                    }
                });
                
                return config;
            }
            
            /// <summary>
            /// Agrega un dispositivo a la configuración
            /// </summary>
            public void AddDevice(DeviceInfo device)
            {
                AvailableDevices.Add(device);
                DeviceRegistry[device.DeviceId] = device;
            }
            
            /// <summary>
            /// Obtiene dispositivos compatibles con un contexto específico
            /// </summary>
            public List<DeviceInfo> GetDevicesForContext(IDeviceSelectionOps.LaunchContext context)
            {
                return AvailableDevices.FindAll(d => d.IsCompatibleWith(context) && d.IsAvailable);
            }
        }

        /// <summary>
        /// Tipos de dispositivos disponibles
        /// </summary>
        public enum DeviceType
        {
            Unknown,
            RoboticArm,
            EducationalKit,
            IndustrialMachine,
            Simulator,
            IoTDevice
        }

        /// <summary>
        /// Estados posibles de un dispositivo
        /// </summary>
        public enum DeviceStatus
        {
            Unknown,
            Online,      // Disponible y funcionando
            Offline,     // No disponible
            Busy,        // En uso por otro usuario
            Maintenance, // En mantenimiento
            Error        // Error de sistema
        }
    }
}