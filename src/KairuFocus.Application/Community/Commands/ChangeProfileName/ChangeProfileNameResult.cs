using KairuFocus.Application.Community.Queries.GetCommunityMembership;

namespace KairuFocus.Application.Community.Commands.ChangeProfileName;

public sealed record ChangeProfileNameResult
{
    public bool IsSuccess { get; init; }
    public MembershipViewModel? Membership { get; init; }
    public string? Error { get; init; }

    private ChangeProfileNameResult() { }

    public static ChangeProfileNameResult Success(MembershipViewModel membership) =>
        new() { IsSuccess = true, Membership = membership };

    public static ChangeProfileNameResult Failure(string error) => new() { Error = error };
}
