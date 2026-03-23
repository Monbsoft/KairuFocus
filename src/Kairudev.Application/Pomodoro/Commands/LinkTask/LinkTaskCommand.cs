using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Pomodoro.Commands.LinkTask;

public sealed record LinkTaskCommand(Guid TaskId) : ICommand<LinkTaskResult>;
