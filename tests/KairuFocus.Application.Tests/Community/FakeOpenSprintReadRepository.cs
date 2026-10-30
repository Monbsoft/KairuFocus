using KairuFocus.Domain.Community;

namespace KairuFocus.Application.Tests.Community;

/// <summary>
/// In-memory open sprints. A sprint whose end is reached is no longer returned, and joinable sprints are
/// looked up by <see cref="ProfileName.Key"/>. Which sprints are open or closed to join is decided by the
/// seed: this fake holds no session.
/// </summary>
internal sealed class FakeOpenSprintReadRepository : IOpenSprintReadRepository
{
    private readonly List<OpenSprintGroup> _groups = [];
    private readonly Dictionary<string, JoinableSprint> _joinable = [];
    private readonly Dictionary<Guid, IReadOnlyList<SprintMember>> _participants = [];
    private readonly Dictionary<Guid, GroupBreakRules> _groupBreaks = [];

    /// <summary>Thrown by every read when set.</summary>
    public Exception? ThrowOnRead { get; set; }

    /// <summary>The limit received by the last <see cref="GetOpenGroupsAsync"/> call.</summary>
    public int? LastMaxGroups { get; private set; }

    /// <summary>Adds an open group. Returned in seeding order.</summary>
    public void SeedGroup(OpenSprintGroup group) => _groups.Add(group);

    /// <summary>Makes the sprint of <paramref name="targetName"/> joinable.</summary>
    public void SeedJoinable(string targetName, JoinableSprint sprint) =>
        _joinable[ProfileName.Create(targetName).Value.Key] = sprint;

    /// <summary>Makes the group rooted at <paramref name="rootSessionId"/> a group that takes a break.</summary>
    public void SeedGroupBreak(Guid rootSessionId, GroupBreakRules rules) => _groupBreaks[rootSessionId] = rules;

    public void SeedParticipants(Guid rootSessionId, params SprintMember[] participants) =>
        _participants[rootSessionId] = participants;

    public Task<IReadOnlyList<OpenSprintGroup>> GetOpenGroupsAsync(DateTime nowUtc, int maxGroups, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        LastMaxGroups = maxGroups;

        IReadOnlyList<OpenSprintGroup> groups = _groups
            .Where(g => g.EndsAtUtc > nowUtc)
            .Take(maxGroups)
            .ToList();

        return Task.FromResult(groups);
    }

    public Task<JoinableSprint?> FindJoinableAsync(ProfileName targetName, DateTime nowUtc, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        var sprint = _joinable.GetValueOrDefault(targetName.Key);

        return Task.FromResult(sprint is not null && sprint.EndsAtUtc > nowUtc ? sprint : null);
    }

    public Task<IReadOnlyList<SprintMember>> GetParticipantsAsync(Guid rootSessionId, DateTime nowUtc, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        return Task.FromResult(_participants.GetValueOrDefault(rootSessionId, []));
    }

    public Task<GroupBreakRules?> FindGroupBreakAsync(Guid rootSessionId, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        return Task.FromResult(_groupBreaks.GetValueOrDefault(rootSessionId));
    }

    private void ThrowIfReadFails()
    {
        if (ThrowOnRead is not null)
            throw ThrowOnRead;
    }
}
