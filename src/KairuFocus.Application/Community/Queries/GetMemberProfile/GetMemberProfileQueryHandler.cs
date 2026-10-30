using KairuFocus.Application.Common;
using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Gamification.Common;
using KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Gamification;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetMemberProfile;

/// <summary>
/// Composes the public profile of a member from the directory, the all-time leaderboard and the open
/// sprints. Standing, activity and open sprint are independent zones: the failure of one is flagged and
/// leaves the others intact. Finding the member and the viewer is not a zone: without them there is no page.
/// Reads are sequential: the repositories share one scoped DbContext.
/// </summary>
public sealed class GetMemberProfileQueryHandler
    : IQueryHandler<GetMemberProfileQuery, GetMemberProfileResult>
{
    private const int HeatmapDays = 371;

    private static readonly GetMemberProfileResult NotFound = new(false, null);

    private readonly IMemberDirectoryReadRepository _directory;
    private readonly IMemberProfileRepository _profileRepository;
    private readonly ILeaderboardStandingsRepository _standingsRepository;
    private readonly IOpenSprintReadRepository _openSprintRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GetMemberProfileQueryHandler> _logger;

    public GetMemberProfileQueryHandler(
        IMemberDirectoryReadRepository directory,
        IMemberProfileRepository profileRepository,
        ILeaderboardStandingsRepository standingsRepository,
        IOpenSprintReadRepository openSprintRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<GetMemberProfileQueryHandler> logger)
    {
        _directory = directory;
        _profileRepository = profileRepository;
        _standingsRepository = standingsRepository;
        _openSprintRepository = openSprintRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<GetMemberProfileResult> Handle(
        GetMemberProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        // A name that cannot exist gets the same answer as a name nobody holds.
        var name = ProfileName.Create(query.Name);
        if (name.IsFailure)
            return NotFound;

        var member = await _directory.FindByNameAsync(name.Value, cancellationToken);
        if (member is null)
            return NotFound;

        var viewer = await _profileRepository.GetByUserIdAsync(_currentUserService.CurrentUserId, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // From here on, member.Name and not the requested name: the URL may differ in case from the stored
        // name, and a ProfileName compares on its exact value.
        var (standing, standingFailed) = await LoadZoneAsync(nameof(MemberProfileViewModel.Standing), () => ReadStandingAsync(member.Name, cancellationToken));
        var (activity, activityFailed) = await LoadZoneAsync(nameof(MemberProfileViewModel.Activity), () => ReadActivityAsync(member.Name, now, cancellationToken));
        var (openSprint, openSprintFailed) = await LoadZoneAsync(nameof(MemberProfileViewModel.OpenSprint), () => ReadOpenSprintAsync(member.Name, now, cancellationToken));

        return new GetMemberProfileResult(true, new MemberProfileViewModel(
            member.Name.Value,
            IsGitHub: member.NameSource == ProfileNameSource.GitHub,
            member.AvatarUrl,
            JoinedMonth: new DateOnly(member.JoinedAtUtc.Year, member.JoinedAtUtc.Month, 1),
            IsMe: viewer is not null && viewer.Name.Key == member.Name.Key,
            ViewerIsMember: viewer is not null,
            standing,
            standingFailed,
            activity,
            activityFailed,
            openSprint,
            openSprintFailed));
    }

    private async Task<MemberStandingViewModel> ReadStandingAsync(ProfileName name, CancellationToken cancellationToken)
    {
        var standings = await _standingsRepository.GetAllTimeStandingsAsync(cancellationToken);

        return LeaderboardRanker.Rank(standings, name, isParticipant: true).Me is { } ranked
            ? new MemberStandingViewModel(ranked.Xp, ranked.Rank)
            : new MemberStandingViewModel(Xp: 0, Rank: null);
    }

    private async Task<MemberActivityViewModel> ReadActivityAsync(ProfileName name, DateTime now, CancellationToken cancellationToken)
    {
        var heatmapSinceUtc = DateTime.SpecifyKind(now.Date.AddDays(-(HeatmapDays - 1)), DateTimeKind.Utc);
        var activity = await _directory.GetActivityAsync(name, heatmapSinceUtc, cancellationToken);

        return new MemberActivityViewModel(
            activity.CompletedSprints,
            SprintActivityCalculator.ToRoundedMinutes(activity.Focus),
            activity.SprintsTogether,
            activity.Days.Select(d => new ActivityDayViewModel(d.DateUtc, d.Sprints)).ToList());
    }

    private async Task<OpenSprintStatusViewModel?> ReadOpenSprintAsync(ProfileName name, DateTime now, CancellationToken cancellationToken)
    {
        var sprint = await _openSprintRepository.FindJoinableAsync(name, now, cancellationToken);
        if (sprint is null)
            return null;

        var participants = await _openSprintRepository.GetParticipantsAsync(sprint.RootSessionId, now, cancellationToken);

        // EF Core reads datetime2 as DateTimeKind.Unspecified: pinned to UTC so that the conversion to
        // DateTimeOffset never interprets it as local time.
        return new OpenSprintStatusViewModel(
            new DateTimeOffset(DateTime.SpecifyKind(sprint.EndsAtUtc, DateTimeKind.Utc)),
            participants
                .Where(p => p.Name.Key != name.Key)
                .Select(p => new SprintMemberViewModel(p.Name.Value, p.AvatarUrl))
                .ToList());
    }

    private async Task<(T? Value, bool Failed)> LoadZoneAsync<T>(string zone, Func<Task<T>> read)
        where T : class?
    {
        try
        {
            return (await read(), false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Other members' names are never logged.
            _logger.LogWarning(ex, "Member profile: zone {Zone} could not be read", zone);
            return (null, true);
        }
    }
}
