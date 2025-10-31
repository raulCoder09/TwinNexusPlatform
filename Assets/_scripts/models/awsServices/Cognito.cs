using System;
using System.Collections.Generic;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace _scripts.models.awsServices
{
    public sealed class Cognito
    {
        private static string _clientId;
        private static IAmazonCognitoIdentityProvider _client =
            new AmazonCognitoIdentityProviderClient(RegionEndpoint.USEast1);
        private static Cognito? _instance;
        private static readonly object Locker = new object();
        private Cognito(string clientId, IAmazonCognitoIdentityProvider client)
        {
            _clientId = clientId;
            _client = client;
        }
        public static Cognito GetInstance(string clientId, IAmazonCognitoIdentityProvider client)
        {
                if (_instance != null) return _instance;
                lock (Locker)
                {
                    _instance ??= new Cognito(clientId,client);
                }
                return _instance;
        }

        #region AuthenticationAndSessions
                internal (string idToken, string accessToken, string refreshToken) Login(string username, string password)
                    {
                        var req = new InitiateAuthRequest
                        {
                            ClientId = _clientId,
                            AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                            AuthParameters = new Dictionary<string, string>
                            {
                                ["USERNAME"] = username,
                                ["PASSWORD"] = password
                            }
                        };
                        try
                        {
                            var resp = _client.InitiateAuthAsync(req).Result;

                            if (resp.AuthenticationResult != null)
                            {
                                return (resp.AuthenticationResult.IdToken,
                                    resp.AuthenticationResult.AccessToken,
                                    resp.AuthenticationResult.RefreshToken);
                            }

                            return ($"authentication failed: {resp.ChallengeName}", null, null);
                        }
                        catch (Exception e)
                        {
                            var ex = (e as AggregateException)?.InnerException ?? e;
                            return ($"error: {ex.GetType().Name}: {ex.Message}", null, null);
                        }
                    }
                internal (string idToken, string accessToken, string refreshToken) RefreshTokens(string refreshToken)
                {
                    var req = new InitiateAuthRequest
                    {
                        ClientId = _clientId,
                        AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                        AuthParameters = new Dictionary<string, string>
                        {
                            ["REFRESH_TOKEN"] = refreshToken
                        }
                    };

                    try
                    {
                        var resp = _client.InitiateAuthAsync(req).Result;

                        if (resp.AuthenticationResult != null)
                        {
                            return (
                                resp.AuthenticationResult.IdToken,
                                resp.AuthenticationResult.AccessToken,
                                refreshToken
                            );
                        }

                        return ($"refresh failed: {resp.ChallengeName}", null, refreshToken);
                    }
                    catch (Exception e)
                    {
                        var ex = (e as AggregateException)?.InnerException ?? e;
                        return ($"error: {ex.GetType().Name}: {ex.Message}", null, refreshToken);
                    }
                }
                internal (bool ok, string error) GlobalSignOut(string accessToken)
                {
                    var req = new GlobalSignOutRequest
                    {
                        AccessToken = accessToken
                    };

                    try
                    {
                        var resp = _client.GlobalSignOutAsync(req).Result;
                        return (true, null);
                    }
                    catch (Exception e)
                    {
                        var ex = (e as AggregateException)?.InnerException ?? e;
                        return (false, $"error: {ex.GetType().Name}: {ex.Message}");
                    }
                }
                internal (bool ok, string error) RevokeToken(string refreshToken, string clientSecret = null)
                {
                    var req = new RevokeTokenRequest
                    {
                        ClientId = _clientId,
                        Token = refreshToken,
                        ClientSecret = clientSecret // opcional; depende de tu app client
                    };

                    try
                    {
                        var resp = _client.RevokeTokenAsync(req).Result;
                        return (true, null);
                    }
                    catch (Exception e)
                    {
                        var ex = (e as AggregateException)?.InnerException ?? e;
                        return (false, $"error: {ex.GetType().Name}: {ex.Message}");
                    }
                }
                internal (bool ok, string error) SignOut(string refreshToken)
                {
                    var (ok, error) = RevokeToken(refreshToken);
                    return (ok, error);
                }
        #endregion

        #region AccountRegistrationAndVerification
                
            internal (bool ok, string error, string deliveryMedium, string destination) SignUp(
                    string username,
                    string password,
                    IDictionary<string, string> attributes = null,
                    string clientSecret = null
                )
                {
                    var req = new SignUpRequest
                    {
                        ClientId = _clientId,
                        Username = username,
                        Password = password
                    };

                    req.UserAttributes = req.UserAttributes ?? new List<AttributeType>();
                    if (attributes != null)
                    {
                        foreach (var kv in attributes)
                            req.UserAttributes.Add(new AttributeType { Name = kv.Key, Value = kv.Value });
                    }

                    if (!string.IsNullOrEmpty(clientSecret))
                    {
                        req.SecretHash = clientSecret;
                    }

                    try
                    {
                        var resp = _client.SignUpAsync(req).Result;
                        var d = resp.CodeDeliveryDetails;

                        return (
                            resp.UserConfirmed ?? false,
                            null,
                            d?.DeliveryMedium?.Value,
                            d?.Destination
                        );
                    }
                    catch (Exception e)
                    {
                        var ex = (e as AggregateException)?.InnerException ?? e;
                        return (false, $"error: {ex.GetType().Name}: {ex.Message}", null, null);
                    }
                }

                internal (bool ok, string error) ConfirmSignUp(
                    string username,
                    string confirmationCode,
                    bool forceAliasCreation = false,
                    string clientSecret = null // si tu App Client usa secreto
                )
                {
                    var req = new ConfirmSignUpRequest
                    {
                        ClientId = _clientId,
                        Username = username,
                        ConfirmationCode = confirmationCode,
                        ForceAliasCreation = forceAliasCreation
                    };

                    if (!string.IsNullOrEmpty(clientSecret))
                    {
                        req.SecretHash = clientSecret; 
                    }

                    try
                    {
                        var resp = _client.ConfirmSignUpAsync(req).Result;
                        return (true, null);
                    }
                    catch (Exception e)
                    {
                        var ex = (e as AggregateException)?.InnerException ?? e;
                        return (false, $"error: {ex.GetType().Name}: {ex.Message}");
                    }
                }
                
                internal (bool ok, string error, string deliveryMedium, string destination) ResendConfirmationCode(
                    string username,
                    string clientSecret = null
                )
                {
                    var req = new ResendConfirmationCodeRequest
                    {
                        ClientId = _clientId,
                        Username = username
                    };

                    if (!string.IsNullOrEmpty(clientSecret))
                    {
                        req.SecretHash = clientSecret;
                    }

                    try
                    {
                        var resp = _client.ResendConfirmationCodeAsync(req).Result;
                        var d = resp.CodeDeliveryDetails;

                        return (
                            true,
                            null,
                            d?.DeliveryMedium?.Value,  // EMAIL o SMS
                            d?.Destination
                        );
                    }
                    catch (Exception e)
                    {
                        var ex = (e as AggregateException)?.InnerException ?? e;
                        return (false, $"error: {ex.GetType().Name}: {ex.Message}", null, null);
                    }
                }

                internal (bool ok, string error, string deliveryMedium, string destination) ForgotPasswordStart(
                    string username,
                    string clientSecret = null
                )
                {
                    var req = new ForgotPasswordRequest
                    {
                        ClientId = _clientId,
                        Username = username
                    };

                    if (!string.IsNullOrEmpty(clientSecret))
                    {
                        req.SecretHash = clientSecret; 
                    }

                    try
                    {
                        var resp = _client.ForgotPasswordAsync(req).Result;
                        var d = resp.CodeDeliveryDetails;

                        return (
                            true,
                            null,
                            d?.DeliveryMedium?.Value, 
                            d?.Destination             
                        );
                    }
                    catch (Exception e)
                    {
                        var ex = (e as AggregateException)?.InnerException ?? e;
                        return (false, $"error: {ex.GetType().Name}: {ex.Message}", null, null);
                    }
                }
                
                internal (bool ok, string error) ForgotPasswordConfirm(
                    string username,
                    string confirmationCode,
                    string newPassword,
                    string clientSecret = null
                )
                {
                    var req = new ConfirmForgotPasswordRequest
                    {
                        ClientId = _clientId,
                        Username = username,
                        ConfirmationCode = confirmationCode,
                        Password = newPassword
                    };

                    if (!string.IsNullOrEmpty(clientSecret))
                    {
                        req.SecretHash = clientSecret; 
                    }

                    try
                    {
                        var resp = _client.ConfirmForgotPasswordAsync(req).Result;
                        return (true, null);
                    }
                    catch (Exception e)
                    {
                        var ex = (e as AggregateException)?.InnerException ?? e;
                        return (false, $"error: {ex.GetType().Name}: {ex.Message}");
                    }
                }

                internal (bool ok, string error) ChangePassword(
                    string accessToken,
                    string oldPassword,
                    string newPassword
                )
                {
                    var req = new ChangePasswordRequest
                    {
                        AccessToken = accessToken,
                        PreviousPassword = oldPassword,
                        ProposedPassword = newPassword
                    };

                    try
                    {
                        var resp = _client.ChangePasswordAsync(req).Result;
                        return (true, null);
                    }
                    catch (Exception e)
                    {
                        var ex = (e as AggregateException)?.InnerException ?? e;
                        return (false, $"error: {ex.GetType().Name}: {ex.Message}");
                    }
                }
        
        #endregion
        

    }
}