using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using SynchronousApi.Core.Queries;
using HelloWorld;
using Microsoft.Extensions.DependencyInjection;
using SynchronousApi.Core.Services;
using Amazon.XRay.Recorder.Core;

namespace GetProduct
{
    public class Function
    {
        private readonly GetProductQueryHandler _queryHandler;
        private readonly ILoggingService _loggingService;

        public Function() : this(null, null)
        {
        }

        internal Function(GetProductQueryHandler handler = null, ILoggingService loggingService = null)
        {
            Startup.ConfigureServices();
            
            this._queryHandler = handler ?? Startup.Services.GetRequiredService<GetProductQueryHandler>();
            this._loggingService = loggingService ?? Startup.Services.GetRequiredService<ILoggingService>();
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            if (AWSXRayRecorder.IsLambda())
            {
                this._loggingService.AddTraceId(AWSXRayRecorder.Instance.GetEntity().TraceId);
            }

            if (apigProxyEvent.HttpMethod != "GET" || apigProxyEvent.PathParameters.ContainsKey("productId") == false)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            var product =
                await this._queryHandler.Execute(new GetProductQuery(apigProxyEvent.PathParameters["productId"]));

            if (product == null)
            {
                
                return new APIGatewayProxyResponse
                {
                    Body = "{\"message\": \"Product not found\"}",
                    StatusCode = 404,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(product),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
