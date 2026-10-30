namespace KairuFocus.Domain.Community;

/// <summary>Reads across users about open sprints. No user identifier enters or leaves this contract.</summary>
public interface IOpenSprintReadRepository
{
    Task<IReadOnlyList<OpenSprintGroup>> GetOpenGroupsAsync(DateTime nowUtc, int maxGroups, CancellationToken ct = default);
    Task<JoinableSprint?> FindJoinableAsync(ProfileName targetName, DateTime nowUtc, CancellationToken ct = default);
    Task<IReadOnlyList<SprintMember>> GetParticipantsAsync(Guid rootSessionId, DateTime nowUtc, CancellationToken ct = default);

    /// <summary>
    /// The break rules of the group rooted at <paramref name="rootSessionId"/> once its sprint is over, or null when
    /// the root is unknown or fewer than two members took part (sessions not interrupted).
    /// </summary>
    Task<GroupBreakRules?> FindGroupBreakAsync(Guid rootSessionId, CancellationToken ct = default);
}
