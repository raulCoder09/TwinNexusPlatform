using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.DashboardController
{
    /// <summary>
    /// Contenedor de todas las estructuras de datos específicas del Dashboard
    /// Organiza la información en categorías lógicas siguiendo el patrón de WelcomeInfo
    /// </summary>
    [Serializable]
    public class DashboardInfo
    {
        #region UI Configuration

        /// <summary>
        /// Configuración de elementos UI del Dashboard
        /// </summary>
        [Serializable]
        public class UIConfiguration
        {
            #region Main UI Elements
            
            public VisualElement Body { get; set; }
            public VisualElement Header { get; set; }
            public VisualElement Main { get; set; }
            public VisualElement Footer { get; set; }
            
            #endregion

            #region Header Elements
            
            public Button MenuButton { get; set; }
            public Label UsernameLabel { get; set; }
            public Label LogoutLabel { get; set; }
            
            #endregion

            #region Widget Panels
            
            public VisualElement WelcomePanel { get; set; }
            public VisualElement IoTStatusPanel { get; set; }
            public VisualElement SystemStatusPanel { get; set; }
            public VisualElement RecentActivityPanel { get; set; }
            public VisualElement DeviceControlPanel { get; set; }
            
            #endregion

            #region Navigation Menu
            
            public VisualElement NavigationMenuContainer { get; set; }
            public VisualElement NavigationMenu { get; set; }
            public VisualElement Scrim { get; set; }
            public Button HideMenuButton { get; set; }
            
            // Navigation Buttons
            public Dictionary<DashboardSection, Button> NavigationButtons { get; set; } = new Dictionary<DashboardSection, Button>();
            
            #endregion

            #region Device Control Elements
            
            public Button DeviceNavLeftButton { get; set; }
            public Button DeviceNavRightButton { get; set; }
            public Label DeviceTitleLabel { get; set; }
            public Label DeviceStatusOnlineLabel { get; set; }
            public Label DeviceStatusBusyLabel { get; set; }
            public Label DeviceStatusAlarmedLabel { get; set; }
            public VisualElement DeviceVisualizationBox { get; set; }
            public Label DeviceModeLabel { get; set; }
            public Label CoordinateXLabel { get; set; }
            public Label CoordinateYLabel { get; set; }
            public Label CoordinateZLabel { get; set; }
            
            #endregion

            #region IoT Status Elements
            
            public Label LocalIoTStatusLabel { get; set; }
            public Label LocalIoTModeLabel { get; set; }
            public Label VMIoTStatusLabel { get; set; }
            public Label VMIoTModeLabel { get; set; }
            public Label CloudIoTStatusLabel { get; set; }
            public Label CloudIoTModeLabel { get; set; }
            
            #endregion

            #region CSS Classes
            
            public string ScrimVisibleClass { get; set; } = "ScrimOpaque";
            public string ScrimHiddenClass { get; set; } = "ScrimTransparent";
            public string MenuVisibleClass { get; set; } = "NavigationMenuPanelinMainScreen";
            public string MenuHiddenClass { get; set; } = "NavigationMenuPanelOutMainScreen";
            
            #endregion
        }

        #endregion

        #region Device Management

        /// <summary>
        /// Información y estado de dispositivos del sistema
        /// </summary>
        [Serializable]
        public class DeviceData
        {
            public Dictionary<string, DeviceInfo> Devices { get; set; } = new Dictionary<string, DeviceInfo>();
            public string CurrentSelectedDevice { get; set; } = "ARSCARA";
            public List<string> DeviceOrder { get; set; } = new List<string> { "ARSCARA", "Robotics kit 1", "Robotics kit 2", "Device 3" };
        }

        /// <summary>
        /// Información individual de un dispositivo
        /// </summary>
        [Serializable]
        public class DeviceInfo
        {
            public string Name { get; set; }
            public DeviceStatus Status { get; set; } = DeviceStatus.Offline;
            public OperationMode Mode { get; set; } = OperationMode.Manual;
            public Vector3 Coordinates { get; set; } = Vector3.zero;
            public DateTime LastUpdate { get; set; } = DateTime.Now;
            public bool IsAlarmed { get; set; } = false;
            public bool IsBusy { get; set; } = false;

            public DeviceInfo(string name)
            {
                Name = name;
                LastUpdate = DateTime.Now;
            }

            public string GetDisplayStatus()
            {
                if (IsAlarmed) return "Alarmed";
                if (IsBusy) return "Busy";
                return Status.ToString();
            }

            public Color GetStatusColor()
            {
                if (IsAlarmed) return Color.red;
                if (IsBusy) return Color.yellow;
                return Status == DeviceStatus.Online ? Color.green : Color.red;
            }
        }

        #endregion

        #region IoT Infrastructure

        /// <summary>
        /// Estado de la infraestructura IoT
        /// </summary>
        [Serializable]
        public class IoTInfrastructureData
        {
            public Dictionary<IoTService, IoTServiceInfo> Services { get; set; } = new Dictionary<IoTService, IoTServiceInfo>();
            
            public IoTInfrastructureData()
            {
                // Inicializar servicios por defecto
                Services[IoTService.LocalIoT] = new IoTServiceInfo("Local IoT", ConnectionStatus.Disconnected, OperationMode.Manual);
                Services[IoTService.VMIoT] = new IoTServiceInfo("VM IoT", ConnectionStatus.Disconnected, OperationMode.Manual);
                Services[IoTService.AWSIoTCore] = new IoTServiceInfo("AWS IoT core", ConnectionStatus.Disconnected, OperationMode.Manual);
            }
        }

        /// <summary>
        /// Información de un servicio IoT específico
        /// </summary>
        [Serializable]
        public class IoTServiceInfo
        {
            public string DisplayName { get; set; }
            public ConnectionStatus Status { get; set; }
            public OperationMode Mode { get; set; }
            public DateTime LastUpdate { get; set; }
            public string LastError { get; set; }

            public IoTServiceInfo(string displayName, ConnectionStatus status, OperationMode mode)
            {
                DisplayName = displayName;
                Status = status;
                Mode = mode;
                LastUpdate = DateTime.Now;
            }

            public string GetStatusDisplayText()
            {
                return Status switch
                {
                    ConnectionStatus.Connected => "Online",
                    ConnectionStatus.Connecting => "Connecting",
                    ConnectionStatus.Disconnected => "Offline",
                    ConnectionStatus.Error => "Error",
                    _ => "Unknown"
                };
            }

            public Color GetStatusColor()
            {
                return Status switch
                {
                    ConnectionStatus.Connected => Color.green,
                    ConnectionStatus.Connecting => Color.yellow,
                    ConnectionStatus.Disconnected => Color.red,
                    ConnectionStatus.Error => Color.red,
                    _ => Color.gray
                };
            }
        }

        #endregion

        #region System Activity

        /// <summary>
        /// Gestión del historial de actividades del sistema
        /// </summary>
        [Serializable]
        public class SystemActivityData
        {
            public List<SystemActivity> RecentActivities { get; set; } = new List<SystemActivity>();
            public int MaxActivities { get; set; } = 50;
            
            public void AddActivity(SystemActivity activity)
            {
                RecentActivities.Insert(0, activity); // Insertar al principio (más reciente)
                
                // Mantener solo las actividades más recientes
                if (RecentActivities.Count > MaxActivities)
                {
                    RecentActivities.RemoveAt(RecentActivities.Count - 1);
                }
            }

            public List<SystemActivity> GetRecentActivities(int count = 10)
            {
                int actualCount = Math.Min(count, RecentActivities.Count);
                return RecentActivities.GetRange(0, actualCount);
            }

            public void ClearHistory()
            {
                RecentActivities.Clear();
            }
        }

        #endregion

        #region User Session

        /// <summary>
        /// Información de la sesión del usuario
        /// </summary>
        [Serializable]
        public class UserSessionData
        {
            public string Username { get; set; }
            public string UserGroup { get; set; }
            public bool IsAuthenticated { get; set; }
            public DateTime LoginTime { get; set; }
            public DateTime LastActivity { get; set; }
            
            public void UpdateActivity()
            {
                LastActivity = DateTime.Now;
            }

            public string GetWelcomeMessage()
            {
                return $"Welcome back, {Username}";
            }

            public TimeSpan GetSessionDuration()
            {
                return DateTime.Now - LoginTime;
            }
        }

        #endregion

        #region Navigation State

        /// <summary>
        /// Estado de navegación del Dashboard
        /// </summary>
        [Serializable]
        public class NavigationState
        {
            public bool IsMenuVisible { get; set; } = false;
            public DashboardSection CurrentSection { get; set; } = DashboardSection.Main;
            public DashboardSection PreviousSection { get; set; } = DashboardSection.Main;
            public Stack<DashboardSection> NavigationHistory { get; set; } = new Stack<DashboardSection>();

            public void NavigateTo(DashboardSection section)
            {
                if (CurrentSection != section)
                {
                    NavigationHistory.Push(CurrentSection);
                    PreviousSection = CurrentSection;
                    CurrentSection = section;
                }
            }

            public DashboardSection GoBack()
            {
                if (NavigationHistory.Count > 0)
                {
                    var previousSection = NavigationHistory.Pop();
                    PreviousSection = CurrentSection;
                    CurrentSection = previousSection;
                    return CurrentSection;
                }
                return CurrentSection;
            }

            public void ToggleMenu()
            {
                IsMenuVisible = !IsMenuVisible;
            }
        }

        #endregion

        #region Dashboard State

        /// <summary>
        /// Estado general del Dashboard
        /// </summary>
        [Serializable]
        public class DashboardState
        {
            public bool IsInitialized { get; set; } = false;
            public bool IsVisible { get; set; } = false;
            public DateTime LastRefresh { get; set; } = DateTime.Now;
            public bool AutoRefreshEnabled { get; set; } = true;
            public float RefreshInterval { get; set; } = 5.0f; // segundos

            // Eventos de estado
            public event Action OnDashboardInitialized;
            public event Action OnDashboardShown;
            public event Action OnDashboardHidden;
            public event Action OnRefreshCompleted;
            public event Action<string> OnError;

            public void TriggerInitialized() => OnDashboardInitialized?.Invoke();
            public void TriggerShown() => OnDashboardShown?.Invoke();
            public void TriggerHidden() => OnDashboardHidden?.Invoke();
            public void TriggerRefreshCompleted() => OnRefreshCompleted?.Invoke();
            public void TriggerError(string error) => OnError?.Invoke(error);

            public bool ShouldRefresh()
            {
                return AutoRefreshEnabled && (DateTime.Now - LastRefresh).TotalSeconds >= RefreshInterval;
            }

            public void MarkRefreshed()
            {
                LastRefresh = DateTime.Now;
            }
        }

        #endregion
    }
}