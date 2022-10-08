using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using ApplicationIntegrationPatterns.Core.Command;
using Microsoft.Extensions.DependencyInjection;
using ApplicationIntegrationPatterns.Core.Services;
using ApplicationIntegrationPatterns.Implementations;
using Shared;

namespace UpdateProduct
{
    public class Function : ApiGatewayTracedFunction
    {
        private readonly UpdateProductCommandHandler _handler;
        private readonly ILoggingService _loggingService;

        public Function() : this(null, null)
        {
        }

        public override string SERVICE_NAME => "UpdateProduct";
        public override Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> Handler => FunctionHandler;

        internal Function(UpdateProductCommandHandler handler = null, ILoggingService loggingService = null)
        {
            Startup.ConfigureServices();
            
            this._handler = handler ?? Startup.Services.GetRequiredService<UpdateProductCommandHandler>();
            this._loggingService = loggingService ?? Startup.Services.GetRequiredService<ILoggingService>();
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            if (apigProxyEvent.HttpMethod != "PUT" || string.IsNullOrEmpty(apigProxyEvent.Body))
            {
                this._loggingService.LogWarning("Request received that isn't a PUT request, returning error");

                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" },
                        { "X_TRACE_ID", Activity.Current.TraceId.ToString() }
                    }
                };
            }

            this._loggingService.LogInfo("Received request to create product");

            var product =
                await this._handler.Handle(JsonSerializer.Deserialize<UpdateProductCommand>(apigProxyEvent.Body));

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(product),
                StatusCode = 200,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "X_TRACE_ID", Activity.Current.TraceId.ToString() }
                }
            };
        }
    }
}
