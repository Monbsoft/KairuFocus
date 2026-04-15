using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Pomodoro.Commands.LinkTask;

public sealed record LinkTaskCommand(Guid TaskId) : ICommand<LinkTaskResult>;
