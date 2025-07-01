using System.Text.RegularExpressions;

namespace _Scripts.Controllers.WelcomeController
{
    public static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, emailPattern);
        }

        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Patrón para números de teléfono internacionales (debe empezar con +)
            var phonePattern = @"^\+[1-9]\d{1,14}$";
            return Regex.IsMatch(phoneNumber.Replace(" ", "").Replace("-", ""), phonePattern);
        }

        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Al menos 8 caracteres, una mayúscula, una minúscula y un número
            return password.Length >= 8 &&
                   Regex.IsMatch(password, @"[A-Z]") &&
                   Regex.IsMatch(password, @"[a-z]") &&
                   Regex.IsMatch(password, @"[0-9]");
        }

        public static bool IsValidVerificationCode(string verificationCode)
        {
            if (string.IsNullOrWhiteSpace(verificationCode))
                return false;

            // Código de verificación de 6 dígitos
            var codePattern = @"^\d{6}$";
            return Regex.IsMatch(verificationCode, codePattern);
        }
    }
}