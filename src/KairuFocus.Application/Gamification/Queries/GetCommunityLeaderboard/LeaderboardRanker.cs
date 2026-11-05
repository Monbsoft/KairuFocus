using KairuFocus.Domain.Community;
using KairuFocus.Domain.Gamification;

namespace KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;

/// <summary>
/// Pure ranking (no I/O). Members with 0 XP are excluded; rows are sorted by XP descending, then by
/// profile name (ordinal); rank = 1 + number of rows with strictly more XP ("standard competition":
/// 10, 8, 8, 5 → 1, 2, 2, 4). Top = the first <see cref="TopCount"/> rows, strictly: ties beyond the
/// 10th row are not included. "My" row is found by profile name, which is unique: no user identifier involved.
/// </summary>
internal static class LeaderboardRanker
{
    public const int TopCount = 10;

    public static GetCommunityLeaderboardResult Rank(
        IReadOnlyList<XpStanding> standings,
        ProfileName? me,
        bool isParticipant)
    {
        var ordered = standings
            .Where(s => s.Xp > 0)
            .OrderByDescending(s => s.Xp)
            .ThenBy(s => s.Name.Value, StringComparer.Ordinal)
            .ToList();

        var ranks = new int[ordered.Count];
        for (var i = 0; i < ordered.Count; i++)
            ranks[i] = i > 0 && ordered[i].Xp == ordered[i - 1].Xp ? ranks[i - 1] : i + 1;

        var top = ordered
            .Take(TopCount)
            .Select((standing, i) => new LeaderboardRowViewModel(
                ranks[i],
                standing.Name.Value,
                standing.AvatarUrl,
                standing.Xp,
                IsMe: standing.Name == me))
            .ToList();

        MyStandingViewModel? myStanding = null;
        var myIndex = me is null ? -1 : ordered.FindIndex(s => s.Name == me);
        if (myIndex >= 0)
        {
            var mine = ordered[myIndex];
            var isTied = ordered.Count(s => s.Xp == mine.Xp) > 1;
            myStanding = new MyStandingViewModel(
                ranks[myIndex], isTied, mine.Xp, mine.Name.Value, mine.AvatarUrl, IsInTop: myIndex < TopCount);
        }

        return new GetCommunityLeaderboardResult(isParticipant, ordered.Count, myStanding, top);
    }
}
