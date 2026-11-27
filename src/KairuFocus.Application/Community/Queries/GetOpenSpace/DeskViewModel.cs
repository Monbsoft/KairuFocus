using KairuFocus.Application.Community.Common;

namespace KairuFocus.Application.Community.Queries.GetOpenSpace;

/// <param name="Seated">The host first, then the members by arrival.</param>
/// <param name="EndsAt">End of the current step, already past while the group waits for its host.</param>
/// <param name="StepType">Name of the step type: <c>Sprint</c>, <c>ShortBreak</c> or <c>LongBreak</c>.</param>
/// <param name="Phase">Name of the phase: <c>Running</c> or <c>WaitingForHost</c>.</param>
/// <param name="Ambiance">The ambiance of the host; null when none.</param>
public sealed record DeskViewModel(
    IReadOnlyList<MemberChipViewModel> Seated,
    DateTimeOffset EndsAt,
    string StepType,
    string Phase,
    bool IsClosedToJoin,
    AmbianceViewModel? Ambiance);
