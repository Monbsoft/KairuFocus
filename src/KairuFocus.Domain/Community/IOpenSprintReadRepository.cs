namespace KairuFocus.Domain.Community;

/// <summary>
/// Reads across users about open sprints and group sessions. No user identifier enters or leaves this
/// contract: a member is known by <see cref="ProfileName"/> only.
/// </summary>
public interface IOpenSprintReadRepository
{
    /// <summary>
    /// The open group in which <paramref name="targetName"/> is host or active member, otherwise the open sprint
    /// of <paramref name="targetName"/> nobody joined yet, otherwise null.
    /// </summary>
    Task<JoinableSprint?> FindJoinableAsync(ProfileName targetName, DateTime nowUtc, CancellationToken ct = default);

    /// <summary>
    /// The host, then the active members by arrival, of the group <paramref name="rootSessionId"/>, whatever its
    /// state. Empty when no group has this id (a sprint nobody joined has none).
    /// </summary>
    Task<IReadOnlyList<SprintMember>> GetGroupMembersAsync(Guid rootSessionId, CancellationToken ct = default);

    /// <summary>
    /// The desks of the open space, each with the members seated at it: the sprints nobody joined yet and the
    /// group sessions open to new members, during a step or while the group waits for its host (a group whose
    /// host wait limit has passed is not returned). One desk per host, preferring a running group, then a running
    /// sprint nobody joined, then a group waiting for its host. Most time left first (a waiting group comes last), at
    /// most <paramref name="maxDesks"/>. Only desks open to joins are returned (closed sprints and closed groups
    /// are excluded), so <see cref="OpenSpaceDesk.IsClosedToJoin"/> is always false in this list; a desk closed
    /// to joins is read individually with <see cref="GetDeskAsync"/>.
    /// </summary>
    Task<IReadOnlyList<OpenSpaceDesk>> GetDesksAsync(DateTime nowUtc, int maxDesks, CancellationToken ct = default);

    /// <summary>
    /// The desk of the group or sprint <paramref name="rootSessionId"/>. Null only when the group or sprint is
    /// over (or unknown); a desk closed to joins is still returned.
    /// </summary>
    Task<OpenSpaceDesk?> GetDeskAsync(Guid rootSessionId, DateTime nowUtc, CancellationToken ct = default);

    /// <summary>
    /// The ambiance code of the host of the desk <paramref name="rootSessionId"/>, whatever its state: for a group
    /// root, the code of its host; for a solo-sprint root (a session that is not a joined step), the code of its
    /// owner. Null when the host chose none, and for a joined step (not a root) or an unknown id.
    /// </summary>
    Task<string?> GetHostAmbianceCodeAsync(Guid rootSessionId, CancellationToken ct = default);
}
