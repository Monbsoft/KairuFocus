namespace Kairudev.Application.Pomodoro.Commands.InterruptSession;

public sealed record InterruptSessionResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    private InterruptSessionResult() { }

    public static InterruptSessionResult Success() => new() { IsSuccess = true };
    public static InterruptSessionResult Failure(string error) => new() { Error = error };
}
