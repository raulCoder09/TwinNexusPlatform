using System.Linq;
using Amazon;
using Amazon.CognitoIdentity.Model;

namespace _Scripts.Models.CognitoManagement
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Amazon.CognitoIdentityProvider;
    using Amazon.CognitoIdentity;
    using Amazon.Runtime;
    using UnityEngine;
    using _Scripts.Models.CognitoManagement;

    public class CognitoManager : MonoBehaviour
    {
        #region Singleton Pattern
        private static CognitoManager _instance;
        public static CognitoManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CognitoManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CognitoManager");
                        _instance = go.AddComponent<CognitoManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected void OnDestroy()
        {
            if (_instance == this)
            {
                if (_authHandler != null)
                {
                    _authHandler.OnTokensReceived -= OnTokensReceived;
                }
                _authHandler = null;
                _cognitoIdentity?.Dispose();
                _instance = null;
            }
        }
        #endregion

        // Propiedades configurables (sobrescritas por CognitoTesting)
        protected string _userPoolId = "us-east-1_abc123xyz";
        protected string _clientId = "yourclientid123";
        protected string _identityPoolId = "us-east-1:abc123-xyz";
        protected RegionEndpoint _regionEndpoint = RegionEndpoint.USEast1;
        protected bool _enableDebugLogs = true;

        private CognitoAuthHandler _authHandler;
        private AmazonCognitoIdentityClient _cognitoIdentity;
        private CognitoInfo.TokenData _tokenData = new CognitoInfo.TokenData();
        private CognitoInfo.Credentials _credentials;
        private CognitoInfo.UserInfo _userInfo = new CognitoInfo.UserInfo();
        private List<string> _userGroups = new List<string>();
        protected bool _isInitialized = false;

        // Eventos
        public event Action<bool, string> OnAuthenticationComplete;
        public event Action<bool, string> OnRegistrationComplete;
        public event Action<bool, string> OnPasswordRecoveryComplete;
        public event Action<bool, string> OnEmailVerificationComplete;
        public event Action<bool, string> OnResendVerificationComplete;
        public event Action<bool, string> OnAWSCredentialsObtained;

        public string AccessToken => _tokenData.AccessToken;
        public string IdToken => _tokenData.IdToken;
        public string RefreshToken => _tokenData.RefreshToken;
        public bool IsUserAuthenticated => _userInfo.IsAuthenticated;
        public string CurrentUsername => _userInfo.Username;
        public string PendingUsername => _userInfo.PendingUsername;
        public string PendingEmail => _userInfo.PendingEmail;
        public Amazon.Runtime.AWSCredentials CurrentAWSCredentials => _credentials != null 
            ? new SessionAWSCredentials(_credentials.AccessKeyId, _credentials.SecretKey, _credentials.SessionToken) 
            : null;
        public string CurrentUserGroup => _userInfo.CurrentUserGroup;
        public List<string> UserGroups => _userGroups;

        protected void Initialize()
        {
            try
            {
                if (_isInitialized)
                {
                    LogDebug("CognitoManager already initialized");
                    return;
                }

                LogDebug($"Initializing CognitoManager with UserPoolId: {_userPoolId}, ClientId: {_clientId}, Region: {_regionEndpoint.SystemName}");
                _authHandler = new CognitoAuthHandler(_userPoolId, _clientId, _regionEndpoint);
                _authHandler.OnTokensReceived += OnTokensReceived;
                _cognitoIdentity = new AmazonCognitoIdentityClient(new AnonymousAWSCredentials(), _regionEndpoint);
                _isInitialized = true;
                LogDebug("CognitoManager initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"Initialization error: {ex.Message}");
            }
        }

        private void OnTokensReceived(string idToken, string accessToken, string refreshToken)
        {
            _tokenData.IdToken = idToken;
            _tokenData.AccessToken = accessToken;
            _tokenData.RefreshToken = refreshToken;
            LogDebug($"Tokens received - IdToken: {idToken?.Substring(0, 10)}..., AccessToken: {accessToken?.Substring(0, 10)}..., RefreshToken: {refreshToken?.Substring(0, 10)}...");
        }

        public async Task<bool> SignInAsync(string username, string password)
        {
            if (!_isInitialized) Initialize();
            LogDebug($"Attempting sign-in for {username}");
            var success = await _authHandler.SignInAsync(username, password);
            if (success)
            {
                _userInfo.Username = username;
                _userInfo.IsAuthenticated = true;
                await GetAWSCredentialsAsync();
                await GetUserGroupsAsync();
                OnAuthenticationComplete?.Invoke(true, "Authentication successful");
            }
            else
            {
                OnAuthenticationComplete?.Invoke(false, "Authentication failed");
            }
            return success;
        }

        public async Task<bool> SignUpAsync(string username, string password, string email, string phoneNumber = null)
        {
            if (!_isInitialized) Initialize();
            LogDebug($"Attempting sign-up for {username}");
            var success = await _authHandler.SignUpAsync(username, password, email, phoneNumber);
            if (success)
            {
                _userInfo.PendingUsername = username;
                _userInfo.PendingEmail = email;
                OnRegistrationComplete?.Invoke(true, "Registration successful. Please check your email for verification.");
            }
            else
            {
                OnRegistrationComplete?.Invoke(false, "Registration failed");
            }
            return success;
        }

        public async Task<bool> ConfirmSignUpAsync(string username, string confirmationCode)
        {
            if (!_isInitialized) Initialize();
            LogDebug($"Attempting email verification for {username}");
            var success = await _authHandler.ConfirmSignUpAsync(username, confirmationCode);
            if (success)
            {
                _userInfo.PendingUsername = null;
                _userInfo.PendingEmail = null;
                OnEmailVerificationComplete?.Invoke(true, "Email verification successful!");
            }
            else
            {
                OnEmailVerificationComplete?.Invoke(false, "Email verification failed");
            }
            return success;
        }

        public async Task<bool> ForgotPasswordAsync(string username)
        {
            if (!_isInitialized) Initialize();
            LogDebug($"Attempting forgot password for {username}");
            var success = await _authHandler.ForgotPasswordAsync(username);
            if (success)
            {
                OnPasswordRecoveryComplete?.Invoke(true, "Password recovery email sent successfully");
            }
            else
            {
                OnPasswordRecoveryComplete?.Invoke(false, "Password recovery failed");
            }
            return success;
        }

        public async Task<bool> ConfirmForgotPasswordAsync(string username, string confirmationCode, string newPassword)
        {
            if (!_isInitialized) Initialize();
            LogDebug($"Attempting password reset for {username}");
            var success = await _authHandler.ConfirmForgotPasswordAsync(username, confirmationCode, newPassword);
            return success;
        }

        public void SignOut()
        {
            if (!_isInitialized) Initialize();
            LogDebug("Attempting sign-out");
            _authHandler.SignOut();
            _tokenData = new CognitoInfo.TokenData();
            _credentials = null;
            _userInfo = new CognitoInfo.UserInfo();
            _userGroups.Clear();
            _userInfo.IsAuthenticated = false;
        }

        public async Task<bool> RefreshTokenAsync()
        {
            if (!_isInitialized) Initialize();
    
            if (string.IsNullOrEmpty(RefreshToken))
            {
                LogError("No refresh token available for refresh");
                return false;
            }
    
            LogDebug("Attempting token refresh");
            var success = await _authHandler.RefreshTokenAsync(RefreshToken);
    
            if (success)
            {
                LogDebug("Token refresh completed successfully");
                // Los nuevos tokens se actualizan automáticamente via OnTokensReceived
            }
            else
            {
                LogError("Token refresh failed");
            }
    
            return success;
        }

        public async Task<bool> GetAWSCredentialsAsync()
        {
            if (!_isInitialized) Initialize();
            if (string.IsNullOrEmpty(IdToken))
            {
                LogError("No ID token available for getting AWS credentials");
                OnAWSCredentialsObtained?.Invoke(false, "No ID token available");
                return false;
            }

            try
            {
                LogDebug("Getting AWS credentials");
                var getIdRequest = new GetIdRequest
                {
                    IdentityPoolId = _identityPoolId,
                    Logins = new Dictionary<string, string>
                    {
                        { $"cognito-idp.{_regionEndpoint.SystemName}.amazonaws.com/{_userPoolId}", IdToken }
                    }
                };

                var getIdResponse = await _cognitoIdentity.GetIdAsync(getIdRequest);

                var getCredentialsRequest = new GetCredentialsForIdentityRequest
                {
                    IdentityId = getIdResponse.IdentityId,
                    Logins = new Dictionary<string, string>
                    {
                        { $"cognito-idp.{_regionEndpoint.SystemName}.amazonaws.com/{_userPoolId}", IdToken }
                    }
                };

                var getCredentialsResponse = await _cognitoIdentity.GetCredentialsForIdentityAsync(getCredentialsRequest);

                _credentials = new CognitoInfo.Credentials
                {
                    AccessKeyId = getCredentialsResponse.Credentials.AccessKeyId,
                    SecretKey = getCredentialsResponse.Credentials.SecretKey,
                    SessionToken = getCredentialsResponse.Credentials.SessionToken
                };

                LogDebug("AWS credentials obtained successfully");
                OnAWSCredentialsObtained?.Invoke(true, "AWS credentials obtained successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Error getting AWS credentials: {ex.Message}");
                OnAWSCredentialsObtained?.Invoke(false, ex.Message);
                return false;
            }
        }

        public async Task<List<string>> GetUserGroupsAsync()
        {
            if (!_isInitialized) Initialize();
    
            if (string.IsNullOrEmpty(IdToken))
            {
                LogError("No ID token available for getting user groups");
                return new List<string> { "usuarios-basicos" };
            }
    
            try
            {
                LogDebug("Getting user groups from ID token...");
                var groups = await _authHandler.GetUserGroupsFromTokenAsync(IdToken);
                _userGroups = groups;
                _userInfo.CurrentUserGroup = groups.FirstOrDefault() ?? "usuarios-basicos";
        
                LogDebug($"User groups retrieved: {string.Join(", ", groups)}");
                LogDebug($"Primary group set to: {_userInfo.CurrentUserGroup}");
        
                return groups;
            }
            catch (Exception ex)
            {
                LogError($"Error getting user groups: {ex.Message}");
                var defaultGroups = new List<string> { "usuarios-basicos" };
                _userGroups = defaultGroups;
                _userInfo.CurrentUserGroup = "usuarios-basicos";
                return defaultGroups;
            }
        }

        public bool IsUserInGroup(string groupName)
        {
            if (!_isInitialized) Initialize();
    
            if (string.IsNullOrEmpty(groupName))
            {
                print("Group name is null or empty");
                return false;
            }
    
            bool isInGroup = _userGroups.Contains(groupName);
            LogDebug($"User is{(isInGroup ? "" : " not")} in group: {groupName}");
            return isInGroup;
        }

        public string GetUserRole()
        {
            if (!_isInitialized) Initialize();
    
            // Define role hierarchy (you can customize this based on your groups)
            var roleHierarchy = new string[]
            {
                "super-admin",      // Highest priority
                "students",
                "operators",
                "basic-users"      // Default/lowest priority
            };
    
            // Return the highest priority role the user belongs to
            foreach (var role in roleHierarchy)
            {
                if (IsUserInGroup(role))
                {
                    LogDebug($"User role determined: {role}");
                    return role;
                }
            }
    
            LogDebug("User role defaulted to: usuarios-basicos");
            return "usuarios-basicos";
        }

        public async Task<bool> ResendConfirmationCodeAsync()
        {
            if (!_isInitialized) Initialize();
            if (string.IsNullOrEmpty(PendingUsername))
            {
                LogError("No pending username for resending code");
                return false;
            }
            var success = await _authHandler.ResendConfirmationCodeAsync(PendingUsername);
            if (success)
            {
                OnResendVerificationComplete?.Invoke(true, "Verification code resent successfully");
            }
            else
            {
                OnResendVerificationComplete?.Invoke(false, "Failed to resend verification code");
            }
            return success;
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
                Debug.Log($"[CognitoManager] {message}");
        }

        private void LogError(string message)
        {
            if (_enableDebugLogs)
                Debug.LogError($"[CognitoManager] {message}");
        }
    }
}