using KairuFocus.Application.Common;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Pomodoro;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetOpenSprints;

public sealed class GetOpenSprintsQueryHandler : IQueryHandler<GetOpenSprintsQuery, GetOpenSprintsResult>
{
    private const int MaxGroups = 10;

    private readonly IOpenSprintReadRepository _openSprintRepository;
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IGroupSessionRepository _groupRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public GetOpenSprintsQueryHandler(
        IOpenSprintReadRepository openSprintRepository,
        IMemberProfileRepository profileRepository,
        IPomodoroSessionRepository sessionRepository,
        IGroupSessionRepository groupRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _openSprintRepository = openSprintRepository;
        _profileRepository = profileRepository;
        _sessionRepository = sessionRepository;
        _groupRepository = groupRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<GetOpenSprintsResult> Handle(
        GetOpenSprintsQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);

        // A read: a host absence not recorded yet is only computed (StateAt), never written here.
        var latestGroup = await _groupRepository.FindLatestForUserAsync(userId, cancellationToken);
        var myGroup = latestGroup is not null && latestGroup.StateAt(now).Phase != GroupPhase.Ended ? latestGroup : null;

        // The group makes the row mine even while it waits for the host, when no session is active.
        Guid? myRootId = myGroup?.Id;
        if (myRootId is null)
        {
            var active = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
            myRootId = active is null ? null : (active.SharedSprintId ?? active.Id).Value;
        }

        var groups = await _openSprintRepository.GetOpenGroupsAsync(now, MaxGroups, cancellationToken);

        return new GetOpenSprintsResult(
            profile is not null,
            myGroup is not null,
            groups.Select(g => new OpenSprintRowViewModel(
                    g.Lead.Name.Value,
                    g.Lead.AvatarUrl,
                    // A datetime2 column comes back with an unspecified kind: all session instants are UTC.
                    new DateTimeOffset(DateTime.SpecifyKind(g.EndsAtUtc, DateTimeKind.Utc)),
                    g.ParticipantCount,
                    g.RootSessionId == myRootId,
                    g.StepType.ToString(),
                    g.Phase.ToString()))
                .ToList());
    }
}
