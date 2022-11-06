using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleNotificationService;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;
using Membership.Shared;
using MembershipCustomerCreatedAdapter.Events;

namespace MembershipCustomerCreatedAdapter
{
    public class Function
    {
        private AmazonSimpleNotificationServiceClient _snsClient;

        public Function()
        {
            this._snsClient = new AmazonSimpleNotificationServiceClient();
        }
        
        [Tracing]
        public async Task FunctionHandler(SQSEvent inputEvent, ILambdaContext context)
        {
            foreach (var queuedMessage in inputEvent.Records)
            {
                Logger.LogInformation(queuedMessage.Body);
                
                var receivedEvent = JsonSerializer.Deserialize<CloudWatchEvent<NewCustomerCreatedEvent>>(queuedMessage.Body, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
                
                Logger.LogInformation($"Received event for {receivedEvent.Detail.CustomerId}");
                
                await this._snsClient.PublishAsync(Environment.GetEnvironmentVariable("TOPIC_ARN"),
                    JsonSerializer.Serialize(new NewCustomerCreatedEventReceived()
                    {
                        MemberCustomerId = receivedEvent.Detail.CustomerId
                    }));
            }
            
        }
    }
}
