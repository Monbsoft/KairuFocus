using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetMemberProfile;

/// <summary>UC-CO-04 — public profile of a member, by name. Also the public part of UC-CO-05 (my own profile).</summary>
public sealed record GetMemberProfileQuery(string Name) : IQuery<GetMemberProfileResult>;
