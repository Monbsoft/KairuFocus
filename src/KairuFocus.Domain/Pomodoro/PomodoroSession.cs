using KairuFocus.Domain.Common;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Tasks;

namespace KairuFocus.Domain.Pomodoro;

public sealed class PomodoroSession : AggregateRoot<PomodoroSessionId>
{
    private readonly List<Guid> _linkedTaskIdValues = [];

    private PomodoroSession(
        PomodoroSessionId id,
        UserId ownerId,
        PomodoroSessionType sessionType,
        int plannedDurationMinutes,
        string? journalComment)
        : base(id)
    {
        OwnerId = ownerId;
        SessionType = sessionType;
        PlannedDurationMinutes = plannedDurationMinutes;
        Status = PomodoroSessionStatus.Planned;
        JournalComment = journalComment;
    }

    public UserId OwnerId { get; private set; } = default!;
    public PomodoroSessionType SessionType { get; }
    public PomodoroSessionStatus Status { get; private set; }
    public int PlannedDurationMinutes { get; }
    public string? JournalComment { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }

    // Backing field and no setter on purpose: EF Core discovers a settable property of a non-scalar type
    // as a navigation, and a model that does not map it explicitly can no longer be built. Mapped explicitly,
    // the property is read and written through this field.
    private PomodoroSessionId? _sharedSprintId;

    /// <summary>Root session of the shared sprint this session joined. Null for a host or a solo session.</summary>
    public PomodoroSessionId? SharedSprintId => _sharedSprintId;

    /// <summary>Exact end of the shared sprint or break this session joined: <see cref="PlannedDurationMinutes"/>,
    /// a whole number of minutes rounded up, cannot end to the second. Null unless joined.</summary>
    public DateTime? SharedEndAt { get; private set; }

    /// <summary>Whether the host closed this sprint to new members. Read on the root session.</summary>
    public bool IsClosedToJoin { get; private set; }

    public IReadOnlyList<TaskId> LinkedTaskIds =>
        _linkedTaskIdValues.ConvertAll(TaskId.From).AsReadOnly();

    // Public for EF Core materialization — domain consumers use LinkedTaskIds
    public List<Guid> LinkedTaskIdValues
    {
        get => _linkedTaskIdValues;
        init => _linkedTaskIdValues = value;
    }

    public static PomodoroSession Create(
        PomodoroSessionType sessionType,
        int plannedDurationMinutes,
        UserId ownerId,
        string? journalComment = null) =>
        new(PomodoroSessionId.New(), ownerId, sessionType, plannedDurationMinutes, journalComment);

    /// <summary>
    /// Creates the session of a member joining a shared sprint, not started yet.
    /// <paramref name="rootId"/> is the root of the group (the root of the target session when the
    /// target itself joined), <paramref name="sharedEndAt"/> its exact end.
    /// </summary>
    public static Result<PomodoroSession> CreateJoined(
        UserId ownerId,
        PomodoroSessionId rootId,
        DateTime sharedEndAt,
        DateTime now) =>
        CreateShared(PomodoroSessionType.Sprint, ownerId, rootId, sharedEndAt, now);

    /// <summary>
    /// Creates the short break that follows a shared sprint, not started yet. It keeps the group's
    /// <paramref name="rootId"/> and ends at <paramref name="sharedEndAt"/>, the same instant for every member.
    /// </summary>
    public static Result<PomodoroSession> CreateJoinedBreak(
        UserId ownerId,
        PomodoroSessionId rootId,
        DateTime sharedEndAt,
        DateTime now) =>
        CreateShared(PomodoroSessionType.ShortBreak, ownerId, rootId, sharedEndAt, now);

    private static Result<PomodoroSession> CreateShared(
        PomodoroSessionType sessionType,
        UserId ownerId,
        PomodoroSessionId rootId,
        DateTime sharedEndAt,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(rootId);

        if (now >= sharedEndAt)
            return Result.Failure<PomodoroSession>(DomainErrors.Pomodoro.SharedSprintEnded);

        var remainingMinutes = (int)Math.Ceiling((sharedEndAt - now).TotalMinutes);

        return Result.Success(
            new PomodoroSession(PomodoroSessionId.New(), ownerId, sessionType, remainingMinutes, journalComment: null)
            {
                _sharedSprintId = rootId,
                SharedEndAt = sharedEndAt,
            });
    }

