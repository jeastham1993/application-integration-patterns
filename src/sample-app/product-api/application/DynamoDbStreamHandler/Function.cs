using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using ApplicationIntegrationPatterns.Core.Services;
using ApplicationIntegrationPatterns.Implementations;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.SimpleNotificationService;
using ApplicationIntegrationPatterns.Core.DataTransfer;
using Amazon.DynamoDBv2.DocumentModel;
using System;
using Amazon.SimpleNotificationService.Model;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
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

        public async Task MessageHandler(DynamoDBEvent.DynamodbStreamRecord record, ILambdaContext context)
        {
            using var span = Activity.Current.Source
                .StartActivity("HandlingDynamoDbStream", ActivityKind.Consumer)
                .AddDynamoDbAttributes(record);

            var topicName = "";
            var eventType = "";
            ProductDTO productData = null;

            using var documentAttributeMapping = Activity.Current.Source.StartActivity("MappingDocumentAttributes");

            this._loggingService.LogInfo($"Processing {record.EventName}");

            switch (record.EventName.ToString())
            {
                case "INSERT":
                    var insertRecordAsDocument = Document.FromAttributeMap(record.Dynamodb.NewImage);
                    topicName = CREATED_TOPIC_ARN;
                    eventType = "product-created";
                    productData =
                        new ProductDTO(
                            DynamoDbProductAdapter.DynamoDbItemToProduct(insertRecordAsDocument.ToAttributeMap()));
                    break;
                case "MODIFY":
                    var updateRecordAsDocument = Document.FromAttributeMap(record.Dynamodb.NewImage);
                    topicName = UPDATED_TOPIC_ARN;
                    eventType = "product-updated";
                    productData =
                        new ProductDTO(
                            DynamoDbProductAdapter.DynamoDbItemToProduct(updateRecordAsDocument.ToAttributeMap()));
                    break;
                case "REMOVE":
                    var deleteRecordAsDocument = Document.FromAttributeMap(record.Dynamodb.OldImage);
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

                return;
            }

            documentAttributeMapping.Dispose();

            this._loggingService.LogInfo($"Publishing event data to {topicName} with type {eventType}");

            using var snsPublishActivity = Activity.Current.Source.StartActivity("SNSPublish");

            await this._snsClient.PublishAsync(new PublishRequest()
            {
                Message = JsonSerializer.Serialize(new MessageWrapper<ProductDTO>() {Data = productData}),
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

        public override string SERVICE_NAME => "DynamoDbStreamHandler";

        public override Func<DynamoDBEvent.DynamodbStreamRecord, ILambdaContext, Task> MessageProcessor => MessageHandler;
        public override Func<DynamoDBEvent.DynamodbStreamRecord, Activity, bool> AddRequestAttributes => (evt, ctx) =>
        {
            return true;
        };

        public override Func<string, Activity, bool> AddResponseAttributes => (evt, ctx) => { return true; };
    }
}