using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

namespace ProductApi.IntegrationTests;

public class Setup : IDisposable
{
    public static string ApiUrl { get; set; } = "";

    public Setup()
    {
    }

    public void Dispose()
    {
        // Do "global" teardown here; Only called once.
    }
}