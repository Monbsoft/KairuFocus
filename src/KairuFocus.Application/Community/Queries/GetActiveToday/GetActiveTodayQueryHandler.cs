using KairuFocus.Application.Common;
using KairuFocus.Domain.Community;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetActiveToday;

public sealed class GetActiveTodayQueryHandler : IQueryHandler<GetActiveTodayQuery, GetActiveTodayResult>
{
    private const int MaxMembers = 5;

    private readonly IMemberDirectoryReadRepository _directory;
    private readonly IMemberProfileRepository _profileRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public GetActiveTodayQueryHandler(
        IMemberDirectoryReadRepository directory,
        IMemberProfileRepository profileRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider)
    {
        _directory = directory;
        _profileRepository = profileRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<GetActiveTodayResult> Handle(
        GetActiveTodayQuery query,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByUserIdAsync(_currentUserService.CurrentUserId, cancellationToken);

        var from = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var active = await _directory.GetActiveMembersAsync(from, from.AddDays(1), MaxMembers, cancellationToken);

        var myKey = profile?.Name.Key;

        return new GetActiveTodayResult(
            active.Total,
            active.Members
                .Select(m => new ActiveMemberViewModel(m.Name.Value, m.AvatarUrl, m.Name.Key == myKey))
                .ToList());
    }
}
