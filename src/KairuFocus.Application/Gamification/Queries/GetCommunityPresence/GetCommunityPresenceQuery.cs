using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Gamification.Queries.GetCommunityPresence;

/// <summary>UC-XP-10 — anonymous live presence: sprints in progress now, members who completed a sprint today (UTC day).</summary>
public sealed record GetCommunityPresenceQuery : IQuery<GetCommunityPresenceResult>;
