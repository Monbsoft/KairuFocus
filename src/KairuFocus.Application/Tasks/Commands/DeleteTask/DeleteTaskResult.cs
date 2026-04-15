namespace KairuFocus.Application.Tasks.Commands.DeleteTask;

public sealed record DeleteTaskResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public string? Error { get; init; }

    private DeleteTaskResult() { }

    public static DeleteTaskResult Success() => new() { IsSuccess = true };
    public static DeleteTaskResult NotFound() => new() { IsNotFound = true };
    public static DeleteTaskResult Failure(string error) => new() { Error = error };
}
