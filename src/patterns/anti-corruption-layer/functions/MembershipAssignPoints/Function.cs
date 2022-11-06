using System.Threading.Tasks;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;
using MembershipAssignPoints.Events;

namespace MembershipAssignPoints
{
    public class Function
    {
        [Tracing]
        public async Task FunctionHandler(CloudWatchEvent<NewCustomerCreatedEvent> inputEvent, ILambdaContext context)
        {
            Logger.LogInformation($"Received event for {inputEvent.Detail.CustomerId}");
            
            var member = new Member()
            {
                MemberId = inputEvent.Detail.CustomerId
            };
            
            member.RegisterInitialMembershipPoints();
        }
    }
}
