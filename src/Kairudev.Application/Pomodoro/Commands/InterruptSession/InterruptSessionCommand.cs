using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Pomodoro.Commands.InterruptSession;

public sealed record InterruptSessionCommand : ICommand<InterruptSessionResult>;
