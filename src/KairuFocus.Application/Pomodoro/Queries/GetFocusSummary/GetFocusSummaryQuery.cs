using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Pomodoro.Queries.GetFocusSummary;

/// <summary>
/// Query for the dashboard "Focus today" summary (no parameters needed).
/// </summary>
public sealed record GetFocusSummaryQuery : IQuery<GetFocusSummaryResult>;
