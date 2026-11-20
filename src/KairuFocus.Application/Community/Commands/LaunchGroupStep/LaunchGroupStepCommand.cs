using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Community.Commands.LaunchGroupStep;

/// <summary>
/// UC-CO-10 — the host launches the next step of their group: <c>Sprint</c>, <c>ShortBreak</c> or
/// <c>LongBreak</c>, at the host's own configured duration.
/// </summary>
public sealed record LaunchGroupStepCommand(string StepType) : ICommand<LaunchGroupStepResult>;
