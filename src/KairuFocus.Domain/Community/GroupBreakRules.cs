namespace KairuFocus.Domain.Community;

/// <summary>The break that follows a shared sprint: the short break length set by the host of the group.</summary>
public sealed record GroupBreakRules(int ShortBreakMinutes);
