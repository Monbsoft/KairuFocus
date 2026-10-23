using KairuFocus.Application.Gamification.Common;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Gamification.Queries.GetCommunityProgress;

/// <summary>UC-XP-07 — the current user's own progression for the period. Reads no other user's data.</summary>
public sealed record GetCommunityProgressQuery(CommunityPeriod Period) : IQuery<GetCommunityProgressResult>;
