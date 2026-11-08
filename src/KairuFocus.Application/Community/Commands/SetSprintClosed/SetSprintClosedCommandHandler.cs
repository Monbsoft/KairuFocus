using KairuFocus.Application.Common;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Community.Commands.SetSprintClosed;

public sealed class SetSprintClosedCommandHandler : ICommandHandler<SetSprintClosedCommand, SetSprintClosedResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IGroupSessionRepository _groupRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SetSprintClosedCommandHandler> _logger;

    public SetSprintClosedCommandHandler(
        IMemberProfileRepository profileRepository,
        IPomodoroSessionRepository sessionRepository,
        IGroupSessionRepository groupRepository,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<SetSprintClosedCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _sessionRepository = sessionRepository;
        _groupRepository = groupRepository;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<SetSprintClosedResult> Handle(
        SetSprintClosedCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var session = await _sessionRepository.GetActiveAsync(userId, cancellationToken);

        // Once the group exists, it holds the closed flag: the root session is no longer read for it.
        var groupResult = await TrySetGroupClosedAsync(userId, session, command.Closed, cancellationToken);
        if (groupResult is not null)
            return groupResult;

        // A break or a free sprint has no common end: nobody can join it, so there is nothing to close.
        if (session is not { SessionType: PomodoroSessionType.Sprint, PlannedDurationMinutes: > 0 })
            return SetSprintClosedResult.Failure(CommunityErrors.Community.NoActiveSprint);

        if (session.SharedSprintId is not null)
            return SetSprintClosedResult.Failure(CommunityErrors.Community.NotSprintHost);

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return SetSprintClosedResult.Failure(CommunityErrors.Community.NotSprintHost);

        var change = command.Closed ? session.CloseToJoin() : session.ReopenToJoin();
        if (change.IsFailure)
        {
            // Not reachable after the checks above: the session is an active root. Reported in the terms of
            // this use case rather than with an error the callers of this command do not know.
            _logger.LogWarning("Session {SessionId} refused the change: {Error}", session.Id.Value, change.Error);
            return SetSprintClosedResult.Failure(CommunityErrors.Community.NoActiveSprint);
        }

        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return SetSprintClosedResult.Success();
    }

    /// <summary>
    /// Null when the user is in no live group: the root session path applies. A participant who started a
    /// session of their own outside the group has left it, so the group does not answer for them.
    /// </summary>
    private async Task<SetSprintClosedResult?> TrySetGroupClosedAsync(
        UserId userId,
        PomodoroSession? active,
        bool closed,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            var group = await _groupRepository.FindLatestForUserAsync(userId, cancellationToken);
            if (group is null)
                return null;

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (group.EndIfHostAbsent(now))
                await _groupRepository.TryUpdateAsync(group, cancellationToken);

            if (group.StateAt(now).Phase == GroupPhase.Ended)
                return null;

            if (!group.IsHostedBy(userId))
            {
                var movedOn = active is not null
                    && active.Id.Value != group.Id
                    && active.SharedSprintId?.Value != group.Id;
                return movedOn
                    ? null
                    : SetSprintClosedResult.Failure(CommunityErrors.Community.NotSprintHost);
            }

            var change = closed ? group.Close() : group.Reopen();
            if (change.IsFailure)
                return SetSprintClosedResult.Failure(CommunityErrors.Community.NoActiveSprint);

            if (await _groupRepository.TryUpdateAsync(group, cancellationToken))
                return SetSprintClosedResult.Success();

            // A Version conflict: the host launched a step or stopped the group in another request.
            // The aggregate is detached, a fresh load gives the new state; once is enough.
            if (attempt == 1)
            {
                _logger.LogWarning("Closing group session {GroupId} rejected by concurrent writes", group.Id);
                return SetSprintClosedResult.Failure(CommunityErrors.Community.GroupStepRunning);
            }
        }
    }
}
