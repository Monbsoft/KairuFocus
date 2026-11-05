using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Community.Commands.StartGroupBreak;

/// <summary>UC-CO-10 — starts the break of the group the current member just finished a shared sprint with.</summary>
public sealed record StartGroupBreakCommand : ICommand<StartGroupBreakResult>;
