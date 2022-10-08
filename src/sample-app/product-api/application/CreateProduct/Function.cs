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

namespace CreateProduct
{
    public class Function : ApiGatewayTracedFunction
    {
        private readonly CreateProductCommandHandler _handler;
        private readonly ILoggingService _loggingService;

        public override string SERVICE_NAME => "CreateProduct";

        public override Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> Handler =>
            FunctionHandler;

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
            using var apiActivity = Activity.Current.Source.StartActivity();
            
            if (apigProxyEvent.HttpMethod != "POST" || string.IsNullOrEmpty(apigProxyEvent.Body))
            {
                this._loggingService.LogWarning("Request received that isn't a POST request, returning error");

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
                await this._handler.Handle(JsonSerializer.Deserialize<CreateProductCommand>(apigProxyEvent.Body));

            apiActivity.AddTag("product-api.name", product.Name);
            apiActivity.AddTag("product-api.price", product.CurrentPrice);
            apiActivity.AddTag("product-api.product-id", product.ProductId);

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
