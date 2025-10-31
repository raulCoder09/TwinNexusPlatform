// IdentityContext.cs

using _scripts.models.awsServices;
using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentity;

namespace AmazonWebServices
{
    public sealed class IdentityContext : IAwsIdentityContext
    {
        private readonly string _identityPoolId;
        private readonly string _userPoolId;
        private string _idToken; // null => guest

        public RegionEndpoint Region { get; }

        public IdentityContext(RegionEndpoint region, string identityPoolId, string userPoolId)
        {
            Region = region ?? RegionEndpoint.USEast1;
            _identityPoolId = identityPoolId;
            _userPoolId = userPoolId;
        }

        public IdentityContext AsGuest()
        {
            _idToken = null;
            return this;
        }

        public IdentityContext AsUser(string idToken)
        {
            _idToken = idToken;
            return this;
        }

        public AWSCredentials GetCredentials()
        {
            var creds = new CognitoAWSCredentials(_identityPoolId, Region);
            if (!string.IsNullOrEmpty(_idToken))
            {
                var provider = $"cognito-idp.{Region.SystemName}.amazonaws.com/{_userPoolId}";
                creds.AddLogin(provider, _idToken);
            }
            return creds;
        }
    }
}