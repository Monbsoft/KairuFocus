using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Sprint.Commands.RecordSprint;

public sealed record RecordSprintCommand(
    string? Name,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    string Outcome,
    Guid? LinkedTaskId,
    int SprintNumber) : ICommand<RecordSprintResult>;
