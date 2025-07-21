using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.EdgeLocalBrokerController
{
    /// <summary>
    /// Clases de configuración e información para el sistema EdgeLocalBroker
    /// Equivalente a WelcomeInfo pero para EdgeLocalBroker
    /// </summary>
    [System.Serializable]
    public class EdgeLocalBrokerInfo
    {
        /// <summary>
        /// Configuración de la UI del EdgeLocalBroker
        /// </summary>
        public class UIConfiguration
        {
            // Contenedores principales
            public VisualElement Body { get; set; }
            public VisualElement SubpanelsContainer { get; set; }
            public VisualElement Scrim { get; set; }
            
            // Paneles configurables
            public Dictionary<IEdgeLocalBrokerOps.PanelType, PanelData> Panels { get; set; } 
                = new Dictionary<IEdgeLocalBrokerOps.PanelType, PanelData>();
            
            // Referencias específicas del EdgeLocalBroker
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
        /// Datos del usuario actual en el EdgeLocalBroker
        /// </summary>
        public class UserData
        {
            public string Username { get; set; }
            public string UserGroup { get; set; }
            public string UserRole { get; set; }
            public bool IsAuthenticated { get; set; }
            public DateTime LastLoginTime { get; set; }
            
            // Estados específicos del EdgeLocalBroker
            public string CurrentMode { get; set; } // "Operations", "Training", etc.
            public string SelectedModeUIName { get; set; }
        }

        /// <summary>
        /// Estado del EdgeLocalBroker
        /// </summary>
        public class EdgeLocalBrokerState
        {
            public bool IsInitialized { get; set; }
            public bool IsNavigationMenuOpen { get; set; }
            public IEdgeLocalBrokerOps.PanelType CurrentActivePanel { get; set; } = IEdgeLocalBrokerOps.PanelType.None;
            public string CurrentSection { get; set; } = "EdgeLocalBroker";
            
            // Estados de conexión IoT
            public ConnectionStatus LocalIoTStatus { get; set; } = ConnectionStatus.Disconnected;
            public ConnectionStatus VMIoTStatus { get; set; } = ConnectionStatus.Disconnected;
            public ConnectionStatus CloudIoTStatus { get; set; } = ConnectionStatus.Disconnected;
            
            // Eventos de estado
            public event Action<IEdgeLocalBrokerOps.PanelType> OnPanelChanged;
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
        /// Configuración de navegación del EdgeLocalBroker
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