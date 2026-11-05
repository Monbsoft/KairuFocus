using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Community.Commands.SetSprintClosed;

/// <summary>UC-CO-09 — closes the sprint of the current member to new arrivals, or reopens it.</summary>
public sealed record SetSprintClosedCommand(bool Closed) : ICommand<SetSprintClosedResult>;
