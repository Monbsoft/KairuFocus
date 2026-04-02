using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Pomodoro.Commands.LinkTask;

public sealed record LinkTaskCommand(Guid TaskId) : ICommand<LinkTaskResult>;
