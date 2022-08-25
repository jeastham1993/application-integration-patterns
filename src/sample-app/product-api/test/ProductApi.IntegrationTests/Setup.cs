using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

namespace ProductApi.IntegrationTests;

public class Setup : IDisposable
{
    public static string ApiUrl { get; set; } = "https://ei1g9jv0j3.execute-api.eu-west-1.amazonaws.com/dev/";

    public Setup()
    {
    }

    public void Dispose()
    {
        // Do "global" teardown here; Only called once.
    }
}