using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Pomodoro.Queries.GetTodaySprintSessions;

/// <summary>
/// Query for the free sprints started "today" in the user's local day.
/// OffsetMinutes: the client's UTC offset in minutes (local = UTC + offset).
/// Default 0 preserves UTC behaviour for backward compatibility and existing tests.
/// </summary>
public sealed record GetTodaySprintSessionsQuery(int OffsetMinutes = 0) : IQuery<GetTodaySprintSessionsResult>;
