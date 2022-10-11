using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using ApplicationIntegrationPatterns.Core.Command;
using Microsoft.Extensions.DependencyInjection;
using ApplicationIntegrationPatterns.Core.Services;
using ApplicationIntegrationPatterns.Implementations;
using ApplicationIntegrationPatterns.Core.Events;
using System.Collections.Generic;
using Amazon.SimpleSystemsManagement;
using System;
using System.Diagnostics;
using System.Linq;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.Logging;
using Amazon.Lambda.SNSEvents;
using ApplicationIntegrationPatterns.Core.DataTransfer;
using static Amazon.Lambda.SNSEvents.SNSEvent;
using ApplicationIntegrationPatterns.Implementations.Models;
using OpenTelemetry.Trace;
using Shared;
using Shared.Messaging;

namespace UpdateProductCatalogue
{
    public class Function : SqsTracedFunction<string>
    {
        private readonly ILoggingService _loggingService;
        private readonly UpdateProductCatalogueCommandHandler _handler;

        public Function() : this(null, null)
        {
        }
        
        public override string SERVICE_NAME => "ExternalEventPublisher";
        public override Func<SQSEvent.SQSMessage, ILambdaContext, Task> MessageProcessor => MessageHandler;

        internal Function(ILoggingService loggingService = null,
            UpdateProductCatalogueCommandHandler handler = null)
        {
            Startup.ConfigureServices();
            
            this._loggingService = loggingService ?? Startup.Services.GetRequiredService<ILoggingService>();
            this._handler = handler ?? Startup.Services.GetRequiredService<UpdateProductCatalogueCommandHandler>();
        }

        public async Task MessageHandler(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            this._loggingService.LogInfo($"MessageHandler: {message.MessageId}");
            
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