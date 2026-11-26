namespace KairuFocus.Domain.Community;

/// <summary>
/// Reads across users about open sprints and group sessions. No user identifier enters or leaves this
/// contract: a member is known by <see cref="ProfileName"/> only.
/// </summary>
public interface IOpenSprintReadRepository
{
    /// <summary>
    /// The sprints nobody joined yet and the group sessions open to new members, during a step or while the
    /// group waits for its host. A group whose host wait limit has passed is not returned.
    /// </summary>
    Task<IReadOnlyList<OpenSprintGroup>> GetOpenGroupsAsync(DateTime nowUtc, int maxGroups, CancellationToken ct = default);

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
    /// The desks of the open space: the same sprints and groups as <see cref="GetOpenGroupsAsync"/>, each with
    /// the members seated at it, at most <paramref name="maxDesks"/>.
    /// </summary>
    Task<IReadOnlyList<OpenSpaceDesk>> GetDesksAsync(DateTime nowUtc, int maxDesks, CancellationToken ct = default);

    /// <summary>The desk of the group <paramref name="rootSessionId"/> when it is open, otherwise null.</summary>
    Task<OpenSpaceDesk?> GetDeskAsync(Guid rootSessionId, DateTime nowUtc, CancellationToken ct = default);

    /// <summary>The ambiance code chosen by the host of <paramref name="rootSessionId"/>, null when none or no such group.</summary>
    Task<string?> GetHostAmbianceCodeAsync(Guid rootSessionId, CancellationToken ct = default);
}
