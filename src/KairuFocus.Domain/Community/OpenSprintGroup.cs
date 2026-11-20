namespace KairuFocus.Domain.Community;

/// <summary>
/// A sprint open to members, or a group session open to new members. <see cref="RootSessionId"/> stays on the
/// server side, never in the API. <see cref="EndsAtUtc"/> is the end of the current step: already past while
/// the group waits for its host. A sprint nobody joined yet is a running <see cref="GroupStepType.Sprint"/>.
/// </summary>
public sealed record OpenSprintGroup(
    Guid RootSessionId,
    SprintMember Lead,
    DateTime EndsAtUtc,
    int ParticipantCount,
    GroupStepType StepType,
    GroupPhase Phase);
