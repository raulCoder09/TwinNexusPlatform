using _Scripts.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using Newtonsoft.Json;

namespace _Scripts.Testing
{
    public class LambdaTest : MonoBehaviour
    {
        private InputAction _testAction;
        [SerializeField] private string functionName;
        private InputAction _executeFunctionLambda;

        void Start()
        {
            _testAction = new InputAction("TestLambda", InputActionType.Button, "<Keyboard>/t");
            _testAction.performed += OnTestKeyPressed;
            _testAction.Enable();
            
            _executeFunctionLambda=new InputAction("ExecuteFunctionLambda", InputActionType.Button, "<Keyboard>/f");
            _executeFunctionLambda.performed += OnExecuteFunctionLambdaKeyPressed;
            _executeFunctionLambda.Enable();
            
            if (LambdaManager.Instance != null)
            {
                LambdaManager.Instance.OnLambdaExecutionComplete += OnLambdaResult;
            }
        }

        void OnDestroy()
        {
            if (_testAction != null)
            {
                _testAction.performed -= OnTestKeyPressed;
                _testAction.Disable();
                _testAction.Dispose();
            }
            if (_executeFunctionLambda != null)
            {
                _executeFunctionLambda.performed -= OnExecuteFunctionLambdaKeyPressed;
                _executeFunctionLambda.Disable();
                _executeFunctionLambda.Dispose();
            }
        }

        private void OnTestKeyPressed(InputAction.CallbackContext context)
        {
            ExecuteTestFunctionLambda();
        }
        
        private void OnExecuteFunctionLambdaKeyPressed(InputAction.CallbackContext context)
        {
            ExecuteFunctionLambda();
        }

        async void ExecuteTestFunctionLambda()
        {
            if (LambdaManager.Instance == null)
            {
                return;
            }
            bool success = await LambdaManager.Instance.ExecuteDefaultFunctionAsync();
        }
        
        async void ExecuteFunctionLambda()
        {
            if (LambdaManager.Instance == null)
            {
                return;
            }
            bool success = await LambdaManager.Instance.ExecuteLambdaFunctionAsync(functionName, new { test = "Hola desde Unity!" });
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