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
using ApplicationIntegrationPatterns.Core.Events;
using System.Text.Json;

namespace DynamoDbStreamHandler
{
    public class Function
    {
        private static string TOPIC_ARN = Environment.GetEnvironmentVariable("PRODUCT_CREATED_TOPIC_ARN");

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
            this._loggingService.LogInfo($"Received {dynamoStreamEvent.Records.Count} stream records to process");

            foreach (var evt in dynamoStreamEvent.Records)
            {
                if (evt.EventName != OperationType.INSERT)
                {
                    continue;
                }

                this._loggingService.LogInfo($"Processing {evt.EventName}");

                var recordAsDocument = Document.FromAttributeMap(evt.Dynamodb.NewImage);

                var newProduct = DynamoDbProductAdapter.DynamoDbItemToProduct(recordAsDocument.ToAttributeMap());

                await this._snsClient.PublishAsync(new PublishRequest()
                {
                    Message = JsonSerializer.Serialize(new ProductCreatedEvent()
                    {
                        Product = new ProductDTO(newProduct)
                    }),
                    TopicArn = TOPIC_ARN,
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>(1)
                    {
                        { 
                            "EVENT_TYPE", new MessageAttributeValue() { StringValue = "product-created", DataType = "String" } 
                        }
                    }
                });
            }
        }
    }
}
