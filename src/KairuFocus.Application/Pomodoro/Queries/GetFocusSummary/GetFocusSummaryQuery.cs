using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Pomodoro.Queries.GetFocusSummary;

/// <summary>
/// Query for the dashboard "Focus today" summary.
/// OffsetMinutes: the client's UTC offset in minutes (local = UTC + offset).
/// Default 0 preserves UTC behaviour for backward compatibility and existing tests.
/// </summary>
public sealed record GetFocusSummaryQuery(int OffsetMinutes = 0) : IQuery<GetFocusSummaryResult>;
