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
using OpenTelemetry.Trace;
using Shared;
using Shared.Messaging;

namespace DynamoDbStreamHandler
{
    public class Function : DynamoDbStreamTracedFunction<string>
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
                var hydratedContext = this.HydrateContextFromStreamRecord(evt);
                
                using var span = Activity.Current.Source.StartActivity("HandlingDynamoDbStream", ActivityKind.Consumer, parentContext: hydratedContext).AddDynamoDbAttributes(evt);
                
                var topicName = "";
                var eventType = "";
                ProductDTO productData = null;

                using var documentAttributeMapping = Activity.Current.Source.StartActivity("MappingDocumentAttributes");
                
                this._loggingService.LogInfo($"Processing {evt.EventName}");
                
                switch (evt.EventName.ToString())
                {
                    case "INSERT":
                        var insertRecordAsDocument = Document.FromAttributeMap(evt.Dynamodb.NewImage);
                        topicName = CREATED_TOPIC_ARN;
                        eventType = "product-created";
                        productData =
                            new ProductDTO(
                                DynamoDbProductAdapter.DynamoDbItemToProduct(insertRecordAsDocument.ToAttributeMap()));
                        break;
                    case "MODIFY":
                        var updateRecordAsDocument = Document.FromAttributeMap(evt.Dynamodb.NewImage);
                        topicName = UPDATED_TOPIC_ARN;
                        eventType = "product-updated";
                        productData =
                            new ProductDTO(
                                DynamoDbProductAdapter.DynamoDbItemToProduct(updateRecordAsDocument.ToAttributeMap()));
                        break;
                    case "REMOVE":
                        var deleteRecordAsDocument = Document.FromAttributeMap(evt.Dynamodb.OldImage);
                        topicName = DELETED_TOPIC_ARN;
                        eventType = "product-deleted";
                        productData =
                            new ProductDTO(
                                DynamoDbProductAdapter.DynamoDbItemToProduct(deleteRecordAsDocument.ToAttributeMap()));
                        break;
                }

                if (string.IsNullOrEmpty(topicName))
                {
                    documentAttributeMapping.RecordException(new Exception("Topic is not found"));
                    
                    this._loggingService.LogWarning("Invalid event type");
                    
                    return "OK";
                }
                
                documentAttributeMapping.Dispose();
                
                this._loggingService.LogInfo($"Publishing event data to {topicName} with type {eventType}");

                using var snsPublishActivity = Activity.Current.Source.StartActivity("SNSPublish");

                await this._snsClient.PublishAsync(new PublishRequest()
                {
                    Message = JsonSerializer.Serialize(new MessageWrapper<ProductDTO>(){Data = productData}),
                    TopicArn = topicName,
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>(1)
                    {
                        {
                            "EVENT_TYPE",
                            new MessageAttributeValue() {StringValue = eventType, DataType = "String"}
                        }
                    }
                });
                
                span.Dispose();
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
