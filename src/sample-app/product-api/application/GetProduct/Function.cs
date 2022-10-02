using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using ApplicationIntegrationPatterns.Core.Queries;
using Microsoft.Extensions.DependencyInjection;
using ApplicationIntegrationPatterns.Core.Services;
using Amazon.XRay.Recorder.Core;
using AWS.Lambda.Powertools.Tracing;
using AWS.Lambda.Powertools.Metrics;
using ApplicationIntegrationPatterns.Implementations;
using Shared;

namespace GetProduct
{
    public class Function : ApiGatewayTracedFunction
    {
        private readonly GetProductQueryHandler _queryHandler;
        private readonly ILoggingService _loggingService;

        public override string SERVICE_NAME => "GetProduct";

        public override Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> Handler =>
            FunctionHandler;

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
            using var apiActivity = Activity.Current.Source.StartActivity();
            
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
