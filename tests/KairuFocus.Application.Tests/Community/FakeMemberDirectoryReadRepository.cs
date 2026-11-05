using KairuFocus.Domain.Community;

namespace KairuFocus.Application.Tests.Community;

/// <summary>
/// In-memory directory of members, looked up by <see cref="ProfileName.Key"/>. A member not seeded is
/// unknown (no profile) and has an empty activity, like a name nobody holds.
/// </summary>
internal sealed class FakeMemberDirectoryReadRepository : IMemberDirectoryReadRepository
{
    private static readonly MemberActivity NoActivity = new(0, TimeSpan.Zero, 0, []);

    private readonly Dictionary<string, PublicMemberProfile> _profiles = [];
    private readonly Dictionary<string, MemberActivity> _activities = [];
    private ActiveMembers _active = new(0, []);

    /// <summary>Thrown by every read when set.</summary>
    public Exception? ThrowOnRead { get; set; }

    /// <summary>The window start received by the last <see cref="GetActivityAsync"/> call.</summary>
    public DateTime? LastHeatmapSinceUtc { get; private set; }

    /// <summary>The window and cap received by the last <see cref="GetActiveMembersAsync"/> call.</summary>
    public (DateTime From, DateTime To, int Max)? LastActiveWindow { get; private set; }

    /// <summary>Sets what <see cref="GetActiveMembersAsync"/> returns.</summary>
    public void SeedActive(ActiveMembers result) => _active = result;

    /// <summary>Adds a member. A later seed with the same key replaces the previous one.</summary>
    public void Seed(PublicMemberProfile profile, MemberActivity? activity = null)
    {
        _profiles[profile.Name.Key] = profile;
        if (activity is not null)
            _activities[profile.Name.Key] = activity;
    }

    public Task<PublicMemberProfile?> FindByNameAsync(ProfileName name, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        return Task.FromResult(_profiles.GetValueOrDefault(name.Key));
    }

    public Task<MemberActivity> GetActivityAsync(ProfileName name, DateTime heatmapSinceUtc, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        LastHeatmapSinceUtc = heatmapSinceUtc;

        return Task.FromResult(_activities.GetValueOrDefault(name.Key, NoActivity));
    }

    public Task<ActiveMembers> GetActiveMembersAsync(DateTime fromUtc, DateTime toUtc, int maxMembers, CancellationToken ct = default)
    {
        ThrowIfReadFails();

        LastActiveWindow = (fromUtc, toUtc, maxMembers);

        return Task.FromResult(_active);
    }

    private void ThrowIfReadFails()
    {
        if (ThrowOnRead is not null)
            throw ThrowOnRead;
    }
}
