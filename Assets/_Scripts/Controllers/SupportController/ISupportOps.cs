using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace _Scripts.Controllers.SupportController
{
    /// <summary>
    /// Interface que define las operaciones disponibles en el Support Center
    /// Similar a IDashboardOps pero para operaciones de soporte técnico
    /// </summary>
    public interface ISupportOps
    {
        // Navegación de paneles
        void NavigateToPanel(PanelType panelType);
        void CloseCurrentPanel();
        void SwitchPanel(PanelType fromPanel, PanelType toPanel);
        
        // Operaciones específicas del Support Center
        void ContactTechnicalSupport();
        void BrowseDocumentation();
        void RunSystemDiagnostics();
        void RequestRemoteAssistance();
        void SubmitSupportTicket(string subject, string description, SupportPriority priority);
        void ViewSupportHistory();
        void DownloadSystemLogs();
        void ShowNavigationMenu();
        void HideNavigationMenu();
        
        // Estados
        bool IsNavigationMenuOpen { get; }
        PanelType CurrentActivePanel { get; }
        SupportStatus CurrentSupportStatus { get; }
        
        // Eventos
        event Action OnNavigationMenuOpened;
        event Action OnNavigationMenuClosed;
        event Action<PanelType> OnPanelTransitionComplete;
        event Action<string> OnSupportActionCompleted;
        event Action<DiagnosticResult> OnDiagnosticsCompleted;
        event Action<SupportTicket> OnTicketSubmitted;

        /// <summary>
        /// Tipos de paneles disponibles en el Support Center
        /// </summary>
        public enum PanelType
        {
            None,
            NavigationMenu,         // Menú lateral principal
            TechnicalSupport,       // Panel de soporte técnico
            Documentation,          // Panel de documentación
            SystemDiagnostics,      // Panel de diagnósticos del sistema
            RemoteAssistance,       // Panel de asistencia remota
            SupportTickets,         // Panel de tickets de soporte
            SupportHistory,         // Panel de historial de soporte
            ContactInfo,            // Panel de información de contacto
            SystemLogs,             // Panel de logs del sistema
            FAQ                     // Panel de preguntas frecuentes
        }

        /// <summary>
        /// Prioridades de tickets de soporte
        /// </summary>
        public enum SupportPriority
        {
            Low,        // Baja prioridad - consultas generales
            Medium,     // Prioridad media - problemas no críticos
            High,       // Alta prioridad - afecta operaciones
            Critical,   // Crítico - sistema inoperativo
            Emergency   // Emergencia - seguridad o pérdida de datos
        }

        /// <summary>
        /// Estados del sistema de soporte
        /// </summary>
        public enum SupportStatus
        {
            Available,      // Sistema de soporte disponible
            Busy,           // Soporte ocupado
            Maintenance,    // En mantenimiento
            Offline,        // Fuera de línea
            Emergency       // Modo de emergencia
        }
    }

    #region Supporting Classes

    /// <summary>
    /// Resultado de diagnósticos del sistema
    /// </summary>
    [System.Serializable]
    public class DiagnosticResult
    {
        public DateTime Timestamp { get; set; }
        public bool OverallHealthy { get; set; }
        public List<DiagnosticIssue> Issues { get; set; } = new List<DiagnosticIssue>();
        public Dictionary<string, object> SystemMetrics { get; set; } = new Dictionary<string, object>();
        public string ReportId { get; set; }
        public string Summary { get; set; }

        public class DiagnosticIssue
        {
            public string Component { get; set; }
            public string Issue { get; set; }
            public string Severity { get; set; } // "Low", "Medium", "High", "Critical"
            public string Recommendation { get; set; }
        }
    }

    /// <summary>
    /// Ticket de soporte
    /// </summary>
    [System.Serializable]
    public class SupportTicket
    {
        public string TicketId { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public ISupportOps.SupportPriority Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string Status { get; set; } // "Open", "InProgress", "Resolved", "Closed"
        public string AssignedTo { get; set; }
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public List<string> Attachments { get; set; } = new List<string>();
        public List<TicketComment> Comments { get; set; } = new List<TicketComment>();

        public class TicketComment
        {
            public DateTime Timestamp { get; set; }
            public string Author { get; set; }
            public string Message { get; set; }
            public bool IsInternal { get; set; }
        }
    }

    /// <summary>
    /// Información de contacto del soporte
    /// </summary>
    [System.Serializable]
    public class SupportContactInfo
    {
        public string EmergencyPhone { get; set; }
        public string GeneralEmail { get; set; }
        public string TechnicalEmail { get; set; }
        public string BusinessHours { get; set; }
        public string TimeZone { get; set; }
        public string WebsiteUrl { get; set; }
        public string RemoteAssistanceUrl { get; set; }
    }

    /// <summary>
    /// Configuración del sistema de soporte
    /// </summary>
    [System.Serializable]
    public class SupportConfiguration
    {
        public SupportContactInfo ContactInfo { get; set; } = new SupportContactInfo();
        public bool EnableRemoteAssistance { get; set; } = true;
        public bool EnableSystemDiagnostics { get; set; } = true;
        public bool EnableTicketSystem { get; set; } = true;
        public bool AutoSubmitDiagnostics { get; set; } = false;
        public int MaxTicketsPerUser { get; set; } = 10;
        public List<string> AvailableDocuments { get; set; } = new List<string>();
    }

    #endregion
}