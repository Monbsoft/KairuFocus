using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Tasks.Commands.DeleteTask;

public sealed record DeleteTaskCommand(Guid TaskId) : ICommand<DeleteTaskResult>;
