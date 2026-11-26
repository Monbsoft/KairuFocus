using KairuFocus.Domain.Community;

namespace KairuFocus.Application.Tests.Community;

/// <summary>
/// In-memory open sprints. A running step whose end is reached is no longer returned (a seeded
/// <see cref="GroupPhase.WaitingForHost"/> row is), and joinable sprints are looked up by
/// <see cref="ProfileName.Key"/>. Which sprints are open or closed to join is decided by the seed: this
/// fake holds no session.
/// </summary>
internal sealed class FakeOpenSprintReadRepository : IOpenSprintReadRepository
{
    private readonly List<OpenSprintGroup> _groups = [];
    private readonly Dictionary<string, JoinableSprint> _joinable = [];
    private readonly Dictionary<Guid, IReadOnlyList<SprintMember>> _members = [];

    /// <summary>Thrown by every read when set.</summary>
    public Exception? ThrowOnRead { get; set; }

    /// <summary>The limit received by the last <see cref="GetOpenGroupsAsync"/> call.</summary>
    public int? LastMaxGroups { get; private set; }

    /// <summary>Adds an open group. Returned in seeding order.</summary>
    public void SeedGroup(OpenSprintGroup group) => _groups.Add(group);

    /// <summary>Makes the sprint of <paramref name="targetName"/> joinable.</summary>
    public void SeedJoinable(string targetName, JoinableSprint sprint) =>
        _joinable[ProfileName.Create(targetName).Value.Key] = sprint;

    /// <summary>The host then the active members of the group <paramref name="rootSessionId"/>, in that order.</summary>
    public void SeedGroupMembers(Guid rootSessionId, params SprintMember[] members) =>
        _members[rootSessionId] = members;

    public Task<IReadOnlyList<OpenSprintGroup>> GetOpenGroupsAsync(DateTime nowUtc, int maxGroups, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        LastMaxGroups = maxGroups;

        IReadOnlyList<OpenSprintGroup> groups = _groups
            .Where(g => IsVisible(g.Phase, g.EndsAtUtc, nowUtc))
            .Take(maxGroups)
            .ToList();

        return Task.FromResult(groups);
    }

    public Task<JoinableSprint?> FindJoinableAsync(ProfileName targetName, DateTime nowUtc, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        var sprint = _joinable.GetValueOrDefault(targetName.Key);

        return Task.FromResult(sprint is not null && IsVisible(sprint.Phase, sprint.EndsAtUtc, nowUtc) ? sprint : null);
    }

    public Task<IReadOnlyList<OpenSpaceDesk>> GetDesksAsync(DateTime nowUtc, int maxDesks, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<OpenSpaceDesk?> GetDeskAsync(Guid rootSessionId, DateTime nowUtc, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<string?> GetHostAmbianceCodeAsync(Guid rootSessionId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<SprintMember>> GetGroupMembersAsync(Guid rootSessionId, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        return Task.FromResult(_members.GetValueOrDefault(rootSessionId, []));
    }

    private static bool IsVisible(GroupPhase phase, DateTime endsAtUtc, DateTime nowUtc) =>
        phase switch
        {
            GroupPhase.Running => endsAtUtc > nowUtc,
            GroupPhase.WaitingForHost => true,
            _ => false,
        };

    private void ThrowIfReadFails()
    {
        if (ThrowOnRead is not null)
            throw ThrowOnRead;
    }
}
