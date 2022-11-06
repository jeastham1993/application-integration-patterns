using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using NewCustomerPublisher;
using Amazon.EventBridge;
using AWS.Lambda.Powertools.Logging;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using AWS.Lambda.Powertools.Tracing;
using NewCustomerPublisher.Events;

namespace NewCustomerPublisher
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
            this._eventProducer = eventProducer ?? new EventProducer(new AmazonEventBridgeClient());
        }

        [Logging(LogEvent = true)]
        [Tracing]
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            Logger.LogInformation("Received request to create new customer");

            var customerRequest = JsonSerializer.Deserialize<CreateCustomerRequest>(apigProxyEvent.Body);

            var customer = new Customer(customerRequest.EmailAddress, customerRequest.Name);

            Logger.LogInformation("Publishing");

            await this._eventProducer.Publish(new NewCustomerCreatedEvent()
            {
                CustomerId = customer.Id,
            });

            Logger.LogInformation("Returning API response");

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(customer),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }
    }
}
