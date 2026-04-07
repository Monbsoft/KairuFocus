using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Tasks.Commands.DeleteTask;

public sealed record DeleteTaskCommand(Guid TaskId) : ICommand<DeleteTaskResult>;
