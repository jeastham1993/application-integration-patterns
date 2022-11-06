using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;
using Membership.Shared;

namespace MembershipAssignPoints
{
    public class Function
    {
        [Tracing]
        public async Task FunctionHandler(SNSEvent inputEvent, ILambdaContext context)
        {
            foreach (var snsEvent in inputEvent.Records)
            {
                var evtPayload = JsonSerializer.Deserialize<NewCustomerCreatedEventReceived>(snsEvent.Sns.Message);
                
                Logger.LogInformation($"Received data for member with customer id {evtPayload.MemberCustomerId}");
                
                var member = new Member()
                {
                    MemberId = evtPayload.MemberCustomerId
                };

                Logger.LogInformation("Adding additional membership points");
            
                member.RegisterInitialMembershipPoints();
            }
        }
    }
}
