namespace Kairudev.Application.Tasks.Commands.AddTask;

/// <summary>
/// Command to add a new task to the backlog.
/// </summary>
public sealed record AddTaskCommand(string Title, string? Description);
