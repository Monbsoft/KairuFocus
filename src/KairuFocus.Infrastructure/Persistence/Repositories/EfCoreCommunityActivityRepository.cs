using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Pomodoro;
using Microsoft.EntityFrameworkCore;

namespace KairuFocus.Infrastructure.Persistence.Repositories;

/// <summary>
/// CROSS-USERS read (ADR-024): anonymous aggregates over the Pomodoro sessions of all members.
/// Only counts, sums and one date leave this class; the Users table is never read.
/// <para>
/// Per-row rules (4 h cap, presence deadline) come from <see cref="CommunityRules"/> and are applied
/// while streaming anonymous projections: no DATEADD / DATEDIFF on a column, so a single definition
/// and identical results under SQL Server and InMemory. <c>OwnerId</c> is never projected: it only
/// excludes sessions without owner and feeds the distinct count of active members.
/// </para>
/// </summary>
internal sealed class EfCoreCommunityActivityRepository : ICommunityActivityRepository
{
    private readonly KairuFocusDbContext _context;

    public EfCoreCommunityActivityRepository(KairuFocusDbContext context)
    {
        _context = context;
    }

    public async Task<int> CountSprintsInProgressAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        // Counts sessions, read as "members in sprint": the two only match because the filtered unique index
        // PomodoroSessionConfiguration.ActiveSessionIndexName allows at most one Active session per non-null owner.
        // Dropping that index would count a member once per active sprint.
        //
        // Plain comparison pre-filter: a sprint is never present for more than 4 h after its start,
        // so StartedAt > now − 4 h is a necessary condition. The exact deadline is CommunityRules'.
        var earliestStart = nowUtc - CommunityRules.MaxCountedSprintDuration;

        var activeSprints = _context.PomodoroSessions
            .AsNoTracking()
            .Where(s => s.OwnerId != null
                        && s.SessionType == PomodoroSessionType.Sprint
                        && s.Status == PomodoroSessionStatus.Active
                        && s.StartedAt.HasValue
                        && s.StartedAt.Value > earliestStart)
            .Select(s => new { StartedAt = s.StartedAt!.Value, s.PlannedDurationMinutes })
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        var inProgress = 0;
        await foreach (var sprint in activeSprints)
        {
            if (CommunityRules.IsSprintInProgress(sprint.StartedAt, sprint.PlannedDurationMinutes, nowUtc))
                inProgress++;
        }

        return inProgress;
    }

    public async Task<int> CountActiveMembersAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default) =>
        await CompletedSprints(startUtc, endUtc)
            .Select(s => s.OwnerId)
            .Distinct()
            .CountAsync(cancellationToken);

    public async Task<CompletedSprintTotals> GetCompletedSprintTotalsAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        // Streamed: no list is materialized, and only the two instants of each sprint are read.
        var intervals = CompletedSprints(startUtc, endUtc)
            .Select(s => new { StartedAt = s.StartedAt!.Value, EndedAt = s.EndedAt!.Value })
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        var sprints = 0;
        var focus = TimeSpan.Zero;
        await foreach (var interval in intervals)
        {
            sprints++;
            focus += CommunityRules.CountedFocus(interval.StartedAt, interval.EndedAt);
        }

        return new CompletedSprintTotals(sprints, focus);
    }

    public async Task<DateTime?> GetFirstXpGainUtcAsync(CancellationToken cancellationToken = default) =>
        // Nullable projection: MIN over an empty set yields NULL in SQL (and null in LINQ).
        await _context.XpGains
            .AsNoTracking()
            .Select(g => (DateTime?)g.EarnedAtUtc)
            .MinAsync(cancellationToken);

    // Completed sprints of all members, standard or free, with EndedAt within [startUtc, endUtc).
    // OwnerId != null: PomodoroSessions.OwnerId is nullable (legacy); a session without owner is not a member.
    private IQueryable<PomodoroSession> CompletedSprints(DateTime startUtc, DateTime endUtc) =>
        _context.PomodoroSessions
            .AsNoTracking()
            .Where(s => s.OwnerId != null
                        && s.SessionType == PomodoroSessionType.Sprint
                        && s.Status == PomodoroSessionStatus.Completed
                        && s.StartedAt.HasValue
                        && s.EndedAt.HasValue
                        && s.EndedAt.Value >= startUtc
                        && s.EndedAt.Value < endUtc);
}
