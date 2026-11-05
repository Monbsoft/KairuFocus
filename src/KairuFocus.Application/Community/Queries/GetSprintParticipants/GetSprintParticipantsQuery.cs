using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetSprintParticipants;

/// <summary>UC-CO-08 — the other members of the group of the current member. The group is never a parameter:
/// a member can only read their own.</summary>
public sealed record GetSprintParticipantsQuery : IQuery<GetSprintParticipantsResult>;
