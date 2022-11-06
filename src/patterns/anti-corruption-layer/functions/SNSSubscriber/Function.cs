using System.Threading.Tasks;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Tracing;
using Amazon.Lambda.CloudWatchEvents;
using AWS.Lambda.Powertools.Logging;
using Amazon.Lambda.SNSEvents;
using System.Text.Json;
using SNSSubscriber.Events;

namespace SNSSubscriber
{
    public class Function
    {
        [Tracing]
        public async Task FunctionHandler(SNSEvent inputEvent, ILambdaContext context)
        {
            foreach (var record in inputEvent.Records)
            {
                var evtData = JsonSerializer.Deserialize<ProductCreatedEvent>(record.Sns.Message);

                Logger.LogInformation(evtData.EventName);
                Logger.LogInformation(evtData.Source);
            }
        }
    }
}
