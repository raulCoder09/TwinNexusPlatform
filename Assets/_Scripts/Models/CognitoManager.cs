using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.CognitoIdentity;
using Amazon;
using Amazon.CognitoIdentity.Model;
using Amazon.Runtime;
using Newtonsoft.Json;
using System.Text;

namespace _Scripts.Models
{
    public class CognitoManager : MonoBehaviour
    {
        [Header("AWS Cognito Configuration")]
        [SerializeField] private string userPoolId;
        [SerializeField] private string clientId;
        [SerializeField] private string identityPoolId;
        
        [Header("AWS Region Configuration")]
        [SerializeField] private AWSRegion awsRegion = AWSRegion.USEast1;
        
        // Enum para regions que Unity puede serializar
        public enum AWSRegion
        {
            USEast1,      // us-east-1 (Virginia)
            USEast2,      // us-east-2 (Ohio) 
            USWest1,      // us-west-1 (N. California)
            USWest2,      // us-west-2 (Oregon)
            EUWest1,      // eu-west-1 (Ireland)
            EUCentral1,   // eu-central-1 (Frankfurt)
            APSoutheast1, // ap-southeast-1 (Singapore)
            APNortheast1  // ap-northeast-1 (Tokyo)
        }
        
        // Método helper para convertir enum a RegionEndpoint
        private RegionEndpoint GetRegionEndpoint()
        {
            return awsRegion switch
            {
                AWSRegion.USEast1 => RegionEndpoint.USEast1,
                AWSRegion.USEast2 => RegionEndpoint.USEast2,
                AWSRegion.USWest1 => RegionEndpoint.USWest1,
                AWSRegion.USWest2 => RegionEndpoint.USWest2,
                AWSRegion.EUWest1 => RegionEndpoint.EUWest1,
                AWSRegion.EUCentral1 => RegionEndpoint.EUCentral1,
                AWSRegion.APSoutheast1 => RegionEndpoint.APSoutheast1,
                AWSRegion.APNortheast1 => RegionEndpoint.APNortheast1,
                _ => RegionEndpoint.USEast1
            };
        }

        // AWS Cognito clients
        private AmazonCognitoIdentityProviderClient cognitoUserPool;
        private AmazonCognitoIdentityClient cognitoIdentity;

        // Current user session data
        public string AccessToken { get; private set; }
        public string IdToken { get; private set; }
        public string RefreshToken { get; private set; }
        public bool IsUserAuthenticated { get; private set; }
        public string CurrentUsername { get; private set; }
        

        // Events for UI callbacks
        public event Action<bool, string> OnAuthenticationComplete;
        public event Action<bool, string> OnRegistrationComplete;
        public event Action<bool, string> OnPasswordRecoveryComplete;
        public event Action<bool, string> OnEmailVerificationComplete;
        public event Action<bool, string> OnResendVerificationComplete;

        // Singleton instance
        public static CognitoManager Instance { get; private set; }
        
        // Pending verification data
        public string PendingUsername { get; private set; }
        public string PendingEmail { get; private set; }
        
        // AWS Credentials and user group info
        public Amazon.Runtime.AWSCredentials CurrentAWSCredentials { get; private set; }
        public string CurrentUserGroup { get; private set; }
        public List<string> UserGroups { get; private set; } = new List<string>();

        // Events for AWS credentials
        public event Action<bool, string> OnAWSCredentialsObtained;

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAWSClients();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAWSClients()
        {
            try
            {
                var regionEndpoint = GetRegionEndpoint();
                
                // Initialize Cognito User Pool client
                cognitoUserPool = new AmazonCognitoIdentityProviderClient(new Amazon.Runtime.AnonymousAWSCredentials(), regionEndpoint);
                
                // Initialize Cognito Identity client
                cognitoIdentity = new AmazonCognitoIdentityClient(new Amazon.Runtime.AnonymousAWSCredentials(), regionEndpoint);
                
                Debug.Log($"AWS Cognito clients initialized successfully with region: {awsRegion}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize AWS Cognito clients: {ex.Message}");
            }
        }

        #region Authentication Methods

