using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Logging;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using AWS.Lambda.Powertools.Tracing;
using Amazon.SimpleNotificationService;
using SNSPublisher.Events;

namespace SNSPublisher
{
    public class Function
    {
        private readonly EventProducer _eventProducer;

        public Function() : this(null)
        {
            AWSSDKHandler.RegisterXRayForAllServices();
        }

        internal Function(EventProducer eventProducer)
        {
            this._eventProducer = eventProducer ?? new EventProducer(new AmazonSimpleNotificationServiceClient());
        }

        [Logging(LogEvent = true)]
        [Tracing]
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            Logger.LogInformation("Received request to publish event");

            var evt = new ProductCreatedEvent();

            Logger.LogInformation("Publishing");

            await this._eventProducer.Publish(evt);

            Logger.LogInformation("Returning API response");

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(evt),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
