using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Community;

public interface IMemberPresenceRepository
{
    /// <summary>
    /// Records that <paramref name="userId"/> was seen at <paramref name="seenAtUtc"/>: creates the presence, or
    /// moves it forward. An older instant never moves it back. A member who leaves the community during the signal
    /// (presence or profile deleted meanwhile) loses that signal silently: nothing is recorded, nothing is thrown.
    /// </summary>
    Task RecordSeenAsync(UserId userId, DateTime seenAtUtc, CancellationToken ct = default);
}
