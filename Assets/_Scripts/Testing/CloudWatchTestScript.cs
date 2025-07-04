using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using _Scripts.Models.CloudWatchManagement;
using _Scripts.Controller;

namespace _Scripts.Testing
{
    /// <summary>
    /// Script de pruebas para CloudWatch usando la nueva arquitectura
    /// Accede a CloudWatchManager.Instance sin configuraciones complejas
    /// </summary>
    public class CloudWatchTestScript : MonoBehaviour
    {
        [Header("Input Actions - Control por Teclado")] [SerializeField]
        private KeyCode testKeyPublishMetric = KeyCode.M;

        [SerializeField] private KeyCode testKeyListMetrics = KeyCode.L;
        [SerializeField] private KeyCode testKeyCreateAlarm = KeyCode.A;
        [SerializeField] private KeyCode testKeySendLog = KeyCode.G;
        [SerializeField] private KeyCode testKeyMonitoringLog = KeyCode.O;
        [SerializeField] private KeyCode testKeyUIEvent = KeyCode.U;
        [SerializeField] private KeyCode testKeyTestMetric = KeyCode.T;
        [SerializeField] private KeyCode testKeyTestAlarm = KeyCode.Y;

        // Input Actions
        private InputAction actionPublishMetric;
        private InputAction actionListMetrics;
        private InputAction actionCreateAlarm;
        private InputAction actionSendLog;
        private InputAction actionMonitoringLog;
        private InputAction actionUIEvent;
        private InputAction actionTestMetric;
        private InputAction actionTestAlarm;

        [Header("UI References")] [SerializeField]
        private Button _publishMetricButton;

        [SerializeField] private Button _listMetricsButton;
        [SerializeField] private Button _createAlarmButton;
        [SerializeField] private Button _sendLogButton;
        [SerializeField] private Button _monitoringLogButton;
        [SerializeField] private Button _uiEventButton;
        [SerializeField] private Button _testMetricButton;
        [SerializeField] private Button _testAlarmButton;

        [Header("Test Configuration")] [SerializeField]
        private string _testMetricName = "UnityTestMetric";

        [SerializeField] private double _testMetricValue = 100.0;
        [SerializeField] private string _testAlarmName = "UnityTestAlarm";
        [SerializeField] private double _testAlarmThreshold = 75.0;
        [SerializeField] private string _testLogMessage = "Test log from Unity CloudWatch";

        [SerializeField] private List<string> _testEventTypes = new List<string>
        {
            "UserLogin",
            "ButtonClick",
            "SceneChange",
            "Performance",
            "Error"
        };

        [Header("Status Display")] [SerializeField]
        private Text _statusText;

        [SerializeField] private Text _metricsText;
        [SerializeField] private Text _logsText;

        private void Start()
        {
            // Configurar Input Actions
            SetupInputActions();

            // Configurar botones UI
            SetupButtons();

            // Mostrar estado inicial
            UpdateStatusDisplay();

            // Suscribirse a eventos de ServiceController para monitorear estado
            if (ServiceController.Instance != null)
            {
                ServiceController.Instance.OnServiceActivated += OnServiceActivated;
                ServiceController.Instance.OnServiceError += OnServiceError;
            }

            // Mostrar controles en consola
            ShowControlsHelp();
        }

