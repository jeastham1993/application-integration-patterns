using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using ApplicationIntegrationPatterns.Core.Command;
using HelloWorld;
using Microsoft.Extensions.DependencyInjection;
using ApplicationIntegrationPatterns.Core.Services;
using ApplicationIntegrationPatterns.Implementations;
using AWS.Lambda.Powertools.Tracing;
using AWS.Lambda.Powertools.Metrics;

namespace CreateProduct
{
    public class Function
    {
        private readonly CreateProductCommandHandler _handler;
        private readonly ILoggingService _loggingService;

        public Function() : this(null, null)
        {
        }

        internal Function(CreateProductCommandHandler handler = null, ILoggingService loggingService = null)
        {
            Startup.ConfigureServices();
            
            this._handler = handler ?? Startup.Services.GetRequiredService<CreateProductCommandHandler>();
            this._loggingService = loggingService ?? Startup.Services.GetRequiredService<ILoggingService>();
        }

        [Tracing]
        [Metrics(CaptureColdStart =true)]
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            if (apigProxyEvent.HttpMethod != "POST" || string.IsNullOrEmpty(apigProxyEvent.Body))
            {
                this._loggingService.LogWarning("Request received that isn't a POST request, returning error");

                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            this._loggingService.LogInfo("Received request to create product");

            var product =
                await this._handler.Handle(JsonSerializer.Deserialize<CreateProductCommand>(apigProxyEvent.Body));

            MetricService.IncrementMetric("ProductCreated", 1);

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(product),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
