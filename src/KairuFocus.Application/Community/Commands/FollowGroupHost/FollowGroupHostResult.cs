namespace KairuFocus.Application.Community.Commands.FollowGroupHost;

/// <summary>
/// What following the host gave. A success always carries an <see cref="Outcome"/>; <see cref="Error"/> is
/// only set for a refusal (not a member, another session already active).
/// </summary>
public sealed record FollowGroupHostResult
{
    public const string NewStepOutcome = "NewStep";
    public const string SameStepOutcome = "SameStep";
    public const string WaitingOutcome = "Waiting";
    public const string EndedOutcome = "Ended";
    public const string NotInGroupOutcome = "NotInGroup";

    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    /// <summary><c>NewStep</c>, <c>SameStep</c>, <c>Waiting</c>, <c>Ended</c> or <c>NotInGroup</c>.</summary>
    public string? Outcome { get; init; }

    /// <summary>Type of the current step (<c>Sprint</c>, <c>ShortBreak</c>, <c>LongBreak</c>), for a step or a wait.</summary>
    public string? StepType { get; init; }

    /// <summary>Exact end of the current step, for a step or a wait.</summary>
    public DateTimeOffset? StepEndsAt { get; init; }

    /// <summary>Instant the group ends if the host launches nothing, while waiting.</summary>
    public DateTimeOffset? WaitLimitAt { get; init; }

    /// <summary>Why the group ended (<c>Stopped</c>, <c>HostAbsent</c>, <c>HostAlone</c>, <c>HostLeftCommunity</c>).</summary>
    public string? EndReason { get; init; }

    /// <summary>XP credited by closing the participant's own session in this request (0 otherwise).</summary>
    public int XpAwarded { get; init; }

    private FollowGroupHostResult() { }

    public static FollowGroupHostResult NewStep(string stepType, DateTimeOffset stepEndsAt, int xpAwarded) =>
        new()
        {
            IsSuccess = true,
            Outcome = NewStepOutcome,
            StepType = stepType,
            StepEndsAt = stepEndsAt,
            XpAwarded = xpAwarded,
        };

    public static FollowGroupHostResult SameStep(string stepType, DateTimeOffset stepEndsAt) =>
        new() { IsSuccess = true, Outcome = SameStepOutcome, StepType = stepType, StepEndsAt = stepEndsAt };

    public static FollowGroupHostResult Waiting(string stepType, DateTimeOffset stepEndsAt, DateTimeOffset waitLimitAt) =>
        new()
        {
            IsSuccess = true,
            Outcome = WaitingOutcome,
            StepType = stepType,
            StepEndsAt = stepEndsAt,
            WaitLimitAt = waitLimitAt,
        };

    public static FollowGroupHostResult Ended(string? endReason, int xpAwarded) =>
        new() { IsSuccess = true, Outcome = EndedOutcome, EndReason = endReason, XpAwarded = xpAwarded };

    public static FollowGroupHostResult NotInGroup() =>
        new() { IsSuccess = true, Outcome = NotInGroupOutcome };

    public static FollowGroupHostResult Failure(string error) =>
        new() { Error = error };
}
