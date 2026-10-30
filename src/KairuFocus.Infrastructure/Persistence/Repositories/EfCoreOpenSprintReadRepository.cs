using KairuFocus.Domain.Community;
using KairuFocus.Domain.Pomodoro;
using Microsoft.EntityFrameworkCore;

namespace KairuFocus.Infrastructure.Persistence.Repositories;

/// <summary>
/// CROSS-USERS read (ADR-026): the active timed sprints of community members only (inner join), with
/// their public name and avatar. The Users table is never read, and <c>OwnerId</c> only appears in the
/// join condition: no user identifier leaves the query. <see cref="FindGroupBreakAsync"/> reads the host's
/// identifier to find their settings, and keeps it inside the method: nothing identifying crosses the port.
/// <para>
/// The deadline of a session is computed in memory: no DATEADD / DATEDIFF on a column, so a single
/// definition and identical results under SQL Server and InMemory (ADR-024). Nothing closes an expired
/// session on the server, so such rows are read and dropped here; the unique index bounds them to at most
/// one Active session per member.
/// </para>
/// </summary>
internal sealed class EfCoreOpenSprintReadRepository : IOpenSprintReadRepository
{
    // A group break needs a group: two members at least.
    private const int MinGroupBreakParticipants = 2;

    private readonly KairuFocusDbContext _context;