        /// <summary>
        /// Authenticates a user with username/email and password
        /// </summary>
        public async Task<bool> SignInAsync(string username, string password)
        {
            try
            {
                var authRequest = new InitiateAuthRequest
                {
                    ClientId = clientId,
                    AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                    AuthParameters = new Dictionary<string, string>
                    {
                        {"USERNAME", username},
                        {"PASSWORD", password}
                    }
                };

                var response = await cognitoUserPool.InitiateAuthAsync(authRequest);

                if (response.AuthenticationResult != null)
                {
                    // Store tokens
                    AccessToken = response.AuthenticationResult.AccessToken;
                    IdToken = response.AuthenticationResult.IdToken;
                    RefreshToken = response.AuthenticationResult.RefreshToken;
                    CurrentUsername = username;
                    IsUserAuthenticated = true;

                    Debug.Log("Authentication successful");

                    // Get AWS credentials and user groups
                    await GetAWSCredentialsAsync();
                    await GetUserGroupsAsync();

                    OnAuthenticationComplete?.Invoke(true, "Authentication successful");
                    return true;
                }
                else
                {
                    Debug.LogWarning("Authentication failed: No authentication result");
                    OnAuthenticationComplete?.Invoke(false, "Authentication failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Authentication error: {ex.Message}");
                OnAuthenticationComplete?.Invoke(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        public async Task<bool> SignUpAsync(string username, string password, string email)
        {
            try
            {
                var signUpRequest = new SignUpRequest
                {
                    ClientId = clientId,
                    Username = username,
                    Password = password,
                    UserAttributes = new List<AttributeType>
                    {
                        new AttributeType
                        {
                            Name = "email",
                            Value = email
                        },
                        new AttributeType
                        {
                            Name = "preferred_username",
                            Value = username
                        }
                    }
                };

                var response = await cognitoUserPool.SignUpAsync(signUpRequest);
                
                // Store pending verification data
                PendingUsername = username;
                PendingEmail = email;
                
                Debug.Log($"Registration successful. User sub: {response.UserSub}");
                OnRegistrationComplete?.Invoke(true, "Registration successful. Please check your email for verification.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Registration error: {ex.Message}");
                OnRegistrationComplete?.Invoke(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Registers a new user with phone number
        /// </summary>
        public async Task<bool> SignUpAsync(string username, string password, string email, string phoneNumber = null)
        {
            try
            {
                var userAttributes = new List<AttributeType>
                {
                    new AttributeType
                    {
                        Name = "email",
                        Value = email
                    },
                    new AttributeType
                    {
                        Name = "preferred_username",
                        Value = username
                    }
                };

                // Add phone number if provided
                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    userAttributes.Add(new AttributeType
                    {
                        Name = "phone_number",
                        Value = FormatPhoneNumber(phoneNumber)
                    });
                }

                var signUpRequest = new SignUpRequest
                {
                    ClientId = clientId,
                    Username = username,
                    Password = password,
                    UserAttributes = userAttributes
                };

                var response = await cognitoUserPool.SignUpAsync(signUpRequest);
                
                // Store pending verification data
                PendingUsername = username;
                PendingEmail = email;
                
                Debug.Log($"Registration successful. User sub: {response.UserSub}");
                OnRegistrationComplete?.Invoke(true, "Registration successful. Please check your email for verification.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Registration error: {ex.Message}");
                OnRegistrationComplete?.Invoke(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Formats phone number to E.164 format (+country code)
        /// </summary>
        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            // Remove all non-numeric characters
            var cleaned = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[^\d]", "");
            
            // If it doesn't start with +, assume it's a US number and add +1
            if (!phoneNumber.StartsWith("+"))
            {
                // If it's 10 digits, assume US number
                if (cleaned.Length == 10)
                {
                    return $"+1{cleaned}";
                }
                // If it's 11 digits and starts with 1, assume US number
                else if (cleaned.Length == 11 && cleaned.StartsWith("1"))
                {
                    return $"+{cleaned}";
                }
                // For Mexican numbers (if 10 digits), add +52
                else if (cleaned.Length == 10)
                {
                    return $"+52{cleaned}";
                }
            }

            return phoneNumber; // Return as-is if already formatted
        }

        /// <summary>
        /// Validates phone number format
        /// </summary>
        public bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return false;

            // Basic validation for international format
            var phoneRegex = @"^\+[1-9]\d{1,14}$";
            return System.Text.RegularExpressions.Regex.IsMatch(FormatPhoneNumber(phoneNumber), phoneRegex);
        }

        /// <summary>
        /// Validates verification code format
        /// </summary>
        public bool IsValidVerificationCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return false;

            // Remove any spaces or special characters
            var cleanCode = System.Text.RegularExpressions.Regex.Replace(code, @"[^\d]", "");
            
            // Verification codes are typically 6 digits
            return cleanCode.Length == 6 && cleanCode.All(char.IsDigit);
        }

        /// <summary>
        /// Cleans verification code (removes spaces, special characters)
        /// </summary>
        public string CleanVerificationCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return code;

            return System.Text.RegularExpressions.Regex.Replace(code, @"[^\d]", "");
        }

        /// <summary>
        /// Checks if there's a pending verification
        /// </summary>
        public bool HasPendingVerification()
        {
            return !string.IsNullOrEmpty(PendingUsername) && !string.IsNullOrEmpty(PendingEmail);
        }

        /// <summary>
        /// Clears pending verification data
        /// </summary>
        public void ClearPendingVerification()
        {
            PendingUsername = null;
            PendingEmail = null;
        }

        /// <summary>
        /// Confirms user registration with verification code
        /// </summary>
        public async Task<bool> ConfirmSignUpAsync(string username, string confirmationCode)
        {
            try
            {
                var confirmRequest = new ConfirmSignUpRequest
                {
                    ClientId = clientId,
                    Username = username,
                    ConfirmationCode = confirmationCode
                };

                await cognitoUserPool.ConfirmSignUpAsync(confirmRequest);
                
                // Clear pending verification data
                PendingUsername = null;
                PendingEmail = null;
                
                Debug.Log("Email verification successful");
                OnEmailVerificationComplete?.Invoke(true, "Email verification successful! You can now login.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Email verification error: {ex.Message}");
                OnEmailVerificationComplete?.Invoke(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Confirms user registration with verification code using pending username
        /// </summary>
        public async Task<bool> ConfirmSignUpAsync(string confirmationCode)
        {
            if (string.IsNullOrEmpty(PendingUsername))
            {
                Debug.LogError("No pending username for verification");
                OnEmailVerificationComplete?.Invoke(false, "No pending verification. Please register again.");
                return false;
            }
            
            return await ConfirmSignUpAsync(PendingUsername, confirmationCode);
        }

        /// <summary>
        /// Resends verification code for pending user
        /// </summary>
        public async Task<bool> ResendConfirmationCodeAsync()
        {
            if (string.IsNullOrEmpty(PendingUsername))
            {
                Debug.LogError("No pending username for resending code");
                OnResendVerificationComplete?.Invoke(false, "No pending verification. Please register again.");
                return false;
            }

            return await ResendConfirmationCodeAsync(PendingUsername);
        }

        /// <summary>
        /// Resends verification code for specific username
        /// </summary>
        public async Task<bool> ResendConfirmationCodeAsync(string username)
        {
            try
            {
                var resendRequest = new ResendConfirmationCodeRequest
                {
                    ClientId = clientId,
                    Username = username
                };

                await cognitoUserPool.ResendConfirmationCodeAsync(resendRequest);
                
                Debug.Log("Verification code resent successfully");
                OnResendVerificationComplete?.Invoke(true, "Verification code sent! Please check your email.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Resend verification code error: {ex.Message}");
                OnResendVerificationComplete?.Invoke(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Initiates password recovery process
        /// </summary>
        public async Task<bool> ForgotPasswordAsync(string username)
        {
            try
            {
                var forgotPasswordRequest = new ForgotPasswordRequest
                {
                    ClientId = clientId,
                    Username = username
                };

                await cognitoUserPool.ForgotPasswordAsync(forgotPasswordRequest);
                
                Debug.Log("Password recovery email sent");
                OnPasswordRecoveryComplete?.Invoke(true, "Password recovery email sent successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Password recovery error: {ex.Message}");
                OnPasswordRecoveryComplete?.Invoke(false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Confirms password reset with verification code
        /// </summary>
        public async Task<bool> ConfirmForgotPasswordAsync(string username, string confirmationCode, string newPassword)
        {
            try
            {
                var confirmRequest = new ConfirmForgotPasswordRequest
                {
                    ClientId = clientId,
                    Username = username,
                    ConfirmationCode = confirmationCode,
                    Password = newPassword
                };

                await cognitoUserPool.ConfirmForgotPasswordAsync(confirmRequest);
                
                Debug.Log("Password reset successful");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Password reset error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Signs out current user
        /// </summary>
        public void SignOut()
        {
            AccessToken = null;
            IdToken = null;
            RefreshToken = null;
            CurrentUsername = null;
            IsUserAuthenticated = false;
            
            // Clear pending verification data
            PendingUsername = null;
            PendingEmail = null;
            
            Debug.Log("User signed out successfully");
        }

        /// <summary>
        /// Refreshes the access token using refresh token
        /// </summary>
        public async Task<bool> RefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(RefreshToken))
            {
                Debug.LogWarning("No refresh token available");
                return false;
            }

            try
            {
                var refreshRequest = new InitiateAuthRequest
                {
                    ClientId = clientId,
                    AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                    AuthParameters = new Dictionary<string, string>
                    {
                        {"REFRESH_TOKEN", RefreshToken}
                    }
                };

                var response = await cognitoUserPool.InitiateAuthAsync(refreshRequest);

                if (response.AuthenticationResult != null)
                {
                    AccessToken = response.AuthenticationResult.AccessToken;
                    IdToken = response.AuthenticationResult.IdToken;
                    
                    // Refresh token might be updated
                    if (!string.IsNullOrEmpty(response.AuthenticationResult.RefreshToken))
                    {
                        RefreshToken = response.AuthenticationResult.RefreshToken;
                    }

                    Debug.Log("Token refreshed successfully");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Token refresh error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region User Management

        /// <summary>
        /// Gets current user information
        /// </summary>
        public async Task<GetUserResponse> GetCurrentUserAsync()
        {
            if (string.IsNullOrEmpty(AccessToken))
            {
                Debug.LogWarning("No access token available");
                return null;
            }

            try
            {
                var getUserRequest = new GetUserRequest
                {
                    AccessToken = AccessToken
                };

                return await cognitoUserPool.GetUserAsync(getUserRequest);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Get user error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Updates user attributes
        /// </summary>
        public async Task<bool> UpdateUserAttributesAsync(Dictionary<string, string> attributes)
        {
            if (string.IsNullOrEmpty(AccessToken))
            {
                Debug.LogWarning("No access token available");
                return false;
            }

            try
            {
                var updateRequest = new UpdateUserAttributesRequest
                {
                    AccessToken = AccessToken,
                    UserAttributes = new List<AttributeType>()
                };

                foreach (var attribute in attributes)
                {
                    updateRequest.UserAttributes.Add(new AttributeType
                    {
                        Name = attribute.Key,
                        Value = attribute.Value
                    });
                }

                await cognitoUserPool.UpdateUserAttributesAsync(updateRequest);
                Debug.Log("User attributes updated successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Update user attributes error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Validates if current tokens are still valid
        /// </summary>
        public bool IsTokenValid()
        {
            // Simple check - in production you'd want to validate JWT expiration
            return !string.IsNullOrEmpty(AccessToken) && IsUserAuthenticated;
        }

        /// <summary>
        /// Gets user claims from ID token (simplified version)
        /// </summary>
        public Dictionary<string, string> GetUserClaims()
        {
            var claims = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(IdToken))
                return claims;

            try
            {
                // Parse JWT token (simplified - use a proper JWT library in production)
                var parts = IdToken.Split('.');
                if (parts.Length != 3)
                    return claims;

                var payload = parts[1];
                // Add padding if needed
                while (payload.Length % 4 != 0)
                    payload += "=";

                var jsonBytes = Convert.FromBase64String(payload);
                var json = System.Text.Encoding.UTF8.GetString(jsonBytes);
                
                Debug.Log($"JWT Payload: {json}");
                // In a real implementation, you'd parse this JSON properly
                // For now, we'll return basic info
                claims["username"] = CurrentUsername ?? "unknown";
                
                return claims;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing ID token: {ex.Message}");
                return claims;
            }
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates password strength
        /// </summary>
        public bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            // Basic validation - you can customize this
            return password.Length >= 8 && 
                   System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]") && // Uppercase
                   System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]") && // Lowercase
                   System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]"); // Number
        }

        #endregion

        #region AWS Credentials and Identity Pool

/// <summary>
/// Gets AWS credentials from Identity Pool after successful authentication
/// </summary>
public async Task<bool> GetAWSCredentialsAsync()
{
    if (string.IsNullOrEmpty(IdToken))
    {
        Debug.LogWarning("No ID token available for getting AWS credentials");
        return false;
    }

    try
    {
        // Get identity ID
        var getIdRequest = new GetIdRequest
        {
            IdentityPoolId = identityPoolId,
            Logins = new Dictionary<string, string>
            {
                { $"cognito-idp.{GetRegionEndpoint().SystemName}.amazonaws.com/{userPoolId}", IdToken }
            }
        };

        var getIdResponse = await cognitoIdentity.GetIdAsync(getIdRequest);
        
        // Get credentials for identity
        var getCredentialsRequest = new GetCredentialsForIdentityRequest
        {
            IdentityId = getIdResponse.IdentityId,
            Logins = new Dictionary<string, string>
            {
                { $"cognito-idp.{GetRegionEndpoint().SystemName}.amazonaws.com/{userPoolId}", IdToken }
            }
        };

        var getCredentialsResponse = await cognitoIdentity.GetCredentialsForIdentityAsync(getCredentialsRequest);
        
        // Create AWS credentials
        CurrentAWSCredentials = new SessionAWSCredentials(
            getCredentialsResponse.Credentials.AccessKeyId,
            getCredentialsResponse.Credentials.SecretKey,
            getCredentialsResponse.Credentials.SessionToken
        );

        Debug.Log("AWS credentials obtained successfully");
        OnAWSCredentialsObtained?.Invoke(true, "AWS credentials obtained successfully");
        return true;
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error getting AWS credentials: {ex.Message}");
        OnAWSCredentialsObtained?.Invoke(false, ex.Message);
        return false;
    }
}

/// <summary>
/// Gets user groups from the current user
/// </summary>
public async Task<List<string>> GetUserGroupsAsync()
{
    try
    {
        var userResponse = await GetCurrentUserAsync();
        if (userResponse == null)
        {
            Debug.LogWarning("Could not get current user information");
            return new List<string>();
        }

        // Parse groups from user attributes or from ID token
        var groups = await ParseUserGroupsFromToken();
        UserGroups = groups;
        CurrentUserGroup = groups.FirstOrDefault() ?? "usuarios-basicos";
        
        Debug.Log($"User groups: {string.Join(", ", groups)}");
        Debug.Log($"Primary group: {CurrentUserGroup}");
        
        return groups;
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error getting user groups: {ex.Message}");
        return new List<string>();
    }
}

/// <summary>
/// Parses user groups from ID token
/// </summary>
private async Task<List<string>> ParseUserGroupsFromToken()
{
    try
    {
        if (string.IsNullOrEmpty(IdToken))
            return new List<string>();

        var parts = IdToken.Split('.');
        if (parts.Length != 3)
            return new List<string>();

        var payload = parts[1];
        while (payload.Length % 4 != 0)
            payload += "=";

        var jsonBytes = Convert.FromBase64String(payload);
        var json = Encoding.UTF8.GetString(jsonBytes);
        
        // Parse JSON to extract groups
        var tokenData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        
        if (tokenData.ContainsKey("cognito:groups"))
        {
            var groupsObj = tokenData["cognito:groups"];
            if (groupsObj is Newtonsoft.Json.Linq.JArray groupsArray)
            {
                return groupsArray.Select(g => g.ToString()).ToList();
            }
        }
        
        // If no groups found, return default
        return new List<string> { "usuarios-basicos" };
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error parsing groups from token: {ex.Message}");
        return new List<string> { "usuarios-basicos" };
    }
}

/// <summary>
/// Checks if user belongs to a specific group
/// </summary>
public bool IsUserInGroup(string groupName)
{
    return UserGroups.Contains(groupName);
}

/// <summary>
/// Gets the user's primary role based on group hierarchy
/// </summary>
public string GetUserRole()
{
    if (IsUserInGroup("super-admin"))
        return "super-admin";
    if (IsUserInGroup("operadores"))
        return "operadores";
    if (IsUserInGroup("estudiantes"))
        return "estudiantes";
    
    return "usuarios-basicos";
}

#endregion
        private void OnDestroy()
        {
            cognitoUserPool?.Dispose();
            cognitoIdentity?.Dispose();
        }
    }
}