using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetCommunityMembership;

/// <summary>State of the community membership of the current user, with the GitHub login to propose.</summary>
public sealed record GetCommunityMembershipQuery : IQuery<MembershipViewModel>;
