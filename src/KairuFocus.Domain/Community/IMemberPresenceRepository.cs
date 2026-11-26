using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Community;

public interface IMemberPresenceRepository
{
    /// <summary>
    /// Records that <paramref name="userId"/> was seen at <paramref name="seenAtUtc"/>: creates the presence, or
    /// moves it forward. An older instant never moves it back.
    /// </summary>
    Task RecordSeenAsync(UserId userId, DateTime seenAtUtc, CancellationToken ct = default);
}
