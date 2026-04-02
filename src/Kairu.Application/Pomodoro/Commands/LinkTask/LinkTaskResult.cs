namespace Kairu.Application.Pomodoro.Commands.LinkTask;

public sealed record LinkTaskResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public string? Error { get; init; }

    private LinkTaskResult() { }

    public static LinkTaskResult Success() => new() { IsSuccess = true };
    public static LinkTaskResult NotFound() => new() { IsNotFound = true };
    public static LinkTaskResult Failure(string error) => new() { Error = error };
}
