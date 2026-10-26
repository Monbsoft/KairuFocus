namespace KairuFocus.Application.Gamification.Common;

/// <summary>Half-open UTC interval [StartUtc, EndUtc). Comparisons use ticks only (the Kind is ignored).</summary>
internal readonly record struct UtcRange(DateTime StartUtc, DateTime EndUtc)
{
    public bool Contains(DateTime instantUtc) => instantUtc >= StartUtc && instantUtc < EndUtc;
}
