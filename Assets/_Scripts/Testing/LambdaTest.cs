using _Scripts.Models;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.Testing
{
    public class LambdaTest : MonoBehaviour
    {
        private InputAction testAction;

        void Start()
        {
            // Configurar la acción de input
            testAction = new InputAction("TestLambda", InputActionType.Button, "<Keyboard>/t");
            testAction.performed += OnTestKeyPressed;
            testAction.Enable();

            // Suscribirse al evento de LambdaManager para ver los resultados
            if (LambdaManager.Instance != null)
            {
                LambdaManager.Instance.OnLambdaExecutionComplete += OnLambdaResult;
            }
            else
            {
                Debug.LogError("LambdaManager no encontrado en la escena.");
            }
        }

        void OnDestroy()
        {
            // Limpiar la acción de input
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
            
            bool success = await LambdaManager.Instance.ExecuteDefaultFunctionAsync();
        }



        void OnLambdaResult(bool success, string message, object result)
        {
            if (result != null)
            {
                Debug.Log($"Respondiendo a rulo con: {JsonUtility.ToJson(result)}");
            }
        }
    }
}