using KairuFocus.Application.Common;
using KairuFocus.Application.Pomodoro.Commands.InterruptSession;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Community.Commands.LeaveOrStopGroupSession;

/// <summary>
/// The host stops the group for everyone, a participant leaves alone. Only the sessions of the current user
/// are written: the sessions of the others end on their own side when they read that the group is over.
/// </summary>
public sealed class LeaveOrStopGroupSessionCommandHandler
    : ICommandHandler<LeaveOrStopGroupSessionCommand, LeaveOrStopGroupSessionResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IGroupSessionRepository _groupRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LeaveOrStopGroupSessionCommandHandler> _logger;

    public LeaveOrStopGroupSessionCommandHandler(
        IMemberProfileRepository profileRepository,
        IGroupSessionRepository groupRepository,
        IPomodoroSessionRepository sessionRepository,
        IMediator mediator,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<LeaveOrStopGroupSessionCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _groupRepository = groupRepository;
        _sessionRepository = sessionRepository;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<LeaveOrStopGroupSessionResult> Handle(
        LeaveOrStopGroupSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return LeaveOrStopGroupSessionResult.Failure(CommunityErrors.Community.NotMember);

        var group = await FindLiveGroupAsync(userId, cancellationToken);
        if (group is null)
            return LeaveOrStopGroupSessionResult.Failure(CommunityErrors.Community.NotInGroup);

        await InterruptSessionOfGroupAsync(group, userId, cancellationToken);

        var asHost = group.IsHostedBy(userId);
        var reason = command.BecauseLeavingCommunity
            ? GroupSessionEndReason.HostLeftCommunity
            : GroupSessionEndReason.Stopped;

        // A rejected write is a Version conflict (a step launched, the group closed in between): the
        // repository detached the aggregate, so a fresh load gives the new state. Once is enough, a
        // second conflict means the group keeps moving under the request.
        for (var attempt = 0; ; attempt++)
        {
            var now = UtcNow;
            var applied = asHost ? group.Stop(reason, now) : group.Leave(userId, now);
            if (applied.IsFailure)
                return LeaveOrStopGroupSessionResult.Failure(applied.Error);

            if (await _groupRepository.TryUpdateAsync(group, cancellationToken))
            {
                _logger.LogInformation(
                    "User {UserId} {Action} group session {GroupId}",
                    userId, asHost ? "stopped" : "left", group.Id);
                return LeaveOrStopGroupSessionResult.Success(stopped: asHost);
            }

            if (attempt == 1)
            {
                _logger.LogWarning(
                    "Departure of user {UserId} from group session {GroupId} rejected by concurrent writes",
                    userId, group.Id);
                return LeaveOrStopGroupSessionResult.Failure(CommunityErrors.Community.GroupStepRunning);
            }

            group = await FindLiveGroupAsync(userId, cancellationToken);
            if (group is null)
                return LeaveOrStopGroupSessionResult.Failure(CommunityErrors.Community.NotInGroup);
        }
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>The group the user hosts or belongs to, unless it is over. A host absence that is only
    /// reported is recorded first; losing that race changes nothing, the group is over either way.</summary>
    private async Task<GroupSession?> FindLiveGroupAsync(UserId userId, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.FindLatestForUserAsync(userId, cancellationToken);
        if (group is null)
            return null;

        var now = UtcNow;
        if (group.EndIfHostAbsent(now))
            await _groupRepository.TryUpdateAsync(group, cancellationToken);

        return group.StateAt(now).Phase == GroupPhase.Ended ? null : group;
    }

    /// <summary>Interrupts the active session of the user when it belongs to the group (the host's root
    /// session, or a step joined): journal entry, no XP. A session started outside the group is left alone.</summary>
    private async Task InterruptSessionOfGroupAsync(GroupSession group, UserId userId, CancellationToken cancellationToken)
    {
        var active = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
        if (active is null || (active.Id.Value != group.Id && active.SharedSprintId?.Value != group.Id))
            return;

        var interrupted = await _mediator.DispatchAsync<InterruptSessionCommand, InterruptSessionResult>(
            new InterruptSessionCommand(), cancellationToken);
        if (!interrupted.IsSuccess)
            _logger.LogWarning(
                "Session {SessionId} of user {UserId} not interrupted when leaving group {GroupId}: {Error}",
                active.Id.Value, userId, group.Id, interrupted.Error);
    }
}
