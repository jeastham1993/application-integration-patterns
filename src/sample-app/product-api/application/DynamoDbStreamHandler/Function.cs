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

namespace DynamoDbStreamHandler
{
    public class Function
    {
        private readonly ILoggingService _loggingService;
        private readonly AmazonSimpleNotificationServiceClient _snsClient;

        public Function() : this(null, null)
        {
        }

        internal Function(ILoggingService loggingService = null, AmazonSimpleNotificationServiceClient snsClient = null)
        {
            Startup.ConfigureServices();
            
            this._loggingService = loggingService ?? Startup.Services.GetRequiredService<ILoggingService>();
            this._snsClient = snsClient;
        }

        [Tracing]
        [Metrics(CaptureColdStart =true)]
        public async Task FunctionHandler(DynamoDBEvent dynamoStreamEvent, ILambdaContext context)
        {
            this._loggingService.LogInfo($"Received {dynamoStreamEvent.Records.Count} stream records to process");

            foreach (var evt in dynamoStreamEvent.Records)
            {
                this._loggingService.LogInfo($"Processing {evt.EventName}");

                var newProduct = DynamoDbProductAdapter.DynamoDbItemToProduct(evt.Dynamodb.NewImage);

                await this._snsClient.PublishAsync("PRODUCT_CREATED_TOPIC_ARN", new ProductDTO(newProduct).ToString());
            }
        }
    }
}
