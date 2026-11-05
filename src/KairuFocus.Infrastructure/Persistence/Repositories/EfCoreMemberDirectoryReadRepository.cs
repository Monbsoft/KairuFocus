using KairuFocus.Domain.Community;
using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Pomodoro;
using Microsoft.EntityFrameworkCore;

namespace KairuFocus.Infrastructure.Persistence.Repositories;

/// <summary>
/// CROSS-USERS read (ADR-026): the public profile and the aggregated activity of a community member, found
/// by the normalized key of the name, and the members active in a window. The Users table is never read, and neither a user identifier nor a
/// session identifier leaves a query: <c>OwnerId</c> is only the join condition with the member profile,
/// which also keeps non-members out.
/// <para>
/// Per-row rules (4 h cap, UTC day of a sprint) are applied in memory while streaming, as in
/// <see cref="EfCoreCommunityActivityRepository"/>: no DATEADD / DATEDIFF on a column.
/// </para>
/// </summary>
internal sealed class EfCoreMemberDirectoryReadRepository : IMemberDirectoryReadRepository
{
    private readonly KairuFocusDbContext _context;

    public EfCoreMemberDirectoryReadRepository(KairuFocusDbContext context)
    {
        _context = context;
    }

    public async Task<PublicMemberProfile?> FindByNameAsync(ProfileName name, CancellationToken ct = default) =>
        await _context.MemberProfiles
            .AsNoTracking()
            .Where(p => p.NameKey == name.Key)
            .Select(p => new PublicMemberProfile(p.Name, p.NameSource, p.AvatarUrl, p.JoinedAtUtc))
            .FirstOrDefaultAsync(ct);

    public async Task<ActiveMembers> GetActiveMembersAsync(DateTime fromUtc, DateTime toUtc, int maxMembers, CancellationToken ct = default)
    {
        // CROSS-USERS read (ADR-026), same join debt as GetActivityAsync: PomodoroSessions.OwnerId is
        // nvarchar(50) and MemberProfiles.UserId uniqueidentifier, converted implicitly by SQL Server.
        // The join keeps non-members out; no identifier is projected, only the public name, its key (the
        // grouping key), the avatar and the end of each sprint. Grouping and ordering run in memory.
        var sprints = (
                from s in _context.PomodoroSessions.AsNoTracking()
                join p in _context.MemberProfiles.AsNoTracking() on s.OwnerId equals p.Id
                where s.SessionType == PomodoroSessionType.Sprint
                      && s.Status == PomodoroSessionStatus.Completed
                      && s.EndedAt >= fromUtc
                      && s.EndedAt < toUtc
                select new { ProfileName = p.Name, p.NameKey, p.AvatarUrl, EndedAt = s.EndedAt!.Value })
            .AsAsyncEnumerable()
            .WithCancellation(ct);

        var lastEnds = new Dictionary<string, (SprintMember Member, DateTime LastEnd)>(StringComparer.Ordinal);
        await foreach (var sprint in sprints)
        {
            if (!lastEnds.TryGetValue(sprint.NameKey, out var known) || sprint.EndedAt > known.LastEnd)
                lastEnds[sprint.NameKey] = (new SprintMember(sprint.ProfileName, sprint.AvatarUrl), sprint.EndedAt);
        }

        var members = lastEnds.Values
            .OrderByDescending(m => m.LastEnd)
            .ThenBy(m => m.Member.Name.Value, StringComparer.Ordinal)
            .Take(maxMembers)
            .Select(m => m.Member)
            .ToList();

        return new ActiveMembers(lastEnds.Count, members);
    }

    public async Task<MemberActivity> GetActivityAsync(ProfileName name, DateTime heatmapSinceUtc, CancellationToken ct = default)
    {
        // Totals are all-time, only the days are bounded by the window: every completed sprint is read.
        // PomodoroSessions.OwnerId is nvarchar(50) and MemberProfiles.UserId uniqueidentifier: SQL Server
        // converts the former implicitly, and a session without owner (legacy) never matches.
        var sprints = (
                from s in _context.PomodoroSessions.AsNoTracking()
                join p in _context.MemberProfiles.AsNoTracking() on s.OwnerId equals p.Id
                where p.NameKey == name.Key
                      && s.SessionType == PomodoroSessionType.Sprint
                      && s.Status == PomodoroSessionStatus.Completed
                      && s.StartedAt.HasValue
                      && s.EndedAt.HasValue
                select new
                {
                    StartedAt = s.StartedAt!.Value,
                    EndedAt = s.EndedAt!.Value,
                    // Part of a group of at least two sessions: it joined one, or another session joined it.
                    // The status of the other session does not matter, but it must be a sprint: the break of a
                    // group carries the same root without having been a sprint done together.
                    IsTogether = s.SharedSprintId != null
                                 || _context.PomodoroSessions.Any(other =>
                                     other.SharedSprintId == s.Id && other.SessionType == PomodoroSessionType.Sprint),
                })
            .AsAsyncEnumerable()
            .WithCancellation(ct);

        var completed = 0;
        var together = 0;
        var focus = TimeSpan.Zero;
        var sprintsPerDay = new Dictionary<DateOnly, int>();
        await foreach (var sprint in sprints)
        {
            completed++;
            focus += CommunityRules.CountedFocus(sprint.StartedAt, sprint.EndedAt);

            if (sprint.IsTogether)
                together++;

            if (sprint.EndedAt >= heatmapSinceUtc)
            {
                var day = DateOnly.FromDateTime(sprint.EndedAt);
                sprintsPerDay[day] = sprintsPerDay.GetValueOrDefault(day) + 1;
            }
        }

        var days = sprintsPerDay
            .OrderBy(d => d.Key)
            .Select(d => new MemberActivityDay(d.Key, d.Value))
            .ToList();

        return new MemberActivity(completed, focus, together, days);
    }
}
