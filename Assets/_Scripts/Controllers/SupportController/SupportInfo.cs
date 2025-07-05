using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.SupportController
{
    /// <summary>
    /// Clases de configuración e información para el sistema Support
    /// Equivalente a DashboardInfo pero para Support Center
    /// </summary>
    [System.Serializable]
    public class SupportInfo
    {
        /// <summary>
        /// Configuración de la UI del Support Center
        /// </summary>
        public class UIConfiguration
        {
            // Contenedores principales
            public VisualElement Body { get; set; }
            public VisualElement SubpanelsContainer { get; set; }
            public VisualElement Scrim { get; set; }
            
            // Paneles configurables
            public Dictionary<ISupportOps.PanelType, PanelData> Panels { get; set; } 
                = new Dictionary<ISupportOps.PanelType, PanelData>();
            
            // Referencias específicas del Support Center
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
        /// Datos del usuario actual en el Support Center
        /// </summary>
        public class UserData
        {
            public string Username { get; set; }
            public string UserGroup { get; set; }
            public string UserRole { get; set; }
            public string Email { get; set; }
            public bool IsAuthenticated { get; set; }
            public DateTime LastLoginTime { get; set; }
            
            // Estados específicos del Support
            public string CurrentSupportLevel { get; set; } // "Basic", "Premium", "Enterprise"
            public int OpenTicketsCount { get; set; }
            public DateTime? LastSupportInteraction { get; set; }
            public List<string> RecentSupportActions { get; set; } = new List<string>();
        }

        /// <summary>
        /// Estado del Support Center
        /// </summary>
        public class SupportState
        {
            public bool IsInitialized { get; set; }
            public bool IsNavigationMenuOpen { get; set; }
            public ISupportOps.PanelType CurrentActivePanel { get; set; } = ISupportOps.PanelType.None;
            public string CurrentSection { get; set; } = "Support";
            
            // Estados del sistema de soporte
            public ISupportOps.SupportStatus SystemSupportStatus { get; set; } = ISupportOps.SupportStatus.Available;
            public bool DiagnosticsRunning { get; set; } = false;
            public bool RemoteAssistanceActive { get; set; } = false;
            public DateTime? LastDiagnosticsRun { get; set; }
            
            // Estadísticas del soporte
            public int TotalTicketsSubmitted { get; set; }
            public int OpenTickets { get; set; }
            public int ResolvedTickets { get; set; }
            public TimeSpan AverageResponseTime { get; set; }
            
            // Eventos de estado
            public event Action<ISupportOps.PanelType> OnPanelChanged;
            public event Action<bool> OnNavigationMenuToggled;
            public event Action<ISupportOps.SupportStatus> OnSupportStatusChanged;
            public event Action<DiagnosticResult> OnDiagnosticsCompleted;
            public event Action<SupportTicket> OnTicketCreated;
        }

        /// <summary>
        /// Configuración de navegación del Support Center
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

        /// <summary>
        /// Configuración de diagnósticos del sistema
        /// </summary>
        public class DiagnosticsConfig
        {
            public bool AutoRunOnStartup { get; set; } = false;
            public TimeSpan AutoRunInterval { get; set; } = TimeSpan.FromHours(24);
            public bool IncludeSystemLogs { get; set; } = true;
            public bool IncludePerformanceMetrics { get; set; } = true;
            public bool IncludeNetworkStatus { get; set; } = true;
            public bool IncludeIoTStatus { get; set; } = true;
            public bool AutoSubmitResults { get; set; } = false;
            public List<string> CustomChecks { get; set; } = new List<string>();
        }

        /// <summary>
        /// Configuración de tickets de soporte
        /// </summary>
        public class TicketingConfig
        {
            public bool EnableTicketSystem { get; set; } = true;
            public int MaxTicketsPerUser { get; set; } = 10;
            public bool AutoAssignPriority { get; set; } = true;
            public bool NotifyOnStatusChange { get; set; } = true;
            public bool AllowFileAttachments { get; set; } = true;
            public long MaxAttachmentSize { get; set; } = 10 * 1024 * 1024; // 10MB
            public List<string> AllowedAttachmentTypes { get; set; } = new List<string>
            {
                ".txt", ".log", ".pdf", ".png", ".jpg", ".zip"
            };
        }

        /// <summary>
        /// Configuración de asistencia remota
        /// </summary>
        public class RemoteAssistanceConfig
        {
            public bool EnableRemoteAssistance { get; set; } = true;
            public bool RequireUserConfirmation { get; set; } = true;
            public bool LogRemoteSessions { get; set; } = true;
            public TimeSpan MaxSessionDuration { get; set; } = TimeSpan.FromHours(2);
            public bool AllowFileTransfer { get; set; } = false;
            public bool AllowScreenControl { get; set; } = true;
            public string RemoteToolUrl { get; set; } = "https://remote.support.twinnexus.com";
        }

        /// <summary>
        /// Configuración de documentación
        /// </summary>
        public class DocumentationConfig
        {
            public string BaseDocumentationUrl { get; set; } = "https://docs.twinnexus.com";
            public List<DocumentCategory> Categories { get; set; } = new List<DocumentCategory>();
            public bool EnableSearch { get; set; } = true;
            public bool EnableDownloads { get; set; } = true;
            public bool EnableOfflineAccess { get; set; } = false;
            
            public class DocumentCategory
            {
                public string Name { get; set; }
                public string Description { get; set; }
                public List<Document> Documents { get; set; } = new List<Document>();
                
                public class Document
                {
                    public string Title { get; set; }
                    public string Description { get; set; }
                    public string Url { get; set; }
                    public string Type { get; set; } // "PDF", "HTML", "Video", etc.
                    public DateTime LastUpdated { get; set; }
                    public string Version { get; set; }
                }
            }
        }

        /// <summary>
        /// Configuración completa del Support Center
        /// </summary>
        public class SupportConfiguration
        {
            public SupportContactInfo ContactInfo { get; set; } = new SupportContactInfo();
            public DiagnosticsConfig Diagnostics { get; set; } = new DiagnosticsConfig();
            public TicketingConfig Ticketing { get; set; } = new TicketingConfig();
            public RemoteAssistanceConfig RemoteAssistance { get; set; } = new RemoteAssistanceConfig();
            public DocumentationConfig Documentation { get; set; } = new DocumentationConfig();
            
            // Configuraciones generales
            public bool EnableAnalytics { get; set; } = true;
            public bool EnableNotifications { get; set; } = true;
            public string DefaultLanguage { get; set; } = "en-US";
            public string ThemeMode { get; set; } = "cyberpunk";
        }

        /// <summary>
        /// Métricas y estadísticas del Support Center
        /// </summary>
        public class SupportMetrics
        {
            public DateTime LastUpdated { get; set; }
            public int TotalSupportRequests { get; set; }
            public int ActiveSupportSessions { get; set; }
            public TimeSpan AverageResponseTime { get; set; }
            public double CustomerSatisfactionScore { get; set; }
            public int DiagnosticsRunToday { get; set; }
            public int TicketsCreatedToday { get; set; }
            public int TicketsResolvedToday { get; set; }
            
            public Dictionary<string, int> IssueCategories { get; set; } = new Dictionary<string, int>();
            public Dictionary<ISupportOps.SupportPriority, int> TicketsByPriority { get; set; } = 
                new Dictionary<ISupportOps.SupportPriority, int>();
        }
    }
}