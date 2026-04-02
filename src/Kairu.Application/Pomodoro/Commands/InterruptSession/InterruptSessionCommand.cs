using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Pomodoro.Commands.InterruptSession;

public sealed record InterruptSessionCommand : ICommand<InterruptSessionResult>;
