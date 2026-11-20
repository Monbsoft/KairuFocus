using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Tests.Community;

/// <summary>
/// In-memory group sessions. Holds the aggregates themselves (no snapshot): a handler's change is visible
/// in <see cref="Groups"/> as soon as it is made, whatever <see cref="TryUpdateAsync"/> then answers.
/// <see cref="FindLatestForUserAsync"/> follows the real contract through the public API of the aggregate.
/// </summary>
internal sealed class FakeGroupSessionRepository : IGroupSessionRepository
{
    private bool _addRejected;

    public List<GroupSession> Groups { get; } = [];

    /// <summary>What <see cref="TryAddAsync"/> answers when the id is not stored yet. False stores nothing.</summary>
    public bool TryAddResult { get; set; } = true;

    /// <summary>What <see cref="TryUpdateAsync"/> answers (false: <see cref="GroupSession.Version"/> conflict).</summary>
    public bool TryUpdateResult { get; set; } = true;

    /// <summary>Calls to <see cref="TryUpdateAsync"/>, accepted or not.</summary>
    public int UpdateCallCount { get; private set; }

    /// <summary>
    /// The group the next <see cref="GetByIdAsync"/> reads after a rejected <see cref="TryAddAsync"/>: the
    /// group a concurrent request created. Stored in <see cref="Groups"/> when returned. Read once.
    /// </summary>
    public Func<GroupSession?>? OnReloadAfterRejectedAdd { get; set; }

    /// <summary>Stores a group directly (does not count as a call).</summary>
    public void Seed(GroupSession group) => Groups.Add(group);

    public Task<GroupSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (_addRejected && OnReloadAfterRejectedAdd is not null)
        {
            _addRejected = false;
            var concurrent = OnReloadAfterRejectedAdd();
            if (concurrent is not null && !Groups.Contains(concurrent))
                Groups.Add(concurrent);
            return Task.FromResult(concurrent);
        }

        return Task.FromResult(Groups.FirstOrDefault(g => g.Id == id));
    }

    public Task<GroupSession?> FindLatestForUserAsync(UserId userId, CancellationToken ct = default) =>
        Task.FromResult(Groups
            .Where(g => g.IsHostedBy(userId) || g.IsActiveMember(userId))
            .OrderBy(g => g.EndedAtUtc is null ? 0 : 1)
            .ThenByDescending(g => g.CurrentStep.StartedAtUtc)
            .FirstOrDefault());

    public Task<bool> TryAddAsync(GroupSession group, CancellationToken ct = default)
    {
        if (!TryAddResult || Groups.Any(g => g.Id == group.Id))
        {
            _addRejected = true;
            return Task.FromResult(false);
        }

        Groups.Add(group);
        return Task.FromResult(true);
    }

    public Task<bool> TryUpdateAsync(GroupSession group, CancellationToken ct = default)
    {
        UpdateCallCount++;
        return Task.FromResult(TryUpdateResult);
    }
}
