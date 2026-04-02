using Kairu.Application.Tasks.Common;

namespace Kairu.Application.Pomodoro.Commands.CreateTaskDuringSession;

public sealed record CreateTaskDuringSessionResult
{
    public bool IsSuccess { get; init; }
    public TaskViewModel? Task { get; init; }
    public string? Error { get; init; }

    private CreateTaskDuringSessionResult() { }

    public static CreateTaskDuringSessionResult Success(TaskViewModel task) =>
        new() { IsSuccess = true, Task = task };

    public static CreateTaskDuringSessionResult Failure(string error) =>
        new() { Error = error };
}
