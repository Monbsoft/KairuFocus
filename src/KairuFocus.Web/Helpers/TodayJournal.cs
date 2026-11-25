using KairuFocus.Web.Services;

namespace KairuFocus.Web.Helpers;

public static class TodayJournal
{
    public const int MaxEntries = 4;

    public static IReadOnlyList<JournalEntryDto> Latest(IEnumerable<JournalEntryDto> entries, int max = MaxEntries) =>
        entries.OrderByDescending(e => e.OccurredAt).Take(max).ToList();

    public static JournalEntryDto? NoteTarget(IEnumerable<JournalEntryDto> entries) =>
        entries.OrderByDescending(e => e.OccurredAt).FirstOrDefault(e => JournalEntryMeta.CanAddComment(e.EventType));
}
