using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.CloudInfrastructureBrokerController
{
    /// <summary>
    /// Clases de configuración e información para el sistema CloudInfrastructureBroker
    /// Equivalente a WelcomeInfo pero para CloudInfrastructureBroker
    /// </summary>
    [System.Serializable]
    public class CloudInfrastructureBrokerInfo
    {
        /// <summary>
        /// Configuración de la UI del CloudInfrastructureBroker
        /// </summary>
        public class UIConfiguration
        {
            // Contenedores principales
            public VisualElement Body { get; set; }
            public VisualElement SubpanelsContainer { get; set; }
            public VisualElement Scrim { get; set; }
            
            // Paneles configurables
            public Dictionary<ICloudInfrastructureBrokerOps.PanelType, PanelData> Panels { get; set; } 
                = new Dictionary<ICloudInfrastructureBrokerOps.PanelType, PanelData>();
            
            // Referencias específicas del CloudInfrastructureBroker
            public VisualElement NavigationMenuPanel { get; set; }
            public VisualElement MainContentArea { get; set; }
            public VisualElement StatusBar { get; set; }
            
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
        /// Datos del usuario actual en el CloudInfrastructureBroker
        /// </summary>
        public class UserData
        {
            public string Username { get; set; }
            public string UserGroup { get; set; }
            public string UserRole { get; set; }
            public bool IsAuthenticated { get; set; }
            public DateTime LastLoginTime { get; set; }
            
            // Estados específicos del CloudInfrastructureBroker
            public string CurrentMode { get; set; } // "Operations", "Training", etc.
            public string SelectedModeUIName { get; set; }
        }

        /// <summary>
        /// Estado del CloudInfrastructureBroker
        /// </summary>
        public class CloudInfrastructureBrokerState
        {
            public bool IsInitialized { get; set; }
            public bool IsNavigationMenuOpen { get; set; }
            public ICloudInfrastructureBrokerOps.PanelType CurrentActivePanel { get; set; } = ICloudInfrastructureBrokerOps.PanelType.None;
            public string CurrentSection { get; set; } = "CloudInfrastructureBroker";
            
            // Estados de conexión IoT
            public ConnectionStatus LocalIoTStatus { get; set; } = ConnectionStatus.Disconnected;
            public ConnectionStatus VMIoTStatus { get; set; } = ConnectionStatus.Disconnected;
            public ConnectionStatus CloudIoTStatus { get; set; } = ConnectionStatus.Disconnected;
            
            // Eventos de estado
            public event Action<ICloudInfrastructureBrokerOps.PanelType> OnPanelChanged;
            public event Action<bool> OnNavigationMenuToggled;
            public event Action<ConnectionStatus, ConnectionStatus, ConnectionStatus> OnIoTStatusChanged;
        }

        /// <summary>
        /// Estados de conexión para IoT
        /// </summary>
        public enum ConnectionStatus
        {
            Disconnected,
            Connecting,
            Connected,
            Error,
            Unknown
        }

        /// <summary>
        /// Configuración de navegación del CloudInfrastructureBroker
        /// </summary>
        public class NavigationConfig
        {
            public List<NavigationItem> MenuItems { get; set; } = new List<NavigationItem>();
            public List<NavigationItem> QuickActions { get; set; } = new List<NavigationItem>();
            
            public class NavigationItem
            {
                public string Name { get; set; }
                public string DisplayName { get; set; }
                public string IconClass { get; set; }
                public bool IsEnabled { get; set; } = true;
                public bool RequiresPermission { get; set; } = false;
                public string RequiredRole { get; set; }
                public Action OnClick { get; set; }
            }
        }
    }
}