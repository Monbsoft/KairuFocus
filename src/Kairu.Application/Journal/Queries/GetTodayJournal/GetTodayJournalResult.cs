using Kairu.Application.Journal.Common;

namespace Kairu.Application.Journal.Queries.GetTodayJournal;

public sealed record GetTodayJournalResult(IReadOnlyList<JournalEntryViewModel> Entries);
