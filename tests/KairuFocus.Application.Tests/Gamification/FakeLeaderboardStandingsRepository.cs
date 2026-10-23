using KairuFocus.Domain.Gamification;

namespace KairuFocus.Application.Tests.Gamification;

/// <summary>Returns the configured standings whatever the window, and records the windows it receives.</summary>
internal sealed class FakeLeaderboardStandingsRepository : ILeaderboardStandingsRepository
{
    public List<XpStanding> Standings { get; } = [];
    public List<(DateTime StartUtc, DateTime EndUtc)> BetweenWindows { get; } = [];
    public int AllTimeCallCount { get; private set; }

    public void Add(string pseudonym, int xp) =>
        Standings.Add(new XpStanding(Pseudonym.Create(pseudonym).Value, xp));

    public Task<IReadOnlyList<XpStanding>> GetStandingsBetweenAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
    {
        BetweenWindows.Add((startUtc, endUtc));
        return Task.FromResult<IReadOnlyList<XpStanding>>(Standings.ToList());
    }

    public Task<IReadOnlyList<XpStanding>> GetAllTimeStandingsAsync(CancellationToken cancellationToken = default)
    {
        AllTimeCallCount++;
        return Task.FromResult<IReadOnlyList<XpStanding>>(Standings.ToList());
    }
}
