using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using EventBridgePublisher;
using AWS.Lambda.Powertools.Tracing;
using Amazon.Lambda.CloudWatchEvents;
using AWS.Lambda.Powertools.Logging;

namespace EventBridgeSubscriber
{
    public class Function
    {
        [Tracing]
        public async Task FunctionHandler(CloudWatchEvent<ProductCreatedEvent> inputEvent, ILambdaContext context)
        {
            Logger.LogInformation(inputEvent.Detail.EventName);
        }
    }
}
