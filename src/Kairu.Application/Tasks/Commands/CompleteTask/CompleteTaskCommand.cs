using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Tasks.Commands.CompleteTask;

/// <summary>
/// Command to mark a task as completed.
/// </summary>
public sealed record CompleteTaskCommand(Guid TaskId) : ICommand<CompleteTaskResult>;
