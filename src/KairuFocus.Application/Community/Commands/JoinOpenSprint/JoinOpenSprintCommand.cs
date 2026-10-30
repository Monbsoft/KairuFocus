using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Community.Commands.JoinOpenSprint;

/// <summary>UC-CO-07 — joins the open sprint of the member named <paramref name="Name"/>.</summary>
public sealed record JoinOpenSprintCommand(string Name) : ICommand<JoinOpenSprintResult>;
