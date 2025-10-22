using System;
using System.Collections.Generic;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace _scripts
{
    public class Cognito
    {
        private readonly string _clientId;
        private readonly IAmazonCognitoIdentityProvider _client = new AmazonCognitoIdentityProviderClient(RegionEndpoint.USEast1);
        public Cognito(string clientId)
        {
            _clientId = clientId;
        }

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
        
        
    }
}