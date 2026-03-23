using Kairudev.Application.Common;
using Kairudev.Application.Journal.Commands.CreateEntry;
using Kairudev.Application.Sprint.Common;
using Kairudev.Domain.Journal;
using Kairudev.Domain.Sprint;
using Kairudev.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Sprint.Commands.RecordSprint;

public sealed class RecordSprintCommandHandler : ICommandHandler<RecordSprintCommand, RecordSprintResult>
{
    private readonly ISprintSessionRepository _sprintRepository;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RecordSprintCommandHandler> _logger;

    public RecordSprintCommandHandler(
        ISprintSessionRepository sprintRepository,
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<RecordSprintCommandHandler> logger)
    {
        _sprintRepository = sprintRepository;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<RecordSprintResult> Handle(
        RecordSprintCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Recording sprint {SprintNumber} for user {UserId}", command.SprintNumber, userId);

        var nameResult = SprintName.Create(command.Name, command.SprintNumber);
        if (nameResult.IsFailure)
            return RecordSprintResult.Failure(nameResult.Error);

        if (!Enum.TryParse<SprintOutcome>(command.Outcome, true, out var outcome))
            return RecordSprintResult.Failure($"Invalid sprint outcome: '{command.Outcome}'. Expected 'Completed' or 'Interrupted'.");

        TaskId? linkedTaskId = command.LinkedTaskId.HasValue
            ? TaskId.From(command.LinkedTaskId.Value)
            : null;

        var sessionResult = SprintSession.Record(
            nameResult.Value,
            userId,
            command.StartedAt,
            command.EndedAt,
            outcome,
            linkedTaskId);

        if (sessionResult.IsFailure)
            return RecordSprintResult.Failure(sessionResult.Error);

        var session = sessionResult.Value;
        await _sprintRepository.AddAsync(session, cancellationToken);
        _logger.LogInformation("Sprint {SprintId} ({Outcome}) recorded for user {UserId}", session.Id.Value, outcome, userId);

        // Create retro-active journal entries
        await _mediator.DispatchAsync<CreateEntryCommand, CreateEntryResult>(
            new CreateEntryCommand(
                JournalEventType.SprintStarted,
                session.Id.Value,
                command.StartedAt.UtcDateTime,
                userId),
            cancellationToken);

        var completionEventType = outcome == SprintOutcome.Completed
            ? JournalEventType.SprintCompleted
            : JournalEventType.SprintInterrupted;

        await _mediator.DispatchAsync<CreateEntryCommand, CreateEntryResult>(
            new CreateEntryCommand(
                completionEventType,
                session.Id.Value,
                command.EndedAt.UtcDateTime,
                userId),
            cancellationToken);

        return RecordSprintResult.Success(SprintSessionViewModel.From(session));
    }
}
