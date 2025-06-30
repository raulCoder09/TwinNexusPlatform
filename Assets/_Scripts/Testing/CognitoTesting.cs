using Amazon;

namespace _Scripts.Models.CognitoManagement
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using _Scripts.Models.CognitoManagement;

    public class CognitoTesting : CognitoManager
    {
        [Header("Input Configuration")]
        [SerializeField] private string testKeySignIn = "l"; // Tecla para login
        [SerializeField] private string testKeySignUp = "r"; // Tecla para registro
        [SerializeField] private string testKeyConfirmSignUp = "v"; // Tecla para verificar email
        [SerializeField] private string testKeyForgotPassword = "f"; // Tecla para recuperar contraseña
        [SerializeField] private string testKeySignOut = "o"; // Tecla para cerrar sesión
        [SerializeField] private string testKeyResendCode = "e"; // Tecla para reintentar código

        [Header("Test Configuration")]
        [SerializeField] private string userPoolId; // Sobrescribe el predeterminado
        [SerializeField] private string clientId; // Sobrescribe el predeterminado
        [SerializeField] private string identityPoolId; // Sobrescribe el predeterminado
        [SerializeField] private RegionEndpoint awsRegion; // Sobrescribe el predeterminado
        [SerializeField] private string testUsername = "testuser"; // Usuario de prueba
        [SerializeField] private string testPassword = "TestPass123!"; // Contraseña de prueba
        [SerializeField] private string testEmail = "test@example.com"; // Email de prueba
        [SerializeField] private string testPhoneNumber = "+12025550123"; // Número de teléfono (opcional)
        [SerializeField] private string testConfirmationCode = "123456"; // Código de verificación de prueba
        [SerializeField] private string testNewPassword = "NewPass123!"; // Nueva contraseña para recuperación

        private InputAction actionSignIn;
        private InputAction actionSignUp;
        private InputAction actionConfirmSignUp;
        private InputAction actionForgotPassword;
        private InputAction actionSignOut;
        private InputAction actionResendCode;

        private void Awake()
        {
            // Sobrescribir valores predeterminados con los del Inspector
            _userPoolId = userPoolId;
            _clientId = clientId;
            _identityPoolId = identityPoolId;
            _regionEndpoint = awsRegion;
        }

        private void OnEnable()
        {
            // Configurar acciones de entrada
            actionSignIn = new InputAction("signIn", InputActionType.Button, $"<Keyboard>/{testKeySignIn}");
            actionSignIn.performed += OnSignInPerformed;
            actionSignIn.Enable();

            actionSignUp = new InputAction("signUp", InputActionType.Button, $"<Keyboard>/{testKeySignUp}");
            actionSignUp.performed += OnSignUpPerformed;
            actionSignUp.Enable();

            actionConfirmSignUp = new InputAction("confirmSignUp", InputActionType.Button, $"<Keyboard>/{testKeyConfirmSignUp}");
            actionConfirmSignUp.performed += OnConfirmSignUpPerformed;
            actionConfirmSignUp.Enable();

            actionForgotPassword = new InputAction("forgotPassword", InputActionType.Button, $"<Keyboard>/{testKeyForgotPassword}");
            actionForgotPassword.performed += OnForgotPasswordPerformed;
            actionForgotPassword.Enable();

            actionSignOut = new InputAction("signOut", InputActionType.Button, $"<Keyboard>/{testKeySignOut}");
            actionSignOut.performed += OnSignOutPerformed;
            actionSignOut.Enable();

            actionResendCode = new InputAction("resendCode", InputActionType.Button, $"<Keyboard>/{testKeyResendCode}");
            actionResendCode.performed += OnResendCodePerformed;
            actionResendCode.Enable();
        }

        private void OnDisable()
        {
            // Desactivar acciones - verificar que no sean null antes de desuscribirse
            if (actionSignIn != null)
            {
                actionSignIn.performed -= OnSignInPerformed;
                actionSignIn.Disable();
                actionSignIn.Dispose();
            }

            if (actionSignUp != null)
            {
                actionSignUp.performed -= OnSignUpPerformed;
                actionSignUp.Disable();
                actionSignUp.Dispose();
            }

            if (actionConfirmSignUp != null)
            {
                actionConfirmSignUp.performed -= OnConfirmSignUpPerformed;
                actionConfirmSignUp.Disable();
                actionConfirmSignUp.Dispose();
            }

            if (actionForgotPassword != null)
            {
                actionForgotPassword.performed -= OnForgotPasswordPerformed;
                actionForgotPassword.Disable();
                actionForgotPassword.Dispose();
            }

            if (actionSignOut != null)
            {
                actionSignOut.performed -= OnSignOutPerformed;
                actionSignOut.Disable();
                actionSignOut.Dispose();
            }

            if (actionResendCode != null)
            {
                actionResendCode.performed -= OnResendCodePerformed;
                actionResendCode.Disable();
                actionResendCode.Dispose();
            }
        }

        private async void OnSignInPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[CognitoTesting] Testing sign-in...");
            var success = await SignInAsync(testUsername, testPassword);
            Debug.Log($"[CognitoTesting] Sign-in: {(success ? "Success" : "Failed")}");
        }

        private async void OnSignUpPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[CognitoTesting] Testing sign-up...");
            var success = await SignUpAsync(testUsername, testPassword, testEmail, testPhoneNumber);
            Debug.Log($"[CognitoTesting] Sign-up: {(success ? "Success" : "Failed")}");
        }

        private async void OnConfirmSignUpPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[CognitoTesting] Testing email verification...");
            var success = await ConfirmSignUpAsync(testUsername, testConfirmationCode);
            Debug.Log($"[CognitoTesting] Email verification: {(success ? "Success" : "Failed")}");
        }

        private async void OnForgotPasswordPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[CognitoTesting] Testing forgot password...");
            var success = await ForgotPasswordAsync(testUsername);
            Debug.Log($"[CognitoTesting] Forgot password: {(success ? "Success" : "Failed")}");
        }

        private async void OnSignOutPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[CognitoTesting] Testing sign-out...");
            SignOut();
            Debug.Log("[CognitoTesting] Sign-out: Success");
        }

        private async void OnResendCodePerformed(InputAction.CallbackContext context)
        {
            Debug.Log("[CognitoTesting] Testing resend verification code...");
            var success = await ResendConfirmationCodeAsync();
            Debug.Log($"[CognitoTesting] Resend verification code: {(success ? "Success" : "Failed")}");
        }
    }
}