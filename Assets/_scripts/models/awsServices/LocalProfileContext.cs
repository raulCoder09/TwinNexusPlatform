using System;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace _scripts.models.awsServices
{
    public sealed class LocalProfileContext : IAwsIdentityContext
    {
        private readonly string _profile;
        private readonly RegionEndpoint _region;

        public LocalProfileContext(string profile = "default", RegionEndpoint region = null)
        {
            _profile = profile;
            _region = region ?? RegionEndpoint.USEast1;
        }

        public RegionEndpoint Region => _region;

        public AWSCredentials GetCredentials()
        {
            var chain = new CredentialProfileStoreChain(); // lee ~/.aws/credentials
            if (chain.TryGetAWSCredentials(_profile, out var creds))
            {
                return creds;
            }

            throw new InvalidOperationException(
                $"No se encontraron credenciales para el perfil '{_profile}'. Revisa ~/.aws/credentials."
            );
        }
    }
}