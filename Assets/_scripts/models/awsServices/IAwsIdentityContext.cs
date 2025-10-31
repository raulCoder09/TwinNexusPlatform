using Amazon;
using Amazon.Runtime;

namespace _scripts.models.awsServices
{
    public interface IAwsIdentityContext
    {
        RegionEndpoint Region { get; }
        AWSCredentials GetCredentials();
    }
}