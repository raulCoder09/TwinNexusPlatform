using System;
using UnityEngine;
using UnityEngine.InputSystem;
using _Scripts.Models;

namespace _Scripts.Testing
{
    public class CloudWatchTest : MonoBehaviour
    {
        private InputAction publishTestMetricAction;
        private InputAction listMetricsAction;
        private InputAction createAlarmAction;
        private InputAction triggerAlarmAction;
        private InputAction sendMonitoringLogAction; // ← NUEVO

        void Start()
        {
            // Initialize input actions
            publishTestMetricAction = new InputAction("PublishTestMetric", InputActionType.Button, "<Keyboard>/m");
            listMetricsAction = new InputAction("ListMetrics", InputActionType.Button, "<Keyboard>/l");
            createAlarmAction = new InputAction("CreateAlarm", InputActionType.Button, "<Keyboard>/a");
            triggerAlarmAction = new InputAction("TriggerAlarm", InputActionType.Button, "<Keyboard>/t");
            sendMonitoringLogAction = new InputAction("SendMonitoringLog", InputActionType.Button, "<Keyboard>/o"); // ← NUEVO
            
            // Register callbacks
            publishTestMetricAction.performed += OnPublishTestMetricPressed;
            listMetricsAction.performed += OnListMetricsPressed;
            createAlarmAction.performed += OnCreateAlarmPressed;
            triggerAlarmAction.performed += OnTriggerAlarmPressed;
            sendMonitoringLogAction.performed += OnSendMonitoringLogPressed; // ← NUEVO
            
            // Enable actions
            publishTestMetricAction.Enable();
            listMetricsAction.Enable();
            createAlarmAction.Enable();
            triggerAlarmAction.Enable();
            sendMonitoringLogAction.Enable(); // ← NUEVO
            
            // Subscribe to CloudWatch events
            if (CloudWatchManager.Instance != null)
            {
                CloudWatchManager.Instance.OnMetricPublished += OnMetricPublishedResult;
                CloudWatchManager.Instance.OnMetricsListed += OnMetricsListedResult;
                CloudWatchManager.Instance.OnAlarmCreated += OnAlarmCreatedResult;
            }
            
            Debug.Log("🚀 CloudWatchTest iniciado:");
            Debug.Log("📊 Presiona 'M' para publicar métrica de prueba (valor: 42)");
            Debug.Log("📋 Presiona 'L' para listar métricas existentes");
            Debug.Log("🚨 Presiona 'A' para crear alarma de prueba (< 30)");
            Debug.Log("🔥 Presiona 'T' para disparar alarma (valor: 25)");
            Debug.Log("📱 Presiona 'O' para enviar logs de monitoreo de app"); // ← NUEVO
            Debug.Log("⚙️ Asegúrate de estar autenticado primero");
        }

        void OnDestroy()
        {
            if (publishTestMetricAction != null)
            {
                publishTestMetricAction.performed -= OnPublishTestMetricPressed;
                publishTestMetricAction.Disable();
                publishTestMetricAction.Dispose();
            }
            
            if (listMetricsAction != null)
            {
                listMetricsAction.performed -= OnListMetricsPressed;
                listMetricsAction.Disable();
                listMetricsAction.Dispose();
            }
            
            if (createAlarmAction != null)
            {
                createAlarmAction.performed -= OnCreateAlarmPressed;
                createAlarmAction.Disable();
                createAlarmAction.Dispose();
            }
            
            if (triggerAlarmAction != null)
            {
                triggerAlarmAction.performed -= OnTriggerAlarmPressed;
                triggerAlarmAction.Disable();
                triggerAlarmAction.Dispose();
            }
            
            // ← NUEVO
            if (sendMonitoringLogAction != null)
            {
                sendMonitoringLogAction.performed -= OnSendMonitoringLogPressed;
                sendMonitoringLogAction.Disable();
                sendMonitoringLogAction.Dispose();
            }
        }

        #region Input Action Callbacks
        
        private void OnPublishTestMetricPressed(InputAction.CallbackContext context)
        {
            ExecutePublishTestMetric();
        }
        
        private void OnListMetricsPressed(InputAction.CallbackContext context)
        {
            ExecuteListMetrics();
        }
        
        private void OnCreateAlarmPressed(InputAction.CallbackContext context)
        {
            ExecuteCreateAlarm();
        }
        
        private void OnTriggerAlarmPressed(InputAction.CallbackContext context)
        {
            ExecuteTriggerAlarm();
        }
        
        // ← NUEVO
        private void OnSendMonitoringLogPressed(InputAction.CallbackContext context)
        {
            ExecuteSendMonitoringLog();
        }
        
        #endregion

        #region Test Execution Methods
        
        async void ExecutePublishTestMetric()
        {
            if (CloudWatchManager.Instance == null)
            {
                Debug.LogError("CloudWatchManager no disponible.");
                return;
            }
            
            Debug.Log("📊 Iniciando publicación de métrica de prueba...");
            bool success = await CloudWatchManager.Instance.PublishTestMetricAsync();
            Debug.Log(success ? "✅ Test métrica completado" : "❌ Test métrica falló");
        }
        
