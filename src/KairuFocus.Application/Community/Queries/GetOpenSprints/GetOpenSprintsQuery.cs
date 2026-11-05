using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetOpenSprints;

/// <summary>UC-CO-06 — the sprints a member can join now, one row per group.</summary>
public sealed record GetOpenSprintsQuery : IQuery<GetOpenSprintsResult>;
