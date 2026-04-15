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

    public static string GetLabel(JournalEntryDto entry) => entry.EventType switch
    {
        "SprintStarted"     => SeqLabel("Sprint", entry.Sequence, "démarré"),
        "SprintCompleted"   => SeqLabel("Sprint", entry.Sequence, "complété"),
        "SprintInterrupted" => SeqLabel("Sprint", entry.Sequence, "interrompu"),
        "BreakStarted"      => SeqLabel("Pause",  entry.Sequence, "démarrée"),
        "BreakCompleted"    => SeqLabel("Pause",  entry.Sequence, "terminée"),
        "BreakInterrupted"  => SeqLabel("Pause",  entry.Sequence, "interrompue"),
        "TaskStarted"       => "Tâche démarrée",
        "TaskCompleted"     => "Tâche complétée",
        _                   => entry.EventType
    };

    private static string SeqLabel(string noun, int? seq, string verb) =>
        seq.HasValue ? $"{noun} #{seq} {verb}" : $"{noun} {verb}";
}
