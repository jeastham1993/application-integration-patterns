using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using ApplicationIntegrationPatterns.Core.Command;
using Microsoft.Extensions.DependencyInjection;
using ApplicationIntegrationPatterns.Core.Services;
using ApplicationIntegrationPatterns.Implementations;
using Amazon.Lambda.SQSEvents;
using ApplicationIntegrationPatterns.Core.DataTransfer;
using ApplicationIntegrationPatterns.Implementations.Models;
using Microsoft.AspNetCore.Http.Connections;
using OpenTelemetry.Trace;
using Shared;
using Shared.Messaging;

namespace UpdateProductCatalogue
{
    public class Function : SqsTracedFunction<string>
    {
        private readonly UpdateProductCatalogueCommandHandler _handler;
        private readonly ILoggingService _loggingService;

        public Function() : this(null, null)
        {
        }

        public override string SERVICE_NAME => "UpdateProductCatalogue";
        public override Func<SQSEvent, ILambdaContext, Task<string>> Handler => FunctionHandler;

        public override Func<SQSEvent.SQSMessage, ILambdaContext, Task> MessageProcessor => MessageHandler;

        internal Function(UpdateProductCatalogueCommandHandler handler = null, ILoggingService loggingService = null)
        {
            Startup.ConfigureServices();

            this._handler = handler ?? Startup.Services.GetRequiredService<UpdateProductCatalogueCommandHandler>();
            this._loggingService = loggingService ?? Startup.Services.GetRequiredService<ILoggingService>();
        }

        public async Task<string> FunctionHandler(SQSEvent evt, ILambdaContext context)
        {
            foreach (var record in evt.Records)
            {
                await this.MessageProcessor(record, context);
            }

            return "OK";
        }

        public async Task MessageHandler(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            using (var activity =
                   Activity.Current.Source
                       .StartActivity("UpdatingProductCatalogue", ActivityKind.Consumer)
                       .AddSqsAttributes(message))
            {
                var snsData = JsonSerializer.Deserialize<SnsToSqsMessageBody>(message.Body);

                this._loggingService.LogInfo(message.Body);

                var eventData = JsonSerializer.Deserialize<MessageWrapper<ProductDTO>>(snsData.Message);

                this._loggingService.LogInfo(snsData.Message);

                using (var updateCatalogueActivity = Activity.Current.Source.StartActivity(
                           "UpdateProductCatalogueCommand",
                           ActivityKind.Consumer))
                {
                    await this._handler.Handle(new UpdateProductCatalogueCommand()
                    {
                        Product = eventData.Data
                    });
                }
            }
        }
    }
}