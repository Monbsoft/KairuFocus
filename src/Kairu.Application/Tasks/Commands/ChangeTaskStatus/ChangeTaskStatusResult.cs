using Kairu.Application.Tasks.Common;

namespace Kairu.Application.Tasks.Commands.ChangeTaskStatus;

public sealed record ChangeTaskStatusResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public TaskViewModel? Task { get; init; }
    public string? ValidationError { get; init; }
    public string? ConflictError { get; init; }

    private ChangeTaskStatusResult() { }

    public static ChangeTaskStatusResult Success(TaskViewModel task) =>
        new() { IsSuccess = true, Task = task };

    public static ChangeTaskStatusResult NotFound() =>
        new() { IsNotFound = true };

    public static ChangeTaskStatusResult Validation(string error) =>
        new() { ValidationError = error };

    public static ChangeTaskStatusResult Conflict(string error) =>
        new() { ConflictError = error };
}
