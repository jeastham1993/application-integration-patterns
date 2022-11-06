using System.Threading.Tasks;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;
using MembershipUpdateAnalytics.Events;

namespace MembershipUpdateAnalytics
{
    public class Function
    {
        [Tracing]
        public async Task FunctionHandler(CloudWatchEvent<NewCustomerCreatedEvent> inputEvent, ILambdaContext context)
        {
            Logger.LogInformation($"Received event for {inputEvent.Detail.CustomerId}");

            Logger.LogInformation("Updating analytics");

            // Update analytics
        }
    }
}
