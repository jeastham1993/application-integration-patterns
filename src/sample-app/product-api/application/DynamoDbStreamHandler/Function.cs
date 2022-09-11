using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using ApplicationIntegrationPatterns.Core.Services;
using ApplicationIntegrationPatterns.Implementations;
using AWS.Lambda.Powertools.Tracing;
using AWS.Lambda.Powertools.Metrics;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.SimpleNotificationService;
using ApplicationIntegrationPatterns.Core.DataTransfer;
using AWS.Lambda.Powertools.Logging;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System;
using Amazon.SimpleNotificationService.Model;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using ApplicationIntegrationPatterns.Core.Events;
using System.Text.Json;

namespace DynamoDbStreamHandler
{
    public class Function
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

        [Tracing]
        [Metrics(CaptureColdStart =true)]
        [Logging(LogEvent = true)]
        public async Task FunctionHandler(DynamoDBEvent dynamoStreamEvent, ILambdaContext context)
        {
            foreach (var evt in dynamoStreamEvent.Records)
            {
                var topicName = "";
                var eventPayload = "";
                var eventType = "";

                this._loggingService.LogInfo($"Processing {evt.EventName}");
                
                switch (evt.EventName.ToString())
                {
                    case "INSERT":
                        var insertRecordAsDocument = Document.FromAttributeMap(evt.Dynamodb.NewImage);
                        topicName = CREATED_TOPIC_ARN;
                        eventType = "product-created";
                        var productDTO = DynamoDbProductAdapter.DynamoDbItemToProduct(insertRecordAsDocument.ToAttributeMap());
                        eventPayload = JsonSerializer.Serialize(productDTO);
                        break;
                    case "MODIFY":
                        var updateRecordAsDocument = Document.FromAttributeMap(evt.Dynamodb.NewImage);
                        topicName = UPDATED_TOPIC_ARN;
                        eventType = "product-updated";
                        var updatedProductDTO = DynamoDbProductAdapter.DynamoDbItemToProduct(updateRecordAsDocument.ToAttributeMap());
                        eventPayload = JsonSerializer.Serialize(updatedProductDTO);
                        break;
                    case "REMOVE":
                        var deleteRecordAsDocument = Document.FromAttributeMap(evt.Dynamodb.OldImage);
                        topicName = DELETED_TOPIC_ARN;
                        eventType = "product-deleted";
                        var deletedProductDTO = DynamoDbProductAdapter.DynamoDbItemToProduct(deleteRecordAsDocument.ToAttributeMap());
                        eventPayload = JsonSerializer.Serialize(deletedProductDTO);
                        break;
                }

                if (string.IsNullOrEmpty(topicName))
                {
                    this._loggingService.LogWarning("Invalid event type");
                    return;
                }
                
                this._loggingService.LogInfo($"Publishing event data to {topicName} with type {eventType}");

                await this._snsClient.PublishAsync(new PublishRequest()
                {
                    Message = eventPayload,
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
        }
    }
}
