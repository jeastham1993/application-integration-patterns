using AWS.Lambda.Powertools.Logging;

namespace MembershipAssignPoints;

public class Member
{
    public string MemberId { get; set; }

    public void RegisterInitialMembershipPoints()
    {
        Logger.LogInformation("20 signup points added");
    }
}