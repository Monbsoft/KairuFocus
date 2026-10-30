using KairuFocus.Application.Common;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Community.Commands.SetSprintClosed;

public sealed class SetSprintClosedCommandHandler : ICommandHandler<SetSprintClosedCommand, SetSprintClosedResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SetSprintClosedCommandHandler> _logger;

    public SetSprintClosedCommandHandler(
        IMemberProfileRepository profileRepository,
        IPomodoroSessionRepository sessionRepository,
        ICurrentUserService currentUserService,
        ILogger<SetSprintClosedCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _sessionRepository = sessionRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<SetSprintClosedResult> Handle(
        SetSprintClosedCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        // A break or a free sprint has no common end: nobody can join it, so there is nothing to close.
        var session = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
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
}
