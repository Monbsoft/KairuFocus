namespace KairuFocus.Application.Community.Queries.GetOpenSprints;

/// <param name="EndsAt">End of the current step, already past while the group waits for its host.</param>
/// <param name="StepType">Name of the step type: <c>Sprint</c>, <c>ShortBreak</c> or <c>LongBreak</c>.</param>
/// <param name="Phase">Name of the phase: <c>Running</c> or <c>WaitingForHost</c>.</param>
public sealed record OpenSprintRowViewModel(
    string Name,
    string? AvatarUrl,
    DateTimeOffset EndsAt,
    int ParticipantCount,
    bool IsMine,
    string StepType,
    string Phase);
