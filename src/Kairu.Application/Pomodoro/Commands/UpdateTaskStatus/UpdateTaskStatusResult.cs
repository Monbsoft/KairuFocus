using Kairu.Application.Tasks.Common;

namespace Kairu.Application.Pomodoro.Commands.UpdateTaskStatus;

public sealed record UpdateTaskStatusResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public TaskViewModel? Task { get; init; }
    public string? Error { get; init; }

    private UpdateTaskStatusResult() { }

    public static UpdateTaskStatusResult Success(TaskViewModel task) =>
        new() { IsSuccess = true, Task = task };

    public static UpdateTaskStatusResult NotFound() =>
        new() { IsNotFound = true };

    public static UpdateTaskStatusResult Failure(string error) =>
        new() { Error = error };
}
