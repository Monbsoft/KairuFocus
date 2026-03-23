using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Pomodoro.Commands.StartSession;

public sealed record StartSessionCommand(string? SessionType) : ICommand<StartSessionResult>;
