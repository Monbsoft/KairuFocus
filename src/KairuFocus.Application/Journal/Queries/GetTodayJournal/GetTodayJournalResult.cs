using KairuFocus.Application.Journal.Common;

namespace KairuFocus.Application.Journal.Queries.GetTodayJournal;

public sealed record GetTodayJournalResult(IReadOnlyList<JournalEntryViewModel> Entries);
