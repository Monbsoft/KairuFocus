using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Tasks.Commands.DeleteTask;

public sealed record DeleteTaskCommand(Guid TaskId) : ICommand<DeleteTaskResult>;
