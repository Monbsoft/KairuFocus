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
        var entry = JournalEntry.Create(command.EventType, command.ResourceId, command.OccurredAt);
        await _repository.AddAsync(entry, cancellationToken);
        return CreateEntryResult.Success();
    }
}
