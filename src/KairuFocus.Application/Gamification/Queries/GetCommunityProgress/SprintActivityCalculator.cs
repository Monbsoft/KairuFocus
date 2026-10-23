using KairuFocus.Application.Gamification.Common;
using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Pomodoro;

namespace KairuFocus.Application.Gamification.Queries.GetCommunityProgress;

/// <summary>
/// Pure computation (no I/O) of the sprint figures of the community page. A sprint counts when its
/// EndedAt falls within the window, standard or free, without the 5-minute XP floor; its focus is
/// capped by <see cref="CommunityRules.CountedFocus"/>. Same source and rule as FocusStatsAggregator
/// (EndedAt − StartedAt), but capped per sprint and rounded once on the sum: the Statistics page is unchanged.
/// </summary>
internal static class SprintActivityCalculator
{
    public static (int Sprints, TimeSpan Focus) Summarize(IEnumerable<CompletedSprintInterval> intervals, UtcRange window)
    {
        var sprints = 0;
        var focus = TimeSpan.Zero;

        foreach (var interval in intervals)
        {
            if (!window.Contains(interval.EndedAt))
                continue;

            sprints++;
            focus += CommunityRules.CountedFocus(interval.StartedAt, interval.EndedAt);
        }

        return (sprints, focus);
    }

    /// <summary>Single rounding of the summed focus, same rounding as FocusStatsAggregator.</summary>
    public static int ToRoundedMinutes(TimeSpan focus) => (int)Math.Round(focus.TotalMinutes);
}
