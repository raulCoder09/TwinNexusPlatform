using Amazon;

namespace _Scripts.Models.CognitoManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Amazon.CognitoIdentityProvider;
    using Amazon.CognitoIdentityProvider.Model;
    using Amazon.Runtime;
    using UnityEngine;

    public class CognitoAuthHandler : ICognitoOperations
    {
        private readonly AmazonCognitoIdentityProviderClient _cognitoClient;
        private readonly string _userPoolId;
        private readonly string _clientId;
        private readonly bool _enableDebugLogs = true;

        // Evento para notificar tokens
        public event Action<string, string, string> OnTokensReceived;

        public CognitoAuthHandler(string userPoolId, string clientId, RegionEndpoint regionEndpoint)
        {
            _userPoolId = userPoolId ?? throw new ArgumentNullException(nameof(userPoolId));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _cognitoClient = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), regionEndpoint);
        }

        public async Task<bool> SignInAsync(string username, string password)
        {
            try
            {
                var authRequest = new InitiateAuthRequest
                {
                    ClientId = _clientId,
                    AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                    AuthParameters = new Dictionary<string, string>
                    {
                        { "USERNAME", username },
                        { "PASSWORD", password }
                    }
                };

                var response = await _cognitoClient.InitiateAuthAsync(authRequest);

                if (response.AuthenticationResult != null)
                {
                    LogDebug("Authentication successful");
                    OnTokensReceived?.Invoke(response.AuthenticationResult.IdToken, response.AuthenticationResult.AccessToken, response.AuthenticationResult.RefreshToken);
                    return true;
                }
                LogWarning("Authentication failed: No authentication result");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Authentication error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SignUpAsync(string username, string password, string email, string phoneNumber = null)
        {
            try
            {
                var userAttributes = new List<AttributeType>
                {
                    new AttributeType { Name = "email", Value = email },
                    new AttributeType { Name = "preferred_username", Value = username }
                };

                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    userAttributes.Add(new AttributeType { Name = "phone_number", Value = FormatPhoneNumber(phoneNumber) });
                }

                var signUpRequest = new SignUpRequest
                {
                    ClientId = _clientId,
                    Username = username,
                    Password = password,
                    UserAttributes = userAttributes
                };

                await _cognitoClient.SignUpAsync(signUpRequest);
                LogDebug($"Registration successful for {username}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Registration error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ConfirmSignUpAsync(string username, string confirmationCode)
        {
            try
            {
                var confirmRequest = new ConfirmSignUpRequest
                {
                    ClientId = _clientId,
                    Username = username,
                    ConfirmationCode = confirmationCode
                };

                await _cognitoClient.ConfirmSignUpAsync(confirmRequest);
                LogDebug("Email verification successful");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Email verification error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ForgotPasswordAsync(string username)
        {
            try
            {
                var forgotPasswordRequest = new ForgotPasswordRequest
                {
                    ClientId = _clientId,
                    Username = username
                };

                await _cognitoClient.ForgotPasswordAsync(forgotPasswordRequest);
                LogDebug("Password recovery email sent");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Password recovery error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ConfirmForgotPasswordAsync(string username, string confirmationCode, string newPassword)
        {
            try
            {
                var confirmRequest = new ConfirmForgotPasswordRequest
                {
                    ClientId = _clientId,
                    Username = username,
                    ConfirmationCode = confirmationCode,
                    Password = newPassword
                };

                await _cognitoClient.ConfirmForgotPasswordAsync(confirmRequest);
                LogDebug("Password reset successful");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Password reset error: {ex.Message}");
                return false;
            }
        }

        public void SignOut()
        {
            LogDebug("User signed out successfully");
        }

        public async Task<bool> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshToken))
                {
                    LogError("No refresh token available");
                    return false;
                }

                var refreshRequest = new InitiateAuthRequest
                {
                    ClientId = _clientId,
                    AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                    AuthParameters = new Dictionary<string, string>
                    {
                        { "REFRESH_TOKEN", refreshToken }
                    }
                };

                var response = await _cognitoClient.InitiateAuthAsync(refreshRequest);

                if (response.AuthenticationResult != null)
                {
                    LogDebug("Token refreshed successfully");
                    OnTokensReceived?.Invoke(
                        response.AuthenticationResult.IdToken, 
                        response.AuthenticationResult.AccessToken, 
                        response.AuthenticationResult.RefreshToken ?? refreshToken // Keep old refresh token if not provided
                    );
                    return true;
                }
                LogWarning("Token refresh failed: No authentication result");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Token refresh error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> GetAWSCredentialsAsync()
        {
            try
            {
                LogDebug("Getting AWS credentials...");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error getting AWS credentials: {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetUserGroupsAsync()
        {
            try
            {
                LogDebug("Getting user groups from ID token...");
        
                // This method will be called from CognitoManager which has access to the current ID token
                // For now, return empty list as CognitoManager will handle the token parsing
                return new List<string>();
            }
            catch (Exception ex)
            {
                LogError($"Error getting user groups: {ex.Message}");
                return new List<string>();
            }
        }
        
        /// <summary>
        /// Gets user groups from provided ID token
        /// </summary>
        public async Task<List<string>> GetUserGroupsFromTokenAsync(string idToken)
        {
            try
            {
                LogDebug("Parsing user groups from ID token...");
                return await ParseUserGroupsFromToken(idToken);
            }
            catch (Exception ex)
            {
                LogError($"Error getting user groups from token: {ex.Message}");
                return new List<string> { "usuarios-basicos" };
            }
        }

        public bool IsUserInGroup(string groupName)
        {
            // This will be handled by CognitoManager which maintains the user groups list
            LogDebug($"Checking if user is in group: {groupName}");
            return false; // CognitoManager will override this behavior
        }

        public string GetUserRole()
        {
            // This will be handled by CognitoManager which determines role based on groups
            LogDebug("Getting user role...");
            return "usuarios-basicos"; // Default role
        }

        public async Task<bool> ResendConfirmationCodeAsync(string username)
        {
            try
            {
                var resendRequest = new ResendConfirmationCodeRequest
                {
                    ClientId = _clientId,
                    Username = username
                };

                await _cognitoClient.ResendConfirmationCodeAsync(resendRequest);
                LogDebug("Verification code resent successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Resend verification code error: {ex.Message}");
                return false;
            }
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            var cleaned = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[^\d]", "");
            if (!phoneNumber.StartsWith("+"))
            {
                if (cleaned.Length == 10)
                    return $"+1{cleaned}";
                else if (cleaned.Length == 11 && cleaned.StartsWith("1"))
                    return $"+{cleaned}";
                else if (cleaned.Length == 10)
                    return $"+52{cleaned}";
            }
            return phoneNumber;
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[CognitoAuthHandler] {message}");
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogs)
                Debug.LogWarning($"[CognitoAuthHandler] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[CognitoAuthHandler] {message}");
        }
        
        /// <summary>
/// Parses user groups from ID token
/// </summary>
private async Task<List<string>> ParseUserGroupsFromToken(string idToken)
{
    try
    {
        if (string.IsNullOrEmpty(idToken))
        {
            LogDebug("No ID token available for parsing groups");
            return new List<string> { "usuarios-basicos" }; // Default group
        }

        var parts = idToken.Split('.');
        if (parts.Length != 3)
        {
            LogWarning("Invalid ID token format");
            return new List<string> { "usuarios-basicos" };
        }

        var payload = parts[1];
        // Add padding if needed for Base64 decoding
        while (payload.Length % 4 != 0)
            payload += "=";

        var jsonBytes = System.Convert.FromBase64String(payload);
        var json = System.Text.Encoding.UTF8.GetString(jsonBytes);
        
        LogDebug($"JWT Payload parsed successfully");
        
        // Parse JSON to extract groups (simplified version)
        // You can use Newtonsoft.Json if available, or simple string parsing
        if (json.Contains("\"cognito:groups\""))
        {
            // Extract groups array from JSON (basic string parsing)
            var groupsStart = json.IndexOf("\"cognito:groups\"");
            var groupsSection = json.Substring(groupsStart);
            var arrayStart = groupsSection.IndexOf('[');
            var arrayEnd = groupsSection.IndexOf(']');
            
            if (arrayStart != -1 && arrayEnd != -1)
            {
                var groupsArray = groupsSection.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                var groups = new List<string>();
                
                // Parse individual groups (remove quotes and split by comma)
                var groupItems = groupsArray.Split(',');
                foreach (var item in groupItems)
                {
                    var cleanGroup = item.Trim().Trim('"').Trim();
                    if (!string.IsNullOrEmpty(cleanGroup))
                    {
                        groups.Add(cleanGroup);
                    }
                }
                
                LogDebug($"Found groups: {string.Join(", ", groups)}");
                return groups.Count > 0 ? groups : new List<string> { "usuarios-basicos" };
            }
        }
        
        // If no groups found, return default
        LogDebug("No groups found in token, using default");
        return new List<string> { "usuarios-basicos" };
    }
    catch (Exception ex)
    {
        LogError($"Error parsing groups from token: {ex.Message}");
        return new List<string> { "usuarios-basicos" };
    }
}
    }
}