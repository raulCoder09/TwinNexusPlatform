using _Scripts.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using Newtonsoft.Json;

namespace _Scripts.Testing
{
    public class LambdaTest : MonoBehaviour
    {
        private InputAction testAction;

        void Start()
        {
            testAction = new InputAction("TestLambda", InputActionType.Button, "<Keyboard>/t");
            testAction.performed += OnTestKeyPressed;
            testAction.Enable();
            
            if (LambdaManager.Instance != null)
            {
                LambdaManager.Instance.OnLambdaExecutionComplete += OnLambdaResult;
            }
        }

        void OnDestroy()
        {
            if (testAction != null)
            {
                testAction.performed -= OnTestKeyPressed;
                testAction.Disable();
                testAction.Dispose();
            }
        }

        private void OnTestKeyPressed(InputAction.CallbackContext context)
        {
            ExecuteHelloWorldFunction();
        }

        async void ExecuteHelloWorldFunction()
        {
            if (LambdaManager.Instance == null)
            {
                return;
            }
            // bool success = await LambdaManager.Instance.ExecuteDefaultFunctionAsync();
            bool success = await LambdaManager.Instance.ExecuteLambdaFunctionAsync("TwinNexusPlatform-LambdaTest", new { test = "Hola desde Unity!" });
        }

        void OnLambdaResult(bool success, string message, object result)
        {
            if (result != null)
            {
                try
                {
                    string resultJson = JsonConvert.SerializeObject(result, Formatting.Indented);
                    var jsonObject = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(resultJson);
                    
                    if (jsonObject.ContainsKey("data"))
                    {
                        var dataObject = jsonObject["data"] as Newtonsoft.Json.Linq.JObject;
                        if (dataObject != null)
                        {
                            if (dataObject.ContainsKey("message"))
                            {
                                string lambdaMessage = dataObject["message"].ToString();
                                Debug.Log($"💬 Mensaje: {lambdaMessage}");
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {

                }
            }
        }
    }
}