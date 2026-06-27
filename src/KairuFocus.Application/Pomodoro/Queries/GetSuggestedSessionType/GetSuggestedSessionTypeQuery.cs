using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Pomodoro.Queries.GetSuggestedSessionType;

/// <summary>
/// Query for the suggested next session type.
/// OffsetMinutes: the client's UTC offset in minutes (local = UTC + offset).
/// Default 0 preserves UTC behaviour for backward compatibility.
/// </summary>
public sealed record GetSuggestedSessionTypeQuery(int OffsetMinutes = 0) : IQuery<GetSuggestedSessionTypeResult>;
