namespace Kairudev.Application.Tasks.Commands.CompleteTask;

/// <summary>
/// Command to mark a task as completed.
/// </summary>
public sealed record CompleteTaskCommand(Guid TaskId);
