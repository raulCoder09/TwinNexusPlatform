// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.InputSystem;
// using _Scripts.Models;
// using _Scripts.Models.IoTCoreManagement;
//
// namespace _Scripts.Testing
// {
//     public class IoTCoreTesting : MonoBehaviour
//     {
//         private InputAction listThingsAction;
//         private InputAction testConnectivityAction;
//         private InputAction createThingAction;
//         private InputAction getThingAction;
//         private InputAction createCertificateAction;
//         private InputAction attachCertificateAction;
//         private InputAction listCertificatesAction;
//         private InputAction createPolicyAction;
//         private InputAction attachPolicyAction;
//
//         void Start()
//         {
//             
//             // Initialize input actions
//             listThingsAction = new InputAction("ListThings", InputActionType.Button, "<Keyboard>/l");
//             testConnectivityAction = new InputAction("TestConnectivity", InputActionType.Button, "<Keyboard>/k");
//             createThingAction = new InputAction("CreateThing", InputActionType.Button, "<Keyboard>/c");
//             getThingAction = new InputAction("GetThing", InputActionType.Button, "<Keyboard>/g");
//             createCertificateAction = new InputAction("CreateCertificate", InputActionType.Button, "<Keyboard>/t");
//             attachCertificateAction = new InputAction("AttachCertificate", InputActionType.Button, "<Keyboard>/a");
//             listCertificatesAction = new InputAction("ListCertificates", InputActionType.Button, "<Keyboard>/r");
//             createPolicyAction = new InputAction("CreatePolicy", InputActionType.Button, "<Keyboard>/p");
//             attachPolicyAction = new InputAction("AttachPolicy", InputActionType.Button, "<Keyboard>/s");
//             
//             // Register callbacks
//             listThingsAction.performed += OnListThingsPressed;
//             testConnectivityAction.performed += OnTestConnectivityPressed;
//             createThingAction.performed += OnCreateThingPressed;
//             getThingAction.performed += OnGetThingPressed;
//             createCertificateAction.performed += OnCreateCertificatePressed;
//             attachCertificateAction.performed += OnAttachCertificatePressed;
//             listCertificatesAction.performed += OnListCertificatesPressed;
//             createPolicyAction.performed += OnCreatePolicyPressed;
//             attachPolicyAction.performed += OnAttachPolicyPressed;
//             
//             // Enable actions
//             listThingsAction.Enable();
//             testConnectivityAction.Enable();
//             createThingAction.Enable();
//             getThingAction.Enable();
//             createCertificateAction.Enable();
//             attachCertificateAction.Enable();
//             listCertificatesAction.Enable();
//             createPolicyAction.Enable();
//             attachPolicyAction.Enable();
//             
//             // Subscribe to IoT Core events
//             if (IoTCoreManager.Instance != null)
//             {
//                 IoTCoreManager.Instance.OnThingsListed += OnThingsListedResult;
//                 IoTCoreManager.Instance.OnThingCreated += OnThingCreatedResult;
//                 IoTCoreManager.Instance.OnThingRetrieved += OnThingRetrievedResult;
//                 IoTCoreManager.Instance.OnCertificateCreated += OnCertificateCreatedResult;
//                 IoTCoreManager.Instance.OnCertificateAttached += OnCertificateAttachedResult;
//                 IoTCoreManager.Instance.OnThingCertificatesListed += OnThingCertificatesListedResult;
//                 IoTCoreManager.Instance.OnPolicyCreated += OnPolicyCreatedResult;
//                 IoTCoreManager.Instance.OnPolicyAttached += OnPolicyAttachedResult;
//             }
//             
//             Debug.Log("🚀 IoTCoreTesting iniciado:");
//             Debug.Log("📋 Presiona 'L' para listar Things de IoT Core");
//             Debug.Log("🔗 Presiona 'K' para test de conectividad IoT");
//             Debug.Log("📱 Presiona 'C' para crear Thing (configurado en Inspector)");
//             Debug.Log("🔍 Presiona 'G' para obtener detalles de Thing (configurado en Inspector)");
//             Debug.Log("🔐 Presiona 'T' para crear cerTificado X.509 (Certificate)");
//             Debug.Log("🔗 Presiona 'A' para Asociar certificado a Thing (configurado en Inspector)");
//             Debug.Log("📋 Presiona 'R' para listaR certificados de Thing (configurado en Inspector)");
//             Debug.Log("🛡️ Presiona 'P' para crear Política IoT (configurado en Inspector)");
//             Debug.Log("🔗 Presiona 'S' para aSociar política a certificado (configurado en Inspector)");
//             Debug.Log("⚙️ Asegúrate de estar autenticado primero");
//         }
//
//         void OnDestroy()
//         {
//             if (listThingsAction != null)
//             {
//                 listThingsAction.performed -= OnListThingsPressed;
//                 listThingsAction.Disable();
//                 listThingsAction.Dispose();
//             }
//             
//             if (testConnectivityAction != null)
//             {
//                 testConnectivityAction.performed -= OnTestConnectivityPressed;
//                 testConnectivityAction.Disable();
//                 testConnectivityAction.Dispose();
//             }
//             
//             if (createThingAction != null)
//             {
//                 createThingAction.performed -= OnCreateThingPressed;
//                 createThingAction.Disable();
//                 createThingAction.Dispose();
//             }
//             
//             if (getThingAction != null)
//             {
//                 getThingAction.performed -= OnGetThingPressed;
//                 getThingAction.Disable();
//                 getThingAction.Dispose();
//             }
//             
//             if (createCertificateAction != null)
//             {
//                 createCertificateAction.performed -= OnCreateCertificatePressed;
//                 createCertificateAction.Disable();
//                 createCertificateAction.Dispose();
//             }
//             
//             if (attachCertificateAction != null)
//             {
//                 attachCertificateAction.performed -= OnAttachCertificatePressed;
//                 attachCertificateAction.Disable();
//                 attachCertificateAction.Dispose();
//             }
//             
//             if (listCertificatesAction != null)
//             {
//                 listCertificatesAction.performed -= OnListCertificatesPressed;
//                 listCertificatesAction.Disable();
//                 listCertificatesAction.Dispose();
//             }
//             
//             if (createPolicyAction != null)
//             {
//                 createPolicyAction.performed -= OnCreatePolicyPressed;
//                 createPolicyAction.Disable();
//                 createPolicyAction.Dispose();
//             }
//             
//             if (attachPolicyAction != null)
//             {
//                 attachPolicyAction.performed -= OnAttachPolicyPressed;
//                 attachPolicyAction.Disable();
//                 attachPolicyAction.Dispose();
//             }
//         }
//
//         #region Input Action Callbacks
//         
//
//         
//         private void OnListThingsPressed(InputAction.CallbackContext context)
//         {
//             ExecuteListThings();
//         }
//         
//         private void OnTestConnectivityPressed(InputAction.CallbackContext context)
//         {
//             ExecuteTestConnectivity();
//         }
//         
//         private void OnCreateThingPressed(InputAction.CallbackContext context)
//         {
//             ExecuteCreateThing();
//         }
//         
//         private void OnGetThingPressed(InputAction.CallbackContext context)
//         {
//             ExecuteGetThing();
//         }
//         
//         private void OnCreateCertificatePressed(InputAction.CallbackContext context)
//         {
//             ExecuteCreateCertificate();
//         }
//         
//         private void OnAttachCertificatePressed(InputAction.CallbackContext context)
//         {
//             ExecuteAttachCertificate();
//         }
//         
//         private void OnListCertificatesPressed(InputAction.CallbackContext context)
//         {
//             ExecuteListCertificates();
//         }
//
//         private void OnCreatePolicyPressed(InputAction.CallbackContext context)
//         {
//             ExecuteCreatePolicy();
//         }
//         
//         private void OnAttachPolicyPressed(InputAction.CallbackContext context)
//         {
//             ExecuteAttachPolicy();
//         }
//         
//         #endregion
//
//         #region Test Execution Methods
//         
//         
//         async void ExecuteListThings()
//         {
//             if (IoTCoreManager.Instance == null)
//             {
//                 Debug.LogError("IoTCoreManager no disponible.");
//                 return;
//             }
//             
//             Debug.Log("📋 Iniciando listado de IoT Things...");
//             var things = await IoTCoreManager.Instance.ListThingsAsync();
//             Debug.Log(things.Count > 0 ? "✅ Listado completado" : "❌ Listado falló o no hay Things");
//         }
//         
//         async void ExecuteTestConnectivity()
//         {
//             if (IoTCoreManager.Instance == null)
//             {
//                 Debug.LogError("IoTCoreManager no disponible.");
//                 return;
//             }
//             
//             Debug.Log("🔗 Iniciando test de conectividad IoT Core...");
//             bool success = await IoTCoreManager.Instance.TestIoTConnectivityAsync();
//             Debug.Log(success ? "✅ Conectividad IoT verificada" : "❌ Test de conectividad falló");
//         }
//         
//         async void ExecuteCreateThing()
//         {
//             if (IoTCoreManager.Instance == null)
//             {
//                 Debug.LogError("IoTCoreManager no disponible.");
//                 return;
//             }
//             
//             Debug.Log("📱 Iniciando creación de Thing...");
//             bool success = await IoTCoreManager.Instance.CreateConfiguredThingAsync();
//             Debug.Log(success ? "✅ Creación completada" : "❌ Creación falló");
//         }
//         
//         async void ExecuteGetThing()
//         {
//             if (IoTCoreManager.Instance == null)
//             {
//                 Debug.LogError("IoTCoreManager no disponible.");
//                 return;
//             }
//             
//             Debug.Log("🔍 Iniciando obtención de detalles de Thing...");
//             var thingInfo = await IoTCoreManager.Instance.GetConfiguredThingAsync();
//             Debug.Log(thingInfo != null ? "✅ Detalles obtenidos" : "❌ Obtención falló");
//         }
//         
//         async void ExecuteCreateCertificate()
//         {
//             if (IoTCoreManager.Instance == null)
//             {
//                 Debug.LogError("IoTCoreManager no disponible.");
//                 return;
//             }
//             
//             Debug.Log("🔐 Iniciando creación de certificado X.509...");
//             Debug.LogWarning("⚠️ IMPORTANTE: La clave privada solo se mostrará UNA VEZ!");
//             var certificateData = await IoTCoreManager.Instance.CreateConfiguredCertificateAsync();
//             Debug.Log(certificateData != null ? "✅ Certificado creado" : "❌ Creación de certificado falló");
//         }
//         
//         async void ExecuteAttachCertificate()
//         {
//             if (IoTCoreManager.Instance == null)
//             {
//                 Debug.LogError("IoTCoreManager no disponible.");
//                 return;
//             }
//     
//             Debug.Log("🔗 Iniciando asociación de certificado a Thing...");
//             bool success = await IoTCoreManager.Instance.AttachConfiguredCertificateAsync();
//             Debug.Log(success ? "✅ Certificado asociado" : "❌ Asociación falló");
//         }
//         
//         async void ExecuteListCertificates()
//         {
//             if (IoTCoreManager.Instance == null)
//             {
//                 Debug.LogError("IoTCoreManager no disponible.");
//                 return;
//             }
//     
//             Debug.Log("📋 Iniciando listado de certificados de Thing...");
//             var certificates = await IoTCoreManager.Instance.ListConfiguredThingCertificatesAsync();
//             Debug.Log(certificates.Count > 0 ? "✅ Certificados listados" : "❌ No hay certificados o listado falló");
//         }
//
//         async void ExecuteCreatePolicy()
//         {
//             if (IoTCoreManager.Instance == null)
//             {
//                 Debug.LogError("IoTCoreManager no disponible.");
//                 return;
//             }
//     
//             Debug.Log("🛡️ Iniciando creación de política IoT...");
//             bool success = await IoTCoreManager.Instance.CreateConfiguredPolicyAsync();
//             Debug.Log(success ? "✅ Política creada" : "❌ Creación de política falló");
//         }
//         
//         async void ExecuteAttachPolicy()
//         {
//             if (IoTCoreManager.Instance == null)
//             {
//                 Debug.LogError("IoTCoreManager no disponible.");
//                 return;
//             }
//     
//             Debug.Log("🔗 Iniciando asociación de política a certificado...");
//             bool success = await IoTCoreManager.Instance.AttachConfiguredPolicyAsync();
//             Debug.Log(success ? "✅ Política asociada" : "❌ Asociación de política falló");
//         }
//         
//         #endregion
//
//         #region Event Handlers
//         
//
//         
//         void OnThingsListedResult(bool success, string message, List<ThingInfo> things)
//         {
//             if (success)
//             {
//                 Debug.Log($"🎉 ¡Things listados exitosamente!");
//                 Debug.Log($"📊 Total encontrados: {things?.Count ?? 0}");
//                 Debug.Log($"💬 Mensaje: {message}");
//                 
//                 if (things != null && things.Count > 0)
//                 {
//                     Debug.Log("📱 Resumen de IoT Things:");
//                     for (int i = 0; i < Math.Min(things.Count, 5); i++) // Solo mostrar los primeros 5
//                     {
//                         var thing = things[i];
//                         Debug.Log($"   {i + 1}. {thing.ThingName}");
//                         if (!string.IsNullOrEmpty(thing.ThingTypeName))
//                         {
//                             Debug.Log($"      Type: {thing.ThingTypeName}");
//                         }
//                     }
//                     
//                     if (things.Count > 5)
//                     {
//                         Debug.Log($"   ... y {things.Count - 5} Things más");
//                     }
//                     
//                     Debug.Log($"🔍 Ve a AWS Console → IoT Core → Things para ver detalles completos");
//                 }
//                 else
//                 {
//                     Debug.Log("📭 No se encontraron Things en el registro IoT");
//                     Debug.Log("💡 Crea algunos Things en AWS Console primero");
//                 }
//             }
//             else
//             {
//                 Debug.LogError($"❌ Error listando Things: {message}");
//             }
//         }
//         
//         void OnThingCreatedResult(bool success, string message, string thingName)
//         {
//             if (success)
//             {
//                 Debug.Log($"🎉 ¡Thing creado exitosamente!");
//                 Debug.Log($"📱 Thing: {thingName}");
//                 Debug.Log($"💬 Mensaje: {message}");
//                 Debug.Log($"🔍 Ve a AWS Console → IoT Core → Things para verlo");
//             }
//             else
//             {
//                 Debug.LogError($"❌ Error creando Thing: {message}");
//                 Debug.LogError($"📱 Thing: {thingName}");
//             }
//         }
//         
//         void OnThingRetrievedResult(bool success, string message, ThingInfo thingInfo)
//         {
//             if (success && thingInfo != null)
//             {
//                 Debug.Log($"🎉 ¡Detalles de Thing obtenidos exitosamente!");
//                 Debug.Log($"📱 Thing: {thingInfo.ThingName}");
//                 Debug.Log($"🏷️ Tipo: {thingInfo.ThingTypeName ?? "Sin tipo"}");
//                 Debug.Log($"🔢 Versión: {thingInfo.Version}");
//                 Debug.Log($"💬 Mensaje: {message}");
//                 
//                 if (!string.IsNullOrEmpty(thingInfo.AttributesInfo))
//                 {
//                     Debug.Log($"📋 Atributos: {thingInfo.AttributesInfo}");
//                 }
//                 
//                 Debug.Log($"📍 ARN completo: {thingInfo.ThingArn}");
//             }
//             else
//             {
//                 Debug.LogError($"❌ Error obteniendo Thing: {message}");
//                 Debug.LogError("💡 Verifica que el nombre en 'thingNameToGet' sea correcto");
//             }
//         }
//         
//         void OnCertificateCreatedResult(bool success, string message, CertificateData certificateData)
//         {
//             if (success && certificateData != null)
//             {
//                 Debug.Log($"🎉 ¡Certificado X.509 creado exitosamente!");
//                 Debug.Log($"🆔 Certificate ID: {certificateData.CertificateId}");
//                 Debug.Log($"📍 Certificate ARN: {certificateData.CertificateArn}");
//                 Debug.Log($"🔓 Estado: {(certificateData.IsActive ? "ACTIVO" : "INACTIVO")}");
//                 Debug.Log($"💬 Mensaje: {message}");
//                 Debug.Log($"📅 Creado: {certificateData.CreationDate:yyyy-MM-dd HH:mm:ss}");
//                 
//                 // Show data lengths for verification (but NOT the actual keys!)
//                 Debug.Log($"📄 Datos del certificado:");
//                 Debug.Log($"   🔖 Certificate PEM: {certificateData.CertificatePem?.Length ?? 0} caracteres");
//                 Debug.Log($"   🔑 Public Key: {certificateData.PublicKey?.Length ?? 0} caracteres");
//                 Debug.Log($"   🔐 Private Key: {certificateData.PrivateKey?.Length ?? 0} caracteres");
//                 
//                 // Security warnings
//                 Debug.LogWarning("🚨 SEGURIDAD CRÍTICA:");
//                 Debug.LogWarning("   • La clave privada NO se puede recuperar después");
//                 Debug.LogWarning("   • Guarda TODOS los datos del certificado de forma segura");
//                 Debug.LogWarning("   • Nunca compartas la clave privada");
//                 
//                 // Next steps guidance
//                 Debug.Log("🎯 Próximos pasos recomendados:");
//                 Debug.Log("   1. Guardar certificado y claves en lugar seguro");
//                 Debug.Log("   2. Asociar certificado a un Thing específico");
//                 Debug.Log("   3. Configurar políticas de acceso");
//                 Debug.Log("   4. Probar conectividad MQTT");
//                 
//                 Debug.Log($"🔍 Ve a AWS Console → IoT Core → Security → Certificates para verificar");
//             }
//             else
//             {
//                 Debug.LogError($"❌ Error creando certificado: {message}");
//                 Debug.LogError("💡 Verifica permisos y configuración de AWS IoT Core");
//                 Debug.LogError("🔧 Soluciones posibles:");
//                 Debug.LogError("   • Verificar autenticación de AWS");
//                 Debug.LogError("   • Revisar políticas IAM");
//                 Debug.LogError("   • Comprobar límites de la cuenta AWS");
//             }
//         }
//         
//         void OnCertificateAttachedResult(bool success, string message, string thingName, string certificateId)
//         {
//             if (success)
//             {
//                 Debug.Log($"🎉 ¡Certificado asociado exitosamente!");
//                 Debug.Log($"📱 Thing: {thingName}");
//                 Debug.Log($"🆔 Certificate: {certificateId?.Substring(0, 8)}...");
//                 Debug.Log($"💬 Mensaje: {message}");
//                 Debug.Log($"🔐 Ahora {thingName} puede autenticarse con este certificado");
//                 Debug.Log($"🔍 Ve a AWS Console → IoT Core → Things → {thingName} → Security");
//             }
//             else
//             {
//                 Debug.LogError($"❌ Error asociando certificado: {message}");
//                 Debug.LogError($"📱 Thing: {thingName}");
//                 Debug.LogError($"🆔 Certificate: {certificateId}");
//                 Debug.LogError("💡 Verifica que el Certificate ID y Thing Name sean correctos en Inspector");
//             }
//         }
//         
//         void OnThingCertificatesListedResult(bool success, string message, string thingName, List<string> certificateArns)
//         {
//             if (success)
//             {
//                 Debug.Log($"🎉 ¡Certificados listados exitosamente!");
//                 Debug.Log($"📱 Thing: {thingName}");
//                 Debug.Log($"📊 Total certificados: {certificateArns?.Count ?? 0}");
//                 Debug.Log($"💬 Mensaje: {message}");
//         
//                 if (certificateArns != null && certificateArns.Count > 0)
//                 {
//                     Debug.Log("🔐 Certificados encontrados:");
//                     for (int i = 0; i < certificateArns.Count; i++)
//                     {
//                         string arn = certificateArns[i];
//                         string certId = arn.Split('/').Length > 1 ? arn.Split('/')[1] : "Unknown";
//                         Debug.Log($"   {i + 1}. 🆔 ID: {certId.Substring(0, Math.Min(12, certId.Length))}...");
//                         Debug.Log($"      📍 Status: ATTACHED");
//                     }
//                     Debug.Log($"🔍 Ve a AWS Console → IoT Core → Things → {thingName} → Security");
//                 }
//                 else
//                 {
//                     Debug.Log("📭 No hay certificados asociados a este Thing");
//                     Debug.Log("💡 Usa AttachCertificateToThingAsync() primero");
//                 }
//             }
//             else
//             {
//                 Debug.LogError($"❌ Error listando certificados: {message}");
//                 Debug.LogError($"📱 Thing: {thingName}");
//                 Debug.LogError("💡 Verifica que el nombre en 'thingNameToListCertificates' sea correcto");
//             }
//         }
//         
//         void OnPolicyCreatedResult(bool success, string message, string policyName)
//         {
//             if (success)
//             {
//                 Debug.Log($"🎉 ¡Política IoT creada exitosamente!");
//                 Debug.Log($"🛡️ Policy: {policyName}");
//                 Debug.Log($"💬 Mensaje: {message}");
//                 Debug.Log($"🔑 Permisos incluidos:");
//                 Debug.Log($"   • ✅ MQTT Connect");
//                 Debug.Log($"   • ✅ MQTT Publish/Subscribe");
//                 Debug.Log($"   • ✅ Thing Shadow operations");
//                 Debug.Log($"🎯 Próximo paso: Asociar política a certificado");
//                 Debug.Log($"🔍 Ve a AWS Console → IoT Core → Security → Policies");
//             }
//             else
//             {
//                 Debug.LogError($"❌ Error creando política: {message}");
//                 Debug.LogError($"🛡️ Policy: {policyName}");
//             }
//         }
//         
//         void OnPolicyAttachedResult(bool success, string message, string policyName, string certificateId)
//         {
//             if (success)
//             {
//                 Debug.Log($"🎉 ¡Política asociada exitosamente!");
//                 Debug.Log($"🛡️ Policy: {policyName}");
//                 Debug.Log($"🆔 Certificate: {certificateId?.Substring(0, 8)}...");
//                 Debug.Log($"💬 Mensaje: {message}");
//                 Debug.Log($"🔥 ¡El certificado ahora tiene permisos completos!");
//                 Debug.Log($"✅ Puede conectarse, publicar, suscribirse y usar Thing Shadow");
//                 Debug.Log($"🎯 Próximo paso: Probar MQTT o Thing Shadow");
//                 Debug.Log($"🔍 Ve a AWS Console → IoT Core → Security → Certificates → Policies");
//             }
//             else
//             {
//                 Debug.LogError($"❌ Error asociando política: {message}");
//                 Debug.LogError($"🛡️ Policy: {policyName}");
//                 Debug.LogError($"🆔 Certificate: {certificateId}");
//                 Debug.LogError("💡 Verifica que el Policy Name y Certificate ID sean correctos en Inspector");
//             }
//         }
//         
//         #endregion
//     }
// }