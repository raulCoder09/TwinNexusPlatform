using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Scripts.Controllers.DeviceSelectionController
{
    /// <summary>
    /// Contenedor de todas las estructuras de datos específicas de Device Selection
    /// Organiza la información en categorías lógicas siguiendo el patrón de WelcomeInfo y DashboardInfo
    /// </summary>
    [Serializable]
    public class DeviceSelectionInfo
    {
        #region UI Configuration

        /// <summary>
        /// Configuración de elementos UI de Device Selection
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
            public Label SelectedModeUINameLabel { get; set; }
            public Label UsernameLabel { get; set; }
            public Label LogoutLabel { get; set; }
            
            #endregion

            #region Main Content Elements
            
            public ScrollView MainScrollView { get; set; }
            public List<VisualElement> DeviceRows { get; set; } = new List<VisualElement>();
            
            #endregion

            #region Device Button Elements
            
            public Dictionary<string, Button> DeviceButtons { get; set; } = new Dictionary<string, Button>();
            public Dictionary<string, VisualElement> DeviceVisualElements { get; set; } = new Dictionary<string, VisualElement>();
            public Dictionary<string, Label> DeviceLabels { get; set; } = new Dictionary<string, Label>();
            
            #endregion

            #region Navigation Menu Elements
            
            public VisualElement SubpanelsContainer { get; set; }
            public VisualElement NavigationMenuPanel { get; set; }
            public VisualElement NavigationMenu { get; set; }
            public VisualElement Scrim { get; set; }
            public Button HideMenuButton { get; set; }
            
            // Navigation Buttons
            public Dictionary<NavigationContext, Button> NavigationButtons { get; set; } = new Dictionary<NavigationContext, Button>();
            
            #endregion

            #region CSS Classes
            
            public string ScrimVisibleClass { get; set; } = "Opaque";
            public string ScrimHiddenClass { get; set; } = "Transparent";
            public string MenuVisibleClass { get; set; } = "NavigationMenuPanelInMainScreen";
            public string MenuHiddenClass { get; set; } = "NavigationMenuPanelOutMainScreen";
            public string DeviceSelectedClass { get; set; } = "device-button-selected";
            public string DeviceAvailableClass { get; set; } = "device-button-available";
            public string DeviceUnavailableClass { get; set; } = "device-button-unavailable";
            
            #endregion
        }

        #endregion

        #region Device Management

        /// <summary>
        /// Información y configuración de dispositivos disponibles
        /// </summary>
        [Serializable]
        public class DeviceData
        {
            public Dictionary<string, DeviceInfo> AvailableDevices { get; set; } = new Dictionary<string, DeviceInfo>();
            public string SelectedDevice { get; set; } = "";
            public DeviceMode CurrentMode { get; set; } = DeviceMode.Operating;
            public List<string> DeviceDisplayOrder { get; set; } = new List<string>();
            public int DevicesPerRow { get; set; } = 3;
            
            public DeviceData()
            {
                InitializeDefaultDevices();
            }
            
            /// <summary>
            /// Inicializa dispositivos por defecto
            /// </summary>
            private void InitializeDefaultDevices()
            {
                // Dispositivos principales
                AvailableDevices["ARSCARA"] = new DeviceInfo("ARSCARA", DeviceType.Industrial, true);
                AvailableDevices["RobotKit1"] = new DeviceInfo("Robot kit 1", DeviceType.Educational, true);
                AvailableDevices["RobotKit2"] = new DeviceInfo("Robot kit 2", DeviceType.Educational, false);
                
                // Dispositivos de repuesto/futuros
                for (int i = 1; i <= 6; i++)
                {
                    AvailableDevices[$"Spare{i}"] = new DeviceInfo($"Spare {i}", DeviceType.Placeholder, false);
                }
                
                // Orden de visualización
                DeviceDisplayOrder = new List<string>
                {
                    "ARSCARA", "RobotKit1", "RobotKit2",
                    "Spare1", "Spare2", "Spare3",
                    "Spare4", "Spare5", "Spare6"
                };
            }
            
            /// <summary>
            /// Obtiene dispositivos disponibles para un modo específico
            /// </summary>
            /// <param name="mode">Modo de dispositivo</param>
            /// <returns>Lista de dispositivos disponibles para el modo</returns>
            public List<DeviceInfo> GetAvailableDevicesForMode(DeviceMode mode)
            {
                var availableDevices = new List<DeviceInfo>();
                
                foreach (var device in AvailableDevices.Values)
                {
                    if (device.IsAvailable && device.SupportedModes.Contains(mode))
                    {
                        availableDevices.Add(device);
                    }
                }
                
                return availableDevices;
            }
            
            /// <summary>
            /// Verifica si un dispositivo está disponible para selección
            /// </summary>
            /// <param name="deviceId">ID del dispositivo</param>
            /// <param name="mode">Modo requerido</param>
            /// <returns>True si está disponible</returns>
            public bool IsDeviceAvailableForMode(string deviceId, DeviceMode mode)
            {
                if (!AvailableDevices.TryGetValue(deviceId, out var device))
                    return false;
                    
                return device.IsAvailable && device.SupportedModes.Contains(mode);
            }
        }

        /// <summary>
        /// Información individual de un dispositivo
        /// </summary>
        [Serializable]
        public class DeviceInfo
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public DeviceType Type { get; set; }
            public bool IsAvailable { get; set; }
            public List<DeviceMode> SupportedModes { get; set; } = new List<DeviceMode>();
            public string IconPath { get; set; }
            public Color StatusColor { get; set; } = Color.green;
            public DateTime LastUpdate { get; set; } = DateTime.Now;
            
            public DeviceInfo() { }
            
            public DeviceInfo(string displayName, DeviceType type, bool isAvailable)
            {
                Id = displayName.Replace(" ", "").Replace("kit", "Kit"); // "Robot kit 1" → "RobotKit1"
                DisplayName = displayName;
                Type = type;
                IsAvailable = isAvailable;
                LastUpdate = DateTime.Now;
                
                // Configurar modos soportados según el tipo
                ConfigureSupportedModes();
                
                // Configurar descripción según el tipo
                ConfigureDescription();
            }
            
            /// <summary>
            /// Configura los modos soportados según el tipo de dispositivo
            /// </summary>
            private void ConfigureSupportedModes()
            {
                SupportedModes.Clear();
                
                switch (Type)
                {
                    case DeviceType.Industrial:
                        // Dispositivos industriales soportan todos los modos
                        SupportedModes.AddRange(new[]
                        {
                            DeviceMode.Learning,
                            DeviceMode.Operating,
                            DeviceMode.Monitoring,
                            DeviceMode.Configuration
                        });
                        break;
                        
                    case DeviceType.Educational:
                        // Dispositivos educativos principalmente para learning y monitoring
                        SupportedModes.AddRange(new[]
                        {
                            DeviceMode.Learning,
                            DeviceMode.Monitoring,
                            DeviceMode.Configuration
                        });
                        break;
                        
                    case DeviceType.Simulation:
                        // Dispositivos de simulación para learning y monitoring
                        SupportedModes.AddRange(new[]
                        {
                            DeviceMode.Learning,
                            DeviceMode.Monitoring
                        });
                        break;
                        
                    case DeviceType.Placeholder:
                        // Placeholders no soportan ningún modo por defecto
                        break;
                }
            }
            
            /// <summary>
            /// Configura la descripción según el tipo de dispositivo
            /// </summary>
            private void ConfigureDescription()
            {
                Description = Type switch
                {
                    DeviceType.Industrial => $"Industrial automation device - {DisplayName}",
                    DeviceType.Educational => $"Educational robotics kit - {DisplayName}",
                    DeviceType.Simulation => $"Simulation device - {DisplayName}",
                    DeviceType.Placeholder => $"Future device slot - {DisplayName}",
                    _ => DisplayName
                };
            }
            
            /// <summary>
            /// Obtiene el color de estado según disponibilidad y tipo
            /// </summary>
            /// <returns>Color del dispositivo</returns>
            public Color GetDisplayColor()
            {
                if (!IsAvailable)
                    return Color.gray;
                    
                return Type switch
                {
                    DeviceType.Industrial => Color.green,
                    DeviceType.Educational => Color.cyan,
                    DeviceType.Simulation => Color.yellow,
                    DeviceType.Placeholder => Color.gray,
                    _ => Color.white
                };
            }
            
            /// <summary>
            /// Verifica si el dispositivo soporta un modo específico
            /// </summary>
            /// <param name="mode">Modo a verificar</param>
            /// <returns>True si soporta el modo</returns>
            public bool SupportsMode(DeviceMode mode)
            {
                return SupportedModes.Contains(mode);
            }
        }

        #endregion

        #region Context Management

        /// <summary>
        /// Información del contexto de navegación actual
        /// </summary>
        [Serializable]
        public class ContextData
        {
            public NavigationContext CurrentContext { get; set; } = NavigationContext.None;
            public NavigationContext SourceContext { get; set; } = NavigationContext.None;
            public DeviceMode RequiredMode { get; set; } = DeviceMode.Operating;
            public string UITitle { get; set; } = "Select Device";
            public string TargetScene { get; set; } = "";
            public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
            
            /// <summary>
            /// Configura el contexto basado en el NavigationContextManager
            /// </summary>
            public void ConfigureFromNavigationContext()
            {
                var navManager = NavigationContextManager.Instance;
                if (navManager != null)
                {
                    CurrentContext = navManager.CurrentContext;
                    SourceContext = navManager.PreviousContext;
                    RequiredMode = navManager.GetDeviceModeForContext(CurrentContext);
                    UITitle = navManager.GetUITitleForContext(CurrentContext);
                    TargetScene = navManager.GetTargetSceneForContext(CurrentContext);
                    
                    // Copiar datos adicionales
                    AdditionalData.Clear();
                    foreach (var kvp in navManager.ContextData)
                    {
                        AdditionalData[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            /// <summary>
            /// Verifica si el contexto está configurado correctamente
            /// </summary>
            /// <returns>True si el contexto es válido</returns>
            public bool IsValidContext()
            {
                return CurrentContext != NavigationContext.None && 
                       !string.IsNullOrEmpty(UITitle) && 
                       !string.IsNullOrEmpty(TargetScene);
            }
        }

        #endregion

        #region Navigation State

        /// <summary>
        /// Estado de navegación específico de Device Selection
        /// </summary>
        [Serializable]
        public class NavigationState
        {
            public bool IsMenuVisible { get; set; } = false;
            public NavigationContext ActiveSection { get; set; } = NavigationContext.DeviceSelection;
            public bool IsTransitioning { get; set; } = false;
            public DateTime LastTransition { get; set; } = DateTime.Now;
            
            /// <summary>
            /// Actualiza el estado de la transición
            /// </summary>
            /// <param name="isTransitioning">Si está en transición</param>
            public void SetTransitioning(bool isTransitioning)
            {
                IsTransitioning = isTransitioning;
                if (isTransitioning)
                {
                    LastTransition = DateTime.Now;
                }
            }
            
            /// <summary>
            /// Alterna la visibilidad del menú
            /// </summary>
            public void ToggleMenu()
            {
                IsMenuVisible = !IsMenuVisible;
            }
        }

        #endregion

        #region User Session

        /// <summary>
        /// Información de la sesión del usuario en Device Selection
        /// </summary>
        [Serializable]
        public class UserSessionData
        {
            public string Username { get; set; } = "";
            public bool IsAuthenticated { get; set; } = false;
            public DateTime SessionStart { get; set; } = DateTime.Now;
            public DateTime LastActivity { get; set; } = DateTime.Now;
            public List<string> RecentlySelectedDevices { get; set; } = new List<string>();
            public int MaxRecentDevices { get; set; } = 5;
            
            /// <summary>
            /// Actualiza la actividad del usuario
            /// </summary>
            public void UpdateActivity()
            {
                LastActivity = DateTime.Now;
            }
            
            /// <summary>
            /// Agrega un dispositivo a la lista de recientemente seleccionados
            /// </summary>
            /// <param name="deviceId">ID del dispositivo seleccionado</param>
            public void AddRecentDevice(string deviceId)
            {
                if (string.IsNullOrEmpty(deviceId)) return;
                
                // Remover si ya existe
                RecentlySelectedDevices.Remove(deviceId);
                
                // Agregar al principio
                RecentlySelectedDevices.Insert(0, deviceId);
                
                // Mantener límite
                if (RecentlySelectedDevices.Count > MaxRecentDevices)
                {
                    RecentlySelectedDevices.RemoveAt(RecentlySelectedDevices.Count - 1);
                }
                
                UpdateActivity();
            }
            
            /// <summary>
            /// Obtiene la duración de la sesión actual
            /// </summary>
            /// <returns>Duración de la sesión</returns>
            public TimeSpan GetSessionDuration()
            {
                return DateTime.Now - SessionStart;
            }
        }

        #endregion

        #region Device Selection State

        /// <summary>
        /// Estado general del sistema Device Selection
        /// </summary>
        [Serializable]
        public class DeviceSelectionState
        {
            public bool IsInitialized { get; set; } = false;
            public bool IsVisible { get; set; } = false;
            public bool IsDeviceSelected { get; set; } = false;
            public DateTime LastRefresh { get; set; } = DateTime.Now;
            public string LastError { get; set; } = "";
            
            // Eventos de estado
            public event Action OnDeviceSelectionInitialized;
            public event Action OnDeviceSelectionShown;
            public event Action OnDeviceSelectionHidden;
            public event Action<string> OnDeviceSelected;
            public event Action<string> OnError;
            
            /// <summary>
            /// Dispara el evento de inicialización
            /// </summary>
            public void TriggerInitialized()
            {
                OnDeviceSelectionInitialized?.Invoke();
            }
            
            /// <summary>
            /// Dispara el evento de UI mostrada
            /// </summary>
            public void TriggerShown()
            {
                OnDeviceSelectionShown?.Invoke();
            }
            
            /// <summary>
            /// Dispara el evento de UI oculta
            /// </summary>
            public void TriggerHidden()
            {
                OnDeviceSelectionHidden?.Invoke();
            }
            
            /// <summary>
            /// Dispara el evento de dispositivo seleccionado
            /// </summary>
            /// <param name="deviceId">ID del dispositivo seleccionado</param>
            public void TriggerDeviceSelected(string deviceId)
            {
                OnDeviceSelected?.Invoke(deviceId);
            }
            
            /// <summary>
            /// Dispara el evento de error
            /// </summary>
            /// <param name="error">Mensaje de error</param>
            public void TriggerError(string error)
            {
                LastError = error;
                OnError?.Invoke(error);
            }
            
            /// <summary>
            /// Marca el último refresh
            /// </summary>
            public void MarkRefreshed()
            {
                LastRefresh = DateTime.Now;
            }
        }

        #endregion
    }

    #region Supporting Enums

    /// <summary>
    /// Tipos de dispositivos disponibles
    /// </summary>
    public enum DeviceType
    {
        Industrial,     // Dispositivos industriales reales (ARSCARA)
        Educational,    // Kits educativos (Robot Kit 1, 2)
        Simulation,     // Dispositivos simulados
        Placeholder     // Espacios reservados para futuros dispositivos
    }

    #endregion
}