using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using _Scripts.Models;

namespace _Scripts.Testing
{
    public class IoTCoreTesting : MonoBehaviour
    {
        private InputAction listThingsAction;
        private InputAction testConnectivityAction;
        private InputAction createThingAction;
        private InputAction getThingAction;

        void Start()
        {
            // Initialize input actions
            listThingsAction = new InputAction("ListThings", InputActionType.Button, "<Keyboard>/l");
            testConnectivityAction = new InputAction("TestConnectivity", InputActionType.Button, "<Keyboard>/k");
            createThingAction = new InputAction("CreateThing", InputActionType.Button, "<Keyboard>/c");
            getThingAction = new InputAction("GetThing", InputActionType.Button, "<Keyboard>/g");
            
            // Register callbacks
            listThingsAction.performed += OnListThingsPressed;
            testConnectivityAction.performed += OnTestConnectivityPressed;
            createThingAction.performed += OnCreateThingPressed;
            getThingAction.performed += OnGetThingPressed;
            
            // Enable actions
            listThingsAction.Enable();
            testConnectivityAction.Enable();
            createThingAction.Enable();
            getThingAction.Enable();
            
            // Subscribe to IoT Core events
            if (IoTCoreManager.Instance != null)
            {
                IoTCoreManager.Instance.OnThingsListed += OnThingsListedResult;
                IoTCoreManager.Instance.OnThingCreated += OnThingCreatedResult;
                IoTCoreManager.Instance.OnThingRetrieved += OnThingRetrievedResult;
            }
            
            Debug.Log("🚀 IoTCoreTesting iniciado:");
            Debug.Log("📋 Presiona 'L' para listar Things de IoT Core");
            Debug.Log("🔗 Presiona 'K' para test de conectividad IoT");
            Debug.Log("📱 Presiona 'C' para crear Thing (configurado en Inspector)");
            Debug.Log("🔍 Presiona 'G' para obtener detalles de Thing (configurado en Inspector)");
            Debug.Log("⚙️ Asegúrate de estar autenticado primero");
        }

        void OnDestroy()
        {
            if (listThingsAction != null)
            {
                listThingsAction.performed -= OnListThingsPressed;
                listThingsAction.Disable();
                listThingsAction.Dispose();
            }
            
            if (testConnectivityAction != null)
            {
                testConnectivityAction.performed -= OnTestConnectivityPressed;
                testConnectivityAction.Disable();
                testConnectivityAction.Dispose();
            }
            
            if (createThingAction != null)
            {
                createThingAction.performed -= OnCreateThingPressed;
                createThingAction.Disable();
                createThingAction.Dispose();
            }
            
            if (getThingAction != null)
            {
                getThingAction.performed -= OnGetThingPressed;
                getThingAction.Disable();
                getThingAction.Dispose();
            }
        }

        #region Input Action Callbacks
        
        private void OnListThingsPressed(InputAction.CallbackContext context)
        {
            ExecuteListThings();
        }
        
        private void OnTestConnectivityPressed(InputAction.CallbackContext context)
        {
            ExecuteTestConnectivity();
        }
        
        private void OnCreateThingPressed(InputAction.CallbackContext context)
        {
            ExecuteCreateThing();
        }
        
        private void OnGetThingPressed(InputAction.CallbackContext context)
        {
            ExecuteGetThing();
        }
        
        #endregion

        #region Test Execution Methods
        
        async void ExecuteListThings()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }
            
