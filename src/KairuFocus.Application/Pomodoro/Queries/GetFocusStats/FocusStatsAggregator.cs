using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Application.Pomodoro.Queries.GetFocusStats;

/// <summary>
/// Agrégation pure (sans I/O) des sprints complétés en statistiques quotidiennes.
/// Chaque sprint est attribué à la date LOCALE de sa fin (EndedAt + offset) — ADR-020/021.
/// Par jour : nombre de sprints + somme des durées (minutes, arrondie).
/// Seuls les jours ayant au moins un sprint sont émis ; le client comble les jours vides.
/// La durée n'est PAS clampée à la borne de lookback : un sprint à cheval (~25 min) est
/// crédité en entier — effet négligeable à l'échelle d'une rétrospective annuelle.
/// </summary>
internal static class FocusStatsAggregator
{
    public static IReadOnlyList<FocusDayStat> Aggregate(
        IReadOnlyList<CompletedSprintInterval> intervals,
        TimeSpan offset)
    {
        return intervals
            .GroupBy(i => DateOnly.FromDateTime(i.EndedAt + offset))
            .Select(g => new FocusDayStat(
                g.Key,
                g.Count(),
                (int)Math.Round(g.Sum(i => (i.EndedAt - i.StartedAt).TotalMinutes))))
            .OrderBy(d => d.Date)
            .ToList();
    }
}
