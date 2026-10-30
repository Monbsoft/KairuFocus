using KairuFocus.Application.Common;
using KairuFocus.Application.Journal.Commands.CreateEntry;
using KairuFocus.Application.Pomodoro.Common;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Application.Community.Commands.JoinOpenSprint;

public sealed class JoinOpenSprintCommandHandler : ICommandHandler<JoinOpenSprintCommand, JoinOpenSprintResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IOpenSprintReadRepository _openSprintRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<JoinOpenSprintCommandHandler> _logger;

    public JoinOpenSprintCommandHandler(
        IMemberProfileRepository profileRepository,
        IOpenSprintReadRepository openSprintRepository,
        IPomodoroSessionRepository sessionRepository,
        IMediator mediator,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<JoinOpenSprintCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _openSprintRepository = openSprintRepository;
        _sessionRepository = sessionRepository;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<JoinOpenSprintResult> Handle(
        JoinOpenSprintCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var visitor = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (visitor is null)
            return JoinOpenSprintResult.Failure(CommunityErrors.Community.NotMember);

        // A malformed name is held by nobody: same answer as an unknown one.
        var targetName = ProfileName.Create(command.Name);
        if (targetName.IsFailure)
            return JoinOpenSprintResult.Failure(CommunityErrors.Community.SprintNotJoinable);

        if (targetName.Value.Key == visitor.Name.Key)
            return JoinOpenSprintResult.Failure(CommunityErrors.Community.CannotJoinOwnSprint);

        var sprint = await _openSprintRepository.FindJoinableAsync(targetName.Value, UtcNow, cancellationToken);
        if (sprint is null)
            return JoinOpenSprintResult.Failure(CommunityErrors.Community.SprintNotJoinable);

        var rootId = PomodoroSessionId.From(sprint.RootSessionId);

        var active = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
        if (active is not null)
            return ResolveActiveSession(active, rootId);

        // Sampled as late as possible before the insert: the reads above go through EF's retry policy, and
        // the client derives the elapsed time from StartedAt. Same instant for the planned duration and the
        // start, so that the duration is the time actually left.
        var now = UtcNow;
        var joined = PomodoroSession.CreateJoined(userId, rootId, sprint.EndsAtUtc, now);
        if (joined.IsFailure)
            return JoinOpenSprintResult.Failure(CommunityErrors.Community.SprintNotJoinable);

        var session = joined.Value;
        if (session.Start(now).IsFailure)
            return JoinOpenSprintResult.Failure(CommunityErrors.Community.SprintNotJoinable);

        // The read above is a check-then-act: the "one active session per user" unique index is the real
        // guard, and TryAddAsync reports its rejection as false.
        if (!await _sessionRepository.TryAddAsync(session, cancellationToken))
            return await HandleRejectedInsertAsync(userId, rootId, cancellationToken);

        _logger.LogInformation("Session {SessionId} joined a shared sprint for user {UserId}", session.Id.Value, userId);

        // The session is already persisted: a failing journal must NEVER fail the join (ADR-023), and a
        // client disconnect must not abort the entry, which would be lost with no automatic replay.
        try
        {
            await _mediator.DispatchAsync<CreateEntryCommand, CreateEntryResult>(
                new CreateEntryCommand(JournalEventType.SprintStarted, session.Id.Value, now, userId),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Journal entry failed for session {SessionId}", session.Id.Value);
        }

        return JoinOpenSprintResult.Success(AsUtcOffset(sprint.EndsAtUtc));
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>
    /// The insert was rejected by the "at most one Active session per owner" invariant: another request
    /// won the race. Same decision as for a session found before the insert.
    /// </summary>
    private async Task<JoinOpenSprintResult> HandleRejectedInsertAsync(
        UserId userId,
        PomodoroSessionId rootId,
        CancellationToken cancellationToken)
    {
        var concurrent = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
        if (concurrent is not null)
            return ResolveActiveSession(concurrent, rootId);

        // Rejected but nothing readable: the winning row was already closed in between.
        _logger.LogWarning(
            "Joined session insert rejected for user {UserId} but no active session could be read back",
            userId);
        return JoinOpenSprintResult.Failure(PomodoroErrors.Pomodoro.ConcurrentSessionStart);
    }

    /// <summary>
    /// A session is active: it is the same join submitted again only if it joined this very group within
    /// the idempotency window. Compared on <see cref="PomodoroSession.SharedSprintId"/>: the host of the group
    /// shares its root but never joined it. Must be called right after the read, so that the instant
    /// compared is sampled after it.
    /// </summary>
    private JoinOpenSprintResult ResolveActiveSession(PomodoroSession active, PomodoroSessionId rootId)
    {
        if (active.SharedSprintId == rootId
            && active is { StartedAt: { } startedAt, SharedEndAt: { } sharedEndAt }
            && SessionStartIdempotency.IsResubmission(startedAt, UtcNow))
        {
            _logger.LogInformation(
                "Duplicate join ignored for user {UserId}: returning active session {SessionId}",
                active.OwnerId, active.Id.Value);
            return JoinOpenSprintResult.Success(AsUtcOffset(sharedEndAt));
        }

        return JoinOpenSprintResult.Failure(PomodoroErrors.Pomodoro.SessionAlreadyActive);
    }

    // A datetime2 column comes back with an unspecified kind: all session instants are UTC.
    private static DateTimeOffset AsUtcOffset(DateTime utc) => new(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
}
