using KairuFocus.Application.Common;
using KairuFocus.Application.Community.Common;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Pomodoro;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetSprintParticipants;

public sealed class GetSprintParticipantsQueryHandler
    : IQueryHandler<GetSprintParticipantsQuery, GetSprintParticipantsResult>
{
    private static readonly GetSprintParticipantsResult Nobody = new([]);

    private readonly IMemberProfileRepository _profileRepository;
    private readonly IOpenSprintReadRepository _openSprintRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetSprintParticipantsQueryHandler(
        IMemberProfileRepository profileRepository,
        IOpenSprintReadRepository openSprintRepository,
        IPomodoroSessionRepository sessionRepository,
        ICurrentUserService currentUserService)
    {
        _profileRepository = profileRepository;
        _openSprintRepository = openSprintRepository;
        _sessionRepository = sessionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<GetSprintParticipantsResult> Handle(
        GetSprintParticipantsQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        // Leaving the community keeps the sprint running in private: it also stops showing the others.
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Nobody;

        var active = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
        if (active is null)
            return Nobody;

        // Replaced by GET current-session in the next step of #46. Until then: the members of the group of the
        // active session (empty for a sprint nobody joined, which has no group).
        var members = await _openSprintRepository.GetGroupMembersAsync(
            (active.SharedSprintId ?? active.Id).Value,
            cancellationToken);

        // The read knows no user: the current member is recognised by the unique key of the name.
        return new GetSprintParticipantsResult(members
            .Where(m => m.Name.Key != profile.Name.Key)
            .Select(m => new SprintMemberViewModel(m.Name.Value, m.AvatarUrl))
            .ToList());
    }
}
