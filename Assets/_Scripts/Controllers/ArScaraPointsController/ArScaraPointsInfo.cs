using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.ArScaraPointsController
{
    /// <summary>
    /// Clases de configuración e información para el sistema AWS Settings
    /// Equivalente a DashboardInfo pero para configuraciones AWS
    /// </summary>
    [System.Serializable]
    public class ArScaraPointsInfo
    {
        /// <summary>
        /// Configuración de la UI de AWS Settings
        /// </summary>
        public class UIConfiguration
        {
            // Contenedores principales
            public VisualElement Body { get; set; }
            public VisualElement SubpanelsContainer { get; set; }
            public VisualElement Scrim { get; set; }
            
            // Paneles configurables (futuros)
            public Dictionary<IArScaraPointsOps.PanelType, PanelData> Panels { get; set; } 
                = new Dictionary<IArScaraPointsOps.PanelType, PanelData>();
            
            // Referencias específicas de AWS Settings
            public VisualElement NavigationMenuPanel { get; set; }
            public VisualElement MainContentArea { get; set; }
            public VisualElement HeaderArea { get; set; }
            public VisualElement FooterArea { get; set; }
            
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
        /// Configuración de AWS específica
        /// </summary>
        public class AwsConfiguration
        {
            public string Region { get; set; } = "us-east-1";
            public string AccessKeyId { get; set; }
            public string SecretAccessKey { get; set; }
            public string SessionToken { get; set; }
            
            // Estados de servicios AWS
            public Dictionary<string, bool> ServiceStates { get; set; } = new Dictionary<string, bool>();
            
            // Configuraciones específicas por servicio
            public Dictionary<string, Dictionary<string, object>> ServiceConfigs { get; set; } 
                = new Dictionary<string, Dictionary<string, object>>();
            
            // Resultados de pruebas de conexión
            public Dictionary<string, ConnectionTestResult> TestResults { get; set; } 
                = new Dictionary<string, ConnectionTestResult>();
        }

        /// <summary>
        /// Resultado de prueba de conexión
        /// </summary>
        public class ConnectionTestResult
        {
            public bool IsSuccessful { get; set; }
            public string Message { get; set; }
            public DateTime TestTime { get; set; }
            public TimeSpan ResponseTime { get; set; }
            public string ServiceName { get; set; }
        }

        /// <summary>
        /// Estado de AWS Settings
        /// </summary>
        public class ArScaraPointsState
        {
            public bool IsInitialized { get; set; }
            public bool IsNavigationMenuOpen { get; set; }
            public IArScaraPointsOps.PanelType CurrentActivePanel { get; set; } = IArScaraPointsOps.PanelType.None;
            public string CurrentSection { get; set; } = "ArScaraPoints";
            
            // Estados de configuración
            public bool HasValidCredentials { get; set; }
            public bool IsTestingConnection { get; set; }
            public bool HasUnsavedChanges { get; set; }
            
            // Eventos de estado
            public event Action<IArScaraPointsOps.PanelType> OnPanelChanged;
            public event Action<bool> OnNavigationMenuToggled;
            public event Action<bool> OnCredentialsValidityChanged;
            public event Action<string, ConnectionTestResult> OnConnectionTestCompleted;
            
            // Métodos públicos para invocar eventos desde el Orchestrator
            public void TriggerPanelChanged(IArScaraPointsOps.PanelType panelType)
            {
                OnPanelChanged?.Invoke(panelType);
            }
            
            public void TriggerNavigationMenuToggled(bool isOpen)
            {
                OnNavigationMenuToggled?.Invoke(isOpen);
            }
            
            public void TriggerCredentialsValidityChanged(bool isValid)
            {
                OnCredentialsValidityChanged?.Invoke(isValid);
            }
            
            public void TriggerConnectionTestCompleted(string serviceName, ConnectionTestResult result)
            {
                OnConnectionTestCompleted?.Invoke(serviceName, result);
            }
        }

        /// <summary>
        /// Configuración de navegación de AWS Settings
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
                public bool RequiresValidCredentials { get; set; } = false;
                public Action OnClick { get; set; }
            }
        }

        /// <summary>
        /// Información de servicios AWS disponibles
        /// </summary>
        public class AwsServicesInfo
        {
            public static readonly Dictionary<string, ServiceInfo> Services = new Dictionary<string, ServiceInfo>
            {
                ["Cognito"] = new ServiceInfo { DisplayName = "Amazon Cognito", IsRequired = true },
                ["S3"] = new ServiceInfo { DisplayName = "Amazon S3", IsRequired = false },
                ["SES"] = new ServiceInfo { DisplayName = "Amazon SES", IsRequired = false },
                ["CloudWatch"] = new ServiceInfo { DisplayName = "Amazon CloudWatch", IsRequired = false },
                ["IoTCore"] = new ServiceInfo { DisplayName = "AWS IoT Core", IsRequired = false },
                ["Lambda"] = new ServiceInfo { DisplayName = "AWS Lambda", IsRequired = false },
                ["EC2"] = new ServiceInfo { DisplayName = "Amazon EC2", IsRequired = false }
            };
            
            public class ServiceInfo
            {
                public string DisplayName { get; set; }
                public bool IsRequired { get; set; }
                public bool IsConfigured { get; set; }
                public bool IsEnabled { get; set; }
                public string Description { get; set; }
            }
        }
    }
}