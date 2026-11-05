namespace KairuFocus.Application.Community.Commands.StartGroupBreak;

public sealed record StartGroupBreakResult
{
    public bool IsSuccess { get; init; }

    /// <summary>End of the group break. Null on a success without break: nothing applies to this member.</summary>
    public DateTimeOffset? SharedEndAt { get; init; }

    public string? Error { get; init; }

    private StartGroupBreakResult() { }

    public static StartGroupBreakResult Success(DateTimeOffset sharedEndAt) =>
        new() { IsSuccess = true, SharedEndAt = sharedEndAt };

    public static StartGroupBreakResult NoBreak() =>
        new() { IsSuccess = true };

    public static StartGroupBreakResult Failure(string error) =>
        new() { Error = error };
}
