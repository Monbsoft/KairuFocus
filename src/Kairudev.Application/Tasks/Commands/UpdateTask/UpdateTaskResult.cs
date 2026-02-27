using Kairudev.Application.Tasks.Common;

namespace Kairudev.Application.Tasks.Commands.UpdateTask;

public sealed record UpdateTaskResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public TaskViewModel? Task { get; init; }
    public string? Error { get; init; }

    private UpdateTaskResult() { }

    public static UpdateTaskResult Success(TaskViewModel task) =>
        new() { IsSuccess = true, Task = task };

    public static UpdateTaskResult NotFound() =>
        new() { IsNotFound = true };

    public static UpdateTaskResult Failure(string error) =>
        new() { Error = error };
}
