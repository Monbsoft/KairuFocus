using KairuFocus.Application.Common;
using KairuFocus.Application.Community.Common;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Pomodoro;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetCurrentSession;

public sealed class GetCurrentSessionQueryHandler : IQueryHandler<GetCurrentSessionQuery, GetCurrentSessionResult?>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IGroupSessionRepository _groupRepository;
    private readonly IOpenSprintReadRepository _openSprintRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly AmbianceCatalogue _catalogue;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public GetCurrentSessionQueryHandler(
        IMemberProfileRepository profileRepository,
        IGroupSessionRepository groupRepository,
        IOpenSprintReadRepository openSprintRepository,
        IPomodoroSessionRepository sessionRepository,
        AmbianceCatalogue catalogue,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _profileRepository = profileRepository;
        _groupRepository = groupRepository;
        _openSprintRepository = openSprintRepository;
        _sessionRepository = sessionRepository;
        _catalogue = catalogue;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<GetCurrentSessionResult?> Handle(
        GetCurrentSessionQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Leaving the community keeps the sprint running in private: nothing to describe.
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return null;

        var active = await _sessionRepository.GetActiveAsync(userId, cancellationToken);

        var group = await _groupRepository.FindLatestForUserAsync(userId, cancellationToken);
        if (group is not null)
        {
            // Writing the absence when the read notices it is idempotent; a lost race changes nothing here.
            if (group.EndIfHostAbsent(now))
                await _groupRepository.TryUpdateAsync(group, cancellationToken);

            var state = group.StateAt(now);
            var outsideGroup = active is not null
                && active.Id.Value != group.Id
                && active.SharedSprintId?.Value != group.Id;

            if (state.Phase == GroupPhase.Ended && outsideGroup)
            {
                // The group is over and the user moved on: describe what they run now.
            }
            else if (outsideGroup && !group.IsHostedBy(userId))
            {
                // A participant who started another session left the group: record it, then describe that session.
                group.Leave(userId, now);
                await _groupRepository.TryUpdateAsync(group, cancellationToken);
            }
            else
            {
                return await DescribeGroupAsync(group, state, profile.Name.Key, userId, cancellationToken);
            }
        }

        return DescribeSoloOpenSprint(active, profile.AmbianceCode);
    }

    private async Task<GetCurrentSessionResult> DescribeGroupAsync(
        GroupSession group,
        GroupSessionState state,
        string viewerKey,
        Domain.Identity.UserId userId,
        CancellationToken cancellationToken)
    {
        // The read knows no user: the current member is recognised by the unique key of the name.
        var members = await _openSprintRepository.GetGroupMembersAsync(group.Id, cancellationToken);
        var others = members
            .Where(m => m.Name.Key != viewerKey)
            .Select(m => new SprintMemberViewModel(m.Name.Value, m.AvatarUrl))
            .ToList();

        // The ambiance of the group is its host's, whoever reads it.
        var hostAmbianceCode = await _openSprintRepository.GetHostAmbianceCodeAsync(group.Id, cancellationToken);

        return new GetCurrentSessionResult(
            IsGroup: true,
            Role: group.IsHostedBy(userId) ? "Host" : "Participant",
            Phase: state.Phase.ToString(),
            StepNumber: state.Step.Number,
            SprintNumber: group.SprintCount,
            StepType: state.Step.Type.ToString(),
            StepEndsAt: AsUtc(state.Step.EndsAtUtc),
            WaitLimitAt: state.Phase == GroupPhase.WaitingForHost
                ? AsUtc(state.Step.EndsAtUtc + GroupRules.HostWaitLimit)
                : null,
            EndReason: state.EndReason?.ToString(),
            Others: others,
            IsClosedToJoin: group.IsClosedToJoin,
            Ambiance: AmbianceViewModel.Resolve(hostAmbianceCode, _catalogue));
    }

    /// <param name="ownAmbianceCode">The viewer hosts their solo open sprint: the ambiance is their own.</param>
    private GetCurrentSessionResult? DescribeSoloOpenSprint(PomodoroSession? active, string? ownAmbianceCode)
    {
        if (active is not { SessionType: PomodoroSessionType.Sprint, PlannedDurationMinutes: > 0, SharedSprintId: null }
            || active.ScheduledEndAt is not { } endsAt)
            return null;

        return new GetCurrentSessionResult(
            IsGroup: false,
            Role: "Host",
            Phase: GroupPhase.Running.ToString(),
            StepNumber: 1,
            SprintNumber: 1,
            StepType: GroupStepType.Sprint.ToString(),
            StepEndsAt: AsUtc(endsAt),
            WaitLimitAt: null,
            EndReason: null,
            Others: [],
            IsClosedToJoin: active.IsClosedToJoin,
            Ambiance: AmbianceViewModel.Resolve(ownAmbianceCode, _catalogue));
    }

    private static DateTimeOffset AsUtc(DateTime utc) =>
        new(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
}