    public Result Start(DateTime now)
    {
        if (Status != PomodoroSessionStatus.Planned)
            return Result.Failure(DomainErrors.Pomodoro.InvalidTransition);

        // Otherwise the end time would be bounded before the start of the session.
        if (now >= SharedEndAt)
            return Result.Failure(DomainErrors.Pomodoro.SharedSprintEnded);

        Status = PomodoroSessionStatus.Active;
        StartedAt = now;
        return Result.Success();
    }

    public Result Complete(DateTime now)
    {
        if (Status != PomodoroSessionStatus.Active)
            return Result.Failure(DomainErrors.Pomodoro.SessionNotActive);

        Status = PomodoroSessionStatus.Completed;
        EndedAt = ResolveEndedAt(now);
        return Result.Success();
    }

    public Result Interrupt(DateTime now)
    {
        if (Status != PomodoroSessionStatus.Active)
            return Result.Failure(DomainErrors.Pomodoro.SessionNotActive);

        Status = PomodoroSessionStatus.Interrupted;
        EndedAt = ResolveEndedAt(now);
        return Result.Success();
    }

    /// <summary>Stops new members from joining this sprint.</summary>
    public Result CloseToJoin() => SetClosedToJoin(true);

    /// <summary>Lets new members join this sprint again.</summary>
    public Result ReopenToJoin() => SetClosedToJoin(false);

    /// <summary>Idempotent: closing a closed sprint or reopening an open one succeeds.</summary>
    private Result SetClosedToJoin(bool isClosed)
    {
        if (Status != PomodoroSessionStatus.Active)
            return Result.Failure(DomainErrors.Pomodoro.SessionNotActive);

        var isRoot = SessionType == PomodoroSessionType.Sprint
                     && PlannedDurationMinutes > 0
                     && SharedSprintId is null;
        if (!isRoot)
            return Result.Failure(DomainErrors.Pomodoro.NotSharedSprintRoot);

        IsClosedToJoin = isClosed;
        return Result.Success();
    }

    /// <summary>Scheduled end of a timed session: the exact end of the group for a joined session,
    /// otherwise start + planned duration. Null for a free sprint (PlannedDurationMinutes == 0)
    /// or a session that has not started.</summary>
    public DateTime? ScheduledEndAt =>
        SharedEndAt ?? (PlannedDurationMinutes > 0 && StartedAt.HasValue
            ? StartedAt.Value.AddMinutes(PlannedDurationMinutes)
            : null);

    /// <summary>End time retained for a completion or interruption observed at <paramref name="now"/>:
    /// never before the start, never after the scheduled end of a timed session.</summary>
    private DateTime ResolveEndedAt(DateTime now)
    {
        if (StartedAt.HasValue && now < StartedAt.Value)
            return StartedAt.Value;

        return ScheduledEndAt is { } scheduledEnd && now > scheduledEnd
            ? scheduledEnd
            : now;
    }

    public Result LinkTask(TaskId taskId)
    {
        if (Status != PomodoroSessionStatus.Active)
            return Result.Failure(DomainErrors.Pomodoro.SessionNotActive);

        if (SessionType != PomodoroSessionType.Sprint)
            return Result.Failure(DomainErrors.Pomodoro.TaskLinkingNotAllowedForBreak);

        if (_linkedTaskIdValues.Contains(taskId.Value))
            return Result.Failure(DomainErrors.Pomodoro.TaskAlreadyLinked);

        _linkedTaskIdValues.Add(taskId.Value);
        return Result.Success();
    }

    public Result UnlinkTask(TaskId taskId)
    {
        if (Status != PomodoroSessionStatus.Active)
            return Result.Failure(DomainErrors.Pomodoro.SessionNotActive);

        if (!_linkedTaskIdValues.Remove(taskId.Value))
            return Result.Failure(DomainErrors.Pomodoro.TaskNotLinked);

        return Result.Success();
    }
}
