using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetActiveToday;

/// <summary>UC-CO-13 — the members who finished a sprint today (UTC day).</summary>
public sealed record GetActiveTodayQuery : IQuery<GetActiveTodayResult>;
