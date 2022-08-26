using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace ApplicationIntegrationPatterns.Implementations
{
    public class SystemParameters
    {
        private readonly AmazonSimpleSystemsManagementClient _client;

        public SystemParameters(AmazonSimpleSystemsManagementClient client)
        {
            this._client = client;
        }

        public async Task<string> RetrieveParameter(string name)
        {
            var paramResult = await this._client.GetParameterAsync(new GetParameterRequest()
            {
                Name = name
            });

            return paramResult.Parameter.Value;
        }
    }
}
