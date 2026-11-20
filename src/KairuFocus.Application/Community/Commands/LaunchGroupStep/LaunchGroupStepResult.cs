namespace KairuFocus.Application.Community.Commands.LaunchGroupStep;

public sealed record LaunchGroupStepResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    /// <summary>True when no member was left: the group ended and no step was launched (UC-CO-10 A1).</summary>
    public bool HostAlone { get; init; }

    /// <summary>Exact end of the step launched, the same instant for every member. Null when <see cref="HostAlone"/>.</summary>
    public DateTimeOffset? StepEndsAt { get; init; }

    private LaunchGroupStepResult() { }

    public static LaunchGroupStepResult Success(DateTimeOffset stepEndsAt) =>
        new() { IsSuccess = true, StepEndsAt = stepEndsAt };

    public static LaunchGroupStepResult EndedHostAlone() =>
        new() { IsSuccess = true, HostAlone = true };

    public static LaunchGroupStepResult Failure(string error) =>
        new() { Error = error };
}
