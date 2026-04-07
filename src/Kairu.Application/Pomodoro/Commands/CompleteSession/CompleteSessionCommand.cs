using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Pomodoro.Commands.CompleteSession;

public sealed record CompleteSessionCommand : ICommand<CompleteSessionResult>;
