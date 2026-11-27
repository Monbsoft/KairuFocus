using KairuFocus.Application.Community.Common;

namespace KairuFocus.Application.Community.Queries.GetCurrentSession;

/// <param name="Role">"Host" or "Participant".</param>
/// <param name="Phase">"Running", "WaitingForHost" or "Ended".</param>
/// <param name="StepType">"Sprint", "ShortBreak" or "LongBreak".</param>
/// <param name="WaitLimitAt">Only while waiting for the host.</param>
/// <param name="EndReason">Only when the group is over: a <c>GroupSessionEndReason</c> name.</param>
/// <param name="Ambiance">The ambiance of the host (of the group, or the viewer for their solo open sprint); null when none.</param>
public sealed record GetCurrentSessionResult(
    bool IsGroup,
    string Role,
    string Phase,
    int StepNumber,
    int SprintNumber,
    string StepType,
    DateTimeOffset StepEndsAt,
    DateTimeOffset? WaitLimitAt,
    string? EndReason,
    IReadOnlyList<SprintMemberViewModel> Others,
    bool IsClosedToJoin,
    AmbianceViewModel? Ambiance);
