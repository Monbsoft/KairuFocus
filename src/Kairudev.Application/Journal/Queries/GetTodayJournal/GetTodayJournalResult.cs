using Kairudev.Application.Journal.Common;

namespace Kairudev.Application.Journal.Queries.GetTodayJournal;

public sealed record GetTodayJournalResult(IReadOnlyList<JournalEntryViewModel> Entries);