        private void SetupInputActions()
        {
            // Crear Input Actions siguiendo el patrón
            actionPublishMetric = new InputAction("publishMetric", InputActionType.Button,
                $"<Keyboard>/{testKeyPublishMetric}");
            actionListMetrics =
                new InputAction("listMetrics", InputActionType.Button, $"<Keyboard>/{testKeyListMetrics}");
            actionCreateAlarm =
                new InputAction("createAlarm", InputActionType.Button, $"<Keyboard>/{testKeyCreateAlarm}");
            actionSendLog = new InputAction("sendLog", InputActionType.Button, $"<Keyboard>/{testKeySendLog}");
            actionMonitoringLog = new InputAction("monitoringLog", InputActionType.Button,
                $"<Keyboard>/{testKeyMonitoringLog}");
            actionUIEvent = new InputAction("uiEvent", InputActionType.Button, $"<Keyboard>/{testKeyUIEvent}");
            actionTestMetric = new InputAction("testMetric", InputActionType.Button, $"<Keyboard>/{testKeyTestMetric}");
            actionTestAlarm = new InputAction("testAlarm", InputActionType.Button, $"<Keyboard>/{testKeyTestAlarm}");

            // Suscribirse a eventos
            actionPublishMetric.performed += _ => PublishTestMetric();
            actionListMetrics.performed += _ => ListMetrics();
            actionCreateAlarm.performed += _ => CreateTestAlarm();
            actionSendLog.performed += _ => SendTestLog();
            actionMonitoringLog.performed += _ => SendMonitoringLog();
            actionUIEvent.performed += _ => LogUIEvent();
            actionTestMetric.performed += _ => PublishBuiltInTestMetric();
            actionTestAlarm.performed += _ => CreateAndTriggerTestAlarm();

            // Habilitar todas las acciones
            actionPublishMetric.Enable();
            actionListMetrics.Enable();
            actionCreateAlarm.Enable();
            actionSendLog.Enable();
            actionMonitoringLog.Enable();
            actionUIEvent.Enable();
            actionTestMetric.Enable();
            actionTestAlarm.Enable();
        }

        private void SetupButtons()
        {
            if (_publishMetricButton != null)
                _publishMetricButton.onClick.AddListener(PublishTestMetric);

            if (_listMetricsButton != null)
                _listMetricsButton.onClick.AddListener(ListMetrics);

            if (_createAlarmButton != null)
                _createAlarmButton.onClick.AddListener(CreateTestAlarm);

            if (_sendLogButton != null)
                _sendLogButton.onClick.AddListener(SendTestLog);

            if (_monitoringLogButton != null)
                _monitoringLogButton.onClick.AddListener(SendMonitoringLog);

            if (_uiEventButton != null)
                _uiEventButton.onClick.AddListener(LogUIEvent);

            if (_testMetricButton != null)
                _testMetricButton.onClick.AddListener(PublishBuiltInTestMetric);

            if (_testAlarmButton != null)
                _testAlarmButton.onClick.AddListener(CreateAndTriggerTestAlarm);
        }

        private void ShowControlsHelp()
        {
            var controlsInfo = "📊 CloudWatch Test Controls:\n" +
                               $"[{testKeyPublishMetric}] - Publicar Métrica de Prueba\n" +
                               $"[{testKeyListMetrics}] - Listar Métricas\n" +
                               $"[{testKeyCreateAlarm}] - Crear Alarma de Prueba\n" +
                               $"[{testKeySendLog}] - Enviar Log de Prueba\n" +
                               $"[{testKeyMonitoringLog}] - Enviar Log de Monitoreo\n" +
                               $"[{testKeyUIEvent}] - Registrar Evento UI\n" +
                               $"[{testKeyTestMetric}] - Métrica Incorporada\n" +
                               $"[{testKeyTestAlarm}] - Crear y Activar Alarma";

            Debug.Log($"[CloudWatchTest] {controlsInfo}");
        }

        #region Test Methods - CloudWatch Metrics

