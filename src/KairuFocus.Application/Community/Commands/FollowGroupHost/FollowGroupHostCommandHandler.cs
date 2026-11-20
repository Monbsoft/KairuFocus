using KairuFocus.Application.Common;
using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Journal.Commands.CreateEntry;
using KairuFocus.Application.Pomodoro.Commands.CompleteSession;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Application.Community.Commands.FollowGroupHost;

/// <summary>
/// A participant follows the host: enters the step the host launched (same end for everyone), waits for the
/// next one, or closes their own session of the group at the instant the group ended. Only the sessions of
/// the current user are written (ADR-025): closing goes through <see cref="CompleteSessionCommand"/>, so the
/// journal and the XP follow the usual path.
/// </summary>
public sealed class FollowGroupHostCommandHandler : ICommandHandler<FollowGroupHostCommand, FollowGroupHostResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IGroupSessionRepository _groupRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FollowGroupHostCommandHandler> _logger;

    public FollowGroupHostCommandHandler(
        IMemberProfileRepository profileRepository,
        IGroupSessionRepository groupRepository,
        IPomodoroSessionRepository sessionRepository,
        IMediator mediator,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<FollowGroupHostCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _groupRepository = groupRepository;
        _sessionRepository = sessionRepository;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<FollowGroupHostResult> Handle(
        FollowGroupHostCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return FollowGroupHostResult.Failure(CommunityErrors.Community.NotMember);

        // The host leads, never follows.
        var group = await _groupRepository.FindLatestForUserAsync(userId, cancellationToken);
        if (group is null || group.IsHostedBy(userId))
            return FollowGroupHostResult.NotInGroup();

        // Writing the absence when the read notices it is idempotent; a lost race changes nothing here.
        var now = UtcNow;
        if (group.EndIfHostAbsent(now))
            await _groupRepository.TryUpdateAsync(group, cancellationToken);

        var active = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
        if (active is not null && !BelongsTo(active, group))
            return await RecordDepartureAsync(group, userId, now, cancellationToken);

        var state = group.StateAt(now);
        switch (state.Phase)
        {
            case GroupPhase.Ended:
                // Closed at the instant the group ended, bounded by the session's own deadline (UC-CO-14 A3).
                var xpAwarded = active is null ? 0 : await CompleteAsync(active, state.EndedAtUtc, cancellationToken);
                return FollowGroupHostResult.Ended(state.EndReason?.ToString(), xpAwarded);

            case GroupPhase.WaitingForHost:
                return FollowGroupHostResult.Waiting(
                    state.Step.Type.ToString(),
                    AsUtcOffset(state.Step.EndsAtUtc),
                    AsUtcOffset(GroupRules.HostAbsentAt(state.Step.EndsAtUtc)));

            default:
                return await EnterStepAsync(group, state.Step, active, userId, now, cancellationToken);
        }
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>The host's root session, or a step joined in the group.</summary>
    private static bool BelongsTo(PomodoroSession session, GroupSession group) =>
        session.Id.Value == group.Id || session.SharedSprintId?.Value == group.Id;

    private static bool IsSessionOf(PomodoroSession session, GroupSession group, GroupStep step) =>
        BelongsTo(session, group) && session.SharedEndAt == step.EndsAtUtc;

    /// <summary>The participant started another session on their own (UC-CO-14 A5): they left the group.</summary>
    private async Task<FollowGroupHostResult> RecordDepartureAsync(
        GroupSession group,
        UserId userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // The group was found as one the user is an active member of: Leave cannot be refused here.
        group.Leave(userId, now);
        if (await _groupRepository.TryUpdateAsync(group, cancellationToken))
            _logger.LogInformation(
                "User {UserId} left group session {GroupId}: another session is running", userId, group.Id);
        else
            _logger.LogWarning(
                "Departure of user {UserId} from group session {GroupId} rejected by a concurrent write",
                userId, group.Id);

        return FollowGroupHostResult.NotInGroup();
    }

    /// <summary>Puts the participant on the running step: nothing to do when they already are on it, otherwise
    /// the session of an earlier step is closed first, then the session of this step is created and started.</summary>
    private async Task<FollowGroupHostResult> EnterStepAsync(
        GroupSession group,
        GroupStep step,
        PomodoroSession? active,
        UserId userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var stepType = step.Type.ToString();
        var stepEndsAt = AsUtcOffset(step.EndsAtUtc);

        if (active is not null && IsSessionOf(active, group, step))
            return FollowGroupHostResult.SameStep(stepType, stepEndsAt);

        // A session of an earlier step still open (hidden tab, failed completion): it ended at its deadline.
        var xpAwarded = active is null ? 0 : await CompleteAsync(active, now, cancellationToken);

        var sessionType = step.Type.ToSessionType();
        var created = PomodoroSession.CreateJoinedStep(
            sessionType, userId, PomodoroSessionId.From(group.Id), step.EndsAtUtc, now);
        if (created.IsFailure || created.Value.Start(now).IsFailure)
        {
            // Unreachable while the step runs at "now"; kept as the answer of a step over at this instant (E2).
            return FollowGroupHostResult.Waiting(
                stepType, stepEndsAt, AsUtcOffset(GroupRules.HostAbsentAt(step.EndsAtUtc)));
        }

        var session = created.Value;

        // The read above is a check-then-act: the "one active session per owner" index is the real guard.
        if (!await _sessionRepository.TryAddAsync(session, cancellationToken))
        {
            var concurrent = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
            if (concurrent is not null && IsSessionOf(concurrent, group, step))
                return FollowGroupHostResult.SameStep(stepType, stepEndsAt);

            _logger.LogWarning(
                "Session of user {UserId} for step {StepNumber} of group session {GroupId} rejected: another session is active",
                userId, step.Number, group.Id);
            return FollowGroupHostResult.Failure(PomodoroErrors.Pomodoro.SessionAlreadyActive);
        }

        _logger.LogInformation(
            "Session {SessionId} of type {SessionType} follows step {StepNumber} of group session {GroupId} for user {UserId}",
            session.Id.Value, sessionType, step.Number, group.Id, userId);

        // Same entry as a start on the Pomodoro page (ADR-023). The session is persisted: a failing journal
        // must never fail the follow, and a client disconnect must not abort the entry.
        var eventType = sessionType == PomodoroSessionType.Sprint
            ? JournalEventType.SprintStarted
            : JournalEventType.BreakStarted;
        try
        {
            await _mediator.DispatchAsync<CreateEntryCommand, CreateEntryResult>(
                new CreateEntryCommand(eventType, session.Id.Value, now, userId),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Journal entry failed for session {SessionId}", session.Id.Value);
        }

        return FollowGroupHostResult.NewStep(stepType, stepEndsAt, xpAwarded);
    }

    /// <summary>Closes the user's own session of the group by the usual completion (journal and XP), at
    /// <paramref name="endedAtUtc"/> bounded by the session itself. Returns the XP credited.</summary>
    private async Task<int> CompleteAsync(PomodoroSession session, DateTime? endedAtUtc, CancellationToken cancellationToken)
    {
        var completed = await _mediator.DispatchAsync<CompleteSessionCommand, CompleteSessionResult>(
            new CompleteSessionCommand(session.Id.Value, endedAtUtc), cancellationToken);
        if (completed.IsSuccess)
            return completed.XpAwarded;

        // Another tab closed it first: nothing left to close.
        _logger.LogInformation(
            "Session {SessionId} not completed while following its group: {Error}", session.Id.Value, completed.Error);
        return 0;
    }

    // A datetime2 column comes back with an unspecified kind: all session instants are UTC.
    private static DateTimeOffset AsUtcOffset(DateTime utc) => new(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
}