            Debug.Log("📋 Iniciando listado de IoT Things...");
            var things = await IoTCoreManager.Instance.ListThingsAsync();
            Debug.Log(things.Count > 0 ? "✅ Listado completado" : "❌ Listado falló o no hay Things");
        }
        
        async void ExecuteTestConnectivity()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }
            
            Debug.Log("🔗 Iniciando test de conectividad IoT Core...");
            bool success = await IoTCoreManager.Instance.TestIoTConnectivityAsync();
            Debug.Log(success ? "✅ Conectividad IoT verificada" : "❌ Test de conectividad falló");
        }
        
        async void ExecuteCreateThing()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }
            
            Debug.Log("📱 Iniciando creación de Thing...");
            bool success = await IoTCoreManager.Instance.CreateConfiguredThingAsync();
            Debug.Log(success ? "✅ Creación completada" : "❌ Creación falló");
        }
        
        async void ExecuteGetThing()
        {
            if (IoTCoreManager.Instance == null)
            {
                Debug.LogError("IoTCoreManager no disponible.");
                return;
            }
            
            Debug.Log("🔍 Iniciando obtención de detalles de Thing...");
            var thingInfo = await IoTCoreManager.Instance.GetConfiguredThingAsync();
            Debug.Log(thingInfo != null ? "✅ Detalles obtenidos" : "❌ Obtención falló");
        }
        
        #endregion

        #region Event Handlers
        
        void OnThingsListedResult(bool success, string message, List<ThingInfo> things)
        {
            if (success)
            {
                Debug.Log($"🎉 ¡Things listados exitosamente!");
                Debug.Log($"📊 Total encontrados: {things?.Count ?? 0}");
                Debug.Log($"💬 Mensaje: {message}");
                
                if (things != null && things.Count > 0)
                {
                    Debug.Log("📱 Resumen de IoT Things:");
                    for (int i = 0; i < Math.Min(things.Count, 5); i++) // Solo mostrar los primeros 5
                    {
                        var thing = things[i];
                        Debug.Log($"   {i + 1}. {thing.ThingName}");
                        if (!string.IsNullOrEmpty(thing.ThingTypeName))
                        {
                            Debug.Log($"      Type: {thing.ThingTypeName}");
                        }
                    }
                    
                    if (things.Count > 5)
                    {
                        Debug.Log($"   ... y {things.Count - 5} Things más");
                    }
                    
                    Debug.Log($"🔍 Ve a AWS Console → IoT Core → Things para ver detalles completos");
                }
                else
                {
                    Debug.Log("📭 No se encontraron Things en el registro IoT");
                    Debug.Log("💡 Crea algunos Things en AWS Console primero");
                }
            }
            else
            {
                Debug.LogError($"❌ Error listando Things: {message}");
            }
        }
        
        void OnThingCreatedResult(bool success, string message, string thingName)
        {
            if (success)
            {
                Debug.Log($"🎉 ¡Thing creado exitosamente!");
                Debug.Log($"📱 Thing: {thingName}");
                Debug.Log($"💬 Mensaje: {message}");
                Debug.Log($"🔍 Ve a AWS Console → IoT Core → Things para verlo");
            }
            else
            {
                Debug.LogError($"❌ Error creando Thing: {message}");
                Debug.LogError($"📱 Thing: {thingName}");
            }
        }
        
        void OnThingRetrievedResult(bool success, string message, ThingInfo thingInfo)
        {
            if (success && thingInfo != null)
            {
                Debug.Log($"🎉 ¡Detalles de Thing obtenidos exitosamente!");
                Debug.Log($"📱 Thing: {thingInfo.ThingName}");
                Debug.Log($"🏷️ Tipo: {thingInfo.ThingTypeName ?? "Sin tipo"}");
                Debug.Log($"🔢 Versión: {thingInfo.Version}");
                Debug.Log($"💬 Mensaje: {message}");
                
                if (!string.IsNullOrEmpty(thingInfo.AttributesInfo))
                {
                    Debug.Log($"📋 Atributos: {thingInfo.AttributesInfo}");
                }
                
                Debug.Log($"📍 ARN completo: {thingInfo.ThingArn}");
            }
            else
            {
                Debug.LogError($"❌ Error obteniendo Thing: {message}");
                Debug.LogError("💡 Verifica que el nombre en 'thingNameToGet' sea correcto");
            }
        }
        #endregion
        
    }
}