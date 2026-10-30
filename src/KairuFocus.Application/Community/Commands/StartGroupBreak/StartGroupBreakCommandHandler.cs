using KairuFocus.Application.Common;
using KairuFocus.Application.Journal.Commands.CreateEntry;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Application.Community.Commands.StartGroupBreak;

public sealed class StartGroupBreakCommandHandler : ICommandHandler<StartGroupBreakCommand, StartGroupBreakResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IOpenSprintReadRepository _openSprintRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<StartGroupBreakCommandHandler> _logger;

    public StartGroupBreakCommandHandler(
        IMemberProfileRepository profileRepository,
        IOpenSprintReadRepository openSprintRepository,
        IPomodoroSessionRepository sessionRepository,
        IMediator mediator,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<StartGroupBreakCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _openSprintRepository = openSprintRepository;
        _sessionRepository = sessionRepository;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<StartGroupBreakResult> Handle(
        StartGroupBreakCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var visitor = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (visitor is null)
            return StartGroupBreakResult.NoBreak();

        var active = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
        if (active is not null)
            return ResolveActiveSession(active);

        // Only a timed sprint that was completed opens a group break: an interrupted sprint, a break
        // (completed or interrupted) or a free sprint do not. The latest ended session decides, so that a
        // break already taken or left is never followed by another one.
        var latest = await _sessionRepository.GetLatestEndedAsync(userId, cancellationToken);
        if (latest is not
            {
                SessionType: PomodoroSessionType.Sprint,
                Status: PomodoroSessionStatus.Completed,
                PlannedDurationMinutes: > 0,
                ScheduledEndAt: { } sprintEnd,
            })
        {
            return StartGroupBreakResult.NoBreak();
        }

        var rootId = latest.SharedSprintId ?? latest.Id;
        var rules = await _openSprintRepository.FindGroupBreakAsync(rootId.Value, cancellationToken);
        if (rules is null)
            return StartGroupBreakResult.NoBreak();

        // The break ends a fixed length after the end of the common sprint, not after this request: every
        // member of the group gets the same end whenever they come back.
        var sharedEndAt = sprintEnd.AddMinutes(rules.ShortBreakMinutes);

        // Sampled as late as possible before the insert, like when joining a sprint: the reads above go
        // through EF's retry policy. Same instant for the planned duration and the start.
        var now = UtcNow;
        var created = PomodoroSession.CreateJoinedBreak(userId, rootId, sharedEndAt, now);
        if (created.IsFailure)
            return StartGroupBreakResult.NoBreak(); // The break is already over.

        var session = created.Value;
        if (session.Start(now).IsFailure)
            return StartGroupBreakResult.NoBreak();

        // The read above is a check-then-act: the "one active session per user" unique index is the real
        // guard, and TryAddAsync reports its rejection as false.
        if (!await _sessionRepository.TryAddAsync(session, cancellationToken))
            return await HandleRejectedInsertAsync(userId, cancellationToken);

        _logger.LogInformation("Session {SessionId} started a group break for user {UserId}", session.Id.Value, userId);

        // The session is already persisted: a failing journal must NEVER fail the break (ADR-023), and a
        // client disconnect must not abort the entry, which would be lost with no automatic replay.
        try
        {
            await _mediator.DispatchAsync<CreateEntryCommand, CreateEntryResult>(
                new CreateEntryCommand(JournalEventType.BreakStarted, session.Id.Value, now, userId),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Journal entry failed for session {SessionId}", session.Id.Value);
        }

        return StartGroupBreakResult.Success(AsUtcOffset(sharedEndAt));
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>
    /// The insert was rejected by the "at most one Active session per owner" invariant: another request
    /// won the race. Same decision as for a session found before the insert.
    /// </summary>
    private async Task<StartGroupBreakResult> HandleRejectedInsertAsync(UserId userId, CancellationToken cancellationToken)
    {
        var concurrent = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
        if (concurrent is not null)
            return ResolveActiveSession(concurrent);

        // Rejected but nothing readable: the winning row was already closed in between.
        _logger.LogWarning(
            "Group break insert rejected for user {UserId} but no active session could be read back",
            userId);
        return StartGroupBreakResult.Failure(PomodoroErrors.Pomodoro.ConcurrentSessionStart);
    }

    /// <summary>
    /// A session is active: a group break is the same request submitted again (second tab, double call),
    /// whatever its age, since it can only have been started by this use case. Anything else is a session
    /// the member is running, which a break must not replace.
    /// </summary>
    private StartGroupBreakResult ResolveActiveSession(PomodoroSession active)
    {
        if (active is
            {
                SessionType: PomodoroSessionType.ShortBreak,
                SharedSprintId: not null,
                SharedEndAt: { } sharedEndAt,
            })
        {
            _logger.LogInformation(
                "Duplicate group break ignored for user {UserId}: returning active session {SessionId}",
                active.OwnerId, active.Id.Value);
            return StartGroupBreakResult.Success(AsUtcOffset(sharedEndAt));
        }

        return StartGroupBreakResult.Failure(PomodoroErrors.Pomodoro.SessionAlreadyActive);
    }

    // A datetime2 column comes back with an unspecified kind: all session instants are UTC.
    private static DateTimeOffset AsUtcOffset(DateTime utc) => new(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
}
