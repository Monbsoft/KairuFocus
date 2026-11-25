using KairuFocus.Web.Services;

namespace KairuFocus.Web.Helpers;

public static class JournalEntryMeta
{
    private record struct EventConfig(string Variant, string Icon, bool CanAddComment);

    private static readonly IReadOnlyDictionary<string, EventConfig> Configs =
        new Dictionary<string, EventConfig>
        {
            ["SprintStarted"]     = new("sprint", "🍅", true),
            ["SprintCompleted"]   = new("sprint", "✅", false),
            ["SprintInterrupted"] = new("warn",   "⏸️", false),
            ["BreakStarted"]      = new("break",  "☕", false),
            ["BreakCompleted"]    = new("break",  "🌿", false),
            ["BreakInterrupted"]  = new("warn",   "⚡", false),
            ["TaskStarted"]       = new("task",   "🚀", true),
            ["TaskCompleted"]     = new("task",   "🎉", true),
        };

    public static string GetVariant(string eventType) =>
        Configs.TryGetValue(eventType, out var c) ? c.Variant : "sprint";

    public static string GetIcon(string eventType) =>
        Configs.TryGetValue(eventType, out var c) ? c.Icon : "📌";

    public static bool CanAddComment(string eventType) =>
        !Configs.TryGetValue(eventType, out var c) || c.CanAddComment;

    public static (string Key, object[] Args)? GetLabelKey(JournalEntryDto entry)
    {
        if (!Configs.ContainsKey(entry.EventType)) return null;
        var key = $"Journal.Entry.{entry.EventType}";
        var supportsSequence = entry.EventType.StartsWith("Sprint", StringComparison.Ordinal)
                               || entry.EventType.StartsWith("Break", StringComparison.Ordinal);
        return supportsSequence && entry.Sequence.HasValue
            ? (key + ".Seq", new object[] { entry.Sequence.Value })
            : (key, Array.Empty<object>());
    }

    public static string GetLabel(JournalEntryDto entry, CommunityFormatter f) =>
        GetLabelKey(entry) is { } label ? f.Format(label.Key, label.Args) : entry.EventType;
}
