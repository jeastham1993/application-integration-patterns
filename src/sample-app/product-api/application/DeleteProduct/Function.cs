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

namespace DeleteProduct
{
    public class Function : ApiGatewayTracedFunction
    {
        private readonly DeleteProductCommandHandler _handler;
        private readonly ILoggingService _loggingService;

        public Function() : this(null, null)
        {
        }

        public override string SERVICE_NAME => "DeleteProduct";
        public override Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> Handler => FunctionHandler;

        internal Function(DeleteProductCommandHandler handler = null, ILoggingService loggingService = null)
        {
            Startup.ConfigureServices();

            this._handler = handler ?? Startup.Services.GetRequiredService<DeleteProductCommandHandler>();
            this._loggingService = loggingService ?? Startup.Services.GetRequiredService<ILoggingService>();
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent,
            ILambdaContext context)
        {
            if (apigProxyEvent.HttpMethod != "DELETE" || apigProxyEvent.PathParameters.ContainsKey("productId") == false)
            {
                this._loggingService.LogWarning("Request received that isn't a DELETE request, returning error");

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

            this._loggingService.LogInfo("Received request to delete a product");

            await this._handler.Handle(new DeleteProductCommand()
            {
                ProductId = apigProxyEvent.PathParameters["productId"]
            });

            return new APIGatewayProxyResponse
            {
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