using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using ApplicationIntegrationPatterns.Core.Command;
using Microsoft.Extensions.DependencyInjection;
using ApplicationIntegrationPatterns.Core.Services;
using ApplicationIntegrationPatterns.Implementations;
using ApplicationIntegrationPatterns.Core.Events;
using Amazon.Lambda.SQSEvents;
using ApplicationIntegrationPatterns.Implementations.Models;
using Shared;

namespace UpdateProductCatalogue
{
    public class Function : SqsTracedFunction<string>
    {
        private readonly UpdateProductCatalogueCommandHandler _handler;
        private readonly ILoggingService _loggingService;

        public Function() : this(null, null)
        {
        }

        internal Function(UpdateProductCatalogueCommandHandler handler = null, ILoggingService loggingService = null)
        {
            Startup.ConfigureServices();
            
            this._handler = handler ?? Startup.Services.GetRequiredService<UpdateProductCatalogueCommandHandler>();
            this._loggingService = loggingService ?? Startup.Services.GetRequiredService<ILoggingService>();
        }
        
        public async Task<string> FunctionHandler(SQSEvent evt, ILambdaContext context)
        {
            this._loggingService.LogInfo("Received request to update product catalogue");

            var catalogueUpdated = 0;

            foreach (var record in evt.Records)
            {
                var hydratedContext = this.HydrateContextFromSnsMessage(record);
                
                this._loggingService.LogInfo($"Processing {record.Body}");
                
                using var activity =
                    Activity.Current.Source.StartActivity("UpdatingProductCatalogue", ActivityKind.Consumer, parentContext: hydratedContext).AddSqsAttributes(record);

                var snsData = JsonSerializer.Deserialize<SnsToSqsMessageBody>(record.Body);

                var eventData = JsonSerializer.Deserialize<ProductCreatedEvent>(snsData.Message);

                await this._handler.Handle(new UpdateProductCatalogueCommand()
                {
                    Product = eventData.Product
                });

                catalogueUpdated++;
            }

            return "OK";
        }

        public override string SERVICE_NAME => "UpdateProductCatalogue";
        public override Func<SQSEvent, ILambdaContext, Task<string>> Handler => FunctionHandler;
    }
}
