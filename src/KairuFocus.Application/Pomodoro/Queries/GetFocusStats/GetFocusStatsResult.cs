namespace KairuFocus.Application.Pomodoro.Queries.GetFocusStats;

/// <summary>
/// Activité de focus quotidienne des ~366 derniers jours locaux (jours actifs uniquement,
/// triés par date croissante). Le client comble les jours vides et calcule les agrégats
/// de période (semaine/mois) et la heatmap.
/// </summary>
public sealed record GetFocusStatsResult(IReadOnlyList<FocusDayStat> Days);
