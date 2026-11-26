using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace KairuFocus.Infrastructure.Persistence.Repositories;

/// <summary>
/// CROSS-USERS read (ADR-026) of what a member can join: the sprints nobody joined yet, read from
/// <c>PomodoroSessions</c>, and the group sessions, read from <c>GroupSessions</c> and <c>GroupSessionMembers</c>.
/// Community members only (inner joins with <c>MemberProfiles</c>), never the Users table. <c>OwnerId</c>,
/// <c>HostId</c> and the members' <c>UserId</c> only appear in join and filter conditions: no user identifier
/// leaves a query.
/// <para>
/// Deadlines are computed in memory: no DATEADD / DATEDIFF on a column, so a single definition and identical
/// results under SQL Server and InMemory (ADR-024). The phase of a group comes from
/// <see cref="GroupRules.PhaseAt"/>: a host absence is computed, not necessarily recorded yet. SQL only
/// pre-filters with plain comparisons to a parameter.
/// </para>
/// </summary>
internal sealed class EfCoreOpenSprintReadRepository : IOpenSprintReadRepository
{
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
        var soloSprints = await ReadSoloSprintsAsync(SoloSprints(), nowUtc, ct);
        var groups = await ReadLiveAsync(OpenGroups(nowUtc), nowUtc, ct);
        var memberCounts = await CountActiveMembersAsync(OpenGroups(nowUtc), ct);

        var candidates = soloSprints.Select(Candidate.Solo).Concat(groups.Select(Candidate.Group));

        // One card per lead (see Preferred), then most time left first: a waiting group comes last.
        return candidates
            .GroupBy(c => c.Lead.Name.Key)
            .Select(byLead => Preferred(byLead)!)
            .Select(c => new OpenSprintGroup(
                c.RootSessionId,
                c.Lead,
                c.EndsAtUtc,
                ParticipantCount: c.IsGroup ? 1 + memberCounts.GetValueOrDefault(c.RootSessionId) : 1,
                c.StepType,
                c.Phase))
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

        var ofTarget = OpenGroups(nowUtc).Where(m =>
            m.Host.NameKey == key
            || EF.Property<IEnumerable<GroupMember>>(m.Group, GroupSessionConfiguration.MembersField).Any(member =>
                member.LeftAtUtc == null
                && _context.MemberProfiles.Any(p =>
                    p.NameKey == key && p.Id == EF.Property<UserId>(member, GroupMemberConfiguration.UserField))));

        // The lead is the target here: the target's groups (as host or active member) and its sprint nobody
        // joined compete under the same rule as the cards, so that the card and the join agree.
        var candidates = (await ReadLiveAsync(ofTarget, nowUtc, ct))
            .Select(Candidate.Group)
            .ToList();

        // A running group cannot be beaten: the solo sprint is only read when it could win.
        if (!candidates.Any(c => c.IsGroup && c.Phase == GroupPhase.Running))
        {
            // At most one row: the name key and the active session of an owner are both unique.
            candidates.AddRange((await ReadSoloSprintsAsync(SoloSprints().Where(m => m.Profile.NameKey == key), nowUtc, ct))
                .Select(Candidate.Solo));
        }

