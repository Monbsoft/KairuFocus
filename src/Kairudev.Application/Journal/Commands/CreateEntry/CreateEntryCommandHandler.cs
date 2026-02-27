using Kairudev.Domain.Journal;

namespace Kairudev.Application.Journal.Commands.CreateEntry;

public sealed class CreateEntryCommandHandler
{
    private readonly IJournalEntryRepository _repository;

    public CreateEntryCommandHandler(IJournalEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateEntryResult> HandleAsync(
        CreateEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        int? sequence = null;
        if (command.EventType == JournalEventType.BreakCompleted)
        {
            var today = DateOnly.FromDateTime(command.OccurredAt);
            var count = await _repository.GetTodayCountByTypeAsync(JournalEventType.BreakCompleted, today, cancellationToken);
            sequence = count + 1;
        }

        var entry = JournalEntry.Create(command.EventType, command.ResourceId, command.OccurredAt, sequence);
        await _repository.AddAsync(entry, cancellationToken);
        return CreateEntryResult.Success();
    }
}
