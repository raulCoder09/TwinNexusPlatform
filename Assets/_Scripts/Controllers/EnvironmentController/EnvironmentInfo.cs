using System;
using UnityEngine;

namespace _Scripts.Controllers.EnvironmentController
{
    /// <summary>
    /// Configuración e información del sistema de ambientes
    /// </summary>
    [System.Serializable]
    public class EnvironmentInfo
    {
        /// <summary>
        /// Configuración de un ambiente específico
        /// </summary>
        [System.Serializable]
        public class EnvironmentConfig
        {
            [Header("Environment Settings")]
            public EnvironmentType environmentType;
            public string environmentName;
            public GameObject environmentPrefab;
            
            [Header("UI Settings")]
            public bool requiresTransparentUI;
            public float uiOpacity = 1f;
            
            [Header("Loading Settings")]
            public float estimatedLoadTime = 2f;
            public string loadingMessage;
        }
        
        /// <summary>
        /// Estado actual del sistema de ambientes
        /// </summary>
        public class EnvironmentState
        {
            public bool IsInitialized { get; set; }
            public EnvironmentType CurrentEnvironment { get; set; } = EnvironmentType.None;
            public bool IsTransitioning { get; set; }
            public GameObject CurrentEnvironmentInstance { get; set; }
            public DateTime LastTransitionTime { get; set; }
            
            // Eventos de estado
            public event Action<EnvironmentType> OnEnvironmentChanged;
            public event Action<bool> OnTransitionStateChanged;
            
            // Métodos públicos para disparar eventos
            public void TriggerEnvironmentChanged(EnvironmentType environmentType)
            {
                OnEnvironmentChanged?.Invoke(environmentType);
            }
            
            public void TriggerTransitionStateChanged(bool isTransitioning)
            {
                OnTransitionStateChanged?.Invoke(isTransitioning);
            }
        }
    }
}