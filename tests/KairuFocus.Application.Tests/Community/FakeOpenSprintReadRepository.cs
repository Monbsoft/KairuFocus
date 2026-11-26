using KairuFocus.Domain.Community;

namespace KairuFocus.Application.Tests.Community;

/// <summary>
/// In-memory open sprints. Joinable sprints are looked up by <see cref="ProfileName.Key"/>; a joinable running
/// step whose end is reached is no longer returned (a seeded <see cref="GroupPhase.WaitingForHost"/> one is).
/// Which sprints are open or closed to join, and which desks are over, is decided by the seed: this fake holds
/// no session.
/// </summary>
internal sealed class FakeOpenSprintReadRepository : IOpenSprintReadRepository
{
    private readonly List<OpenSpaceDesk> _desks = [];
    private readonly Dictionary<Guid, OpenSpaceDesk> _desksByRoot = [];
    private readonly Dictionary<Guid, string?> _hostAmbiances = [];
    private readonly Dictionary<string, JoinableSprint> _joinable = [];
    private readonly Dictionary<Guid, IReadOnlyList<SprintMember>> _members = [];

    /// <summary>Thrown by every read when set.</summary>
    public Exception? ThrowOnRead { get; set; }

    /// <summary>The limit received by the last <see cref="GetDesksAsync"/> call; null when never called.</summary>
    public int? LastMaxDesks { get; private set; }

    /// <summary>The root received by the last <see cref="GetDeskAsync"/> call; null when never called.</summary>
    public Guid? LastDeskRoot { get; private set; }

    /// <summary>Adds a desk to the open space list (<see cref="GetDesksAsync"/>). Returned in seeding order.</summary>
    public void SeedDesk(OpenSpaceDesk desk) => _desks.Add(desk);

    /// <summary>Makes <paramref name="desk"/> readable by its root (<see cref="GetDeskAsync"/>), closed or not.</summary>
    public void SeedDeskByRoot(OpenSpaceDesk desk) => _desksByRoot[desk.RootSessionId] = desk;

    /// <summary>Sets the ambiance code <see cref="GetHostAmbianceCodeAsync"/> returns for <paramref name="rootSessionId"/>.</summary>
    public void SeedHostAmbiance(Guid rootSessionId, string? code) => _hostAmbiances[rootSessionId] = code;

    /// <summary>Makes the sprint of <paramref name="targetName"/> joinable.</summary>
    public void SeedJoinable(string targetName, JoinableSprint sprint) =>
        _joinable[ProfileName.Create(targetName).Value.Key] = sprint;

    /// <summary>The host then the active members of the group <paramref name="rootSessionId"/>, in that order.</summary>
    public void SeedGroupMembers(Guid rootSessionId, params SprintMember[] members) =>
        _members[rootSessionId] = members;

    public Task<JoinableSprint?> FindJoinableAsync(ProfileName targetName, DateTime nowUtc, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        var sprint = _joinable.GetValueOrDefault(targetName.Key);

        return Task.FromResult(sprint is not null && IsVisible(sprint.Phase, sprint.EndsAtUtc, nowUtc) ? sprint : null);
    }

    public Task<IReadOnlyList<OpenSpaceDesk>> GetDesksAsync(DateTime nowUtc, int maxDesks, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        LastMaxDesks = maxDesks;

        IReadOnlyList<OpenSpaceDesk> desks = _desks.Take(maxDesks).ToList();
        return Task.FromResult(desks);
    }

    public Task<OpenSpaceDesk?> GetDeskAsync(Guid rootSessionId, DateTime nowUtc, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        LastDeskRoot = rootSessionId;

        return Task.FromResult(_desksByRoot.GetValueOrDefault(rootSessionId));
    }

    public Task<string?> GetHostAmbianceCodeAsync(Guid rootSessionId, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        return Task.FromResult(_hostAmbiances.GetValueOrDefault(rootSessionId));
    }

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
