using KairuFocus.Application.Tasks.Common;

namespace KairuFocus.Application.Tasks.Commands.AddTask;

/// <summary>
/// Result of the AddTask command.
/// </summary>
public sealed record AddTaskResult
{
    public bool IsSuccess { get; init; }
    public TaskViewModel? Task { get; init; }
    public string? Error { get; init; }

    private AddTaskResult() { }

    public static AddTaskResult Success(TaskViewModel task) =>
        new() { IsSuccess = true, Task = task };

    public static AddTaskResult Failure(string error) =>
        new() { IsSuccess = false, Error = error };
}
