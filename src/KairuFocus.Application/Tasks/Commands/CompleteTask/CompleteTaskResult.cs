namespace KairuFocus.Application.Tasks.Commands.CompleteTask;

/// <summary>
/// Result of the CompleteTask command.
/// </summary>
public sealed record CompleteTaskResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public string? Error { get; init; }

    private CompleteTaskResult() { }

    public static CompleteTaskResult Success() =>
        new() { IsSuccess = true };

    public static CompleteTaskResult NotFound() =>
        new() { IsNotFound = true };

    public static CompleteTaskResult Failure(string error) =>
        new() { Error = error };
}
