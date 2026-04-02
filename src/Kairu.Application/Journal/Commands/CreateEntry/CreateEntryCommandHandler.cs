using Kairu.Domain.Journal;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Journal.Commands.CreateEntry;

public sealed class CreateEntryCommandHandler : ICommandHandler<CreateEntryCommand, CreateEntryResult>
{
    private readonly IJournalEntryRepository _repository;
    private readonly ILogger<CreateEntryCommandHandler> _logger;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public CreateEntryCommandHandler(
        IJournalEntryRepository repository,
        ILogger<CreateEntryCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CreateEntryResult> Handle(
        CreateEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            int? sequence = null;
            var today = DateOnly.FromDateTime(command.OccurredAt);

            if (command.EventType == JournalEventType.BreakCompleted)
            {
                var count = await _repository.GetTodayCountByTypeAsync(JournalEventType.BreakCompleted, today, command.OwnerId, cancellationToken);
                sequence = count + 1;
            }
            else if (command.EventType == JournalEventType.SprintStarted)
            {
                var count = await _repository.GetTodayCountByTypeAsync(JournalEventType.SprintStarted, today, command.OwnerId, cancellationToken);
                sequence = count + 1;
            }
            else if (command.EventType is JournalEventType.SprintCompleted or JournalEventType.SprintInterrupted)
            {
                // Le sprint était déjà démarré : count(SprintStarted today) = numéro du sprint courant
                var count = await _repository.GetTodayCountByTypeAsync(JournalEventType.SprintStarted, today, command.OwnerId, cancellationToken);
                if (count > 0)
                    sequence = count;
            }

            var entry = JournalEntry.Create(command.EventType, command.ResourceId, command.OccurredAt, command.OwnerId, sequence);

            if (!string.IsNullOrWhiteSpace(command.InitialComment))
                entry.AddComment(command.InitialComment.Trim());

            await _repository.AddAsync(entry, cancellationToken);
            _logger.LogInformation("Journal entry {EventType} created for resource {ResourceId} by user {OwnerId}", command.EventType, command.ResourceId, command.OwnerId);
            return CreateEntryResult.Success();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
