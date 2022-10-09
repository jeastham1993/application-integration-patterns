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
using OpenTelemetry.Trace;
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
        
        public override string SERVICE_NAME => "ExternalEventPublisher";
        public override Func<SQSEvent.SQSMessage, ILambdaContext, Task> MessageProcessor => MessageHandler;

        internal Function(AmazonEventBridgeClient eventBridgeClient = null, ILoggingService loggingService = null,
            SystemParameters _parameters = null)
        {
            Startup.ConfigureServices();

            this._eventBridgeClient = eventBridgeClient ?? new AmazonEventBridgeClient();
            this._loggingService = loggingService ?? Startup.Services.GetRequiredService<ILoggingService>();
            var parameters = _parameters ?? Startup.Services.GetRequiredService<SystemParameters>();

            _eventBusName = parameters.RetrieveParameter(Environment.GetEnvironmentVariable("EVENT_BUS_PARAMETER"))
                .Result;
        }

        public async Task MessageHandler(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            this._loggingService.LogInfo($"MessageHandler: {message.MessageId}");

            using (var activity =
                   Activity.Current.Source
                       .StartActivity("PublishExternalEvent", ActivityKind.Consumer)
                       .AddSqsAttributes(message))
            {
                var snsData = JsonSerializer.Deserialize<SnsToSqsMessageBody>(message.Body);

                this._loggingService.LogInfo(message.Body);

                var eventPayload = snsData.Message;
                var eventType = snsData.MessageAttributes["EVENT_TYPE"].Value;

                this._loggingService.LogInfo($"Adding event from {eventType}");

                using (var publishActivity = Activity.Current.Source.StartActivity("EventBridgePublish",
                           ActivityKind.Consumer))
                {
                    await this._eventBridgeClient.PutEventsAsync(new PutEventsRequest()
                    {
                        Entries = new List<PutEventsRequestEntry>()
                        {
                            new PutEventsRequestEntry()
                            {
                                Detail = eventPayload,
                                Source = "product-api",
                                DetailType = eventType,
                                EventBusName = _eventBusName
                            }
                        }
                    });
                }
            }
        }
    }
}