using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using SynchronousApi.Core.Command;
using SynchronousApi.Core.Queries;
using HelloWorld;
using Microsoft.Extensions.DependencyInjection;
using SynchronousApi.Core.Services;
using Amazon.XRay.Recorder.Core;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using SynchronousApi.Implementations;

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

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            this._loggingService.AddTraceId(AWSXRayRecorder.Instance.GetEntity().TraceId);

            if (apigProxyEvent.HttpMethod != "POST" || string.IsNullOrEmpty(apigProxyEvent.Body))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            var product =
                await this._handler.Handle(JsonSerializer.Deserialize<CreateProductCommand>(apigProxyEvent.Body));

            await Metrics.IncrementMetric("ProductCreated", 1);

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(product),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
