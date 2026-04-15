using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Tasks.Commands.AddTask;

/// <summary>
/// Command to add a new task to the backlog.
/// </summary>
public sealed record AddTaskCommand(string Title, string? Description, List<string>? Tags = null) : ICommand<AddTaskResult>;

