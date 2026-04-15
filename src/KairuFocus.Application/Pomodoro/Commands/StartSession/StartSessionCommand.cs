using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Pomodoro.Commands.StartSession;

public sealed record StartSessionCommand(
    string? SessionType,
    bool IsFreeSession = false,
    string? JournalComment = null) : ICommand<StartSessionResult>;
