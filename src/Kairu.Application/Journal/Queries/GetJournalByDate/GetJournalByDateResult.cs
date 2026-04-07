using Kairu.Application.Journal.Common;

namespace Kairu.Application.Journal.Queries.GetJournalByDate;

public sealed record GetJournalByDateResult(IReadOnlyList<JournalEntryViewModel> Entries);