    public EfCoreOpenSprintReadRepository(KairuFocusDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OpenSprintGroup>> GetOpenGroupsAsync(
        DateTime nowUtc,
        int maxGroups,
        CancellationToken ct = default)
    {
        var rows = await ReadUnexpiredAsync(InOpenGroups(ActiveMemberSprints()), nowUtc, ct);

        return rows
            .GroupBy(r => r.RootSessionId)
            .Select(ToGroup)
            .OrderByDescending(g => g.EndsAtUtc)
            .ThenBy(g => g.Lead.Name.Value, StringComparer.Ordinal)
            .Take(maxGroups)
            .ToList();
    }

    public async Task<JoinableSprint?> FindJoinableAsync(
        ProfileName targetName,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var key = targetName.Key;
        var sprints = InOpenGroups(ActiveMemberSprints()).Where(m => m.Profile.NameKey == key);

        // At most one row: the name key and the active session of an owner are both unique.
        var rows = await ReadUnexpiredAsync(sprints, nowUtc, ct);

        return rows.Select(r => new JoinableSprint(r.RootSessionId, r.EndsAtUtc)).FirstOrDefault();
    }

    public async Task<IReadOnlyList<SprintMember>> GetParticipantsAsync(
        Guid rootSessionId,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var rootId = PomodoroSessionId.From(rootSessionId);

        // Not limited to open groups: those already present stay together after the host closes the sprint.
        var sprints = ActiveMemberSprints()
            .Where(m => m.Session.Id == rootId || m.Session.SharedSprintId == rootId);
        var rows = await ReadUnexpiredAsync(sprints, nowUtc, ct);

        return rows
            .OrderBy(r => r.StartedAt)
            .ThenBy(r => r.Name.Value, StringComparer.Ordinal)
            .Select(r => new SprintMember(r.Name, r.AvatarUrl))
            .ToList();
    }

    public async Task<GroupBreakRules?> FindGroupBreakAsync(Guid rootSessionId, CancellationToken ct = default)
    {
        var rootId = PomodoroSessionId.From(rootSessionId);

        // Counted in SQL, per owner: a member who took part twice counts once. Whatever the status or the
        // deadline of the session, as long as it was not interrupted; breaks of the group are not sprints.
        var participants = await (
                from s in _context.PomodoroSessions.AsNoTracking()
                join p in _context.MemberProfiles.AsNoTracking() on s.OwnerId equals p.Id
                where (s.Id == rootId || s.SharedSprintId == rootId)
                      && s.SessionType == PomodoroSessionType.Sprint
                      && s.Status != PomodoroSessionStatus.Interrupted
                select p.Id)
            .Distinct()
            .CountAsync(ct);
        if (participants < MinGroupBreakParticipants)
            return null;

        // The host's identifier only feeds the settings lookup below: it stays in this method.
        var root = await _context.PomodoroSessions
            .AsNoTracking()
            .Where(s => s.Id == rootId)
            .Select(s => new { s.OwnerId })
            .FirstOrDefaultAsync(ct);
        if (root is null)
            return null;

        // The break lasts as long as the host set it, like the host's own breaks (default without a row).
        var hostKey = root.OwnerId?.Value.ToString();
        var shortBreakMinutes = await _context.PomodoroSettings
            .AsNoTracking()
            .Where(s => s.UserId == hostKey)
            .Select(s => (int?)s.ShortBreakDurationMinutes)
            .FirstOrDefaultAsync(ct);

        return new GroupBreakRules(shortBreakMinutes ?? PomodoroSettings.Default.ShortBreakDurationMinutes);
    }

    private IQueryable<MemberSprint> ActiveMemberSprints() =>
        from s in _context.PomodoroSessions.AsNoTracking()
        join p in _context.MemberProfiles.AsNoTracking() on s.OwnerId equals p.Id
        where s.SessionType == PomodoroSessionType.Sprint
              && s.Status == PomodoroSessionStatus.Active
              && s.PlannedDurationMinutes > 0
              && s.StartedAt.HasValue
        select new MemberSprint { Session = s, Profile = p };

    // Closure is read on the root session, whatever its status: a host who closed then left leaves the group
    // closed. A root that no longer exists matches nothing, so its group counts as open.
    private IQueryable<MemberSprint> InOpenGroups(IQueryable<MemberSprint> sprints) =>
        sprints.Where(m => !_context.PomodoroSessions.Any(r =>
            r.IsClosedToJoin && (r.Id == m.Session.Id || r.Id == m.Session.SharedSprintId)));

    // Streamed: only the rows still running are kept.
    private static async Task<List<SprintRow>> ReadUnexpiredAsync(
        IQueryable<MemberSprint> sprints,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var rows = sprints
            .Select(m => new SprintRow(
                m.Session.Id,
                m.Session.SharedSprintId,
                m.Session.StartedAt!.Value,
                m.Session.PlannedDurationMinutes,
                m.Session.SharedEndAt,
                m.Profile.Name,
                m.Profile.AvatarUrl))
            .AsAsyncEnumerable()
            .WithCancellation(ct);

        var unexpired = new List<SprintRow>();
        await foreach (var row in rows)
        {
            if (row.EndsAtUtc > nowUtc)
                unexpired.Add(row);
        }

        return unexpired;
    }

    // The lead is the host when still there, otherwise the participant who has been there the longest.
    private static OpenSprintGroup ToGroup(IGrouping<Guid, SprintRow> group)
    {
        var lead = group
            .OrderBy(r => r.IsRoot ? 0 : 1)
            .ThenBy(r => r.StartedAt)
            .ThenBy(r => r.Name.Value, StringComparer.Ordinal)
            .First();

        return new OpenSprintGroup(
            group.Key,
            new SprintMember(lead.Name, lead.AvatarUrl),
            lead.EndsAtUtc,
            group.Count());
    }

    /// <summary>Object initializer on purpose: EF Core can compose further filters through its members.</summary>
    private sealed class MemberSprint
    {
        public required PomodoroSession Session { get; init; }
        public required MemberProfile Profile { get; init; }
    }

    private sealed record SprintRow(
        PomodoroSessionId Id,
        PomodoroSessionId? SharedSprintId,
        DateTime StartedAt,
        int PlannedDurationMinutes,
        DateTime? SharedEndAt,
        ProfileName Name,
        string? AvatarUrl)
    {
        public bool IsRoot => SharedSprintId is null;

        public Guid RootSessionId => (SharedSprintId ?? Id).Value;

        /// <summary>Exact end of a joined session, otherwise start + planned duration. UTC, like all session
        /// instants: a datetime2 column comes back with an unspecified kind.</summary>
        public DateTime EndsAtUtc =>
            DateTime.SpecifyKind(SharedEndAt ?? StartedAt.AddMinutes(PlannedDurationMinutes), DateTimeKind.Utc);
    }
}
