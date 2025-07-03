using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.SettingsController
{
    /// <summary>
    /// Clases de configuración e información para el sistema Settings
    /// Maneja configuraciones del sistema y datos de usuario
    /// </summary>
    [System.Serializable]
    public class SettingsInfo
    {
        /// <summary>
        /// Configuración de la UI del Settings
        /// </summary>
        public class UIConfiguration
        {
            // Contenedores principales
            public VisualElement Body { get; set; }
            public VisualElement SubpanelsContainer { get; set; }
            public VisualElement Scrim { get; set; }
            
            // Paneles configurables
            public Dictionary<ISettingsOps.PanelType, PanelData> Panels { get; set; } 
                = new Dictionary<ISettingsOps.PanelType, PanelData>();
            
            // Referencias específicas del Settings
            public VisualElement NavigationMenuPanel { get; set; }
            public VisualElement MainContentArea { get; set; }
            public VisualElement ConfigurationGrid { get; set; }
            
            // Botones de configuración
            public Dictionary<string, Button> ConfigurationButtons { get; set; } = new Dictionary<string, Button>();
            
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
        /// Información de configuraciones disponibles
        /// </summary>
        public class ConfigurationInfo
        {
            public ISettingsOps.ConfigurationType ConfigType { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string IconClass { get; set; }
            public bool IsAvailable { get; set; } = true;
            public bool RequiresPermission { get; set; } = false;
            public string RequiredRole { get; set; }
            
            /// <summary>
            /// Obtiene el título apropiado para la configuración
            /// </summary>
            public string GetDisplayTitle()
            {
                return ConfigType switch
                {
                    ISettingsOps.ConfigurationType.IoT => "IoT Configuration",
                    ISettingsOps.ConfigurationType.User => "User Settings", 
                    ISettingsOps.ConfigurationType.Cognito => "Cognito Settings",
                    ISettingsOps.ConfigurationType.System => "System Configuration",
                    ISettingsOps.ConfigurationType.Network => "Network Settings",
                    ISettingsOps.ConfigurationType.Database => "Database Settings",
                    ISettingsOps.ConfigurationType.Security => "Security Configuration",
                    ISettingsOps.ConfigurationType.Backup => "Backup Settings",
                    _ => "Configuration"
                };
            }
            
            /// <summary>
            /// Obtiene la descripción de la configuración
            /// </summary>
            public string GetDescription()
            {
                return ConfigType switch
                {
                    ISettingsOps.ConfigurationType.IoT => "Configure IoT devices and connections",
                    ISettingsOps.ConfigurationType.User => "Manage user preferences and profile",
                    ISettingsOps.ConfigurationType.Cognito => "Configure AWS Cognito authentication",
                    ISettingsOps.ConfigurationType.System => "System-wide configuration settings",
                    ISettingsOps.ConfigurationType.Network => "Network and connectivity settings",
                    ISettingsOps.ConfigurationType.Database => "Database connection and settings",
                    ISettingsOps.ConfigurationType.Security => "Security and access control settings",
                    ISettingsOps.ConfigurationType.Backup => "Backup and recovery configuration",
                    _ => "Configuration settings"
                };
            }
        }

        /// <summary>
        /// Estado del Settings Controller
        /// </summary>
        public class SettingsState
        {
            public bool IsInitialized { get; set; }
            public bool IsNavigationMenuOpen { get; set; }
            public ISettingsOps.PanelType CurrentActivePanel { get; set; } = ISettingsOps.PanelType.None;
            public string CurrentSection { get; set; } = "Settings";
            
            // Estado de configuraciones
            public Dictionary<ISettingsOps.ConfigurationType, bool> ConfigurationStates { get; set; } 
                = new Dictionary<ISettingsOps.ConfigurationType, bool>();
            public DateTime LastRefresh { get; set; } = DateTime.Now;
            
            // Eventos de estado
            public event Action<ISettingsOps.PanelType> OnPanelChanged;
            public event Action<bool> OnNavigationMenuToggled;
            public event Action<ISettingsOps.ConfigurationType> OnConfigurationOpened;
        }

        /// <summary>
        /// Configuración de configuraciones disponibles
        /// </summary>
        public class ConfigurationRegistry
        {
            public List<ConfigurationInfo> AvailableConfigurations { get; set; } = new List<ConfigurationInfo>();
            public Dictionary<ISettingsOps.ConfigurationType, ConfigurationInfo> ConfigurationMap { get; set; } 
                = new Dictionary<ISettingsOps.ConfigurationType, ConfigurationInfo>();
            
            /// <summary>
            /// Inicializa la configuración por defecto
            /// </summary>
            public static ConfigurationRegistry CreateDefault()
            {
                var registry = new ConfigurationRegistry();
                
                // IoT Configuration
                registry.AddConfiguration(new ConfigurationInfo
                {
                    ConfigType = ISettingsOps.ConfigurationType.IoT,
                    DisplayName = "IoT Settings",
                    Description = "Configure IoT devices and connections",
                    IconClass = "config-iot-icon",
                    IsAvailable = true,
                    RequiresPermission = false
                });
                
                // User Configuration
                registry.AddConfiguration(new ConfigurationInfo
                {
                    ConfigType = ISettingsOps.ConfigurationType.User,
                    DisplayName = "User Profile",
                    Description = "Manage user preferences and profile",
                    IconClass = "config-user-icon",
                    IsAvailable = true,
                    RequiresPermission = false
                });
                
                // Cognito Configuration
                registry.AddConfiguration(new ConfigurationInfo
                {
                    ConfigType = ISettingsOps.ConfigurationType.Cognito,
                    DisplayName = "Authentication",
                    Description = "Configure AWS Cognito authentication",
                    IconClass = "config-cognito-icon",
                    IsAvailable = true,
                    RequiresPermission = true,
                    RequiredRole = "admin"
                });
                
                // System Configuration
                registry.AddConfiguration(new ConfigurationInfo
                {
                    ConfigType = ISettingsOps.ConfigurationType.System,
                    DisplayName = "System Settings",
                    Description = "System-wide configuration settings",
                    IconClass = "config-system-icon",
                    IsAvailable = true,
                    RequiresPermission = true,
                    RequiredRole = "admin"
                });
                
                // Network Configuration
                registry.AddConfiguration(new ConfigurationInfo
                {
                    ConfigType = ISettingsOps.ConfigurationType.Network,
                    DisplayName = "Network Settings",
                    Description = "Network and connectivity settings",
                    IconClass = "config-network-icon",
                    IsAvailable = true,
                    RequiresPermission = true,
                    RequiredRole = "admin"
                });
                
                return registry;
            }
            
            /// <summary>
            /// Agrega una configuración al registro
            /// </summary>
            public void AddConfiguration(ConfigurationInfo config)
            {
                AvailableConfigurations.Add(config);
                ConfigurationMap[config.ConfigType] = config;
            }
            
            /// <summary>
            /// Obtiene configuraciones disponibles para un usuario específico
            /// </summary>
            public List<ConfigurationInfo> GetAvailableConfigurations(string userRole = null)
            {
                return AvailableConfigurations.FindAll(config => 
                    config.IsAvailable && 
                    (!config.RequiresPermission || 
                     string.IsNullOrEmpty(userRole) || 
                     userRole == config.RequiredRole || 
                     userRole == "admin"));
            }
        }
    }
}