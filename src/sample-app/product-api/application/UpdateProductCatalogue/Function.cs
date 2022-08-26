using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using ApplicationIntegrationPatterns.Core.Command;
using Microsoft.Extensions.DependencyInjection;
using ApplicationIntegrationPatterns.Core.Services;
using ApplicationIntegrationPatterns.Implementations;
using AWS.Lambda.Powertools.Tracing;
using AWS.Lambda.Powertools.Metrics;
using ApplicationIntegrationPatterns.Core.Events;
using Amazon.Lambda.SQSEvents;
using ApplicationIntegrationPatterns.Implementations.Models;

namespace UpdateProductCatalogue
{
    public class Function
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

        [Tracing]
        [Metrics(CaptureColdStart =true)]
        public async Task FunctionHandler(SQSEvent evt, ILambdaContext context)
        {
            this._loggingService.LogInfo("Received request to update product catalogue");

            var catalogueUpdated = 0;

            foreach (var record in evt.Records)
            {
                this._loggingService.LogInfo($"Processing {record.Body}");

                var snsData = JsonSerializer.Deserialize<SnsToSqsMessageBody>(record.Body);

                var eventData = JsonSerializer.Deserialize<ProductCreatedEvent>(snsData.Message);

                await this._handler.Handle(new UpdateProductCatalogueCommand()
                {
                    Product = eventData.Product
                });

                catalogueUpdated++;
            }

            MetricService.IncrementMetric("ProductCatalogueRecordsUpdated", catalogueUpdated);

            return;
        }
    }
}