        /// <summary>
        /// Publica una métrica de prueba personalizada
        /// </summary>
        public async void PublishTestMetric()
        {
            LogTest("Publicando métrica de prueba...");

            try
            {
                var cloudWatchManager = ServiceController.Instance?.CloudWatchManager;
                if (cloudWatchManager == null)
                {
                    LogTest("❌ CloudWatchManager no disponible");
                    return;
                }

                var success = await cloudWatchManager.PublishMetric(_testMetricName, _testMetricValue);

                if (success)
                {
                    LogTest($"✅ Métrica publicada: {_testMetricName} = {_testMetricValue}");
                }
                else
                {
                    LogTest("❌ Error al publicar métrica");
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción: {ex.Message}");
            }
        }

        /// <summary>
        /// Lista todas las métricas disponibles
        /// </summary>
        public async void ListMetrics()
        {
            LogTest("Listando métricas disponibles...");

            try
            {
                var cloudWatchManager = ServiceController.Instance?.CloudWatchManager;
                if (cloudWatchManager == null)
                {
                    LogTest("❌ CloudWatchManager no disponible");
                    return;
                }

                var metrics = await cloudWatchManager.ListMetrics();

                if (metrics.Count > 0)
                {
                    LogTest($"✅ Encontradas {metrics.Count} métricas:");

                    var metricsInfo = "";
                    foreach (var metric in metrics)
                    {
                        metricsInfo += $"• {metric.Name} (Namespace: {metric.Namespace})\n";
                        LogTest($"  • {metric.Name}");
                    }

                    if (_metricsText != null)
                        _metricsText.text = metricsInfo;
                }
                else
                {
                    LogTest("ℹ️ No se encontraron métricas");
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción: {ex.Message}");
            }
        }

        /// <summary>
        /// Usa la función incorporada de CloudWatch para test
        /// </summary>
        public async void PublishBuiltInTestMetric()
        {
            LogTest("Usando métrica de prueba incorporada...");

            try
            {
                var cloudWatchManager = ServiceController.Instance?.CloudWatchManager;
                if (cloudWatchManager?.Metrics == null)
                {
                    LogTest("❌ CloudWatch Metrics no disponible");
                    return;
                }

                var success = await cloudWatchManager.Metrics.PublishTestMetricAsync();

                if (success)
                {
                    LogTest("✅ Métrica de prueba incorporada publicada");
                }
                else
                {
                    LogTest("❌ Error en métrica de prueba incorporada");
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción: {ex.Message}");
            }
        }

        #endregion

        #region Test Methods - CloudWatch Alarms

        /// <summary>
        /// Crea una alarma de prueba
        /// </summary>
        public async void CreateTestAlarm()
        {
            LogTest("Creando alarma de prueba...");

            try
            {
                var cloudWatchManager = ServiceController.Instance?.CloudWatchManager;
                if (cloudWatchManager == null)
                {
                    LogTest("❌ CloudWatchManager no disponible");
                    return;
                }

                var success = await cloudWatchManager.CreateAlarm(
                    _testAlarmName,
                    _testMetricName,
                    _testAlarmThreshold,
                    "LessThanThreshold"
                );

                if (success)
                {
                    LogTest($"✅ Alarma creada: {_testAlarmName} para {_testMetricName} < {_testAlarmThreshold}");
                }
                else
                {
                    LogTest("❌ Error al crear alarma");
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea y activa una alarma usando funciones incorporadas
        /// </summary>
        public async void CreateAndTriggerTestAlarm()
        {
            LogTest("Creando y activando alarma de prueba...");

            try
            {
                var cloudWatchManager = ServiceController.Instance?.CloudWatchManager;
                if (cloudWatchManager?.Alarms == null || cloudWatchManager?.Metrics == null)
                {
                    LogTest("❌ CloudWatch Alarms/Metrics no disponible");
                    return;
                }

                // Crear alarma incorporada
                var createSuccess = await cloudWatchManager.Alarms.CreateTestAlarmAsync("TestMetric");

                if (createSuccess)
                {
                    LogTest("✅ Alarma de prueba creada");

                    // Activar alarma
                    var triggerSuccess =
                        await cloudWatchManager.Alarms.TriggerTestAlarmAsync("TestMetric", cloudWatchManager.Metrics);

                    if (triggerSuccess)
                    {
                        LogTest("✅ Alarma activada con valor bajo");
                    }
                    else
                    {
                        LogTest("❌ Error al activar alarma");
                    }
                }
                else
                {
                    LogTest("❌ Error al crear alarma de prueba");
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción: {ex.Message}");
            }
        }

        #endregion

        #region Test Methods - CloudWatch Logs

        /// <summary>
        /// Envía un log de prueba básico
        /// </summary>
        public async void SendTestLog()
        {
            LogTest("Enviando log de prueba...");

            try
            {
                var cloudWatchManager = ServiceController.Instance?.CloudWatchManager;
                if (cloudWatchManager == null)
                {
                    LogTest("❌ CloudWatchManager no disponible");
                    return;
                }

                var testMessage = $"{_testLogMessage} - {System.DateTime.Now:HH:mm:ss}";
                var success = await cloudWatchManager.SendLog(testMessage, "INFO");

                if (success)
                {
                    LogTest($"✅ Log enviado: {testMessage}");
                }
                else
                {
                    LogTest("❌ Error al enviar log");
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción: {ex.Message}");
            }
        }

        /// <summary>
        /// Envía un log de monitoreo de aplicación
        /// </summary>
        public async void SendMonitoringLog()
        {
            LogTest("Enviando log de monitoreo...");

            try
            {
                var cloudWatchManager = ServiceController.Instance?.CloudWatchManager;
                if (cloudWatchManager == null)
                {
                    LogTest("❌ CloudWatchManager no disponible");
                    return;
                }

                var eventType = _testEventTypes[Random.Range(0, _testEventTypes.Count)];
                var eventData = $"Test {eventType} event at {System.DateTime.Now:HH:mm:ss}";

                var success = await cloudWatchManager.SendAppMonitoringLog(eventType, eventData, null, "INFO");

                if (success)
                {
                    LogTest($"✅ Log de monitoreo enviado: {eventType}");
                }
                else
                {
                    LogTest("❌ Error al enviar log de monitoreo");
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra un evento de UI
        /// </summary>
        public async void LogUIEvent()
        {
            LogTest("Registrando evento UI...");

            try
            {
                var cloudWatchManager = ServiceController.Instance?.CloudWatchManager;
                if (cloudWatchManager == null)
                {
                    LogTest("❌ CloudWatchManager no disponible");
                    return;
                }

                var eventDescription = $"User clicked CloudWatch test button at {System.DateTime.Now:HH:mm:ss}";
                var success = await cloudWatchManager.LogUIEvent(eventDescription);

                if (success)
                {
                    LogTest($"✅ Evento UI registrado: {eventDescription}");
                }
                else
                {
                    LogTest("❌ Error al registrar evento UI");
                }
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Excepción: {ex.Message}");
            }
        }

        #endregion

        #region Métodos de Conveniencia

        /// <summary>
        /// Ejecuta una prueba completa de CloudWatch
        /// </summary>
        public async void RunFullCloudWatchTest()
        {
            LogTest("🧪 Ejecutando prueba completa de CloudWatch...");

            // Esperar entre cada operación
            await System.Threading.Tasks.Task.Delay(1000);
            PublishTestMetric();

            await System.Threading.Tasks.Task.Delay(1000);
            SendTestLog();

            await System.Threading.Tasks.Task.Delay(1000);
            SendMonitoringLog();

            await System.Threading.Tasks.Task.Delay(1000);
            ListMetrics();

            await System.Threading.Tasks.Task.Delay(1000);
            CreateTestAlarm();

            LogTest("✅ Prueba completa de CloudWatch finalizada");
        }

        /// <summary>
        /// Verifica el estado de CloudWatch
        /// </summary>
        public void CheckCloudWatchStatus()
        {
            try
            {
                var serviceController = ServiceController.Instance;
                var isAvailable = serviceController?.IsServiceAvailable("CloudWatchManager") ?? false;
                var cloudWatchManager = serviceController?.CloudWatchManager;

                var statusInfo = $"Estado CloudWatch: {(isAvailable ? "✅ Disponible" : "❌ No disponible")}\n" +
                                 $"Manager: {(cloudWatchManager != null ? "✅ Inicializado" : "❌ No inicializado")}\n" +
                                 $"Métricas: {(cloudWatchManager?.Metrics != null ? "✅ Disponibles" : "❌ No disponibles")}\n" +
                                 $"Alarmas: {(cloudWatchManager?.Alarms != null ? "✅ Disponibles" : "❌ No disponibles")}\n" +
                                 $"Logs: {(cloudWatchManager?.Logs != null ? "✅ Disponibles" : "❌ No disponibles")}";

                LogTest(statusInfo);
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Error verificando estado: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private void OnServiceActivated(string serviceName)
        {
            if (serviceName == "CloudWatchManager")
            {
                LogTest("🚀 CloudWatchManager activado y listo para usar");
                UpdateStatusDisplay();
            }
        }

        private void OnServiceError(string serviceName, string error)
        {
            if (serviceName == "CloudWatchManager")
            {
                LogTest($"❌ Error en CloudWatchManager: {error}");
                UpdateStatusDisplay();
            }
        }

        #endregion

        #region UI Updates

        private void UpdateStatusDisplay()
        {
            if (_statusText == null) return;

            try
            {
                var serviceController = ServiceController.Instance;
                var isAvailable = serviceController?.IsServiceAvailable("CloudWatchManager") ?? false;
                var userInfo = serviceController?.GetUserInfo() ?? ("", "", false);

                var statusInfo = $"Estado CloudWatch: {(isAvailable ? "✅ Disponible" : "❌ No disponible")}\n" +
                                 $"Usuario: {userInfo.username}\n" +
                                 $"Autenticado: {(userInfo.isAuthenticated ? "✅ Sí" : "❌ No")}";

                _statusText.text = statusInfo;
            }
            catch (System.Exception ex)
            {
                _statusText.text = $"Error actualizando estado: {ex.Message}";
            }
        }

        #endregion

        #region Logging

        private void LogTest(string message)
        {
            Debug.Log($"[CloudWatchTest] {message}");

            // También mostrar en UI si hay text component
            if (_logsText != null)
            {
                _logsText.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Desuscribirse de eventos
            if (ServiceController.Instance != null)
            {
                ServiceController.Instance.OnServiceActivated -= OnServiceActivated;
                ServiceController.Instance.OnServiceError -= OnServiceError;
            }

            // Limpiar Input Actions
            CleanupInputActions();
        }

        private void CleanupInputActions()
        {
            // Deshabilitar y limpiar todas las acciones
            actionPublishMetric?.Disable();
            actionListMetrics?.Disable();
            actionCreateAlarm?.Disable();
            actionSendLog?.Disable();
            actionMonitoringLog?.Disable();
            actionUIEvent?.Disable();
            actionTestMetric?.Disable();
            actionTestAlarm?.Disable();

            actionPublishMetric?.Dispose();
            actionListMetrics?.Dispose();
            actionCreateAlarm?.Dispose();
            actionSendLog?.Dispose();
            actionMonitoringLog?.Dispose();
            actionUIEvent?.Dispose();
            actionTestMetric?.Dispose();
            actionTestAlarm?.Dispose();
        }

        #endregion

        #region Editor Testing

#if UNITY_EDITOR
        [Header("Editor Testing")] [SerializeField]
        private bool _enableEditorTesting = true;

        [ContextMenu("Test - Publish Metric")]
        private void EditorTestPublishMetric()
        {
            if (_enableEditorTesting && Application.isPlaying)
                PublishTestMetric();
        }

        [ContextMenu("Test - List Metrics")]
        private void EditorTestListMetrics()
        {
            if (_enableEditorTesting && Application.isPlaying)
                ListMetrics();
        }

        [ContextMenu("Test - Send Log")]
        private void EditorTestSendLog()
        {
            if (_enableEditorTesting && Application.isPlaying)
                SendTestLog();
        }

        [ContextMenu("Test - Check Status")]
        private void EditorTestCheckStatus()
        {
            if (_enableEditorTesting && Application.isPlaying)
                CheckCloudWatchStatus();
        }

        [ContextMenu("Test - Full Test")]
        private void EditorTestFull()
        {
            if (_enableEditorTesting && Application.isPlaying)
                RunFullCloudWatchTest();
        }
#endif

        #endregion
    }
}