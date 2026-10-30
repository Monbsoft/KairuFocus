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
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public GetOpenSprintsQueryHandler(
        IOpenSprintReadRepository openSprintRepository,
        IMemberProfileRepository profileRepository,
        IPomodoroSessionRepository sessionRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _openSprintRepository = openSprintRepository;
        _profileRepository = profileRepository;
        _sessionRepository = sessionRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<GetOpenSprintsResult> Handle(
        GetOpenSprintsQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);

        var active = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
        Guid? myRootId = active is null ? null : (active.SharedSprintId ?? active.Id).Value;

        var groups = await _openSprintRepository.GetOpenGroupsAsync(
            _timeProvider.GetUtcNow().UtcDateTime,
            MaxGroups,
            cancellationToken);

        return new GetOpenSprintsResult(
            profile is not null,
            groups.Select(g => new OpenSprintRowViewModel(
                    g.Lead.Name.Value,
                    g.Lead.AvatarUrl,
                    // A datetime2 column comes back with an unspecified kind: all session instants are UTC.
                    new DateTimeOffset(DateTime.SpecifyKind(g.EndsAtUtc, DateTimeKind.Utc)),
                    g.ParticipantCount,
                    g.RootSessionId == myRootId))
                .ToList());
    }
}