        return Preferred(candidates) is { } preferred
            ? new JoinableSprint(preferred.RootSessionId, preferred.EndsAtUtc, preferred.StepType, preferred.Phase)
            : null;
    }

    public Task<IReadOnlyList<OpenSpaceDesk>> GetDesksAsync(DateTime nowUtc, int maxDesks, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<OpenSpaceDesk?> GetDeskAsync(Guid rootSessionId, DateTime nowUtc, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<string?> GetHostAmbianceCodeAsync(Guid rootSessionId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public async Task<IReadOnlyList<SprintMember>> GetGroupMembersAsync(Guid rootSessionId, CancellationToken ct = default)
    {
        // Whatever the state of the group: those already present stay visible to each other after a closure,
        // and during the wait for the host.
        var host = await (
                from g in _context.GroupSessions.AsNoTracking()
                join p in _context.MemberProfiles.AsNoTracking()
                    on EF.Property<UserId>(g, GroupSessionConfiguration.HostField) equals p.Id
                where g.Id == rootSessionId
                select new { p.Name, p.AvatarUrl })
            .FirstOrDefaultAsync(ct);

        var members = await (
                from m in _context.Set<GroupMember>().AsNoTracking()
                join p in _context.MemberProfiles.AsNoTracking()
                    on EF.Property<UserId>(m, GroupMemberConfiguration.UserField) equals p.Id
                where EF.Property<Guid>(m, GroupMemberConfiguration.GroupField) == rootSessionId
                      && m.LeftAtUtc == null
                select new { p.Name, p.AvatarUrl, m.JoinedAtUtc })
            .ToListAsync(ct);

        var result = new List<SprintMember>(members.Count + 1);
        if (host is not null)
            result.Add(new SprintMember(host.Name, host.AvatarUrl));

        result.AddRange(members
            .OrderBy(m => m.JoinedAtUtc)
            .ThenBy(m => m.Name.Value, StringComparer.Ordinal)
            .Select(m => new SprintMember(m.Name, m.AvatarUrl)));

        return result;
    }

    // A timed sprint not joined itself and not closed. It may still be the root of a group:
    // ReadSoloSprintsAsync drops those.
    private IQueryable<MemberSprint> SoloSprints() =>
        from s in _context.PomodoroSessions.AsNoTracking()
        join p in _context.MemberProfiles.AsNoTracking() on s.OwnerId equals p.Id
        where s.SessionType == PomodoroSessionType.Sprint
              && s.Status == PomodoroSessionStatus.Active
              && s.PlannedDurationMinutes > 0
              && s.StartedAt.HasValue
              && s.SharedSprintId == null
              && !s.IsClosedToJoin
        select new MemberSprint { Session = s, Profile = p };

    /// <summary>
    /// The unexpired sprints nobody joined: a sprint whose id is a <c>GroupSessions</c> row is the root of a
    /// group, which the group row stands for whatever its state (closed included). Two queries: a session id
    /// (<see cref="PomodoroSessionId"/>, converted) cannot be compared in SQL with a group id (<see cref="Guid"/>),
    /// so the group ids are looked up among the few sprints read.
    /// </summary>
    private async Task<List<SprintRow>> ReadSoloSprintsAsync(
        IQueryable<MemberSprint> sprints,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var unexpired = await ReadUnexpiredAsync(sprints, nowUtc, ct);
        if (unexpired.Count == 0)
            return unexpired;

        var sprintIds = unexpired.Select(s => s.Id.Value).ToList();
        var groupRoots = await _context.GroupSessions
            .AsNoTracking()
            .Where(g => sprintIds.Contains(g.Id))
            .Select(g => g.Id)
            .ToListAsync(ct);

        return groupRoots.Count == 0
            ? unexpired
            : unexpired.Where(s => !groupRoots.Contains(s.Id.Value)).ToList();
    }

    // Groups open to new members, led by their host (a member of the community). The end of the current step
    // pre-filters the groups whose host wait limit has passed: end <= now − limit is exactly the Ended phase.
    private IQueryable<GroupWithHost> OpenGroups(DateTime nowUtc)
    {
        var earliestStepEnd = nowUtc - GroupRules.HostWaitLimit;

        return from g in _context.GroupSessions.AsNoTracking()
               join h in _context.MemberProfiles.AsNoTracking()
                   on EF.Property<UserId>(g, GroupSessionConfiguration.HostField) equals h.Id
               where g.EndedAtUtc == null
                     && !g.IsClosedToJoin
                     && g.CurrentStep.EndsAtUtc > earliestStepEnd
               select new GroupWithHost { Group = g, Host = h };
    }

    // Streamed: only the rows still running are kept.
    private static async Task<List<SprintRow>> ReadUnexpiredAsync(
        IQueryable<MemberSprint> sprints,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var rows = sprints
            .Select(m => new SprintRow(
                m.Session.Id,
                m.Session.StartedAt!.Value,
                m.Session.PlannedDurationMinutes,
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

    // The phase is decided here, by the domain rule, never in SQL: an Ended group is dropped.
    private static async Task<List<GroupRow>> ReadLiveAsync(
        IQueryable<GroupWithHost> groups,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var rows = groups
            .Select(m => new
            {
                m.Group.Id,
                m.Group.CurrentStep.Type,
                m.Group.CurrentStep.EndsAtUtc,
                m.Host.Name,
                m.Host.AvatarUrl,
            })
            .AsAsyncEnumerable()
            .WithCancellation(ct);

        var live = new List<GroupRow>();
        await foreach (var row in rows)
        {
            // UTC, like all step instants: a datetime2 column comes back with an unspecified kind.
            var endsAtUtc = DateTime.SpecifyKind(row.EndsAtUtc, DateTimeKind.Utc);
            var phase = GroupRules.PhaseAt(endsAtUtc, endedAtUtc: null, nowUtc);
            if (phase != GroupPhase.Ended)
                live.Add(new GroupRow(row.Id, row.Type, endsAtUtc, phase, row.Name, row.AvatarUrl));
        }

        return live;
    }

    // Active members of the given groups who are still community members (rows of users without a profile
    // stay until their group is deleted). Counted in SQL: only group ids and counts come out.
    private async Task<Dictionary<Guid, int>> CountActiveMembersAsync(IQueryable<GroupWithHost> groups, CancellationToken ct)
    {
        var groupIds = groups.Select(m => m.Group.Id);

        var counts = await (
                from m in _context.Set<GroupMember>().AsNoTracking()
                where m.LeftAtUtc == null
                      && groupIds.Contains(EF.Property<Guid>(m, GroupMemberConfiguration.GroupField))
                      && _context.MemberProfiles.Any(p => p.Id == EF.Property<UserId>(m, GroupMemberConfiguration.UserField))
                group m by EF.Property<Guid>(m, GroupMemberConfiguration.GroupField) into byGroup
                select new { GroupId = byGroup.Key, Count = byGroup.Count() })
            .ToListAsync(ct);

        return counts.ToDictionary(c => c.GroupId, c => c.Count);
    }

    /// <summary>
    /// One row per lead. A member may lead several rows at once (a group that waits for its host while the host
    /// runs a sprint of its own, a member of a waiting group who starts a sprint alone, a step of a group next
    /// to another active sprint): what can be joined now comes first. Preference: a running group (it carries
    /// members), then a running sprint nobody joined, then a group waiting for its host; the latest end breaks a
    /// tie. Shared by the cards and the join, so that both always point at the same row.
    /// </summary>
    private static Candidate? Preferred(IEnumerable<Candidate> ofOneLead) =>
        ofOneLead
            .OrderBy(c => c switch
            {
                { IsGroup: true, Phase: GroupPhase.Running } => 0,
                { IsGroup: false } => 1,
                _ => 2,
            })
            .ThenByDescending(c => c.EndsAtUtc)
            .FirstOrDefault();

    /// <summary>A row competing for its lead: a group (any live phase) or a running sprint nobody joined.</summary>
    private sealed record Candidate(
        Guid RootSessionId,
        bool IsGroup,
        GroupStepType StepType,
        GroupPhase Phase,
        DateTime EndsAtUtc,
        SprintMember Lead)
    {
        public static Candidate Solo(SprintRow sprint) =>
            new(sprint.Id.Value, IsGroup: false, GroupStepType.Sprint, GroupPhase.Running, sprint.EndsAtUtc,
                new SprintMember(sprint.Name, sprint.AvatarUrl));

        public static Candidate Group(GroupRow group) =>
            new(group.Id, IsGroup: true, group.StepType, group.Phase, group.EndsAtUtc,
                new SprintMember(group.HostName, group.HostAvatarUrl));
    }

    /// <summary>Object initializer on purpose: EF Core can compose further filters through its members.</summary>
    private sealed class MemberSprint
    {
        public required PomodoroSession Session { get; init; }
        public required MemberProfile Profile { get; init; }
    }

    /// <summary>Object initializer on purpose: EF Core can compose further filters through its members.</summary>
    private sealed class GroupWithHost
    {
        public required GroupSession Group { get; init; }
        public required MemberProfile Host { get; init; }
    }

    private sealed record SprintRow(
        PomodoroSessionId Id,
        DateTime StartedAt,
        int PlannedDurationMinutes,
        ProfileName Name,
        string? AvatarUrl)
    {
        /// <summary>Start + planned duration. UTC, like all session instants: a datetime2 column comes back
        /// with an unspecified kind.</summary>
        public DateTime EndsAtUtc =>
            DateTime.SpecifyKind(StartedAt.AddMinutes(PlannedDurationMinutes), DateTimeKind.Utc);
    }

    private sealed record GroupRow(
        Guid Id,
        GroupStepType StepType,
        DateTime EndsAtUtc,
        GroupPhase Phase,
        ProfileName HostName,
        string? HostAvatarUrl);
}
