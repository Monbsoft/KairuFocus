using KairuFocus.Application.Community.Queries.GetCommunityMembership;

namespace KairuFocus.Application.Community.Commands.JoinCommunity;

public sealed record JoinCommunityResult
{
    public bool IsSuccess { get; init; }
    public MembershipViewModel? Membership { get; init; }
    public string? Error { get; init; }

    private JoinCommunityResult() { }

    public static JoinCommunityResult Success(MembershipViewModel membership) =>
        new() { IsSuccess = true, Membership = membership };

    public static JoinCommunityResult Failure(string error) => new() { Error = error };
}
