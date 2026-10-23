using KairuFocus.Application.Gamification.Common;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Gamification.Queries.GetCommunityRecap;

/// <summary>UC-XP-09 — anonymous collective recap of the period, all members (participants or not).</summary>
public sealed record GetCommunityRecapQuery(CommunityPeriod Period) : IQuery<GetCommunityRecapResult>;
