using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using ApplicationIntegrationPatterns.Core.Services;
using ApplicationIntegrationPatterns.Implementations;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.SimpleNotificationService;
using ApplicationIntegrationPatterns.Core.DataTransfer;
using AWS.Lambda.Powertools.Logging;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System;
using Amazon.SimpleNotificationService.Model;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using ApplicationIntegrationPatterns.Core.Events;
using System.Text.Json;
using ApplicationIntegrationPatterns.Core.Models;
using Shared;
using Shared.Messaging;

namespace DynamoDbStreamHandler
{
    public class Function : TracedFunction<DynamoDBEvent, string>
    {
        private static string CREATED_TOPIC_ARN = Environment.GetEnvironmentVariable("PRODUCT_CREATED_TOPIC_ARN");
        private static string UPDATED_TOPIC_ARN = Environment.GetEnvironmentVariable("PRODUCT_UPDATED_TOPIC_ARN");
        private static string DELETED_TOPIC_ARN = Environment.GetEnvironmentVariable("PRODUCT_DELETED_TOPIC_ARN");

        private readonly ILoggingService _loggingService;
        private readonly AmazonSimpleNotificationServiceClient _snsClient;

        public Function() : this(null, null)
        {
        }

        internal Function(ILoggingService loggingService = null, AmazonSimpleNotificationServiceClient snsClient = null)
        {
            Startup.ConfigureServices();
            
            this._loggingService = loggingService ?? Startup.Services.GetRequiredService<ILoggingService>();
            this._snsClient = snsClient ?? new AmazonSimpleNotificationServiceClient();
        }
        
        public async Task<string> FunctionHandler(DynamoDBEvent dynamoStreamEvent, ILambdaContext context)
        {
            foreach (var evt in dynamoStreamEvent.Records)
            {
                var topicName = "";
                var eventType = "";
                Product productData = null;

                this._loggingService.LogInfo($"Processing {evt.EventName}");
                
                switch (evt.EventName.ToString())
                {
                    case "INSERT":
                        var insertRecordAsDocument = Document.FromAttributeMap(evt.Dynamodb.NewImage);
                        topicName = CREATED_TOPIC_ARN;
                        eventType = "product-created";
                        productData = DynamoDbProductAdapter.DynamoDbItemToProduct(insertRecordAsDocument.ToAttributeMap());
                        break;
                    case "MODIFY":
                        var updateRecordAsDocument = Document.FromAttributeMap(evt.Dynamodb.NewImage);
                        topicName = UPDATED_TOPIC_ARN;
                        eventType = "product-updated";
                        productData = DynamoDbProductAdapter.DynamoDbItemToProduct(updateRecordAsDocument.ToAttributeMap());
                        break;
                    case "REMOVE":
                        var deleteRecordAsDocument = Document.FromAttributeMap(evt.Dynamodb.OldImage);
                        topicName = DELETED_TOPIC_ARN;
                        eventType = "product-deleted";
                        productData = DynamoDbProductAdapter.DynamoDbItemToProduct(deleteRecordAsDocument.ToAttributeMap());
                        break;
                }

                if (string.IsNullOrEmpty(topicName))
                {
                    this._loggingService.LogWarning("Invalid event type");
                    return "OK";
                }
                
                this._loggingService.LogInfo($"Publishing event data to {topicName} with type {eventType}");

                using var snsPublishActivity = Activity.Current.Source.StartActivity("SNSPublish");

                await this._snsClient.PublishAsync(new PublishRequest()
                {
                    Message = JsonSerializer.Serialize(new MessageWrapper<Product>(){Data = productData}),
                    TopicArn = topicName,
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>(1)
                    {
                        {
                            "EVENT_TYPE",
                            new MessageAttributeValue() {StringValue = eventType, DataType = "String"}
                        }
                    }
                });
            }
            
            return "OK";
        }

        public override string SERVICE_NAME => "DynamoDbStreamHandler";
        public override Func<DynamoDBEvent, ILambdaContext, Task<string>> Handler => FunctionHandler;

        public override Func<DynamoDBEvent, ILambdaContext, bool> ContextPropagator => (evt, ctx) =>
        {
            return true;
        };
        
        public override Func<DynamoDBEvent, Activity, bool> AddRequestAttributes => (evt, ctx) =>
        {
            return true;
        };
        public override Func<string, Activity, bool> AddResponseAttributes => (evt, ctx) =>
        {
            return true;
        };
    }
}
