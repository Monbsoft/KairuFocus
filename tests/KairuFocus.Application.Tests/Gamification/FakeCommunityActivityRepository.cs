using KairuFocus.Domain.Gamification;

namespace KairuFocus.Application.Tests.Gamification;

/// <summary>Returns configured anonymous aggregates and records the instants and windows it receives.</summary>
internal sealed class FakeCommunityActivityRepository : ICommunityActivityRepository
{
    public int SprintsInProgress { get; set; }
    public int ActiveMembers { get; set; }
    public CompletedSprintTotals Totals { get; set; } = new(0, TimeSpan.Zero);
    public DateTime? FirstXpGainUtc { get; set; }

    public List<DateTime> InProgressInstants { get; } = [];
    public List<(DateTime StartUtc, DateTime EndUtc)> ActiveMembersWindows { get; } = [];
    public List<(DateTime StartUtc, DateTime EndUtc)> TotalsWindows { get; } = [];
    public int FirstXpGainCallCount { get; private set; }

    public Task<int> CountSprintsInProgressAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        InProgressInstants.Add(nowUtc);
        return Task.FromResult(SprintsInProgress);
    }

    public Task<int> CountActiveMembersAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
    {
        ActiveMembersWindows.Add((startUtc, endUtc));
        return Task.FromResult(ActiveMembers);
    }

    public Task<CompletedSprintTotals> GetCompletedSprintTotalsAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
    {
        TotalsWindows.Add((startUtc, endUtc));
        return Task.FromResult(Totals);
    }

    public Task<DateTime?> GetFirstXpGainUtcAsync(CancellationToken cancellationToken = default)
    {
        FirstXpGainCallCount++;
        return Task.FromResult(FirstXpGainUtc);
    }
}
