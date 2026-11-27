using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Tests.Community;

/// <summary>Records every presence written, in order.</summary>
internal sealed class FakeMemberPresenceRepository : IMemberPresenceRepository
{
    private readonly List<(UserId UserId, DateTime SeenAtUtc)> _recorded = [];

    public IReadOnlyList<(UserId UserId, DateTime SeenAtUtc)> Recorded => _recorded;

    public Task RecordSeenAsync(UserId userId, DateTime seenAtUtc, CancellationToken ct = default)
    {
        _recorded.Add((userId, seenAtUtc));
        return Task.CompletedTask;
    }
}
