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
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using System.Collections.Generic;
using Amazon.SimpleSystemsManagement;
using System;
using System.Diagnostics;
using System.Linq;
using Amazon.Lambda.SQSEvents;
using AWS.Lambda.Powertools.Logging;
using Amazon.Lambda.SNSEvents;
using static Amazon.Lambda.SNSEvents.SNSEvent;
using ApplicationIntegrationPatterns.Implementations.Models;
using Shared;

namespace ExternalEventPublisher
{
    public class Function : SqsTracedFunction<string>
    {
        private readonly AmazonEventBridgeClient _eventBridgeClient;
        private readonly ILoggingService _loggingService;
        private readonly string _eventBusName;

        public Function() : this(null, null, null)
        {
        }

        internal Function(AmazonEventBridgeClient eventBridgeClient = null, ILoggingService loggingService = null, SystemParameters _parameters = null)
        {
            Startup.ConfigureServices();

            this._eventBridgeClient = eventBridgeClient ?? new AmazonEventBridgeClient();
            this._loggingService = loggingService ?? Startup.Services.GetRequiredService<ILoggingService>();
            var parameters = _parameters ?? Startup.Services.GetRequiredService<SystemParameters>();

            _eventBusName = parameters.RetrieveParameter(Environment.GetEnvironmentVariable("EVENT_BUS_PARAMETER")).Result;
        }
        
        public async Task<string> FunctionHandler(SQSEvent evt, ILambdaContext context)
        {
            this._loggingService.LogInfo($"Processing {evt.Records} SNS events");

            var eventsToPublish = new List<PutEventsRequestEntry>(evt.Records.Count);

            foreach (var record in evt.Records)
            {
                var hydratedContext = this.HydrateContextFromSnsMessage(record);

                using var activity =
                    Activity.Current.Source.StartActivity("PublishExternalEvent", ActivityKind.Consumer, parentContext: hydratedContext).AddSqsAttributes(record);
                
                var snsData = JsonSerializer.Deserialize<SnsToSqsMessageBody>(record.Body);

                this._loggingService.LogInfo(record.Body);

                var eventPayload = snsData.Message;
                var eventType = snsData.MessageAttributes["EVENT_TYPE"].Value;

                this._loggingService.LogInfo($"Adding event from {eventType}");

                eventsToPublish.Add(new PutEventsRequestEntry()
                {
                    Detail = eventPayload,
                    Source = "product-api",
                    DetailType = eventType,
                    EventBusName = _eventBusName
                });

                if (eventsToPublish.Count == 10)
                {
                    this._loggingService.LogInfo("10 records reached, publishing events");

                    await this._eventBridgeClient.PutEventsAsync(new PutEventsRequest()
                    {
                        Entries = eventsToPublish
                    });

                    eventsToPublish = new List<PutEventsRequestEntry>(evt.Records.Count);
                }
            }

            if (eventsToPublish.Count == 0)
            {
                this._loggingService.LogInfo("No events to publish, returning");

                return "OK";
            }

            this._loggingService.LogInfo($"Publishing {eventsToPublish.Count} event(s)");

            await this._eventBridgeClient.PutEventsAsync(new PutEventsRequest()
            {
                Entries = eventsToPublish
            });

            return "OK";
        }

        public override string SERVICE_NAME => "ExternalEventPublisher";
        public override Func<SQSEvent, ILambdaContext, Task<string>> Handler => FunctionHandler;
    }
}
