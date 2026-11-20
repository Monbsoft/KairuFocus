using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Pomodoro.Commands.CompleteSession;

/// <param name="ExpectedSessionId">The session the caller means to complete: a tab still counting down a session
/// that another tab already closed must not end the one that came next. Null: whichever session is active.</param>
/// <param name="EndedAtUtc">The instant the session ended, when it is not now: a participant's session closed at
/// the time its host stopped the group. Bounded by the session itself (never before its start, never after its
/// scheduled end). Null: now. Never bound from HTTP: only the server sets it.</param>
public sealed record CompleteSessionCommand(Guid? ExpectedSessionId = null, DateTime? EndedAtUtc = null)
    : ICommand<CompleteSessionResult>;
