using KairuFocus.Application.Journal.Common;

namespace KairuFocus.Application.Journal.Queries.GetJournalByDate;

public sealed record GetJournalByDateResult(IReadOnlyList<JournalEntryViewModel> Entries);
