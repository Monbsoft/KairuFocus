namespace Kairudev.Application.Pomodoro.Commands.CompleteSession;

public sealed record CompleteSessionResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    private CompleteSessionResult() { }

    public static CompleteSessionResult Success() => new() { IsSuccess = true };
    public static CompleteSessionResult Failure(string error) => new() { Error = error };
}
