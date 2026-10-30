namespace KairuFocus.Domain.Community;

/// <summary>Reads across users about open sprints. No user identifier enters or leaves this contract.</summary>
public interface IOpenSprintReadRepository
{
    Task<IReadOnlyList<OpenSprintGroup>> GetOpenGroupsAsync(DateTime nowUtc, int maxGroups, CancellationToken ct = default);
    Task<JoinableSprint?> FindJoinableAsync(ProfileName targetName, DateTime nowUtc, CancellationToken ct = default);
    Task<IReadOnlyList<SprintMember>> GetParticipantsAsync(Guid rootSessionId, DateTime nowUtc, CancellationToken ct = default);
}
