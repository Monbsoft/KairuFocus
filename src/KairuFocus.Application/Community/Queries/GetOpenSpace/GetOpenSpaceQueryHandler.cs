using KairuFocus.Application.Common;
using KairuFocus.Application.Community.Common;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetOpenSpace;

public sealed class GetOpenSpaceQueryHandler : IQueryHandler<GetOpenSpaceQuery, GetOpenSpaceResult>
{
    private const int MaxDesks = 10;
    private const int MaxConnectedRead = 200;
    private const int MaxConnectedShown = 12;
    private const int MaxActiveToday = 5;

    private readonly IMemberProfileRepository _profileRepository;
    private readonly IOpenSprintReadRepository _openSprintRepository;
    private readonly IMemberDirectoryReadRepository _directory;
    private readonly IGroupSessionRepository _groupRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly AmbianceCatalogue _catalogue;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public GetOpenSpaceQueryHandler(
        IMemberProfileRepository profileRepository,
        IOpenSprintReadRepository openSprintRepository,
        IMemberDirectoryReadRepository directory,
        IGroupSessionRepository groupRepository,
        IPomodoroSessionRepository sessionRepository,
        AmbianceCatalogue catalogue,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _profileRepository = profileRepository;
        _openSprintRepository = openSprintRepository;
        _directory = directory;
        _groupRepository = groupRepository;
        _sessionRepository = sessionRepository;
        _catalogue = catalogue;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<GetOpenSpaceResult> Handle(GetOpenSpaceQuery query, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        // D9 / D10: decided before any read across users, so that nothing nominative is even read.
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Closed(OpenSpaceAccess.NotMember);
        if (!profile.HasConsentedToPresence)
            return Closed(OpenSpaceAccess.ConsentRequired);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var viewerKey = profile.Name.Key;

        // A read: a host absence not recorded yet is only computed (StateAt), never written here.
        var latestGroup = await _groupRepository.FindLatestForUserAsync(userId, cancellationToken);
        var myGroup = latestGroup is not null && latestGroup.StateAt(now).Phase != GroupPhase.Ended ? latestGroup : null;

        var desks = await ReadDesksAsync(userId, latestGroup, myGroup, now, cancellationToken);

        // The connected members seated at a shown desk are already on it.
        var seatedKeys = desks.SelectMany(d => d.Seated).Select(m => m.Name.Key).ToHashSet(StringComparer.Ordinal);
        var connected = await _directory.GetConnectedMembersAsync(PresenceRules.SeenSince(now), MaxConnectedRead, cancellationToken);
        var notSeated = connected.Where(m => !seatedKeys.Contains(m.Name.Key)).ToList();

        var today = now.Date;
        var activeToday = await _directory.GetActiveMembersAsync(today, today.AddDays(1), MaxActiveToday, cancellationToken);

        return new GetOpenSpaceResult(
            OpenSpaceAccess.Open,
            ViewerInGroup: myGroup is not null,
            desks.Select(d => ToViewModel(d, viewerKey)).ToList(),
            notSeated.Take(MaxConnectedShown).Select(m => Chip(m, viewerKey)).ToList(),
            notSeated.Count,
            new ActiveTodayViewModel(activeToday.Total, activeToday.Members.Select(m => Chip(m, viewerKey)).ToList()));
    }

    /// <summary>
    /// The open desks, preceded by the desks the viewer sits at while they are closed to the others: the list
    /// leaves those out, yet the viewer still sees them (spec §5.3).
    /// </summary>
    private async Task<List<OpenSpaceDesk>> ReadDesksAsync(
        UserId userId,
        GroupSession? latestGroup,
        GroupSession? myGroup,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var desks = (await _openSprintRepository.GetDesksAsync(now, MaxDesks, cancellationToken)).ToList();

        var closedRoots = new List<Guid>();

        // FindLatestForUserAsync only returns a group the viewer hosts or is an active member of.
        if (myGroup is { IsClosedToJoin: true })
            closedRoots.Add(myGroup.Id);

        // A timed sprint of the viewer's own, closed, which is not the root of a group (the group row stands for it).
        var active = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
        if (active is { SessionType: PomodoroSessionType.Sprint, PlannedDurationMinutes: > 0, SharedSprintId: null, IsClosedToJoin: true }
            && active.Id.Value != latestGroup?.Id)
            closedRoots.Add(active.Id.Value);

        var ownClosed = new List<OpenSpaceDesk>();
        foreach (var root in closedRoots.Where(r => desks.All(d => d.RootSessionId != r)))
        {
            // Null when the desk ended between the reads: nothing to show then.
            if (await _openSprintRepository.GetDeskAsync(root, now, cancellationToken) is { } desk)
                ownClosed.Add(desk);
        }

        return [.. ownClosed, .. desks];
    }

    private DeskViewModel ToViewModel(OpenSpaceDesk desk, string viewerKey) =>
        new(
            desk.Seated.Select(m => Chip(m, viewerKey)).ToList(),
            // A datetime2 column comes back with an unspecified kind: all session instants are UTC.
            new DateTimeOffset(DateTime.SpecifyKind(desk.EndsAtUtc, DateTimeKind.Utc)),
            desk.StepType.ToString(),
            desk.Phase.ToString(),
            desk.IsClosedToJoin,
            AmbianceViewModel.Resolve(desk.HostAmbianceCode, _catalogue));

    // The reads know no user: the viewer is recognised by the unique key of the name.
    private static MemberChipViewModel Chip(SprintMember member, string viewerKey) =>
        new(member.Name.Value, member.AvatarUrl, member.Name.Key == viewerKey);

    private static GetOpenSpaceResult Closed(string access) =>
        new(access, ViewerInGroup: false, Desks: [], Connected: [], ConnectedTotal: 0, ActiveToday: null);
}
