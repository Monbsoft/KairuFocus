using KairuFocus.Application.Common;
using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Journal.Commands.CreateEntry;
using KairuFocus.Application.Pomodoro.Common;
using KairuFocus.Domain.Common;
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
    private readonly IGroupSessionRepository _groupRepository;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<JoinOpenSprintCommandHandler> _logger;

    public JoinOpenSprintCommandHandler(
        IMemberProfileRepository profileRepository,
        IOpenSprintReadRepository openSprintRepository,
        IPomodoroSessionRepository sessionRepository,
        IGroupSessionRepository groupRepository,
        IMediator mediator,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<JoinOpenSprintCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _openSprintRepository = openSprintRepository;
        _sessionRepository = sessionRepository;
        _groupRepository = groupRepository;
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

        // No step to enter until the host launches the next one.
        if (sprint.Phase == GroupPhase.WaitingForHost)
            return JoinOpenSprintResult.Failure(CommunityErrors.Community.GroupWaitingForHost);

        var rootId = PomodoroSessionId.From(sprint.RootSessionId);

        var active = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
        if (active is not null)
            return ResolveActiveSession(active, rootId);

        // At most one group not ended per user, as host or as active member (spec §3.3).
        var latest = await _groupRepository.FindLatestForUserAsync(userId, cancellationToken);
        if (latest is not null
            && latest.Id != sprint.RootSessionId
            && await IsStillRunningAsync(latest, cancellationToken))
            return JoinOpenSprintResult.Failure(CommunityErrors.Community.AlreadyInGroup);

        var group = await _groupRepository.GetByIdAsync(sprint.RootSessionId, cancellationToken);
        PomodoroSession? root = null;
        if (group is null)
        {
            // Nobody joined yet: the group is born from the host's sprint, the root session.
            root = await _sessionRepository.GetByIdAsync(rootId, cancellationToken);
            if (root is not { Status: PomodoroSessionStatus.Active, OwnerId: not null, StartedAt: not null, ScheduledEndAt: not null })
                return JoinOpenSprintResult.Failure(CommunityErrors.Community.SprintNotJoinable);
        }

        // Sampled after the reads, as late as possible before the writes: the reads go through EF's retry
        // policy, and the client derives the elapsed time from StartedAt. Same instant for the membership,
        // the planned duration and the start, so that the duration is the time actually left.
        var now = UtcNow;

        // Decision 2: the group is written before the Pomodoro session.
        var membership = root is null
            ? await JoinGroupAsync(group!, userId, now, cancellationToken)
            : await CreateGroupAsync(root, userId, now, cancellationToken);
        if (membership.IsFailure)
            return JoinOpenSprintResult.Failure(membership.Error);

        group = membership.Value;
        var step = group.CurrentStep;
        var sessionType = step.Type.ToSessionType();
        var joined = PomodoroSession.CreateJoinedStep(sessionType, userId, rootId, step.EndsAtUtc, now);
        if (joined.IsFailure || joined.Value.Start(now).IsFailure)
        {
            await LeaveGroupAsync(group, userId, cancellationToken);
            return JoinOpenSprintResult.Failure(CommunityErrors.Community.SprintNotJoinable);
        }

        var session = joined.Value;

        // The read above is a check-then-act: the "one active session per user" unique index is the real
        // guard, and TryAddAsync reports its rejection as false.
        if (!await _sessionRepository.TryAddAsync(session, cancellationToken))
        {
            var rejected = await HandleRejectedInsertAsync(userId, rootId, cancellationToken);

            // A resubmission keeps the membership the winning request recorded. Otherwise the member has no
            // session in this group: the membership just written is withdrawn.
            if (!rejected.IsSuccess)
                await LeaveGroupAsync(group, userId, cancellationToken);

            return rejected;
        }

        _logger.LogInformation(
            "Session {SessionId} of type {SessionType} joined a shared step for user {UserId}",
            session.Id.Value, sessionType, userId);

        // Same entry as a start on the Pomodoro page (ADR-023): a break joined is a break started.
        var eventType = sessionType == PomodoroSessionType.Sprint
            ? JournalEventType.SprintStarted
            : JournalEventType.BreakStarted;

        // The session is already persisted: a failing journal must NEVER fail the join (ADR-023), and a
        // client disconnect must not abort the entry, which would be lost with no automatic replay.
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

        return JoinOpenSprintResult.Success(AsUtcOffset(step.EndsAtUtc));
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Whether another group still holds the user. A host absence the group only reports is recorded right
    /// away: left pending on a tracked aggregate, it would be written by the next save of this request and
    /// could make it fail on that group's version.
    /// </summary>
    private async Task<bool> IsStillRunningAsync(GroupSession other, CancellationToken cancellationToken)
    {
        var now = UtcNow;
        if (other.EndIfHostAbsent(now))
            await SaveEndRecordedAsync(other, cancellationToken);

        return other.StateAt(now).Phase != GroupPhase.Ended;
    }

    /// <summary>
    /// First member: creates the group from the host's sprint. The host's id goes from the root session into
    /// the aggregate only. When a concurrent request created it first, joins that group instead.
    /// </summary>
    private async Task<Result<GroupSession>> CreateGroupAsync(
        PomodoroSession root,
        UserId userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var groupId = root.Id.Value;
        var started = GroupSession.Start(
            groupId, root.OwnerId, root.StartedAt!.Value, root.ScheduledEndAt!.Value, userId, now);
        if (started.IsFailure)
            return Result.Failure<GroupSession>(ToJoinError(started.Error));

        if (await _groupRepository.TryAddAsync(started.Value, cancellationToken))
        {
            _logger.LogInformation("Group session {GroupId} created by the join of user {UserId}", groupId, userId);
            return started;
        }

        var concurrent = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (concurrent is null)
        {
            _logger.LogWarning(
                "Group session {GroupId} insert rejected for user {UserId} but the group could not be read back",
                groupId, userId);
            return Result.Failure<GroupSession>(CommunityErrors.Community.SprintNotJoinable);
        }

        return await JoinGroupAsync(concurrent, userId, now, cancellationToken);
    }

    /// <summary>
    /// Joins an existing group. A member already active in it is a concurrent join of the same user (double
    /// click, second tab): nothing to write, the session insert and its idempotency window decide.
    /// </summary>
    private async Task<Result<GroupSession>> JoinGroupAsync(
        GroupSession group,
        UserId userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var endRecorded = group.EndIfHostAbsent(now);

        var joined = group.Join(userId, now);
        if (joined.IsFailure)
        {
            if (endRecorded)
                await SaveEndRecordedAsync(group, cancellationToken);

            return joined.Error == CommunityErrors.Community.AlreadyInGroup
                ? Result.Success(group)
                : Result.Failure<GroupSession>(ToJoinError(joined.Error));
        }

        // Join writes member rows only: a conflict means the group row itself changed in between (step
        // launched, stopped, closed). The card reads its state again.
        if (!await _groupRepository.TryUpdateAsync(group, cancellationToken))
        {
            _logger.LogWarning("Join of user {UserId} to group session {GroupId} rejected by a concurrent write", userId, group.Id);
            return Result.Failure<GroupSession>(CommunityErrors.Community.SprintNotJoinable);
        }

        return Result.Success(group);
    }

    /// <summary>Withdraws a membership written by this request when no session can follow it.</summary>
    private async Task LeaveGroupAsync(GroupSession group, UserId userId, CancellationToken cancellationToken)
    {
        if (group.Leave(userId, UtcNow).IsSuccess
            && !await _groupRepository.TryUpdateAsync(group, cancellationToken))
        {
            _logger.LogWarning(
                "Departure of user {UserId} from group session {GroupId} rejected by a concurrent write",
                userId, group.Id);
        }
    }

    /// <summary>Writes the host absence a read noticed. Losing the race is fine: another request wrote the
    /// group first, and the absence stays computed by <see cref="GroupSession.StateAt"/>.</summary>
    private async Task SaveEndRecordedAsync(GroupSession group, CancellationToken cancellationToken)
    {
        if (!await _groupRepository.TryUpdateAsync(group, cancellationToken))
            _logger.LogInformation("Host absence of group session {GroupId} already recorded by another request", group.Id);
    }

    /// <summary>An ended or closed group looks like any sprint that cannot be joined (410).</summary>
    private static string ToJoinError(string error) =>
        error is CommunityErrors.Community.GroupEnded
            or CommunityErrors.Community.GroupClosed
            or CommunityErrors.Community.InvalidGroupStep
            ? CommunityErrors.Community.SprintNotJoinable
            : error;

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
