using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Pomodoro.Commands.CompleteSession;

/// <param name="ExpectedSessionId">The session the caller means to complete: a tab still counting down a session
/// that another tab already closed must not end the one that came next. Null: whichever session is active.</param>
public sealed record CompleteSessionCommand(Guid? ExpectedSessionId = null) : ICommand<CompleteSessionResult>;