        async void ExecuteListMetrics()
        {
            if (CloudWatchManager.Instance == null)
            {
                Debug.LogError("CloudWatchManager no disponible.");
                return;
            }
            
            Debug.Log("📋 Iniciando listado de métricas...");
            var metrics = await CloudWatchManager.Instance.ListMetricsAsync();
            Debug.Log(metrics.Count > 0 ? "✅ Listado completado" : "❌ Listado falló o no hay métricas");
        }
        
        async void ExecuteCreateAlarm()
        {
            if (CloudWatchManager.Instance == null)
            {
                Debug.LogError("CloudWatchManager no disponible.");
                return;
            }
            
            Debug.Log("🚨 Iniciando creación de alarma de prueba...");
            bool success = await CloudWatchManager.Instance.CreateTestAlarmAsync();
            Debug.Log(success ? "✅ Alarma creada exitosamente" : "❌ Creación de alarma falló");
        }
        
        async void ExecuteTriggerAlarm()
        {
            if (CloudWatchManager.Instance == null)
            {
                Debug.LogError("CloudWatchManager no disponible.");
                return;
            }
            
            Debug.Log("🔥 Iniciando disparo de alarma de prueba...");
            Debug.LogWarning("⚠️ ATENCIÓN: Esto publicará un valor bajo para activar la alarma!");
            bool success = await CloudWatchManager.Instance.TriggerTestAlarmAsync();
            Debug.Log(success ? "✅ Alarma disparada - Ve a AWS Console para ver el estado" : "❌ Disparo de alarma falló");
        }
        
        // ← NUEVO MÉTODO
        async void ExecuteSendMonitoringLog()
        {
            if (CloudWatchManager.Instance == null)
            {
                Debug.LogError("CloudWatchManager no disponible.");
                return;
            }
            
            Debug.Log("📱 Iniciando envío de logs de monitoreo...");
            Debug.Log("📝 Enviando múltiples tipos de eventos de monitoreo...");
            bool success = await CloudWatchManager.Instance.SendTestMonitoringLogAsync();
            Debug.Log(success ? "✅ Logs de monitoreo enviados - Ve a AWS Console → CloudWatch → Logs para verlos" : "❌ Envío de logs falló");
        }
        
        #endregion

        #region Event Handlers
        
        void OnMetricPublishedResult(bool success, string message, string metricName)
        {
            if (success)
            {
                Debug.Log($"🎉 ¡Métrica publicada exitosamente!");
                Debug.Log($"📊 Métrica: {metricName}");
                Debug.Log($"💬 Mensaje: {message}");
                Debug.Log($"🔍 Ve a AWS Console → CloudWatch → Metrics → TwinNexusPlatform para verla");
            }
            else
            {
                Debug.LogError($"❌ Error publicando métrica: {message}");
                Debug.LogError($"📊 Métrica: {metricName}");
            }
        }
        
        void OnMetricsListedResult(bool success, string message, System.Collections.Generic.List<_Scripts.Models.MetricInfo> metrics)
        {
            if (success)
            {
                Debug.Log($"📋 ¡Métricas listadas exitosamente!");
                Debug.Log($"📊 Total encontradas: {metrics?.Count ?? 0}");
                Debug.Log($"💬 Mensaje: {message}");
                
                if (metrics != null && metrics.Count > 0)
                {
                    Debug.Log("📈 Métricas encontradas:");
                    for (int i = 0; i < System.Math.Min(metrics.Count, 10); i++) // Solo mostrar las primeras 10
                    {
                        var metric = metrics[i];
                        Debug.Log($"   {i + 1}. {metric.Name}");
                        if (!string.IsNullOrEmpty(metric.DimensionsInfo))
                        {
                            Debug.Log($"      Dimensions: {metric.DimensionsInfo}");
                        }
                    }
                    
                    if (metrics.Count > 10)
                    {
                        Debug.Log($"   ... y {metrics.Count - 10} métricas más");
                    }
                }
                else
                {
                    Debug.Log("📭 No se encontraron métricas en el namespace");
                }
            }
            else
            {
                Debug.LogError($"❌ Error listando métricas: {message}");
            }
        }
        
        void OnAlarmCreatedResult(bool success, string message, string alarmName)
        {
            if (success)
            {
                Debug.Log($"🚨 ¡Alarma creada exitosamente!");
                Debug.Log($"📢 Alarma: {alarmName}");
                Debug.Log($"💬 Mensaje: {message}");
                Debug.Log($"🔍 Ve a AWS Console → CloudWatch → Alarms para verla");
                Debug.Log($"💡 La alarma se activará cuando TestMetric < 30");
            }
            else
            {
                Debug.LogError($"❌ Error creando alarma: {message}");
                Debug.LogError($"📢 Alarma: {alarmName}");
            }
        }
        
        #endregion
    }
}