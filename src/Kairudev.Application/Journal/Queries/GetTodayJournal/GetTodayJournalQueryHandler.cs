using Kairudev.Application.Journal.Common;
using Kairudev.Domain.Journal;

namespace Kairudev.Application.Journal.Queries.GetTodayJournal;

public sealed class GetTodayJournalQueryHandler
{
    private readonly IJournalEntryRepository _repository;

    public GetTodayJournalQueryHandler(IJournalEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetTodayJournalResult> HandleAsync(
        GetTodayJournalQuery query,
        CancellationToken cancellationToken = default)
    {
        var entries = await _repository.GetTodayEntriesAsync(DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
        var viewModels = entries.Select(JournalEntryViewModel.From).ToList();
        return new GetTodayJournalResult(viewModels);
    }
}
