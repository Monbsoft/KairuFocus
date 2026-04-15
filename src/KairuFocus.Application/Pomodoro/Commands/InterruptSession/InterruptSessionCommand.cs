using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Pomodoro.Commands.InterruptSession;

public sealed record InterruptSessionCommand : ICommand<InterruptSessionResult>;
